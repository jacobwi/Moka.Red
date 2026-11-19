using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Moka.Red.Data.Table;

// Draws a table's markup when the table asks, never when its parameters arrive. Blazor hands the columns and the other
// parts their parameters after the table has rendered, so markup drawn in the table's own render would call the
// templates the parent passed the time before. The table asks once its parts have reported, which QuickGrid gets by
// drawing its grid in a Defer component after its columns. Asking from the end of the collection also waits for the
// columns of a heading group, which get their parameters one step after the group.
internal sealed class MokaTableDraw : ComponentBase
{
	[Parameter]
	public RenderFragment? ChildContent { get; set; }

	// Razor doesn't find an internal component by its tag, so markup opens it here. The capture receives it, as @ref would.
	public static RenderFragment For(RenderFragment content, Action<object> capture) => builder =>
	{
		builder.OpenComponent<MokaTableDraw>(0);
		builder.AddComponentParameter(1, nameof(ChildContent), content);
		builder.AddComponentReferenceCapture(2, capture);
		builder.CloseComponent();
	};

	// A draw already waiting in the render queue covers this one.
	public void Draw() => StateHasChanged();

	public override Task SetParametersAsync(ParameterView parameters)
	{
		parameters.SetParameterProperties(this);
		return Task.CompletedTask;
	}

	protected override void BuildRenderTree(RenderTreeBuilder builder)
	{
		ArgumentNullException.ThrowIfNull(builder);
		builder.AddContent(0, ChildContent);
	}
}
