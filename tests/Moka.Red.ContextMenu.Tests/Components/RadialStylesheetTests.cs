using System.Text.RegularExpressions;
using Moka.Red.Tests.Shared;

namespace Moka.Red.ContextMenu.Tests.Components;

// bUnit applies no CSS, so what the shapes, looks and choice styles draw is read from the stylesheet as text.
public partial class RadialStylesheetTests
{
	private const string Radial = "MokaRadialMenu.razor.css";

	private const string KeyboardLit =
		":not([data-moka-pointer]) > .moka-radial-menu__slot--active:not(.moka-radial-menu__slot--disabled) > .moka-radial-menu__button";

	private const string PointerLit =
		"[data-moka-pointer] > .moka-radial-menu__slot[data-moka-pointed]:not(.moka-radial-menu__slot--disabled) > .moka-radial-menu__button";

	[Fact]
	public void ThePiesSector_TurnsWithTheModule_AndIsAConicGradient()
	{
		IReadOnlyDictionary<string, string> lit = ScopedCss.Declarations(Radial, ".moka-radial-menu__disc-lit");

		Assert.Equal("var(--moka-radial-turn, 0deg)", lit["rotate"]);
		Assert.Equal("0", lit["opacity"]);
		Assert.StartsWith("conic-gradient(", lit["background"], StringComparison.Ordinal);
		Assert.Contains("var(--moka-radial-step)", lit["background"], StringComparison.Ordinal);
		Assert.Contains("rotate 0.16s ease-out", ScopedCss.Values(Radial, ".moka-radial-menu__disc-lit", "transition")[0], StringComparison.Ordinal);
		Assert.Equal("none", ScopedCss.Values(Radial, ".moka-radial-menu__disc-lit", "transition")[^1]);
	}

	[Theory]
	[InlineData(".moka-radial-menu--pie:not([data-moka-pointer]):has(> .moka-radial-menu__slot--active) > .moka-radial-menu__disc > .moka-radial-menu__disc-lit")]
	[InlineData(".moka-radial-menu--pie[data-moka-pointer]:has(> .moka-radial-menu__slot[data-moka-pointed]) > .moka-radial-menu__disc > .moka-radial-menu__disc-lit")]
	public void ThePiesSector_ShowsOnlyWhileAWedgeIsLit(string selector) =>
		Assert.Equal("1", ScopedCss.Declarations(Radial, selector)["opacity"]);

	[Theory]
	[InlineData(".moka-radial-menu--pie:not([data-moka-pointer]):has(> .moka-radial-menu__slot--active.moka-radial-menu__slot--danger)", "error")]
	[InlineData(".moka-radial-menu--pie[data-moka-pointer]:has(> .moka-radial-menu__slot--good[data-moka-pointed])", "success")]
	public void ALitDangerOrGoodWedge_KeepsItsTone(string selector, string color) =>
		Assert.Equal($"var(--moka-color-{color})", ScopedCss.Declarations(Radial, selector)["--_pie-lit"]);

	[Fact]
	public void TheHalfRing_ShowsTheTopHalfOfItsBand() =>
		Assert.Equal("inset(-0.25rem -0.25rem 50% -0.25rem)",
			ScopedCss.Declarations(Radial, ".moka-radial-menu--half-ring > .moka-radial-menu__track")["clip-path"]);

	[Theory]
	[InlineData("up", "left", "inset(-0.25rem 50% 50% -0.25rem)")]
	[InlineData("up", "right", "inset(-0.25rem -0.25rem 50% 50%)")]
	[InlineData("down", "right", "inset(50% -0.25rem -0.25rem 50%)")]
	[InlineData("down", "left", "inset(50% 50% -0.25rem -0.25rem)")]
	public void TheCornerFan_ShowsTheQuarterItOpensInto(string vertical, string horizontal, string clip) =>
		Assert.Equal(clip, ScopedCss.Declarations(Radial,
			$".moka-radial-menu--opens-{vertical}.moka-radial-menu--opens-{horizontal} > .moka-radial-menu__track")["clip-path"]);

	[Fact]
	public void CompactsTag_ShowsOnlyForTheLitButton()
	{
		Assert.Equal("none", ScopedCss.Declarations(Radial, ".moka-radial-menu__tag")["display"]);
		Assert.Equal("block", ScopedCss.Declarations(Radial,
			".moka-radial-menu:not([data-moka-pointer]) > .moka-radial-menu__slot--active > .moka-radial-menu__tag")["display"]);
		Assert.Equal("block", ScopedCss.Declarations(Radial,
			".moka-radial-menu[data-moka-pointer] > .moka-radial-menu__slot[data-moka-pointed] > .moka-radial-menu__tag")["display"]);
	}

	[Fact]
	public void Labeled_KeepsTheHubsWord_WhileThePointerLeads()
	{
		Assert.Equal("block", ScopedCss.Declarations(Radial,
			".moka-radial-menu--labeled[data-moka-pointer] > .moka-radial-menu__hub > .moka-radial-menu__word")["display"]);
		Assert.Equal("none", ScopedCss.Declarations(Radial,
			".moka-radial-menu--labeled[data-moka-pointer] > .moka-radial-menu__hub > .moka-radial-menu__pointer-word")["display"]);
	}

	[Fact]
	public void Marking_LightsTheLineToTheLitWord() =>
		Assert.Contains("var(--_tone)", ScopedCss.Declarations(Radial,
			".moka-radial-menu[data-moka-pointer] > .moka-radial-menu__slot[data-moka-pointed]:not(.moka-radial-menu__slot--disabled) > .moka-radial-menu__line")["background"],
			StringComparison.Ordinal);

	[Theory]
	[InlineData("glass")]
	[InlineData("solid")]
	[InlineData("outline")]
	[InlineData("terminal")]
	[InlineData("soft")]
	public void EveryLook_LightsOneButton_ForThePointerAndTheKeyboardAlike(string look)
	{
		IReadOnlyDictionary<string, string> keyboard = ScopedCss.Declarations(Radial, $".moka-radial-menu--{look}{KeyboardLit}");
		IReadOnlyDictionary<string, string> pointer = ScopedCss.Declarations(Radial, $".moka-radial-menu--{look}{PointerLit}");

		Assert.NotEmpty(keyboard);
		Assert.Equal(keyboard, pointer);
		Assert.Contains("var(--_tone", string.Join(";", keyboard.Values), StringComparison.Ordinal);
	}

	[Fact]
	public void TheGlassLook_BlursThePageBehindIt() =>
		Assert.Equal("blur(var(--moka-backdrop-blur)) saturate(1.5)", ScopedCss.Declarations(Radial,
			".moka-radial-menu--glass > .moka-radial-menu__slot > .moka-radial-menu__button")["backdrop-filter"]);

	[Fact]
	public void TheTerminalLook_WritesTheHubsWordLikeAPrompt()
	{
		Assert.Equal("'> '", ScopedCss.Declarations(Radial,
			".moka-radial-menu--terminal > .moka-radial-menu__hub > .moka-radial-menu__word::before")["content"]);
		Assert.Equal("'_'", ScopedCss.Declarations(Radial,
			".moka-radial-menu--terminal > .moka-radial-menu__hub > .moka-radial-menu__word::after")["content"]);
		Assert.Equal("var(--moka-font-family-mono)", ScopedCss.Declarations(Radial,
			".moka-radial-menu--terminal > .moka-radial-menu__slot > .moka-radial-menu__button > .moka-radial-menu__text")["font-family"]);
	}

	[Fact]
	public void TheArcsBand_IsCutToTheArcByAConicMask()
	{
		IReadOnlyDictionary<string, string> band = ScopedCss.Declarations(Radial, ".moka-radial-menu__arc");

		Assert.StartsWith("conic-gradient(from var(--moka-radial-arc-from)", band["mask"], StringComparison.Ordinal);
		Assert.Contains("var(--moka-radial-arc-span)", band["mask"], StringComparison.Ordinal);
		Assert.Equal("none", band["pointer-events"]);
	}

	[Fact]
	public void TheFlyoutsStem_ShowsOnlyForTheLitButton()
	{
		Assert.Equal("none", ScopedCss.Declarations(Radial, ".moka-radial-menu__stem")["display"]);
		Assert.Equal("block", ScopedCss.Declarations(Radial,
			".moka-radial-menu[data-moka-pointer] > .moka-radial-menu__slot[data-moka-pointed] > .moka-radial-menu__stem")["display"]);
	}

	[Fact]
	public void TheHub_TakesClicks_OnlyWhileItIsBack()
	{
		Assert.Equal("none", ScopedCss.Declarations(Radial, ".moka-radial-menu__hub")["pointer-events"]);
		Assert.Equal("auto", ScopedCss.Declarations(Radial, ".moka-radial-menu--back > .moka-radial-menu__hub")["pointer-events"]);
	}

	[Theory]
	[InlineData(".moka-radial-menu__disc")]
	[InlineData(".moka-radial-menu__sep")]
	[InlineData(".moka-radial-menu__label")]
	[InlineData(".moka-radial-menu__tag")]
	[InlineData(".moka-radial-menu__line")]
	[InlineData(".moka-radial-menu__arc")]
	[InlineData(".moka-radial-menu__choices--arc > .moka-radial-menu__choice")]
	public void ReducedMotion_DropsTheNewOpenings(string selector) =>
		Assert.Equal("none", ScopedCss.Values(Radial, selector, "animation")[^1]);

	// The looks and shapes work on the original design too, so the whole sheet stays on tokens, not only a Gate block.
	[Fact]
	public void TheStylesheet_HardCodesNoColorAndNoPixelSize()
	{
		string css = Comment().Replace(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Css", Radial)), " ");

		Assert.Empty(HardCodedColor().Matches(css));
		Assert.Empty(LargePixelSize().Matches(css));
	}

	[GeneratedRegex(@"/\*.*?\*/", RegexOptions.Singleline)]
	private static partial Regex Comment();

	[GeneratedRegex(@":[^;{}]*?(#[0-9a-fA-F]{3,8}\b|rgba?\()")]
	private static partial Regex HardCodedColor();

	[GeneratedRegex(@"(?<![\d.])([3-9]|\d{2,})px\b")]
	private static partial Regex LargePixelSize();
}
