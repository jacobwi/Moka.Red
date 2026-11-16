using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Moka.Red.Primitives.Carousel;
using Moka.Red.Primitives.InfiniteCarousel;

namespace Moka.Red.Primitives.Tests.Components;

// A slide registered by appending, so one inside an @if that the page rendered again went last. The infinite carousel
// copied the wrong slides into its wrap-around ends, and the active index of both carousels pointed at whichever slide
// now held it when a slide before the showing one came or went.
public class CarouselSlideOrderTests : BunitContext
{
	[Fact]
	public async Task Carousel_RemovingASlideBeforeTheShowingOne_KeepsItShowing_AndTellsTheParent()
	{
		IRenderedComponent<CarouselHost> host = Render<CarouselHost>(p => p.Add(x => x.Active, 2));

		await host.InvokeAsync(() => host.Instance.Set(showSecond: false));

		host.WaitForAssertion(() => Assert.Equal(1, host.Instance.Active));
		Assert.Contains("translateX(-100%)", host.Find(".moka-carousel-track").GetAttribute("style"), StringComparison.Ordinal);
	}

	[Fact]
	public async Task Carousel_ASlideComingBackBeforeTheShowingOne_KeepsItShowing_AndTellsTheParent()
	{
		IRenderedComponent<CarouselHost> host = Render<CarouselHost>(p => p.Add(x => x.Active, 2));
		await host.InvokeAsync(() => host.Instance.Set(showSecond: false));
		host.WaitForAssertion(() => Assert.Equal(1, host.Instance.Active));

		await host.InvokeAsync(() => host.Instance.Set(showSecond: true));

		host.WaitForAssertion(() => Assert.Equal(2, host.Instance.Active));
		Assert.Contains("translateX(-200%)", host.Find(".moka-carousel-track").GetAttribute("style"), StringComparison.Ordinal);
		Assert.Equal(4, host.FindAll(".moka-carousel-dot").Count);
	}

	[Fact]
	public async Task InfiniteCarousel_ASlideThatComesBack_IsCopiedIntoTheRightEnd()
	{
		IRenderedComponent<InfiniteHost> host = Render<InfiniteHost>();

		await host.InvokeAsync(() => host.Instance.Set(showFirst: false));
		await host.InvokeAsync(() => host.Instance.Set(showFirst: true));

		string[] slides = [.. host.FindAll(".moka-infinite-carousel-track > div").Select(slide => slide.TextContent.Trim())];
		Assert.Equal(["Third", "First", "Second", "Third", "First"], slides);
	}

	[Fact]
	public async Task InfiniteCarousel_ASlideComingBackBeforeTheShowingOne_KeepsItShowing_AndTellsTheParent()
	{
		IRenderedComponent<InfiniteHost> host = Render<InfiniteHost>(p => p.Add(x => x.Active, 2));
		await host.InvokeAsync(() => host.Instance.Set(showFirst: false));
		host.WaitForAssertion(() => Assert.Equal(1, host.Instance.Active));

		await host.InvokeAsync(() => host.Instance.Set(showFirst: true));

		host.WaitForAssertion(() => Assert.Equal(2, host.Instance.Active));
		Assert.Contains("translateX(-300%)", host.Find(".moka-infinite-carousel-track").GetAttribute("style"), StringComparison.Ordinal);
	}

	private static void Slide<TSlide>(RenderTreeBuilder builder, int sequence, string text) where TSlide : IComponent
	{
		builder.OpenComponent<TSlide>(sequence);
		builder.AddComponentParameter(sequence + 1, "ChildContent", (RenderFragment)(b => b.AddContent(0, text)));
		builder.CloseComponent();
	}

	private sealed class CarouselHost : ComponentBase
	{
		private bool _showSecond = true;

		[Parameter] public int Active { get; set; }

		public void Set(bool showSecond)
		{
			_showSecond = showSecond;
			StateHasChanged();
		}

		protected override void BuildRenderTree(RenderTreeBuilder builder)
		{
			builder.OpenComponent<MokaCarousel>(0);
			builder.AddComponentParameter(1, nameof(MokaCarousel.ActiveIndex), Active);
			builder.AddComponentParameter(2, nameof(MokaCarousel.ActiveIndexChanged),
				EventCallback.Factory.Create<int>(this, value => Active = value));
			builder.AddComponentParameter(3, nameof(MokaCarousel.ChildContent), (RenderFragment)Slides);
			builder.CloseComponent();
		}

		private void Slides(RenderTreeBuilder builder)
		{
			Slide<MokaCarouselSlide>(builder, 0, "First");
			if (_showSecond)
			{
				Slide<MokaCarouselSlide>(builder, 10, "Second");
			}

			Slide<MokaCarouselSlide>(builder, 20, "Third");
			Slide<MokaCarouselSlide>(builder, 30, "Fourth");
		}
	}

	private sealed class InfiniteHost : ComponentBase
	{
		private bool _showFirst = true;

		[Parameter] public int Active { get; set; }

		public void Set(bool showFirst)
		{
			_showFirst = showFirst;
			StateHasChanged();
		}

		protected override void BuildRenderTree(RenderTreeBuilder builder)
		{
			builder.OpenComponent<MokaInfiniteCarousel>(0);
			builder.AddComponentParameter(1, nameof(MokaInfiniteCarousel.AutoPlay), false);
			builder.AddComponentParameter(2, nameof(MokaInfiniteCarousel.ActiveIndex), Active);
			builder.AddComponentParameter(3, nameof(MokaInfiniteCarousel.ActiveIndexChanged),
				EventCallback.Factory.Create<int>(this, value => Active = value));
			builder.AddComponentParameter(4, nameof(MokaInfiniteCarousel.ChildContent), (RenderFragment)Slides);
			builder.CloseComponent();
		}

		private void Slides(RenderTreeBuilder builder)
		{
			if (_showFirst)
			{
				Slide<MokaInfiniteCarouselSlide>(builder, 0, "First");
			}

			Slide<MokaInfiniteCarouselSlide>(builder, 10, "Second");
			Slide<MokaInfiniteCarouselSlide>(builder, 20, "Third");
		}
	}
}
