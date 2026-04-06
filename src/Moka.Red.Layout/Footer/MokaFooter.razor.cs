using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Base;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Layout.Footer;

/// <summary>
///     A semantic footer component that can be fixed to the bottom, sticky, or flow normally.
///     Renders a <c>&lt;footer&gt;</c> element with configurable positioning, border, and elevation.
/// </summary>
public partial class MokaFooter : MokaVisualComponentBase
{
	/// <summary>The child content to render inside the footer.</summary>
	[Parameter]
	public RenderFragment? ChildContent { get; set; }

	/// <summary>Footer positioning behavior. Default <see cref="MokaFooterPosition.Static" />.</summary>
	[Parameter]
	public MokaFooterPosition Position { get; set; } = MokaFooterPosition.Static;

	/// <summary>When true (default), shows a top border.</summary>
	[Parameter]
	public bool Bordered { get; set; } = true;

	/// <summary>When true, adds a subtle box-shadow for elevation. Default false.</summary>
	[Parameter]
	public bool Elevated { get; set; }

	/// <inheritdoc />
	protected override string RootClass => "moka-footer";

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass($"moka-footer--{MokaEnumHelpers.ToCssClass(Position)}")
		.AddClass("moka-footer--bordered", Bordered)
		.AddClass("moka-footer--elevated", Elevated)
		.AddClass(Class)
		.Build();

	/// <inheritdoc />
	protected override string? CssStyle => new StyleBuilder()
		.AddStyle("margin", ResolvedMargin)
		.AddStyle("padding", ResolvedPadding ?? "var(--moka-spacing-sm) var(--moka-spacing-lg)")
		.AddStyle(Style)
		.Build();
}
