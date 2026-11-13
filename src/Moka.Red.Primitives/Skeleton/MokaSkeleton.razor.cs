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

	/// <summary>
	///     Custom width. Defaults to "100%" for text, rectangle and card, "40px" for circle. With several text lines the
	///     last one is 70% of it, so a paragraph still ends short; a keyword such as <c>auto</c> can't be scaled, and then
	///     every line takes it.
	/// </summary>
	[Parameter]
	public string? Width { get; set; }

	/// <summary>
	///     Custom height. For text it is the height of each line (12px by default), for a circle it defaults to
	///     <see cref="Width" />, for a rectangle to 48px. For a card it is the whole card's height, and the picture area
	///     takes what the three lines leave; unset, the picture is 16:9.
	/// </summary>
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

	private string? TextLineStyle(string width) => ShapeStyle(width, Height);

	private string LastLineWidth => Width switch
	{
		null => "70%",
		_ when CssValues.IsLengthExpression(Width) => $"calc({Width.Trim()} * 0.7)",
		_ => Width
	};

	private string CardClass => new CssBuilder("moka-skeleton-card")
		.AddClass("moka-skeleton-card--sized", Height is not null)
		.Build();

	private string? CardStyle => new StyleBuilder()
		.AddStyle("width", Width)
		.AddStyle("height", Height)
		.Build();

	private string? CardImageStyle => ShapeStyle(null, null);

	private string? ShapeStyle(string? width, string? height) => new StyleBuilder()
		.AddStyle("width", width)
		.AddStyle("height", height)
		.AddStyle("border-radius", ResolvedRounding)
		.Build();
}
