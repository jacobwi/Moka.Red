using System.Globalization;

namespace Moka.Red.Core.Theming;

// One theme while its tokens are written: the parts several tokens share, such as the shadow set and the main
// button's colors, are worked out once here.
internal sealed class MokaTokenContext
{
	internal MokaTokenContext(MokaTheme theme)
	{
		Theme = theme;
		Gate = theme.Look == MokaLook.Gate;
		Shadows = MokaShadowSet.For(theme, Gate);
		(MainPaddingTop, MainPaddingBottom) = PaddingEnds(theme.Spacing.MainPadding);
		Labels = MokaLabelPalette.Values(Gate ? theme.LabelTone : MokaLabelTone.Vivid, theme.IsDark);
		Chart = MokaChartPaletteValues.Values(theme);
	}

	internal MokaTheme Theme { get; }

	internal bool Gate { get; }

	internal MokaPalette Palette => Theme.Palette;

	internal MokaTypography Typography => Theme.Typography;

	internal MokaSpacing Spacing => Theme.Spacing;

	internal bool Dark => Theme.IsDark;

	internal MokaShadowSet Shadows { get; }

	internal string MainPaddingTop { get; }

	internal string MainPaddingBottom { get; }

	internal IReadOnlyList<string> Labels { get; }

	internal IReadOnlyList<string> Chart { get; }

	internal static string Scale(string value, double factor)
	{
		if (factor == 1.0 || string.IsNullOrEmpty(value))
		{
			return value;
		}

		int unitStart = 0;
		while (unitStart < value.Length && (char.IsAsciiDigit(value[unitStart]) || value[unitStart] == '.'))
		{
			unitStart++;
		}

		if (unitStart == 0 ||
		    !double.TryParse(value[..unitStart], NumberStyles.Float, CultureInfo.InvariantCulture, out double size))
		{
			return value;
		}

		return string.Create(CultureInfo.InvariantCulture, $"{size * factor:0.####}{value[unitStart..]}");
	}

	// The top and bottom of a padding shorthand, by CSS's own rule: one value is every side, and from three values on
	// the third is the bottom. Split at the top level, so a var() or calc() stays one value.
	private static (string Top, string Bottom) PaddingEnds(string shorthand)
	{
		List<string> values = MokaToken.TopLevelParts(shorthand);
		return values.Count switch
		{
			0 => ("0", "0"),
			1 or 2 => (values[0], values[0]),
			_ => (values[0], values[2])
		};
	}
}

// The shadow tokens of one theme, which depend on each other's choices and so are made together.
internal sealed record MokaShadowSet(
	string Card,
	string Raised,
	string Floating,
	string Bar,
	string Popup,
	string PopupLarge,
	string Modal,
	string Subtle)
{
	private const string LightCard = "0 1px 3px rgba(0, 0, 0, 0.08), 0 1px 2px rgba(0, 0, 0, 0.06)";
	private const string LightRaised = "0 4px 6px rgba(0, 0, 0, 0.07), 0 2px 4px rgba(0, 0, 0, 0.06)";
	private const string LightFloating = "0 10px 15px rgba(0, 0, 0, 0.07), 0 4px 6px rgba(0, 0, 0, 0.05)";
	private const string LightBar = "0 20px 25px rgba(0, 0, 0, 0.08), 0 8px 10px rgba(0, 0, 0, 0.04)";
	private const string LightPopup = "0 4px 12px rgba(0, 0, 0, 0.15)";
	private const string LightPopupLarge = "0 4px 16px rgba(0, 0, 0, 0.12)";
	private const string LightModal = "0 8px 32px rgba(0, 0, 0, 0.2)";
	private const string LightSubtle = "0 1px 4px rgba(0, 0, 0, 0.1)";

	private const string GateDarkPopup = "0 18px 40px rgba(0, 0, 0, 0.45)";
	private const string GateDarkBar = "0 18px 50px rgba(0, 0, 0, 0.55)";
	private const string GateDarkModal = "0 3px 6px 2px rgba(0, 0, 0, 0.3)";

	private const string GateLightCard = "0 1px 2px rgba(22, 30, 48, 0.05)";
	private const string GateLightRaised = "0 2px 6px rgba(22, 30, 48, 0.08)";
	private const string GateLightPopup = "0 16px 36px rgba(22, 30, 48, 0.14), 0 2px 6px rgba(22, 30, 48, 0.06)";
	private const string GateLightBar = "0 14px 40px rgba(22, 30, 48, 0.16), 0 2px 6px rgba(22, 30, 48, 0.06)";
	private const string GateLightModal = "0 8px 28px rgba(22, 30, 48, 0.18)";

	// The solid current place's glow held inside the row: a light rim on the accent, in both schemes.
	internal const string InsetPlace = "inset 0 0 12px rgba(255, 255, 255, 0.14)";

	private static readonly MokaShadowSet MatrixLight =
		new(LightCard, LightRaised, LightFloating, LightBar, LightPopup, LightPopupLarge, LightModal, LightSubtle);

	// Dark Matrix shadows are accent-tinted glow rings, re-tinted from the primary color when it is hex so WithAccent
	// moves them too, otherwise from the original red. The Gate look has real drop shadows, and on dark only for what
	// floats.
	internal static MokaShadowSet For(MokaTheme theme, bool gate)
	{
		if (gate)
		{
			bool dark = theme.IsDark;
			var drop = new MokaShadowSet(
				dark ? "none" : GateLightCard,
				dark ? "none" : GateLightRaised,
				dark ? GateDarkPopup : GateLightPopup,
				dark ? GateDarkBar : GateLightBar,
				dark ? GateDarkPopup : GateLightPopup,
				dark ? GateDarkBar : GateLightBar,
				dark ? GateDarkModal : GateLightModal,
				dark ? "none" : GateLightCard);
			return theme.Elevation == MokaElevation.Shadow ? drop : drop.WithFloating(theme.Elevation, dark);
		}

		if (!theme.IsDark)
		{
			return MatrixLight;
		}

		string ring = MokaColorMath.TryParseHex(theme.Palette.Primary, out _, out _, out _) ? theme.Palette.Primary : MokaPalette.Dark.Primary;
		string Glow(double alpha) => MokaColorMath.Rgba(ring, alpha);
		return new MokaShadowSet(
			$"0 0 0 1px {Glow(0.06)}",
			$"0 0 0 1px {Glow(0.12)}",
			$"0 0 0 1px {Glow(0.12)}, 0 0 12px {Glow(0.08)}",
			$"0 0 0 1px {Glow(0.20)}, 0 0 24px {Glow(0.12)}",
			$"0 0 0 1px {Glow(0.12)}, 0 0 16px {Glow(0.08)}",
			$"0 0 0 1px {Glow(0.16)}, 0 0 24px {Glow(0.12)}",
			$"0 0 0 1px {Glow(0.20)}, 0 0 32px {Glow(0.15)}",
			$"0 0 0 1px {Glow(0.06)}");
	}

	// A ring and a faint glow in the accent, as the original design draws on dark, or nothing past the panel's own
	// border. Light takes the fainter glow, since the stronger one reads as a stain on white.
	private MokaShadowSet WithFloating(MokaElevation elevation, bool dark)
	{
		if (elevation == MokaElevation.Line)
		{
			return this with { Floating = "none", Bar = "none", Popup = "none", PopupLarge = "none", Modal = "none" };
		}

		string glow = dark ? "var(--moka-color-primary-glow-md)" : "var(--moka-color-primary-glow)";
		string popup = $"0 0 0 1px var(--moka-color-primary-ring), 0 0 24px {glow}";
		string large = $"0 0 0 1px var(--moka-color-primary-ring), 0 0 32px {glow}";
		return this with { Floating = popup, Bar = large, Popup = popup, PopupLarge = large, Modal = large };
	}
}
