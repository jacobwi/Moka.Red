using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using Moka.Red.Core.Base;
using Moka.Red.Core.Enums;
using Moka.Red.Core.Interactions;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Primitives.Kanban;

/// <summary>
///     A Kanban board: columns of cards that people move with a mouse, a pen, a finger or the keyboard. While a card
///     moves, a copy follows the pointer, its place dims, the column it would land in lights up and a bar marks the
///     spot, all in the browser; only the drop reaches .NET, which moves the item in its lists and raises
///     <see cref="OnItemMoved" />. A card can go to another column or to another place in its own.
/// </summary>
/// <typeparam name="TItem">The type of items in the board.</typeparam>
public partial class MokaKanbanBoard<TItem> : MokaVisualComponentBase
{
	private const string JsModulePath = "./_content/Moka.Red.Primitives/Kanban/MokaKanbanBoard.razor.js";

	private readonly string _id = $"moka-kanban-{Guid.NewGuid():N}";
	private ElementReference _root;
	private DotNetObjectReference<MokaKanbanBoard<TItem>>? _dotNetRef;
	private MokaKanbanText? _boundWords;
	private (int Column, int Index)? _focus;

	/// <summary>The columns displayed on the board. Each column contains a title and list of items.</summary>
	[Parameter]
	[EditorRequired]
	public IReadOnlyList<MokaKanbanColumn<TItem>> Columns { get; set; } = [];

	/// <summary>Template for rendering each card in the board. When omitted, items render as their <c>ToString()</c> value.</summary>
	[Parameter]
	public RenderFragment<TItem>? ItemTemplate { get; set; }

	/// <summary>
	///     Raised after an item moved, to another column or to another place in its own. The board has already moved it in
	///     the columns' <see cref="MokaKanbanColumn{TItem}.Items" /> lists.
	/// </summary>
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

	/// <summary>
	///     What identifies an item, so a card keeps its element when items move. Unset, the item itself does. An item that
	///     appears twice in one column goes without a key there.
	/// </summary>
	[Parameter]
	public Func<TItem, object?>? ItemKey { get; set; }

	/// <summary>
	///     A card's name for screen readers, such as a task's title, when the card shows more than its name. Unset, the
	///     card's text names it.
	/// </summary>
	[Parameter]
	public Func<TItem, string?>? ItemLabel { get; set; }

	/// <summary>The words a screen reader hears while a card moves. English unless set.</summary>
	[Parameter]
	public MokaKanbanText? Text { get; set; }

	/// <summary>Stops cards from moving: no dragging and no keyboard moves.</summary>
	[Parameter]
	public override bool Disabled { get; set; }

	/// <inheritdoc />
	protected override string RootClass => "moka-kanban";

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass("moka-kanban--disabled", Disabled)
		.AddClass(Class)
		.Build();

	/// <inheritdoc />
	protected override string? CssStyle => SpacingStyle()
		.AddStyle("max-height", MaxHeight)
		.AddStyle(Style)
		.Build();

	private MokaKanbanText Words => Text ?? MokaKanbanText.Default;

	private string HintId => $"{_id}-hint";

	private string? CardHintId => Disabled ? null : HintId;

	private string? CardTabIndex => Disabled ? null : "0";

	private string? ColumnStyle => new StyleBuilder()
		.AddStyle("width", ColumnWidth)
		.AddStyle("min-width", ColumnWidth)
		.Build();

	/// <summary>Kanban board has internal drag state that changes independently of parameters.</summary>
	protected override bool ShouldRender() => true;

	/// <inheritdoc />
	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		MokaKanbanText? wanted = Disabled ? null : Words;
		if (!ReferenceEquals(wanted, _boundWords))
		{
			if (_boundWords is not null)
			{
				await SafeModuleInvokeVoidAsync(JsModulePath, "unbindBoard", _root);
			}

			_boundWords = wanted;
			if (wanted is not null)
			{
				_dotNetRef ??= DotNetObjectReference.Create(this);
				await SafeModuleInvokeVoidAsync(JsModulePath, "bindBoard", _root, _dotNetRef, Options(wanted));
			}
		}

		if (_focus is { } focus)
		{
			_focus = null;
			await SafeModuleInvokeVoidAsync(JsModulePath, "focusCard", _root, focus.Column, focus.Index);
		}
	}

	/// <summary>
	///     Called from the browser when a card is dropped: the item at <paramref name="fromIndex" /> of column
	///     <paramref name="fromColumn" /> goes to <paramref name="toIndex" /> of column <paramref name="toColumn" />,
	///     counted among that column's other cards.
	/// </summary>
	/// <param name="fromColumn">The column the card came from.</param>
	/// <param name="fromIndex">Where it was in that column.</param>
	/// <param name="toColumn">The column it was dropped in.</param>
	/// <param name="toIndex">Where it lands in that column.</param>
	/// <param name="byKeyboard">Whether it was moved from the keyboard, so focus follows it.</param>
	/// <returns>A completed task; the move is applied before it returns.</returns>
	[JSInvokable]
	public Task Move(int fromColumn, int fromIndex, int toColumn, int toIndex, bool byKeyboard)
	{
		if (Disabled || !InRange(fromColumn) || !InRange(toColumn))
		{
			return Task.CompletedTask;
		}

		IList<TItem> source = Columns[fromColumn].Items;
		IList<TItem> target = Columns[toColumn].Items;
		if (fromIndex < 0 || fromIndex >= source.Count)
		{
			return Task.CompletedTask;
		}

		TItem item = source[fromIndex];
		source.RemoveAt(fromIndex);
		int index = Math.Clamp(toIndex, 0, target.Count);
		target.Insert(index, item);
		if (fromColumn == toColumn && index == fromIndex)
		{
			return Task.CompletedTask;
		}

		if (byKeyboard)
		{
			_focus = (toColumn, index);
		}

		StateHasChanged();
		MokaKanbanItemMovedArgs<TItem> moved = new(item, fromColumn, toColumn) { FromIndex = fromIndex, ToIndex = index };
		RunDetached(() => OnItemMoved.InvokeAsync(moved));
		return Task.CompletedTask;
	}

	/// <inheritdoc />
	protected override async ValueTask DisposeAsyncCore()
	{
		if (_boundWords is not null)
		{
			await SafeModuleInvokeVoidAsync(JsModulePath, "unbindBoard", _root);
		}

		_dotNetRef?.Dispose();
		_dotNetRef = null;

		await base.DisposeAsyncCore();
	}

	private static Dictionary<string, object?> Options(MokaKanbanText words) => new()
	{
		["pickedUp"] = words.PickedUp,
		["moving"] = words.Moving,
		["dropped"] = words.Dropped,
		["cancelled"] = words.Cancelled
	};

	private bool InRange(int column) => column >= 0 && column < Columns.Count;

	private string TitleId(int column) => $"{_id}-c{column}";

	private object? Key(TItem item) => ItemKey is null ? item : ItemKey(item);

	private HashSet<object> Repeated(IList<TItem> items)
	{
		HashSet<object> seen = [];
		HashSet<object> repeated = [];
		foreach (TItem item in items)
		{
			if (Key(item) is { } key && !seen.Add(key))
			{
				repeated.Add(key);
			}
		}

		return repeated;
	}

	private object? KeyOf(TItem item, HashSet<object> repeated) =>
		Key(item) is { } key && !repeated.Contains(key) ? key : null;

	private static string CountText(MokaKanbanColumn<TItem> column) => column.Limit is { } limit
		? $"{column.Items.Count.ToString(CultureInfo.InvariantCulture)}/{limit.ToString(CultureInfo.InvariantCulture)}"
		: column.Items.Count.ToString(CultureInfo.InvariantCulture);

	private static string CountClass(MokaKanbanColumn<TItem> column) =>
		new CssBuilder("moka-kanban__column-count")
			.AddClass("moka-kanban__column-count--over", column.Limit is { } limit && column.Items.Count > limit)
			.Build();

	private static string GetColumnHeaderClass(MokaKanbanColumn<TItem> column) =>
		new CssBuilder("moka-kanban__column-header")
			.AddClass($"moka-kanban__column-header--{MokaEnumHelpers.ToCssClass(column.Color ?? MokaColor.Surface)}")
			.Build();

	private EventCallback<MouseEventArgs> ContextMenuFor(TItem item) => OnCardContextMenu.HasDelegate
		? EventCallback.Factory.Create<MouseEventArgs>(this, e => OnCardContextMenu.InvokeAsync(new MokaItemContextMenuArgs<TItem>(item, e)))
		: default;
}
