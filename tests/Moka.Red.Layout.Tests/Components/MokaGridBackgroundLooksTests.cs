using System.Text.RegularExpressions;
using System.Xml.Linq;
using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Enums;
using Moka.Red.Layout.GridBackground;
using Moka.Red.Tests.Shared;

namespace Moka.Red.Layout.Tests.Components;

// The looks BC picked from the grid background board: twelve patterns, where the pattern fades, the glow, motion
// and tone. Left unset, every one of them keeps the original markup.
public sealed partial class MokaGridBackgroundLooksTests : BunitContext
{
	private const string Css = "MokaGridBackground.razor.css";
	private const string Module = "./_content/Moka.Red.Layout/GridBackground/MokaGridBackground.razor.js";
	private const string CenterFade = "radial-gradient(ellipse at center, black 30%, transparent 80%)";
	private const string PrimaryBorder = "var(--moka-color-primary-border)";

	private static readonly string[] TileElements =
		["svg", "line", "polygon", "path", "circle", "rect", "filter", "feTurbulence", "feColorMatrix"];

	private static readonly string[] MaskPaints = ["black", "none"];

	private static readonly MokaGridPattern[] NewPatterns =
	[
		MokaGridPattern.GraphPaper, MokaGridPattern.Isometric, MokaGridPattern.Triangles, MokaGridPattern.Diamonds,
		MokaGridPattern.Checker, MokaGridPattern.Rows, MokaGridPattern.Columns, MokaGridPattern.Circuit,
		MokaGridPattern.Topographic, MokaGridPattern.Radar, MokaGridPattern.CornerMarks, MokaGridPattern.Grain
	];

	public static TheoryData<MokaGridPattern> EveryNewPattern => [.. NewPatterns];

	public static TheoryData<MokaGridPattern> SvgPatterns =>
	[
		MokaGridPattern.Dashed, MokaGridPattern.Cross, MokaGridPattern.Honeycomb, MokaGridPattern.Isometric,
		MokaGridPattern.Triangles, MokaGridPattern.Diamonds, MokaGridPattern.Circuit, MokaGridPattern.Topographic,
		MokaGridPattern.CornerMarks, MokaGridPattern.Grain
	];

	public static TheoryData<MokaGridPattern, string> HostileColorsForNewPatterns
	{
		get
		{
			TheoryData<MokaGridPattern, string> data = [];
			foreach (MokaGridPattern pattern in NewPatterns)
			{
				data.Add(pattern, "red' onload='alert(1)");
				data.Add(pattern, "red) 1px, url(https://example.com/track.png");
				data.Add(pattern, "red; position: fixed; inset: 0");
			}

			return data;
		}
	}

	private IRenderedComponent<MokaGridBackground> Grid(Action<ComponentParameterCollectionBuilder<MokaGridBackground>> parameters) =>
		Render(parameters);

	private static string Style(IElement element) => element.GetAttribute("style") ?? "";

	private static IElement Pattern(IRenderedComponent<MokaGridBackground> cut) => cut.Find(".moka-grid-bg__pattern");

	private static string Root(IRenderedComponent<MokaGridBackground> cut) =>
		cut.Find(".moka-grid-bg").GetAttribute("class") ?? "";

	private static string WithoutScope(string markup) => Scope().Replace(markup, "");

	private static XDocument Tile(string style)
	{
		Match match = TileUrl().Match(style);
		Assert.True(match.Success, "The style holds no SVG tile.");
		return XDocument.Parse(Uri.UnescapeDataString(match.Groups["svg"].Value));
	}

	[Fact]
	public void TheDefaults_KeepTheOriginalMarkup()
	{
		IRenderedComponent<MokaGridBackground> cut = Grid(_ => { });

		Assert.Equal(
			"<div class=\"moka-grid-bg\"><div class=\"moka-grid-bg__pattern\" style=\"background-image: "
			+ "linear-gradient(to right, var(--moka-color-primary-border) 1.0px, transparent 1.0px), "
			+ "linear-gradient(to bottom, var(--moka-color-primary-border) 1.0px, transparent 1.0px); "
			+ $"background-size: 40px 40px; opacity: 0.70; -webkit-mask-image: {CenterFade}; mask-image: {CenterFade}\">"
			+ "</div><div class=\"moka-grid-bg__content\"></div></div>",
			WithoutScope(cut.Markup));
	}

	[Theory]
	[InlineData(MokaGridPattern.Lines)]
	[InlineData(MokaGridPattern.Dots)]
	[InlineData(MokaGridPattern.DiagonalLines)]
	public void TheGradientPatterns_KeepTheirSingleLayer(MokaGridPattern pattern)
	{
		IRenderedComponent<MokaGridBackground> cut = Grid(p => p
			.Add(x => x.Pattern, pattern)
			.Add(x => x.Highlighted, true)
			.Add(x => x.Motion, MokaGridMotion.Pulse)
			.Add(x => x.Tone, MokaGridTone.Neutral)
			.Add(x => x.Fade, MokaGridFade.Frame));

		Assert.Empty(cut.FindAll(".moka-grid-bg__fade"));
		Assert.StartsWith("background-image: ", Style(Pattern(cut)), StringComparison.Ordinal);
		Assert.Contains("mask-image: radial-gradient(ellipse at center, transparent 32%, black 72%)", Style(Pattern(cut)),
			StringComparison.Ordinal);
		Assert.Empty(JSInterop.Invocations);
	}

	[Theory]
	[MemberData(nameof(EveryNewPattern))]
	public void ANewPattern_IsAMaskOverTheColor_InsideTheFade(MokaGridPattern pattern)
	{
		IRenderedComponent<MokaGridBackground> cut = Grid(p => p.Add(x => x.Pattern, pattern));

		IElement fade = cut.Find(".moka-grid-bg > .moka-grid-bg__fade");
		Assert.Equal($"-webkit-mask-image: {CenterFade}; mask-image: {CenterFade}", Style(fade));
		string style = Style(fade.QuerySelector(".moka-grid-bg__pattern")!);
		Assert.StartsWith("background-color: ", style, StringComparison.Ordinal);
		Assert.Contains("; mask-image: ", style, StringComparison.Ordinal);
		Assert.DoesNotContain("background-image", style, StringComparison.Ordinal);
		Assert.Equal("moka-grid-bg", Root(cut));
		Assert.Empty(JSInterop.Invocations);
	}

	[Theory]
	[MemberData(nameof(SvgPatterns))]
	public void ATile_IsWellFormed_AndHoldsNoColor(MokaGridPattern pattern)
	{
		IRenderedComponent<MokaGridBackground> cut = Grid(p => p
			.Add(x => x.Pattern, pattern)
			.Add(x => x.PatternColor, "#42a5f5"));

		XDocument svg = Tile(Style(Pattern(cut)));

		foreach (XElement element in svg.Descendants())
		{
			Assert.Contains(element.Name.LocalName, TileElements);
			foreach (string paint in new[] { "stroke", "fill" })
			{
				if (element.Attribute(paint)?.Value is { } value)
				{
					Assert.Contains(value, MaskPaints);
				}
			}
		}

		Assert.StartsWith("background-color: #42a5f5;", Style(Pattern(cut)), StringComparison.Ordinal);
	}

	[Theory]
	[MemberData(nameof(HostileColorsForNewPatterns))]
	public void AHostileColor_StaysOutOfTheNewPatterns(MokaGridPattern pattern, string color)
	{
		string expected = Style(Pattern(Grid(p => p.Add(x => x.Pattern, pattern))));

		string style = Style(Pattern(Grid(p => p.Add(x => x.Pattern, pattern).Add(x => x.PatternColor, color))));

		Assert.Equal(expected, style);
		Assert.DoesNotContain("example.com", style, StringComparison.Ordinal);
	}

	[Theory]
	[InlineData(MokaGridPattern.GraphPaper, "mask-size: 16px 16px")]
	[InlineData(MokaGridPattern.Checker, "mask-size: 48px 48px")]
	[InlineData(MokaGridPattern.Rows, "mask-size: 100% 28px")]
	[InlineData(MokaGridPattern.Columns, "mask-size: 48px 100%")]
	[InlineData(MokaGridPattern.Radar, "transparent 0 27px, black 27px 28px")]
	public void AGradientPattern_HasItsOwnCellSize(MokaGridPattern pattern, string expected)
	{
		Assert.Contains(expected, Style(Pattern(Grid(p => p.Add(x => x.Pattern, pattern)))), StringComparison.Ordinal);
	}

	[Theory]
	[InlineData(MokaGridPattern.Isometric, "48", "27.71")]
	[InlineData(MokaGridPattern.Triangles, "32", "55.42")]
	[InlineData(MokaGridPattern.Diamonds, "48", "28")]
	[InlineData(MokaGridPattern.Circuit, "96", "96")]
	[InlineData(MokaGridPattern.CornerMarks, "48", "48")]
	[InlineData(MokaGridPattern.Grain, "160", "160")]
	public void AnSvgPattern_HasItsOwnTileSize(MokaGridPattern pattern, string width, string height)
	{
		XElement svg = Tile(Style(Pattern(Grid(p => p.Add(x => x.Pattern, pattern))))).Root!;

		Assert.Equal(width, svg.Attribute("width")?.Value);
		Assert.Equal(height, svg.Attribute("height")?.Value);
	}

	[Fact]
	public void CellSize_ScalesTheNewPatterns()
	{
		XElement corners = Tile(Style(Pattern(Grid(p => p
			.Add(x => x.Pattern, MokaGridPattern.CornerMarks)
			.Add(x => x.CellSize, 96))))).Root!;
		string rows = Style(Pattern(Grid(p => p.Add(x => x.Pattern, MokaGridPattern.Rows).Add(x => x.CellSize, 20))));

		Assert.Equal("96", corners.Attribute("width")?.Value);
		Assert.Equal("0 0 48 48", corners.Attribute("viewBox")?.Value);
		Assert.Equal("0.5", corners.Descendants().Single(e => e.Name.LocalName == "path").Attribute("stroke-width")?.Value);
		Assert.Contains("mask-size: 100% 20px", rows, StringComparison.Ordinal);
	}

	[Theory]
	[InlineData(5, 0, "80px 80px")]
	[InlineData(4, 0, "64px 64px")]
	[InlineData(3, 10, "30px 30px")]
	[InlineData(1, 0, "80px 80px")]
	public void GraphPaper_DrawsAStrongerLineEveryFewCells(int majorEvery, int cellSize, string majorSize)
	{
		IRenderedComponent<MokaGridBackground> cut = Grid(p => p
			.Add(x => x.Pattern, MokaGridPattern.GraphPaper)
			.Add(x => x.MajorEvery, majorEvery)
			.Add(x => x.CellSize, cellSize));

		string major = Style(cut.Find(".moka-grid-bg__fade > .moka-grid-bg__major"));
		Assert.StartsWith("background-color: color-mix(in srgb, var(--moka-color-primary) 46%, transparent);", major,
			StringComparison.Ordinal);
		Assert.Contains($"; mask-size: {majorSize};", major, StringComparison.Ordinal);
		Assert.StartsWith("background-color: color-mix(in srgb, var(--moka-color-primary) 22%, transparent);",
			Style(Pattern(cut)), StringComparison.Ordinal);
	}

	[Fact]
	public void OnlyGraphPaper_HasMajorLines()
	{
		foreach (MokaGridPattern pattern in NewPatterns.Where(p => p != MokaGridPattern.GraphPaper))
		{
			Assert.Empty(Grid(p => p.Add(x => x.Pattern, pattern)).FindAll(".moka-grid-bg__major"));
		}
	}

	[Fact]
	public void Topographic_CoversTheBox_WithAStrokeThatDoesNotScale()
	{
		string style = Style(Pattern(Grid(p => p.Add(x => x.Pattern, MokaGridPattern.Topographic))));

		Assert.Contains("; mask-size: cover;", style, StringComparison.Ordinal);
		Assert.Contains("; mask-repeat: no-repeat;", style, StringComparison.Ordinal);
		Assert.Contains("; mask-position: center;", style, StringComparison.Ordinal);
		XElement path = Tile(style).Descendants().Single(e => e.Name.LocalName == "path");
		Assert.Equal("non-scaling-stroke", path.Attribute("vector-effect")?.Value);
		Assert.Equal(17, path.Attribute("d")!.Value.Count(c => c == 'z'));
	}

	[Fact]
	public void DotRadius_ScalesTheDiamondDotsAndTheCircuitPads()
	{
		XDocument diamonds = Tile(Style(Pattern(Grid(p => p
			.Add(x => x.Pattern, MokaGridPattern.Diamonds)
			.Add(x => x.DotRadius, 2)))));
		XDocument circuit = Tile(Style(Pattern(Grid(p => p.Add(x => x.Pattern, MokaGridPattern.Circuit)))));

		Assert.All(diamonds.Descendants().Where(e => e.Name.LocalName == "circle"),
			c => Assert.Equal("2.6", c.Attribute("r")?.Value));
		Assert.All(circuit.Descendants().Where(e => e.Name.LocalName == "circle"),
			c => Assert.Equal("2.2", c.Attribute("r")?.Value));
	}

	[Theory]
	[InlineData(MokaGridPattern.Checker, "1.00")]
	[InlineData(MokaGridPattern.CornerMarks, "0.90")]
	[InlineData(MokaGridPattern.Grain, "0.75")]
	[InlineData(MokaGridPattern.Isometric, "0.70")]
	public void APattern_HasItsOwnStrength_UntilPatternOpacityIsSet(MokaGridPattern pattern, string opacity)
	{
		Assert.EndsWith($"opacity: {opacity}", Style(Pattern(Grid(p => p.Add(x => x.Pattern, pattern)))),
			StringComparison.Ordinal);
		Assert.EndsWith("opacity: 0.40", Style(Pattern(Grid(p => p
			.Add(x => x.Pattern, pattern)
			.Add(x => x.PatternOpacity, 0.4)))), StringComparison.Ordinal);
	}

	[Theory]
	[InlineData(MokaGridFade.Center, CenterFade)]
	[InlineData(MokaGridFade.Bottom, "linear-gradient(to bottom, black 5%, transparent 92%)")]
	[InlineData(MokaGridFade.Top, "linear-gradient(to top, black 5%, transparent 92%)")]
	[InlineData(MokaGridFade.Frame, "radial-gradient(ellipse at center, transparent 32%, black 72%)")]
	public void AFade_ShapesTheMask(MokaGridFade fade, string mask)
	{
		string lines = Style(Pattern(Grid(p => p.Add(x => x.Fade, fade).Add(x => x.FadeEdges, false))));
		string dashed = Style(Grid(p => p
			.Add(x => x.Pattern, MokaGridPattern.Dashed)
			.Add(x => x.Fade, fade)).Find(".moka-grid-bg__fade"));

		Assert.EndsWith($"-webkit-mask-image: {mask}; mask-image: {mask}", lines, StringComparison.Ordinal);
		Assert.Equal($"-webkit-mask-image: {mask}; mask-image: {mask}", dashed);
	}

	[Fact]
	public void NoFade_LeavesTheMaskOut_AndFadeWinsOverFadeEdges()
	{
		string none = Style(Pattern(Grid(p => p.Add(x => x.Fade, MokaGridFade.None))));
		string edgesOff = Style(Pattern(Grid(p => p.Add(x => x.FadeEdges, false))));
		IRenderedComponent<MokaGridBackground> layered = Grid(p => p
			.Add(x => x.Pattern, MokaGridPattern.Grain)
			.Add(x => x.Fade, MokaGridFade.None));

		Assert.DoesNotContain("mask-image", none, StringComparison.Ordinal);
		Assert.Equal(none, edgesOff);
		Assert.Null(layered.Find(".moka-grid-bg__fade").GetAttribute("style"));
	}

	[Fact]
	public void FadeMask_WinsOverEveryFade()
	{
		IRenderedComponent<MokaGridBackground> cut = Grid(p => p
			.Add(x => x.Fade, MokaGridFade.Spotlight)
			.Add(x => x.FadeMask, "linear-gradient(black, transparent)"));

		Assert.EndsWith("mask-image: linear-gradient(black, transparent)", Style(Pattern(cut)), StringComparison.Ordinal);
		Assert.DoesNotContain("moka-grid-bg--spotlight", Root(cut), StringComparison.Ordinal);
		Assert.Empty(JSInterop.Invocations);
	}

	[Fact]
	public void Spotlight_FollowsThePointerThroughTheScript()
	{
		BunitJSModuleInterop module = JSInterop.SetupModule(Module);
		module.SetupVoid("bindSpotlight", _ => true).SetVoidResult();

		IRenderedComponent<MokaGridBackground> cut = Grid(p => p.Add(x => x.Fade, MokaGridFade.Spotlight));

		Assert.Contains("moka-grid-bg--spotlight", Root(cut), StringComparison.Ordinal);
		string fade = Style(cut.Find(".moka-grid-bg > .moka-grid-bg__fade"));
		Assert.Contains("radial-gradient(circle 150px at var(--_gb-x, 50%) var(--_gb-y, 50%), black 0 35%", fade,
			StringComparison.Ordinal);
		JSRuntimeInvocation bind = Assert.Single(module.Invocations["bindSpotlight"]);
		Assert.IsType<ElementReference>(bind.Arguments[0]);
	}

	[Fact]
	public async Task Spotlight_LetsGoWhenTurnedOff_OrDisposed()
	{
		BunitJSModuleInterop module = JSInterop.SetupModule(Module);
		module.SetupVoid("bindSpotlight", _ => true).SetVoidResult();
		module.SetupVoid("unbindSpotlight", _ => true).SetVoidResult();

		IRenderedComponent<MokaGridBackground> cut = Grid(p => p.Add(x => x.Fade, MokaGridFade.Spotlight));
		cut.Render(p => p.Add(x => x.Fade, MokaGridFade.Bottom));
		cut.Render(p => p.Add(x => x.Fade, MokaGridFade.Spotlight));
		await cut.Instance.DisposeAsync();

		Assert.Equal(2, module.Invocations["bindSpotlight"].Count);
		Assert.Equal(2, module.Invocations["unbindSpotlight"].Count);
		Assert.DoesNotContain("moka-grid-bg--spotlight", Root(Grid(p => p.Add(x => x.Fade, MokaGridFade.Bottom))),
			StringComparison.Ordinal);
	}

	[Fact]
	public void NoGlow_IsDrawnByDefault()
	{
		Assert.Empty(Grid(_ => { }).FindAll(".moka-grid-bg__highlight"));
	}

	[Fact]
	public void AnUnsetGlow_KeepsHighlighted_AndAGlowWinsWithoutIt()
	{
		string highlighted = Style(Grid(p => p.Add(x => x.Highlighted, true)).Find(".moka-grid-bg__highlight"));
		string center = Style(Grid(p => p.Add(x => x.Glow, MokaGridGlow.Center)).Find(".moka-grid-bg__highlight"));

		Assert.Equal("background: radial-gradient(ellipse at center, var(--moka-color-primary-glow-strong) 0%, transparent 60%)",
			highlighted);
		Assert.Equal(highlighted, center);
	}

	[Theory]
	[InlineData(MokaGridGlow.Aurora, "radial-gradient(55% 85% at 28% -18%, color-mix(in srgb, var(--moka-color-primary) 30%, transparent), transparent 72%)")]
	[InlineData(MokaGridGlow.Aurora, "color-mix(in srgb, var(--moka-color-primary-light) 24%, transparent)")]
	[InlineData(MokaGridGlow.Corner, "radial-gradient(58% 105% at 100% 0%, color-mix(in srgb, var(--moka-color-primary) 30%, transparent), transparent 72%)")]
	[InlineData(MokaGridGlow.Beam, "radial-gradient(70% 40% at 50% 46%, var(--moka-color-primary-glow), transparent 100%)")]
	[InlineData(MokaGridGlow.TwoTone, "radial-gradient(42% 75% at 66% 58%, color-mix(in srgb, var(--moka-color-info) 26%, transparent), transparent 72%)")]
	[InlineData(MokaGridGlow.Horizon, "linear-gradient(to top, var(--moka-color-primary-glow), transparent 45%)")]
	public void AGlow_IsDrawnFromTheThemeTokens(MokaGridGlow glow, string layer)
	{
		string style = Style(Grid(p => p.Add(x => x.Glow, glow)).Find(".moka-grid-bg__highlight"));

		Assert.StartsWith("background: ", style, StringComparison.Ordinal);
		Assert.Contains(layer, style, StringComparison.Ordinal);
	}

	[Fact]
	public void HighlightColor_ReplacesTheAccent_AndTwoToneKeepsItsInfoHalf()
	{
		string aurora = Style(Grid(p => p
			.Add(x => x.Glow, MokaGridGlow.Aurora)
			.Add(x => x.HighlightColor, "#42a5f5")).Find(".moka-grid-bg__highlight"));
		string twoTone = Style(Grid(p => p
			.Add(x => x.Glow, MokaGridGlow.TwoTone)
			.Add(x => x.HighlightColor, "#42a5f5")).Find(".moka-grid-bg__highlight"));

		Assert.DoesNotContain("--moka-color-primary", aurora, StringComparison.Ordinal);
		Assert.Contains("color-mix(in srgb, #42a5f5 24%, transparent)", aurora, StringComparison.Ordinal);
		Assert.Contains("color-mix(in srgb, #42a5f5 12%, transparent)", aurora, StringComparison.Ordinal);
		Assert.Contains("color-mix(in srgb, #42a5f5 26%, transparent)", twoTone, StringComparison.Ordinal);
		Assert.Contains("var(--moka-color-info) 26%", twoTone, StringComparison.Ordinal);
	}

	[Fact]
	public void AHostileHighlightColor_FallsBackToTheTokens()
	{
		string hostile = Style(Grid(p => p
			.Add(x => x.Glow, MokaGridGlow.Corner)
			.Add(x => x.HighlightColor, "red) 1px, url(https://example.com/track.png")).Find(".moka-grid-bg__highlight"));

		Assert.Equal(
			"background: radial-gradient(58% 105% at 100% 0%, color-mix(in srgb, var(--moka-color-primary) 30%, transparent), transparent 72%)",
			hostile);
	}

	[Fact]
	public void HighlightRadius_ScalesHowFarAGlowReaches()
	{
		string corner = Style(Grid(p => p
			.Add(x => x.Glow, MokaGridGlow.Corner)
			.Add(x => x.HighlightRadius, 120)).Find(".moka-grid-bg__highlight"));

		Assert.EndsWith("transparent 144%)", corner, StringComparison.Ordinal);
	}

	[Fact]
	public void NoMotion_AddsNoClass()
	{
		Assert.Equal("moka-grid-bg", Root(Grid(p => p.Add(x => x.Motion, MokaGridMotion.None))));
	}

	[Fact]
	public void Drift_MovesTheLinesUnderAFadeThatStays()
	{
		IRenderedComponent<MokaGridBackground> cut = Grid(p => p.Add(x => x.Motion, MokaGridMotion.Drift));

		Assert.Equal("moka-grid-bg moka-grid-bg--drift", Root(cut));
		IElement fade = cut.Find(".moka-grid-bg > .moka-grid-bg__fade");
		Assert.Equal(
			$"-webkit-mask-image: {CenterFade}; mask-image: {CenterFade}; "
			+ "--_gb-drift-x: 40px; --_gb-drift-y: 40px; --_gb-drift-time: 16s",
			Style(fade));
		Assert.StartsWith($"background-color: {PrimaryBorder}; -webkit-mask-image: linear-gradient(to right, black 1.0px",
			Style(Pattern(cut)), StringComparison.Ordinal);
	}

	[Theory]
	[InlineData(MokaGridPattern.GraphPaper, 0, "80px", "80px", "32s")]
	[InlineData(MokaGridPattern.Rows, 0, "0px", "28px", "11.2s")]
	[InlineData(MokaGridPattern.Grain, 0, "160px", "160px", "64s")]
	[InlineData(MokaGridPattern.DiagonalLines, 45, "29.7px", "0px", "11.88s")]
	[InlineData(MokaGridPattern.DiagonalLines, 0, "0px", "21px", "8.4s")]
	public void Drift_MovesOneTile(MokaGridPattern pattern, int angle, string x, string y, string time)
	{
		IElement fade = Grid(p => p
			.Add(x => x.Pattern, pattern)
			.Add(x => x.DiagonalAngle, angle)
			.Add(x => x.Motion, MokaGridMotion.Drift)).Find(".moka-grid-bg__fade");

		Assert.EndsWith($"--_gb-drift-x: {x}; --_gb-drift-y: {y}; --_gb-drift-time: {time}", Style(fade),
			StringComparison.Ordinal);
	}

	[Theory]
	[InlineData(MokaGridPattern.Topographic)]
	[InlineData(MokaGridPattern.Radar)]
	public void Drift_LeavesAPatternThatDoesNotRepeatStill(MokaGridPattern pattern)
	{
		IRenderedComponent<MokaGridBackground> cut = Grid(p => p
			.Add(x => x.Pattern, pattern)
			.Add(x => x.Motion, MokaGridMotion.Drift));

		Assert.Equal("moka-grid-bg", Root(cut));
		Assert.DoesNotContain("--_gb-drift", Style(cut.Find(".moka-grid-bg__fade")), StringComparison.Ordinal);
	}

	[Fact]
	public void Pulse_MarksTheRoot_AndLeavesThePatternAsItWas()
	{
		IRenderedComponent<MokaGridBackground> cut = Grid(p => p
			.Add(x => x.Highlighted, true)
			.Add(x => x.Motion, MokaGridMotion.Pulse));

		Assert.Equal("moka-grid-bg moka-grid-bg--pulse", Root(cut));
		Assert.Empty(cut.FindAll(".moka-grid-bg__fade"));
		Assert.Single(cut.FindAll(".moka-grid-bg > .moka-grid-bg__highlight"));
	}

	[Theory]
	[InlineData(MokaGridPattern.Lines)]
	[InlineData(MokaGridPattern.Topographic)]
	public void Sweep_PassesABandThroughThePatternsOwnMask(MokaGridPattern pattern)
	{
		IRenderedComponent<MokaGridBackground> cut = Grid(p => p
			.Add(x => x.Pattern, pattern)
			.Add(x => x.Motion, MokaGridMotion.Sweep));

		Assert.Equal("moka-grid-bg moka-grid-bg--sweep", Root(cut));
		string sweep = Style(cut.Find(".moka-grid-bg__fade > .moka-grid-bg__sweep"));
		string layer = Style(Pattern(cut));
		Assert.StartsWith("-webkit-mask-image: ", sweep, StringComparison.Ordinal);
		Assert.Contains(sweep, layer, StringComparison.Ordinal);
		Assert.Empty(Grid(p => p.Add(x => x.Pattern, pattern)).FindAll(".moka-grid-bg__sweep"));
	}

	[Fact]
	public void TheMotions_StopForReducedMotion()
	{
		const string drift = ".moka-grid-bg--drift > .moka-grid-bg__fade > .moka-grid-bg__pattern";
		const string major = ".moka-grid-bg--drift > .moka-grid-bg__fade > .moka-grid-bg__major";
		const string pulse = ".moka-grid-bg--pulse > .moka-grid-bg__highlight";
		const string sweep = ".moka-grid-bg__sweep::before";

		Assert.Equal(["moka-grid-bg-drift var(--_gb-drift-time) linear infinite", "none"],
			ScopedCss.Values(Css, drift, "animation"));
		Assert.Equal(["moka-grid-bg-drift var(--_gb-drift-time) linear infinite", "none"],
			ScopedCss.Values(Css, major, "animation"));
		Assert.Equal(["moka-grid-bg-pulse 5s ease-in-out infinite alternate", "none"],
			ScopedCss.Values(Css, pulse, "animation"));
		Assert.Equal(["moka-grid-bg-sweep 7s ease-in-out infinite", "none"], ScopedCss.Values(Css, sweep, "animation"));
		Assert.Equal(
			["--_gb-x var(--moka-transition-slow), --_gb-y var(--moka-transition-slow)", "none"],
			ScopedCss.Values(Css, ".moka-grid-bg--spotlight", "transition"));
		Assert.Equal("calc(-1 * var(--_gb-drift-y)) 0 0 calc(-1 * var(--_gb-drift-x))",
			ScopedCss.Declarations(Css, drift)["inset"]);
	}

	[Theory]
	[InlineData(MokaGridTone.Neutral, "var(--moka-color-outline-strong)")]
	[InlineData(MokaGridTone.Text, "color-mix(in srgb, var(--moka-color-on-background) 14%, transparent)")]
	[InlineData(MokaGridTone.AccentMajor, PrimaryBorder)]
	[InlineData(MokaGridTone.Accent, PrimaryBorder)]
	public void ATone_ColorsTheLines(MokaGridTone tone, string ink)
	{
		string style = Style(Pattern(Grid(p => p.Add(x => x.Tone, tone))));

		Assert.StartsWith($"background-image: linear-gradient(to right, {ink} 1.0px", style, StringComparison.Ordinal);
		string opacity = tone is MokaGridTone.Accent or MokaGridTone.AccentMajor ? "0.70" : "1.00";
		Assert.Contains($"; opacity: {opacity};", style, StringComparison.Ordinal);
	}

	[Theory]
	[InlineData(MokaGridTone.Neutral, "var(--moka-color-outline)", "var(--moka-color-outline-strong)")]
	[InlineData(MokaGridTone.AccentMajor, "var(--moka-color-outline)", "color-mix(in srgb, var(--moka-color-primary) 42%, transparent)")]
	[InlineData(MokaGridTone.Text, "color-mix(in srgb, var(--moka-color-on-background) 8%, transparent)",
		"color-mix(in srgb, var(--moka-color-on-background) 16%, transparent)")]
	public void ATone_ColorsGraphPapersMinorAndMajorLines(MokaGridTone tone, string minor, string major)
	{
		IRenderedComponent<MokaGridBackground> cut = Grid(p => p
			.Add(x => x.Pattern, MokaGridPattern.GraphPaper)
			.Add(x => x.Tone, tone));

		Assert.StartsWith($"background-color: {minor};", Style(Pattern(cut)), StringComparison.Ordinal);
		Assert.StartsWith($"background-color: {major};", Style(cut.Find(".moka-grid-bg__major")), StringComparison.Ordinal);
		Assert.EndsWith("opacity: 1.00", Style(Pattern(cut)), StringComparison.Ordinal);
	}

	[Fact]
	public void ATone_ReplacesTheOriginalRedOfTheSvgPatterns()
	{
		string style = Style(Pattern(Grid(p => p
			.Add(x => x.Pattern, MokaGridPattern.Honeycomb)
			.Add(x => x.Tone, MokaGridTone.Neutral))));

		Assert.StartsWith("background-color: var(--moka-color-outline-strong);", style, StringComparison.Ordinal);
	}

	[Fact]
	public void PatternColor_WinsOverTone()
	{
		IRenderedComponent<MokaGridBackground> cut = Grid(p => p
			.Add(x => x.Pattern, MokaGridPattern.GraphPaper)
			.Add(x => x.Tone, MokaGridTone.Neutral)
			.Add(x => x.PatternColor, "#123456"));

		Assert.StartsWith("background-color: #123456;", Style(Pattern(cut)), StringComparison.Ordinal);
		Assert.StartsWith("background-color: #123456;", Style(cut.Find(".moka-grid-bg__major")), StringComparison.Ordinal);
		Assert.EndsWith("opacity: 0.70", Style(Pattern(cut)), StringComparison.Ordinal);
	}

	[Fact]
	public void PatternOpacity_WinsOverATonesFullStrength()
	{
		Assert.Contains("; opacity: 0.30;", Style(Pattern(Grid(p => p
			.Add(x => x.Tone, MokaGridTone.Text)
			.Add(x => x.PatternOpacity, 0.3)))), StringComparison.Ordinal);
	}

	[Fact]
	public void TheNewDesign_DrawsTheSvgPatternsInTheThemeToken()
	{
		Assert.Equal(PrimaryBorder, ScopedCss.Declarations(Css, ".moka-gate .moka-grid-bg__pattern")["--_gb-ink"]);
		Assert.StartsWith("background-color: var(--_gb-ink, rgba(239, 83, 80, 0.25));",
			Style(Pattern(Grid(p => p.Add(x => x.Pattern, MokaGridPattern.Cross)))), StringComparison.Ordinal);
	}

	[Fact]
	public void TheLayers_StayOutOfThePointersWay()
	{
		foreach (string layer in new[] { ".moka-grid-bg__fade", ".moka-grid-bg__major", ".moka-grid-bg__sweep" })
		{
			Assert.Equal("none", ScopedCss.Declarations(Css, layer)["pointer-events"]);
			Assert.Equal("absolute", ScopedCss.Declarations(Css, layer)["position"]);
		}
	}

	[GeneratedRegex(" b-[a-z0-9]{10}")]
	private static partial Regex Scope();

	[GeneratedRegex("url\\(\"data:image/svg\\+xml,(?<svg>[^\"]*)\"\\)")]
	private static partial Regex TileUrl();
}
