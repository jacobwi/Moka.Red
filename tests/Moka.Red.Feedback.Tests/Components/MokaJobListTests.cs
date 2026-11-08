using System.Globalization;
using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Moka.Red.Feedback.JobList;
using Moka.Red.Feedback.JobProgress;
using Moka.Red.Feedback.Tooltip;
using Moka.Red.Icons;
using Moka.Red.Primitives.StatusDot;
using Moka.Red.Tests.Shared;

namespace Moka.Red.Feedback.Tests.Components;

public class MokaJobListTests : BunitContext
{
	private const string Module = "./_content/Moka.Red.Feedback/JobList/MokaJobList.razor.js";
	private const string Stylesheet = "MokaJobList.razor.css";

	private static readonly MokaJob Video = new("v", "video-final.mp4", MokaJobState.Running)
	{
		Detail = "To Videos",
		Percent = 62.7,
		Bytes = 1_200_000_000,
		TotalBytes = 1_900_000_000,
		BytesPerSecond = 12_000_000,
		TimeRemaining = TimeSpan.FromSeconds(58)
	};

	private static readonly MokaJob Report = new("r", "report.pdf", MokaJobState.Failed) { Error = "Disk full." };

	private static readonly MokaJob Backup = new("b", "backup.zip", MokaJobState.Done) { Detail = "To Archive", TotalBytes = 48_000_000 };

	private readonly BunitJSModuleInterop _module;

	public MokaJobListTests()
	{
		JSInterop.Mode = JSRuntimeMode.Loose;
		_module = JSInterop.SetupModule(Module);
	}

	private IRenderedComponent<MokaJobList> List(params MokaJob[] jobs) =>
		Render<MokaJobList>(p => p.Add(x => x.Jobs, jobs));

	private static string Number(double value) => value.ToString("0.#", CultureInfo.CurrentCulture);

	private static string Meta(IRenderedComponent<MokaJobList> cut) => cut.Find(".moka-job-list__meta").TextContent;

	private static string Live(IRenderedComponent<MokaJobList> cut) => cut.Find("[role=status]").TextContent.Trim();

	private static IReadOnlyList<IElement> Actions(IRenderedComponent<MokaJobList> cut) =>
		cut.FindAll(".moka-job-list__action");

	private static void Update(IRenderedComponent<MokaJobList> cut, params MokaJob[] jobs) =>
		cut.Render(p => p.Add(x => x.Jobs, jobs));

	[Fact]
	public void Root_HasItsClass_AndClassAndAttributesLandThere()
	{
		IRenderedComponent<MokaJobList> cut = Render<MokaJobList>(p => p
			.Add(x => x.Jobs, [Video])
			.Add(x => x.Class, "mine")
			.Add(x => x.Id, "jobs")
			.AddUnmatched("data-test", "yes"));

		IElement root = cut.Find(".moka-job-list");
		Assert.Equal("moka-job-list mine", root.ClassName);
		Assert.Equal("jobs", root.Id);
		Assert.Equal("yes", root.GetAttribute("data-test"));
		Assert.Null(root.GetAttribute("style"));
	}

	[Fact]
	public void EachJob_IsOneItemInANamedList()
	{
		IRenderedComponent<MokaJobList> cut = List(Video, Report, Backup);

		IElement list = cut.Find("ul[role=list]");
		Assert.Equal("Jobs", list.GetAttribute("aria-label"));
		Assert.Equal(["video-final.mp4", "report.pdf", "backup.zip"],
			cut.FindAll("ul > li .moka-job-list__name").Select(name => name.TextContent));
	}

	[Fact]
	public void TheName_KeepsAllOfItInATitle_ForWhenItIsCutOff()
	{
		IElement name = List(Video).Find(".moka-job-list__name");

		Assert.Equal("video-final.mp4", name.GetAttribute("title"));
		Assert.Equal("auto", name.GetAttribute("dir"));
	}

	[Theory]
	[InlineData(MokaJobState.Queued, "Queued")]
	[InlineData(MokaJobState.Running, "Running")]
	[InlineData(MokaJobState.Done, "Done")]
	[InlineData(MokaJobState.Failed, "Failed")]
	[InlineData(MokaJobState.Canceled, "Canceled")]
	public void TheDot_IsNamedForTheState_SoItIsNotColorAlone(MokaJobState state, string word)
	{
		IElement dot = List(new MokaJob("a", "a.zip", state)).Find(".moka-status-dot");

		Assert.Equal("img", dot.GetAttribute("role"));
		Assert.Equal(word, dot.GetAttribute("aria-label"));
	}

	[Theory]
	[InlineData(MokaJobState.Queued, "surface")]
	[InlineData(MokaJobState.Running, "info")]
	[InlineData(MokaJobState.Done, "success")]
	[InlineData(MokaJobState.Failed, "error")]
	[InlineData(MokaJobState.Canceled, "surface")]
	public void TheDot_TakesTheStatesColor(MokaJobState state, string color)
	{
		IElement dot = List(new MokaJob("a", "a.zip", state)).Find(".moka-status-dot");

		Assert.Contains($"moka-status-dot--{color}", dot.ClassList);
	}

	[Fact]
	public void ARunningJob_ReadsLikeTheBoard()
	{
		Assert.Equal($"To Videos, {Number(1.2)} of {Number(1.9)} GB, 12 MB/s, 58 s left", Meta(List(Video)));
	}

	[Fact]
	public void DifferentUnits_AreEachSaid()
	{
		MokaJob job = new("a", "a.zip", MokaJobState.Running) { Bytes = 900_000, TotalBytes = 1_200_000_000 };

		Assert.Equal($"900 KB of {Number(1.2)} GB", Meta(List(job)));
	}

	[Fact]
	public void WithNoAmount_TheWordsSayThePercent()
	{
		Assert.Equal("To Print, 40%", Meta(List(new MokaJob("a", "a", MokaJobState.Running) { Detail = "To Print", Percent = 40.9 })));
	}

	[Fact]
	public void ARunningJobWithNothingToSay_HasNoSecondLine()
	{
		Assert.Empty(List(new MokaJob("a", "a.zip", MokaJobState.Running)).FindAll(".moka-job-list__meta"));
	}

	[Theory]
	[InlineData(MokaJobState.Queued, "To Archive, 48 MB, Queued")]
	[InlineData(MokaJobState.Done, "To Archive, 48 MB, Done")]
	public void AStillJob_ShowsItsSizeAndSaysWhatHappened(MokaJobState state, string expected)
	{
		Assert.Equal(expected, Meta(List(Backup with { State = state })));
	}

	[Fact]
	public void ACanceledJob_ShowsHowFarItGot()
	{
		MokaJob job = new("a", "a.zip", MokaJobState.Canceled) { Bytes = 12_000_000, TotalBytes = 48_000_000 };

		Assert.Equal("12 of 48 MB, Canceled", Meta(List(job)));
	}

	[Fact]
	public void ASpeedAndTimeLeft_DoNotOutliveTheRun()
	{
		string meta = Meta(List(Video with { State = MokaJobState.Done }));

		Assert.DoesNotContain("MB/s", meta, StringComparison.Ordinal);
		Assert.DoesNotContain("left", meta, StringComparison.Ordinal);
	}

	[Fact]
	public void AFailedJob_ExplainsItself_InPlaceOfTheNumbers()
	{
		IRenderedComponent<MokaJobList> cut = List(Report with { Detail = "To Print", Bytes = 10 });

		Assert.Equal("Disk full.", cut.Find(".moka-job-list__error").TextContent);
		Assert.Empty(cut.FindAll(".moka-job-list__meta"));
	}

	[Theory]
	[InlineData(null)]
	[InlineData("  ")]
	public void AFailedJobWithNoReason_StillSaysSo(string? error)
	{
		Assert.Equal("It didn't finish.", List(Report with { Error = error }).Find(".moka-job-list__error").TextContent);
	}

	[Fact]
	public void TheOptionalIcon_ShowsBeforeTheName()
	{
		IRenderedComponent<MokaJobList> cut = List(Video with { Icon = MokaIcons.Action.Upload }, Backup);

		Assert.Single(cut.FindAll(".moka-job-list__icon svg"));
		Assert.Equal("moka-job-list__icon", cut.Find(".moka-job-list__name").PreviousElementSibling!.ClassName);
	}

	[Theory]
	[InlineData(MokaJobState.Queued, true, false, true)]
	[InlineData(MokaJobState.Running, true, false, false)]
	[InlineData(MokaJobState.Done, false, false, true)]
	[InlineData(MokaJobState.Failed, false, true, true)]
	[InlineData(MokaJobState.Canceled, false, true, true)]
	public void TheState_SaysWhichActionsShow(MokaJobState state, bool cancel, bool retry, bool remove)
	{
		IRenderedComponent<MokaJobList> cut = Render<MokaJobList>(p => p
			.Add(x => x.Jobs, [new MokaJob("a", "a.zip", state)])
			.Add(x => x.OnCancel, (MokaJob _) => { })
			.Add(x => x.OnRetry, (MokaJob _) => { })
			.Add(x => x.OnRemove, (MokaJob _) => { }));

		string[] names = [.. Actions(cut).Select(button => button.GetAttribute("aria-label")!)];
		Assert.Equal(cancel, names.Contains("Cancel a.zip"));
		Assert.Equal(retry, names.Contains("Retry a.zip"));
		Assert.Equal(remove, names.Contains("Remove a.zip"));
	}

	[Fact]
	public void WithNoCallbacks_ThereAreNoActions()
	{
		IRenderedComponent<MokaJobList> cut = List(Video, Report, Backup);

		Assert.Empty(Actions(cut));
		Assert.Empty(cut.FindAll(".moka-job-list__actions"));
	}

	[Fact]
	public void AMissingCallback_HidesOnlyItsAction()
	{
		IRenderedComponent<MokaJobList> cut = Render<MokaJobList>(p => p
			.Add(x => x.Jobs, [Report])
			.Add(x => x.OnRemove, (MokaJob _) => { }));

		Assert.Equal("Remove report.pdf", Assert.Single(Actions(cut)).GetAttribute("aria-label"));
	}

	[Theory]
	[InlineData(MokaJobState.Running, "Cancel a.zip", "cancel")]
	[InlineData(MokaJobState.Failed, "Retry a.zip", "retry")]
	[InlineData(MokaJobState.Done, "Remove a.zip", "remove")]
	public void AnAction_RaisesItsCallbackWithTheJob(MokaJobState state, string button, string expected)
	{
		MokaJob job = new("a", "a.zip", state);
		List<(string What, MokaJob Job)> raised = [];
		IRenderedComponent<MokaJobList> cut = Render<MokaJobList>(p => p
			.Add(x => x.Jobs, [job])
			.Add(x => x.OnCancel, (MokaJob j) => raised.Add(("cancel", j)))
			.Add(x => x.OnRetry, (MokaJob j) => raised.Add(("retry", j)))
			.Add(x => x.OnRemove, (MokaJob j) => raised.Add(("remove", j))));

		cut.Find($".moka-job-list__action[aria-label='{button}']").Click();

		(string what, MokaJob sent) = Assert.Single(raised);
		Assert.Equal(expected, what);
		Assert.Equal(job, sent);
	}

	[Fact]
	public void TheSecondClickOfADoubleClick_IsIgnored_SinceTheButtonMayHaveBecomeAnother()
	{
		int raised = 0;
		IRenderedComponent<MokaJobList> cut = Render<MokaJobList>(p => p
			.Add(x => x.Jobs, [Video])
			.Add(x => x.OnCancel, (MokaJob _) => raised++));

		cut.Find(".moka-job-list__action").Click(new MouseEventArgs { Detail = 1 });
		cut.Find(".moka-job-list__action").Click(new MouseEventArgs { Detail = 2 });

		Assert.Equal(1, raised);
	}

	[Fact]
	public void TheKeyboard_CanStillPress_SinceItReportsNoClickCount()
	{
		int raised = 0;
		IRenderedComponent<MokaJobList> cut = Render<MokaJobList>(p => p
			.Add(x => x.Jobs, [Video])
			.Add(x => x.OnCancel, (MokaJob _) => raised++));

		cut.Find(".moka-job-list__action").Click(new MouseEventArgs { Detail = 0 });

		Assert.Equal(1, raised);
	}

	[Fact]
	public void IconButtons_AreNamedForTheirJob_AndCarryThatInATooltip()
	{
		IRenderedComponent<MokaJobList> cut = Render<MokaJobList>(p => p
			.Add(x => x.Jobs, [Video, Report])
			.Add(x => x.OnCancel, (MokaJob _) => { })
			.Add(x => x.OnRetry, (MokaJob _) => { })
			.Add(x => x.OnRemove, (MokaJob _) => { }));

		IconTooltip.AssertNamed(cut.Find("[aria-label='Cancel video-final.mp4']"), "Cancel video-final.mp4", MokaTooltipPosition.BottomEnd);
		IconTooltip.AssertNamed(cut.Find("[aria-label='Retry report.pdf']"), "Retry report.pdf", MokaTooltipPosition.BottomEnd);
		IconTooltip.AssertNamed(cut.Find("[aria-label='Remove report.pdf']"), "Remove report.pdf", MokaTooltipPosition.BottomEnd);
	}

	[Fact]
	public void CancelAndRetry_ShareTheLastPlace_SoFocusStaysOnThatButton()
	{
		IRenderedComponent<MokaJobList> cut = Render<MokaJobList>(p => p
			.Add(x => x.Jobs, [Report])
			.Add(x => x.OnCancel, (MokaJob _) => { })
			.Add(x => x.OnRetry, (MokaJob _) => { })
			.Add(x => x.OnRemove, (MokaJob _) => { }));

		Assert.Equal(["Remove report.pdf", "Retry report.pdf"], Actions(cut).Select(button => button.GetAttribute("aria-label")));
	}

	[Fact]
	public void Disabled_DisablesEveryAction()
	{
		IRenderedComponent<MokaJobList> cut = Render<MokaJobList>(p => p
			.Add(x => x.Jobs, [Report])
			.Add(x => x.Disabled, true)
			.Add(x => x.OnRetry, (MokaJob _) => { })
			.Add(x => x.OnRemove, (MokaJob _) => { })
			.Add(x => x.OnClearFinished, () => { }));

		Assert.All(cut.FindAll("button"), button => Assert.True(button.HasAttribute("disabled")));
	}

	[Fact]
	public void TheHeader_SummarizesWithNoEmptyCounts()
	{
		IRenderedComponent<MokaJobList> cut = List(Video, Video with { Id = "w" }, Report);

		Assert.Equal("2 running, 1 failed", cut.Find(".moka-job-list__summary").TextContent);
	}

	[Fact]
	public void TheSummary_NamesEveryState()
	{
		IRenderedComponent<MokaJobList> cut = List(
			new MokaJob("q", "q", MokaJobState.Queued),
			new MokaJob("r", "r", MokaJobState.Running),
			new MokaJob("f", "f", MokaJobState.Failed),
			new MokaJob("c", "c", MokaJobState.Canceled),
			new MokaJob("d", "d", MokaJobState.Done));

		Assert.Equal("1 running, 1 queued, 1 failed, 1 canceled, 1 done", cut.Find(".moka-job-list__summary").TextContent);
	}

	[Fact]
	public void ClearFinished_RaisesItsCallback()
	{
		int raised = 0;
		IRenderedComponent<MokaJobList> cut = Render<MokaJobList>(p => p
			.Add(x => x.Jobs, [Backup])
			.Add(x => x.OnClearFinished, () => raised++));

		IElement button = cut.Find(".moka-job-list__header button");
		Assert.Equal("Clear finished", button.TextContent.Trim());
		button.Click();

		Assert.Equal(1, raised);
	}

	[Fact]
	public void ClearFinished_StaysPut_ButWaitsForSomethingToClear()
	{
		IRenderedComponent<MokaJobList> cut = Render<MokaJobList>(p => p
			.Add(x => x.Jobs, [Video])
			.Add(x => x.OnClearFinished, () => { }));

		Assert.True(cut.Find(".moka-job-list__header button").HasAttribute("disabled"));

		Update(cut, Video, Report);

		Assert.False(cut.Find(".moka-job-list__header button").HasAttribute("disabled"));
	}

	[Fact]
	public void WithNoCallback_ThereIsNoClearFinished()
	{
		Assert.Empty(List(Backup).FindAll(".moka-job-list__header button"));
	}

	[Fact]
	public void ShowHeaderOff_LeavesOnlyTheRows()
	{
		IRenderedComponent<MokaJobList> cut = Render<MokaJobList>(p => p
			.Add(x => x.Jobs, [Video])
			.Add(x => x.ShowHeader, false)
			.Add(x => x.OnClearFinished, () => { }));

		Assert.Empty(cut.FindAll(".moka-job-list__header"));
		Assert.Single(cut.FindAll("li"));
	}

	[Fact]
	public void MaxVisible_KeepsTheFirstJobs_AndOffersTheRest()
	{
		IRenderedComponent<MokaJobList> cut = Render<MokaJobList>(p => p
			.Add(x => x.Jobs, Many(5))
			.Add(x => x.MaxVisible, 2));

		Assert.Equal(["job0", "job1"], cut.FindAll(".moka-job-list__name").Select(name => name.TextContent));
		IElement button = cut.Find(".moka-job-list__more button");
		Assert.Equal("Show 3 more", button.TextContent.Trim());
		Assert.Equal("false", button.GetAttribute("aria-expanded"));
	}

	[Fact]
	public void ShowingTheRest_CanBeTakenBack()
	{
		IRenderedComponent<MokaJobList> cut = Render<MokaJobList>(p => p
			.Add(x => x.Jobs, Many(5))
			.Add(x => x.MaxVisible, 2));

		cut.Find(".moka-job-list__more button").Click();

		Assert.Equal(5, cut.FindAll("li").Count);
		Assert.Equal("Show less", cut.Find(".moka-job-list__more button").TextContent.Trim());
		Assert.Equal("true", cut.Find(".moka-job-list__more button").GetAttribute("aria-expanded"));

		cut.Find(".moka-job-list__more button").Click();

		Assert.Equal(2, cut.FindAll("li").Count);
	}

	[Theory]
	[InlineData(0)]
	[InlineData(-1)]
	[InlineData(5)]
	[InlineData(9)]
	public void MaxVisible_ThatCutsNothing_OffersNothing(int max)
	{
		IRenderedComponent<MokaJobList> cut = Render<MokaJobList>(p => p
			.Add(x => x.Jobs, Many(5))
			.Add(x => x.MaxVisible, (int?)max));

		Assert.Equal(5, cut.FindAll("li").Count);
		Assert.Empty(cut.FindAll(".moka-job-list__more"));
	}

	[Fact]
	public void WithNoJobs_TheEmptyStateShows_AndNoHeader()
	{
		IRenderedComponent<MokaJobList> cut = Render<MokaJobList>(p => p.Add(x => x.OnClearFinished, () => { }));

		Assert.Equal("No jobs", cut.Find(".moka-empty-state__title").TextContent);
		Assert.Empty(cut.FindAll(".moka-job-list__header"));
		Assert.Empty(cut.FindAll("ul"));
	}

	[Fact]
	public void TheEmptyState_CanBeTheAppsOwn()
	{
		IRenderedComponent<MokaJobList> cut = Render<MokaJobList>(p => p
			.Add(x => x.Jobs, [])
			.Add(x => x.EmptyContent, "<p class=\"mine\">Nothing yet</p>"));

		Assert.Equal("Nothing yet", cut.Find(".mine").TextContent);
		Assert.Empty(cut.FindAll(".moka-empty-state"));
	}

	[Fact]
	public void Slots_GetTheJob_AndSitBeforeTheNameAndTheActions()
	{
		IRenderedComponent<MokaJobList> cut = Render<MokaJobList>(p => p
			.Add(x => x.Jobs, [Video])
			.Add(x => x.OnCancel, (MokaJob _) => { })
			.Add<MokaJob>(x => x.StartContent, job => $"<img alt=\"{job.Id}\" />")
			.Add<MokaJob>(x => x.EndContent, job => $"<b>{job.Id}</b>"));

		Assert.Equal("v", cut.Find(".moka-job-list__start img").GetAttribute("alt"));
		Assert.Equal("v", cut.Find(".moka-job-list__end b").TextContent);
		Assert.Equal("moka-job-list__actions", cut.Find(".moka-job-list__end").NextElementSibling!.ClassName);
		Assert.Equal("moka-job-list__main", cut.Find(".moka-job-list__start").NextElementSibling!.ClassName);
	}

	[Fact]
	public void TheBar_IsAProgressbarNamedForTheJob()
	{
		IElement bar = List(Video).Find("[role=progressbar]");

		Assert.Equal("video-final.mp4", bar.GetAttribute("aria-label"));
		Assert.Equal("0", bar.GetAttribute("aria-valuemin"));
		Assert.Equal("100", bar.GetAttribute("aria-valuemax"));
		Assert.Equal("62", bar.GetAttribute("aria-valuenow"));
		Assert.Equal($"62%, {Number(1.2)} of {Number(1.9)} GB", bar.GetAttribute("aria-valuetext"));
	}

	[Fact]
	public void TheBar_ShowsOnlyWhileRunning()
	{
		Assert.Single(List(Video, Report, Backup, Video with { Id = "w", State = MokaJobState.Queued }).FindAll("[role=progressbar]"));
	}

	[Theory]
	[InlineData(62.5, "0.625")]
	[InlineData(0d, "0")]
	[InlineData(100d, "1")]
	[InlineData(250d, "1")]
	[InlineData(-5d, "0")]
	public void TheFill_IsAScale(double percent, string scale)
	{
		IElement fill = List(new MokaJob("a", "a", MokaJobState.Running) { Percent = percent }).Find(".moka-job-list__fill");

		Assert.Equal($"transform: scaleX({scale})", fill.GetAttribute("style"));
	}

	[Fact]
	public void TheFill_WritesItsScaleTheSameInEveryCulture()
	{
		CultureInfo before = CultureInfo.CurrentCulture;
		CultureInfo.CurrentCulture = new CultureInfo("sv-SE");
		try
		{
			IElement fill = List(new MokaJob("a", "a", MokaJobState.Running) { Percent = 62.5 }).Find(".moka-job-list__fill");

			Assert.Equal("transform: scaleX(0.625)", fill.GetAttribute("style"));
		}
		finally
		{
			CultureInfo.CurrentCulture = before;
		}
	}

	[Fact]
	public void TheFill_MovesWithoutLayout_AndCannotTrailTheNumbers()
	{
		IReadOnlyDictionary<string, string> fill = ScopedCss.Declarations(Stylesheet, ".moka-job-list__fill");

		Assert.Equal("transform var(--moka-transition-fast)", fill["transition"]);
		Assert.DoesNotContain("width", fill.Keys);
	}

	[Fact]
	public void BytesAndATotal_GiveThePercent_WhenTheAppGivesNone()
	{
		IRenderedComponent<MokaJobList> cut = List(new MokaJob("a", "a", MokaJobState.Running) { Bytes = 50, TotalBytes = 200 });

		Assert.Equal("25", cut.Find("[role=progressbar]").GetAttribute("aria-valuenow"));
		Assert.DoesNotContain("moka-job-list__track--waiting", cut.Find(".moka-job-list__track").ClassList);
	}

	[Fact]
	public void WithNoNumberYet_TheBarSlides_AndSaysNoValue()
	{
		IRenderedComponent<MokaJobList> cut = List(new MokaJob("a", "a", MokaJobState.Running) { Bytes = 5_000_000 });

		IElement bar = cut.Find("[role=progressbar]");
		Assert.Contains("moka-job-list__track--waiting", bar.ClassList);
		Assert.False(bar.HasAttribute("aria-valuenow"));
		Assert.Equal("5 MB", bar.GetAttribute("aria-valuetext"));
		Assert.False(cut.Find(".moka-job-list__fill").HasAttribute("style"));
	}

	[Fact]
	public void ThePercentNeverReads100_BeforeTheEnd()
	{
		Assert.Equal("99", List(new MokaJob("a", "a", MokaJobState.Running) { Percent = 99.9 }).Find("[role=progressbar]").GetAttribute("aria-valuenow"));
	}

	[Fact]
	public void BinaryUnits_ReachTheAmountAndTheSpeed()
	{
		IRenderedComponent<MokaJobList> cut = Render<MokaJobList>(p => p
			.Add(x => x.ByteUnits, MokaByteUnits.Binary)
			.Add(x => x.Jobs, [new MokaJob("a", "a", MokaJobState.Running) { Bytes = 1_048_576, TotalBytes = 2_097_152, BytesPerSecond = 1024 }]));

		Assert.Equal("1 of 2 MiB, 1 KiB/s", Meta(cut));
	}

	[Fact]
	public void AByteFormatter_WinsOverTheUnits()
	{
		IRenderedComponent<MokaJobList> cut = Render<MokaJobList>(p => p
			.Add(x => x.ByteUnits, MokaByteUnits.Binary)
			.Add(x => x.ByteFormatter, bytes => $"{bytes:0} b")
			.Add(x => x.Jobs, [new MokaJob("a", "a", MokaJobState.Running) { Bytes = 5, TotalBytes = 10, BytesPerSecond = 2 }]));

		Assert.Equal("5 b of 10 b, 2 b/s", Meta(cut));
	}

	[Theory]
	[InlineData(0.2, "1 s left")]
	[InlineData(42, "42 s left")]
	[InlineData(60, "1 min left")]
	[InlineData(61, "2 min left")]
	[InlineData(3540, "59 min left")]
	[InlineData(3600, "1 h left")]
	[InlineData(3720, "1 h 2 min left")]
	public void TheTimeLeft_ReadsInTheLargestUnitThatHelps(double seconds, string expected)
	{
		Assert.Equal(expected, new MokaJobListText().TimeLeft(TimeSpan.FromSeconds(seconds)));
	}

	[Fact]
	public void NoTimeLeft_SaysNothing()
	{
		Assert.DoesNotContain("left", Meta(List(Video with { TimeRemaining = TimeSpan.Zero })), StringComparison.Ordinal);
	}

	[Fact]
	public void EveryWord_CanBeReplaced()
	{
		MokaJobListText text = new()
		{
			ListLabel = "Transfers",
			Queued = "Wartet",
			Running = "Läuft",
			Done = "Fertig",
			Failed = "Fehler",
			Canceled = "Abgebrochen",
			NoReason = "Unbekannt",
			ClearFinished = "Aufräumen",
			Empty = "Leer",
			EmptyHint = "Noch nichts",
			ShowLess = "Weniger",
			Separator = " / ",
			Cancel = name => $"{name} abbrechen",
			Retry = name => $"{name} wiederholen",
			Remove = name => $"{name} entfernen",
			ShowMore = hidden => $"Noch {hidden}",
			Summary = counts => $"{counts.Running} aktiv",
			Amount = (done, whole) => $"{done} von {whole}",
			TimeLeft = left => $"noch {left.TotalSeconds:0}",
			FailureAnnouncement = job => $"{job.Name} kaputt"
		};
		MokaJob queued = new("q", "q.zip", MokaJobState.Queued);
		MokaJob done = new("d", "d.zip", MokaJobState.Done);
		MokaJob canceled = new("c", "c.zip", MokaJobState.Canceled);
		MokaJob failed = new("f", "f.zip", MokaJobState.Failed);
		IRenderedComponent<MokaJobList> cut = Render<MokaJobList>(p => p
			.Add(x => x.Jobs, [Video, queued, done, canceled, failed])
			.Add(x => x.Text, text)
			.Add(x => x.MaxVisible, 4)
			.Add(x => x.OnCancel, (MokaJob _) => { })
			.Add(x => x.OnRetry, (MokaJob _) => { })
			.Add(x => x.OnRemove, (MokaJob _) => { })
			.Add(x => x.OnClearFinished, () => { }));

		Assert.Equal("Transfers", cut.Find("ul").GetAttribute("aria-label"));
		Assert.Equal("1 aktiv", cut.Find(".moka-job-list__summary").TextContent);
		Assert.Equal("Aufräumen", cut.Find(".moka-job-list__header button").TextContent.Trim());
		Assert.Equal("Noch 1", cut.Find(".moka-job-list__more button").TextContent.Trim());
		Assert.Contains($"{Number(1.2)} von {Number(1.9)} GB / 12 MB/s / noch 58", cut.Markup, StringComparison.Ordinal);
		Assert.Contains("Wartet", cut.Markup, StringComparison.Ordinal);
		Assert.Contains("Fertig", cut.Markup, StringComparison.Ordinal);
		Assert.Contains("Abgebrochen", cut.Markup, StringComparison.Ordinal);
		Assert.Contains("aria-label=\"Läuft\"", cut.Markup, StringComparison.Ordinal);
		Assert.NotNull(cut.Find("[aria-label='video-final.mp4 abbrechen']"));
		Assert.NotNull(cut.Find("[aria-label='c.zip wiederholen']"));
		Assert.NotNull(cut.Find("[aria-label='c.zip entfernen']"));

		cut.Find(".moka-job-list__more button").Click();
		Assert.Equal("Weniger", cut.Find(".moka-job-list__more button").TextContent.Trim());
		Assert.Equal("Unbekannt", cut.Find(".moka-job-list__error").TextContent);
		Assert.Equal("Fehler", cut.Find(".moka-job-list__item--failed .moka-status-dot").GetAttribute("aria-label"));

		Update(cut, failed with { Id = "g", Name = "g.zip" });
		Assert.Equal("g.zip kaputt", Live(cut));

		Update(cut);
		Assert.Equal("Leer", cut.Find(".moka-empty-state__title").TextContent);
		Assert.Equal("Noch nichts", cut.Find(".moka-empty-state__description").TextContent);
	}

	[Fact]
	public void TheStatusRegion_IsThereBeforeAnythingHappens()
	{
		IElement region = Assert.Single(List(Video).FindAll("[role=status], [aria-live]"));

		Assert.Empty(region.TextContent.Trim());
	}

	[Fact]
	public void ANewPercent_IsNotAnnounced()
	{
		IRenderedComponent<MokaJobList> cut = List(Video);

		Update(cut, Video with { Percent = 70, Bytes = 1_300_000_000 });

		Assert.Empty(Live(cut));
	}

	[Fact]
	public void AJobThatFails_IsAnnouncedOnce_WithTheReason()
	{
		IRenderedComponent<MokaJobList> cut = List(Video);

		Update(cut, Video with { State = MokaJobState.Failed, Error = "Disk full." });

		Assert.Equal("video-final.mp4 failed: Disk full.", Live(cut));
		Assert.Single(cut.FindAll("[role=status] > div > div"));

		Update(cut, Video with { State = MokaJobState.Failed, Error = "Disk full." }, Backup);

		Assert.Single(cut.FindAll("[role=status] > div > div"));
	}

	[Fact]
	public void AJobThatFailsWithNoReason_IsStillAnnounced()
	{
		IRenderedComponent<MokaJobList> cut = List(Video);

		Update(cut, Video with { State = MokaJobState.Failed });

		Assert.Equal("video-final.mp4 failed", Live(cut));
	}

	[Fact]
	public void JobsAlreadyFailedOnArrival_AreNotNews()
	{
		Assert.Empty(Live(List(Report)));
	}

	[Fact]
	public void AJobAddedAlreadyFailed_IsNews()
	{
		IRenderedComponent<MokaJobList> cut = List(Video);

		Update(cut, Video, Report);

		Assert.Equal("report.pdf failed: Disk full.", Live(cut));
	}

	[Fact]
	public void SeveralFailuresAtOnce_AreEachSaid()
	{
		IRenderedComponent<MokaJobList> cut = List(Video, Backup with { State = MokaJobState.Running });

		Update(cut, Video with { State = MokaJobState.Failed, Error = "A" }, Backup with { State = MokaJobState.Failed, Error = "B" });

		Assert.Equal(["video-final.mp4 failed: A", "backup.zip failed: B"],
			cut.FindAll("[role=status] > div > div").Select(line => line.TextContent));
	}

	[Fact]
	public void AJobThatFailsAgain_IsAnnouncedAgain()
	{
		int announced = 0;
		IRenderedComponent<MokaJobList> cut = Render<MokaJobList>(p => p
			.Add(x => x.Jobs, [Report with { State = MokaJobState.Running }])
			.Add(x => x.Text, new MokaJobListText { FailureAnnouncement = job => $"{job.Name} {++announced}" }));

		Update(cut, Report);
		Update(cut, Report);
		Assert.Equal("report.pdf 1", Live(cut));

		Update(cut, Report with { State = MokaJobState.Queued });
		Update(cut, Report);

		Assert.Equal("report.pdf 2", Live(cut));
	}

	[Fact]
	public void AFailureOutOfView_IsStillHeard()
	{
		IRenderedComponent<MokaJobList> cut = Render<MokaJobList>(p => p
			.Add(x => x.Jobs, [Video, Backup with { State = MokaJobState.Running }])
			.Add(x => x.MaxVisible, 1));

		Update(cut, Video, Backup with { State = MokaJobState.Failed, Error = "Gone" });

		Assert.Equal("backup.zip failed: Gone", Live(cut));
	}

	[Fact]
	public void TheFailureLine_IsNotItsOwnLiveRegion()
	{
		Assert.Single(List(Report).FindAll("[role=status], [role=alert], [aria-live]"));
	}

	[Fact]
	public void ATickOnOneJob_RedrawsOnlyThatRow()
	{
		int formats = 0;
		Func<double, string> counting = bytes =>
		{
			formats++;
			return "x";
		};
		MokaJob first = new("a", "a", MokaJobState.Running) { Bytes = 1, TotalBytes = 10 };
		MokaJob second = new("b", "b", MokaJobState.Running) { Bytes = 2, TotalBytes = 10 };
		IRenderedComponent<MokaJobList> cut = Render<MokaJobList>(p => p
			.Add(x => x.Jobs, [first, second])
			.Add(x => x.ByteFormatter, counting));
		int perRow = formats / 2;

		formats = 0;
		cut.Render(p => p.Add(x => x.Jobs, [first, second with { Bytes = 3 }]));

		Assert.Equal(perRow, formats);
	}

	[Fact]
	public void ANewListOfEqualJobs_RedrawsNoRow()
	{
		int formats = 0;
		Func<double, string> counting = bytes =>
		{
			formats++;
			return "x";
		};
		MokaJob job = new("a", "a", MokaJobState.Running) { Bytes = 1, TotalBytes = 10 };
		IRenderedComponent<MokaJobList> cut = Render<MokaJobList>(p => p
			.Add(x => x.Jobs, [job])
			.Add(x => x.ByteFormatter, counting));

		formats = 0;
		cut.Render(p => p.Add(x => x.Jobs, [job with { }]));

		Assert.Equal(0, formats);
	}

	[Fact]
	public void ANewSetting_RedrawsEveryRow()
	{
		IRenderedComponent<MokaJobList> cut = List(new MokaJob("a", "a", MokaJobState.Queued), new MokaJob("b", "b", MokaJobState.Done));

		cut.Render(p => p.Add(x => x.Text, new MokaJobListText { Queued = "Waiting", Done = "Finished" }));

		Assert.Equal("Waiting", cut.FindAll(".moka-job-list__meta")[0].TextContent);
		Assert.Equal("Finished", cut.FindAll(".moka-job-list__meta")[1].TextContent);
	}

	[Fact]
	public void GainingACallback_DrawsItsActionOnRowsThatDidNotChange()
	{
		IRenderedComponent<MokaJobList> cut = List(Backup);
		Assert.Empty(Actions(cut));

		cut.Render(p => p.Add(x => x.OnRemove, (MokaJob _) => { }));

		Assert.Single(Actions(cut));
	}

	[Fact]
	public void ARowWithASlot_RedrawsWithTheList_SinceTheSlotCanReadAnything()
	{
		int drawn = 0;
		IRenderedComponent<MokaJobList> cut = Render<MokaJobList>(p => p
			.Add(x => x.Jobs, [Backup])
			.Add<MokaJob>(x => x.EndContent, job => $"<b>{++drawn}</b>"));

		cut.Render(p => p.Add(x => x.Jobs, [Backup]));

		Assert.Equal("2", cut.Find(".moka-job-list__end b").TextContent);
	}

	[Fact]
	public void LosingASlot_DrawsTheRowWithoutIt()
	{
		IRenderedComponent<MokaJobList> cut = Render<MokaJobList>(p => p
			.Add(x => x.Jobs, [Backup])
			.Add<MokaJob>(x => x.EndContent, job => "<b>badge</b>"));

		cut.Render(p => p.Add(x => x.EndContent, (RenderFragment<MokaJob>?)null));

		Assert.Empty(cut.FindAll(".moka-job-list__end"));
	}

	[Fact]
	public void Rows_KeepTheirComponentsAcrossAReorder()
	{
		IRenderedComponent<MokaJobList> cut = List(Video, Backup);
		MokaStatusDot videoDot = cut.FindComponents<MokaStatusDot>()[0].Instance;
		MokaStatusDot backupDot = cut.FindComponents<MokaStatusDot>()[1].Instance;

		Update(cut, Backup, Video);

		IReadOnlyList<IRenderedComponent<MokaStatusDot>> dots = cut.FindComponents<MokaStatusDot>();
		Assert.Same(backupDot, dots[0].Instance);
		Assert.Same(videoDot, dots[1].Instance);
	}

	[Fact]
	public void RepeatedIds_DrawInsteadOfThrowing()
	{
		IRenderedComponent<MokaJobList> cut = List(Video, Video with { Name = "again.mp4" });

		Assert.Equal(2, cut.FindAll("li").Count);
	}

	[Fact]
	public void RemovingARow_PutsFocusBackInTheList()
	{
		IRenderedComponent<MokaJobList> cut = Render<MokaJobList>(p => p
			.Add(x => x.Jobs, [Report, Backup])
			.Add(x => x.OnRemove, (MokaJob _) => { }));

		cut.Find("[aria-label='Remove report.pdf']").Click();
		_module.VerifyNotInvoke("restoreFocus");

		Update(cut, Backup);

		JSRuntimeInvocation call = _module.VerifyInvoke("restoreFocus");
		Assert.IsType<ElementReference>(call.Arguments[0]);
		Assert.Equal(0, call.Arguments[1]);
	}

	[Fact]
	public void AButtonThatGoes_WithItsJobStillThere_PutsFocusOnThatRow()
	{
		IRenderedComponent<MokaJobList> cut = Render<MokaJobList>(p => p
			.Add(x => x.Jobs, [Backup, Video])
			.Add(x => x.OnCancel, (MokaJob _) => { }));

		cut.Find("[aria-label='Cancel video-final.mp4']").Click();
		Update(cut, Backup, Video with { State = MokaJobState.Canceled });

		Assert.Equal(1, _module.VerifyInvoke("restoreFocus").Arguments[1]);
	}

	[Fact]
	public void NothingPressed_MovesNoFocus()
	{
		IRenderedComponent<MokaJobList> cut = List(Report, Backup);

		Update(cut, Backup);

		_module.VerifyNotInvoke("restoreFocus");
	}

	private static MokaJob[] Many(int count) =>
		[.. Enumerable.Range(0, count).Select(i => new MokaJob($"id{i}", $"job{i}", MokaJobState.Queued))];
}
