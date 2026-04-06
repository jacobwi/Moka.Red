using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Primitives.Tree;

/// <summary>
///     Expandable tree view for hierarchical data.
///     Contains <see cref="MokaTreeItem" /> children.
/// </summary>
public partial class MokaTree
{
	/// <summary>Tree items (MokaTreeItem children).</summary>
	[Parameter]
	public RenderFragment? ChildContent { get; set; }

	/// <summary>Allow items to be selected. Default false.</summary>
	[Parameter]
	public bool Selectable { get; set; }

	/// <inheritdoc />
	protected override string RootClass => "moka-tree";

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass("moka-tree--selectable", Selectable)
		.AddClass(Class)
		.Build();
}
