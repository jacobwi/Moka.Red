using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Enums;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Primitives.Typography;

/// <summary>
///     Caption component for secondary text such as descriptions, timestamps, and hints.
///     Renders a semantic &lt;small&gt; element with reduced size and opacity.
/// </summary>
public partial class MokaCaption
{
	/// <summary>Content to render inside the caption.</summary>
	[Parameter]
	public RenderFragment? ChildContent { get; set; }

	/// <summary>Optional text alignment.</summary>
	[Parameter]
	public MokaTextAlign? Align { get; set; }

	/// <summary>
	///     The small uppercase look for a label over a group of values. It sets the size, weight and color, so
	///     <c>SizeValue</c> and <c>Color</c> are ignored.
	/// </summary>
	[Parameter]
	public bool Micro { get; set; }

	/// <inheritdoc />
	protected override string RootClass => "moka-caption";

	/// <inheritdoc />
	protected override string? CssStyle => SpacingStyle()
		.AddStyle("font-size", SizeValue ?? "var(--moka-font-size-xs)", !Micro)
		.AddStyle("color",
			Color.HasValue ? $"var(--moka-color-{ColorToKebab(Color.Value)})" : "var(--moka-color-on-surface)", !Micro)
		.AddStyle("opacity", "0.7", !Color.HasValue && !Micro)
		.AddStyle("line-height", "var(--moka-line-height-base)")
		.AddStyle("text-align", Align.HasValue ? MokaEnumHelpers.ToCssValue(Align.Value) : null)
		.AddStyle(Style)
		.Build();

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass("moka-caption--micro", Micro)
		.AddClass(Class)
		.Build();
}
