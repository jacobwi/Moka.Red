using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Base;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Data.VirtualList;

/// <summary>
///     Virtualized list for rendering large datasets efficiently. Only renders visible items
///     using Blazor's built-in <c>Virtualize</c> component internally.
/// </summary>
/// <typeparam name="TItem">The type of items in the list.</typeparam>
public partial class MokaVirtualList<TItem> : MokaVisualComponentBase
{
	/// <summary>The full dataset to display. Required.</summary>
	[Parameter]
	[EditorRequired]
	public IReadOnlyList<TItem> Items { get; set; } = [];

	/// <summary>Template for rendering each item. Required.</summary>
	[Parameter]
	[EditorRequired]
	public RenderFragment<TItem> ItemTemplate { get; set; } = default!;

	/// <summary>Fixed height of each item in pixels. Required for virtualization calculations.</summary>
	[Parameter]
	[EditorRequired]
	public float ItemHeight { get; set; }

	/// <summary>Visible height of the list container. Default "400px".</summary>
	[Parameter]
	public string Height { get; set; } = "400px";

	/// <summary>Number of extra items rendered above and below the viewport. Default 3.</summary>
	[Parameter]
	public int OverscanCount { get; set; } = 3;

	/// <summary>Callback when an item is clicked.</summary>
	[Parameter]
	public EventCallback<TItem> OnItemClick { get; set; }

	/// <inheritdoc />
	protected override string RootClass => "moka-virtual-list";

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass("moka-virtual-list--clickable", OnItemClick.HasDelegate)
		.AddClass("moka-virtual-list--disabled", Disabled)
		.AddClass(Class)
		.Build();

	/// <inheritdoc />
	protected override string? CssStyle => new StyleBuilder()
		.AddStyle("height", Height)
		.AddStyle("margin", ResolvedMargin)
		.AddStyle("padding", ResolvedPadding)
		.AddStyle(Style)
		.Build();

	private async Task HandleClick(TItem item)
	{
		if (OnItemClick.HasDelegate)
		{
			await OnItemClick.InvokeAsync(item);
		}
	}
}
