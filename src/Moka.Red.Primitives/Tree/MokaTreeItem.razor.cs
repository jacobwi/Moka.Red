using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Moka.Red.Core.Icons;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Primitives.Tree;

/// <summary>
///     Individual item within a <see cref="MokaTree" />.
///     Can contain nested MokaTreeItem children for hierarchy.
/// </summary>
public partial class MokaTreeItem
{
	private bool _expanded;
	private bool _hasChildren;
	private bool? _lastExpanded;
	private bool? _lastSelected;
	private bool _selected;

	/// <summary>Nested tree items.</summary>
	[Parameter]
	public RenderFragment? ChildContent { get; set; }

	/// <summary>Display text for the item.</summary>
	[Parameter]
	public string? Text { get; set; }

	/// <summary>Icon displayed before the text.</summary>
	[Parameter]
	public MokaIconDefinition? Icon { get; set; }

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
		.Build();

	private string ToggleCssClass => new CssBuilder("moka-tree-item__toggle")
		.AddClass("moka-tree-item__toggle--expanded", _expanded)
		.Build();

	/// <inheritdoc />
	protected override void OnParametersSet()
	{
		base.OnParametersSet();
		_hasChildren = ChildContent is not null;

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
