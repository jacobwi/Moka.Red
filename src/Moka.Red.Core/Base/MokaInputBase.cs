using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
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
	private bool _disposed;
	private IJSObjectReference? _jsModule;
	private SemaphoreSlim? _jsModuleLock;
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

	/// <summary>Theme cascaded from <see cref="MokaThemeProvider" />.</summary>
	[CascadingParameter]
	public MokaTheme? Theme { get; set; }

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

		// Blazor calls only DisposeAsync on a component that has one, so InputBase's Dispose, which
		// unsubscribes from the EditContext, never ran and a form kept every removed input alive.
		((IDisposable)this).Dispose();

		if (_jsModule is not null)
		{
			try
			{
				await _jsModule.DisposeAsync();
			}
			catch (JSDisconnectedException)
			{
				// Circuit already disconnected
			}

			_jsModule = null;
		}

		_jsModuleLock?.Dispose();
		_jsModuleLock = null;

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
		if (!parameters.TryGetValue<Expression<Func<TValue>>>(nameof(ValueExpression), out _))
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
	///     Lazily imports a collocated JS module (.razor.js). Cached and auto-disposed.
	///     Thread-safe: concurrent callers are serialized via SemaphoreSlim.
	/// </summary>
	[SuppressMessage("Reliability", "CA1508:Avoid dead conditional code",
		Justification = "Double-checked locking - _jsModule may be set between outer check and lock acquisition")]
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
			if (_jsModule is not null)
			{
				return _jsModule;
			}

			long start = Stopwatch.GetTimestamp();
			_jsModule = await JsRuntime.InvokeAsync<IJSObjectReference>("import", modulePath);
			ReportJsCall("import", start);
			return _jsModule;
		}
		finally
		{
			_jsModuleLock.Release();
		}
	}

	/// <summary>
	///     Invokes a void function on the module <see cref="GetJsModuleAsync" /> returns, and
	///     swallows the exceptions a lost circuit or prerendering throws. The module cache holds
	///     one module per component, so a component that already imports another one cannot use this.
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
		catch (JSDisconnectedException)
		{
			// Circuit disconnected - nothing to do
		}
		catch (ObjectDisposedException)
		{
			// Circuit or JS runtime torn down mid-call
		}
		catch (OperationCanceledException)
		{
			// Covers TaskCanceledException too - the circuit went away while awaiting
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
