using System.Collections.Concurrent;
using System.Globalization;
using Moka.Red.Core.Colors;

namespace Moka.Red.Core.Theming;

/// <summary>
///     The chart tokens (<c>--moka-chart-*</c>): series colors for each palette, scheme and accent preset, the ramps that
///     follow the accent, and the greys behind the marks.
/// </summary>
internal static class MokaChartPaletteValues
{
	/// <summary>How many series colors a palette has before they repeat.</summary>
	internal const int Slots = 8;

	/// <summary>How many steps the sequential and ordinal ramps have.</summary>
	internal const int Steps = 5;

	// The theme roles in turn, as the charts have always drawn them. The eighth comes round to the accent again.
	internal static readonly string[] Rotation =
	[
		"var(--moka-color-primary)", "var(--moka-color-secondary)", "var(--moka-color-success)", "var(--moka-color-info)",
		"var(--moka-color-warning)", "var(--moka-color-error)", "var(--moka-color-on-surface)", "var(--moka-color-primary)"
	];

	// Blue, orange, teal, amber, pink, green, violet, red: the data viz method's hues moved onto Moka's label hues and
	// stepped so all eight clear 3:1 on white, and dark as its own steps. Orange and amber never touch, since they read
	// alike to color-blind eyes, and green and red come last because Moka uses them for status.
	internal static readonly string[] ReferenceLight = ["#3073E2", "#ED6B03", "#0CA397", "#BF8804", "#E36AA1", "#028039", "#7541D9", "#EB3F41"];

	internal static readonly string[] ReferenceDark = ["#4083F3", "#D56004", "#019D90", "#BB8605", "#D9498F", "#028039", "#9679EB", "#EA5A56"];

	// Lower chroma, just over the floor, so lightness does more of the separating and neighbours alternate light and dark.
	internal static readonly string[] MutedLight = ["#4F7CBF", "#A05725", "#01A6A0", "#9B6C0A", "#85375D", "#4C985F", "#574189", "#B35B59"];

	internal static readonly string[] MutedDark = ["#5D8BD0", "#A75D2B", "#01A6A0", "#A77820", "#98496E", "#58A46A", "#9682CF", "#C76C6B"];

	// Slot 1 is the preset's accent, moved into the lightness band at its own hue where the accent itself sits outside it
	// (Blue, Orchid and Pink in dark, Cyan in both). The rest are the reference hues without the one nearest the accent, in
	// the order a search found to keep every touching pair apart, a donut's last slice against its first up to four, in
	// both schemes. Red's is the board's, whose amber and green moved a step to clear the bar. Silver is too grey to lead,
	// so it has none and takes the reference order.
	private static readonly AccentValues[] Accents =
	[
		new(MokaAccent.Red,
			["#C62828", "#2D73E5", "#08A397", "#753FDB", "#EC6B06", "#E568A1", "#BF8800", "#007936"],
			["#EF5350", "#3D83F6", "#059D90", "#9778EE", "#D66002", "#DB4790", "#BB8600", "#007936"],
			["#FEE0DC", "#FFB0A7", "#FF7267", "#DE4640", "#B7191E"],
			["#5D0004", "#8A010D", "#BA1E21", "#E24943", "#FF7F74"],
			["#95000D", "#B3131A", "#CC3431", "#E54D46", "#FF645B"],
			["#FEABA1", "#FF857A", "#F75E55", "#DE4640", "#C52C2A"]),
		new(MokaAccent.Blue,
			["#2F58D6", "#028039", "#E36AA1", "#BF8804", "#0CA397", "#ED6B03", "#7541D9", "#EB3F41"],
			["#6B8EF7", "#028039", "#D9498F", "#BB8605", "#019D90", "#D56004", "#9679EB", "#EA5A56"],
			["#DDE8FF", "#ADC7FF", "#79A1FF", "#4978F9", "#2C55D2"],
			["#07058F", "#1533B0", "#2F58D6", "#4C7BFC", "#83A8FF"],
			["#193AB7", "#2A51CF", "#3C68E7", "#4F7EFF", "#7099FF"],
			["#A9C3FF", "#87ABFF", "#6692FF", "#4978F9", "#3761E0"]),
		new(MokaAccent.Violet,
			["#5B45E0", "#ED6B03", "#0CA397", "#BF8804", "#E36AA1", "#028039", "#3073E2", "#EB3F41"],
			["#8F7DFF", "#D56004", "#019D90", "#BB8605", "#D9498F", "#028039", "#4083F3", "#EA5A56"],
			["#E3E6FF", "#BDC1FF", "#9697FF", "#7467FF", "#5840DB"],
			["#2A0083", "#4116B9", "#5A44DF", "#776CFF", "#9D9FFF"],
			["#4520C0", "#553DD8", "#6755F0", "#7A71FF", "#8F8FFF"],
			["#BABDFF", "#A0A3FF", "#8987FF", "#7467FF", "#624EE9"]),
		new(MokaAccent.Cyan,
			["#06829C", "#ED6B03", "#7541D9", "#BF8804", "#E36AA1", "#028039", "#3073E2", "#EB3F41"],
			["#3BA4BC", "#D56004", "#9679EB", "#BB8605", "#D9498F", "#028039", "#4083F3", "#EA5A56"],
			["#C3F1FF", "#58DAFD", "#27B6D8", "#0092B0", "#006F86"],
			["#003440", "#005062", "#00728A", "#0095B4", "#32BDDF"],
			["#005669", "#006C82", "#00829C", "#0099B7", "#1AB0D2"],
			["#54D7FA", "#37C0E2", "#04A9CB", "#0092B0", "#007B95"]),
		new(MokaAccent.Orchid,
			["#9330B8", "#ED6B03", "#0CA397", "#BF8804", "#E36AA1", "#028039", "#3073E2", "#EB3F41"],
			["#C369E0", "#D56004", "#019D90", "#BB8605", "#D9498F", "#028039", "#4083F3", "#EA5A56"],
			["#F5DFFF", "#E6AEFF", "#D474FD", "#B050D7", "#8E29B2"],
			["#48005F", "#6D008E", "#912DB6", "#B453DA", "#D97EFF"],
			["#750098", "#8B25AF", "#A03FC6", "#B757DE", "#CE6DF6"],
			["#E4A9FF", "#DA84FF", "#C767EF", "#B050D7", "#9A38C0"]),
		new(MokaAccent.Pink,
			["#B02A88", "#BF8804", "#3073E2", "#028039", "#7541D9", "#ED6B03", "#0CA397", "#EB3F41"],
			["#D369B4", "#BB8605", "#4083F3", "#028039", "#9679EB", "#D56004", "#019D90", "#EA5A56"],
			["#FFDCF0", "#FFA7DD", "#F36BC4", "#CC47A1", "#A71F80"],
			["#55003E", "#7F005F", "#AA2383", "#D04AA4", "#FA71CB"],
			["#880066", "#A31A7D", "#BB3692", "#D34EA8", "#EC65BE"],
			["#FFA1DB", "#FD75CE", "#E55EB7", "#CC47A1", "#B42F8C"]),
		new(MokaAccent.Silver,
			null,
			null,
			["#DCE9FF", "#BBC8DF", "#99A5BC", "#788499", "#586478"],
			["#242E40", "#3D485B", "#5B667B", "#7B879C", "#9FABC2"],
			["#434D61", "#566175", "#69758A", "#7E8A9F", "#939FB5"],
			["#B8C5DC", "#A2AFC5", "#8D99AF", "#788499", "#646F84"])
	];

	// OKLCH lightness of each step, read off the board's red ramps. Sequential runs from the surface to the strongest, so
	// a low value fades into the page; ordinal stays saturated, strongest first, for stages read in order.
	internal static readonly double[] SequentialLightnessLight = [0.93, 0.83, 0.72, 0.61, 0.50];
	internal static readonly double[] SequentialLightnessDark = [0.30, 0.40, 0.51, 0.62, 0.74];
	internal static readonly double[] OrdinalLightnessLight = [0.42, 0.49, 0.56, 0.63, 0.70];
	internal static readonly double[] OrdinalLightnessDark = [0.82, 0.75, 0.68, 0.61, 0.54];

	// Below this OKLCH chroma a color reads as grey, so it can't tell series apart.
	internal const double ChromaFloor = 0.10;

	private static readonly int[] MixPercents = [20, 40, 60, 80];

	private static readonly double[] ReferenceHuesLight = [.. ReferenceLight.Select(HueOf)];

	private static readonly double[] ReferenceHuesDark = [.. ReferenceDark.Select(HueOf)];

	private static readonly string[] Names = BuildNames();

	private static readonly string[] MatrixValues = BuildMatrixValues();

	private const int GateCacheSize = 64;

	private static readonly ConcurrentDictionary<GateKey, string[]> GateCache = new();

	// Every chart token's name, in the order Values gives them.
	internal static IReadOnlyList<string> TokenNames => Names;

	// Every chart token's value for a theme, in the order of TokenNames.
	internal static IReadOnlyList<string> Values(MokaTheme theme)
	{
		if (theme.Look != MokaLook.Gate)
		{
			return MatrixValues;
		}

		var key = new GateKey(theme.ChartPalette, theme.IsDark, theme.Palette.Primary);
		if (GateCache.TryGetValue(key, out string[]? cached))
		{
			return cached;
		}

		// Few keys repeat in practice, so a full cache is emptied rather than tracked; an app that makes a theme for every
		// accent a slider passes through never holds more than this.
		if (GateCache.Count >= GateCacheSize)
		{
			GateCache.Clear();
		}

		string[] values = GateValues(theme);
		GateCache[key] = values;
		return values;
	}

	/// <summary>The eight series colors a palette gives on the new design.</summary>
	/// <param name="palette">The palette.</param>
	/// <param name="isDark">Whether the scheme is dark.</param>
	/// <param name="primary">The theme's accent, which <see cref="MokaChartPalette.AccentFirst" /> leads with.</param>
	internal static IReadOnlyList<string> Series(MokaChartPalette palette, bool isDark, string primary) => palette switch
	{
		MokaChartPalette.Reference => isDark ? ReferenceDark : ReferenceLight,
		MokaChartPalette.Muted => isDark ? MutedDark : MutedLight,
		MokaChartPalette.AccentFirst => AccentFirst(isDark, primary),
		_ => Rotation
	};

	/// <summary>The sequential ramp, low to high, for an accent on the new design.</summary>
	internal static IReadOnlyList<string> Sequential(bool isDark, string primary) =>
		ValuesFor(primary, isDark) is { } preset
			? isDark ? preset.SequentialDark : preset.SequentialLight
			: Ramp(primary, isDark ? SequentialLightnessDark : SequentialLightnessLight) ?? SurfaceMixes(reverse: false);

	/// <summary>The ordinal steps, first stage first, for an accent on the new design.</summary>
	internal static IReadOnlyList<string> Ordinal(bool isDark, string primary) =>
		ValuesFor(primary, isDark) is { } preset
			? isDark ? preset.OrdinalDark : preset.OrdinalLight
			: Ramp(primary, isDark ? OrdinalLightnessDark : OrdinalLightnessLight) ?? SurfaceMixes(reverse: true);

	/// <summary>
	///     One step per lightness at an accent's OKLCH hue, each as saturated as the accent or as sRGB can show, whichever
	///     is less. Null when the accent isn't a hex color.
	/// </summary>
	internal static string[]? Ramp(string accent, IReadOnlyList<double> lightness)
	{
		if (!MokaColorMath.TryParseHex(accent, out int r, out int g, out int b))
		{
			return null;
		}

		MokaOklch oklch = MokaSrgb.FromBytes(r, g, b).ToOklch();
		return Ramp(oklch.C, oklch.H, lightness);
	}

	/// <summary>
	///     One step per lightness at this OKLCH hue, each with this chroma or as much as sRGB can show. The preset ramps were
	///     made this way from the light accent's hue and the larger of its two chromas.
	/// </summary>
	internal static string[] Ramp(double chroma, double hue, IReadOnlyList<double> lightness) =>
		[.. lightness.Select(step => new MokaOklch(step, chroma, hue).ReduceChromaToFit().ToSrgb().ToHex(uppercase: true))];

	private static AccentValues? ValuesFor(string primary, bool isDark) =>
		Accents.FirstOrDefault(values => string.Equals(values.Accent.For(isDark).Color, primary, StringComparison.OrdinalIgnoreCase));

	// A preset has its own checked order. Any other accent leads the reference hues without the one nearest it, unchecked,
	// and one too grey to tell from the others leads nothing.
	private static string[] AccentFirst(bool isDark, string primary)
	{
		string[] reference = isDark ? ReferenceDark : ReferenceLight;
		if (ValuesFor(primary, isDark) is { } preset)
		{
			return (isDark ? preset.AccentFirstDark : preset.AccentFirstLight) ?? reference;
		}

		if (!MokaColorMath.TryParseHex(primary, out int r, out int g, out int b))
		{
			return reference;
		}

		MokaOklch accent = MokaSrgb.FromBytes(r, g, b).ToOklch();
		if (accent.C < ChromaFloor)
		{
			return reference;
		}

		double[] hues = isDark ? ReferenceHuesDark : ReferenceHuesLight;
		int nearest = 0;
		for (int i = 1; i < hues.Length; i++)
		{
			if (HueDistance(hues[i], accent.H) < HueDistance(hues[nearest], accent.H))
			{
				nearest = i;
			}
		}

		return [primary, .. reference.Where((_, i) => i != nearest)];
	}

	private static double HueOf(string hex) =>
		MokaColorMath.TryParseHex(hex, out int r, out int g, out int b) ? MokaSrgb.FromBytes(r, g, b).ToOklch().H : 0;

	private static string[] GateValues(MokaTheme theme)
	{
		bool dark = theme.IsDark;
		string primary = theme.Palette.Primary;
		List<string> values = [.. Series(theme.ChartPalette, dark, primary)];
		values.Add(dark ? "#3B82F6" : "#2563EB");
		values.Add(dark ? "#EF5350" : "#C62828");
		values.Add(OverSurface(34));
		values.Add(OverSurface(7));
		values.Add(OverSurface(22));
		values.Add(OverSurface(5));
		values.Add(OverSurface(10));
		values.Add(OverSurface(17));
		values.AddRange(Sequential(dark, primary));
		values.AddRange(Ordinal(dark, primary));
		values.Add(OverSurface(5));
		return [.. values];
	}

	// Mixed in OKLab, so a step reads as the same lightness on graphite, blue-grey and white alike.
	private static string OverSurface(int percent) =>
		string.Create(CultureInfo.InvariantCulture, $"color-mix(in oklab, var(--moka-color-on-surface) {percent}%, var(--moka-color-surface))");

	private static string[] SurfaceMixes(bool reverse)
	{
		string[] steps = [.. MixPercents
			.Select(percent => string.Create(CultureInfo.InvariantCulture, $"color-mix(in oklab, var(--moka-color-primary) {percent}%, var(--moka-color-surface))"))
			.Append("var(--moka-color-primary)")];
		return reverse ? [.. Enumerable.Reverse(steps)] : steps;
	}

	private static string[] BuildNames()
	{
		List<string> names = [];
		for (int slot = 1; slot <= Slots; slot++)
		{
			names.Add(Name("", slot));
		}

		names.AddRange(["--moka-chart-up", "--moka-chart-down", "--moka-chart-muted", "--moka-chart-grid", "--moka-chart-axis"]);
		for (int band = 1; band <= 3; band++)
		{
			names.Add(Name("band-", band));
		}

		for (int step = 1; step <= Steps; step++)
		{
			names.Add(Name("seq-", step));
		}

		for (int step = 1; step <= Steps; step++)
		{
			names.Add(Name("ord-", step));
		}

		names.Add("--moka-chart-zero");
		return [.. names];
	}

	private static string Name(string group, int number) =>
		string.Create(CultureInfo.InvariantCulture, $"--moka-chart-{group}{number}");

	// Every value reads a token, so Dark and Light keep exactly the colors their charts had.
	private static string[] BuildMatrixValues()
	{
		string Transparent(string token, int percent) =>
			string.Create(CultureInfo.InvariantCulture, $"color-mix(in srgb, var({token}) {percent}%, transparent)");

		string[] sequential = [.. MixPercents.Select(p => Transparent("--moka-color-primary", p)).Append("var(--moka-color-primary)")];

		return
		[
			.. Rotation,
			"var(--moka-color-info)",
			"var(--moka-color-error)",
			"var(--moka-color-on-surface-tertiary)",
			"var(--moka-color-outline-variant)",
			"var(--moka-color-outline-variant)",
			Transparent("--moka-color-on-surface", 6),
			Transparent("--moka-color-on-surface", 10),
			Transparent("--moka-color-on-surface", 16),
			.. sequential,
			.. Enumerable.Reverse(sequential),
			"var(--moka-color-outline-variant)"
		];
	}

	private static double HueDistance(double a, double b)
	{
		double distance = Math.Abs(a - b) % 360;
		return distance > 180 ? 360 - distance : distance;
	}

	private readonly record struct GateKey(MokaChartPalette Palette, bool IsDark, string Primary);

	private sealed record AccentValues(
		MokaAccent Accent,
		string[]? AccentFirstLight,
		string[]? AccentFirstDark,
		string[] SequentialLight,
		string[] SequentialDark,
		string[] OrdinalLight,
		string[] OrdinalDark);
}
