using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Icons;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Primitives.Tree;

/// <summary>
///     Individual item within a <see cref="MokaTree" />.
///     Can contain nested MokaTreeItem children for hierarchy.
/// </summary>
public partial class MokaTreeItem
{
	private bool _hasChildren;

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

	/// <summary>Whether this item is disabled.</summary>
	[Parameter]
	public bool Disabled { get; set; }

	/// <summary>Whether the tree supports selection (cascaded from MokaTree).</summary>
	[CascadingParameter(Name = "TreeSelectable")]
	private bool TreeSelectable { get; set; }

	/// <summary>Current depth level (cascaded from parent).</summary>
	[CascadingParameter(Name = "TreeDepth")]
	private int Depth { get; set; }

	/// <inheritdoc />
	protected override string RootClass => "moka-tree-item";

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass("moka-tree-item--disabled", Disabled)
		.AddClass("moka-tree-item--expanded", Expanded)
		.AddClass(Class)
		.Build();

	/// <summary>Tree items have expand/collapse and selection state.</summary>
	protected override bool ShouldRender() => true;

	/// <inheritdoc />
	protected override void OnParametersSet()
	{
		base.OnParametersSet();
		_hasChildren = ChildContent is not null;
	}

	private async Task ToggleExpand()
	{
		if (Disabled)
		{
			return;
		}

		Expanded = !Expanded;
		await ExpandedChanged.InvokeAsync(Expanded);
	}

	private async Task HandleClick()
	{
		if (Disabled)
		{
			return;
		}

		if (TreeSelectable)
		{
			Selected = !Selected;
			await SelectedChanged.InvokeAsync(Selected);
		}
	}
}
