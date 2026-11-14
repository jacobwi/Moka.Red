using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Moka.Red.Feedback.HoverCard;
using Moka.Red.Tests.Shared;

namespace Moka.Red.Feedback.Tests.Components;

// The card shows once the pointer has rested on the trigger, and hides a moment after the pointer leaves, so the pointer
// can move onto it. The pointer routinely moves on before either wait runs out, and a wait cut short changes nothing.
public class MokaHoverCardDelayTests : BunitContext
{
	private static readonly TimeSpan ShowDelay = TimeSpan.FromMilliseconds(300);
	private static readonly TimeSpan HideDelay = TimeSpan.FromMilliseconds(150);
	private static readonly TimeSpan OneMillisecond = TimeSpan.FromMilliseconds(1);

	private readonly FakeClock _clock = new();

	public MokaHoverCardDelayTests()
	{
		JSInterop.Mode = JSRuntimeMode.Loose;
		Services.AddSingleton<TimeProvider>(_clock);
	}

	[Fact]
	public async Task RestingOnTheTrigger_ShowsTheCardOnceTheDelayRunsOut()
	{
		IRenderedComponent<MokaHoverCard> cut = RenderCard();

		await Trigger(cut).TriggerEventAsync("onmouseenter", new MouseEventArgs());
		await AdvanceAsync(cut, ShowDelay - OneMillisecond);
		Assert.Empty(Cards(cut));

		await AdvanceAsync(cut, OneMillisecond);
		Assert.Single(Cards(cut));
	}

	[Fact]
	public async Task LeavingBeforeTheDelayRunsOut_ShowsNothing()
	{
		IRenderedComponent<MokaHoverCard> cut = RenderCard();

		await Trigger(cut).TriggerEventAsync("onmouseenter", new MouseEventArgs());
		await AdvanceAsync(cut, ShowDelay - OneMillisecond);
		await Trigger(cut).TriggerEventAsync("onmouseleave", new MouseEventArgs());
		await AdvanceAsync(cut, TimeSpan.FromSeconds(5));

		Assert.Empty(Cards(cut));
	}

	[Fact]
	public async Task AShowThatAlreadyFired_DoesNothingOnceThePointerHasLeft()
	{
		IRenderedComponent<MokaHoverCard> cut = RenderCard();

		await Trigger(cut).TriggerEventAsync("onmouseenter", new MouseEventArgs());
		FakeTimer waiting = _clock.Timers.Single();
		await Trigger(cut).TriggerEventAsync("onmouseleave", new MouseEventArgs());
		await cut.InvokeAsync(waiting.InvokeCallback);

		Assert.Empty(Cards(cut));
	}

	[Fact]
	public async Task LeavingTheTrigger_HidesTheCardOnceTheGracePeriodRunsOut()
	{
		IRenderedComponent<MokaHoverCard> cut = await ShowCardAsync();

		await Trigger(cut).TriggerEventAsync("onmouseleave", new MouseEventArgs());
		await AdvanceAsync(cut, HideDelay - OneMillisecond);
		Assert.Single(Cards(cut));

		await AdvanceAsync(cut, OneMillisecond);
		Assert.Empty(Cards(cut));
	}

	[Fact]
	public async Task MovingOntoTheCardBeforeTheGracePeriodRunsOut_KeepsItShown()
	{
		IRenderedComponent<MokaHoverCard> cut = await ShowCardAsync();

		await Trigger(cut).TriggerEventAsync("onmouseleave", new MouseEventArgs());
		await AdvanceAsync(cut, HideDelay - OneMillisecond);
		await Cards(cut)[0].TriggerEventAsync("onmouseenter", new MouseEventArgs());
		await AdvanceAsync(cut, TimeSpan.FromSeconds(5));

		Assert.Single(Cards(cut));
	}

	[Fact]
	public async Task NoDelay_ShowsTheCardWithinTheEvent()
	{
		IRenderedComponent<MokaHoverCard> cut = RenderCard(delay: 0);

		await Trigger(cut).TriggerEventAsync("onmouseenter", new MouseEventArgs());

		Assert.Single(Cards(cut));
		Assert.Empty(_clock.Timers);
	}

	[Fact]
	public async Task Disposing_StopsTheWait()
	{
		IRenderedComponent<MokaHoverCard> cut = RenderCard();

		await Trigger(cut).TriggerEventAsync("onmouseenter", new MouseEventArgs());
		await cut.Instance.DisposeAsync();

		Assert.True(_clock.Timers.Single().Disposed);
	}

	private IRenderedComponent<MokaHoverCard> RenderCard(int? delay = null) =>
		Render<MokaHoverCard>(p =>
		{
			p.Add(x => x.ChildContent, "Ada");
			p.Add(x => x.CardContent, "Ada Lovelace");
			if (delay is { } ms)
			{
				p.Add(x => x.Delay, ms);
			}
		});

	private async Task<IRenderedComponent<MokaHoverCard>> ShowCardAsync()
	{
		IRenderedComponent<MokaHoverCard> cut = RenderCard();
		await Trigger(cut).TriggerEventAsync("onmouseenter", new MouseEventArgs());
		await AdvanceAsync(cut, ShowDelay);
		Assert.Single(Cards(cut));
		return cut;
	}

	private Task AdvanceAsync(IRenderedComponent<MokaHoverCard> cut, TimeSpan by) =>
		cut.InvokeAsync(() => _clock.Advance(by));

	private static IElement Trigger(IRenderedComponent<MokaHoverCard> cut) => cut.Find(".moka-hover-card__trigger");

	private static IReadOnlyList<IElement> Cards(IRenderedComponent<MokaHoverCard> cut) =>
		cut.FindAll(".moka-hover-card__card");
}
