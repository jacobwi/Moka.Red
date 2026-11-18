using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Moka.Red.Primitives.InfiniteCarousel;

namespace Moka.Red.Primitives.Tests.Components;

// A slide rendered by a component of the page's reports after the carousel's own slides, so the infinite carousel copied
// the wrong slides into its wrap-around ends. Order says where it stands.
public class CarouselSlideOrderKeyTests : BunitContext
{
	[Fact]
	public void AWrappedSlideWithoutOrder_ListsLast_SoTheEndsCopyTheWrongSlides()
	{
		IRenderedComponent<Host> host = Render<Host>(p => p.Add(x => x.Orders, [0, 0, 0]));

		Assert.Equal(["Second", "First", "Second", "Third", "First"], Track(host));
	}

	[Fact]
	public void AWrappedSlide_StandsWhereItsOrderSays()
	{
		IRenderedComponent<Host> host = Render<Host>(p => p.Add(x => x.Orders, [1, 2, 3]));

		Assert.Equal(["Third", "First", "Second", "Third", "First"], Track(host));
	}

	private static string[] Track(IRenderedComponent<Host> host) =>
		[.. host.FindAll(".moka-infinite-carousel-track > div").Select(slide => slide.TextContent.Trim())];

	private sealed class Host : ComponentBase
	{
		[Parameter] public int[] Orders { get; set; } = [0, 0, 0];

		protected override void BuildRenderTree(RenderTreeBuilder builder)
		{
			builder.OpenComponent<MokaInfiniteCarousel>(0);
			builder.AddComponentParameter(1, nameof(MokaInfiniteCarousel.AutoPlay), false);
			builder.AddComponentParameter(2, nameof(MokaInfiniteCarousel.ChildContent), (RenderFragment)Slides);
			builder.CloseComponent();
		}

		private void Slides(RenderTreeBuilder builder)
		{
			Slide(builder, 0, "First", Orders[0]);
			builder.OpenComponent<WrappedSlide>(10);
			builder.AddComponentParameter(11, nameof(WrappedSlide.Text), "Second");
			builder.AddComponentParameter(12, nameof(WrappedSlide.Order), Orders[1]);
			builder.CloseComponent();
			Slide(builder, 20, "Third", Orders[2]);
		}

		internal static void Slide(RenderTreeBuilder builder, int sequence, string text, int order)
		{
			builder.OpenComponent<MokaInfiniteCarouselSlide>(sequence);
			builder.AddComponentParameter(sequence + 1, nameof(MokaInfiniteCarouselSlide.Order), order);
			builder.AddComponentParameter(sequence + 2, nameof(MokaInfiniteCarouselSlide.ChildContent), (RenderFragment)(b => b.AddContent(0, text)));
			builder.CloseComponent();
		}
	}

	private sealed class WrappedSlide : ComponentBase
	{
		[Parameter] public string Text { get; set; } = "";

		[Parameter] public int Order { get; set; }

		protected override void BuildRenderTree(RenderTreeBuilder builder) => Host.Slide(builder, 0, Text, Order);
	}
}
