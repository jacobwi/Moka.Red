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

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass(Class)
		.Build();

	/// <inheritdoc />
	/// <remarks>A radius here also clips the band's ends, since the wrapper hides its overflow.</remarks>
	protected override string? CssStyle => new StyleBuilder()
		.AddStyle("margin", ResolvedMargin)
		.AddStyle("padding", ResolvedPadding)
		.AddStyle("border-radius", ResolvedRounding)
		.AddStyle(Style)
		.Build();

	private string RibbonCss => new CssBuilder("moka-ribbon")
		.AddClass($"moka-ribbon--{MokaEnumHelpers.ToCssClass(Position)}")
		.AddClass($"moka-ribbon--{ColorToKebab(Color ?? MokaColor.Primary)}")
		.Build();
}
