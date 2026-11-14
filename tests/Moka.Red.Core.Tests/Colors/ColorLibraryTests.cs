using System.Globalization;
using Moka.Red.Core.Colors;

namespace Moka.Red.Core.Tests.Colors;

public class ColorLibraryTests
{
	[Theory]
	[InlineData("#f00", "#ff0000", 1)]
	[InlineData("#0f08", "#00ff00", 0x88 / 255.0)]
	[InlineData("#EF5350", "#ef5350", 1)]
	[InlineData("#ef535080", "#ef5350", 0x80 / 255.0)]
	[InlineData("rgb(239, 83, 80)", "#ef5350", 1)]
	[InlineData("rgba(239, 83, 80, 0.5)", "#ef5350", 0.5)]
	[InlineData("rgb(239 83 80 / 25%)", "#ef5350", 0.25)]
	[InlineData("RGB(100%, 0%, 50%)", "#ff0080", 1)]
	[InlineData("hsl(120 100% 50%)", "#00ff00", 1)]
	[InlineData("hsl(120deg, 100%, 25%)", "#008000", 1)]
	[InlineData("hsla(0.5turn 100% 50% / 0.2)", "#00ffff", 0.2)]
	[InlineData("oklch(62.8% 0.2577 29.23)", "#ff0000", 1)]
	[InlineData("oklch(1 0 0)", "#ffffff", 1)]
	[InlineData("oklab(0 0 0)", "#000000", 1)]
	[InlineData("rebeccapurple", "#663399", 1)]
	[InlineData("White", "#ffffff", 1)]
	[InlineData("transparent", "#000000", 0)]
	public void TryParse_ReadsEachForm(string text, string hex, double alpha)
	{
		Assert.True(MokaCssColor.TryParse(text, out MokaSrgb color));
		Assert.Equal(hex, color.ToHex());
		Assert.Equal(alpha, color.Alpha, 6);
	}

	[Theory]
	[InlineData(null)]
	[InlineData("")]
	[InlineData("#12")]
	[InlineData("#ggg")]
	[InlineData("rgb(1, 2)")]
	[InlineData("rgb(a, b, c)")]
	[InlineData("hsl(10%, 50%, 50%)")]
	[InlineData("lab(50 20 20)")]
	[InlineData("currentColor")]
	[InlineData("var(--moka-color-primary)")]
	[InlineData("notacolor")]
	public void TryParse_RefusesTheRest(string? text) => Assert.False(MokaCssColor.TryParse(text, out _));

	[Fact]
	public void TryParseHex_TakesOnlyHex()
	{
		Assert.True(MokaCssColor.TryParseHex("#abc", out _));
		Assert.False(MokaCssColor.TryParseHex("rgb(1, 2, 3)", out _));
		Assert.False(MokaCssColor.TryParseHex("abc", out _));
	}

	[Fact]
	public void Format_WritesEachForm()
	{
		MokaSrgb color = MokaCssColor.Parse("#ef5350");

		Assert.Equal("#EF5350", MokaCssColor.Format(color, MokaCssFormat.Hex));
		Assert.Equal("rgb(239 83 80)", MokaCssColor.Format(color, MokaCssFormat.Rgb));
		Assert.Equal("hsl(1 83% 63%)", MokaCssColor.Format(color, MokaCssFormat.Hsl));
		Assert.Equal("oklch(65.4% 0.193 25.1)", MokaCssColor.Format(color, MokaCssFormat.Oklch));
		Assert.Equal("rgba(239, 83, 80, 0.08)", MokaCssColor.Format(color with { Alpha = 0.08 }, MokaCssFormat.LegacyRgba));
		Assert.Equal("rgb(239 83 80 / 50%)", MokaCssColor.Format(color with { Alpha = 0.5 }, MokaCssFormat.Rgb));
		Assert.Equal("#EF535080", MokaCssColor.Format(color with { Alpha = 0.5 }, MokaCssFormat.Hex));
	}

	[Fact]
	public void Format_WritesInvariantNumbers_InAnyCulture()
	{
		CultureInfo culture = CultureInfo.CurrentCulture;
		CultureInfo.CurrentCulture = new CultureInfo("sv-SE");
		try
		{
			Assert.Equal("oklch(65.4% 0.193 25.1)", MokaCssColor.Format(MokaCssColor.Parse("#ef5350"), MokaCssFormat.Oklch));
			Assert.True(MokaCssColor.TryParse("rgba(1, 2, 3, 0.5)", out MokaSrgb color));
			Assert.Equal(0.5, color.Alpha);
		}
		finally
		{
			CultureInfo.CurrentCulture = culture;
		}
	}

	// Björn Ottosson's reference values for sRGB red.
	[Fact]
	public void Oklch_OfRed_IsTheReferenceValue()
	{
		MokaOklch red = MokaSrgb.FromBytes(255, 0, 0).ToOklch();

		Assert.Equal(0.627955, red.L, 5);
		Assert.Equal(0.257683, red.C, 5);
		Assert.Equal(29.2339, red.H, 3);
	}

	[Theory]
	[InlineData("#ef5350")]
	[InlineData("#42a5f5")]
	[InlineData("#00e676")]
	[InlineData("#7e57c2")]
	[InlineData("#8d8d96")]
	public void EveryColor_RoundTripsThroughLinearOklabOklchHslAndHsv(string hex)
	{
		MokaSrgb color = MokaCssColor.Parse(hex);

		Assert.Equal(hex, color.ToLinear().ToSrgb().ToHex());
		Assert.Equal(hex, color.ToOklab().ToSrgb().ToHex());
		Assert.Equal(hex, color.ToOklch().ToSrgb().ToHex());
		Assert.Equal(hex, color.ToHsl().ToSrgb().ToHex());
		Assert.Equal(hex, color.ToHsv().ToSrgb().ToHex());
	}

	[Fact]
	public void ToSrgbInGamut_KeepsLightnessAndHue_AndLandsInsideSrgb()
	{
		var vivid = new MokaOklch(0.7, 0.4, 150);
		Assert.False(vivid.ToSrgb().IsInGamut);

		MokaSrgb mapped = vivid.ToSrgbInGamut();
		MokaOklch result = mapped.ToOklch();

		Assert.True(mapped.IsInGamut);
		Assert.Equal(0.7, result.L, 1);
		Assert.InRange(result.H, 140, 160);
	}

	[Fact]
	public void ReduceChromaToFit_KeepsTheHueExactly()
	{
		MokaOklch fitted = new MokaOklch(0.6, 0.35, 260).ReduceChromaToFit();

		Assert.True(fitted.ToOklab().ToLinear().IsInGamut);
		Assert.Equal(260, fitted.H);
		Assert.True(fitted.C < 0.35);
	}

	[Fact]
	public void InSrgb_MatchesColorMix()
	{
		MokaSrgb red = MokaCssColor.Parse("red");

		Assert.Equal("#800080", MokaColorMix.InSrgb(red, MokaCssColor.Parse("blue"), 0.5).ToHex());

		MokaSrgb faded = MokaColorMix.InSrgb(red, MokaSrgb.Transparent, 0.5);
		Assert.Equal("#ff0000", faded.ToHex());
		Assert.Equal(0.5, faded.Alpha, 10);
	}

	// color-mix(in oklab, white 50%, black) is the grey at OKLab lightness 0.5.
	[Fact]
	public void InOklab_MatchesColorMix() =>
		Assert.Equal("#636363", MokaColorMix.InOklab(MokaSrgb.White, MokaSrgb.Black, 0.5).ToHex());

	[Theory]
	[InlineData("#000000", "#ffffff", 21)]
	[InlineData("#767676", "#ffffff", 4.54)]
	[InlineData("#ef5350", "#ef5350", 1)]
	public void Ratio_KnownPairs(string foreground, string background, double ratio) =>
		Assert.Equal(ratio, MokaContrast.Ratio(MokaCssColor.Parse(foreground), MokaCssColor.Parse(background)), 2);

	[Fact]
	public void Ratio_DrawsATranslucentForegroundOverTheBackground()
	{
		MokaSrgb halfBlack = MokaSrgb.Black with { Alpha = 0.5 };

		Assert.Equal(MokaContrast.Ratio(new MokaSrgb(0.5, 0.5, 0.5), MokaSrgb.White), MokaContrast.Ratio(halfBlack, MokaSrgb.White), 10);
	}

	// The APCA-W3 reference results: SAPC's published examples.
	[Theory]
	[InlineData("#000000", "#ffffff", 106.04)]
	[InlineData("#ffffff", "#000000", -107.88)]
	[InlineData("#888888", "#ffffff", 63.06)]
	[InlineData("#ffffff", "#888888", -68.54)]
	[InlineData("#ffffff", "#ffffff", 0)]
	public void Apca_KnownPairs(string text, string background, double lc) =>
		Assert.Equal(lc, MokaContrast.Apca(MokaCssColor.Parse(text), MokaCssColor.Parse(background)), 1);

	[Fact]
	public void Over_CompositesAsTheEyeSeesIt()
	{
		MokaSrgb result = (MokaSrgb.White with { Alpha = 0.5 }).Over(MokaSrgb.Black);

		Assert.Equal("#808080", result.ToHex());
		Assert.Equal(1, result.Alpha);
	}

	[Fact]
	public void ToBytes_RoundsHalfAwayFromZero_UnlessToldOtherwise()
	{
		var half = new MokaSrgb(76.5 / 255, 0, 0);

		Assert.Equal(77, half.ToBytes().R);
		Assert.Equal(76, half.ToBytes(MidpointRounding.ToEven).R);
	}
}
