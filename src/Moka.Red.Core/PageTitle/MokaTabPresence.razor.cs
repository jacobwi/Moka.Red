using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Moka.Red.Core.Base;

namespace Moka.Red.Core.PageTitle;

/// <summary>
///     Tells the app when its tab has been out of sight for a while, so refreshes can rest, and when it's back. A look at
///     another tab says nothing. Renders no markup.
/// </summary>
public sealed partial class MokaTabPresence : ComponentBase
{
	private const string ModulePath = "./_content/Moka.Red.Core/PageTitle/MokaTabPresence.razor.js";

	private bool _disposed;
	private DotNetObjectReference<MokaTabPresence>? _dotNetRef;
	private IJSObjectReference? _module;
	private IJSObjectReference? _watch;
	private TimeSpan? _watchedAfter;

	/// <summary>Raised once the tab has been out of sight for <see cref="HiddenAfter" />.</summary>
	[Parameter]
	public EventCallback Hidden { get; set; }

	/// <summary>Raised when the tab comes back, only if <see cref="Hidden" /> was raised.</summary>
	[Parameter]
	public EventCallback Visible { get; set; }

	/// <summary>How long the tab stays out of sight before <see cref="Hidden" />. Defaults to a minute.</summary>
	[Parameter]
	public TimeSpan HiddenAfter { get; set; } = TimeSpan.FromMinutes(1);

	/// <inheritdoc />
	public async ValueTask DisposeAsync()
	{
		if (_disposed)
		{
			return;
		}

		_disposed = true;
		await StopWatchingAsync();
		await _module.SafeDisposeAsync();
		_module = null;
		_dotNetRef?.Dispose();
	}

	/// <summary>Called from JS once the tab has been out of sight long enough.</summary>
	/// <returns>A task that completes when <see cref="Hidden" /> has run.</returns>
	[JSInvokable]
	public Task OnTabHidden() => RaiseAsync(Hidden);

	/// <summary>Called from JS when a tab that was reported hidden comes back.</summary>
	/// <returns>A task that completes when <see cref="Visible" /> has run.</returns>
	[JSInvokable]
	public Task OnTabVisible() => RaiseAsync(Visible);

	/// <inheritdoc />
	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (_watchedAfter == HiddenAfter)
		{
			return;
		}

		_watchedAfter = HiddenAfter;
		await StopWatchingAsync();
		await WatchAsync();
	}

	private async Task WatchAsync()
	{
		try
		{
			_module ??= await JsRuntime.InvokeAsync<IJSObjectReference>("import", ModulePath);
			_dotNetRef ??= DotNetObjectReference.Create(this);
			_watch = await _module.InvokeAsync<IJSObjectReference>(
				"watch", _dotNetRef, (int)HiddenAfter.TotalMilliseconds);
		}
		catch (Exception ex) when (MokaJsInterop.IsCircuitGone(ex))
		{
		}
	}

	private async Task StopWatchingAsync()
	{
		await _watch.SafeInvokeVoidAsync("dispose");
		await _watch.SafeDisposeAsync();
		_watch = null;
	}

	// JS swallows what a call from it throws, so a failing handler is handed to the error boundary from here.
	private async Task RaiseAsync(EventCallback callback)
	{
		try
		{
			await callback.InvokeAsync();
		}
		catch (Exception ex) when (!_disposed)
		{
			await DispatchExceptionAsync(ex);
		}
	}
}
