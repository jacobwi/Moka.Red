using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Base;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Layout.Panel;

/// <summary>
///     A titled content panel with optional toolbar actions and collapsible body.
/// </summary>
public partial class MokaPanel : MokaVisualComponentBase
{
	/// <summary>The child content rendered in the panel body.</summary>
	[Parameter]
	public RenderFragment? ChildContent { get; set; }

	/// <summary>Simple text title for the panel header.</summary>
	[Parameter]
	public string? Title { get; set; }

	/// <summary>Custom title content. Overrides <see cref="Title" />.</summary>
	[Parameter]
	public RenderFragment? TitleContent { get; set; }

	/// <summary>Toolbar actions rendered in the header, aligned to the right.</summary>
	[Parameter]
	public RenderFragment? Actions { get; set; }

	/// <summary>When true, the panel body can be collapsed. Default false.</summary>
	[Parameter]
	public bool Collapsible { get; set; }

	/// <summary>Whether the panel body is collapsed. Two-way bindable.</summary>
	[Parameter]
	public bool Collapsed { get; set; }

	/// <summary>Callback when <see cref="Collapsed" /> changes.</summary>
	[Parameter]
	public EventCallback<bool> CollapsedChanged { get; set; }

	/// <summary>When true (default), renders a border around the panel.</summary>
	[Parameter]
	public bool Bordered { get; set; } = true;

	/// <summary>When true, applies elevation shadow instead of border.</summary>
	[Parameter]
	public bool Elevated { get; set; }

	/// <inheritdoc />
	protected override string RootClass => "moka-panel";

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass("moka-panel--bordered", Bordered && !Elevated)
		.AddClass("moka-panel--elevated", Elevated)
		.AddClass("moka-panel--collapsed", Collapsed)
		.AddClass(Class)
		.Build();

	/// <inheritdoc />
	protected override string? CssStyle => new StyleBuilder()
		.AddStyle("margin", ResolvedMargin)
		.AddStyle("padding", ResolvedPadding)
		.AddStyle(Style)
		.Build();

	private string? BodyStyle => Collapsed
		? "max-height: 0"
		: "max-height: 1000px";

	private async Task ToggleCollapse()
	{
		Collapsed = !Collapsed;
		await CollapsedChanged.InvokeAsync(Collapsed);
	}
}
