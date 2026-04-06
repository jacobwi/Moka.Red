using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Base;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Layout.Accordion;

/// <summary>
///     A container for collapsible accordion items. Can enforce single-expand or allow multiple.
/// </summary>
public partial class MokaAccordion : MokaVisualComponentBase
{
	private readonly List<MokaAccordionItem> _items = [];

	/// <summary>The accordion items to render.</summary>
	[Parameter]
	public RenderFragment? ChildContent { get; set; }

	/// <summary>When true, multiple items can be expanded simultaneously. Default false.</summary>
	[Parameter]
	public bool Multiple { get; set; }

	/// <summary>When true (default), renders outer border around the accordion.</summary>
	[Parameter]
	public bool Bordered { get; set; } = true;

	/// <summary>When true, removes outer border and border-radius for flush layout.</summary>
	[Parameter]
	public bool Flush { get; set; }

	/// <inheritdoc />
	protected override string RootClass => "moka-accordion";

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass("moka-accordion--bordered", Bordered && !Flush)
		.AddClass("moka-accordion--flush", Flush)
		.AddClass(Class)
		.Build();

	/// <inheritdoc />
	protected override string? CssStyle => new StyleBuilder()
		.AddStyle("margin", ResolvedMargin)
		.AddStyle("padding", ResolvedPadding)
		.AddStyle(Style)
		.Build();

	/// <summary>Registers an accordion item with this parent.</summary>
	internal void AddItem(MokaAccordionItem item)
	{
		if (!_items.Contains(item))
		{
			_items.Add(item);
		}
	}

	/// <summary>Unregisters an accordion item from this parent.</summary>
	internal void RemoveItem(MokaAccordionItem item) => _items.Remove(item);

	/// <summary>Notifies the parent that an item is expanding. Collapses others in single mode.</summary>
	internal void NotifyItemExpanding(MokaAccordionItem expandingItem)
	{
		if (Multiple)
		{
			return;
		}

		foreach (MokaAccordionItem item in _items)
		{
			if (item != expandingItem && item.IsExpanded)
			{
				item.Collapse();
			}
		}
	}
}
