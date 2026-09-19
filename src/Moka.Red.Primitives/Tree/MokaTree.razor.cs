using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Primitives.Tree;

/// <summary>
///     Expandable tree view for hierarchical data.
///     Contains <see cref="MokaTreeItem" /> children.
/// </summary>
public partial class MokaTree
{
	private const string TreeModule = "./_content/Moka.Red.Primitives/Tree/MokaTree.razor.js";

	private ElementReference _root;

	/// <summary>Tree items (MokaTreeItem children).</summary>
	[Parameter]
	public RenderFragment? ChildContent { get; set; }

	/// <summary>Allow items to be selected. Default false.</summary>
	[Parameter]
	public bool Selectable { get; set; }

	/// <inheritdoc />
	protected override string RootClass => "moka-tree";

	/// <inheritdoc />
	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		// The arrow keys, the roving tab stop and Enter/Space live in the browser: they move DOM focus
		// between items spread across many components.
		if (firstRender)
		{
			await SafeModuleInvokeVoidAsync(TreeModule, "bindTree", _root);
		}
	}

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass("moka-tree--selectable", Selectable)
		.AddClass(Class)
		.Build();
}
