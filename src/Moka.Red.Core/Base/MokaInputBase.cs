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
	private static readonly bool IsNullable = Nullable.GetUnderlyingType(typeof(TValue)) is not null;

	private readonly IEqualityComparer<TValue?> _comparer;
	private readonly MokaInputValue<TValue> _input;
	private bool _boundToField;
	private bool _disposed;
	private bool _fieldNameUnavailable;
	private string? _incomingText;
	private IMokaJsInteropObserver? _jsObserver;
	private bool _jsObserverLookedUp;
	private bool _parsingFailed;
	private ValidationMessageStore? _parsingMessages;
	private bool _previousParsingFailed;

	/// <summary>Sets up the value the input shows, apart from the <see cref="InputBase{TValue}.Value" /> parameter.</summary>
	protected MokaInputBase() : this(null)
	{
	}

	/// <summary>
	///     Sets up the value the input shows, comparing values with <paramref name="comparer" />, for a value such as a
	///     list where a new instance with the same content is the same value.
	/// </summary>
	/// <param name="comparer">Decides whether two values are the same. Null takes the type's own equality.</param>
	protected MokaInputBase(IEqualityComparer<TValue?>? comparer)
	{
		_comparer = comparer ?? EqualityComparer<TValue?>.Default;
		_input = new MokaInputValue<TValue>(ShowParentValue, ex => _disposed ? Task.CompletedTask : DispatchExceptionAsync(ex),
			comparer: _comparer);
	}

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
	///     The value the input shows. Setting it raises <see cref="InputBase{TValue}.ValueChanged" /> and tells the
	///     <see cref="EditContext" />, but never writes the <see cref="InputBase{TValue}.Value" /> parameter. A parent that
	///     passes <c>Value</c> and listens to <c>ValueChanged</c> owns the value: what was set shows while the parent's
	///     handler runs, and once it has finished the input shows the parent's <c>Value</c> again unless the parent passed
	///     a new one, so a change the parent refuses is undone. Without both, the input keeps what was set, and follows
	///     <c>Value</c> only when the parent passes one different from the last.
	/// </summary>
	/// <remarks>
	///     It hides <see cref="InputBase{TValue}.CurrentValue" />, which writes the component's own <c>Value</c>
	///     parameter: Blazor sends a parameter again only when the parent passes a changed value, so a parent that kept
	///     its value never put the input back.
	/// </remarks>
	protected new TValue? CurrentValue
	{
		get => _input.Shown;
		set
		{
			if (_comparer.Equals(value, _input.Shown))
			{
				return;
			}

			_parsingFailed = false;
			_input.Pick(value, ValueChanged);
			EditContext?.NotifyFieldChanged(FieldIdentifier);
		}
	}

	/// <summary>
	///     <see cref="CurrentValue" /> as text, read through <see cref="InputBase{TValue}.FormatValueAsString" /> and
	///     written through <see cref="InputBase{TValue}.TryParseValueFromString" />. Text that doesn't parse stays as typed
	///     and adds a validation message to the <see cref="EditContext" />, and the value doesn't change.
	/// </summary>
	protected new string? CurrentValueAsString
	{
		// InputBase's own text keeps what a static form post attempted when it failed to parse. That text belongs to
		// the parent's Value, so it applies while the input shows that value.
		get => _parsingFailed
			? _incomingText
			: _comparer.Equals(_input.Shown, Value)
				? base.CurrentValueAsString
				: FormatValueAsString(_input.Shown);
		set
		{
			_incomingText = value;
			_parsingMessages?.Clear();

			if (IsNullable && string.IsNullOrEmpty(value))
			{
				_parsingFailed = false;
				CurrentValue = default;
			}
			else if (TryParseValueFromString(value, out TValue? parsed, out string? message))
			{
				_parsingFailed = false;
				CurrentValue = parsed;
			}
			else
			{
				_parsingFailed = true;
				if (EditContext is not null)
				{
					_parsingMessages ??= new ValidationMessageStore(EditContext);
					_parsingMessages.Add(FieldIdentifier, message);
					EditContext.NotifyFieldChanged(FieldIdentifier);
				}
			}

			if (_parsingFailed || _previousParsingFailed)
			{
				EditContext?.NotifyValidationStateChanged();
				_previousParsingFailed = _parsingFailed;
			}
		}
	}

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

	/// <summary>
	///     Runs <see cref="DisposeAsyncCore" /> once, then the input's own disposal, which stops listening to the
	///     <see cref="EditContext" />. A JavaScript call that fails because the browser side is gone (see
	///     <see cref="MokaJsInterop.IsCircuitGone" />) is ignored.
	/// </summary>
	public async ValueTask DisposeAsync()
	{
		if (_disposed)
		{
			return;
		}

		_disposed = true;
		_input.Stop();

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
	///     static SSR previews) without throwing a missing-parameter exception. Takes the parent's
	///     <see cref="InputBase{TValue}.Value" /> into <see cref="CurrentValue" /> first.
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

		_input.Take(parameters, nameof(Value));
		return base.SetParametersAsync(parameters);
	}

	/// <summary>
	///     Sets <see cref="HasRendered" /> after the first render, and shows the parent's value again when it kept its value
	///     through a change. An override calls the base.
	/// </summary>
	protected override void OnAfterRender(bool firstRender)
	{
		if (firstRender)
		{
			HasRendered = true;
		}

		_input.AfterRender();
	}

	/// <summary>Clears the parsing messages this input added to the <see cref="EditContext" />. An override calls the base.</summary>
	/// <param name="disposing">Whether the input is being disposed rather than finalized.</param>
	protected override void Dispose(bool disposing)
	{
		if (disposing && _parsingMessages is not null)
		{
			_parsingMessages.Clear();
			EditContext.NotifyValidationStateChanged();
		}

		base.Dispose(disposing);
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

	/// <summary>
	///     Creates a holder for another value the input binds, such as the ends of a range, that renders this input when
	///     it goes back to the parent's value and sends a late failure in the parent's handler to the error boundary.
	///     The input calls its <see cref="MokaInputValue{TValue}.AfterRender" /> from <see cref="OnAfterRender" />.
	/// </summary>
	/// <typeparam name="T">The type of the value.</typeparam>
	/// <param name="initial">What the input shows before the parent passes a value.</param>
	/// <param name="comparer">Decides whether two values are the same. Defaults to the type's own equality.</param>
	/// <returns>The holder.</returns>
	protected MokaInputValue<T> CreateInputValue<T>(T? initial = default, IEqualityComparer<T?>? comparer = null) =>
		new(StateHasChanged, ex => _disposed ? Task.CompletedTask : DispatchExceptionAsync(ex), initial, comparer);

	/// <summary>
	///     Called when <see cref="CurrentValue" /> goes back to the parent's <see cref="InputBase{TValue}.Value" /> after a
	///     change the parent didn't take, just before the render that shows it. A component that keeps state worked out
	///     from the value updates it here, as it does in <c>OnParametersSet</c> when the parent passes a new value.
	/// </summary>
	protected virtual void OnCurrentValueRestored()
	{
	}

	private void ShowParentValue()
	{
		OnCurrentValueRestored();
		StateHasChanged();
	}

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
