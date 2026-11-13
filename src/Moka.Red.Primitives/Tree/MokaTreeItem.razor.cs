using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Moka.Red.Core.Enums;
using Moka.Red.Core.Icons;
using Moka.Red.Core.Theming;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Primitives.Tree;

/// <summary>
///     Individual item within a <see cref="MokaTree" />.
///     Can contain nested MokaTreeItem children for hierarchy.
/// </summary>
public partial class MokaTreeItem
{
	private bool _expanded;
	private int _dragDepth;
	private bool _hasChildren;
	private bool? _lastExpanded;
	private bool? _lastSelected;
	private bool _selected;

	/// <summary>Nested tree items.</summary>
	[Parameter]
	public RenderFragment? ChildContent { get; set; }

	/// <summary>
	///     Whether the item has children, which decides whether it draws the expand arrow. <c>true</c> draws it
	///     before the children are loaded, so <see cref="ExpandedChanged" /> can load them; <c>false</c> draws no
	///     arrow and no children. <c>null</c>, the default, draws the arrow when there is child content or
	///     <see cref="Loading" /> is set.
	/// </summary>
	[Parameter]
	public bool? HasChildren { get; set; }

	/// <summary>
	///     Whether the item's children are loading, such as from <see cref="ExpandedChanged" />. A spinner takes the
	///     expand arrow's place and, while the item is expanded, its group shows a row saying <see cref="LoadingText" />
	///     after any children it has, with <c>aria-busy</c> on the group. The row isn't a tree item, so the arrow keys
	///     pass over it, and the arrow still expands and collapses.
	/// </summary>
	[Parameter]
	public bool Loading { get; set; }

	/// <summary>The row an expanded item shows while its children load. Defaults to "Loading".</summary>
	[Parameter]
	public string LoadingText { get; set; } = "Loading";

	/// <summary>Display text for the item.</summary>
	[Parameter]
	public string? Text { get; set; }

	/// <summary>Icon displayed before the text.</summary>
	[Parameter]
	public MokaIconDefinition? Icon { get; set; }

	/// <summary>
	///     Anything drawn in the icon's place, such as an emoji, a small picture or a <c>MokaIdenticon</c>, in the same
	///     box and spacing as <see cref="Icon" />, which it wins over, so a tree of mixed items stays aligned. Pictures
	///     and SVG fill the box; text is set at the box's size. Hidden from screen readers like the icon, since
	///     <see cref="Text" /> names the item.
	/// </summary>
	[Parameter]
	public RenderFragment? IconContent { get; set; }

	/// <summary>
	///     The icon's color: a hex value, a color keyword or a color function such as <c>var()</c>. A value that is not
	///     a CSS color is ignored.
	/// </summary>
	[Parameter]
	public string? IconColor { get; set; }

	/// <summary>
	///     A color someone gave this item, such as a library or a folder they colored. <see cref="LabelMark" /> says how it
	///     shows: by default the icon, or whatever <see cref="IconContent" /> draws, takes the label's color, and an item
	///     with no icon gets a dot before its name. <see cref="IconColor" /> wins over the label on the icon. The items
	///     under it keep their own. The shades follow the theme, light or dark, and <c>MokaTheme.WithLabelTone</c>.
	/// </summary>
	[Parameter]
	public MokaLabelColor? Label { get; set; }

	/// <summary>How <see cref="Label" /> shows. Default <see cref="MokaLabelMark.Icon" />.</summary>
	[Parameter]
	public MokaLabelMark LabelMark { get; set; } = MokaLabelMark.Icon;

	/// <summary>
	///     Content at the end of the row, such as tags. A click on a control in it doesn't select the row; a click on
	///     anything else in it does.
	/// </summary>
	[Parameter]
	public RenderFragment? EndContent { get; set; }

	/// <summary>Whether the row can be dragged. A drag has no keyboard path, so offer another way to move an item.</summary>
	[Parameter]
	public bool Draggable { get; set; }

	/// <summary>Raised when a drag of this row starts.</summary>
	[Parameter]
	public EventCallback<DragEventArgs> OnDragStart { get; set; }

	/// <summary>Raised when a drag of this row ends, dropped or not.</summary>
	[Parameter]
	public EventCallback<DragEventArgs> OnDragEnd { get; set; }

	/// <summary>Raised when something is dropped on this row. While set, the row accepts drops and marks itself under a drag.</summary>
	[Parameter]
	public EventCallback<DragEventArgs> OnDrop { get; set; }

	/// <summary>Whether this item is expanded. Two-way bindable.</summary>
	[Parameter]
	public bool Expanded { get; set; }

	/// <summary>Callback when Expanded changes.</summary>
	[Parameter]
	public EventCallback<bool> ExpandedChanged { get; set; }

	/// <summary>Whether this item is selected. Two-way bindable.</summary>
	[Parameter]
	public bool Selected { get; set; }

	/// <summary>Callback when Selected changes.</summary>
	[Parameter]
	public EventCallback<bool> SelectedChanged { get; set; }

	/// <summary>
	///     Whether this item is disabled. The items under it are disabled too: it cannot be selected,
	///     expanded or collapsed, and neither can they.
	/// </summary>
	[Parameter]
	public bool Disabled { get; set; }

	/// <summary>
	///     Right-click (context menu) callback for this node. When attached, the browser's
	///     default context menu is suppressed. Pair with a context-menu service.
	/// </summary>
	[Parameter]
	public EventCallback<MouseEventArgs> OnContextMenu { get; set; }

	/// <summary>Whether the tree supports selection (cascaded from MokaTree).</summary>
	[CascadingParameter(Name = "TreeSelectable")]
	private bool TreeSelectable { get; set; }

	/// <summary>Current depth level (cascaded from parent).</summary>
	[CascadingParameter(Name = "TreeDepth")]
	private int Depth { get; set; }

	// Set when an item above this one is disabled. The keyboard reaches items a disabled parent
	// hides from the mouse, so they have to know.
	[CascadingParameter(Name = "TreeAncestorDisabled")]
	private bool AncestorDisabled { get; set; }

	private bool IsDisabled => Disabled || AncestorDisabled;

	/// <inheritdoc />
	protected override string RootClass => "moka-tree-item";

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass("moka-tree-item--disabled", IsDisabled)
		.AddClass("moka-tree-item--expanded", _expanded)
		.AddClass(Class)
		.Build();

	/// <summary>Tree items have expand/collapse and selection state.</summary>
	protected override bool ShouldRender() => true;

	private string RowCssClass => new CssBuilder("moka-tree-item__row")
		.AddClass("moka-tree-item__row--selected", _selected)
		.AddClass("moka-tree-item__row--drop-target", _dragDepth > 0)
		.AddClass("moka-tree-item__row--label-icon", LabelColorsIcon)
		.AddClass("moka-tree-item__row--label-tinted", LabelColorsIcon && LabelMark == MokaLabelMark.TintedIcon)
		.Build();

	private bool HasIcon => IconContent is not null || Icon is not null;

	private bool LabelColorsIcon => Label is not null && LabelMark != MokaLabelMark.Dot && HasIcon;

	private bool ShowsLabelDot => Label is not null && (LabelMark == MokaLabelMark.Dot || !HasIcon);

	private string? IconColorValue => CssValues.IsColor(IconColor)
		? IconColor.Trim()
		: LabelColorsIcon ? MokaLabelColors.Solid(Label!.Value) : null;

	private string? IconStyle => new StyleBuilder()
		.AddStyle("color", IconColorValue)
		.Build();

	private string IconSlotStyle => new StyleBuilder()
		.AddStyle("--moka-icon-slot", MokaEnumHelpers.ToPixels(MokaSize.Xs))
		.AddStyle("color", IconColorValue)
		.Build()!;

	private string? LabelDotStyle => new StyleBuilder()
		.AddStyle("background-color", Label is { } label ? MokaLabelColors.Solid(label) : null)
		.Build();

	private bool CanDrag => Draggable && !IsDisabled;

	private bool AcceptsDrop => OnDrop.HasDelegate && !IsDisabled;

	private EventCallback<DragEventArgs> DragStartCallback => CanDrag && OnDragStart.HasDelegate ? OnDragStart : default;

	private EventCallback<DragEventArgs> DragEndCallback => CanDrag && OnDragEnd.HasDelegate ? OnDragEnd : default;

	// Moving between the row's own parts fires an enter and a leave, so one drag over the row is counted, not flagged.
	private EventCallback DragEnterCallback => AcceptsDrop
		? EventCallback.Factory.Create(this, () => _dragDepth++)
		: default;

	private EventCallback DragLeaveCallback => AcceptsDrop
		? EventCallback.Factory.Create(this, () => _dragDepth = Math.Max(0, _dragDepth - 1))
		: default;

	private EventCallback<DragEventArgs> DropCallback => AcceptsDrop
		? EventCallback.Factory.Create<DragEventArgs>(this, HandleDropAsync)
		: default;

	private string ToggleCssClass => new CssBuilder("moka-tree-item__toggle")
		.AddClass("moka-tree-item__toggle--expanded", _expanded)
		.Build();

	private string ToggleLabel => _expanded ? "Collapse" : "Expand";

	// Lined up with the rows of the items it stands in for, one level down.
	private string LoadingRowStyle => new StyleBuilder()
		.AddStyle("padding-left", ((Depth + 1) * 20).ToString(CultureInfo.InvariantCulture) + "px")
		.Build()!;

	/// <inheritdoc />
	protected override void OnParametersSet()
	{
		base.OnParametersSet();
		_hasChildren = HasChildren ?? (ChildContent is not null || Loading);

		// The parent passes every parameter again whenever it renders. Taking Expanded and Selected
		// only when they differ from what it passed last keeps an unbound item from snapping back.
		if (_lastExpanded != Expanded)
		{
			_lastExpanded = Expanded;
			_expanded = Expanded;
		}

		if (_lastSelected != Selected)
		{
			_lastSelected = Selected;
			_selected = Selected;
		}
	}

	private async Task HandleDropAsync(DragEventArgs args)
	{
		_dragDepth = 0;
		await OnDrop.InvokeAsync(args);
	}

	private async Task ToggleExpand()
	{
		if (IsDisabled)
		{
			return;
		}

		_expanded = !_expanded;
		await ExpandedChanged.InvokeAsync(_expanded);
	}

	private async Task HandleClick()
	{
		if (IsDisabled)
		{
			return;
		}

		if (TreeSelectable)
		{
			_selected = !_selected;
			await SelectedChanged.InvokeAsync(_selected);
		}
	}

	private async Task HandleContextMenu(MouseEventArgs args)
	{
		if (!IsDisabled && OnContextMenu.HasDelegate)
		{
			await OnContextMenu.InvokeAsync(args);
		}
	}
}
