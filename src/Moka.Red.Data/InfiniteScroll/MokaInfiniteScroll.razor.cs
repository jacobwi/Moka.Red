using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Moka.Red.Core.Base;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Data.InfiniteScroll;

/// <summary>
///     A scroll sentinel component that triggers loading more items when the user
///     scrolls near the bottom of the container. Uses scroll position detection
///     to fire a load-more callback without requiring JS interop.
/// </summary>
public partial class MokaInfiniteScroll : MokaComponentBase
{
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

		// Use JS interop to check scroll position
		try
		{
			double[] scrollInfo = await SafeJsInvokeAsync<double[]>(
				"eval",
				$"(function(){{var el=document.getElementById('{Id ?? ""}');if(!el)return[0,0,0];return[el.scrollTop,el.scrollHeight,el.clientHeight];}})()");

			if (scrollInfo is { Length: 3 })
			{
				double scrollTop = scrollInfo[0];
				double scrollHeight = scrollInfo[1];
				double clientHeight = scrollInfo[2];

				if (scrollHeight - scrollTop - clientHeight <= ThresholdPx)
				{
					_isLoading = true;
					await OnLoadMore.InvokeAsync();
					_isLoading = false;
				}
			}
		}
		catch (JSDisconnectedException)
		{
			// JS interop not available during prerendering or after circuit disconnect
		}
		catch (InvalidOperationException)
		{
			// JS interop not available during prerendering
		}
	}
}
