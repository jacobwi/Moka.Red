using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Base;
using Moka.Red.Core.Enums;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Primitives.Ribbon;

/// <summary>
///     A corner ribbon badge that overlays on a parent container (e.g., "NEW", "SALE", "BETA").
///     Wraps child content in a relative container and positions a rotated ribbon at the chosen corner.
/// </summary>
public partial class MokaRibbon : MokaVisualComponentBase
{
	/// <summary>Ribbon text (e.g., "NEW", "SALE", "BETA").</summary>
	[Parameter]
	[EditorRequired]
	public string Text { get; set; } = string.Empty;

	/// <summary>Corner position for the ribbon. Default is <see cref="MokaRibbonPosition.TopRight" />.</summary>
	[Parameter]
	public MokaRibbonPosition Position { get; set; } = MokaRibbonPosition.TopRight;

	/// <summary>Content to wrap with the ribbon overlay.</summary>
	[Parameter]
	public RenderFragment? ChildContent { get; set; }

	/// <inheritdoc />
	protected override string RootClass => "moka-ribbon-wrapper";

	private string WrapperCss => new CssBuilder("moka-ribbon-wrapper")
		.AddClass(Class)
		.Build();

	private string RibbonCss => new CssBuilder("moka-ribbon")
		.AddClass($"moka-ribbon--{MokaEnumHelpers.ToCssClass(Position)}")
		.AddClass($"moka-ribbon--{ColorToKebab(Color ?? MokaColor.Primary)}")
		.Build();
}
