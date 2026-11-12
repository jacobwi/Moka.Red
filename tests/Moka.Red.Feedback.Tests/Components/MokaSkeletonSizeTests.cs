using AngleSharp.Dom;
using Bunit;
using Moka.Red.Core.Enums;
using Moka.Red.Feedback.Loading;
using Moka.Red.Tests.Shared;

namespace Moka.Red.Feedback.Tests.Components;

// Up to 0.4.2 text lines ignored Height, the last of several lines was 70% whatever Width said, and the card
// ignored Width and Height.
public class MokaSkeletonSizeTests : BunitContext
{
	private const string Stylesheet = "MokaSkeleton.razor.css";

	[Fact]
	public void Text_ByDefault_IsUnchanged()
	{
		IRenderedComponent<MokaSkeleton> cut = Render<MokaSkeleton>(p => p.Add(x => x.Lines, 3));

		Assert.Equal(["width: 100%", "width: 100%", "width: 70%"], LineStyles(cut));
	}

	[Fact]
	public void Text_HeightSetsEveryLine()
	{
		IRenderedComponent<MokaSkeleton> cut = Render<MokaSkeleton>(p => p
			.Add(x => x.Lines, 2)
			.Add(x => x.Height, "1rem"));

		Assert.Equal(["width: 100%; height: 1rem", "width: 70%; height: 1rem"], LineStyles(cut));
	}

	[Fact]
	public void Text_TheLastLineIs70PercentOfWidth()
	{
		IRenderedComponent<MokaSkeleton> cut = Render<MokaSkeleton>(p => p
			.Add(x => x.Lines, 3)
			.Add(x => x.Width, "240px"));

		Assert.Equal(["width: 240px", "width: 240px", "width: calc(240px * 0.7)"], LineStyles(cut));
	}

	[Fact]
	public void Text_AWidthFromAToken_IsScaledToo()
	{
		IRenderedComponent<MokaSkeleton> cut = Render<MokaSkeleton>(p => p
			.Add(x => x.Lines, 2)
			.Add(x => x.Width, "var(--line-width, 60%)"));

		Assert.Equal("width: calc(var(--line-width, 60%) * 0.7)", LineStyles(cut)[1]);
	}

	[Fact]
	public void Text_AKeywordWidth_GoesOnEveryLine()
	{
		IRenderedComponent<MokaSkeleton> cut = Render<MokaSkeleton>(p => p
			.Add(x => x.Lines, 2)
			.Add(x => x.Width, "fit-content"));

		Assert.Equal(["width: fit-content", "width: fit-content"], LineStyles(cut));
	}

	[Fact]
	public void Text_OneLine_TakesWidthWhole()
	{
		IRenderedComponent<MokaSkeleton> cut = Render<MokaSkeleton>(p => p.Add(x => x.Width, "40%"));

		Assert.Equal(["width: 40%"], LineStyles(cut));
	}

	[Fact]
	public void Text_RoundingStillReachesTheLines()
	{
		IRenderedComponent<MokaSkeleton> cut = Render<MokaSkeleton>(p => p
			.Add(x => x.Height, "8px")
			.Add(x => x.Rounded, MokaRounding.None));

		Assert.Equal(["width: 100%; height: 8px; border-radius: 0"], LineStyles(cut));
	}

	[Fact]
	public void Card_ByDefault_IsUnchanged()
	{
		IRenderedComponent<MokaSkeleton> cut = Render<MokaSkeleton>(p => p.Add(x => x.Shape, MokaSkeletonShape.Card));

		IElement card = cut.Find(".moka-skeleton-card");
		Assert.Equal("moka-skeleton-card", card.ClassName);
		Assert.Null(card.GetAttribute("style"));
		Assert.Null(cut.Find(".moka-skeleton-card-image").GetAttribute("style"));
	}

	[Fact]
	public void Card_TakesWidthAndHeight()
	{
		IRenderedComponent<MokaSkeleton> cut = Render<MokaSkeleton>(p => p
			.Add(x => x.Shape, MokaSkeletonShape.Card)
			.Add(x => x.Width, "280px")
			.Add(x => x.Height, "320px"));

		IElement card = cut.Find(".moka-skeleton-card");
		Assert.Equal("width: 280px; height: 320px", card.GetAttribute("style"));
		Assert.Contains("moka-skeleton-card--sized", card.ClassList);
	}

	[Fact]
	public void Card_WidthAlone_KeepsThePictureAt16By9()
	{
		IRenderedComponent<MokaSkeleton> cut = Render<MokaSkeleton>(p => p
			.Add(x => x.Shape, MokaSkeletonShape.Card)
			.Add(x => x.Width, "280px"));

		IElement card = cut.Find(".moka-skeleton-card");
		Assert.Equal("width: 280px", card.GetAttribute("style"));
		Assert.DoesNotContain("moka-skeleton-card--sized", card.ClassList);
	}

	[Fact]
	public void SizedCard_ThePictureFillsWhatTheLinesLeave()
	{
		IReadOnlyDictionary<string, string> picture =
			ScopedCss.Declarations(Stylesheet, ".moka-skeleton-card--sized > .moka-skeleton-card-image");

		Assert.Equal("1 1 0", picture["flex"]);
		Assert.Equal("0", picture["padding-top"]);
		Assert.Equal("0", picture["min-height"]);
	}

	[Fact]
	public void Rectangle_AndCircle_AreUnchanged()
	{
		Assert.Equal("width: 100%; height: 48px", Render<MokaSkeleton>(p => p
			.Add(x => x.Shape, MokaSkeletonShape.Rectangle)).Find(".moka-skeleton-rect").GetAttribute("style"));
		Assert.Equal("width: 48px; height: 48px", Render<MokaSkeleton>(p => p
			.Add(x => x.Shape, MokaSkeletonShape.Circle)
			.Add(x => x.Width, "48px")).Find(".moka-skeleton-circle").GetAttribute("style"));
	}

	private static List<string?> LineStyles(IRenderedComponent<MokaSkeleton> cut) =>
		cut.FindAll(".moka-skeleton-line").Select(line => line.GetAttribute("style")).ToList();
}
