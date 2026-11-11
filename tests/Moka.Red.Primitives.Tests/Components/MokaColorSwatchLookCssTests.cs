using System.Text.RegularExpressions;
using Moka.Red.Tests.Shared;

namespace Moka.Red.Primitives.Tests.Components;

// What the swatch's looks, marks and entries do in CSS alone, read from the stylesheet since bUnit applies none.
public partial class MokaColorSwatchLookCssTests
{
	private const string Css = "MokaColorSwatch.razor.css";

	private static IReadOnlyDictionary<string, string> Rule(string selector) => ScopedCss.Declarations(Css, selector);

	// The rules for both designs read tokens only, so the presets and the original design both draw them in their own
	// colors. White and black stay only where they are the color space itself, never the theme.
	[Fact]
	public void TheNewRules_HardCodeNoColor()
	{
		string text = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Css", Css));
		int start = text.IndexOf("/* Looks, marks and entries", StringComparison.Ordinal);
		int end = text.IndexOf("/* Gate look.", StringComparison.Ordinal);
		Assert.True(start > 0 && end > start);

		string rules = Comment().Replace(text[start..end], " ");
		Assert.Empty(HardCodedColor().Matches(rules));
		Assert.Empty(LargePixelSize().Matches(rules));
	}

	[Fact]
	public void TheRingSizes_ComeFromTheFocusWidth_AndTheGapFollowsThePage()
	{
		IReadOnlyDictionary<string, string> root = Rule(".moka-color-swatch");

		Assert.Equal("var(--moka-focus-width)", root["--_swatch-line"]);
		Assert.Equal("calc(2 * var(--_swatch-line))", root["--_swatch-ring"]);
		Assert.Equal("var(--moka-swatch-ring-gap, var(--moka-color-surface))", root["--_swatch-ring-gap"]);
		Assert.Equal("var(--moka-swatch-halo, var(--moka-color-primary-ring))", root["--_swatch-halo"]);
	}

	[Fact]
	public void TheLooks_Flow_CappedByColumns_UnlessOneRowHoldsThemAll()
	{
		IReadOnlyDictionary<string, string> flow = Rule(".moka-color-swatch-grid--flow");

		Assert.Equal("flex", flow["display"]);
		Assert.Equal("wrap", flow["flex-wrap"]);
		Assert.Equal("var(--_swatch-gap)", flow["gap"]);
		Assert.Equal("calc(var(--moka-swatch-columns, 8) * (var(--moka-swatch-size) + var(--_swatch-gap)))", flow["max-width"]);
		Assert.Equal(flow, Rule(".moka-color-swatch--look .moka-color-swatch-grid"));
		Assert.Equal("none", Rule(".moka-color-swatch .moka-color-swatch-grid--one-row")["max-width"]);
	}

	[Fact]
	public void Ring_DrawsARingInTheTextColor_WithAGapOfPage()
	{
		Assert.Equal("var(--moka-spacing-md)", Rule(".moka-color-swatch--ring")["--_swatch-gap"]);
		Assert.Equal("inset 0 0 0 1px var(--_swatch-edge), 0 0 0 var(--_swatch-line) var(--_swatch-ring-gap), 0 0 0 var(--_swatch-ring) var(--moka-color-on-surface)",
			Normal(Rule(".moka-color-swatch--ring .moka-color-swatch-item--selected")["box-shadow"]));
		Assert.Equal(["translateY(-1px)", "none"], ScopedCss.Values(Css,
			".moka-color-swatch--ring .moka-color-swatch-item:hover:not(.moka-color-swatch-item--disabled):not(.moka-color-swatch-item--selected)",
			"transform"));
	}

	[Fact]
	public void Tiles_AreRoundedSquares_WithAnInnerGapWhenChosen()
	{
		Assert.Equal("var(--moka-radius-md)", Rule(".moka-color-swatch--tiles .moka-color-swatch-item")["border-radius"]);
		Assert.StartsWith("inset 0 0 0 var(--_swatch-line) var(--_swatch-ring-gap)",
			Rule(".moka-color-swatch--tiles .moka-color-swatch-item--selected")["box-shadow"], StringComparison.Ordinal);
	}

	[Fact]
	public void Chips_ArePills_BorderedInTheTextColorWhenChosen()
	{
		IReadOnlyDictionary<string, string> chip = Rule(".moka-color-swatch--chips .moka-color-swatch-item");
		Assert.Equal("var(--moka-radius-full)", chip["border-radius"]);
		Assert.Equal("1px solid var(--moka-color-outline)", chip["border"]);
		Assert.Equal("auto", chip["width"]);
		Assert.Equal("none", Rule(".moka-color-swatch--chips .moka-color-swatch-grid")["max-width"]);
		Assert.Equal("var(--moka-color-on-surface)", Rule(".moka-color-swatch--chips .moka-color-swatch-item--selected")["border-color"]);
	}

	[Fact]
	public void Strip_IsOneBar_AndTheChosenColorLiftsOverAMark()
	{
		IReadOnlyDictionary<string, string> grid = Rule(".moka-color-swatch--strip .moka-color-swatch-grid");
		Assert.Equal("none", grid["max-width"]);
		Assert.Equal("0px", Rule(".moka-color-swatch--strip")["--_swatch-gap"]);
		Assert.Equal("0", Rule(".moka-color-swatch--strip .moka-color-swatch-item")["border-radius"]);
		Assert.Equal("var(--moka-radius-md)", Rule(".moka-color-swatch--strip .moka-color-swatch-item--start")["border-start-start-radius"]);
		Assert.Equal("var(--moka-radius-md)", Rule(".moka-color-swatch--strip .moka-color-swatch-item--end")["border-end-end-radius"]);
		Assert.Equal("translateY(calc(-1.5 * var(--_swatch-line)))", Rule(".moka-color-swatch--strip .moka-color-swatch-item--selected")["transform"]);

		IReadOnlyDictionary<string, string> bar =
			Rule(".moka-color-swatch--strip:not(.moka-color-swatch--marked) .moka-color-swatch-item--selected::before");
		Assert.Equal("\"\"", bar["content"]);
		Assert.Equal("var(--moka-color-on-surface)", bar["background"]);
	}

	[Fact]
	public void Menu_IsAPanel_ThatReadsThePopupLook()
	{
		IReadOnlyDictionary<string, string> panel = Rule(".moka-color-swatch--menu > .moka-color-swatch-grid");
		Assert.Equal("var(--moka-popup-bg, var(--moka-color-surface))", panel["background"]);
		Assert.Equal("var(--moka-popup-radius, var(--moka-radius-popup))", panel["border-radius"]);
		Assert.Equal("var(--moka-popup-shadow, var(--moka-shadow-popup))", panel["box-shadow"]);
		Assert.Equal("100%", panel["max-width"]);
		Assert.Equal("column", Rule(".moka-color-swatch--menu .moka-color-swatch-grid")["flex-direction"]);
		Assert.Equal("var(--moka-menu-row)", Rule(".moka-color-swatch--menu .moka-color-swatch-item")["height"]);
	}

	[Fact]
	public void Terminal_GlowsInTheAccent_AndItsLineReadsLikeAPrompt()
	{
		Assert.Contains("var(--moka-color-primary-glow-strong)",
			Rule(".moka-color-swatch--terminal .moka-color-swatch-item--selected")["box-shadow"], StringComparison.Ordinal);
		Assert.Equal("uppercase", Rule(".moka-color-swatch--terminal .moka-color-swatch-preview")["text-transform"]);
		Assert.Equal(["\">\"", "\">\" / \"\""],
			ScopedCss.Values(Css, ".moka-color-swatch--terminal .moka-color-swatch-preview::before", "content"));
	}

	[Fact]
	public void TheMarks_ReplaceTheLooksOwn()
	{
		IReadOnlyDictionary<string, string> check = Rule(".moka-color-swatch--mark-check .moka-color-swatch-item--selected");
		Assert.Equal("scale(1.08)", check["transform"]);
		Assert.Equal("transparent", check["border-color"]);

		IReadOnlyDictionary<string, string> ring = Rule(".moka-color-swatch--mark-ring .moka-color-swatch-item--selected");
		Assert.Equal(ring, Rule(".moka-color-swatch--mark-ring-and-check .moka-color-swatch-item--selected"));
		Assert.EndsWith("0 0 0 var(--_swatch-ring) var(--_swatch-mark, var(--moka-color-on-surface))", Normal(ring["box-shadow"]), StringComparison.Ordinal);
		Assert.Equal("var(--_swatch-color, var(--moka-color-on-surface))",
			Rule(".moka-color-swatch--mark-color-ring .moka-color-swatch-item--selected")["--_swatch-mark"]);

		Assert.Equal("inset 0 0 0 1px var(--_swatch-edge)", Rule(".moka-color-swatch--mark-dot .moka-color-swatch-item--selected")["box-shadow"]);
		Assert.Equal("var(--moka-color-primary)", Rule(".moka-color-swatch-badge")["background-color"]);
		Assert.Equal("var(--moka-color-on-primary)", Rule(".moka-color-swatch-badge")["color"]);
	}

	// The dot is black or white by the color's lightness, as the check is, with the same blend where the browser can't
	// read a color's lightness.
	[Fact]
	public void TheDot_IsBlackOrWhiteByTheColorsLightness()
	{
		Assert.Equal(["white", "lab(from var(--_swatch-color) clamp(0, (49.4 - l) * 1000, 100) 0 0)"],
			ScopedCss.Values(Css, ".moka-color-swatch-dot-mark", "background-color"));
		Assert.Equal("var(--moka-color-on-surface)", Rule(".moka-color-swatch-item--none .moka-color-swatch-dot-mark")["background-color"]);
		Assert.Equal("var(--moka-color-on-surface)",
			Rule(".moka-color-swatch ::deep .moka-color-swatch-item--none .moka-color-swatch-check")["color"]);
	}

	[Fact]
	public void TheNoneLooks_DrawFromTokens()
	{
		IReadOnlyDictionary<string, string> chosenChip = Rule(
			".moka-color-swatch:not(.moka-color-swatch--named) .moka-color-swatch-item--none-chip.moka-color-swatch-item--selected");
		Assert.Equal("var(--moka-color-on-surface)", chosenChip["background-color"]);
		Assert.Equal("none", chosenChip["box-shadow"]);
		Assert.Equal("var(--moka-color-background)", Rule(
			".moka-color-swatch:not(.moka-color-swatch--named) .moka-color-swatch-item--none-chip.moka-color-swatch-item--selected .moka-color-swatch-name")["color"]);

		Assert.Equal("calc(0.75 * var(--_swatch-line)) dashed var(--moka-color-outline-hover)",
			Rule(".moka-color-swatch:not(.moka-color-swatch--named) .moka-color-swatch-item--none-empty")["border"]);
		Assert.Equal("var(--_swatch-checker)", Rule(".moka-color-swatch-item--none-checkerboard")["background-image"]);
		Assert.Equal("repeating-conic-gradient(var(--moka-color-outline) 0 25%, var(--moka-color-surface) 0 50%)",
			Rule(".moka-color-swatch")["--_swatch-checker"]);
	}

	[Fact]
	public void TheCustomEntries_DrawTheHueCircle_AndThePanelReadsThePopupLook()
	{
		Assert.StartsWith("conic-gradient(hsl(0 100% 50%)", Rule(".moka-color-swatch")["--_swatch-hues"], StringComparison.Ordinal);
		Assert.Equal("var(--_swatch-hues)", Rule(".moka-color-swatch-rainbow")["background-image"]);
		Assert.Equal("var(--_swatch-hues)", Rule(".moka-color-swatch-pill-dot")["background-image"]);
		Assert.Equal("1px dashed var(--moka-color-outline-hover)", Rule(".moka-color-swatch-pill")["border"]);

		IReadOnlyDictionary<string, string> panel = Rule(".moka-color-swatch-panel");
		Assert.Equal("var(--moka-popup-bg, var(--moka-color-surface))", panel["background"]);
		Assert.Equal("var(--moka-popup-shadow, var(--moka-shadow-popup))", panel["box-shadow"]);

		Assert.Equal("var(--moka-font-family-mono)", Rule(".moka-color-swatch-hex-input")["font-family"]);
		Assert.Equal("var(--moka-focus-ring)", Rule(".moka-color-swatch-hex:focus-within")["box-shadow"]);
		Assert.Equal("var(--moka-focus-ring-error)", Rule(".moka-color-swatch-hex--invalid:focus-within")["box-shadow"]);
	}

	// Each look's focus ring doubles the look's class, so it wins over that look's hover.
	[Theory]
	[InlineData("ring")]
	[InlineData("tiles")]
	[InlineData("chips")]
	[InlineData("strip")]
	[InlineData("menu")]
	[InlineData("terminal")]
	public void EveryLook_HasAFocusRing(string look)
	{
		IReadOnlyDictionary<string, string> focus = Rule(
			$".moka-color-swatch--look.moka-color-swatch--{look} .moka-color-swatch-item:has(.moka-color-swatch-input:focus-visible)");

		Assert.Contains("var(--moka-color-primary)", focus["box-shadow"] + focus.GetValueOrDefault("border-color"), StringComparison.Ordinal);
	}

	[Fact]
	public void TheNewDesign_AnimatesTheRings_Too()
	{
		Assert.Contains("box-shadow var(--moka-transition-fast)",
			Rule(".moka-gate .moka-color-swatch--look .moka-color-swatch-item")["transition"], StringComparison.Ordinal);
	}

	private static string Normal(string value) => Whitespace().Replace(value, " ");

	[GeneratedRegex(@"/\*.*?\*/", RegexOptions.Singleline)]
	private static partial Regex Comment();

	[GeneratedRegex(@"#[0-9a-fA-F]{3,8}\b|rgba?\(")]
	private static partial Regex HardCodedColor();

	[GeneratedRegex(@"(?<![\d.])([3-9]|\d{2,})px\b")]
	private static partial Regex LargePixelSize();

	[GeneratedRegex(@"\s+")]
	private static partial Regex Whitespace();
}
