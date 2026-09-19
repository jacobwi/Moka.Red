using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Moka.Red.Core.Base;
using Moka.Red.Core.Enums;
using Moka.Red.Core.Interactions;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Primitives.Kanban;

/// <summary>
///     A drag-and-drop Kanban board with configurable columns and card templates.
///     Uses HTML5 native drag-and-drop - no JS interop required.
/// </summary>
/// <typeparam name="TItem">The type of items in the board.</typeparam>
public partial class MokaKanbanBoard<TItem> : MokaComponentBase
{
	private TItem? _draggedItem;
	private int _dragSourceColumnIndex = -1;

	/// <summary>The columns displayed on the board. Each column contains a title and list of items.</summary>
	[Parameter]
	[EditorRequired]
	public IReadOnlyList<MokaKanbanColumn<TItem>> Columns { get; set; } = [];

	/// <summary>Template for rendering each card in the board. When omitted, items render as their <c>ToString()</c> value.</summary>
	[Parameter]
	public RenderFragment<TItem>? ItemTemplate { get; set; }

	/// <summary>Callback invoked when an item is dragged from one column to another.</summary>
	[Parameter]
	public EventCallback<MokaKanbanItemMovedArgs<TItem>> OnItemMoved { get; set; }

	/// <summary>
	///     Fires on right-click of a card. When attached, the browser's default context menu is
	///     suppressed. Pair with a context-menu service:
	///     <c>OnCardContextMenu="a =&gt; Menu.Show(a.MouseEvent, ItemsFor(a.Item))"</c>.
	/// </summary>
	[Parameter]
	public EventCallback<MokaItemContextMenuArgs<TItem>> OnCardContextMenu { get; set; }

	/// <summary>CSS width for each column. Defaults to auto (flexible).</summary>
	[Parameter]
	public string? ColumnWidth { get; set; }

	/// <summary>Maximum height for the board container. Defaults to "600px".</summary>
	[Parameter]
	public string MaxHeight { get; set; } = "600px";

	/// <inheritdoc />
	protected override string RootClass => "moka-kanban";

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass(Class)
		.Build();

	/// <inheritdoc />
	protected override string? CssStyle => new StyleBuilder()
		.AddStyle("max-height", MaxHeight)
		.AddStyle(Style)
		.Build();

	private string? ColumnStyle => new StyleBuilder()
		.AddStyle("width", ColumnWidth)
		.AddStyle("min-width", ColumnWidth)
		.Build();

	/// <summary>Kanban board has internal drag state that changes independently of parameters.</summary>
	protected override bool ShouldRender() => true;

	private static string GetColumnHeaderClass(MokaKanbanColumn<TItem> column) =>
		new CssBuilder("moka-kanban__column-header")
			.AddClass($"moka-kanban__column-header--{MokaEnumHelpers.ToCssClass(column.Color ?? MokaColor.Surface)}")
			.Build();

	private void HandleDragStart(TItem item, int columnIndex)
	{
		_draggedItem = item;
		_dragSourceColumnIndex = columnIndex;
	}

	private async Task HandleDrop(int targetColumnIndex)
	{
		if (_draggedItem is null || _dragSourceColumnIndex < 0)
		{
			return;
		}

		if (_dragSourceColumnIndex == targetColumnIndex)
		{
			return;
		}

		MokaKanbanColumn<TItem> sourceColumn = Columns[_dragSourceColumnIndex];
		MokaKanbanColumn<TItem> targetColumn = Columns[targetColumnIndex];

		sourceColumn.Items.Remove(_draggedItem);
		targetColumn.Items.Add(_draggedItem);

		var args = new MokaKanbanItemMovedArgs<TItem>(_draggedItem, _dragSourceColumnIndex, targetColumnIndex);

		_draggedItem = default;
		_dragSourceColumnIndex = -1;

		await OnItemMoved.InvokeAsync(args);
	}

	private void HandleDragEnd()
	{
		_draggedItem = default;
		_dragSourceColumnIndex = -1;
	}

	private async Task HandleCardContextMenu(TItem item, MouseEventArgs e)
	{
		if (OnCardContextMenu.HasDelegate)
		{
			await OnCardContextMenu.InvokeAsync(new MokaItemContextMenuArgs<TItem>(item, e));
		}
	}
}
