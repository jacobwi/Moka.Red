using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Base;
using Moka.Red.Core.Enums;
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
		.AddClass("moka-skeleton--rounded", Rounded is not null && Rounded != MokaRounding.None)
		.AddClass(Class)
		.Build();

	/// <inheritdoc />
	protected override string? CssStyle => new StyleBuilder()
		.AddStyle("border-radius", ResolvedRounding)
		.AddStyle("margin", ResolvedMargin)
		.AddStyle("padding", ResolvedPadding)
		.AddStyle(Style)
		.Build();

	private string CircleSizeStyle
	{
		get
		{
			string w = Width ?? "40px";
			string h = Height ?? w;
			return $"width: {w}; height: {h}";
		}
	}

	private string RectSizeStyle
	{
		get
		{
			string w = Width ?? "100%";
			string h = Height ?? "48px";
			return $"width: {w}; height: {h}";
		}
	}
}
