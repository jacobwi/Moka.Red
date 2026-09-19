using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Primitives.Utility;

/// <summary>
///     Fixed-position button that appears once the window, or the element named by
///     <see cref="ScrollContainerSelector" />, has scrolled down, and scrolls it back to the top on click.
/// </summary>
public partial class MokaScrollToTop
{
	private const string ModulePath = "./_content/Moka.Red.Primitives/Utility/MokaScrollToTop.razor.js";

	private bool _disposed;
	private DotNetObjectReference<MokaScrollToTop>? _dotNetRef;
	private int _scrollHandle;
	private bool _visible;

	// What the JS listener was set up with. A change to either parameter moves the listener.
	private (int ShowAfter, string? Selector)? _watching;
	private Task _watchSync = Task.CompletedTask;

	/// <summary>Pixels scrolled before showing the button. Default 200.</summary>
	[Parameter]
	public int ShowAfter { get; set; } = 200;

	/// <summary>Use smooth scroll animation. Default true.</summary>
	[Parameter]
	public bool Smooth { get; set; } = true;

	/// <summary>
	///     CSS selector of the element that scrolls, such as <c>".app-main"</c>. The button then
	///     watches and scrolls that element instead of the window. Set it when the page content
	///     scrolls inside a panel, as in dock layouts and app shells, where the window never scrolls.
	///     Default null, the window.
	/// </summary>
	[Parameter]
	public string? ScrollContainerSelector { get; set; }

	/// <summary>Tooltip and accessible name of the button. Default "Scroll to top".</summary>
	[Parameter]
	public string Label { get; set; } = "Scroll to top";

	/// <inheritdoc />
	protected override string RootClass => "moka-scroll-top";

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass(Class)
		.Build();

	/// <inheritdoc />
	protected override Task OnAfterRenderAsync(bool firstRender)
	{
		base.OnAfterRender(firstRender);

		// One sync at a time: the first report from JS renders again before init has returned, and
		// that render must not start a second listener.
		_watchSync = WatchAsync(_watchSync);
		return _watchSync;
	}

	/// <summary>Called from JS when the button should appear or disappear.</summary>
	[JSInvokable]
	public void OnScrollChanged(bool visible)
	{
		if (_visible != visible)
		{
			_visible = visible;
			InvokeAsync(ForceRender);
		}
	}

	private async Task WatchAsync(Task previous)
	{
		await previous;

		(int ShowAfter, string? Selector) wanted = (ShowAfter, ScrollContainerSelector);
		if (_disposed || _watching == wanted)
		{
			return;
		}

		await StopWatchingAsync();
		_dotNetRef ??= DotNetObjectReference.Create(this);
		_scrollHandle = await SafeModuleInvokeAsync<int>(ModulePath, "init", _dotNetRef, ShowAfter,
			ScrollContainerSelector);
		_watching = wanted;
	}

	private async Task StopWatchingAsync()
	{
		if (_scrollHandle != 0)
		{
			await SafeModuleInvokeVoidAsync(ModulePath, "dispose", _scrollHandle);
			_scrollHandle = 0;
		}

		_watching = null;
	}

	private async Task ScrollToTop()
	{
		// The disabled attribute stops clicks in the browser; this also covers events raised another way.
		if (Disabled)
		{
			return;
		}

		await SafeModuleInvokeVoidAsync(ModulePath, "scrollToTop", _scrollHandle, Smooth);
	}

	/// <inheritdoc />
	protected override async ValueTask DisposeAsyncCore()
	{
		// Let an attach in flight finish, then drop the scroll listener before the base disposes the
		// module. Otherwise it stays attached for the life of the page, calling a dead component.
		_disposed = true;
		try
		{
			await _watchSync;
		}
		catch (JSException)
		{
			// The attach failed in the browser, so there is no listener to remove.
		}

		await StopWatchingAsync();

		_dotNetRef?.Dispose();
		await base.DisposeAsyncCore();
	}
}
