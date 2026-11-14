using System.Diagnostics;
using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.Extensions.DependencyInjection;
using Moka.Red.Core.Theming;
using Moka.Red.Feedback.Extensions;
using Moka.Red.Primitives.CodeBlock;
using Moka.Red.Tests.Shared;
using Moka.Red.ThemeGen.Preview;
using Moka.Red.ThemeGen.Serialization;

namespace Moka.Red.ThemeGen.Tests.Components;

public sealed class MokaThemeStudioTests : BunitContext
{
	private readonly FakeClock _clock = new();
	private readonly List<MokaThemePair> _raised = [];

	public MokaThemeStudioTests()
	{
		JSInterop.Mode = JSRuntimeMode.Loose;
		Services.AddSingleton<TimeProvider>(_clock);
		Services.AddMokaFeedback();
		SetRendererInfo(new RendererInfo("Server", true));
	}

	[Fact]
	public void ItStarts_FromTheDefaultTheme_InBothSchemes()
	{
		IRenderedComponent<MokaThemeStudio> cut = RenderStudio();

		IReadOnlyList<IRenderedComponent<MokaThemePreview>> previews = cut.FindComponents<MokaThemePreview>();
		Assert.Equal(2, previews.Count);
		Assert.Equal(MokaTheme.Red, previews[0].Instance.PreviewTheme);
		Assert.Equal(MokaTheme.RedLight, previews[1].Instance.PreviewTheme);
		Assert.Empty(_raised);
	}

	[Fact]
	public async Task ASeed_ChangesTheThemeAndThePreview()
	{
		IRenderedComponent<MokaThemeStudio> cut = RenderStudio();

		await SeedInputs(cut)[0].InputAsync(new ChangeEventArgs { Value = "#7C3AED" });

		MokaThemePair pair = Assert.Single(_raised);
		Assert.Equal("#7C3AED", pair.Dark.Seed?.Accent);
		Assert.Equal("#7C3AED", pair.Light.Seed?.Accent);
		Assert.True(pair.Dark.IsDark);
		Assert.False(pair.Light.IsDark);
		Assert.Contains("--moka-color-primary: #7C3AED", PreviewRoots(cut)[0].GetAttribute("style"), StringComparison.Ordinal);
		Assert.Equal(pair.Light, cut.FindComponents<MokaThemePreview>()[1].Instance.PreviewTheme);
	}

	[Fact]
	public async Task AMeaningSeed_IsKept_AndCanBePutBack()
	{
		IRenderedComponent<MokaThemeStudio> cut = RenderStudio();

		await SeedInputs(cut)[2].InputAsync(new ChangeEventArgs { Value = "#00A86B" });
		Assert.Equal("#00A86B", _raised[^1].Dark.Seed?.Success);

		await cut.FindAll("button").First(button => button.TextContent.Contains(new MokaThemeStudioText().ResetSeed("Good"), StringComparison.Ordinal))
			.ClickAsync(new MouseEventArgs());
		Assert.Null(_raised[^1].Dark.Seed?.Success);
	}

	[Fact]
	public async Task PickingAStart_GivesThatPresetsPair()
	{
		IRenderedComponent<MokaThemeStudio> cut = RenderStudio();

		await cut.Find("input[type='radio'][value='gate']").ChangeAsync(new ChangeEventArgs { Value = "gate" });

		MokaThemePair pair = Assert.Single(_raised);
		Assert.Equal(MokaTheme.Gate, pair.Dark);
		Assert.Equal(MokaTheme.GateLight, pair.Light);
	}

	[Fact]
	public async Task AStyleChoice_ChangesBothSchemes()
	{
		IRenderedComponent<MokaThemeStudio> cut = RenderStudio();

		await cut.PickAsync("Tight");

		MokaThemePair pair = Assert.Single(_raised);
		Assert.Equal(MokaTheme.Red.WithCorners(MokaCorners.Tight), pair.Dark);
		Assert.Equal(MokaTheme.RedLight.WithCorners(MokaCorners.Tight), pair.Light);
	}

	[Fact]
	public async Task HighContrast_AppliesToBothSchemes()
	{
		IRenderedComponent<MokaThemeStudio> cut = RenderStudio();

		await cut.Find(".moka-theme-studio__controls input[role='switch']").ChangeAsync(new ChangeEventArgs { Value = true });

		MokaThemePair pair = Assert.Single(_raised);
		Assert.True(pair.Dark.HighContrast);
		Assert.True(pair.Light.HighContrast);
	}

	[Fact]
	public async Task APin_Applies_AndResets()
	{
		IRenderedComponent<MokaThemeStudio> cut = RenderStudio();
		await OpenTabAsync(cut, new MokaThemeStudioText().TokensTab);

		await PinAsync(cut, "--moka-radius-card", "6px");
		Assert.Equal("6px", _raised[^1].Dark.Pins["--moka-radius-card"]);
		Assert.Equal("6px", _raised[^1].Light.Pins["--moka-radius-card"]);
		Assert.Contains("--moka-radius-card: 6px", PreviewRoots(cut)[1].GetAttribute("style"), StringComparison.Ordinal);

		await cut.Find("button[aria-label='Reset --moka-radius-card']").ClickAsync(new MouseEventArgs());
		Assert.Empty(_raised[^1].Dark.Pins);
		Assert.Empty(_raised[^1].Light.Pins);
	}

	[Fact]
	public async Task AColorPin_HoldsInTheSchemeInspectedOnly()
	{
		IRenderedComponent<MokaThemeStudio> cut = RenderStudio();
		await OpenTabAsync(cut, new MokaThemeStudioText().TokensTab);

		await PinAsync(cut, "--moka-color-surface", "#101418");

		Assert.Equal("#101418", _raised[^1].Dark.Pins["--moka-color-surface"]);
		Assert.False(_raised[^1].Light.Pins.ContainsKey("--moka-color-surface"));
	}

	[Fact]
	public async Task APin_ThatTheTokenDoesntTake_SaysSo()
	{
		IRenderedComponent<MokaThemeStudio> cut = RenderStudio();
		await OpenTabAsync(cut, new MokaThemeStudioText().TokensTab);

		await PinAsync(cut, "--moka-radius-card", "red");

		Assert.Empty(_raised);
		Assert.Equal(new MokaThemeStudioText().PinInvalid, cut.Find(".moka-theme-studio__pin-editor .moka-field-helper--error").TextContent.Trim());
	}

	[Fact]
	public void TheContrastTab_ListsTheReport_FailuresFirst()
	{
		MokaTheme theme = MokaTheme.Red.WithToken("--moka-color-on-surface-2", "#3A3A3A");
		IRenderedComponent<MokaThemeStudio> cut = RenderStudio(p => p.Add(x => x.Theme, theme));

		MokaContrastReport report = theme.CheckContrast();
		IReadOnlyList<IElement> rows = cut.FindAll(".moka-theme-studio__check");
		int failing = report.Failures.Count();
		Assert.Equal(report.Checks.Count, rows.Count);
		Assert.True(failing > 0);
		Assert.All(rows.Take(failing), row => Assert.Contains("moka-theme-studio__check--fail", row.ClassName, StringComparison.Ordinal));
		Assert.All(rows.Skip(failing), row => Assert.DoesNotContain("--fail", row.ClassName, StringComparison.Ordinal));
		Assert.Contains("Lc", rows[0].TextContent, StringComparison.Ordinal);
	}

	[Fact]
	public async Task TheExportTab_RoundTripsTheInspectedTheme()
	{
		IRenderedComponent<MokaThemeStudio> cut = RenderStudio();
		await SeedInputs(cut)[0].InputAsync(new ChangeEventArgs { Value = "#0EA5E9" });
		await OpenTabAsync(cut, new MokaThemeStudioText().ExportTab);

		await cut.PickAsync("JSON");

		Assert.Equal(_raised[^1].Dark, MokaThemeSerializer.FromJson(cut.FindComponent<MokaCodeBlock>().Instance.Code));
	}

	[Fact]
	public async Task TheCSharpExport_OfAStudioTheme_IsAWithChainThatCompiles()
	{
		IRenderedComponent<MokaThemeStudio> cut = RenderStudio();
		await SeedInputs(cut)[1].InputAsync(new ChangeEventArgs { Value = "#7A8494" });
		await cut.PickAsync("Tight");
		await OpenTabAsync(cut, new MokaThemeStudioText().TokensTab);
		await PinAsync(cut, "--moka-radius-card", "6px");
		await OpenTabAsync(cut, new MokaThemeStudioText().ExportTab);

		string code = cut.FindComponent<MokaCodeBlock>().Instance.Code;

		Assert.DoesNotContain("Palette = new", code, StringComparison.Ordinal);
		Assert.Contains(".WithCorners(MokaCorners.Tight)", code, StringComparison.Ordinal);
		MokaTheme restored = await CSharpScript.EvaluateAsync<MokaTheme>(code, ScriptOptions.Default
			.AddReferences(typeof(MokaTheme).Assembly)
			.AddImports("Moka.Red.Core.Theming"), cancellationToken: Xunit.TestContext.Current.CancellationToken);
		Assert.Equal(_raised[^1].Dark, restored);
	}

	[Fact]
	public async Task Importing_ReplacesTheTheme()
	{
		MokaTheme imported = MokaTheme.FromSeed(new MokaThemeSeed { Accent = "#E11D48" }, false).WithTitleFont(MokaTitleFont.Inter);
		IRenderedComponent<MokaThemeStudio> cut = RenderStudio();
		await OpenTabAsync(cut, new MokaThemeStudioText().ExportTab);

		await cut.Find("textarea").InputAsync(new ChangeEventArgs { Value = MokaThemeSerializer.ToJson(imported) });
		await cut.FindAll("button").Last(button => button.TextContent.Trim() == "Apply").ClickAsync(new MouseEventArgs());

		Assert.Equal(imported, _raised[^1].Light);
		Assert.Equal(imported.WithDark(true), _raised[^1].Dark);
	}

	[Fact]
	public async Task ASingleBoundTheme_ComesBackInItsOwnScheme()
	{
		MokaTheme? changed = null;
		IRenderedComponent<MokaThemeStudio> cut = RenderStudio(p => p
			.Add(x => x.Theme, MokaTheme.GateLight)
			.Add(x => x.ThemeChanged, EventCallback.Factory.Create<MokaTheme>(this, theme => changed = theme)));

		await cut.PickAsync("Tight");

		Assert.Equal(MokaTheme.GateLight.WithCorners(MokaCorners.Tight), changed);
	}

	[Fact]
	public void AThemeMatchingNoStart_ShowsFirstAsTheCurrentTheme()
	{
		MokaTheme theme = MokaTheme.FromSeed(new MokaThemeSeed { Accent = "#16A34A" }, true);
		IRenderedComponent<MokaThemeStudio> cut = RenderStudio(p => p.Add(x => x.Theme, theme));

		Assert.Equal(theme, cut.FindComponents<MokaThemePreview>()[0].Instance.PreviewTheme);
		Assert.Contains(new MokaThemeStudioText().CurrentStart, cut.Find(".moka-theme-studio__controls").TextContent, StringComparison.Ordinal);
	}

	[Fact]
	public async Task TheView_ShowsOneScheme_OrBoth()
	{
		IRenderedComponent<MokaThemeStudio> cut = RenderStudio();

		await cut.PickAsync("Light");

		IRenderedComponent<MokaThemePreview> preview = Assert.Single(cut.FindComponents<MokaThemePreview>());
		Assert.False(preview.Instance.PreviewTheme.IsDark);
	}

	// A slider fires on every step of a drag. Each value is queued without a render, and the theme is built once a frame.
	[Fact]
	public void ANeutralStrengthDrag_RebuildsOncePerFrame_AndRendersNothingInBetween()
	{
		IRenderedComponent<MokaThemeStudio> cut = RenderStudio();
		IRenderedComponent<MokaThemePreview> dark = cut.FindComponents<MokaThemePreview>()[0];
		int studioRenders = StudioRenders(cut);
		int previewRenders = dark.RenderCount;

		for (int step = 1; step <= 30; step++)
		{
			cut.Find(".moka-slider-input").Input(FormattableString.Invariant($"{step * 0.002:0.000}"));
		}

		Assert.Empty(_raised);
		Assert.Equal(studioRenders, StudioRenders(cut));
		Assert.Equal(previewRenders, dark.RenderCount);

		_clock.Advance(TimeSpan.FromMilliseconds(16));

		cut.WaitForAssertion(() => Assert.Single(_raised));
		Assert.Equal(0.06, _raised[0].Dark.Seed!.NeutralChroma!.Value, 6);
		Assert.Equal(studioRenders + 1, StudioRenders(cut));
		Assert.True(dark.RenderCount > previewRenders);
	}

	[Fact]
	public void AClickAfterADrag_TakesTheQueuedValueFirst()
	{
		IRenderedComponent<MokaThemeStudio> cut = RenderStudio();

		cut.Find(".moka-slider-input").Input("0.04");
		cut.Segment("Tight").Change(new ChangeEventArgs { Value = "on" });

		MokaThemePair pair = Assert.Single(_raised);
		Assert.Equal(0.04, pair.Dark.Seed!.NeutralChroma!.Value, 6);
		_clock.Advance(TimeSpan.FromMilliseconds(32));
		Assert.Single(_raised);
	}

	[Fact]
	public async Task SwitchingTabs_LeavesThePreviewsAlone()
	{
		IRenderedComponent<MokaThemeStudio> cut = RenderStudio();
		IRenderedComponent<MokaThemePreview> dark = cut.FindComponents<MokaThemePreview>()[0];
		int renders = dark.RenderCount;

		await OpenTabAsync(cut, new MokaThemeStudioText().TokensTab);
		await OpenTabAsync(cut, new MokaThemeStudioText().ExportTab);

		Assert.Equal(renders, dark.RenderCount);
	}

	// Inside the app's own provider a preview writes only the tokens that differ from the app's theme.
	[Fact]
	public async Task InsideAnAppsProvider_APreviewWritesOnlyWhatDiffers()
	{
		IRenderedComponent<MokaThemeProvider> host = Render<MokaThemeProvider>(p => p
			.Add(x => x.Theme, MokaTheme.Red)
			.AddChildContent<MokaThemeStudio>(studio => studio
				.Add(x => x.ThemesChanged, EventCallback.Factory.Create<MokaThemePair>(this, _raised.Add))));

		Assert.Null(host.Find(".moka-theme-studio__preview .moka-root").GetAttribute("style"));

		await host.FindAll(".moka-color-input__box--split input")[0].InputAsync(new ChangeEventArgs { Value = "#7C3AED" });

		string written = host.Find(".moka-theme-studio__preview .moka-root").GetAttribute("style") ?? "";
		int declarations = written.Split(';', StringSplitOptions.RemoveEmptyEntries).Length;
		Assert.Contains("--moka-color-primary: #7C3AED", written, StringComparison.Ordinal);
		Assert.InRange(declarations, 1, MokaTokens.All.Count - 1);
	}

	// Not a pass or fail on time, which depends on the machine: it writes what a theme change costs, for the record.
	[Fact]
	public void OneChange_CostsOneStudioRender_AndRedrawsEachPreviewTheSameWay()
	{
		IRenderedComponent<MokaThemeStudio> cut = RenderStudio();
		IReadOnlyList<IRenderedComponent<MokaThemePreview>> previews = cut.FindComponents<MokaThemePreview>();
		int studio = StudioRenders(cut);
		int[] before = [previews[0].RenderCount, previews[1].RenderCount];
		const int changes = 20;

		Stopwatch clock = Stopwatch.StartNew();
		for (int i = 1; i <= changes; i++)
		{
			cut.Find(".moka-slider-input").Input(FormattableString.Invariant($"{i * 0.004:0.000}"));
			_clock.Advance(TimeSpan.FromMilliseconds(16));
			cut.WaitForAssertion(() => Assert.Equal(i, _raised.Count));
		}

		clock.Stop();
		int darkBatches = previews[0].RenderCount - before[0];
		Assert.Equal(studio + changes, StudioRenders(cut));
		Assert.Equal(0, darkBatches % changes);
		Assert.Equal(darkBatches, previews[1].RenderCount - before[1]);

		Stopwatch build = Stopwatch.StartNew();
		for (int i = 0; i < changes; i++)
		{
			MokaThemePair pair = MokaThemePair.From(MokaTheme.Red.WithSeed(new MokaThemeSeed { Accent = "#7C3AED", NeutralChroma = i * 0.004 }));
			_ = pair.Dark.ToCssVariables();
			_ = pair.Light.ToCssVariables();
		}

		build.Stop();
		Xunit.TestContext.Current.TestOutputHelper?.WriteLine(FormattableString.Invariant(
			$"{changes} changes: {clock.Elapsed.TotalMilliseconds / changes:0.0} ms each in bUnit (build of both schemes, studio render, both previews redrawn in {darkBatches / changes} batch(es) each); a build of both schemes with their tokens alone: {build.Elapsed.TotalMilliseconds / changes:0.00} ms"));
	}

	// The studio's first heading takes a render fragment, which counts as changed on every render, so it renders exactly when
	// the studio does: bUnit's count on the studio itself also counts the renders of everything inside it.
	private static int StudioRenders(IRenderedComponent<MokaThemeStudio> cut) =>
		cut.FindComponents<Moka.Red.Primitives.Typography.MokaHeading>()[0].RenderCount;

	private IRenderedComponent<MokaThemeStudio> RenderStudio(Action<ComponentParameterCollectionBuilder<MokaThemeStudio>>? parameters = null) =>
		Render<MokaThemeStudio>(p =>
		{
			p.Add(x => x.ThemesChanged, EventCallback.Factory.Create<MokaThemePair>(this, _raised.Add));
			parameters?.Invoke(p);
		});

	private static IReadOnlyList<IElement> SeedInputs(IRenderedComponent<MokaThemeStudio> cut) => cut.FindAll(".moka-color-input__box--split input");

	private static IReadOnlyList<IElement> PreviewRoots(IRenderedComponent<MokaThemeStudio> cut) => cut.FindAll(".moka-theme-studio__preview .moka-root");

	private static Task OpenTabAsync(IRenderedComponent<MokaThemeStudio> cut, string tab) =>
		cut.FindAll("[role='tab']").First(element => element.TextContent.Trim() == tab).ClickAsync(new MouseEventArgs());

	private static async Task PinAsync(IRenderedComponent<MokaThemeStudio> cut, string token, string value)
	{
		await cut.Find($"button[aria-label='Pin {token}']").ClickAsync(new MouseEventArgs());
		await cut.Find(".moka-theme-studio__pin-editor input").InputAsync(new ChangeEventArgs { Value = value });
		await cut.FindAll(".moka-theme-studio__pin-editor button").First(button => button.TextContent.Trim() == new MokaThemeStudioText().ApplyPin)
			.ClickAsync(new MouseEventArgs());
	}
}
