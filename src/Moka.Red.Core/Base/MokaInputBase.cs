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
	protected string ComponentCssClass => new CssBuilder(RootClass)
		.AddClass(CssClass) // InputBase validation classes (valid/invalid/modified)
		.AddClass(Class)
		.Build();

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
			if (_jsModule is not null)
			{
				return _jsModule;
			}

			_jsModule = await JsRuntime.InvokeAsync<IJSObjectReference>("import", modulePath);
			return _jsModule;
		}
		finally
		{
			_jsModuleLock.Release();
		}
	}

	/// <summary>
	///     Override this to dispose component-specific resources.
	/// </summary>
	protected virtual ValueTask DisposeAsyncCore() => ValueTask.CompletedTask;
}
