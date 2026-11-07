using System.Globalization;
using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Moka.Red.Feedback.ActionBar;
using Moka.Red.Feedback.ErrorBoundary;
using Moka.Red.Tests.Shared;

namespace Moka.Red.Feedback.Tests.Components;

// The clock is the test's own, so nothing here waits on a real second.
public class MokaActionBarTests : BunitContext
{
	private const string Stylesheet = "MokaActionBar.razor.css";

	private static readonly TimeSpan Plenty = TimeSpan.FromSeconds(10);

	private readonly ManualClock _clock = new();

	public MokaActionBarTests()
	{
		JSInterop.Mode = JSRuntimeMode.Loose;
		Services.AddSingleton<TimeProvider>(_clock);
	}

	private IRenderedComponent<MokaActionBar> Bar(Action<ComponentParameterCollectionBuilder<MokaActionBar>>? more = null) =>
		Render<MokaActionBar>(p =>
		{
			p.Add(x => x.Visible, true);
			more?.Invoke(p);
		});

	private IRenderedComponent<MokaActionBar> Counting(TimeSpan countdown, TimeSpan? duration = null) =>
		Bar(p => p.Add(x => x.Countdown, countdown).Add(x => x.Duration, duration));

	private static void AssertRing(IRenderedComponent<MokaActionBar> cut, string text) =>
		cut.WaitForAssertion(() => Assert.Equal(text, cut.Find(".moka-action-bar__ring-text").TextContent), Plenty);

	[Fact]
	public void Hidden_ItDrawsNoBar()
	{
		IRenderedComponent<MokaActionBar> cut = Render<MokaActionBar>(p => p
			.Add(x => x.Title, b => b.AddContent(0, "Waiting")));

		Assert.Empty(cut.FindAll(".moka-action-bar"));
		Assert.Empty(cut.FindAll(".moka-action-bar-dock"));
		Assert.Equal(string.Empty, cut.Find("[role=status]").TextContent);
	}

	[Fact]
	public void Visible_ItIsANamedRegion()
	{
		IElement bar = Bar().Find(".moka-action-bar");

		Assert.Equal("region", bar.GetAttribute("role"));
		Assert.Equal("Waiting on you", bar.GetAttribute("aria-label"));
	}

	[Fact]
	public void Label_NamesIt()
	{
		IElement bar = Bar(p => p.Add(x => x.Label, "Change waiting")).Find(".moka-action-bar");

		Assert.Equal("Change waiting", bar.GetAttribute("aria-label"));
	}

	// A region inserted already filled isn't read out, so the announcement goes in one that was on the page first.
	[Fact]
	public void Showing_AnnouncesTheTitle_ThroughARegionThatWasAlreadyThere()
	{
		IRenderedComponent<MokaActionBar> cut = Render<MokaActionBar>(p => p
			.Add(x => x.Title, b => b.AddContent(0, "3 changes are live")));
		IElement status = cut.Find("[role=status]");
		Assert.Equal(string.Empty, status.TextContent);

		cut.Render(p => p.Add(x => x.Visible, true));

		Assert.Equal("3 changes are live", cut.Find("[role=status]").TextContent);
		Assert.Contains("moka-visually-hidden", cut.Find("[role=status]").ClassName, StringComparison.Ordinal);
	}

	[Fact]
	public void TheSlots_LandWhereTheyBelong()
	{
		IRenderedComponent<MokaActionBar> cut = Bar(p => p
			.Add(x => x.Icon, b => b.AddContent(0, "I"))
			.Add(x => x.Title, b => b.AddContent(0, "T"))
			.Add(x => x.Detail, b => b.AddContent(0, "D"))
			.Add(x => x.Actions, b => b.AddContent(0, "A"))
			.Add(x => x.Body, b => b.AddContent(0, "B")));

		Assert.Equal("I", cut.Find(".moka-action-bar__icon").TextContent);
		Assert.Equal("T", cut.Find(".moka-action-bar__title").TextContent);
		Assert.Equal("D", cut.Find(".moka-action-bar__detail").TextContent);
		Assert.Equal("A", cut.Find(".moka-action-bar__actions").TextContent);
		Assert.Equal("B", cut.Find(".moka-action-bar__body").TextContent);
	}

	[Fact]
	public void SlotsLeftOut_DrawNothing()
	{
		IRenderedComponent<MokaActionBar> cut = Bar();

		foreach (string part in new[] { "icon", "title", "detail", "actions", "body", "ring" })
		{
			Assert.Empty(cut.FindAll($".moka-action-bar__{part}"));
		}
	}

	[Fact]
	public void TheBody_ComesFirst_AndTheKeyboardCanScrollIt()
	{
		IRenderedComponent<MokaActionBar> cut = Bar(p => p.Add(x => x.Body, b => b.AddContent(0, "B")));

		IElement body = cut.Find(".moka-action-bar").FirstElementChild!;
		Assert.Contains("moka-action-bar__body", body.ClassName, StringComparison.Ordinal);
		Assert.Equal("0", body.GetAttribute("tabindex"));
		Assert.Contains("moka-thin-scrollbar", body.ClassName, StringComparison.Ordinal);
	}

	[Fact]
	public void TheBody_ScrollsPast340Pixels()
	{
		IReadOnlyDictionary<string, string> body = ScopedCss.Declarations(Stylesheet, ".moka-action-bar__body");

		Assert.Equal("21.25rem", body["max-height"]);
		Assert.Equal("auto", body["overflow"]);
	}

	// A transform would make the bar the containing block of every fixed tooltip inside it (gotcha 30).
	[Fact]
	public void NothingInTheStylesheet_UsesATransform()
	{
		string css = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Css", Stylesheet));
		string withoutComments = System.Text.RegularExpressions.Regex.Replace(
			css, @"/\*.*?\*/", string.Empty, System.Text.RegularExpressions.RegexOptions.Singleline);

		Assert.DoesNotContain("transform", withoutComments, StringComparison.Ordinal);
		Assert.DoesNotContain("translate", withoutComments, StringComparison.Ordinal);
	}

	[Fact]
	public void TheDock_CentresTheBarAndLetsClicksThrough()
	{
		IReadOnlyDictionary<string, string> dock = ScopedCss.Declarations(Stylesheet, ".moka-action-bar-dock");

		Assert.Equal("fixed", dock["position"]);
		Assert.Equal("center", dock["justify-content"]);
		Assert.Equal("none", dock["pointer-events"]);
		Assert.Equal("auto", ScopedCss.Declarations(Stylesheet, ".moka-action-bar")["pointer-events"]);
	}

	[Fact]
	public void TheRing_ShowsTheTimeLeft_AndHowMuchOfTheDurationThatIs()
	{
		IRenderedComponent<MokaActionBar> cut = Counting(TimeSpan.FromSeconds(42), TimeSpan.FromSeconds(60));

		IElement ring = cut.Find(".moka-action-bar__ring");
		Assert.Equal("0:42", cut.Find(".moka-action-bar__ring-text").TextContent);
		Assert.Equal("timer", ring.GetAttribute("role"));
		Assert.Equal("Time left: 0:42", ring.GetAttribute("aria-label"));
		Assert.Equal("--_ring-fill: 70%", ring.GetAttribute("style"));
	}

	[Fact]
	public void TheRing_TakesTheIconsPlace()
	{
		IRenderedComponent<MokaActionBar> cut = Bar(p => p
			.Add(x => x.Icon, b => b.AddContent(0, "I"))
			.Add(x => x.Countdown, TimeSpan.FromSeconds(10)));

		Assert.Single(cut.FindAll(".moka-action-bar__ring"));
		Assert.Empty(cut.FindAll(".moka-action-bar__icon"));
	}

	[Fact]
	public void WithoutADuration_TheRingStartsFull()
	{
		IRenderedComponent<MokaActionBar> cut = Counting(TimeSpan.FromSeconds(30));

		Assert.Equal("--_ring-fill: 100%", cut.Find(".moka-action-bar__ring").GetAttribute("style"));
	}

	[Fact]
	public void TheRingsFill_IsWrittenInTheInvariantCulture()
	{
		CultureInfo before = CultureInfo.CurrentCulture;
		CultureInfo.CurrentCulture = new CultureInfo("de-DE");
		try
		{
			IRenderedComponent<MokaActionBar> cut = Counting(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(8));

			Assert.Equal("--_ring-fill: 12.5%", cut.Find(".moka-action-bar__ring").GetAttribute("style"));
		}
		finally
		{
			CultureInfo.CurrentCulture = before;
		}
	}

	[Fact]
	public void CountdownLabel_IsWhatScreenReadersHear()
	{
		IRenderedComponent<MokaActionBar> cut = Bar(p => p
			.Add(x => x.Countdown, TimeSpan.FromSeconds(90))
			.Add(x => x.CountdownLabel, "Undoes itself in"));

		Assert.Equal("Undoes itself in: 1:30", cut.Find(".moka-action-bar__ring").GetAttribute("aria-label"));
	}

	[Fact]
	public void ItCountsDownByItself()
	{
		IRenderedComponent<MokaActionBar> cut = Counting(TimeSpan.FromSeconds(60));

		_clock.Advance(TimeSpan.FromSeconds(1));

		AssertRing(cut, "0:59");
	}

	[Fact]
	public void TheRing_EmptiesAsItCounts()
	{
		IRenderedComponent<MokaActionBar> cut = Counting(TimeSpan.FromSeconds(60));

		_clock.Advance(TimeSpan.FromSeconds(30));

		cut.WaitForAssertion(() =>
			Assert.Equal("--_ring-fill: 50%", cut.Find(".moka-action-bar__ring").GetAttribute("style")), Plenty);
	}

	[Fact]
	public void TheLastFifteenSeconds_LookUrgent()
	{
		IRenderedComponent<MokaActionBar> cut = Counting(TimeSpan.FromSeconds(16));
		Assert.Empty(cut.FindAll(".moka-action-bar__ring--urgent"));

		_clock.Advance(TimeSpan.FromSeconds(2));

		cut.WaitForAssertion(() => Assert.Single(cut.FindAll(".moka-action-bar__ring--urgent")), Plenty);
	}

	[Fact]
	public void ReachingZero_RaisesTheEndOnce_AndStopsTheTimer()
	{
		int ended = 0;
		IRenderedComponent<MokaActionBar> cut = Bar(p => p
			.Add(x => x.Countdown, TimeSpan.FromSeconds(2))
			.Add(x => x.OnCountdownEnd, EventCallback.Factory.Create(this, () => ended++)));

		_clock.Advance(TimeSpan.FromSeconds(5));

		AssertRing(cut, "0:00");
		cut.WaitForAssertion(() => Assert.Equal(1, ended), Plenty);
		cut.WaitForAssertion(() => Assert.True(Assert.Single(_clock.Timers).Disposed), Plenty);
		_clock.Advance(TimeSpan.FromSeconds(5));
		Assert.Equal(1, ended);
	}

	[Fact]
	public void ANewCountdown_StartsFromThatValue()
	{
		IRenderedComponent<MokaActionBar> cut = Counting(TimeSpan.FromSeconds(60));
		_clock.Advance(TimeSpan.FromSeconds(10));
		AssertRing(cut, "0:50");

		cut.Render(p => p.Add(x => x.Countdown, TimeSpan.FromSeconds(30)));

		AssertRing(cut, "0:30");
		_clock.Advance(TimeSpan.FromSeconds(1));
		AssertRing(cut, "0:29");
	}

	[Fact]
	public void ThePassingOfTheSameValue_LeavesTheCountAlone()
	{
		IRenderedComponent<MokaActionBar> cut = Counting(TimeSpan.FromSeconds(60));
		_clock.Advance(TimeSpan.FromSeconds(10));
		AssertRing(cut, "0:50");

		cut.Render(p => p.Add(x => x.Countdown, TimeSpan.FromSeconds(60)));

		AssertRing(cut, "0:50");
	}

	// An app whose deadline lives elsewhere passes a new value every second.
	[Fact]
	public void ACountdownTheParentDrives_KeepsTheRingFilledAgainstTheLongest()
	{
		IRenderedComponent<MokaActionBar> cut = Counting(TimeSpan.FromSeconds(60));

		cut.Render(p => p.Add(x => x.Countdown, TimeSpan.FromSeconds(30)));

		Assert.Equal("--_ring-fill: 50%", cut.Find(".moka-action-bar__ring").GetAttribute("style"));
	}

	[Fact]
	public void ShowingAgain_StartsTheCountAgain()
	{
		IRenderedComponent<MokaActionBar> cut = Counting(TimeSpan.FromSeconds(60));
		_clock.Advance(TimeSpan.FromSeconds(10));
		AssertRing(cut, "0:50");

		cut.Render(p => p.Add(x => x.Visible, false));
		Assert.All(_clock.Timers, timer => Assert.True(timer.Disposed));
		cut.Render(p => p.Add(x => x.Visible, true));

		AssertRing(cut, "1:00");
		Assert.Equal(2, _clock.Timers.Count);
	}

	[Fact]
	public void WithoutACountdown_NoTimerRuns()
	{
		Bar();

		Assert.Empty(_clock.Timers);
	}

	[Fact]
	public void WhileHidden_NoTimerRuns()
	{
		Render<MokaActionBar>(p => p.Add(x => x.Countdown, TimeSpan.FromSeconds(60)));

		Assert.Empty(_clock.Timers);
	}

	[Fact]
	public void TakingTheCountdownAway_StopsTheTimer()
	{
		IRenderedComponent<MokaActionBar> cut = Counting(TimeSpan.FromSeconds(60));

		cut.Render(p => p.Add(x => x.Countdown, (TimeSpan?)null));

		Assert.Empty(cut.FindAll(".moka-action-bar__ring"));
		Assert.True(Assert.Single(_clock.Timers).Disposed);
	}

	[Fact]
	public async Task Disposing_DisposesTheTimer()
	{
		Counting(TimeSpan.FromSeconds(60));

		await DisposeComponentsAsync();

		Assert.True(Assert.Single(_clock.Timers).Disposed);
	}

	// From a timer's thread nothing else could catch it.
	[Fact]
	public void AHandlerThatFailsAtTheEnd_ReachesTheErrorBoundary()
	{
		IRenderedComponent<MokaErrorBoundary> cut = Render<MokaErrorBoundary>(p => p
			.Add(x => x.ChildContent, b =>
			{
				b.OpenComponent<MokaActionBar>(0);
				b.AddComponentParameter(1, nameof(MokaActionBar.Visible), true);
				b.AddComponentParameter(2, nameof(MokaActionBar.Countdown), (TimeSpan?)TimeSpan.FromSeconds(1));
				b.AddComponentParameter(3, nameof(MokaActionBar.OnCountdownEnd), EventCallback.Factory.Create(this,
					() => throw new InvalidOperationException("Could not undo")));
				b.CloseComponent();
			}));

		_clock.Advance(TimeSpan.FromSeconds(1));

		cut.WaitForAssertion(() => Assert.Equal("Could not undo", cut.Find("details pre").TextContent), Plenty);
	}

	[Fact]
	public void TheBar_TakesClassStyleIdAndAttributes()
	{
		IElement bar = Bar(p => p
			.Add(x => x.Class, "mine")
			.Add(x => x.Style, "max-width: 30rem")
			.Add(x => x.Id, "bar")
			.AddUnmatched("data-area", "main")).Find(".moka-action-bar");

		Assert.EndsWith("mine", bar.ClassName, StringComparison.Ordinal);
		Assert.Equal("max-width: 30rem", bar.GetAttribute("style"));
		Assert.Equal("bar", bar.Id);
		Assert.Equal("main", bar.GetAttribute("data-area"));
	}

	// What it says is what a settings form's footer clears. Without it the question before leaving sat under the bar.
	[Fact]
	public void Shown_ItReportsTheRoomItTakes_Once()
	{
		(BunitJSModuleInterop module, _) = SpaceModule();
		IRenderedComponent<MokaActionBar> cut = Bar();

		cut.Render(p => p.Add(x => x.Label, "Changes waiting"));

		Assert.Single(module.Invocations["watchSpace"]);
	}

	[Fact]
	public void Hidden_ItTakesNoRoom()
	{
		(BunitJSModuleInterop module, _) = SpaceModule();

		Render<MokaActionBar>();

		Assert.Empty(module.Invocations["watchSpace"]);
	}

	[Fact]
	public void Hiding_GivesTheRoomBack_AndShowingAgainWatchesTheNewBar()
	{
		(BunitJSModuleInterop module, BunitJSModuleInterop space) = SpaceModule();
		IRenderedComponent<MokaActionBar> cut = Bar();

		cut.Render(p => p.Add(x => x.Visible, false));
		Assert.Single(space.Invocations["dispose"]);
		cut.Render(p => p.Add(x => x.Visible, true));

		Assert.Equal(2, module.Invocations["watchSpace"].Count);
	}

	[Fact]
	public async Task Disposing_GivesTheRoomBack()
	{
		(_, BunitJSModuleInterop space) = SpaceModule();
		IRenderedComponent<MokaActionBar> cut = Bar();

		await cut.Instance.DisposeAsync();

		Assert.Single(space.Invocations["dispose"]);
	}

	private (BunitJSModuleInterop Module, BunitJSModuleInterop Space) SpaceModule()
	{
		BunitJSModuleInterop module = JSInterop.SetupModule("./_content/Moka.Red.Core/moka-space.js");
		BunitJSModuleInterop space = module.SetupModule(invocation => invocation.Identifier == "watchSpace");
		space.Mode = JSRuntimeMode.Loose;
		return (module, space);
	}

	private sealed class ManualClock : TimeProvider
	{
		private DateTimeOffset _now = new(2026, 11, 6, 12, 0, 0, TimeSpan.Zero);

		public List<ManualTimer> Timers { get; } = [];

		public override DateTimeOffset GetUtcNow() => _now;

		public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
		{
			ManualTimer timer = new(callback, state);
			Timers.Add(timer);
			return timer;
		}

		public void Advance(TimeSpan by)
		{
			_now += by;
			foreach (ManualTimer timer in Timers.ToArray())
			{
				timer.Fire();
			}
		}
	}

	private sealed class ManualTimer(TimerCallback callback, object? state) : ITimer
	{
		public bool Disposed { get; private set; }

		public void Fire()
		{
			if (!Disposed)
			{
				callback(state);
			}
		}

		public bool Change(TimeSpan dueTime, TimeSpan period) => true;

		public void Dispose() => Disposed = true;

		public ValueTask DisposeAsync()
		{
			Disposed = true;
			return ValueTask.CompletedTask;
		}
	}
}
