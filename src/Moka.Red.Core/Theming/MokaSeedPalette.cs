using Moka.Red.Core.Colors;

namespace Moka.Red.Core.Theming;

// A whole new-design palette from a seed. Surfaces and greys sit on a lightness ladder read off the Red and Gate presets
// (OKLCH lightness, with the neutral's chroma scaled per step the way those presets scale it); text, icons, lines and
// the accent's and meanings' shades then move in lightness, away from what they sit on, until they meet their contrast
// target. The accent itself is never moved.
internal static class MokaSeedPalette
{
	private const double MaxNeutralChroma = 0.1;
	private const double LightnessStep = 0.0025;

	// How far high contrast moves secondary text toward the main text, and borders away from the surface.
	private const double HighContrastTextPull = 0.35;
	private const double HighContrastLinePush = 0.10;

	private static readonly Ladder Dark = new(
		Background: new(0.160, 0.65), Surface2: new(0.180, 0.85), Surface: new(0.199, 1.0), Raised: new(0.214, 1.15),
		Variant: new(0.227, 1.2), Hover: new(0.247, 1.4), Surface3: new(0.247, 1.4), NeutralBg: new(0.276, 1.8),
		OutlineVariant: new(0.250, 1.2), Outline: new(0.284, 1.4), OutlineStrong: new(0.332, 1.75), OutlineHover: new(0.386, 1.9),
		Quaternary: new(0.389, 1.4), Tertiary: new(0.590, 1.4), Icon: new(0.630, 1.35), Muted: new(0.708, 1.3),
		Text2: new(0.837, 0.9), Text: new(0.941, 0.45));

	private static readonly Ladder Light = new(
		Background: new(0.972, 0.25), Surface2: new(0.980, 0.2), Surface: new(1.0, 0), Raised: new(0.988, 0.12),
		Variant: new(0.955, 0.3), Hover: new(0.946, 0.35), Surface3: new(0.938, 0.4), NeutralBg: new(0.928, 0.45),
		OutlineVariant: new(0.934, 0.45), Outline: new(0.908, 0.55), OutlineStrong: new(0.851, 0.75), OutlineHover: new(0.773, 1.0),
		Quaternary: new(0.798, 0.85), Tertiary: new(0.523, 1.3), Icon: new(0.541, 1.4), Muted: new(0.474, 1.35),
		Text2: new(0.335, 1.3), Text: new(0.206, 1.1));

	// The new design's meanings, used where the seed gives none: the Gate presets' dark and light colors.
	private static readonly (string Dark, string Light)[] DefaultMeanings =
	[
		("#3DD68C", "#0E7A4A"), ("#FFB547", "#9A5B00"), ("#FF6369", "#C62832"), ("#5B9CFF", "#1F62D0")
	];

	internal static MokaPalette Derive(MokaThemeSeed seed, bool dark, bool highContrast)
	{
		MokaSrgb accent = Read(seed.Accent, nameof(MokaThemeSeed.Accent));
		if (accent.Alpha < 1)
		{
			throw new ArgumentException($"A seed's accent must be solid, got '{seed.Accent}'.", nameof(seed));
		}

		MokaOklch neutral = seed.Neutral is null ? default : Read(seed.Neutral, nameof(MokaThemeSeed.Neutral)).ToOklch();
		double chroma = Math.Clamp(seed.NeutralChroma ?? (seed.Neutral is null ? 0 : neutral.C), 0, MaxNeutralChroma);
		bool more = seed.Contrast == MokaSeedContrast.More;
		var targets = new Targets(
			Text: more ? MokaContrast.EnhancedTextRatio : MokaContrast.TextRatio,
			Icon: more ? MokaContrast.TextRatio : MokaContrast.LargeTextRatio,
			Line: MokaContrast.LargeTextRatio,
			AllLines: more);

		Ladder ladder = dark ? Dark : Light;
		var tones = new Tones(ladder, neutral.H, chroma, dark, highContrast);

		MokaSrgb background = tones.Surface(ladder.Background);
		MokaSrgb surface = tones.Surface(ladder.Surface);
		MokaSrgb surface2 = tones.Surface(ladder.Surface2);
		MokaSrgb raised = tones.Surface(ladder.Raised);
		MokaSrgb variant = tones.Surface(ladder.Variant);
		MokaSrgb[] grounds = [surface, variant, background, surface2, raised];

		MokaSrgb text = tones.Text(ladder.Text, grounds, Math.Max(targets.Text, MokaContrast.EnhancedTextRatio));
		MokaSrgb text2 = tones.Text(ladder.Text2, grounds, targets.Text);
		MokaSrgb muted = tones.Text(ladder.Muted, grounds, targets.Text);
		MokaSrgb tertiary = tones.Text(ladder.Tertiary, grounds, targets.Text);
		MokaSrgb icon = tones.Text(ladder.Icon, grounds, targets.Icon);
		MokaSrgb outline = tones.Line(ladder.Outline, grounds, targets.AllLines ? targets.Line : 0);
		MokaSrgb outlineStrong = tones.Line(ladder.OutlineStrong, grounds, targets.Line);
		MokaSrgb outlineHover = tones.Line(ladder.OutlineHover, grounds, Math.Max(targets.Line, MokaContrast.Ratio(outlineStrong, surface)));

		string ink = Hex(dark ? background : text);
		MokaPalette palette = (dark ? MokaPalette.Gate : MokaPalette.GateLight) with
		{
			Background = Hex(background),
			Surface2 = Hex(surface2),
			Surface = Hex(surface),
			SurfaceRaised = Hex(raised),
			SurfaceVariant = Hex(variant),
			SurfaceHover = Hex(tones.Surface(ladder.Hover)),
			Surface3 = Hex(tones.Surface(ladder.Surface3)),
			NeutralBg = Hex(tones.Surface(ladder.NeutralBg)),
			OutlineVariant = Hex(tones.Line(ladder.OutlineVariant, grounds, 0)),
			Outline = Hex(outline),
			OutlineStrong = Hex(outlineStrong),
			OutlineHover = Hex(outlineHover),
			OnSurfaceQuaternary = Hex(tones.Line(ladder.Quaternary, grounds, 0)),
			OnSurfaceTertiary = Hex(tertiary),
			Icon = Hex(icon),
			OnSurfaceVariant = Hex(muted),
			OnSurface2 = Hex(text2),
			OnSurface = Hex(text),
			OnBackground = Hex(text),
			Secondary = Hex(muted),
			SecondaryLight = Hex(text2),
			SecondaryDark = Hex(tertiary),
			OnSecondary = Hex(OnColor(muted, dark, background, text, targets.Text)),
			Knob = dark ? Hex(text) : Hex(MokaSrgb.White),
			Backdrop = dark
				? MokaCssColor.Format(background with { Alpha = 0.78 }, MokaCssFormat.LegacyRgba)
				: MokaCssColor.Format(text with { Alpha = 0.36 }, MokaCssFormat.LegacyRgba)
		};

		palette = WithAccent(palette, accent, dark, background, text, surface, grounds, targets);
		return WithMeanings(palette, seed, dark, background, surface, grounds, targets);
	}

	internal static MokaSrgb Read(string color, string what) =>
		MokaCssColor.TryParse(color, out MokaSrgb parsed)
			? parsed
			: throw new ArgumentException($"A seed's {what} must be a CSS color, got '{color}'.", what);

	private static MokaPalette WithAccent(
		MokaPalette palette, MokaSrgb accent, bool dark, MokaSrgb background, MokaSrgb text, MokaSrgb surface, MokaSrgb[] grounds, Targets targets)
	{
		MokaOklch a = accent.ToOklch();
		string hex = accent.ToHex(uppercase: true);

		// Links on dark cards read 7:1 on the presets, so seeds keep that.
		double linkTarget = dark ? MokaContrast.EnhancedTextRatio : targets.Text;
		MokaSrgb soft = dark
			? Solve(new MokaOklch(Math.Max(0.78, a.L + 0.14), a.C * 0.6, a.H), grounds, linkTarget, lighter: true)
			: Solve(new MokaOklch(Math.Min(0.55, a.L - 0.04), a.C, a.H), grounds, linkTarget, lighter: false);
		MokaSrgb tint = dark
			? Fit(new MokaOklch(0.232 + (surface.ToOklch().L - Dark.Surface.L), Math.Min(a.C * 0.21, 0.045), a.H))
			: Fit(new MokaOklch(0.947, Math.Min(a.C * 0.12, 0.025), a.H));
		MokaSrgb selectedText = Solve(soft.ToOklch(), [tint, .. grounds], targets.Text, lighter: dark);

		MokaPalette tiers = (dark ? MokaAccentTiers.GateDark : MokaAccentTiers.GateLight).ApplyTo(palette, hex);
		return tiers with
		{
			Primary = hex,
			PrimaryLight = Hex(soft),
			PrimaryDark = MokaColorMath.PressedShade(hex, Hex(dark ? background : text)),
			OnPrimary = Hex(OnColor(accent, dark, background, text, targets.Text)),
			PrimaryBg = Hex(tint),
			PrimaryText = Hex(selectedText)
		};
	}

	private static MokaPalette WithMeanings(
		MokaPalette palette, MokaThemeSeed seed, bool dark, MokaSrgb background, MokaSrgb surface, MokaSrgb[] grounds, Targets targets)
	{
		Meaning success = MakeMeaning(seed.Success, 0, dark, background, grounds, targets);
		Meaning warning = MakeMeaning(seed.Warning, 1, dark, background, grounds, targets);
		Meaning error = MakeMeaning(seed.Error, 2, dark, background, grounds, targets);
		Meaning info = MakeMeaning(seed.Info, 3, dark, background, grounds, targets);

		MokaOklch errorHue = Read(error.Strong, nameof(MokaThemeSeed.Error)).ToOklch();
		MokaSrgb soft = dark
			? Solve(new MokaOklch(0.763, errorHue.C * 0.75, errorHue.H), [Read(error.Bg, "error tint"), .. grounds], targets.Text, lighter: true)
			: Solve(new MokaOklch(0.502, errorHue.C * 0.93, errorHue.H), [Read(error.Bg, "error tint"), .. grounds], targets.Text, lighter: false);

		return palette with
		{
			Success = success.Strong, OnSuccess = success.On, SuccessDim = success.Dim,
			SuccessBg = success.Bg, SuccessInk = success.Ink, SuccessText = success.Text, SuccessLine = success.Line,
			Warning = warning.Strong, OnWarning = warning.On, WarningDim = warning.Dim,
			WarningBg = warning.Bg, WarningInk = warning.Ink, WarningText = warning.Text, WarningLine = warning.Line,
			Error = error.Strong, OnError = error.On, ErrorDim = error.Dim,
			ErrorBg = error.Bg, ErrorInk = error.Ink, ErrorText = error.Text, ErrorLine = error.Line, ErrorSoft = Hex(soft),
			Info = info.Strong, OnInfo = info.On, InfoDim = info.Dim,
			InfoBg = info.Bg, InfoInk = info.Ink, InfoText = info.Text, InfoLine = info.Line
		};
	}

	private static Meaning MakeMeaning(string? given, int index, bool dark, MokaSrgb background, MokaSrgb[] grounds, Targets targets)
	{
		string source = given ?? (dark ? DefaultMeanings[index].Dark : DefaultMeanings[index].Light);
		MokaOklch m = Read(source, "meaning color").ToOklch();
		MokaSrgb ink = dark ? background : MokaSrgb.White;

		// Kept as given where it reads on the surfaces with its own text color on it; moved in lightness otherwise.
		bool Reads(MokaSrgb candidate) =>
			grounds.Take(3).All(ground => MokaContrast.Ratio(candidate, ground) >= targets.Text) &&
			Math.Max(MokaContrast.Ratio(ink, candidate), MokaContrast.Ratio(dark ? MokaSrgb.White : MokaSrgb.Black, candidate)) >= targets.Text;

		MokaSrgb strong = Walk(m, Reads, lighter: dark);
		MokaSrgb tint = Fit(dark ? new MokaOklch(0.265, Math.Min(m.C * 0.28, 0.05), m.H) : new MokaOklch(0.952, Math.Min(m.C * 0.18, 0.03), m.H));
		MokaSrgb shortText = Solve(dark ? new MokaOklch(0.86, m.C * 0.5, m.H) : new MokaOklch(0.47, m.C * 0.9, m.H), [tint], targets.Text, lighter: dark);
		double shortRatio = MokaContrast.Ratio(shortText, tint);
		MokaSrgb longText = Solve(dark ? new MokaOklch(0.91, m.C * 0.32, m.H) : new MokaOklch(0.40, m.C * 0.78, m.H), [tint], Math.Max(targets.Text, shortRatio), lighter: dark);
		MokaSrgb line = Fit(dark ? new MokaOklch(0.42, m.C * 0.5, m.H) : new MokaOklch(0.84, m.C * 0.45, m.H));

		return new Meaning(
			Hex(strong),
			Hex(OnColor(strong, dark, background, MokaSrgb.Black, targets.Text)),
			MokaCssColor.Format(strong with { Alpha = dark ? 0.14 : 0.10 }, MokaCssFormat.LegacyRgba),
			Hex(tint),
			Hex(shortText),
			Hex(longText),
			Hex(line));
	}

	// The scheme's own ink first (the page's dark color on dark, white on light), then the other one, then whichever
	// end of the scale reads best.
	private static MokaSrgb OnColor(MokaSrgb fill, bool dark, MokaSrgb darkInk, MokaSrgb lightInk, double target)
	{
		MokaSrgb[] candidates = dark ? [darkInk, MokaSrgb.White, MokaSrgb.Black] : [MokaSrgb.White, lightInk, MokaSrgb.Black];
		foreach (MokaSrgb candidate in candidates)
		{
			if (MokaContrast.Ratio(candidate, fill) >= target)
			{
				return candidate;
			}
		}

		return candidates.MaxBy(candidate => MokaContrast.Ratio(candidate, fill));
	}

	private static MokaSrgb Solve(MokaOklch start, MokaSrgb[] grounds, double target, bool lighter) =>
		Walk(start, candidate => grounds.All(ground => MokaContrast.Ratio(candidate, ground) >= target), lighter);

	// Steps the lightness away from what the color sits on until it reads, keeping hue and as much chroma as sRGB allows.
	// At the end of the scale it gives the lightest or darkest step, the most contrast there is at that hue.
	private static MokaSrgb Walk(MokaOklch start, Func<MokaSrgb, bool> reads, bool lighter)
	{
		MokaOklch current = start;
		MokaSrgb color = Fit(current);
		while (!reads(color) && current.L is > 0 and < 1)
		{
			current = current with { L = Math.Clamp(current.L + (lighter ? LightnessStep : -LightnessStep), 0, 1) };
			color = Fit(current);
		}

		return color;
	}

	private static MokaSrgb Fit(MokaOklch color) => QuantizeToHex(color.ReduceChromaToFit().ToSrgb());

	// What is written is hex, so what is checked is the hex color.
	private static MokaSrgb QuantizeToHex(MokaSrgb color)
	{
		(int r, int g, int b) = color.ToBytes();
		return MokaSrgb.FromBytes(r, g, b);
	}

	private static string Hex(MokaSrgb color) => color.ToHex(uppercase: true);

	private readonly record struct Step(double L, double Chroma);

	private sealed record Ladder(
		Step Background, Step Surface2, Step Surface, Step Raised, Step Variant, Step Hover, Step Surface3, Step NeutralBg,
		Step OutlineVariant, Step Outline, Step OutlineStrong, Step OutlineHover, Step Quaternary, Step Tertiary, Step Icon,
		Step Muted, Step Text2, Step Text);

	private readonly record struct Targets(double Text, double Icon, double Line, bool AllLines);

	private readonly record struct Meaning(string Strong, string On, string Dim, string Bg, string Ink, string Text, string Line);

	private sealed class Tones(Ladder ladder, double hue, double chroma, bool dark, bool highContrast)
	{
		internal MokaSrgb Surface(Step step) => Fit(new MokaOklch(step.L, chroma * step.Chroma, hue));

		internal MokaSrgb Text(Step step, MokaSrgb[] grounds, double target)
		{
			double l = highContrast ? step.L + ((ladder.Text.L - step.L) * HighContrastTextPull) : step.L;
			return Solve(new MokaOklch(l, chroma * step.Chroma, hue), grounds, target, lighter: dark);
		}

		internal MokaSrgb Line(Step step, MokaSrgb[] grounds, double target)
		{
			double push = highContrast ? HighContrastLinePush : 0;
			double l = Math.Clamp(step.L + (dark ? push : -push), 0, 1);
			return Solve(new MokaOklch(l, chroma * step.Chroma, hue), grounds, target, lighter: dark);
		}
	}
}
