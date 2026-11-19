using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Moka.Red.Primitives.Carousel;
using Moka.Red.Tests.Shared;

namespace Moka.Red.Primitives.Tests.Components;

// A carousel was as tall as its tallest slide, and a ratio set on its root never reached the viewport, the track or the
// slides. AspectRatio sets the root's ratio and makes the parts inside fill it, so slide content can fill the carousel.
public class MokaCarouselAspectRatioTests : BunitContext
{
	private const string Css = "MokaCarousel.razor.css";

	private static RenderFragment Slides => builder =>
	{
		builder.OpenComponent<MokaCarouselSlide>(0);
		builder.AddComponentParameter(1, nameof(MokaCarouselSlide.ChildContent), (RenderFragment)(b => b.AddContent(0, "One")));
		builder.CloseComponent();
		builder.OpenComponent<MokaCarouselSlide>(2);
		builder.AddComponentParameter(3, nameof(MokaCarouselSlide.ChildContent), (RenderFragment)(b => b.AddContent(0, "Two")));
		builder.CloseComponent();
	};

	[Fact]
	public void WithoutAspectRatio_TheRootIsAsBefore()
	{
		IRenderedComponent<MokaCarousel> cut = Render<MokaCarousel>(p => p.Add(x => x.ChildContent, Slides));

		IElement root = cut.Find(".moka-carousel");
		Assert.Equal("moka-carousel moka-fill-width", root.ClassName);
		Assert.Null(root.GetAttribute("style"));
	}

	[Theory]
	[InlineData("16/9", "aspect-ratio: 16/9")]
	[InlineData(" 4 / 3 ", "aspect-ratio: 4 / 3")]
	[InlineData("var(--hero-ratio)", "aspect-ratio: var(--hero-ratio)")]
	public void AspectRatio_SetsTheRatio_AndMarksTheRootToFill(string ratio, string style)
	{
		IRenderedComponent<MokaCarousel> cut = Render<MokaCarousel>(p => p
			.Add(x => x.AspectRatio, ratio)
			.Add(x => x.ChildContent, Slides));

		IElement root = cut.Find(".moka-carousel");
		Assert.Contains("moka-carousel--ratio", root.ClassList);
		Assert.Equal(style, root.GetAttribute("style"));
	}

	[Fact]
	public void AspectRatio_ComesBeforeTheAppsStyle()
	{
		IRenderedComponent<MokaCarousel> cut = Render<MokaCarousel>(p => p
			.Add(x => x.AspectRatio, "1/1")
			.Add(x => x.Style, "max-width: 20rem")
			.Add(x => x.ChildContent, Slides));

		Assert.Equal("aspect-ratio: 1/1; max-width: 20rem", cut.Find(".moka-carousel").GetAttribute("style"));
	}

	[Theory]
	[InlineData("16:9")]
	[InlineData("16/9; height: 0")]
	[InlineData("wide")]
	public void AnAspectRatioThatIsntOne_IsIgnored(string ratio)
	{
		IRenderedComponent<MokaCarousel> cut = Render<MokaCarousel>(p => p
			.Add(x => x.AspectRatio, ratio)
			.Add(x => x.ChildContent, Slides));

		IElement root = cut.Find(".moka-carousel");
		Assert.DoesNotContain("moka-carousel--ratio", root.ClassList);
		Assert.Null(root.GetAttribute("style"));
	}

	// The root clips, so its content can't stretch it past the ratio; the viewport and the track fill it, and the slides
	// stretch to the track, which gives content at height: 100% a height to fill.
	[Fact]
	public void WithARatio_TheViewportAndTheTrackFillTheRoot()
	{
		Assert.Equal("hidden", ScopedCss.Declarations(Css, ".moka-carousel")["overflow"]);
		Assert.Equal("100%", ScopedCss.Declarations(Css, ".moka-carousel--ratio .moka-carousel-viewport")["height"]);
		Assert.Equal("100%", ScopedCss.Declarations(Css, ".moka-carousel--ratio .moka-carousel-track")["height"]);
		Assert.False(ScopedCss.Declarations(Css, ".moka-carousel-track").ContainsKey("align-items"));
	}
}
