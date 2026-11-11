using System.Diagnostics;
using System.Linq.Expressions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using Moka.Red.Core.Theming;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Core.Base;

/// <summary>
///     Performance-focused base class for Moka.Red form input components.
///     Extends <see cref="InputBase{TValue}" /> with theming, CSS composition,
///     and proper disposal.
/// </summary>
/// <typeparam name="TValue">The type of the input value.</typeparam>
public abstract class MokaInputBase<TValue> : InputBase<TValue>, IAsyncDisposable
{
	private bool _boundToField;
	private bool _disposed;
	private bool _fieldNameUnavailable;
	private IMokaJsInteropObserver? _jsObserver;
	private bool _jsObserverLookedUp;

	[Inject] private IJSRuntime JsRuntime { get; set; } = default!;

	// The observer is optional and [Inject] has no optional form, so it is looked up through the
	// provider on the first JS call, as in MokaComponentBase.
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
	///     The root CSS class for this input component (e.g., "moka-input").
	///     Every input component must declare its own root class.
	/// </summary>
	protected abstract string RootClass { get; }

	/// <summary>
	///     Computed CSS class string combining <see cref="RootClass" />,
	///     validation state classes from <see cref="InputBase{TValue}.CssClass" />,
	///     and user <see cref="Class" />.
	/// </summary>
	protected virtual string ComponentCssClass => new CssBuilder(RootClass)
		.AddClass(CssClass) // InputBase validation classes (valid/invalid/modified)
		.AddClass(Class)
		.Build();

	/// <summary>
	///     Validation messages the cascaded <see cref="EditContext" /> currently holds for this field.
	///     Empty when the input renders outside an <c>EditForm</c>.
	/// </summary>
	protected IEnumerable<string> ValidationMessages =>
		EditContext is not null ? EditContext.GetValidationMessages(FieldIdentifier) : [];

	/// <summary>
	///     True when the cascaded <see cref="EditContext" /> reports at least one validation
	///     message for this field. Components combine this with their own <c>ErrorText</c> so
	///     DataAnnotations results are actually visible.
	/// </summary>
	protected bool HasValidationError => ValidationMessages.Any();

	/// <summary>First validation message for this field, or <c>null</c> when there is none.</summary>
	protected string? ValidationErrorText => ValidationMessages.FirstOrDefault();

	/// <summary>
	///     Computed inline style string. Returns user <see cref="Style" /> by default.
	/// </summary>
	protected virtual string? ComponentStyle => Style;

	/// <summary>
	///     The name a form post carries this input's value under, for the element that holds the value.
	///     Under static server rendering a field with no name isn't posted at all. Null when nothing ties
	///     the input to a field, in the browser, where nothing posts, and for a binding that can't be
	///     written as a name.
	/// </summary>
	protected string? FieldName
	{
		get
		{
			if (AdditionalAttributes?.ContainsKey("name") != true && (!_boundToField || _fieldNameUnavailable))
			{
				return null;
			}

			try
			{
				string name = NameAttributeValue;
				return name.Length == 0 ? null : name;
			}
			catch (InvalidOperationException)
			{
				// Blazor can't format a binding like @bind-Value="Find().Title" and its own InputText
				// throws here. An interactive app runs such a binding fine and has no use for the name.
				_fieldNameUnavailable = true;
				return null;
			}
		}
	}

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
			// A JS call in a derived class's cleanup met a circuit that had already ended.
		}
		finally
		{
			// Blazor calls only DisposeAsync on a component that has one, so InputBase's Dispose, which
			// unsubscribes from the EditContext, never ran and a form kept every removed input alive.
			((IDisposable)this).Dispose();
		}

		GC.SuppressFinalize(this);
	}

	/// <inheritdoc />
	/// <remarks>
	///     Provides a no-op <see cref="InputBase{TValue}.ValueExpression" /> when none is supplied,
	///     so that components can be rendered outside of an <see cref="EditForm" /> (e.g. in
	///     static SSR previews) without throwing a missing-parameter exception.
	/// </remarks>
	public override Task SetParametersAsync(ParameterView parameters)
	{
		if (parameters.TryGetValue<Expression<Func<TValue>>>(nameof(ValueExpression), out _))
		{
			_boundToField = true;
		}
		else
		{
			ValueExpression = () => Value!;
		}

		return base.SetParametersAsync(parameters);
	}

	protected override void OnAfterRender(bool firstRender)
	{
		if (firstRender)
		{
			HasRendered = true;
		}
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
	///     Invokes a void function on the module <see cref="GetJsModuleAsync" /> returns, and
	///     swallows the exceptions a lost circuit or prerendering throws.
	/// </summary>
	protected async ValueTask SafeModuleInvokeVoidAsync(string modulePath, string identifier, params object?[] args)
	{
		try
		{
			IJSObjectReference? module = await GetJsModuleAsync(modulePath);
			if (module is null)
			{
				return;
			}

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
	/// </summary>
	protected virtual ValueTask DisposeAsyncCore() => ValueTask.CompletedTask;

	// Same reporting as MokaComponentBase, so the Network tab also counts the calls inputs make.
	private void ReportJsCall(string identifier, long startTimestamp)
	{
		if (!_jsObserverLookedUp)
		{
			_jsObserverLookedUp = true;
			try
			{
				_jsObserver = ServiceProvider.GetService(typeof(IMokaJsInteropObserver)) as IMokaJsInteropObserver;
			}
			catch (ObjectDisposedException)
			{
				// A call that finishes while the scope is torn down (an input disposing itself as its
				// circuit ends) has no one left to report to.
			}
		}

		_jsObserver?.OnJsInteropCompleted(identifier, Stopwatch.GetElapsedTime(startTimestamp));
	}
}
