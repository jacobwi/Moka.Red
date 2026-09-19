using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Base;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Feedback.Loading;

/// <summary>
///     Shimmer placeholder shapes for content that is loading.
///     Supports text lines, circles, rectangles, and card layouts.
/// </summary>
public partial class MokaSkeleton : MokaVisualComponentBase
{
	/// <summary>Shape of the skeleton placeholder. Defaults to <see cref="MokaSkeletonShape.Text" />.</summary>
	[Parameter]
	public MokaSkeletonShape Shape { get; set; } = MokaSkeletonShape.Text;

	/// <summary>
	///     Number of text lines to render. Only used when <see cref="Shape" /> is <see cref="MokaSkeletonShape.Text" />.
	///     Defaults to 1.
	/// </summary>
	[Parameter]
	public int Lines { get; set; } = 1;

	/// <summary>Custom width. Defaults to "100%" for text/rectangle, "40px" for circle.</summary>
	[Parameter]
	public string? Width { get; set; }

	/// <summary>Custom height. Default varies by shape.</summary>
	[Parameter]
	public string? Height { get; set; }

	/// <summary>Animation type. Defaults to <see cref="MokaSkeletonAnimation.Shimmer" />.</summary>
	[Parameter]
	public MokaSkeletonAnimation Animation { get; set; } = MokaSkeletonAnimation.Shimmer;

	/// <inheritdoc />
	protected override string RootClass => "moka-skeleton";

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass($"moka-skeleton--{MokaEnumHelpers.ToCssClass(Animation)}")
		.AddClass(Class)
		.Build();

	// The root only holds the shapes and draws nothing, so a radius there would not show. Every
	// shape takes it instead. Margin and padding stay on the root.

	/// <inheritdoc />
	protected override string? CssStyle => new StyleBuilder()
		.AddStyle("margin", ResolvedMargin)
		.AddStyle("padding", ResolvedPadding)
		.AddStyle(Style)
		.Build();

	private string? CircleSizeStyle
	{
		get
		{
			string w = Width ?? "40px";
			return ShapeStyle(w, Height ?? w);
		}
	}

	private string? RectSizeStyle => ShapeStyle(Width ?? "100%", Height ?? "48px");

	private string? LineStyle(string width) => ShapeStyle(width, null);

	private string? CardImageStyle => ShapeStyle(null, null);

	private string? ShapeStyle(string? width, string? height) => new StyleBuilder()
		.AddStyle("width", width)
		.AddStyle("height", height)
		.AddStyle("border-radius", ResolvedRounding)
		.Build();
}
