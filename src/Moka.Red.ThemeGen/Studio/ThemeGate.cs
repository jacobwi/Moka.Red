using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Moka.Red.ThemeGen.Studio;

// Draws its content again only when its key changes. A RenderFragment parameter counts as changed on every parent
// render (blazor/performance/rendering.md), so without the gate every studio render would redraw the previews, the
// token list and the style rows.
internal sealed class ThemeGate : ComponentBase
{
	private bool _received;

	[Parameter]
	public object? Key { get; set; }

	[Parameter]
	public RenderFragment? ChildContent { get; set; }

	// Razor doesn't find an internal component by its tag, so markup opens it through this.
	public static RenderFragment For(object? key, RenderFragment content) => builder =>
	{
		builder.OpenComponent<ThemeGate>(0);
		builder.AddComponentParameter(1, nameof(Key), key);
		builder.AddComponentParameter(2, nameof(ChildContent), content);
		builder.CloseComponent();
	};

	public override Task SetParametersAsync(ParameterView parameters)
	{
		if (_received && parameters.TryGetValue(nameof(Key), out object? key) && Equals(key, Key))
		{
			return Task.CompletedTask;
		}

		_received = true;
		return base.SetParametersAsync(parameters);
	}

	protected override void BuildRenderTree(RenderTreeBuilder builder) => builder.AddContent(0, ChildContent);
}
