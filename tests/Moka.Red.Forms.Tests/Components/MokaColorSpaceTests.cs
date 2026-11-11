using Moka.Red.Core.Theming;
using Moka.Red.Forms.ColorPicker;
using Moka.Red.Forms.Common;

namespace Moka.Red.Forms.Tests.Components;

// The color math behind the color inputs' new looks: reading colors, HSV, HSL and OKLCH, WCAG contrast, and the
// formats the picker writes.
public class MokaColorSpaceTests
{
	public static TheoryData<string, int, int, int, double> Readable => new()
	{
		{ "#ef5350", 239, 83, 80, 1 },
		{ "#EF5350", 239, 83, 80, 1 },
		{ "#abc", 170, 187, 204, 1 },
		{ "#abcd", 170, 187, 204, 221 / 255.0 },
		{ "#ef535080", 239, 83, 80, 128 / 255.0 },
		{ "rgb(239, 83, 80)", 239, 83, 80, 1 },
		{ "rgb(239 83 80 / 50%)", 239, 83, 80, 0.5 },
		{ "rgba(239, 83, 80, 0.25)", 239, 83, 80, 0.25 },
		{ "  rgb(100% 0% 0%)  ", 255, 0, 0, 1 }
	};

	[Theory]
	[MemberData(nameof(Readable))]
	public void TryParse_ReadsHexAndRgb(string text, int r, int g, int b, double a)
	{
		Assert.True(MokaColorSpace.TryParse(text, out MokaRgba color));
		Assert.Equal((r, g, b), (color.R, color.G, color.B));
		Assert.Equal(a, color.A, 6);
	}

	[Theory]
	[InlineData(null)]
	[InlineData("")]
	[InlineData("red")]
	[InlineData("#12")]
	[InlineData("#ggg")]
	[InlineData("#12345")]
	[InlineData("rgb(1, 2)")]
	[InlineData("rgb(a, b, c)")]
	[InlineData("hsl(0 100% 50%)")]
	[InlineData("var(--moka-color-primary)")]
	public void TryParse_RefusesTheRest(string? text) => Assert.False(MokaColorSpace.TryParse(text, out _));

	[Fact]
	public void ToHex_WritesLowercase_AndTheAlphaOnlyWhenAskedAndBelowOne()
	{
		Assert.Equal("#ef5350", MokaColorSpace.ToHex(new MokaRgba(239, 83, 80)));
		Assert.Equal("#ef5350", MokaColorSpace.ToHex(new MokaRgba(239, 83, 80), withAlpha: true));
		Assert.Equal("#ef5350", MokaColorSpace.ToHex(new MokaRgba(239, 83, 80, 0.72)));
		Assert.Equal("#ef5350b8", MokaColorSpace.ToHex(new MokaRgba(239, 83, 80, 0.72), withAlpha: true));
	}

	[Fact]
	public void Hsv_KnownColors()
	{
		Assert.Equal(new MokaRgba(255, 0, 0), MokaColorSpace.FromHsv(0, 1, 1));
		Assert.Equal(new MokaRgba(0, 255, 0), MokaColorSpace.FromHsv(120, 1, 1));
		Assert.Equal(new MokaRgba(0, 0, 255), MokaColorSpace.FromHsv(240, 1, 1));
		Assert.Equal(new MokaRgba(255, 0, 0), MokaColorSpace.FromHsv(360, 1, 1));
		Assert.Equal(new MokaRgba(128, 128, 128), MokaColorSpace.FromHsv(200, 0, 0.5));

		(double h, double s, double v) = MokaColorSpace.ToHsv(new MokaRgba(126, 87, 194));
		Assert.Equal(262, Math.Round(h));
		Assert.Equal(55, Math.Round(s * 100));
		Assert.Equal(76, Math.Round(v * 100));
	}

	[Fact]
	public void Hsl_KnownColors()
	{
		(double h, double s, double l) = MokaColorSpace.ToHsl(new MokaRgba(38, 166, 154));
		Assert.Equal(174, Math.Round(h));
		Assert.Equal(63, Math.Round(s * 100));
		Assert.Equal(40, Math.Round(l * 100));
		Assert.Equal(new MokaRgba(255, 255, 255), MokaColorSpace.FromHsl(10, 0.5, 1));
		Assert.Equal(new MokaRgba(0, 0, 0), MokaColorSpace.FromHsl(10, 0.5, 0));
	}

	// Every 8-bit color comes back as itself through HSV, through HSL, and through OKLCH.
	[Fact]
	public void EveryColor_RoundTripsThroughHsvHslAndOklch()
	{
		for (int r = 0; r < 256; r += 15)
		{
			for (int g = 0; g < 256; g += 15)
			{
				for (int b = 0; b < 256; b += 15)
				{
					MokaRgba color = new(r, g, b);

					(double h, double s, double v) = MokaColorSpace.ToHsv(color);
					Assert.Equal(color, MokaColorSpace.FromHsv(h, s, v));

					(double hh, double ss, double ll) = MokaColorSpace.ToHsl(color);
					Assert.Equal(color, MokaColorSpace.FromHsl(hh, ss, ll));

					(double lightness, double chroma, double hue) = MokaColorSpace.ToOklch(color);
					Assert.Equal(color, MokaColorSpace.FromOklch(lightness, chroma, hue));
				}
			}
		}
	}

	[Fact]
	public void Oklch_KnownValues()
	{
		(double l, double c, double h) = MokaColorSpace.ToOklch(new MokaRgba(255, 0, 0));
		Assert.Equal(0.628, l, 3);
		Assert.Equal(0.258, c, 3);
		Assert.Equal(29.23, h, 1);

		(double white, double whiteChroma, double whiteHue) = MokaColorSpace.ToOklch(new MokaRgba(255, 255, 255));
		Assert.Equal(1, white, 3);
		Assert.True(whiteChroma < 0.0005);
		Assert.Equal(0, whiteHue);

		Assert.Equal(0, MokaColorSpace.ToOklch(new MokaRgba(0, 0, 0)).Lightness, 6);
	}

	// What the format switch writes comes back as the same color within a step of rounding.
	[Theory]
	[InlineData("#ef5350")]
	[InlineData("#42a5f5")]
	[InlineData("#26a69a")]
	[InlineData("#ffb547")]
	[InlineData("#7e57c2")]
	[InlineData("#8d8d96")]
	public void TheOklchFormat_RoundTripsWithinRounding(string hex)
	{
		Assert.True(MokaColorSpace.TryParse(hex, out MokaRgba color));
		string text = MokaColorSpace.Format(color, MokaColorFormat.Oklch);

		string[] parts = text["oklch(".Length..^1].Split(' ');
		double l = double.Parse(parts[0].TrimEnd('%'), System.Globalization.CultureInfo.InvariantCulture) / 100;
		double c = double.Parse(parts[1], System.Globalization.CultureInfo.InvariantCulture);
		double h = double.Parse(parts[2], System.Globalization.CultureInfo.InvariantCulture);
		MokaRgba back = MokaColorSpace.FromOklch(l, c, h);

		Assert.InRange(Math.Abs(back.R - color.R), 0, 2);
		Assert.InRange(Math.Abs(back.G - color.G), 0, 2);
		Assert.InRange(Math.Abs(back.B - color.B), 0, 2);
	}

	[Fact]
	public void Format_WritesEachFormat()
	{
		MokaRgba color = new(239, 83, 80);

		Assert.Equal("#EF5350", MokaColorSpace.Format(color, MokaColorFormat.Hex));
		Assert.Equal("rgb(239 83 80)", MokaColorSpace.Format(color, MokaColorFormat.Rgb));
		Assert.Equal("hsl(1 83% 63%)", MokaColorSpace.Format(color, MokaColorFormat.Hsl));
		Assert.Matches(@"^oklch\(\d+\.\d% 0\.\d{3} \d+\.\d\)$", MokaColorSpace.Format(color, MokaColorFormat.Oklch));
	}

	[Fact]
	public void Format_AddsTheAlpha_BelowFullOpacity()
	{
		MokaRgba color = new(239, 83, 80, 0.72);

		Assert.Equal("#EF5350B8", MokaColorSpace.Format(color, MokaColorFormat.Hex));
		Assert.Equal("rgb(239 83 80 / 72%)", MokaColorSpace.Format(color, MokaColorFormat.Rgb));
		Assert.EndsWith(" / 72%)", MokaColorSpace.Format(color, MokaColorFormat.Hsl), StringComparison.Ordinal);
		Assert.EndsWith(" / 72%)", MokaColorSpace.Format(color, MokaColorFormat.Oklch), StringComparison.Ordinal);
	}

	[Fact]
	public void Format_WritesInvariantNumbers_InAnyCulture()
	{
		System.Globalization.CultureInfo culture = System.Globalization.CultureInfo.CurrentCulture;
		try
		{
			System.Globalization.CultureInfo.CurrentCulture = new System.Globalization.CultureInfo("sv-SE");

			Assert.DoesNotContain(",", MokaColorSpace.Format(new MokaRgba(239, 83, 80), MokaColorFormat.Oklch), StringComparison.Ordinal);
			Assert.Equal("4.47:1", MokaColorSpace.RatioText(4.4789));
		}
		finally
		{
			System.Globalization.CultureInfo.CurrentCulture = culture;
		}
	}

	[Fact]
	public void ContrastRatio_KnownPairs()
	{
		MokaRgba black = new(0, 0, 0);
		MokaRgba white = new(255, 255, 255);

		Assert.Equal(21, MokaColorSpace.ContrastRatio(black, white), 6);
		Assert.Equal(21, MokaColorSpace.ContrastRatio(white, black), 6);
		Assert.Equal(1, MokaColorSpace.ContrastRatio(white, white), 6);

		// #767676 is the lightest grey that passes AA on white, #777777 the first that doesn't.
		Assert.Equal(4.54, MokaColorSpace.ContrastRatio(new MokaRgba(0x76, 0x76, 0x76), white), 2);
		Assert.True(MokaColorSpace.ContrastRatio(new MokaRgba(0x77, 0x77, 0x77), white) < 4.5);
	}

	[Theory]
	[InlineData(21, MokaContrastLevel.Aaa)]
	[InlineData(7, MokaContrastLevel.Aaa)]
	[InlineData(6.999, MokaContrastLevel.Aa)]
	[InlineData(4.5, MokaContrastLevel.Aa)]
	[InlineData(4.4999, MokaContrastLevel.AaLarge)]
	[InlineData(3, MokaContrastLevel.AaLarge)]
	[InlineData(2.999, MokaContrastLevel.Fails)]
	[InlineData(1, MokaContrastLevel.Fails)]
	public void LevelOf_ComparesTheRatioUnrounded(double ratio, MokaContrastLevel level) =>
		Assert.Equal(level, MokaColorSpace.LevelOf(ratio));

	[Fact]
	public void RatioText_CutsRatherThanRounds_SoAFailIsNeverShownAsAPass()
	{
		Assert.Equal("4.49:1", MokaColorSpace.RatioText(4.4999));
		Assert.Equal("21.00:1", MokaColorSpace.RatioText(21));
		Assert.Equal("1.00:1", MokaColorSpace.RatioText(1));
	}

	[Fact]
	public void Over_CompositesATranslucentColor()
	{
		MokaRgba half = new(255, 0, 0, 0.5);

		Assert.Equal(new MokaRgba(255, 128, 128), MokaColorSpace.Over(half, new MokaRgba(255, 255, 255)));
		Assert.Equal(new MokaRgba(128, 0, 0), MokaColorSpace.Over(half, new MokaRgba(0, 0, 0)));
		Assert.Equal(new MokaRgba(10, 20, 30), MokaColorSpace.Over(new MokaRgba(10, 20, 30), new MokaRgba(255, 255, 255)));
	}

	[Theory]
	[InlineData("var(--moka-color-primary)", true)]
	[InlineData("var(--moka-color-on-surface)", true)]
	[InlineData("var( --moka-color-primary )", true)]
	[InlineData("var(--moka-color-primary, red)", false)]
	[InlineData("var(--other)", false)]
	[InlineData("var(--moka-color-primary); color: red", false)]
	[InlineData("#ef5350", false)]
	public void IsThemeToken_TakesOnlyAMokaColorToken(string value, bool token) =>
		Assert.Equal(token, MokaColorValues.IsThemeToken(value));

	[Theory]
	[InlineData("#ef5350", true)]
	[InlineData("var(--moka-color-primary)", true)]
	[InlineData("red", false)]
	[InlineData("red; background-image: url(x)", false)]
	[InlineData(null, false)]
	public void IsShown_TakesHexAndThemeTokens(string? value, bool shown) =>
		Assert.Equal(shown, MokaColorValues.IsShown(value));

	[Fact]
	public void TryResolve_ReadsATokenFromTheTheme()
	{
		Assert.True(MokaColorValues.TryResolve("var(--moka-color-primary)", MokaTheme.Dark, out MokaRgba primary));
		Assert.True(MokaColorSpace.TryParse(MokaTheme.Dark.Palette.Primary, out MokaRgba expected));
		Assert.Equal(expected, primary);
	}

	// The original palettes write outline-strong as a reference to outline.
	[Fact]
	public void TryResolve_FollowsAReferenceToAnotherToken()
	{
		Assert.True(MokaColorValues.TryResolve("var(--moka-color-outline-strong)", MokaTheme.Light, out MokaRgba strong));
		Assert.True(MokaColorValues.TryResolve("var(--moka-color-outline)", MokaTheme.Light, out MokaRgba outline));
		Assert.Equal(outline, strong);
	}

	[Fact]
	public void TryResolve_FollowsLightAndDark()
	{
		Assert.True(MokaColorValues.TryResolve("var(--moka-color-background)", MokaTheme.Red, out MokaRgba dark));
		Assert.True(MokaColorValues.TryResolve("var(--moka-color-background)", MokaTheme.RedLight, out MokaRgba light));

		Assert.True(MokaColorSpace.Luminance(dark) < 0.1);
		Assert.True(MokaColorSpace.Luminance(light) > 0.8);
	}

	[Fact]
	public void TryResolve_RefusesAnythingElse()
	{
		Assert.False(MokaColorValues.TryResolve("red", MokaTheme.Dark, out _));
		Assert.False(MokaColorValues.TryResolve("var(--moka-color-nothing-here)", MokaTheme.Dark, out _));
		Assert.False(MokaColorValues.TryResolve(null, MokaTheme.Dark, out _));
	}

	[Fact]
	public void ThemeLabel_NamesAToken()
	{
		Assert.Equal("Primary", MokaColorValues.ThemeLabel("var(--moka-color-primary)"));
		Assert.Equal("On surface", MokaColorValues.ThemeLabel("var(--moka-color-on-surface)"));
		Assert.Null(MokaColorValues.ThemeLabel("#ef5350"));
	}
}
