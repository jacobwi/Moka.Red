using System.Globalization;
using System.Text.RegularExpressions;
using Moka.Red.Core.Theming;
using Moka.Red.Tests.Shared;

namespace Moka.Red.Core.Tests.Theming;

// The chart tokens and the chart classes in moka.css. Dark and Light keep exactly the colors their charts have always
// had: every token reads one they already write.
public sealed partial class ChartTokenTests
{
	private static readonly string[] Rotation =
	[
		"var(--moka-color-primary)", "var(--moka-color-secondary)", "var(--moka-color-success)", "var(--moka-color-info)",
		"var(--moka-color-warning)", "var(--moka-color-error)", "var(--moka-color-on-surface)", "var(--moka-color-primary)"
	];

	public static TheoryData<string> Presets => ["Dark", "Light", "Red", "RedLight", "Gate", "GateLight", "Crisp", "CrispLight", "Matrix", "MatrixLight"];

	public static TheoryData<string> NewDesign => ["Red", "RedLight", "Gate", "GateLight", "Crisp", "CrispLight", "Matrix", "MatrixLight"];

	[Fact]
	public void There_Are27()
	{
		Assert.Equal(27, MokaChartPaletteValues.TokenNames.Count);
		Assert.All(MokaChartPaletteValues.TokenNames, name => Assert.StartsWith("--moka-chart-", name, StringComparison.Ordinal));
	}

	[Theory]
	[MemberData(nameof(Presets))]
	public void EveryTheme_WritesEveryChartToken(string preset)
	{
		Dictionary<string, string> tokens = LabelColorTests.Tokens(Theme(preset).ToCssVariables());

		Assert.All(MokaChartPaletteValues.TokenNames, name => Assert.True(tokens.ContainsKey(name), $"{preset}: {name} is missing."));
	}

	[Fact]
	public void MokaCss_HoldsTheOriginalDesignsValues_ForBothSchemes()
	{
		IReadOnlyDictionary<string, string> root = ScopedCss.Declarations("moka.css", ":root");
		IReadOnlyDictionary<string, string> dark = ScopedCss.Declarations("moka.css", ".moka-dark");
		Dictionary<string, string> darkTheme = LabelColorTests.Tokens(MokaTheme.Dark.ToCssVariables());
		Dictionary<string, string> lightTheme = LabelColorTests.Tokens(MokaTheme.Light.ToCssVariables());

		foreach (string name in MokaChartPaletteValues.TokenNames)
		{
			Assert.Equal(darkTheme[name], root[name]);
			Assert.Equal(lightTheme[name], root[name]);
			Assert.Equal(root[name], dark[name]);
		}
	}

	[Fact]
	public void TheOriginalDesign_IgnoresEveryChartChoice()
	{
		foreach (MokaTheme original in new[] { MokaTheme.Dark, MokaTheme.Light })
		{
			Assert.Same(original, original.WithCharts(MokaChartLook.Bold));
			Assert.Same(original, original.WithChartPalette(MokaChartPalette.Reference));
			Assert.Same(original, original.WithChartTexture(MokaChartTexture.Hatched));
			Assert.Same(original, original.WithChartPrint(MokaChartPrint.Ink));
		}
	}

	[Theory]
	[MemberData(nameof(NewDesign))]
	public void TheChartChoices_AreTodaysCharts_UntilAnAppAsks(string preset)
	{
		MokaTheme theme = Theme(preset);

		Assert.Equal(MokaChartLook.Standard, theme.Charts);
		Assert.Equal(MokaChartPalette.Standard, theme.ChartPalette);
		Assert.Equal(MokaChartTexture.None, theme.ChartTexture);
		Assert.Equal(MokaChartPrint.AsShown, theme.ChartPrint);
		Assert.Equal(Rotation, Series(theme));
	}

	[Theory]
	[MemberData(nameof(NewDesign))]
	public void EveryChoice_TakesHold_OnTheNewDesign(string preset)
	{
		MokaTheme theme = Theme(preset)
			.WithCharts(MokaChartLook.Dense)
			.WithChartPalette(MokaChartPalette.Muted)
			.WithChartTexture(MokaChartTexture.Hatched)
			.WithChartPrint(MokaChartPrint.Ink);

		Assert.Equal((MokaChartLook.Dense, MokaChartPalette.Muted, MokaChartTexture.Hatched, MokaChartPrint.Ink),
			(theme.Charts, theme.ChartPalette, theme.ChartTexture, theme.ChartPrint));
		Assert.Equal(theme.IsDark ? MokaChartPaletteValues.MutedDark : MokaChartPaletteValues.MutedLight, Series(theme));
	}

	[Theory]
	[InlineData(false, "#3073E2")]
	[InlineData(true, "#4083F3")]
	public void Reference_WritesItsHexes(bool isDark, string first)
	{
		MokaTheme theme = (isDark ? MokaTheme.Gate : MokaTheme.GateLight).WithChartPalette(MokaChartPalette.Reference);

		Assert.Equal(first, Series(theme)[0]);
		Assert.Equal(isDark ? MokaChartPaletteValues.ReferenceDark : MokaChartPaletteValues.ReferenceLight, Series(theme));
	}

	// The theme doesn't keep the accent's id, so the preset is found by its color.
	[Fact]
	public void AccentFirst_FindsThePresetFromThePrimaryColor()
	{
		MokaTheme blue = MokaTheme.Red.WithAccent(MokaAccent.Blue).WithChartPalette(MokaChartPalette.AccentFirst);

		Assert.Equal(MokaChartPaletteValues.Series(MokaChartPalette.AccentFirst, true, MokaAccent.Blue.Dark.Color), Series(blue));
		Assert.Equal("#6B8EF7", Series(blue)[0]);
		Assert.Equal("#EF5350", Series(MokaTheme.Red.WithChartPalette(MokaChartPalette.AccentFirst))[0]);
	}

	[Fact]
	public void APalette_LeavesEveryOtherTokenAlone()
	{
		Dictionary<string, string> before = LabelColorTests.Tokens(MokaTheme.Red.ToCssVariables());
		Dictionary<string, string> after = LabelColorTests.Tokens(MokaTheme.Red.WithChartPalette(MokaChartPalette.Reference).ToCssVariables());

		Assert.Equal(before.Where(t => !SeriesToken().IsMatch(t.Key)), after.Where(t => !SeriesToken().IsMatch(t.Key)));
		Assert.NotEqual(before["--moka-chart-1"], after["--moka-chart-1"]);
	}

	[Fact]
	public void TheLookTextureAndPrint_WriteNoTokens()
	{
		string before = MokaTheme.Red.ToCssVariables();

		Assert.Equal(before, MokaTheme.Red.WithCharts(MokaChartLook.Bold).ToCssVariables());
		Assert.Equal(before, MokaTheme.Red.WithChartTexture(MokaChartTexture.Hatched).ToCssVariables());
		Assert.Equal(before, MokaTheme.Red.WithChartPrint(MokaChartPrint.Hatched).ToCssVariables());
	}

	// What the board's surfaces give, within 2. Mixed in OKLab: in sRGB the muted grey and the axis land over 2 away.
	[Theory]
	[InlineData("--moka-chart-muted", 34, "#A9A9B3", "#55555F")]
	[InlineData("--moka-chart-grid", 7, "#EDEDF0", "#212126")]
	[InlineData("--moka-chart-axis", 22, "#C4C4CB", "#3A3A42")]
	[InlineData("--moka-chart-band-1", 5, "#F1F1F3", "#1C1C21")]
	[InlineData("--moka-chart-band-2", 10, "#E4E4E8", "#26262C")]
	[InlineData("--moka-chart-band-3", 17, "#D2D2D8", "#33333A")]
	[InlineData("--moka-chart-zero", 5, "#F1F1F3", "#1C1C21")]
	public void TheGreys_AreTheOnSurfaceOverEachPresetsSurface(string token, int percent, string onWhite, string onGraphite)
	{
		string expected = string.Create(CultureInfo.InvariantCulture,
			$"color-mix(in oklab, var(--moka-color-on-surface) {percent}%, var(--moka-color-surface))");

		foreach (MokaTheme theme in new[] { MokaTheme.RedLight, MokaTheme.Red })
		{
			Assert.Equal(expected, LabelColorTests.Tokens(theme.ToCssVariables())[token]);

			string mixed = PaletteValidator.MixOklab(theme.Palette.OnSurface, theme.Palette.Surface, percent / 100.0);
			string board = theme.IsDark ? onGraphite : onWhite;
			Assert.True(PaletteValidator.DeltaE(mixed, board) <= 2, $"{token}: {mixed} is far from the board's {board}.");
		}
	}

	[Theory]
	[InlineData("Red", "#3B82F6", "#EF5350")]
	[InlineData("RedLight", "#2563EB", "#C62828")]
	[InlineData("Gate", "#3B82F6", "#EF5350")]
	[InlineData("GateLight", "#2563EB", "#C62828")]
	public void UpAndDown_AreBlueAndRed(string preset, string up, string down)
	{
		Dictionary<string, string> tokens = LabelColorTests.Tokens(Theme(preset).ToCssVariables());

		Assert.Equal(up, tokens["--moka-chart-up"]);
		Assert.Equal(down, tokens["--moka-chart-down"]);
	}

	[Fact]
	public void TheRamps_FollowTheAccent()
	{
		Dictionary<string, string> red = LabelColorTests.Tokens(MokaTheme.Red.ToCssVariables());
		Dictionary<string, string> violet = LabelColorTests.Tokens(MokaTheme.Red.WithAccent(MokaAccent.Violet).ToCssVariables());

		Assert.Equal("#5D0004", red["--moka-chart-seq-1"]);
		Assert.Equal("#C52C2A", red["--moka-chart-ord-5"]);
		Assert.Equal("#2A0083", violet["--moka-chart-seq-1"]);
	}

	[Fact]
	public void OnTheOriginalDesign_TheRampsAreTheAccentOverNothing()
	{
		Dictionary<string, string> tokens = LabelColorTests.Tokens(MokaTheme.Dark.ToCssVariables());

		Assert.Equal("color-mix(in srgb, var(--moka-color-primary) 20%, transparent)", tokens["--moka-chart-seq-1"]);
		Assert.Equal("var(--moka-color-primary)", tokens["--moka-chart-seq-5"]);
		Assert.Equal("var(--moka-color-primary)", tokens["--moka-chart-ord-1"]);
		Assert.Equal("color-mix(in srgb, var(--moka-color-primary) 20%, transparent)", tokens["--moka-chart-ord-5"]);
	}

	[Theory]
	[InlineData("standard", "2px", "6px", "8px", "0px", "none", "0px", "3px", "0.12", "11px", "var(--moka-font-family)", "600 / 260", "28px", "6px")]
	[InlineData("quiet", "2px", "8px", "8px", "2px", "24px", "2px", "4px", "0.10", "var(--moka-font-size-xs)", "var(--moka-font-family)", "600 / 260", "28px", "6px")]
	[InlineData("dense", "1.5px", "6px", "6px", "2px", "16px", "1px", "2px", "0.10", "var(--moka-font-size-xxs)", "var(--moka-font-family-mono)", "600 / 200", "20px", "5px")]
	[InlineData("soft", "2px", "8px", "8px", "2px", "40px", "3px", "8px", "0.22", "var(--moka-font-size-xs)", "var(--moka-font-family)", "600 / 260", "32px", "7px")]
	[InlineData("bold", "3px", "10px", "12px", "2px", "32px", "2px", "4px", "0.10", "var(--moka-font-size-sm)", "var(--moka-font-family)", "600 / 300", "36px", "8px")]
	public void EachLook_SetsItsSizes(string look, string line, string dot, string endDot, string ring, string barMax, string barGap,
		string barRadius, string area, string tickSize, string tickFont, string ratio, string segbar, string hatchGap)
	{
		IReadOnlyDictionary<string, string> rule = ScopedCss.Declarations("moka.css", $".moka-chart-look-{look}");

		Assert.Equal(
			[line, dot, endDot, ring, barMax, barGap, barRadius, area, tickSize, tickFont, ratio, segbar, hatchGap],
			LookVariables.Select(variable => rule[variable]));
		Assert.Equal(LookVariables.Length, rule.Count);
	}

	[Fact]
	public void EverySlot_ReadsItsToken_AndHatchesAtItsOwnAngleAndSpacing()
	{
		for (int slot = 1; slot <= MokaChartPaletteValues.Slots; slot++)
		{
			IReadOnlyDictionary<string, string> rule = ScopedCss.Declarations("moka.css", $".moka-chart-c{slot}");

			Assert.Equal($"var(--moka-chart-{slot})", rule["--moka-chart-color"]);
			Assert.Equal(slot % 2 == 1 ? "45deg" : "135deg", rule["--moka-chart-hatch-angle"]);
			Assert.Equal(slot <= 2 ? "var(--moka-chart-hatch-gap, 6px)" : "calc(var(--moka-chart-hatch-gap, 6px) * 1.5)", rule["--moka-chart-hatch-step"]);
		}
	}

	[Theory]
	[InlineData("primary", "var(--moka-color-primary)")]
	[InlineData("secondary", "var(--moka-color-secondary)")]
	[InlineData("error", "var(--moka-color-error)")]
	[InlineData("warning", "var(--moka-color-warning)")]
	[InlineData("success", "var(--moka-color-success)")]
	[InlineData("info", "var(--moka-color-info)")]
	[InlineData("surface", "var(--moka-color-on-surface)")]
	public void AToneClass_ReadsItsRole(string tone, string expected) =>
		Assert.Equal(expected, ScopedCss.Declarations("moka.css", $".moka-chart-tone-{tone}")["--moka-chart-color"]);

	[Fact]
	public void AMutedSeries_ReadsTheMutedToken() =>
		Assert.Equal("var(--moka-chart-muted)", ScopedCss.Declarations("moka.css", ".moka-chart-muted")["--moka-chart-color"]);

	[Fact]
	public void AHatchableMark_PaintsItsColor_UntilTextureIsOn()
	{
		Assert.Equal("var(--moka-chart-color)", ScopedCss.Declarations("moka.css", ".moka-chart-hatch")["--moka-chart-fill"]);
		Assert.Equal("exact", ScopedCss.Declarations("moka.css", ".moka-chart-hatch")["print-color-adjust"]);

		string hatched = ScopedCss.Declarations("moka.css", ".moka-gate .moka-chart-texture .moka-chart-hatch")["--moka-chart-fill"];
		Assert.StartsWith("repeating-linear-gradient(calc(var(--moka-chart-hatch-angle, 45deg) - 90deg)", hatched, StringComparison.Ordinal);
		Assert.Contains("var(--moka-chart-hatch-step, 6px)", hatched, StringComparison.Ordinal);
	}

	[Fact]
	public void Ink_DrawsTheTextColorOnThePage_InPrintAndForcedColors()
	{
		IReadOnlyList<string> fills = ScopedCss.Values("moka.css", ".moka-gate .moka-chart-print-ink .moka-chart-hatch", "--moka-chart-fill");
		IReadOnlyList<string> colors = ScopedCss.Values("moka.css", ".moka-gate .moka-chart-print-ink *", "--moka-chart-color");

		Assert.Equal(2, fills.Count);
		Assert.All(fills, fill => Assert.Contains("CanvasText 0 1.5px, Canvas 1.5px", fill, StringComparison.Ordinal));
		Assert.Equal(["CanvasText !important", "CanvasText !important"], colors);
		Assert.Equal(["none"], ScopedCss.Values("moka.css", ".moka-gate .moka-chart-print-ink .moka-chart-hatch", "forced-color-adjust"));
	}

	// The original design prints and shows forced colors as it always has.
	[Fact]
	public void TextureAndPrintRules_StayOnTheNewDesign()
	{
		string css = CommentText().Replace(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Css", "moka.css")), "");
		List<string> selectors = [.. RuleHead().Matches(css)
			.SelectMany(head => head.Groups["head"].Value.Split(','))
			.Select(selector => selector.Trim())
			.Where(selector => selector.Contains("moka-chart-texture", StringComparison.Ordinal) || selector.Contains("moka-chart-print-", StringComparison.Ordinal))];

		Assert.True(selectors.Count >= 8);
		Assert.All(selectors, selector => Assert.StartsWith(".moka-gate ", selector, StringComparison.Ordinal));
	}

	[Fact]
	public void MeterRows_LineUpLabelTrackAndFigure() =>
		Assert.Equal("minmax(0, max-content) minmax(0, 1fr) max-content",
			ScopedCss.Declarations("moka.css", ".moka-meter-rows")["grid-template-columns"]);

	private static readonly string[] LookVariables =
	[
		"--moka-chart-line", "--moka-chart-dot", "--moka-chart-end-dot", "--moka-chart-dot-ring", "--moka-chart-bar-max",
		"--moka-chart-bar-gap", "--moka-chart-bar-radius", "--moka-chart-area", "--moka-chart-tick-size", "--moka-chart-tick-font",
		"--moka-chart-plot-ratio", "--moka-chart-segbar", "--moka-chart-hatch-gap"
	];

	private static string[] Series(MokaTheme theme)
	{
		Dictionary<string, string> tokens = LabelColorTests.Tokens(theme.ToCssVariables());
		return [.. Enumerable.Range(1, MokaChartPaletteValues.Slots).Select(slot => tokens[$"--moka-chart-{slot}"])];
	}

	private static MokaTheme Theme(string preset) => preset switch
	{
		"Dark" => MokaTheme.Dark,
		"Light" => MokaTheme.Light,
		"Red" => MokaTheme.Red,
		"RedLight" => MokaTheme.RedLight,
		"Gate" => MokaTheme.Gate,
		"GateLight" => MokaTheme.GateLight,
		"Crisp" => MokaTheme.Crisp,
		"CrispLight" => MokaTheme.CrispLight,
		"Matrix" => MokaTheme.Matrix,
		_ => MokaTheme.MatrixLight
	};

	[GeneratedRegex(@"^--moka-chart-\d$")]
	private static partial Regex SeriesToken();

	[GeneratedRegex(@"/\*.*?\*/", RegexOptions.Singleline)]
	private static partial Regex CommentText();

	[GeneratedRegex(@"(?<head>[^{};]+)\{")]
	private static partial Regex RuleHead();
}
