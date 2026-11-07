using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Moka.Red.Core.Theming;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Core.Base;

/// <summary>
///     Performance-focused base class for all Moka.Red components.
///     Provides parameter change tracking, safe JS interop, CSS class composition,
///     and proper async disposal.
/// </summary>
public abstract class MokaComponentBase : ComponentBase, IAsyncDisposable
{
	// What IJSRuntime.InvokeAsync asks the trimmer to keep on a type it reads a result into.
	private const DynamicallyAccessedMemberTypes JsonSerialized = DynamicallyAccessedMemberTypes.PublicConstructors |
	                                                              DynamicallyAccessedMemberTypes.PublicFields |
	                                                              DynamicallyAccessedMemberTypes.PublicProperties;

	private bool _disposed;
	private IMokaJsInteropObserver? _jsObserver;
	private bool _jsObserverLookedUp;
	private volatile bool _parametersChanged = true;

	[Inject] private IJSRuntime JsRuntime { get; set; } = default!;

	// The observer is optional and [Inject] has no optional form, so it is looked up through the
	// provider on the first JS call.
	[Inject] private IServiceProvider ServiceProvider { get; set; } = default!;

	/// <summary>User-provided CSS classes appended to the root element.</summary>
	[Parameter]
	public string? Class { get; set; }

	/// <summary>User-provided inline styles applied to the root element.</summary>
	[Parameter]
	public string? Style { get; set; }

	/// <summary>HTML id attribute for the root element.</summary>
	[Parameter]
	public string? Id { get; set; }

	/// <summary>Additional unmatched HTML attributes splatted onto the root element.</summary>
	[Parameter(CaptureUnmatchedValues = true)]
	public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

	/// <summary>
	///     The current theme, from the nearest <see cref="MokaThemeProvider" /> or the app's registered theme.
	///     Reading it doesn't redraw the component when the theme changes, and nothing in a theme needs that
	///     unless C# computes from it: CSS variables carry it to the page. A component that does compute from it
	///     declares its own <c>[CascadingParameter] MokaTheme?</c>, which re-renders it on a change.
	/// </summary>
	public MokaTheme? Theme => ThemeSource?.Theme;

	[CascadingParameter]
	private MokaThemeSource? ThemeSource { get; set; }

	/// <summary>
	///     The root CSS class for this component (e.g., "moka-button").
	///     Every component must declare its own root class.
	/// </summary>
	protected abstract string RootClass { get; }

	/// <summary>
	///     Computed CSS class string combining <see cref="RootClass" /> and user <see cref="Class" />.
	///     Override in derived classes to add conditional classes.
	/// </summary>
	protected virtual string CssClass => new CssBuilder(RootClass)
		.AddClass(Class)
		.Build();

	/// <summary>
	///     Computed inline style string. Returns user <see cref="Style" /> by default.
	///     Override in derived classes to add computed styles.
	/// </summary>
	protected virtual string? CssStyle => Style;

	/// <summary>Whether this component has completed its first render.</summary>
	protected bool HasRendered { get; private set; }

	public async ValueTask DisposeAsync()
	{
		if (_disposed)
		{
			return;
		}

		_disposed = true;

		try
		{
			await DisposeAsyncCore();
		}
		catch (Exception ex) when (MokaJsInterop.IsCircuitGone(ex))
		{
			// A JS call in a derived class's cleanup, a consumer's too, met a circuit that had already
			// ended. Blazor would log it as an error of the circuit, though the browser is already gone.
		}

		GC.SuppressFinalize(this);
	}

	protected override void OnParametersSet() => _parametersChanged = true;

	protected override bool ShouldRender()
	{
		if (!_parametersChanged)
		{
			return false;
		}

		_parametersChanged = false;
		return true;
	}

	protected override void OnAfterRender(bool firstRender)
	{
		if (firstRender)
		{
			HasRendered = true;
		}
	}

	/// <summary>
	///     Force a re-render on the next render cycle. Use sparingly -
	///     prefer parameter changes for triggering renders.
	/// </summary>
	protected void ForceRender()
	{
		_parametersChanged = true;
		StateHasChanged();
	}

	/// <summary>
	///     Runs work nothing can await, such as a timer's callback or a handler raised from a synchronous
	///     path, on the renderer's thread. A failure reaches the nearest error boundary like one from a
	///     lifecycle method, where a discarded task would lose it and an async void callback would end the
	///     process. Once the component is disposed the work no longer starts, and a failure in work already
	///     running has no one left to reach.
	/// </summary>
	/// <param name="work">The work to run.</param>
	protected void RunDetached(Func<Task> work) =>
		_ = RunDetachedAsync(() => InvokeAsync(() => _disposed ? Task.CompletedTask : work()));

	/// <inheritdoc cref="RunDetached(Func{Task})" />
	protected void RunDetached(Action work) =>
		_ = RunDetachedAsync(() => InvokeAsync(() =>
		{
			if (!_disposed)
			{
				work();
			}
		}));

	// Starting inside this method is what keeps a timer's thread safe: anything thrown here ends up in
	// the discarded task, never on the thread pool.
	private async Task RunDetachedAsync(Func<Task> start)
	{
		try
		{
			await start();
		}
		catch (Exception ex) when (!_disposed)
		{
			await DispatchExceptionAsync(ex);
		}
	}

	/// <summary>
	///     Gets a JS module, imported once per path for the whole circuit or page and shared with every
	///     other component that asks for it. Do not dispose what this returns: other components hold it too.
	/// </summary>
	/// <param name="modulePath">
	///     Path relative to wwwroot, e.g., "./_content/Moka.Red.Core/Components/Button/MokaButton.razor.js"
	/// </param>
	protected async ValueTask<IJSObjectReference> GetJsModuleAsync(string modulePath)
	{
		long start = Stopwatch.GetTimestamp();
		IJSObjectReference module = await MokaJsModules.Get(JsRuntime, modulePath, out bool started);
		if (started)
		{
			ReportJsCall("import", start);
		}

		return module;
	}

	/// <summary>
	///     Invokes a JS function with exception handling for disconnected circuits
	///     and prerendering scenarios.
	/// </summary>
	protected async ValueTask<T> SafeJsInvokeAsync<[DynamicallyAccessedMembers(JsonSerialized)] T>(
		string identifier, params object?[] args)
	{
		try
		{
			long start = Stopwatch.GetTimestamp();
			T result = await JsRuntime.InvokeAsync<T>(identifier, args);
			ReportJsCall(identifier, start);
			return result;
		}
		catch (Exception ex) when (MokaJsInterop.IsCircuitGone(ex))
		{
			return default!;
		}
		catch (InvalidOperationException) when (!HasRendered)
		{
			// JS interop called during prerendering - silently ignore
			return default!;
		}
	}

	/// <summary>
	///     Invokes a void JS function with exception handling for disconnected circuits
	///     and prerendering scenarios.
	/// </summary>
	protected async ValueTask SafeJsInvokeVoidAsync(string identifier, params object?[] args)
	{
		try
		{
			long start = Stopwatch.GetTimestamp();
			await JsRuntime.InvokeVoidAsync(identifier, args);
			ReportJsCall(identifier, start);
		}
		catch (Exception ex) when (MokaJsInterop.IsCircuitGone(ex))
		{
		}
		catch (InvalidOperationException) when (!HasRendered)
		{
			// JS interop called during prerendering - silently ignore
		}
	}

	/// <summary>
	///     Invokes a function on the module <see cref="GetJsModuleAsync" /> returns and gives back its
	///     result, or <c>default</c> when a lost circuit or prerendering stopped the call.
	/// </summary>
	protected async ValueTask<T?> SafeModuleInvokeAsync<[DynamicallyAccessedMembers(JsonSerialized)] T>(
		string modulePath, string identifier, params object?[] args)
	{
		try
		{
			IJSObjectReference module = await GetJsModuleAsync(modulePath);
			long start = Stopwatch.GetTimestamp();
			T result = await module.InvokeAsync<T>(identifier, args);
			ReportJsCall(identifier, start);
			return result;
		}
		catch (Exception ex) when (MokaJsInterop.IsCircuitGone(ex))
		{
			return default;
		}
		catch (InvalidOperationException) when (!HasRendered)
		{
			return default;
		}
	}

	/// <summary>
	///     Moves focus to an element, swallowing the ways that can fail through no fault of the caller: the
	///     circuit going away, the element leaving the DOM before the call lands, and prerendering, where there
	///     is no browser to focus anything.
	/// </summary>
	/// <param name="element">The element to focus.</param>
	/// <returns>A task that completes when the call has been made.</returns>
	protected static async ValueTask SafeFocusAsync(ElementReference element)
	{
		try
		{
			await element.FocusAsync();
		}
		catch (Exception ex) when (MokaJsInterop.IsCircuitGone(ex))
		{
		}
		catch (JSException)
		{
			// The element left the DOM before the call reached the browser.
		}
		catch (InvalidOperationException)
		{
			// No JS runtime (prerender), or the reference was never attached to an element.
		}
	}

	/// <summary>
	///     Invokes a void function on the module <see cref="GetJsModuleAsync" /> returns, with the
	///     same exception handling as <see cref="SafeJsInvokeVoidAsync" />.
	/// </summary>
	protected async ValueTask SafeModuleInvokeVoidAsync(string modulePath, string identifier, params object?[] args)
	{
		try
		{
			IJSObjectReference module = await GetJsModuleAsync(modulePath);
			long start = Stopwatch.GetTimestamp();
			await module.InvokeVoidAsync(identifier, args);
			ReportJsCall(identifier, start);
		}
		catch (Exception ex) when (MokaJsInterop.IsCircuitGone(ex))
		{
		}
		catch (InvalidOperationException) when (!HasRendered)
		{
			// JS interop called during prerendering - silently ignore
		}
	}

	/// <summary>
	///     Override this to dispose component-specific resources.
	///     Base implementation is a no-op. Always call base when overriding.
	/// </summary>
	protected virtual ValueTask DisposeAsyncCore() => ValueTask.CompletedTask;

	private void ReportJsCall(string identifier, long startTimestamp)
	{
		// This reports; it never breaks the component it reports for. A component whose JS call finishes while its
		// scope is being torn down — a circuit ending under the very render the call belongs to — can reach this
		// after there is anything left to resolve: an app start-up crash has traced to exactly this ending an
		// OnAfterRender chain, so both the lookup and the observer's callback are guarded.
		try
		{
			if (!_jsObserverLookedUp)
			{
				_jsObserverLookedUp = true;
				_jsObserver = ServiceProvider?.GetService(typeof(IMokaJsInteropObserver)) as IMokaJsInteropObserver;
			}

			_jsObserver?.OnJsInteropCompleted(identifier, Stopwatch.GetElapsedTime(startTimestamp));
		}
		catch (Exception ex) when (ex is ObjectDisposedException or InvalidOperationException or NullReferenceException)
		{
			// A call that finishes while the scope is torn down (a component disposing itself as its circuit ends)
			// has no one left to report to.
		}
	}
}
