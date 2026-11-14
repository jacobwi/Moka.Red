using System.Globalization;
using Moka.Red.Core.Colors;
using Moka.Red.Core.Theming;

namespace Moka.Red.Core.Tests.Theming;

// Every series palette the new design writes, checked the way the chart board checked it: the data viz method's
// validator (validate_palette.js), written again here. Light colors are checked on white and dark ones on the Red, Gate
// and Matrix surfaces. A checked-in order that drops under the bar fails here.
public sealed class ChartPaletteTests
{
	private static readonly string[] LightSurfaces = ["#FFFFFF"];
	private static readonly string[] DarkSurfaces = ["#141417", "#111723", "#0C0C10"];

	public static TheoryData<string> AccentPresets => [.. MokaAccent.Presets.Select(accent => accent.Id)];

	public static TheoryData<string> PresetsThatLead => [.. MokaAccent.Presets.Where(accent => accent != MokaAccent.Silver).Select(accent => accent.Id)];

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public void Reference_PassesTheBar_DonutWrapIncluded(bool isDark) =>
		AssertPasses(MokaChartPaletteValues.Series(MokaChartPalette.Reference, isDark, "#000000"), isDark, donut: true);

	// The board: when a third slice touches the first, teal meets blue at 14.5 light and 13.0 dark under normal vision, so
	// Muted is checked as the lines and bars it was picked for.
	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public void Muted_PassesTheBar_ForTouchingSeries(bool isDark) =>
		AssertPasses(MokaChartPaletteValues.Series(MokaChartPalette.Muted, isDark, "#000000"), isDark, donut: false);

	[Theory]
	[MemberData(nameof(PresetsThatLead))]
	public void AccentFirst_PassesTheBar_ForEveryPresetAndScheme(string id)
	{
		MokaAccent accent = Accent(id);

		AssertPasses(AccentFirst(accent, isDark: false), isDark: false, donut: true);
		AssertPasses(AccentFirst(accent, isDark: true), isDark: true, donut: true);
	}

	[Theory]
	[MemberData(nameof(PresetsThatLead))]
	public void AccentFirst_KeepsOneOrderInBothSchemes(string id)
	{
		MokaAccent accent = Accent(id);
		IReadOnlyList<string> light = AccentFirst(accent, isDark: false);
		IReadOnlyList<string> dark = AccentFirst(accent, isDark: true);

		for (int slot = 1; slot < MokaChartPaletteValues.Slots; slot++)
		{
			Assert.True(HueGap(light[slot], dark[slot]) < 20, $"{id} slot {slot + 1}: {light[slot]} and {dark[slot]} are different hues.");
		}
	}

	// Slot 1 is the accent itself wherever the accent passes on its own; elsewhere it is moved into the band at its hue.
	[Theory]
	[MemberData(nameof(PresetsThatLead))]
	public void AccentFirst_LeadsWithTheAccent(string id)
	{
		MokaAccent accent = Accent(id);

		foreach (bool isDark in new[] { false, true })
		{
			string first = AccentFirst(accent, isDark)[0];
			string own = accent.For(isDark).Color;

			if (PaletteValidator.ColorPasses(own, isDark, Surfaces(isDark)))
			{
				Assert.Equal(own, first, ignoreCase: true);
			}
			else
			{
				Assert.True(HueGap(own, first) < 8, $"{id} {(isDark ? "dark" : "light")}: {first} is not the hue of {own}.");
			}
		}
	}

	[Theory]
	[MemberData(nameof(PresetsThatLead))]
	public void AccentFirst_DropsTheReferenceHueNearestTheAccent(string id)
	{
		MokaAccent accent = Accent(id);

		foreach (bool isDark in new[] { false, true })
		{
			IReadOnlyList<string> reference = MokaChartPaletteValues.Series(MokaChartPalette.Reference, isDark, "#000000");
			string nearest = reference.MinBy(hue => HueGap(hue, accent.Light.Color))!;
			List<string> rest = [.. AccentFirst(accent, isDark).Skip(1)];

			Assert.Equal(7, rest.Count);
			foreach (string color in reference.Where(color => color != nearest))
			{
				string match = rest.MinBy(candidate => PaletteValidator.DeltaE(candidate, color))!;
				Assert.True(PaletteValidator.DeltaE(match, color) < 3, $"{id}: {color} is missing.");
			}
		}
	}

	[Fact]
	public void AccentFirst_OnRed_IsTheBoards()
	{
		Assert.Equal(["#C62828", "#2D73E5", "#08A397", "#753FDB", "#EC6B06", "#E568A1", "#BF8800", "#007936"], AccentFirst(MokaAccent.Red, isDark: false));
		Assert.Equal(["#EF5350", "#3D83F6", "#059D90", "#9778EE", "#D66002", "#DB4790", "#BB8600", "#007936"], AccentFirst(MokaAccent.Red, isDark: true));
	}

	// Silver is under the chroma floor, so it would read as a grey series.
	[Fact]
	public void AccentFirst_OnSilver_IsTheReference()
	{
		Assert.False(PaletteValidator.ColorPasses(MokaAccent.Silver.Dark.Color, isDark: true, DarkSurfaces));
		Assert.Equal(MokaChartPaletteValues.ReferenceLight, AccentFirst(MokaAccent.Silver, isDark: false));
		Assert.Equal(MokaChartPaletteValues.ReferenceDark, AccentFirst(MokaAccent.Silver, isDark: true));
	}

	[Theory]
	[MemberData(nameof(AccentPresets))]
	public void TheSequentialRamp_ReadsAsARamp(string id)
	{
		foreach (bool isDark in new[] { false, true })
		{
			IReadOnlyList<string> ramp = MokaChartPaletteValues.Sequential(isDark, Accent(id).For(isDark).Color);

			AssertRamp(ramp, $"{id} {(isDark ? "dark" : "light")} sequential");
			Assert.True(PaletteValidator.Lightness(ramp[0]) > PaletteValidator.Lightness(ramp[^1]) != isDark, "Low values sit nearest the surface.");
		}
	}

	// A heatmap's lowest step fades into the page on purpose, so only ordinal steps, drawn as separate stages, must stand
	// 2:1 off the surface.
	[Theory]
	[MemberData(nameof(AccentPresets))]
	public void TheOrdinalSteps_ReadAsARamp_AndTheLightestStandsOffThePage(string id)
	{
		foreach (bool isDark in new[] { false, true })
		{
			IReadOnlyList<string> steps = MokaChartPaletteValues.Ordinal(isDark, Accent(id).For(isDark).Color);

			AssertRamp(steps, $"{id} {(isDark ? "dark" : "light")} ordinal");
			string nearestThePage = isDark ? steps[^1] : steps.MaxBy(PaletteValidator.Lightness)!;
			foreach (string surface in Surfaces(isDark))
			{
				double contrast = PaletteValidator.Contrast(nearestThePage, surface);
				Assert.True(contrast >= 2, $"{id}: {nearestThePage} is {contrast:0.00}:1 on {surface}.");
			}
		}
	}

	[Fact]
	public void RedsRamps_AreTheBoards()
	{
		Assert.Equal(["#FEE0DC", "#FFB0A7", "#FF7267", "#DE4640", "#B7191E"], MokaChartPaletteValues.Sequential(false, MokaAccent.Red.Light.Color));
		Assert.Equal(["#5D0004", "#8A010D", "#BA1E21", "#E24943", "#FF7F74"], MokaChartPaletteValues.Sequential(true, MokaAccent.Red.Dark.Color));
		Assert.Equal(["#95000D", "#B3131A", "#CC3431", "#E54D46", "#FF645B"], MokaChartPaletteValues.Ordinal(false, MokaAccent.Red.Light.Color));
		Assert.Equal(["#FEABA1", "#FF857A", "#F75E55", "#DE4640", "#C52C2A"], MokaChartPaletteValues.Ordinal(true, MokaAccent.Red.Dark.Color));
	}

	// The other presets' ramps come from the rule read off Red's: its lightness steps at the light accent's hue, as much
	// chroma as the accent has. Red itself lands within 1 of the board.
	[Theory]
	[MemberData(nameof(AccentPresets))]
	public void EveryPresetsRamps_FollowTheRule(string id)
	{
		MokaAccent accent = Accent(id);
		(double _, double lightChroma, double hue) = Oklch(accent.Light.Color);
		double chroma = Math.Max(lightChroma, Oklch(accent.Dark.Color).Chroma);

		(IReadOnlyList<string> Checked, double[] Lightness)[] ramps =
		[
			(MokaChartPaletteValues.Sequential(false, accent.Light.Color), MokaChartPaletteValues.SequentialLightnessLight),
			(MokaChartPaletteValues.Sequential(true, accent.Dark.Color), MokaChartPaletteValues.SequentialLightnessDark),
			(MokaChartPaletteValues.Ordinal(false, accent.Light.Color), MokaChartPaletteValues.OrdinalLightnessLight),
			(MokaChartPaletteValues.Ordinal(true, accent.Dark.Color), MokaChartPaletteValues.OrdinalLightnessDark)
		];

		foreach ((IReadOnlyList<string> checkedIn, double[] lightness) in ramps)
		{
			string[] made = MokaChartPaletteValues.Ramp(chroma, hue, lightness);
			for (int i = 0; i < made.Length; i++)
			{
				Assert.True(PaletteValidator.DeltaE(made[i], checkedIn[i]) < 1, $"{id}: {checkedIn[i]} should be about {made[i]}.");
			}
		}
	}

	[Fact]
	public void ACustomAccent_LeadsTheReferenceWithoutItsNearestHue()
	{
		MokaTheme theme = MokaTheme.Red.WithAccent(MokaAccent.FromHue(140));
		IReadOnlyList<string> series = MokaChartPaletteValues.Series(MokaChartPalette.AccentFirst, true, theme.Palette.Primary);

		Assert.Equal(theme.Palette.Primary, series[0]);
		Assert.Equal(MokaChartPaletteValues.ReferenceDark.Where(color => color != "#028039"), series.Skip(1));
	}

	[Fact]
	public void ACustomAccent_GetsRampsAtItsHue()
	{
		string primary = MokaTheme.Gate.WithAccent(MokaAccent.FromHue(140)).Palette.Primary;
		IReadOnlyList<string> ramp = MokaChartPaletteValues.Sequential(true, primary);

		AssertRamp(ramp, "custom");
		Assert.True(HueGap(ramp[2], primary) < 8);
	}

	[Theory]
	[InlineData("#808080")]
	[InlineData("var(--brand)")]
	public void AnAccentThatCantLead_LeavesTheReference(string primary) =>
		Assert.Equal(MokaChartPaletteValues.ReferenceLight, MokaChartPaletteValues.Series(MokaChartPalette.AccentFirst, false, primary));

	[Fact]
	public void AnAccentThatIsntHex_GetsRampsMixedFromTheToken()
	{
		IReadOnlyList<string> ramp = MokaChartPaletteValues.Sequential(false, "var(--brand)");

		Assert.Equal("color-mix(in oklab, var(--moka-color-primary) 20%, var(--moka-color-surface))", ramp[0]);
		Assert.Equal("var(--moka-color-primary)", ramp[^1]);
		Assert.Equal("var(--moka-color-primary)", MokaChartPaletteValues.Ordinal(false, "var(--brand)")[0]);
	}

	// The helper is the board's validator only if it gives the board's numbers.
	[Fact]
	public void TheValidator_GivesTheBoardsNumbers()
	{
		string[] reference = MokaChartPaletteValues.ReferenceLight;

		Assert.Equal(13.8, Math.Round(PaletteValidator.WorstColorBlind(reference, PaletteValidator.Adjacent(8)), 1));
		Assert.Equal(20.0, Math.Round(PaletteValidator.WorstNormal(reference, PaletteValidator.Adjacent(8)), 1));
		Assert.Equal(12.6, Math.Round(PaletteValidator.WorstColorBlind(MokaChartPaletteValues.ReferenceDark, PaletteValidator.Adjacent(8)), 1));
		Assert.Equal(16.2, Math.Round(PaletteValidator.WorstColorBlind(MokaChartPaletteValues.MutedLight, PaletteValidator.Adjacent(8)), 1));
	}

	private static void AssertPasses(IReadOnlyList<string> palette, bool isDark, bool donut)
	{
		Assert.Equal(MokaChartPaletteValues.Slots, palette.Count);
		string scheme = isDark ? "dark" : "light";
		foreach (string color in palette)
		{
			Assert.True(PaletteValidator.ColorPasses(color, isDark, Surfaces(isDark)), $"{scheme}: {color} is outside the band, under the chroma floor or under 3:1.");
		}

		IReadOnlyList<(int, int)> pairs = donut ? PaletteValidator.WithDonutWrap(palette.Count) : PaletteValidator.Adjacent(palette.Count);
		double colorBlind = PaletteValidator.WorstColorBlind(palette, pairs);
		double normal = PaletteValidator.WorstNormal(palette, pairs);
		Assert.True(colorBlind >= PaletteValidator.ColorBlindTarget, $"{scheme}: color blind worst {colorBlind:0.0}.");
		Assert.True(normal >= PaletteValidator.NormalFloor, $"{scheme}: normal vision worst {normal:0.0}.");
	}

	private static void AssertRamp(IReadOnlyList<string> ramp, string what)
	{
		Assert.Equal(MokaChartPaletteValues.Steps, ramp.Count);
		double[] lightness = [.. ramp.Select(PaletteValidator.Lightness)];
		bool rising = lightness[1] > lightness[0];
		for (int i = 1; i < lightness.Length; i++)
		{
			Assert.True(lightness[i] > lightness[i - 1] == rising, $"{what}: lightness is not monotone.");
			Assert.True(Math.Abs(lightness[i] - lightness[i - 1]) >= 0.06, $"{what}: {ramp[i - 1]} and {ramp[i]} are too close.");
		}

		double[] hues = [.. ramp.Select(color => Oklch(color).Hue)];
		double spread = hues.SelectMany(a => hues.Select(b => HueGap(a, b))).Max();
		Assert.True(spread <= 40, $"{what}: hues spread {spread:0} degrees, more than one hue.");
	}

	private static IReadOnlyList<string> AccentFirst(MokaAccent accent, bool isDark) =>
		MokaChartPaletteValues.Series(MokaChartPalette.AccentFirst, isDark, accent.For(isDark).Color);

	private static MokaAccent Accent(string id) => MokaAccent.Presets.Single(accent => accent.Id == id);

	private static string[] Surfaces(bool isDark) => isDark ? DarkSurfaces : LightSurfaces;

	private static (double Lightness, double Chroma, double Hue) Oklch(string hex) => PaletteValidator.Oklch(hex);

	private static double HueGap(string a, string b) => HueGap(Oklch(a).Hue, Oklch(b).Hue);

	private static double HueGap(double a, double b)
	{
		double gap = Math.Abs(a - b) % 360;
		return gap > 180 ? 360 - gap : gap;
	}
}

/// <summary>
///     The data viz method's palette checks: OKLCH lightness band and chroma floor, WCAG contrast, and OKLab distance
///     (times 100) between touching colors under normal vision and under protanopia and deuteranopia simulated with the
///     Machado, Oliveira and Fernandes (2009) matrices at full severity.
/// </summary>
internal static class PaletteValidator
{
	internal const double ColorBlindTarget = 8;
	internal const double NormalFloor = 15;
	internal const double ChromaFloor = 0.10;
	internal const double ContrastFloor = 3;

	private static readonly double[][] Protan =
		[[0.152286, 1.052583, -0.204868], [0.114503, 0.786281, 0.099216], [-0.003882, -0.048116, 1.051998]];

	private static readonly double[][] Deutan =
		[[0.367322, 0.860646, -0.227968], [0.280085, 0.672501, 0.047413], [-0.011820, 0.042940, 0.968881]];

	/// <summary>Neighbours in a line, bar or stack.</summary>
	internal static IReadOnlyList<(int, int)> Adjacent(int count) =>
		[.. Enumerable.Range(0, count - 1).Select(i => (i, i + 1))];

	/// <summary>Neighbours, plus a donut's last slice against its first for three and four slices.</summary>
	internal static IReadOnlyList<(int, int)> WithDonutWrap(int count) =>
		[.. Adjacent(count), .. Enumerable.Range(3, Math.Max(0, Math.Min(4, count) - 2)).Select(slices => (slices - 1, 0))];

	internal static bool ColorPasses(string hex, bool isDark, IEnumerable<string> surfaces)
	{
		(double lightness, double chroma, _) = Oklch(hex);
		(double low, double high) = isDark ? (0.48, 0.67) : (0.43, 0.77);
		return lightness >= low && lightness <= high && chroma >= ChromaFloor && surfaces.All(surface => Contrast(hex, surface) >= ContrastFloor);
	}

	internal static double WorstColorBlind(IReadOnlyList<string> palette, IReadOnlyList<(int, int)> pairs) =>
		pairs.Min(pair => Math.Min(DeltaE(palette[pair.Item1], palette[pair.Item2], Protan), DeltaE(palette[pair.Item1], palette[pair.Item2], Deutan)));

	internal static double WorstNormal(IReadOnlyList<string> palette, IReadOnlyList<(int, int)> pairs) =>
		pairs.Min(pair => DeltaE(palette[pair.Item1], palette[pair.Item2]));

	internal static double DeltaE(string a, string b, double[][]? simulate = null) =>
		100 * Simulated(a, simulate).ToOklab().DistanceTo(Simulated(b, simulate).ToOklab());

	internal static double Lightness(string hex) => Oklch(hex).Lightness;

	internal static (double Lightness, double Chroma, double Hue) Oklch(string hex)
	{
		MokaOklch oklch = MokaCssColor.Parse(hex).ToOklch();
		return (oklch.L, oklch.C, oklch.H);
	}

	internal static double Contrast(string a, string b) => MokaContrast.Ratio(MokaCssColor.Parse(a), MokaCssColor.Parse(b));

	/// <summary>A CSS <c>color-mix(in oklab, a p%, b)</c>.</summary>
	internal static string MixOklab(string a, string b, double share) =>
		MokaColorMix.InOklab(MokaCssColor.Parse(a), MokaCssColor.Parse(b), share).ToHex(uppercase: true);

	private static MokaLinearRgb Simulated(string hex, double[][]? m)
	{
		MokaLinearRgb c = MokaCssColor.Parse(hex).ToLinear();
		if (m is null)
		{
			return c;
		}

		double Row(int i) => Math.Clamp((m[i][0] * c.R) + (m[i][1] * c.G) + (m[i][2] * c.B), 0, 1);
		return new MokaLinearRgb(Row(0), Row(1), Row(2));
	}
}
