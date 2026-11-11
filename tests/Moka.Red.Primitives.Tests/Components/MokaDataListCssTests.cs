using System.Text.RegularExpressions;
using Moka.Red.Tests.Shared;

namespace Moka.Red.Primitives.Tests.Components;

// What the data list's layout does lives in its stylesheet, which bUnit doesn't apply, so these read the rules.
public partial class MokaDataListCssTests
{
	private const string Css = "MokaDataList.razor.css";
	private const string Entry = ".moka-data-list ::deep > div";
	private const string Term = ".moka-data-list ::deep > div > dt";
	private const string Value = ".moka-data-list ::deep > div > dd";

	[Fact]
	public void ALongValue_BreaksInsteadOfRunningOutOfTheList()
	{
		IReadOnlyDictionary<string, string> value = ScopedCss.Declarations(Css, Value);

		Assert.Equal("anywhere", value["overflow-wrap"]);
		Assert.Equal("0", value["min-width"]);
	}

	[Fact]
	public void ALongTerm_BreaksInsteadOfRunningOutOfTheList()
	{
		Assert.Equal("anywhere", ScopedCss.Declarations(Css, Term)["overflow-wrap"]);
	}

	[Fact]
	public void InARow_TheValueMovesUnderTheTerm_WhenTheresNoRoomBesideIt()
	{
		Assert.Equal("wrap", ScopedCss.Declarations(Css, ".moka-data-list--row ::deep > div")["flex-wrap"]);
		Assert.Equal("100%", ScopedCss.Declarations(Css, ".moka-data-list--row ::deep > div > dt")["max-width"]);
		Assert.Equal("1 1 8rem", ScopedCss.Declarations(Css, ".moka-data-list--row ::deep > div > dd")["flex"]);
	}

	[Fact]
	public void Striped_PadsEveryRow_SoTheTermsLineUp()
	{
		IReadOnlyDictionary<string, string> every = ScopedCss.Declarations(Css, ".moka-data-list--striped ::deep > div");
		IReadOnlyDictionary<string, string> even = ScopedCss.Declarations(Css, ".moka-data-list--striped ::deep > div:nth-child(even)");

		Assert.Equal("var(--moka-spacing-sm)", every["padding-inline"]);
		Assert.DoesNotContain(even.Keys, property => property.StartsWith("padding", StringComparison.Ordinal));
		Assert.Equal("var(--moka-color-surface-variant)", even["background"]);
	}

	[Fact]
	public void Dense_KeepsTheGapBesideARowsTerm()
	{
		IReadOnlyDictionary<string, string> dense = ScopedCss.Declarations(Css, ".moka-data-list--dense ::deep > div");

		Assert.DoesNotContain("gap", dense.Keys);
		Assert.DoesNotContain("padding", dense.Keys);
		Assert.Equal("0", dense["row-gap"]);
		Assert.Equal("var(--moka-spacing-xxs)", dense["padding-block"]);
	}

	[Fact]
	public void PairsWrittenAsChildContent_TakeTheListsStyles()
	{
		Assert.Equal("flex", ScopedCss.Declarations(Css, Entry)["display"]);
		Assert.Equal("0", ScopedCss.Declarations(Css, Value)["margin"]);
		Assert.Equal("var(--moka-font-weight-semibold)", ScopedCss.Declarations(Css, Term)["font-weight"]);
	}

	[Fact]
	public void TermWidth_GivesEveryTermInARowTheSameWidth()
	{
		IReadOnlyDictionary<string, string> term =
			ScopedCss.Declarations(Css, ".moka-data-list--row.moka-data-list--term-width ::deep > div > dt");

		Assert.Equal("0 1 var(--_dl-term-width)", term["flex"]);
		Assert.Equal("0", term["min-width"]);
	}

	[Fact]
	public void Columns_AreAGridThatNeverGoesNarrowerThanTheMinimum()
	{
		IReadOnlyDictionary<string, string> list = ScopedCss.Declarations(Css, ".moka-data-list--columns");

		Assert.Equal("grid", list["display"]);
		Assert.StartsWith("repeat(auto-fill, minmax(min(100%, max(var(--_dl-min-column, 16rem),", list["grid-template-columns"],
			StringComparison.Ordinal);
	}

	[Fact]
	public void BorderedColumns_DrawALineUnderEveryRowButTheLast()
	{
		const string every = ".moka-data-list--columns.moka-data-list--bordered ::deep > div";

		Assert.Equal("clip", ScopedCss.Declarations(Css, ".moka-data-list--columns.moka-data-list--bordered")["overflow-y"]);
		Assert.Equal("calc(-1 * var(--moka-border-width))", ScopedCss.Declarations(Css, every)["margin-bottom"]);
		Assert.Equal("var(--moka-border-width) solid var(--moka-color-outline-variant)",
			ScopedCss.Declarations(Css, every + ":last-child")["border-bottom"]);
	}

	[Theory]
	[InlineData("settings")]
	[InlineData("inset")]
	[InlineData("spec")]
	[InlineData("terminal")]
	public void TheLooksBesideTheirTerms_PutTheValueUnderTheTerm_WhenTheresNoRoom(string look)
	{
		IReadOnlyDictionary<string, string> row = ScopedCss.Declarations(Css, $".moka-data-list--{look} ::deep > div");

		Assert.Equal("row", row["flex-direction"]);
		Assert.Equal("wrap", row["flex-wrap"]);
		Assert.Equal("1", ScopedCss.Declarations(Css, $".moka-data-list--{look} ::deep > div > dt")["opacity"]);
		Assert.Equal("var(--_dl-term-width)",
			ScopedCss.Declarations(Css, $".moka-data-list--{look}.moka-data-list--term-width ::deep > div > dt")["flex-basis"]);
	}

	[Theory]
	[InlineData("settings", "var(--moka-spacing-xs)")]
	[InlineData("inset", "var(--moka-spacing-xs)")]
	[InlineData("spec", "var(--moka-spacing-xxs)")]
	[InlineData("terminal", "0")]
	public void Dense_TightensEveryLooksRows(string look, string padding)
	{
		Assert.Equal(padding,
			ScopedCss.Declarations(Css, $".moka-data-list--{look}.moka-data-list--dense ::deep > div")["padding-block"]);
	}

	[Fact]
	public void SettingsRows_HaveATermColumnAndBodySizedValues()
	{
		IReadOnlyDictionary<string, string> term = ScopedCss.Declarations(Css, ".moka-data-list--settings ::deep > div > dt");
		IReadOnlyDictionary<string, string> value = ScopedCss.Declarations(Css, ".moka-data-list--settings ::deep > div > dd");

		Assert.Equal("0 1 9.5rem", term["flex"]);
		Assert.Equal("var(--moka-color-on-surface-variant)", term["color"]);
		Assert.Equal("1 1 11rem", value["flex"]);
		Assert.Equal("var(--moka-font-size-md)", value["font-size"]);
		Assert.Equal("var(--moka-spacing-xxs)",
			ScopedCss.Declarations(Css,
				".moka-data-list--settings:not(.moka-data-list--columns):not(.moka-data-list--striped) ::deep > div:first-child")["padding-top"]);
	}

	[Fact]
	public void TheInsetPanel_IsAPanel_WithValuesAtTheEnd()
	{
		IReadOnlyDictionary<string, string> panel = ScopedCss.Declarations(Css, ".moka-data-list--inset");
		IReadOnlyDictionary<string, string> value = ScopedCss.Declarations(Css, ".moka-data-list--inset ::deep > div > dd");

		Assert.Equal("var(--moka-color-surface-variant)", panel["background"]);
		Assert.Equal("var(--moka-radius-lg)", panel["border-radius"]);
		Assert.Equal("var(--moka-inner-padding)", panel["padding-inline"]);
		Assert.Equal("auto", value["margin-inline-start"]);
		Assert.Equal("end", value["text-align"]);
	}

	[Fact]
	public void FactTiles_AreAGridOfTilesWithAMicroLabel()
	{
		IReadOnlyDictionary<string, string> grid = ScopedCss.Declarations(Css, ".moka-data-list--tiles");
		IReadOnlyDictionary<string, string> tile = ScopedCss.Declarations(Css, ".moka-data-list--tiles ::deep > div");
		IReadOnlyDictionary<string, string> term = ScopedCss.Declarations(Css, ".moka-data-list--tiles ::deep > div > dt");

		Assert.Equal("grid", grid["display"]);
		Assert.Equal("repeat(auto-fill, minmax(min(100%, var(--_dl-min-column, 11rem)), 1fr))", grid["grid-template-columns"]);
		Assert.Equal("var(--moka-panel-padding)", tile["padding"]);
		Assert.Equal("var(--moka-color-surface-variant)", tile["background"]);
		Assert.Equal("uppercase", term["text-transform"]);
		Assert.Equal("var(--moka-color-on-surface-tertiary)", term["color"]);
		Assert.Equal("1 / -1", ScopedCss.Declarations(Css, ".moka-data-list--tiles ::deep > div.moka-data-list-entry--wide")["grid-column"]);
		Assert.Equal("1 / -1", ScopedCss.Declarations(Css, ".moka-data-list--columns ::deep > div.moka-data-list-entry--wide")["grid-column"]);
	}

	[Fact]
	public void BorderedTiles_AreOutlined_AndKeepEveryLine()
	{
		IReadOnlyDictionary<string, string> last =
			ScopedCss.Declarations(Css, ".moka-data-list--tiles.moka-data-list--bordered ::deep > div:last-child");

		Assert.Equal("var(--moka-border-width) solid var(--moka-color-outline-variant)", last["border"]);
		Assert.Equal("0", last["margin-bottom"]);
	}

	[Fact]
	public void TheSpecSheet_DrawsADottedLeaderToTheValue()
	{
		IReadOnlyDictionary<string, string> leader = ScopedCss.Declarations(Css, ".moka-data-list--spec ::deep > div > dt::after");
		IReadOnlyDictionary<string, string> value = ScopedCss.Declarations(Css, ".moka-data-list--spec ::deep > div > dd");

		Assert.Equal("\"\"", leader["content"]);
		Assert.Equal("var(--moka-border-width) dotted var(--moka-color-outline-hover)", leader["border-bottom"]);
		Assert.Equal("1 0 auto", ScopedCss.Declarations(Css, ".moka-data-list--spec ::deep > div > dt")["flex"]);
		Assert.Equal("tabular-nums", value["font-variant-numeric"]);
		Assert.Equal("auto", value["margin-inline-start"]);
	}

	[Fact]
	public void TheTerminal_IsMonoWithAnAccentEdge_AndLowercaseKeys()
	{
		IReadOnlyDictionary<string, string> list = ScopedCss.Declarations(Css, ".moka-data-list--terminal");
		IReadOnlyDictionary<string, string> term = ScopedCss.Declarations(Css, ".moka-data-list--terminal ::deep > div > dt");

		Assert.Equal("var(--moka-font-family-mono)", list["font-family"]);
		Assert.Equal("calc(2 * var(--moka-border-width)) solid var(--moka-color-primary)", list["border-inline-start"]);
		Assert.Equal("var(--moka-radius-none)", list["border-radius"]);
		Assert.Equal("lowercase", term["text-transform"]);
		Assert.Equal("0 0 11ch", term["flex"]);
		Assert.Equal("\":\"", ScopedCss.Declarations(Css, ".moka-data-list--terminal ::deep > div > dt::after")["content"]);
		Assert.Equal("\"-\"",
			ScopedCss.Declarations(Css, ".moka-data-list--terminal ::deep > div > dd.moka-data-list-description--empty::before")["content"]);
	}

	[Fact]
	public void TheTermStyles_ComeAfterTheLooks_SoTheyWin()
	{
		string css = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Css", Css));

		Assert.True(css.IndexOf(".moka-data-list--term-quiet ::deep", StringComparison.Ordinal) >
		            css.LastIndexOf(".moka-data-list--settings ::deep > div > dt {", StringComparison.Ordinal));
		Assert.Equal("var(--moka-font-weight-normal)", ScopedCss.Declarations(Css, ".moka-data-list--term-quiet ::deep > div > dt")["font-weight"]);
		Assert.Equal("uppercase", ScopedCss.Declarations(Css, ".moka-data-list--term-micro ::deep > div > dt")["text-transform"]);
		Assert.Equal("var(--moka-color-on-surface)", ScopedCss.Declarations(Css, ".moka-data-list--term-strong ::deep > div > dt")["color"]);
		Assert.Equal("var(--moka-color-on-surface-variant)",
			ScopedCss.Declarations(Css, ".moka-data-list--term-strong ::deep > div > dd")["color"]);
	}

	[Theory]
	[InlineData(".moka-gate .moka-data-list--tiles ::deep > div > dt", "var(--moka-color-on-surface-tertiary)")]
	[InlineData(".moka-gate .moka-data-list--terminal ::deep > div > dt", "var(--moka-color-on-surface-tertiary)")]
	[InlineData(".moka-gate .moka-data-list--term-micro ::deep > div > dt", "var(--moka-color-on-surface-tertiary)")]
	[InlineData(".moka-gate .moka-data-list--term-strong ::deep > div > dt", "var(--moka-color-on-surface)")]
	public void OnTheNewDesign_TheTermColorsThatDiffer_AreSaidAgain(string selector, string color)
	{
		Assert.Equal(color, ScopedCss.Declarations(Css, selector)["color"]);
	}

	[Fact]
	public void AlwaysShownCopyButtons_AreOutlinedAndNeverFade()
	{
		const string button = ".moka-data-list--copy-always ::deep > div > dd > .moka-copyable > .moka-tooltip-trigger > .moka-copy-btn";

		IReadOnlyDictionary<string, string> always = ScopedCss.Declarations(Css, button);
		Assert.Equal("1", always["opacity"]);
		Assert.Equal("var(--moka-border-width) solid var(--moka-color-outline-strong)", always["border"]);
		Assert.Equal("var(--moka-color-surface)",
			ScopedCss.Declarations(Css, button + ":not(.moka-copy-btn--copied):not(.moka-copy-btn--failed)")["background-color"]);
	}

	[Fact]
	public void AValueThatCopies_IsATextButtonWithACopyCursor_AndItsIconComesUpOnHoverFocusOrTouch()
	{
		const string button = ".moka-data-list--copy-value ::deep > div > dd > .moka-tooltip-trigger > .moka-data-list-copy-value";

		IReadOnlyDictionary<string, string> value = ScopedCss.Declarations(Css, button);
		Assert.Equal("copy", value["cursor"]);
		Assert.Equal("inherit", value["font"]);
		Assert.Equal("auto", value["width"]);
		Assert.Equal(["0", "1"], ScopedCss.Values(Css, button + " > .moka-data-list-copy-icon", "opacity"));
		Assert.Equal("1", ScopedCss.Declarations(Css, button + ":focus-visible > .moka-data-list-copy-icon")["opacity"]);
		Assert.Equal("none", ScopedCss.Declarations(Css, button + " > .moka-data-list-copy-icon--done")["display"]);
		Assert.Equal("none", ScopedCss.Declarations(Css, button + ".moka-copy-btn--copied > .moka-data-list-copy-icon--idle")["display"]);
		Assert.Equal("flex", ScopedCss.Declarations(Css, button + ".moka-copy-btn--copied > .moka-data-list-copy-icon--done")["display"]);
	}

	[Fact]
	public void TheStylesheet_UsesTokensForColorsAndSizes()
	{
		string css = Comment().Replace(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Css", Css)), "");

		Assert.Empty(HardCodedColor().Matches(css));
		Assert.All(PixelSize().Matches(css), match => Assert.Equal("1px", match.Value));
	}

	[Fact]
	public void ANestedList_IsntReachedByTheOuterListsModifiers()
	{
		string css = Comment().Replace(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Css", Css)), "");

		List<string> reaching = DescendantOfModifier().Matches(css).Select(m => m.Value).ToList();

		Assert.Empty(reaching);
	}

	// A modifier class followed by a descendant combinator reaches every list inside it, not just its own rows.
	[GeneratedRegex(@"\.moka-data-list--[a-z-]+(\.[a-z-]+)*(:[a-z-]+(\([^)]*\))?)*\s+(?!::deep\s*>)(?!\{)[^\s,{]+")]
	private static partial Regex DescendantOfModifier();

	[GeneratedRegex(@"/\*.*?\*/", RegexOptions.Singleline)]
	private static partial Regex Comment();

	[GeneratedRegex(@"#[0-9a-fA-F]{3,8}\b|rgba?\(|hsla?\(")]
	private static partial Regex HardCodedColor();

	[GeneratedRegex(@"(?<![\w.-])\d+(\.\d+)?px\b")]
	private static partial Regex PixelSize();
}
