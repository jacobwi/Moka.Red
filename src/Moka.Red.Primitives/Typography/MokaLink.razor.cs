using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Enums;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Primitives.Typography;

/// <summary>
///     Anchor link component with underline behavior, color, and external link support.
///     Renders a semantic &lt;a&gt; element.
/// </summary>
public partial class MokaLink
{
	/// <summary>Content to render inside the link.</summary>
	[Parameter]
	public RenderFragment? ChildContent { get; set; }

	/// <summary>The URL the link points to.</summary>
	[Parameter]
	[EditorRequired]
	public string Href { get; set; } = "#";

	/// <summary>When true, opens link in a new tab with rel="noopener noreferrer".</summary>
	[Parameter]
	public bool External { get; set; }

	/// <summary>Controls underline behavior. Defaults to Hover.</summary>
	[Parameter]
	public MokaLinkUnderline Underline { get; set; } = MokaLinkUnderline.Hover;

	/// <summary>Optional font weight override.</summary>
	[Parameter]
	public MokaFontWeight? Weight { get; set; }

	/// <inheritdoc />
	protected override string RootClass => "moka-link";

	/// <inheritdoc />
	protected override string? CssStyle => new StyleBuilder()
		.AddStyle("font-size", SizeValue ?? MokaEnumHelpers.ToFontSize(Size))
		.AddStyle("font-weight", Weight.HasValue ? MokaEnumHelpers.ToCssValue(Weight.Value) : null)
		.AddStyle("color", Color.HasValue ? $"var(--moka-color-{ColorToKebab(Color.Value)})" : null)
		.AddStyle("margin", ResolvedMargin)
		.AddStyle("padding", ResolvedPadding)
		.AddStyle(Style)
		.Build();

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass($"moka-link--underline-{MokaEnumHelpers.ToCssClass(Underline)}")
		.AddClass(Class)
		.Build();
}
