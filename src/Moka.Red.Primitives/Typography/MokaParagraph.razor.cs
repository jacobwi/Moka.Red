using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Enums;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Primitives.Typography;

/// <summary>
///     Semantic paragraph component with auto-spacing, first-line indent, and selectable text support.
///     Renders a &lt;p&gt; element with typography styling via design tokens.
/// </summary>
public partial class MokaParagraph
{
	/// <summary>Content to render inside the paragraph.</summary>
	[Parameter]
	public RenderFragment? ChildContent { get; set; }

	/// <summary>When true, applies a first-line text indent.</summary>
	[Parameter]
	public bool Indent { get; set; }

	/// <summary>Custom indent amount. Defaults to "1.5em" when <see cref="Indent" /> is true.</summary>
	[Parameter]
	public string? IndentValue { get; set; }

	/// <summary>Optional text alignment.</summary>
	[Parameter]
	public MokaTextAlign? Align { get; set; }

	/// <summary>Optional font weight override.</summary>
	[Parameter]
	public MokaFontWeight? Weight { get; set; }

	/// <summary>Custom line-height override (e.g., "1.8", "2rem").</summary>
	[Parameter]
	public string? Leading { get; set; }

	/// <summary>When false, disables text selection via user-select: none. Defaults to true.</summary>
	[Parameter]
	public bool Selectable { get; set; } = true;

	/// <inheritdoc />
	protected override string RootClass => "moka-paragraph";

	/// <inheritdoc />
	protected override string? CssStyle => new StyleBuilder()
		.AddStyle("font-size", SizeValue ?? MokaEnumHelpers.ToFontSize(Size))
		.AddStyle("font-weight", Weight.HasValue ? MokaEnumHelpers.ToCssValue(Weight.Value) : null)
		.AddStyle("text-align", Align.HasValue ? MokaEnumHelpers.ToCssValue(Align.Value) : null)
		.AddStyle("line-height", Leading ?? "var(--moka-line-height-relaxed)")
		.AddStyle("text-indent", Indent ? IndentValue ?? "1.5em" : null)
		.AddStyle("color", Color.HasValue ? $"var(--moka-color-{ColorToKebab(Color.Value)})" : null)
		.AddStyle("user-select", "none", !Selectable)
		.AddStyle("margin", ResolvedMargin)
		.AddStyle("padding", ResolvedPadding)
		.AddStyle(Style)
		.Build();
}
