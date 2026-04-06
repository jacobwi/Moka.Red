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
	private bool _disposed;
	private IJSObjectReference? _jsModule;
	private SemaphoreSlim? _jsModuleLock;
	private volatile bool _parametersChanged = true;

	[Inject] private IJSRuntime JsRuntime { get; set; } = default!;

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

	/// <summary>Theme cascaded from <see cref="MokaThemeProvider" />.</summary>
	[CascadingParameter]
	public MokaTheme? Theme { get; set; }

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

		await DisposeAsyncCore();

		if (_jsModule is not null)
		{
			try
			{
				await _jsModule.DisposeAsync();
			}
			catch (JSDisconnectedException)
			{
				// Circuit already disconnected — module is already gone
			}

			_jsModule = null;
		}

		_jsModuleLock?.Dispose();
		_jsModuleLock = null;

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
	///     Force a re-render on the next render cycle. Use sparingly —
	///     prefer parameter changes for triggering renders.
	/// </summary>
	protected void ForceRender()
	{
		_parametersChanged = true;
		StateHasChanged();
	}

	/// <summary>
	///     Lazily imports a collocated JS module (.razor.js). The module is cached
	///     for the lifetime of the component and disposed automatically.
	///     Thread-safe: concurrent callers are serialized via SemaphoreSlim.
	/// </summary>
	/// <param name="modulePath">
	///     Path relative to wwwroot, e.g., "./_content/Moka.Red.Core/Components/Button/MokaButton.razor.js"
	/// </param>
	[SuppressMessage("Reliability", "CA1508:Avoid dead conditional code",
		Justification = "Double-checked locking — _jsModule may be set between outer check and lock acquisition")]
	protected async ValueTask<IJSObjectReference> GetJsModuleAsync(string modulePath)
	{
		if (_jsModule is not null)
		{
			return _jsModule;
		}

		_jsModuleLock ??= new SemaphoreSlim(1, 1);
		await _jsModuleLock.WaitAsync();
		try
		{
			return _jsModule ??= await JsRuntime.InvokeAsync<IJSObjectReference>("import", modulePath);
		}
		finally
		{
			_jsModuleLock.Release();
		}
	}

	/// <summary>
	///     Invokes a JS function with exception handling for disconnected circuits
	///     and prerendering scenarios.
	/// </summary>
	protected async ValueTask<T> SafeJsInvokeAsync<T>(string identifier, params object?[] args)
	{
		try
		{
			return await JsRuntime.InvokeAsync<T>(identifier, args);
		}
		catch (JSDisconnectedException)
		{
			return default!;
		}
		catch (InvalidOperationException) when (!HasRendered)
		{
			// JS interop called during prerendering — silently ignore
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
			await JsRuntime.InvokeVoidAsync(identifier, args);
		}
		catch (JSDisconnectedException)
		{
			// Circuit disconnected — nothing to do
		}
		catch (InvalidOperationException) when (!HasRendered)
		{
			// JS interop called during prerendering — silently ignore
		}
	}

	/// <summary>
	///     Override this to dispose component-specific resources.
	///     Base implementation is a no-op. Always call base when overriding.
	/// </summary>
	protected virtual ValueTask DisposeAsyncCore() => ValueTask.CompletedTask;
}
