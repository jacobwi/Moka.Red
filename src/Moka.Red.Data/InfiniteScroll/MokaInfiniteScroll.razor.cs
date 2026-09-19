using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Moka.Red.Core.Base;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Data.InfiniteScroll;

/// <summary>
///     A scroll sentinel component that triggers loading more items when the user
///     scrolls near the bottom of the container. Reads scroll position through a small
///     collocated JS module on each scroll event.
/// </summary>
public partial class MokaInfiniteScroll : MokaComponentBase
{
	private const string ModulePath = "./_content/Moka.Red.Data/InfiniteScroll/MokaInfiniteScroll.razor.js";

	private bool _isLoading;
	private ElementReference _scrollRef;

	/// <summary>The scrollable content. Required.</summary>
	[Parameter]
	[EditorRequired]
	public RenderFragment ChildContent { get; set; } = default!;

	/// <summary>Callback fired when the scroll position reaches the sentinel threshold.</summary>
	[Parameter]
	public EventCallback OnLoadMore { get; set; }

	/// <summary>Whether data is currently being loaded. Shows a loading indicator when true.</summary>
	[Parameter]
	public bool Loading { get; set; }

	/// <summary>Whether there are more items to load. Hides the sentinel when false. Defaults to true.</summary>
	[Parameter]
	public bool HasMore { get; set; } = true;

	/// <summary>
	///     Distance from the bottom (in pixels) at which to trigger loading.
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

	private int ThresholdPx
	{
		get
		{
			string numeric = new(Threshold.Where(c => char.IsDigit(c) || c == '.').ToArray());
			return int.TryParse(numeric, out int px) ? px : 200;
		}
	}

	/// <summary>Override to allow internal state changes to trigger re-render.</summary>
	protected override bool ShouldRender() => true;

	private async Task HandleScroll()
	{
		if (Loading || !HasMore || !OnLoadMore.HasDelegate || _isLoading)
		{
			return;
		}

		// The guard has to be released in a finally: an exception thrown by OnLoadMore used to
		// wedge the component for good.
		_isLoading = true;
		try
		{
			IJSObjectReference module = await GetJsModuleAsync(ModulePath);
			double[] metrics = await module.InvokeAsync<double[]>("getScrollMetrics", _scrollRef);

			if (metrics is { Length: 3 } && metrics[1] - metrics[0] - metrics[2] <= ThresholdPx)
			{
				await OnLoadMore.InvokeAsync();
			}
		}
		catch (JSDisconnectedException)
		{
			// Circuit disconnected
		}
		catch (ObjectDisposedException)
		{
			// JS runtime torn down mid-call
		}
		catch (OperationCanceledException)
		{
			// Covers TaskCanceledException too
		}
		catch (InvalidOperationException) when (!HasRendered)
		{
			// JS interop not available during prerendering
		}
		finally
		{
			_isLoading = false;
		}
	}
}
