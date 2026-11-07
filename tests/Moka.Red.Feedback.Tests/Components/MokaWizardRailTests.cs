using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Moka.Red.Feedback.Progress;
using Moka.Red.Feedback.Wizard;
using Moka.Red.Tests.Shared;

namespace Moka.Red.Feedback.Tests.Components;

public class MokaWizardRailTests : BunitContext
{
	private const string Stylesheet = "MokaWizard.razor.css";

	public MokaWizardRailTests() => JSInterop.Mode = JSRuntimeMode.Loose;

	private IRenderedComponent<MokaWizard> RenderRail(
		Action<ComponentParameterCollectionBuilder<MokaWizard>>? parameters = null,
		string secondDecided = "Choosing a port",
		bool secondProblem = false,
		bool secondValid = true) => Render<MokaWizard>(p =>
	{
		p.Add(x => x.Rail, true);
		parameters?.Invoke(p);
		p.Add(x => x.ChildContent, StepContent(secondDecided, secondProblem, secondValid));
	});

	// One fragment rather than AddChildContent per step: bUnit's re-render takes a parameter only once.
	private static RenderFragment StepContent(string secondDecided, bool secondProblem, bool secondValid) => builder =>
	{
		AddStep(builder, 0, "Account", "BC, owner", false, true, "One");
		AddStep(builder, 10, "Internet", secondDecided, secondProblem, secondValid, "Two");
		AddStep(builder, 20, "Wi-Fi", null, false, true, "Three");
	};

	private static void AddStep(RenderTreeBuilder builder, int sequence, string title, string? decided, bool problem, bool valid, string content)
	{
		builder.OpenComponent<MokaWizardStep>(sequence);
		builder.AddComponentParameter(sequence + 1, nameof(MokaWizardStep.Title), title);
		builder.AddComponentParameter(sequence + 2, nameof(MokaWizardStep.Decided), decided);
		builder.AddComponentParameter(sequence + 3, nameof(MokaWizardStep.HasProblem), problem);
		builder.AddComponentParameter(sequence + 4, nameof(MokaWizardStep.IsValid), valid);
		builder.AddComponentParameter(sequence + 5, nameof(MokaWizardStep.ChildContent), (RenderFragment)(inner => inner.AddContent(0, content)));
		builder.CloseComponent();
	}

	private static IElement Button<T>(IRenderedComponent<T> cut, string text) where T : IComponent =>
		cut.FindAll("button").Single(button => button.TextContent.Trim() == text);

	private static IReadOnlyList<IElement> Steps(IRenderedComponent<MokaWizard> cut) => cut.FindAll(".moka-wizard-rail-step");

	private static IElement Rail(IRenderedComponent<MokaWizard> cut, int index) => cut.FindAll(".moka-wizard-rail-button")[index];

	private static string Showing(IRenderedComponent<MokaWizard> cut) => cut.Find(".moka-wizard-step").TextContent.Trim();

	private static IElement Mark(IRenderedComponent<MokaWizard> cut, int index) => Rail(cut, index).QuerySelector(".moka-wizard-rail-mark")!;

	[Fact]
	public void WithoutRail_NothingChanges()
	{
		IRenderedComponent<MokaWizard> cut = Render<MokaWizard>(p => p.Add(x => x.ChildContent, StepContent("Choosing", false, true)));

		Assert.Equal("moka-wizard", cut.Find(".moka-wizard").ClassName);
		Assert.Single(cut.FindAll(".moka-wizard-indicator"));
		Assert.Single(cut.FindAll(".moka-wizard-nav"));
		Assert.Empty(cut.FindAll(".moka-wizard-rail"));
		Assert.Empty(cut.FindAll(".moka-wizard-bar"));
		Assert.Empty(cut.FindAll(".moka-wizard-progress"));
		Assert.Equal("Previous", cut.FindAll(".moka-wizard-nav button")[0].TextContent.Trim());
		Assert.Equal("Next", cut.FindAll(".moka-wizard-nav button")[1].TextContent.Trim());
		Assert.DoesNotContain("Choosing", cut.Markup, StringComparison.Ordinal);
	}

	[Fact]
	public void WithoutRail_ShowSkipDoesNothing()
	{
		IRenderedComponent<MokaWizard> cut = Render<MokaWizard>(p => p
			.Add(x => x.ShowSkip, true)
			.Add(x => x.ChildContent, StepContent("", false, true)));

		Assert.DoesNotContain(cut.FindAll("button"), button => button.TextContent.Trim() == "Skip");
	}

	[Fact]
	public void Rail_ReplacesTheIndicatorAndTheNav()
	{
		IRenderedComponent<MokaWizard> cut = RenderRail();

		Assert.Contains("moka-wizard--rail", cut.Find(".moka-wizard").ClassName, StringComparison.Ordinal);
		Assert.Single(cut.FindAll(".moka-wizard-rail"));
		Assert.Single(cut.FindAll(".moka-wizard-bar"));
		Assert.Empty(cut.FindAll(".moka-wizard-indicator"));
		Assert.Empty(cut.FindAll(".moka-wizard-nav"));
	}

	[Fact]
	public void Rail_ListsTheStepsWithWhatEachDecided()
	{
		IRenderedComponent<MokaWizard> cut = RenderRail();

		Assert.Equal("Steps", cut.Find("ol.moka-wizard-rail-steps").GetAttribute("aria-label"));
		Assert.Equal(["Account", "Internet", "Wi-Fi"], cut.FindAll(".moka-wizard-rail-title").Select(title => title.TextContent.Trim()));
		Assert.Equal(["BC, owner", "Choosing a port"], cut.FindAll(".moka-wizard-rail-decided").Select(decided => decided.TextContent));
	}

	[Fact]
	public void Rail_ShowsTheStepsContent()
	{
		Assert.Equal("One", Showing(RenderRail()));
	}

	[Fact]
	public void RailLabel_NamesTheListAndTheProgressBar()
	{
		IRenderedComponent<MokaWizard> cut = RenderRail(p => p.Add(x => x.RailLabel, "Setup steps"));

		Assert.Equal("Setup steps", cut.Find("ol.moka-wizard-rail-steps").GetAttribute("aria-label"));
		Assert.Equal("Setup steps", cut.Find(".moka-progress").GetAttribute("aria-label"));
	}

	[Fact]
	public void TheStepShowing_IsMarkedCurrentAndKeepsItsNumber()
	{
		IRenderedComponent<MokaWizard> cut = RenderRail();

		Assert.Equal("step", Rail(cut, 0).GetAttribute("aria-current"));
		Assert.Null(Rail(cut, 1).GetAttribute("aria-current"));
		Assert.Equal("1", Mark(cut, 0).TextContent.Trim());
		Assert.Contains("moka-wizard-rail-step--now", Steps(cut)[0].ClassName, StringComparison.Ordinal);
		Assert.Contains("moka-wizard-rail-step--later", Steps(cut)[1].ClassName, StringComparison.Ordinal);
	}

	[Fact]
	public void AStepNotReached_CannotBeClicked()
	{
		IRenderedComponent<MokaWizard> cut = RenderRail();

		Assert.False(Rail(cut, 0).HasAttribute("disabled"));
		Assert.True(Rail(cut, 1).HasAttribute("disabled"));
		Assert.True(Rail(cut, 2).HasAttribute("disabled"));
	}

	[Fact]
	public async Task AStepPassed_IsDoneWithACheck()
	{
		IRenderedComponent<MokaWizard> cut = RenderRail();

		await Button(cut, "Continue").ClickAsync(new MouseEventArgs());

		Assert.Contains("moka-wizard-rail-step--done", Steps(cut)[0].ClassName, StringComparison.Ordinal);
		Assert.NotNull(Mark(cut, 0).QuerySelector("svg"));
		Assert.Equal("step", Rail(cut, 1).GetAttribute("aria-current"));
		Assert.Equal("Two", Showing(cut));
	}

	[Fact]
	public async Task EveryStepReached_CanBeClicked_AfterGoingBack()
	{
		int reported = -1;
		IRenderedComponent<MokaWizard> cut = RenderRail(p => p.Add(x => x.ActiveStepChanged, EventCallback.Factory.Create<int>(this, step => reported = step)));
		await Button(cut, "Continue").ClickAsync(new MouseEventArgs());
		await Button(cut, "Continue").ClickAsync(new MouseEventArgs());

		await Rail(cut, 0).ClickAsync(new MouseEventArgs());

		Assert.Equal(0, reported);
		Assert.Equal("One", Showing(cut));
		Assert.False(Rail(cut, 1).HasAttribute("disabled"));
		Assert.False(Rail(cut, 2).HasAttribute("disabled"));

		await Rail(cut, 2).ClickAsync(new MouseEventArgs());

		Assert.Equal(2, reported);
		Assert.Equal("Three", Showing(cut));
	}

	[Fact]
	public async Task ClickingTheStepShowing_ReportsNothing()
	{
		int reports = 0;
		IRenderedComponent<MokaWizard> cut = RenderRail(p => p.Add(x => x.ActiveStepChanged, EventCallback.Factory.Create<int>(this, _ => reports++)));

		await Rail(cut, 0).ClickAsync(new MouseEventArgs());

		Assert.Equal(0, reports);
	}

	[Fact]
	public void Rail_StartingPartWay_HasReachedTheStepsBefore()
	{
		IRenderedComponent<MokaWizard> cut = RenderRail(p => p.Add(x => x.ActiveStep, 1));

		Assert.False(Rail(cut, 0).HasAttribute("disabled"));
		Assert.False(Rail(cut, 1).HasAttribute("disabled"));
		Assert.True(Rail(cut, 2).HasAttribute("disabled"));
		Assert.Contains("moka-wizard-rail-step--done", Steps(cut)[0].ClassName, StringComparison.Ordinal);
	}

	[Fact]
	public void Linear_DoesNotOpenAStepNotReached()
	{
		IRenderedComponent<MokaWizard> cut = RenderRail(p => p.Add(x => x.Linear, false));

		Assert.True(Rail(cut, 2).HasAttribute("disabled"));
	}

	[Fact]
	public void HasProblem_PutsAMarkOnTheStep_AndSaysIt()
	{
		IRenderedComponent<MokaWizard> cut = RenderRail(secondProblem: true);

		Assert.Contains("moka-wizard-rail-step--problem", Steps(cut)[1].ClassName, StringComparison.Ordinal);
		Assert.Equal("!", Mark(cut, 1).TextContent.Trim());
		Assert.Equal("true", Mark(cut, 1).GetAttribute("aria-hidden"));
		Assert.Equal("Problem:", Rail(cut, 1).QuerySelector(".moka-visually-hidden")!.TextContent.Trim());
	}

	[Fact]
	public void ProblemText_ChangesTheWord()
	{
		IRenderedComponent<MokaWizard> cut = RenderRail(p => p.Add(x => x.ProblemText, "Fel"), secondProblem: true);

		Assert.Equal("Fel:", Rail(cut, 1).QuerySelector(".moka-visually-hidden")!.TextContent.Trim());
	}

	[Fact]
	public async Task AProblemOnTheStepShowing_KeepsItsNumber()
	{
		IRenderedComponent<MokaWizard> cut = RenderRail(secondProblem: true);

		await Button(cut, "Continue").ClickAsync(new MouseEventArgs());

		Assert.Equal("2", Mark(cut, 1).TextContent.Trim());
		Assert.Contains("moka-wizard-rail-step--now", Steps(cut)[1].ClassName, StringComparison.Ordinal);
	}

	[Fact]
	public void ANewDecided_ShowsInTheSameRender()
	{
		IRenderedComponent<MokaWizard> cut = RenderRail();

		cut.Render(p => p.Add(x => x.ChildContent, StepContent("Port 1", false, true)));

		Assert.Equal("Port 1", cut.FindAll(".moka-wizard-rail-decided")[1].TextContent);
	}

	[Fact]
	public void ANewProblem_ShowsInTheSameRender()
	{
		IRenderedComponent<MokaWizard> cut = RenderRail();

		cut.Render(p => p.Add(x => x.ChildContent, StepContent("Port 1", true, true)));

		Assert.Equal("!", Mark(cut, 1).TextContent.Trim());
	}

	[Fact]
	public async Task AStepTurningValid_ReachesContinueInTheSameRender()
	{
		IRenderedComponent<MokaWizard> cut = RenderRail(secondValid: false);
		await Button(cut, "Continue").ClickAsync(new MouseEventArgs());
		Assert.True(Button(cut, "Continue").HasAttribute("disabled"));

		cut.Render(p => p.Add(x => x.ChildContent, StepContent("Port 1", false, true)));

		Assert.False(Button(cut, "Continue").HasAttribute("disabled"));
	}

	[Fact]
	public void TheBar_SaysBackAndContinue()
	{
		IRenderedComponent<MokaWizard> cut = RenderRail();

		IReadOnlyList<IElement> buttons = cut.FindAll(".moka-wizard-bar button");
		Assert.Equal(["Back", "Continue"], buttons.Select(button => button.TextContent.Trim()));
		Assert.True(buttons[0].HasAttribute("disabled"));
	}

	[Fact]
	public void TheBar_TakesTheTextsItIsGiven()
	{
		IRenderedComponent<MokaWizard> cut = RenderRail(p => p.Add(x => x.PreviousText, "Earlier").Add(x => x.NextText, "Later"));

		Assert.Equal(["Earlier", "Later"], cut.FindAll(".moka-wizard-bar button").Select(button => button.TextContent.Trim()));
	}

	[Fact]
	public async Task TheBar_SaysFinishOnTheLastStep_AndHasNoSkip()
	{
		bool finished = false;
		IRenderedComponent<MokaWizard> cut = RenderRail(p => p
			.Add(x => x.ActiveStep, 2)
			.Add(x => x.ShowSkip, true)
			.Add(x => x.OnFinish, EventCallback.Factory.Create(this, () => finished = true)));

		Assert.Equal(["Back", "Finish"], cut.FindAll(".moka-wizard-bar button").Select(button => button.TextContent.Trim()));

		await Button(cut, "Finish").ClickAsync(new MouseEventArgs());

		Assert.True(finished);
	}

	[Fact]
	public async Task Back_GoesToTheStepBefore()
	{
		IRenderedComponent<MokaWizard> cut = RenderRail(p => p.Add(x => x.ActiveStep, 1));

		await Button(cut, "Back").ClickAsync(new MouseEventArgs());

		Assert.Equal("One", Showing(cut));
	}

	[Fact]
	public void Skip_IsOffByDefault()
	{
		Assert.DoesNotContain(RenderRail().FindAll(".moka-wizard-bar button"), button => button.TextContent.Trim() == "Skip");
	}

	[Fact]
	public void Skip_SitsBetweenBackAndContinue()
	{
		IRenderedComponent<MokaWizard> cut = RenderRail(p => p.Add(x => x.ShowSkip, true));

		Assert.Equal(["Back", "Skip", "Continue"], cut.FindAll(".moka-wizard-bar button").Select(button => button.TextContent.Trim()));
	}

	[Fact]
	public void SkipText_ChangesTheWord()
	{
		IRenderedComponent<MokaWizard> cut = RenderRail(p => p.Add(x => x.ShowSkip, true).Add(x => x.SkipText, "Skip for now"));

		Assert.Equal("Skip for now", cut.FindAll(".moka-wizard-bar button")[1].TextContent.Trim());
	}

	[Fact]
	public async Task Skip_RaisesOnSkip_AndMovesOn()
	{
		int skips = 0;
		int reported = -1;
		IRenderedComponent<MokaWizard> cut = RenderRail(p => p
			.Add(x => x.ShowSkip, true)
			.Add(x => x.OnSkip, EventCallback.Factory.Create(this, () => skips++))
			.Add(x => x.ActiveStepChanged, EventCallback.Factory.Create<int>(this, step => reported = step)));

		await Button(cut, "Skip").ClickAsync(new MouseEventArgs());

		Assert.Equal(1, skips);
		Assert.Equal(1, reported);
		Assert.Equal("Two", Showing(cut));
	}

	[Fact]
	public async Task Skip_PassesAStepThatIsNotValid()
	{
		IRenderedComponent<MokaWizard> cut = RenderRail(p => p.Add(x => x.ShowSkip, true).Add(x => x.Linear, true), secondValid: false);
		await Button(cut, "Continue").ClickAsync(new MouseEventArgs());
		Assert.True(Button(cut, "Continue").HasAttribute("disabled"));

		await Button(cut, "Skip").ClickAsync(new MouseEventArgs());

		Assert.Equal("Three", Showing(cut));
	}

	[Fact]
	public void ShowNavigation_HidesTheBar()
	{
		Assert.Empty(RenderRail(p => p.Add(x => x.ShowNavigation, false)).FindAll(".moka-wizard-bar"));
	}

	[Fact]
	public void ShowStepIndicator_HidesTheRailAndTheProgressBar()
	{
		IRenderedComponent<MokaWizard> cut = RenderRail(p => p.Add(x => x.ShowStepIndicator, false));

		Assert.Empty(cut.FindAll(".moka-wizard-rail"));
		Assert.Empty(cut.FindAll(".moka-wizard-progress"));
		Assert.Equal("One", Showing(cut));
	}

	[Fact]
	public void TheProgressBar_IsThinAndCountsTheSteps()
	{
		IRenderedComponent<MokaWizard> cut = RenderRail(p => p.Add(x => x.ActiveStep, 1));

		MokaProgress progress = cut.FindComponent<MokaProgress>().Instance;
		Assert.True(progress.Thin);
		Assert.Equal(200d / 3, progress.Value!.Value, 6);
		Assert.Equal("Step 2 of 3", cut.Find(".moka-wizard-progress-count").TextContent);
		Assert.Equal("Internet", cut.Find(".moka-wizard-progress-title").TextContent);
	}

	[Fact]
	public void StepCountText_WritesTheCount()
	{
		IRenderedComponent<MokaWizard> cut = RenderRail(p => p.Add(x => x.StepCountText, (number, total) => $"{number}/{total}"));

		Assert.Equal("1/3", cut.Find(".moka-wizard-progress-count").TextContent);
	}

	// Both stuck at the window's top edge, under a sticky app bar or the app shell's phone bar.
	[Fact]
	public void TheStickyParts_KeepBelowAStickyAppBar()
	{
		IReadOnlyDictionary<string, string> steps = ScopedCss.Declarations(Stylesheet, ".moka-wizard-rail-steps");
		Assert.Equal("var(--moka-appbar-space, 0px)", steps["top"]);
		Assert.Equal("calc(100vh - var(--moka-appbar-space, 0px))", steps["max-height"]);

		Assert.Equal("var(--moka-appbar-space, 0px)", ScopedCss.Declarations(Stylesheet, ".moka-wizard--rail > .moka-wizard-progress")["top"]);
	}

	[Fact]
	public void Under900Px_TheProgressBarReplacesTheRail()
	{
		Assert.Equal(["none"], ScopedCss.Values(Stylesheet, ".moka-wizard--rail > .moka-wizard-rail", "display"));
		Assert.Equal(["none", "flex"], ScopedCss.Values(Stylesheet, ".moka-wizard--rail > .moka-wizard-progress", "display"));

		string css = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Css", Stylesheet));
		Assert.Contains("@media (max-width: 900px)", css, StringComparison.Ordinal);
	}

	[Fact]
	public void TheRail_Is300PxWide()
	{
		Assert.Equal("18.75rem", ScopedCss.Declarations(Stylesheet, ".moka-wizard--rail > .moka-wizard-rail")["width"]);
	}

	[Fact]
	public void TheBar_StaysAtTheBottom()
	{
		IReadOnlyDictionary<string, string> bar = ScopedCss.Declarations(Stylesheet, ".moka-wizard--rail > .moka-wizard-bar");

		Assert.Equal("sticky", bar["position"]);
		Assert.Equal("0", bar["bottom"]);
		Assert.Equal("var(--moka-color-surface-raised)", bar["background"]);
	}

	[Theory]
	[InlineData(".moka-wizard-rail-step--done .moka-wizard-rail-mark", "--moka-color-success-bg", "--moka-color-success-ink")]
	[InlineData(".moka-wizard-rail-step--problem .moka-wizard-rail-mark", "--moka-color-error-bg", "--moka-color-error-soft")]
	[InlineData(".moka-wizard-rail-step--now .moka-wizard-rail-mark", "--moka-color-primary", "--moka-color-on-primary")]
	public void TheMarks_ReadTokens(string selector, string background, string color)
	{
		IReadOnlyDictionary<string, string> mark = ScopedCss.Declarations(Stylesheet, selector);

		Assert.Equal($"var({background})", mark["background"]);
		Assert.Equal($"var({color})", mark["color"]);
	}
}
