using System.Globalization;
using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Theming;
using Moka.Red.Feedback.BootScreen;
using Moka.Red.Icons;
using Moka.Red.Tests.Shared;

namespace Moka.Red.Feedback.Tests.Components;

public class MokaBootScreenTests : BunitContext
{
	private const string Module = "./_content/Moka.Red.Feedback/BootScreen/MokaBootScreen.razor.js";

	private static readonly MokaBootStep[] Startup =
	[
		new("runtime", MokaBootStepState.Done, ".NET 10.0", TimeSpan.FromMilliseconds(142)),
		new("fonts", MokaBootStepState.Done, "3 families", TimeSpan.FromMilliseconds(176)),
		new("cache", MokaBootStepState.Warning, "stale", TimeSpan.FromMilliseconds(286)),
		new("devices", MokaBootStepState.Running, "42 of 118", TimeSpan.FromMilliseconds(600)),
		new("rules")
	];

	private static readonly MokaBootStep[] Stopped =
	[
		new("Connect to the router", MokaBootStepState.Done, "router.local", TimeSpan.FromMilliseconds(120)),
		new("Check your sign-in", MokaBootStepState.Done, "BC, owner", TimeSpan.FromMilliseconds(40)),
		new("Load devices", MokaBootStepState.Failed, Error: "The router didn't answer, 3 tries."),
		new("Load firewall rules"),
		new("Start live traffic")
	];

	public MokaBootScreenTests() => JSInterop.Mode = JSRuntimeMode.Loose;

	public static TheoryData<MokaBootVariant, string> VariantParts => new()
	{
		{ MokaBootVariant.Mark, ".moka-boot-screen__stage" },
		{ MokaBootVariant.Log, ".moka-boot-screen__lines" },
		{ MokaBootVariant.Steps, ".moka-boot-screen__steps" },
		{ MokaBootVariant.Classic, ".moka-boot-screen-brand" },
		{ MokaBootVariant.Terminal, ".moka-boot-screen__screen" },
		{ MokaBootVariant.Pulse, ".moka-boot-screen__orbit" },
		{ MokaBootVariant.Horizon, ".moka-boot-screen__plane" }
	};

	public static TheoryData<MokaBootVariant> Variants => [.. Enum.GetValues<MokaBootVariant>()];

	[Fact]
	public void Visible_Default_RendersRoot()
	{
		IRenderedComponent<MokaBootScreen> cut = Render<MokaBootScreen>();

		Assert.Single(cut.FindAll(".moka-boot-screen"));
	}

	[Fact]
	public void Visible_False_RendersNoScreen_ButKeepsTheStatusRegion()
	{
		IRenderedComponent<MokaBootScreen> cut = Render<MokaBootScreen>(p => p
			.Add(x => x.Visible, false));

		Assert.Empty(cut.FindAll(".moka-boot-screen"));
		Assert.Equal(string.Empty, cut.Find("[role=status]").TextContent);
	}

	[Fact]
	public void Brand_Renders()
	{
		IRenderedComponent<MokaBootScreen> cut = Render<MokaBootScreen>(p => p
			.Add(x => x.Brand, "MOKA"));

		Assert.Contains("MOKA", cut.Find(".moka-boot-screen-mark").TextContent, StringComparison.Ordinal);
	}

	[Fact]
	public void BrandSecondary_RendersDotSeparator()
	{
		IRenderedComponent<MokaBootScreen> cut = Render<MokaBootScreen>(p => p
			.Add(x => x.Brand, "MOKA")
			.Add(x => x.BrandSecondary, "RED"));

		Assert.NotNull(cut.Find(".moka-boot-screen-dot"));
		Assert.Contains("RED", cut.Find(".moka-boot-screen-mark").TextContent, StringComparison.Ordinal);
	}

	[Fact]
	public void StatusText_RendersSubtitle()
	{
		IRenderedComponent<MokaBootScreen> cut = Render<MokaBootScreen>(p => p
			.Add(x => x.StatusText, "booting"));

		Assert.Contains("booting", cut.Find(".moka-boot-screen-subtitle").TextContent, StringComparison.Ordinal);
	}

	[Fact]
	public void ChipsContent_Renders()
	{
		IRenderedComponent<MokaBootScreen> cut = Render<MokaBootScreen>(p => p
			.Add(x => x.ChipsContent, "<span id=\"chip\">engine</span>"));

		Assert.NotNull(cut.Find(".moka-boot-screen-chips").QuerySelector("#chip"));
	}

	[Fact]
	public void ShowBar_Default_RendersBar()
	{
		IRenderedComponent<MokaBootScreen> cut = Render<MokaBootScreen>();

		Assert.NotNull(cut.Find(".moka-boot-screen-bar"));
	}

	[Fact]
	public void ShowBar_False_NoBar()
	{
		IRenderedComponent<MokaBootScreen> cut = Render<MokaBootScreen>(p => p
			.Add(x => x.ShowBar, false));

		Assert.Empty(cut.FindAll(".moka-boot-screen-bar"));
	}

	[Fact]
	public void Default_HasGridAndScanline_NotEmbedded()
	{
		IRenderedComponent<MokaBootScreen> cut = Render<MokaBootScreen>();

		string cls = cut.Find(".moka-boot-screen").ClassName!;
		Assert.Contains("moka-boot-screen--grid", cls, StringComparison.Ordinal);
		Assert.Contains("moka-boot-screen--scanline", cls, StringComparison.Ordinal);
		Assert.DoesNotContain("moka-boot-screen--embedded", cls, StringComparison.Ordinal);
	}

	[Fact]
	public void FullScreen_False_AddsEmbedded()
	{
		IRenderedComponent<MokaBootScreen> cut = Render<MokaBootScreen>(p => p
			.Add(x => x.FullScreen, false));

		Assert.Contains("moka-boot-screen--embedded", cut.Find(".moka-boot-screen").ClassName, StringComparison.Ordinal);
	}

	// No variant named on the original design is the screen it always was: the same classes, the same parts and
	// no card row in a small box.
	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public void OriginalDesign_WithNoVariant_DrawsTheClassicScreenAsBefore(bool light)
	{
		IRenderedComponent<MokaBootScreen> cut = Render<MokaBootScreen>(p => p
			.AddCascadingValue(light ? MokaTheme.Light : MokaTheme.Dark)
			.Add(x => x.FullScreen, false)
			.Add(x => x.BrandSecondary, "RED"));

		IElement root = cut.Find(".moka-boot-screen");
		Assert.Equal("moka-boot-screen moka-boot-screen--grid moka-boot-screen--scanline moka-boot-screen--embedded",
			root.ClassName);
		Assert.Equal("moka-boot-screen-brand", root.FirstElementChild?.ClassName);
		Assert.Empty(cut.FindAll(".moka-boot-screen__card"));
		Assert.Empty(cut.FindAll(".moka-boot-screen__logo"));
		Assert.Null(cut.Find(".moka-boot-screen-bar > span").GetAttribute("style"));
	}

	[Fact]
	public void NewDesign_WithNoVariant_DrawsMark()
	{
		IRenderedComponent<MokaBootScreen> cut = Render<MokaBootScreen>(p => p
			.AddCascadingValue(MokaTheme.Default)
			.Add(x => x.Brand, "Moka")
			.Add(x => x.BrandSecondary, "Red"));

		IElement root = cut.Find(".moka-boot-screen");
		Assert.Contains("moka-boot-screen--mark", root.ClassList);
		Assert.Contains("moka-boot-screen--plain", root.ClassList);
		Assert.DoesNotContain("moka-boot-screen--grid", root.ClassList);
		Assert.Equal("M", cut.Find(".moka-boot-screen__logo").TextContent);
		Assert.Equal("Moka·Red", cut.Find(".moka-boot-screen__brand").TextContent);
		Assert.Equal("Red", cut.Find(".moka-boot-screen__second").TextContent);
		Assert.Equal("initializing", cut.Find(".moka-boot-screen__status").TextContent);
		Assert.Empty(cut.FindAll(".moka-boot-screen-mark"));
	}

	[Fact]
	public void Classic_CanBeChosenOnTheNewDesign()
	{
		IRenderedComponent<MokaBootScreen> cut = Render<MokaBootScreen>(p => p
			.AddCascadingValue(MokaTheme.Default)
			.Add(x => x.BootVariant, MokaBootVariant.Classic));

		IElement root = cut.Find(".moka-boot-screen");
		Assert.Contains("moka-boot-screen--classic", root.ClassList);
		Assert.Contains("moka-boot-screen--grid", root.ClassList);
		Assert.Contains("moka-boot-screen--scanline", root.ClassList);
		Assert.NotNull(cut.Find(".moka-boot-screen-mark"));
	}

	[Fact]
	public void Mark_CanBeChosenOnTheOriginalDesign()
	{
		IRenderedComponent<MokaBootScreen> cut = Render<MokaBootScreen>(p => p
			.AddCascadingValue(MokaTheme.Dark)
			.Add(x => x.BootVariant, MokaBootVariant.Mark));

		Assert.Contains("moka-boot-screen--mark", cut.Find(".moka-boot-screen").ClassList);
		Assert.NotNull(cut.Find(".moka-boot-screen__stage"));
	}

	[Theory]
	[MemberData(nameof(VariantParts))]
	public void EveryVariant_DrawsItsOwnParts(MokaBootVariant variant, string part)
	{
		IRenderedComponent<MokaBootScreen> cut = Render<MokaBootScreen>(p => p
			.Add(x => x.BootVariant, variant)
			.Add(x => x.Steps, Startup));

		Assert.NotNull(cut.Find(part));
		Assert.DoesNotContain("moka-boot-screen--failed", cut.Find(".moka-boot-screen").ClassList);
	}

	// The classic variants keep their grid and scanline to Classic: the design rules ban scanlines elsewhere.
	[Theory]
	[InlineData(MokaBootVariant.Terminal)]
	[InlineData(MokaBootVariant.Pulse)]
	[InlineData(MokaBootVariant.Horizon)]
	public void TheNewClassicVariants_GlowWithoutGridOrScanline(MokaBootVariant variant)
	{
		IRenderedComponent<MokaBootScreen> cut = Render<MokaBootScreen>(p => p.Add(x => x.BootVariant, variant));

		IElement root = cut.Find(".moka-boot-screen");
		Assert.Contains("moka-boot-screen--lit", root.ClassList);
		Assert.DoesNotContain("moka-boot-screen--grid", root.ClassList);
		Assert.DoesNotContain("moka-boot-screen--scanline", root.ClassList);
		Assert.NotNull(cut.Find(".moka-boot-screen__word"));
	}

	[Fact]
	public void Logo_TakesThePlaceOfTheLetterTile()
	{
		IRenderedComponent<MokaBootScreen> cut = Render<MokaBootScreen>(p => p
			.Add(x => x.BootVariant, MokaBootVariant.Mark)
			.Add(x => x.Logo, "<img id=\"logo\" alt=\"\" />"));

		Assert.NotNull(cut.Find(".moka-boot-screen__name #logo"));
		Assert.Empty(cut.FindAll(".moka-boot-screen__logo"));
	}

	[Fact]
	public void Mark_PutsTheChipsOnOneQuietLine()
	{
		IRenderedComponent<MokaBootScreen> cut = Render<MokaBootScreen>(p => p
			.Add(x => x.BootVariant, MokaBootVariant.Mark)
			.Add(x => x.ChipsContent, "<span id=\"chip\">v0.2.2</span>"));

		Assert.NotNull(cut.Find(".moka-boot-screen__meta #chip"));
		Assert.Empty(cut.FindAll(".moka-boot-screen-chips"));
	}

	// The status region used to be the screen itself, created together with its text, so most screen readers
	// never read the first status. It is on the page before the screen shows, and the screen is no region.
	[Fact]
	public void StatusRegion_IsOnThePage_BeforeItsTextArrives()
	{
		IRenderedComponent<MokaBootScreen> cut = Render<MokaBootScreen>(p => p
			.Add(x => x.Visible, false)
			.Add(x => x.StatusText, "Loading your workspace"));

		IElement region = cut.Find("[role=status]");
		Assert.Equal("polite", region.GetAttribute("aria-live"));
		Assert.True(region.HasAttribute("data-moka-boot-live"));
		Assert.Equal(string.Empty, region.TextContent);

		cut.Render(p => p.Add(x => x.Visible, true));

		Assert.Equal("Loading your workspace", cut.Find("[role=status]").TextContent);
		IElement root = cut.Find(".moka-boot-screen");
		Assert.Null(root.GetAttribute("role"));
		Assert.Null(root.GetAttribute("aria-live"));
		Assert.Null(root.QuerySelector("[aria-live]"));
	}

	[Fact]
	public void StatusRegion_ReadsTheRunningStep_ThenTheFailure()
	{
		IRenderedComponent<MokaBootScreen> cut = Render<MokaBootScreen>(p => p
			.Add(x => x.BootVariant, MokaBootVariant.Steps)
			.Add(x => x.Steps, Startup));

		Assert.Equal("devices", cut.Find("[role=status]").TextContent);

		cut.Render(p => p.Add(x => x.Steps, Stopped));

		Assert.Equal("Load devices. The router didn't answer, 3 tries.", cut.Find("[role=status]").TextContent);
	}

	// Full screen it covered the page but left it live, so Tab still walked the controls underneath.
	[Fact]
	public void FullScreen_MakesThePageInert_UntilItGoes()
	{
		BunitJSModuleInterop module = JSInterop.SetupModule(Module);
		module.Mode = JSRuntimeMode.Loose;
		IRenderedComponent<MokaBootScreen> cut = Render<MokaBootScreen>();

		JSRuntimeInvocation hold = module.VerifyInvoke("hold");
		string key = Assert.IsType<string>(hold.Arguments[0]);
		Assert.IsType<ElementReference>(hold.Arguments[1]);
		Assert.Empty(module.Invocations["release"]);

		cut.Render(p => p.Add(x => x.Visible, false));

		Assert.Equal(key, module.VerifyInvoke("release").Arguments[0]);
		Assert.Single(module.Invocations["hold"]);
	}

	[Fact]
	public void Embedded_LeavesThePageAlone()
	{
		BunitJSModuleInterop module = JSInterop.SetupModule(Module);
		module.Mode = JSRuntimeMode.Loose;

		Render<MokaBootScreen>(p => p.Add(x => x.FullScreen, false));

		Assert.Empty(module.Invocations["hold"]);
	}

	[Fact]
	public async Task Disposing_GivesThePageBack()
	{
		BunitJSModuleInterop module = JSInterop.SetupModule(Module);
		module.Mode = JSRuntimeMode.Loose;
		Render<MokaBootScreen>();
		string key = Assert.IsType<string>(module.VerifyInvoke("hold").Arguments[0]);

		await DisposeComponentsAsync();

		Assert.Equal(key, module.VerifyInvoke("release").Arguments[0]);
	}

	[Fact]
	public void Log_PrintsTheStepsWhoseTimeHasCome()
	{
		IRenderedComponent<MokaBootScreen> cut = Render<MokaBootScreen>(p => p
			.Add(x => x.BootVariant, MokaBootVariant.Log)
			.Add(x => x.Steps, Startup));

		IReadOnlyList<IElement> entries = cut.FindAll(".moka-boot-screen__entry");
		Assert.Equal(["runtime", "fonts", "cache", "devices"],
			entries.Select(e => e.QuerySelector(".moka-boot-screen__entry-label")!.TextContent).ToArray());
		Assert.Equal(["ok", "ok", "warn", "run"],
			entries.Select(e => e.QuerySelector(".moka-boot-screen__entry-word")!.TextContent).ToArray());
		Assert.Equal([Clock(0), Clock(0.142), Clock(0.318), Clock(0.604)],
			entries.Select(e => e.QuerySelector(".moka-boot-screen__entry-time")!.TextContent).ToArray());
		Assert.Contains("moka-boot-screen__entry--warning", entries[2].ClassList);
		Assert.NotNull(entries[3].QuerySelector(".moka-boot-screen__cursor"));
		Assert.Single(cut.FindAll(".moka-boot-screen__cursor"));
		Assert.Equal("3 of 5 steps", cut.Find(".moka-boot-screen__log-count span").TextContent);
	}

	[Fact]
	public void Log_WithoutTimes_DropsTheTimeColumn()
	{
		IRenderedComponent<MokaBootScreen> cut = Render<MokaBootScreen>(p => p
			.Add(x => x.BootVariant, MokaBootVariant.Log)
			.Add(x => x.Steps, Startup)
			.Add(x => x.ShowTimes, false));

		Assert.Empty(cut.FindAll(".moka-boot-screen__entry-time"));
		Assert.Contains("moka-boot-screen__lines--untimed", cut.Find(".moka-boot-screen__lines").ClassList);
		Assert.Single(cut.FindAll(".moka-boot-screen__log-count span"));
	}

	[Fact]
	public void Log_MaxLines_KeepsTheNewestLines()
	{
		IRenderedComponent<MokaBootScreen> cut = Render<MokaBootScreen>(p => p
			.Add(x => x.BootVariant, MokaBootVariant.Log)
			.Add(x => x.Steps, Startup)
			.Add(x => x.MaxLines, 2));

		Assert.Equal(["cache", "devices"],
			cut.FindAll(".moka-boot-screen__entry-label").Select(e => e.TextContent).ToArray());
	}

	[Fact]
	public void Steps_ListsEveryStep_WithItsStateAndTime()
	{
		IRenderedComponent<MokaBootScreen> cut = Render<MokaBootScreen>(p => p
			.Add(x => x.BootVariant, MokaBootVariant.Steps)
			.Add(x => x.Steps, Startup)
			.Add(x => x.Title, "Opening")
			.Add(x => x.Hint, "Usually about 5 seconds")
			.Add(x => x.Aside, "<span id=\"aside\">Your home router</span>"));

		IReadOnlyList<IElement> steps = cut.FindAll(".moka-boot-screen__step");
		Assert.Equal(5, steps.Count);
		Assert.Contains("moka-boot-screen__step--done", steps[0].ClassList);
		Assert.Contains("moka-boot-screen__step--running", steps[3].ClassList);
		Assert.Contains("moka-boot-screen__step--waiting", steps[4].ClassList);
		Assert.Equal("Done: runtime", steps[0].QuerySelector(".moka-boot-screen__step-label")!.TextContent);
		Assert.Equal(".NET 10.0", steps[0].QuerySelector(".moka-boot-screen__step-detail")!.TextContent);
		Assert.Equal(Duration("{0:0} ms", 142), steps[0].QuerySelector(".moka-boot-screen__step-time")!.TextContent);
		Assert.Null(steps[4].QuerySelector(".moka-boot-screen__step-time"));
		Assert.NotNull(steps[3].QuerySelector(".moka-boot-screen__spinner"));
		Assert.Equal("Opening", cut.Find(".moka-boot-screen__panel-title").TextContent);
		Assert.Equal("Opening", cut.Find(".moka-boot-screen__steps").GetAttribute("aria-label"));
		Assert.Equal("Usually about 5 seconds", cut.Find(".moka-boot-screen__panel-foot .moka-boot-screen__hint").TextContent);
		Assert.NotNull(cut.Find(".moka-boot-screen__aside #aside"));
	}

	[Fact]
	public void Status_FollowsTheRunningStep_UntilTheStatusIsYourOwn()
	{
		IRenderedComponent<MokaBootScreen> cut = Render<MokaBootScreen>(p => p
			.Add(x => x.BootVariant, MokaBootVariant.Mark)
			.Add(x => x.Steps, Startup));

		Assert.Equal("devices", cut.Find(".moka-boot-screen__status").TextContent);

		cut.Render(p => p.Add(x => x.StatusText, "Waking the router"));

		Assert.Equal("Waking the router", cut.Find(".moka-boot-screen__status").TextContent);
	}

	[Theory]
	[InlineData(null, false, null)]
	[InlineData(46d, false, "width: 46%")]
	[InlineData(150d, false, "width: 100%")]
	[InlineData(null, true, "width: 60%")]
	public void Progress_FillsTheLine(double? progress, bool withSteps, string? expected)
	{
		IRenderedComponent<MokaBootScreen> cut = Render<MokaBootScreen>(p => p
			.Add(x => x.BootVariant, MokaBootVariant.Mark)
			.Add(x => x.Progress, progress)
			.Add(x => x.Steps, withSteps ? Startup : null));

		IElement line = cut.Find(".moka-boot-screen__line");
		Assert.Equal(expected is not null, line.ClassList.Contains("moka-boot-screen__line--determinate"));
		Assert.Equal(expected, line.FirstElementChild!.GetAttribute("style"));
	}

	[Fact]
	public void Progress_FillsTheClassicBarToo()
	{
		IRenderedComponent<MokaBootScreen> cut = Render<MokaBootScreen>(p => p.Add(x => x.Progress, 25d));

		Assert.Contains("moka-boot-screen-bar--determinate", cut.Find(".moka-boot-screen-bar").ClassList);
		Assert.Equal("width: 25%", cut.Find(".moka-boot-screen-bar > span").GetAttribute("style"));
	}

	[Theory]
	[MemberData(nameof(Variants))]
	public void EveryVariant_HasAFailedState_WithAWayOut(MokaBootVariant variant)
	{
		int retries = 0;
		IRenderedComponent<MokaBootScreen> cut = Render<MokaBootScreen>(p => p
			.Add(x => x.BootVariant, variant)
			.Add(x => x.Error, "Couldn't reach the server")
			.Add(x => x.OnRetry, () => retries++)
			.Add(x => x.ErrorContent, "<a id=\"details\" href=\"#\">Show details</a>"));

		IElement root = cut.Find(".moka-boot-screen");
		Assert.Contains("moka-boot-screen--failed", root.ClassList);
		Assert.Contains("Couldn't reach the server", root.TextContent, StringComparison.Ordinal);
		Assert.NotNull(cut.Find(".moka-boot-screen__actions #details"));

		IElement retry = Assert.Single(cut.FindAll(".moka-boot-screen__actions button"));
		Assert.Equal("Try again", retry.TextContent.Trim());
		retry.Click();
		Assert.Equal(1, retries);
	}

	[Fact]
	public void FailedState_DrawsNoTryAgain_WithoutOnRetry()
	{
		IRenderedComponent<MokaBootScreen> cut = Render<MokaBootScreen>(p => p
			.Add(x => x.BootVariant, MokaBootVariant.Mark)
			.Add(x => x.Error, "Couldn't reach the server"));

		Assert.Empty(cut.FindAll("button"));
		Assert.Empty(cut.FindAll(".moka-boot-screen__line"));
	}

	[Fact]
	public void RetryText_RenamesTheButton()
	{
		IRenderedComponent<MokaBootScreen> cut = Render<MokaBootScreen>(p => p
			.Add(x => x.BootVariant, MokaBootVariant.Mark)
			.Add(x => x.Error, "Failed")
			.Add(x => x.OnRetry, () => { })
			.Add(x => x.RetryText, "Reconnect"));

		Assert.Equal("Reconnect", cut.Find("button").TextContent.Trim());
	}

	[Fact]
	public void Steps_AFailedStep_SaysWhy_OffersTryAgain_AndStopsTheBar()
	{
		IRenderedComponent<MokaBootScreen> cut = Render<MokaBootScreen>(p => p
			.Add(x => x.BootVariant, MokaBootVariant.Steps)
			.Add(x => x.Steps, Stopped)
			.Add(x => x.OnRetry, () => { }));

		IElement failed = cut.FindAll(".moka-boot-screen__step")[2];
		Assert.Contains("moka-boot-screen__step--failed", failed.ClassList);
		Assert.Equal("The router didn't answer, 3 tries.", failed.QuerySelector(".moka-boot-screen__step-detail")!.TextContent);
		Assert.NotNull(failed.QuerySelector(".moka-boot-screen__actions button"));
		Assert.Equal("Stopped at step 3 of 5", cut.Find(".moka-boot-screen__stopped").TextContent);
		Assert.Empty(cut.FindAll(".moka-boot-screen__panel-foot .moka-boot-screen__line"));
	}

	[Fact]
	public void Classic_OnTheOriginalDesign_FailsInItsOwnLook()
	{
		IRenderedComponent<MokaBootScreen> cut = Render<MokaBootScreen>(p => p
			.AddCascadingValue(MokaTheme.Dark)
			.Add(x => x.Error, "Server unreachable"));

		Assert.Contains("moka-boot-screen--failed", cut.Find(".moka-boot-screen").ClassList);
		Assert.Contains("Server unreachable", cut.Find(".moka-boot-screen-subtitle--failed").TextContent, StringComparison.Ordinal);
		Assert.Empty(cut.FindAll(".moka-boot-screen-bar"));
	}

	[Fact]
	public void Embedded_RendersTheCardRow_First()
	{
		IRenderedComponent<MokaBootScreen> cut = Render<MokaBootScreen>(p => p
			.AddCascadingValue(MokaTheme.Default)
			.Add(x => x.FullScreen, false)
			.Add(x => x.Steps, Startup));

		IElement root = cut.Find(".moka-boot-screen");
		Assert.Contains("moka-boot-screen--shrinks", root.ClassList);
		Assert.Equal("moka-boot-screen__card", root.FirstElementChild?.ClassName);
		Assert.Equal("devices", cut.Find(".moka-boot-screen__card-title").TextContent);
		Assert.Equal($"Step 4 of 5 · {Duration("{0:0.##} s", 1.204)}", cut.Find(".moka-boot-screen__card-sub").TextContent);
		Assert.NotNull(cut.Find(".moka-boot-screen__card .moka-boot-screen__spinner"));
	}

	[Fact]
	public void FullScreen_HasNoCardRow()
	{
		IRenderedComponent<MokaBootScreen> cut = Render<MokaBootScreen>(p => p.AddCascadingValue(MokaTheme.Default));

		Assert.Empty(cut.FindAll(".moka-boot-screen__card"));
		Assert.DoesNotContain("moka-boot-screen--shrinks", cut.Find(".moka-boot-screen").ClassList);
	}

	[Fact]
	public void Card_ShowsTheIcon_InPlaceOfTheSpinner_AndTheFailure()
	{
		IRenderedComponent<MokaBootScreen> cut = Render<MokaBootScreen>(p => p
			.Add(x => x.BootVariant, MokaBootVariant.Mark)
			.Add(x => x.FullScreen, false)
			.Add(x => x.Icon, MokaIcons.File.Terminal));

		Assert.Empty(cut.FindAll(".moka-boot-screen__card .moka-boot-screen__spinner"));
		Assert.NotNull(cut.Find(".moka-boot-screen__card-tile svg"));

		cut.Render(p => p.Add(x => x.Error, "It refused the connection."));

		Assert.Contains("moka-boot-screen__card-tile--failed", cut.Find(".moka-boot-screen__card-tile").ClassList);
		Assert.Equal("It refused the connection.", cut.Find(".moka-boot-screen__card-title").TextContent);
	}

	[Fact]
	public void TheCardRow_TakesOverInABoxUnder360By240()
	{
		string css = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Css", "MokaBootScreen.razor.css"));

		Assert.Contains("container: moka-boot-screen / size", css, StringComparison.Ordinal);
		Assert.Contains("@container moka-boot-screen (width < 22.5rem) or (height < 15rem)", css, StringComparison.Ordinal);
		Assert.Equal(["none", "flex"], ScopedCss.Values("MokaBootScreen.razor.css", ".moka-boot-screen__card", "display"));
		Assert.Equal("none", ScopedCss.Declarations("MokaBootScreen.razor.css", ".moka-boot-screen__card ~ *")["display"]);
	}

	// The keyframes went -100%, 200%, 500%, eased per half: the piece stopped dead mid-track and was off the track
	// for about a quarter of each loop.
	[Theory]
	[InlineData("MokaBootScreen.razor.css", ".moka-boot-screen-bar > span", "moka-boot-screen-bar")]
	[InlineData("MokaBootScreen.razor.css", ".moka-boot-screen__line > span", "moka-boot-screen-bar")]
	[InlineData("moka-boot.css", ".moka-boot-bar > span", "moka-boot-bar")]
	public void TheBar_SlidesEndToEnd_AtOneSpeed(string file, string piece, string keyframes)
	{
		Assert.Equal($"{keyframes} 1.6s linear infinite", ScopedCss.Values(file, piece, "animation")[0]);
		Assert.Equal("from { transform: translateX(-100%); } to { transform: translateX(333.333%); }",
			Keyframes(file, keyframes));
	}

	// The splash before Blazor starts drew the old look, so an app on Red visibly jumped at handoff.
	[Fact]
	public void TheSplash_DrawsMarkInRedsColors_WithTheOldSplashOneClassAway()
	{
		// The @import ahead of it hides this rule from ScopedCss, which reads everything up to a brace as selectors.
		string css = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Css", "moka-boot.css"));
		string dark = css[css.IndexOf("\n.moka-boot {", StringComparison.Ordinal)..];
		dark = dark[..dark.IndexOf('}', StringComparison.Ordinal)];
		Assert.Contains("--moka-boot-bg: #0C0C0E;", dark, StringComparison.Ordinal);
		Assert.Contains("--moka-boot-accent: #EF5350;", dark, StringComparison.Ordinal);
		Assert.Contains("--moka-boot-on-accent: #0A0E15;", dark, StringComparison.Ordinal);
		Assert.StartsWith("@import url(\"moka-fonts.css\");", css[css.IndexOf("@import", StringComparison.Ordinal)..],
			StringComparison.Ordinal);

		IReadOnlyDictionary<string, string> light =
			ScopedCss.Declarations("moka-boot.css", ".moka-boot:where(:not(.moka-boot--matrix))");
		Assert.Equal("#F5F5F6", light["--moka-boot-bg"]);
		Assert.Equal("#C62828", light["--moka-boot-accent"]);

		IReadOnlyDictionary<string, string> matrix = ScopedCss.Declarations("moka-boot.css", ".moka-boot--matrix");
		Assert.Equal("#060608", matrix["--moka-boot-bg"]);
		Assert.Equal("none", ScopedCss.Declarations("moka-boot.css", ".moka-boot--matrix .moka-boot-logo")["display"]);
	}

	[Theory]
	[InlineData(".moka-boot-bar", ".moka-boot-screen__line", "width")]
	[InlineData(".moka-boot-bar", ".moka-boot-screen__line", "height")]
	[InlineData(".moka-boot-logo", ".moka-boot-screen__logo", "width")]
	[InlineData(".moka-boot-logo", ".moka-boot-screen__logo", "height")]
	public void TheSplash_SizesItsPartsAsMarkDoes(string splashPart, string markPart, string property)
	{
		Assert.Equal(ScopedCss.Declarations("MokaBootScreen.razor.css", markPart)[property],
			ScopedCss.Declarations("moka-boot.css", splashPart)[property]);
	}

	[Theory]
	[InlineData(".moka-boot-screen__line > span")]
	[InlineData(".moka-boot-screen__spinner")]
	[InlineData(".moka-boot-screen__cursor")]
	[InlineData(".moka-boot-screen__ring")]
	[InlineData(".moka-boot-screen__dots")]
	[InlineData(".moka-boot-screen__plane")]
	public void EveryAnimation_RunsAtOneSpeed_AndStopsForLessMotion(string part)
	{
		IReadOnlyList<string> animations = ScopedCss.Values("MokaBootScreen.razor.css", part, "animation");

		Assert.Contains(" linear ", animations[0], StringComparison.Ordinal);
		Assert.Equal("none", animations[^1]);
	}

	[Fact]
	public void StepCountText_WritesTheCardRowsCount()
	{
		IRenderedComponent<MokaBootScreen> cut = Render<MokaBootScreen>(p => p
			.Add(x => x.BootVariant, MokaBootVariant.Mark)
			.Add(x => x.FullScreen, false)
			.Add(x => x.Steps, Startup)
			.Add(x => x.ShowTimes, false)
			.Add(x => x.StepCountText, (number, total) => $"Etappe {number}/{total}"));

		Assert.Equal("Etappe 4/5", cut.Find(".moka-boot-screen__card-sub").TextContent);
	}

	[Fact]
	public void StepsDoneText_WritesTheLogsCount()
	{
		IRenderedComponent<MokaBootScreen> cut = Render<MokaBootScreen>(p => p
			.Add(x => x.BootVariant, MokaBootVariant.Log)
			.Add(x => x.Steps, Startup)
			.Add(x => x.StepsDoneText, (done, total) => $"{done}/{total} klara"));

		Assert.Equal("3/5 klara", cut.Find(".moka-boot-screen__log-count span").TextContent);
	}

	[Fact]
	public void StoppedText_WritesTheLineUnderAStoppedList()
	{
		IRenderedComponent<MokaBootScreen> cut = Render<MokaBootScreen>(p => p
			.Add(x => x.BootVariant, MokaBootVariant.Steps)
			.Add(x => x.Steps, Stopped)
			.Add(x => x.StoppedText, (number, total) => $"Stannade vid steg {number} av {total}"));

		Assert.Equal("Stannade vid steg 3 av 5", cut.Find(".moka-boot-screen__stopped").TextContent);
	}

	[Fact]
	public void StepStateText_NamesEachStepsStateForScreenReaders()
	{
		IRenderedComponent<MokaBootScreen> cut = Render<MokaBootScreen>(p => p
			.Add(x => x.BootVariant, MokaBootVariant.Steps)
			.Add(x => x.Steps, Startup)
			.Add(x => x.StepStateText, state => state == MokaBootStepState.Done ? "Klar" : "Pågår"));

		IReadOnlyList<IElement> labels = cut.FindAll(".moka-boot-screen__step-label .moka-visually-hidden");
		Assert.Equal("Klar: ", labels[0].TextContent);
		Assert.Equal("Pågår: ", labels[3].TextContent);
	}

	[Fact]
	public void LogWordText_WritesTheLogsWords_AndTerminalsTagsInCapitals()
	{
		static string Word(MokaBootStepState state) => state == MokaBootStepState.Done ? "klar" : "kör";

		IRenderedComponent<MokaBootScreen> log = Render<MokaBootScreen>(p => p
			.Add(x => x.BootVariant, MokaBootVariant.Log)
			.Add(x => x.Steps, Startup)
			.Add(x => x.LogWordText, Word));
		IRenderedComponent<MokaBootScreen> terminal = Render<MokaBootScreen>(p => p
			.Add(x => x.BootVariant, MokaBootVariant.Terminal)
			.Add(x => x.Steps, Startup)
			.Add(x => x.LogWordText, Word));

		Assert.Equal("klar", log.FindAll(".moka-boot-screen__entry-word")[0].TextContent);
		Assert.Equal("kör", log.FindAll(".moka-boot-screen__entry-word")[3].TextContent);
		Assert.Equal(" KLAR ", terminal.FindAll(".moka-boot-screen__term-state")[0].TextContent);
		Assert.Equal(" KÖR  ", terminal.FindAll(".moka-boot-screen__term-state")[3].TextContent);
	}

	[Fact]
	public void DurationText_WritesEveryDuration()
	{
		IRenderedComponent<MokaBootScreen> cut = Render<MokaBootScreen>(p => p
			.Add(x => x.BootVariant, MokaBootVariant.Steps)
			.Add(x => x.Steps, Startup)
			.Add(x => x.DurationText, elapsed => $"{(int)elapsed.TotalMilliseconds} millisekunder"));

		Assert.Equal("142 millisekunder", cut.FindAll(".moka-boot-screen__step-time")[0].TextContent);
	}

	private static string Clock(double seconds) => seconds.ToString("00.000", CultureInfo.CurrentCulture);

	private static string Duration(string format, double value) => string.Format(CultureInfo.CurrentCulture, format, value);

	private static string Keyframes(string file, string name)
	{
		string css = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Css", file));
		int start = css.IndexOf($"@keyframes {name} {{", StringComparison.Ordinal);
		Assert.True(start >= 0, $"No @keyframes {name} in {file}.");
		int open = css.IndexOf('{', start);
		int depth = 0;
		int end = open;
		for (int i = open; i < css.Length; i++)
		{
			depth += css[i] switch { '{' => 1, '}' => -1, _ => 0 };
			if (depth == 0)
			{
				end = i;
				break;
			}
		}

		return string.Join(' ', css[(open + 1)..end].Split((char[])['\r', '\n', '\t'], StringSplitOptions.RemoveEmptyEntries)
			.Select(s => s.Trim()));
	}
}
