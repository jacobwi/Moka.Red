using Bunit;
using Microsoft.AspNetCore.Components;
using Moka.Red.Primitives.Carousel;
using Moka.Red.Primitives.InfiniteCarousel;
using Moka.Red.Primitives.Motion;
using TestContext = Xunit.TestContext;

namespace Moka.Red.Primitives.Tests.Components;

// These raise a consumer's event from a timer, in a task nothing awaited, so what the handler threw vanished.
public class DetachedFailureTests : BunitContext
{
	private static readonly TimeSpan Wait = TimeSpan.FromSeconds(5);

	public DetachedFailureTests() => JSInterop.Mode = JSRuntimeMode.Loose;

	[Fact]
	public async Task Carousel_AnActiveIndexChangedThatFailsOnAutoPlay_ReachesTheRenderer()
	{
		Render<MokaCarousel>(p => p
			.Add(x => x.AutoPlay, true)
			.Add(x => x.Interval, 5)
			.Add(x => x.ActiveIndexChanged, EventCallback.Factory.Create<int>(this, Fail))
			.AddChildContent<MokaCarouselSlide>()
			.AddChildContent<MokaCarouselSlide>());

		await AssertReachedTheRenderer();
	}

	[Fact]
	public async Task InfiniteCarousel_AnActiveIndexChangedThatFailsOnAutoPlay_ReachesTheRenderer()
	{
		Render<MokaInfiniteCarousel>(p => p
			.Add(x => x.AutoPlay, true)
			.Add(x => x.Interval, 5)
			.Add(x => x.Speed, 1)
			.Add(x => x.ActiveIndexChanged, EventCallback.Factory.Create<int>(this, Fail))
			.AddChildContent<MokaInfiniteCarouselSlide>()
			.AddChildContent<MokaInfiniteCarouselSlide>());

		await AssertReachedTheRenderer();
	}

	[Fact]
	public async Task Typewriter_AnOnCompleteThatFails_ReachesTheRenderer()
	{
		Render<MokaTypewriter>(p => p
			.Add(x => x.Text, "a")
			.Add(x => x.Speed, 1)
			.Add(x => x.OnComplete, EventCallback.Factory.Create(this, () => Fail(0))));

		await AssertReachedTheRenderer();
	}

	private async Task AssertReachedTheRenderer()
	{
		Exception error = await Renderer.UnhandledException.WaitAsync(Wait, TestContext.Current.CancellationToken);
		Assert.Equal("Handler failed", error.Message);
	}

	private static async Task Fail(int _)
	{
		await Task.Yield();
		throw new InvalidOperationException("Handler failed");
	}
}
