using AngleSharp.Dom;
using Bunit;
using Moka.Red.Core.Enums;
using Moka.Red.Primitives.Countdown;

namespace Moka.Red.Primitives.Tests.Components;

// Targets carry a spare 30 seconds or so, so a test never runs across the boundary it reads.
public class MokaCountdownTests : BunitContext
{
	private static string[] Values(IRenderedComponent<MokaCountdown> cut) =>
		cut.FindAll(".moka-countdown-value").Select(e => e.TextContent).ToArray();

	private static bool IsComplete(IRenderedComponent<MokaCountdown> cut) =>
		cut.FindAll(".moka-countdown-completed").Count == 1;

	// A hidden unit was dropped: with days hidden, 2 days and 3 hours showed as 03 hours.
	[Fact]
	public void HiddenDays_RollIntoTheHours()
	{
		IRenderedComponent<MokaCountdown> cut = Render<MokaCountdown>(p => p
			.Add(x => x.TargetDate, DateTime.Now.AddDays(2).AddHours(3).AddSeconds(30))
			.Add(x => x.ShowDays, false));

		Assert.Equal(["51", "00"], Values(cut)[..2]);
	}

	[Fact]
	public void HiddenDaysAndHours_RollIntoTheMinutes()
	{
		IRenderedComponent<MokaCountdown> cut = Render<MokaCountdown>(p => p
			.Add(x => x.TargetDate, DateTime.Now.AddDays(1).AddHours(1).AddMinutes(5).AddSeconds(30))
			.Add(x => x.ShowDays, false)
			.Add(x => x.ShowHours, false));

		Assert.Equal("1505", Values(cut)[0]);
	}

	[Fact]
	public void AHiddenMiddleUnit_RollsIntoTheNextShownOne()
	{
		IRenderedComponent<MokaCountdown> cut = Render<MokaCountdown>(p => p
			.Add(x => x.TargetDate, DateTime.Now.AddDays(2).AddHours(3).AddMinutes(4).AddSeconds(30))
			.Add(x => x.ShowHours, false)
			.Add(x => x.ShowSeconds, false));

		Assert.Equal(["02", "184"], Values(cut));
	}

	// The target was compared with DateTime.Now whatever its Kind, so a UTC target was off by the
	// machine's UTC offset.
	[Fact]
	public void AUtcTarget_IsReadAsUtc()
	{
		IRenderedComponent<MokaCountdown> cut = Render<MokaCountdown>(p => p
			.Add(x => x.TargetDate, DateTime.UtcNow.AddHours(2).AddMinutes(30).AddSeconds(30)));

		Assert.Equal(["00", "02", "30"], Values(cut)[..3]);
	}

	[Fact]
	public void AnUnspecifiedTarget_IsReadAsLocalTime()
	{
		DateTime target = DateTime.SpecifyKind(DateTime.Now.AddHours(2).AddMinutes(30).AddSeconds(30),
			DateTimeKind.Unspecified);

		IRenderedComponent<MokaCountdown> cut = Render<MokaCountdown>(p => p.Add(x => x.TargetDate, target));

		Assert.Equal(["00", "02", "30"], Values(cut)[..3]);
	}

	// With no OnParametersSet a new target showed only on the next tick.
	[Fact]
	public void ANewTarget_ShowsAtOnce()
	{
		IRenderedComponent<MokaCountdown> cut = Render<MokaCountdown>(p => p
			.Add(x => x.TargetDate, DateTime.Now.AddHours(1).AddSeconds(30)));

		cut.Render(p => p.Add(x => x.TargetDate, DateTime.Now.AddHours(3).AddSeconds(30)));

		Assert.Equal("03", Values(cut)[1]);
	}

	// Completion disposed the timer for good, so a new target was ignored.
	[Fact]
	public void ANewTarget_AfterCompletion_StartsOver()
	{
		IRenderedComponent<MokaCountdown> cut = Render<MokaCountdown>(p => p
			.Add(x => x.TargetDate, DateTime.Now.AddMinutes(-1)));
		Assert.True(IsComplete(cut));

		cut.Render(p => p.Add(x => x.TargetDate, DateTime.Now.AddSeconds(5.5)));

		Assert.False(IsComplete(cut));
		Assert.DoesNotContain("moka-countdown--complete", cut.Find(".moka-countdown").ClassName, StringComparison.Ordinal);
		Assert.Equal("05", Values(cut)[3]);
		cut.WaitForAssertion(() => Assert.Equal("04", Values(cut)[3]), TimeSpan.FromSeconds(3));
	}

	// OnComplete ran inside OnInitialized, before the component had rendered anything.
	[Fact]
	public void APassedTarget_RaisesOnComplete_AfterTheFirstRender()
	{
#pragma warning disable CA2000 // The renderer takes the instance over and disposes it with the test context.
		ProbedCountdown countdown = new();
#pragma warning restore CA2000
		ComponentFactories.Add<MokaCountdown>(countdown);
		bool? renderedFirst = null;
		int completions = 0;

		IRenderedComponent<MokaCountdown> cut = Render<MokaCountdown>(p => p
			.Add(x => x.TargetDate, DateTime.Now.AddMinutes(-1))
			.Add(x => x.OnComplete, () =>
			{
				completions++;
				renderedFirst = countdown.Rendered;
			}));

		Assert.Equal(1, completions);
		Assert.True(renderedFirst);
		Assert.True(IsComplete(cut));
	}

	[Fact]
	public void ANewTargetThatHasPassed_RaisesOnComplete_AfterShowingTheCompletedText()
	{
		bool? completedTextShown = null;
		IRenderedComponent<MokaCountdown>? cut = null;
		cut = Render<MokaCountdown>(p => p
			.Add(x => x.TargetDate, DateTime.Now.AddHours(1))
			.Add(x => x.OnComplete, () => completedTextShown = IsComplete(cut!)));

		cut.Render(p => p.Add(x => x.TargetDate, DateTime.Now.AddMinutes(-1)));

		Assert.True(completedTextShown);
	}

	// The handler's task was discarded, so an exception from an async handler vanished.
	[Fact]
	public async Task AnExceptionFromOnComplete_ReachesTheRenderer()
	{
		Render<MokaCountdown>(p => p
			.Add(x => x.TargetDate, DateTime.Now.AddMinutes(-1))
			.Add(x => x.OnComplete, async () =>
			{
				await Task.Yield();
				throw new InvalidOperationException("Handler failed");
			}));

		Exception error = await Renderer.UnhandledException.WaitAsync(TimeSpan.FromSeconds(5),
			Xunit.TestContext.Current.CancellationToken);

		Assert.Equal("Handler failed", error.Message);
	}

	[Fact]
	public async Task TheTickThatReachesZero_RaisesOnCompleteOnce_AndItsExceptionReachesTheRenderer()
	{
		int completions = 0;
		IRenderedComponent<MokaCountdown> cut = Render<MokaCountdown>(p => p
			.Add(x => x.TargetDate, DateTime.Now.AddSeconds(1.5))
			.Add(x => x.OnComplete, async () =>
			{
				completions++;
				await Task.Yield();
				throw new InvalidOperationException("Late handler failed");
			}));
		Assert.False(IsComplete(cut));

		Exception error = await Renderer.UnhandledException.WaitAsync(TimeSpan.FromSeconds(5),
			Xunit.TestContext.Current.CancellationToken);

		Assert.Equal("Late handler failed", error.Message);
		Assert.Equal(1, completions);

		// The handler runs after the render that shows the completed text.
		Assert.True(IsComplete(cut));
	}

	[Fact]
	public void OnComplete_IsNotRaisedAgain_WhenTheParentRendersTheSameTarget()
	{
		DateTime target = DateTime.Now.AddMinutes(-1);
		int completions = 0;
		IRenderedComponent<MokaCountdown> cut = Render<MokaCountdown>(p => p
			.Add(x => x.TargetDate, target)
			.Add(x => x.OnComplete, () => completions++));

		cut.Render(p => p.Add(x => x.TargetDate, target).Add(x => x.CompletedText, "Done"));

		Assert.Equal(1, completions);
		Assert.Equal("Done", cut.Find(".moka-countdown-completed").TextContent);
	}

	// The Flip style had keyframes that nothing used, so its cards never moved.
	[Fact]
	public void Flip_DoesNotAnimateTheFirstRender()
	{
		IRenderedComponent<MokaCountdown> cut = Render<MokaCountdown>(p => p
			.Add(x => x.TargetDate, DateTime.Now.AddHours(5).AddMinutes(30).AddSeconds(30))
			.Add(x => x.CountdownStyle, MokaCountdownStyle.Flip));

		Assert.Empty(cut.FindAll(".moka-countdown-value--changed"));
	}

	[Fact]
	public void Flip_AnimatesTheSecondsOnEachTick()
	{
		IRenderedComponent<MokaCountdown> cut = Render<MokaCountdown>(p => p
			.Add(x => x.TargetDate, DateTime.Now.AddHours(5).AddMinutes(30).AddSeconds(30))
			.Add(x => x.CountdownStyle, MokaCountdownStyle.Flip));
		string first = Values(cut)[3];

		cut.WaitForAssertion(() => Assert.NotEqual(first, Values(cut)[3]), TimeSpan.FromSeconds(3));

		IReadOnlyList<IElement> values = cut.FindAll(".moka-countdown-value");
		Assert.Contains("moka-countdown-value--changed", values[3].ClassName, StringComparison.Ordinal);
		Assert.DoesNotContain("moka-countdown-value--changed", values[1].ClassName, StringComparison.Ordinal);
	}

	[Fact]
	public void Flip_AnimatesTheValuesThatChanged()
	{
		DateTime target = DateTime.Now.AddHours(5).AddMinutes(30).AddSeconds(30);
		IRenderedComponent<MokaCountdown> cut = Render<MokaCountdown>(p => p
			.Add(x => x.TargetDate, target)
			.Add(x => x.CountdownStyle, MokaCountdownStyle.Flip));

		cut.Render(p => p.Add(x => x.TargetDate, target.AddHours(1)));

		IReadOnlyList<IElement> values = cut.FindAll(".moka-countdown-value");
		Assert.Equal("06", values[1].TextContent);
		Assert.Contains("moka-countdown-value--changed", values[1].ClassName, StringComparison.Ordinal);
		Assert.DoesNotContain("moka-countdown-value--changed", values[0].ClassName, StringComparison.Ordinal);
		Assert.DoesNotContain("moka-countdown-value--changed", values[2].ClassName, StringComparison.Ordinal);
	}

	[Fact]
	public void OtherStyles_DoNotAnimate()
	{
		DateTime target = DateTime.Now.AddHours(5).AddMinutes(30).AddSeconds(30);
		IRenderedComponent<MokaCountdown> cut = Render<MokaCountdown>(p => p.Add(x => x.TargetDate, target));

		cut.Render(p => p.Add(x => x.TargetDate, target.AddHours(1)));

		Assert.Empty(cut.FindAll(".moka-countdown-value--changed"));
	}

	// Lets a test ask, from inside OnComplete, whether the component has rendered yet.
	private sealed class ProbedCountdown : MokaCountdown
	{
		public bool Rendered => HasRendered;
	}
}
