using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Moka.Red.Core.Base;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Data.InfiniteScroll;

/// <summary>
///     Asks for more items when the end of its content comes near the bottom of the view, whichever element
///     does the scrolling. An observer in the browser watches for that, so scrolling sends nothing to .NET.
/// </summary>
public partial class MokaInfiniteScroll : MokaComponentBase
{
	private const string ModulePath = "./_content/Moka.Red.Data/InfiniteScroll/MokaInfiniteScroll.razor.js";

	private readonly string _watchId = $"moka-infinite-scroll-{Guid.NewGuid():N}";
	private DotNetObjectReference<MokaInfiniteScroll>? _dotNetRef;
	private bool _isLoading;
	private ElementReference _reachRef;
	private ElementReference _rootRef;
	private bool _watching;

	/// <summary>The scrollable content. Required.</summary>
	[Parameter]
	[EditorRequired]
	public RenderFragment ChildContent { get; set; } = default!;

	/// <summary>
	///     Callback fired when the end of the content comes within <see cref="Threshold" /> of view. It fires
	///     again after each load for as long as the end stays in view, so a short first page fills the space.
	///     A load that adds nothing waits for the next scroll, so a failing server isn't asked over and over.
	/// </summary>
	[Parameter]
	public EventCallback OnLoadMore { get; set; }

	/// <summary>Whether data is currently being loaded. Shows a loading indicator when true.</summary>
	[Parameter]
	public bool Loading { get; set; }

	/// <summary>Whether there are more items to load. Hides the sentinel when false. Defaults to true.</summary>
	[Parameter]
	public bool HasMore { get; set; } = true;

	/// <summary>
	///     How far from the end of the content to start loading, as a CSS length.
	///     Defaults to "200px".
	/// </summary>
	[Parameter]
	public string Threshold { get; set; } = "200px";

	/// <summary>Custom loading indicator template. When null, a default spinner is shown.</summary>
	[Parameter]
	public RenderFragment? LoadingTemplate { get; set; }

	/// <inheritdoc />
	protected override string RootClass => "moka-infinite-scroll";

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass("moka-thin-scrollbar")
		.AddClass("moka-infinite-scroll--loading", Loading)
		.AddClass(Class)
		.Build();

	/// <inheritdoc />
	protected override string? CssStyle => new StyleBuilder()
		.AddStyle(Style)
		.Build();

	// An observer's rootMargin only widens the viewport, not a scrolling element in between, so the element it
	// watches is what reaches ahead.
	private string? ReachStyle => new StyleBuilder()
		.AddStyle("height", Threshold)
		.Build();

	private bool WantsToKnow => HasMore && OnLoadMore.HasDelegate && !Loading && !_isLoading;

	/// <summary>Override to allow internal state changes to trigger re-render.</summary>
	protected override bool ShouldRender() => true;

	/// <inheritdoc />
	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (WantsToKnow == _watching)
		{
			return;
		}

		_watching = !_watching;
		if (_watching)
		{
			_dotNetRef ??= DotNetObjectReference.Create(this);
			await SafeModuleInvokeVoidAsync(ModulePath, "watchSentinel", _watchId, _reachRef, _rootRef, _dotNetRef);
		}
		else
		{
			await SafeModuleInvokeVoidAsync(ModulePath, "unwatchSentinel", _watchId);
		}
	}

	/// <summary>Called from JS, once, when the sentinel comes into view. The next render watches again.</summary>
	[JSInvokable]
	public void OnSentinelVisible()
	{
		_watching = false;
		RunDetached(LoadMoreAsync);
	}

	/// <inheritdoc />
	protected override async ValueTask DisposeAsyncCore()
	{
		if (_dotNetRef is not null)
		{
			await SafeModuleInvokeVoidAsync(ModulePath, "forgetSentinel", _watchId);
			_dotNetRef.Dispose();
		}

		await base.DisposeAsyncCore();
	}

	private async Task LoadMoreAsync()
	{
		if (!WantsToKnow)
		{
			StateHasChanged();
			return;
		}

		// The guard has to be released in a finally: an exception thrown by OnLoadMore used to
		// wedge the component for good.
		_isLoading = true;
		try
		{
			await OnLoadMore.InvokeAsync();
		}
		finally
		{
			_isLoading = false;
			StateHasChanged();
		}
	}
}
