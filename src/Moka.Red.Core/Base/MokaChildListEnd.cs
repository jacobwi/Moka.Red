using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Moka.Red.Core.Base;

// Placed after a parent's content, it renders once every child there has taken its parameters, and settles their order
// then. QuickGrid's Defer does the same for its columns. Its parameter is a delegate, which Blazor always counts as
// changed, so it renders on every render of the parent's content.
internal sealed class MokaChildListEnd : ComponentBase
{
	[Parameter]
	[EditorRequired]
	public Action Finish { get; set; } = default!;

	protected override void BuildRenderTree(RenderTreeBuilder builder) => Finish();
}
