using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Enums;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Primitives.Typography;

/// <summary>
///     Label component for form inputs with optional required indicator.
///     Renders a semantic &lt;label&gt; element.
/// </summary>
public partial class MokaLabel
{
	/// <summary>Content to render inside the label.</summary>
	[Parameter]
	public RenderFragment? ChildContent { get; set; }

	/// <summary>The id of the associated input element (HTML for attribute).</summary>
	[Parameter]
	public string? For { get; set; }

	/// <summary>When true, shows a red asterisk after the label text.</summary>
	[Parameter]
	public bool Required { get; set; }

	/// <summary>Optional font weight override. Defaults to Medium.</summary>
	[Parameter]
	public MokaFontWeight? Weight { get; set; }

	/// <summary>Optional text alignment.</summary>
	[Parameter]
	public MokaTextAlign? Align { get; set; }

	/// <summary>
	///     The small uppercase look for a label over a group of values. It sets the size, weight and color, so
	///     <c>Size</c>, <c>SizeValue</c>, <c>Weight</c> and <c>Color</c> are ignored.
	/// </summary>
	[Parameter]
	public bool Micro { get; set; }

	/// <inheritdoc />
	protected override string RootClass => "moka-label";

	/// <inheritdoc />
	protected override string? CssStyle => SpacingStyle()
		.AddStyle("font-size", SizeValue ?? MokaEnumHelpers.ToFontSize(Size), !Micro)
		.AddStyle("font-weight", MokaEnumHelpers.ToCssValue(Weight ?? MokaFontWeight.Medium), !Micro)
		.AddStyle("text-align", Align.HasValue ? MokaEnumHelpers.ToCssValue(Align.Value) : null)
		.AddStyle("color", Color.HasValue ? $"var(--moka-color-{ColorToKebab(Color.Value)})" : null, !Micro)
		.AddStyle(Style)
		.Build();

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass("moka-label--micro", Micro)
		.AddClass("moka-micro-label", Micro)
		.AddClass(Class)
		.Build();
}
