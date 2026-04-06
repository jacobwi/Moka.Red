using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Base;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Primitives.OrgChart;

/// <summary>
///     A hierarchical organization chart that renders a tree of nodes with connecting lines.
///     Uses nested flex containers and CSS pseudo-element connectors.
/// </summary>
/// <typeparam name="TItem">The type of data each node holds.</typeparam>
public partial class MokaOrganizationChart<TItem> : MokaComponentBase
{
	private HashSet<MokaOrgNode<TItem>>? _collapsedNodes;

	/// <summary>The root node of the tree. Required.</summary>
	[Parameter]
	[EditorRequired]
	public MokaOrgNode<TItem> Root { get; set; } = default!;

	/// <summary>Template for rendering each node's card content. Required.</summary>
	[Parameter]
	[EditorRequired]
	public RenderFragment<TItem> ItemTemplate { get; set; } = default!;

	/// <summary>Layout orientation. Defaults to <see cref="MokaOrgOrientation.TopDown" />.</summary>
	[Parameter]
	public MokaOrgOrientation Orientation { get; set; } = MokaOrgOrientation.TopDown;

	/// <summary>CSS color for connector lines. Defaults to the outline token.</summary>
	[Parameter]
	public string? ConnectorColor { get; set; }

	/// <summary>Width of connector lines in pixels. Defaults to 2.</summary>
	[Parameter]
	public int ConnectorWidth { get; set; } = 2;

	/// <summary>Vertical gap between parent and children. Defaults to the xl spacing token.</summary>
	[Parameter]
	public string NodeSpacing { get; set; } = "var(--moka-spacing-xl)";

	/// <summary>Horizontal gap between sibling nodes. Defaults to the xxl spacing token.</summary>
	[Parameter]
	public string LevelSpacing { get; set; } = "var(--moka-spacing-xxl)";

	/// <summary>Whether nodes can be clicked to collapse/expand their subtrees. Defaults to false.</summary>
	[Parameter]
	public bool Collapsible { get; set; }

	/// <inheritdoc />
	protected override string RootClass => "moka-org-chart";

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass("moka-org-chart--left-right", Orientation == MokaOrgOrientation.LeftRight)
		.AddClass(Class)
		.Build();

	/// <inheritdoc />
	protected override string? CssStyle => new StyleBuilder()
		.AddStyle("--moka-org-connector-color", ConnectorColor ?? "var(--moka-color-outline-variant)")
		.AddStyle("--moka-org-connector-width", $"{ConnectorWidth}px")
		.AddStyle("--moka-org-node-spacing", NodeSpacing)
		.AddStyle("--moka-org-level-spacing", LevelSpacing)
		.AddStyle(Style)
		.Build();

	/// <inheritdoc />
	protected override bool ShouldRender() => true;

	private static bool HasVisibleChildren(MokaOrgNode<TItem> node) =>
		node.Children is { Count: > 0 } && !node.IsCollapsed;

	private void ToggleCollapse(MokaOrgNode<TItem> node)
	{
		if (!Collapsible || node.Children is not { Count: > 0 })
		{
			return;
		}

		// Records are immutable; we swap the node in the tree by rebuilding it.
		// Since the parent re-renders the whole tree, we just toggle via a mutable wrapper approach:
		// Actually, since we re-render the whole tree from Root and records are immutable,
		// we need to mutate the root. The simplest approach for Blazor is to use "with" and
		// let the parent own the Root parameter. For internal collapsing without parent involvement,
		// we track collapsed state internally.
		_collapsedNodes ??= [];

		if (!_collapsedNodes.Remove(node))
		{
			_collapsedNodes.Add(node);
		}
	}

	private bool IsCollapsed(MokaOrgNode<TItem> node) =>
		node.IsCollapsed || (_collapsedNodes?.Contains(node) ?? false);

	private static bool HasExpandableChildren(MokaOrgNode<TItem> node) =>
		node.Children is { Count: > 0 };

	private bool ShouldShowChildren(MokaOrgNode<TItem> node) =>
		HasExpandableChildren(node) && !IsCollapsed(node);
}
