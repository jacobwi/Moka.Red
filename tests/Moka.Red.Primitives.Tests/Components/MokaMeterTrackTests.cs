using System.Globalization;
using Moka.Red.Core.Theming;
using Moka.Red.Tests.Shared;

namespace Moka.Red.Primitives.Tests.Components;

// The track was surface-variant, which is also a MokaStat tile's and a panel's background, so a meter on one lost its
// track. The new design draws it in the color of things that are off.
public class MokaMeterTrackTests
{
	private const string Css = "MokaMeter.razor.css";

	public static TheoryData<string> NewDesignPresets =>
		["Red", "RedLight", "Gate", "GateLight", "Crisp", "CrispLight", "Matrix", "MatrixLight"];

	[Fact]
	public void Track_OnTheNewDesign_ReadsTheOffColor()
	{
		Assert.Equal("var(--moka-color-on-surface-quaternary)",
			ScopedCss.Declarations(Css, ".moka-gate .moka-meter__track")["background-color"]);
	}

	[Fact]
	public void Track_OnTheOriginalDesign_StaysAsItWas()
	{
		Assert.Equal("var(--moka-color-surface-variant)", ScopedCss.Declarations(Css, ".moka-meter__track")["background"]);
		Assert.Equal("var(--moka-color-surface-variant)",
			ScopedCss.Declarations(Css, ".moka-meter__track:has(.moka-meter__segments)")["background"]);
	}

	// A track this faint still reads as the empty part of a bar; surface-variant on a card was about 1.1 to 1.
	[Theory]
	[MemberData(nameof(NewDesignPresets))]
	public void Track_StaysVisible_OnACard_AndOnAPanelOrTile(string preset)
	{
		MokaPalette palette = Preset(preset).Palette;

		Assert.True(Contrast(palette.OnSurfaceQuaternary, palette.Surface) >= 1.5,
			$"{preset}: {palette.OnSurfaceQuaternary} on the card {palette.Surface}");
		Assert.True(Contrast(palette.OnSurfaceQuaternary, palette.SurfaceVariant) >= 1.5,
			$"{preset}: {palette.OnSurfaceQuaternary} on the panel {palette.SurfaceVariant}");
	}

	private static MokaTheme Preset(string name) => name switch
	{
		"Red" => MokaTheme.Red,
		"RedLight" => MokaTheme.RedLight,
		"Gate" => MokaTheme.Gate,
		"GateLight" => MokaTheme.GateLight,
		"Crisp" => MokaTheme.Crisp,
		"CrispLight" => MokaTheme.CrispLight,
		"Matrix" => MokaTheme.Matrix,
		"MatrixLight" => MokaTheme.MatrixLight,
		_ => throw new ArgumentOutOfRangeException(nameof(name), name, null)
	};

	// WCAG relative luminance, written here rather than borrowed from the library.
	private static double Contrast(string a, string b)
	{
		double la = Luminance(a), lb = Luminance(b);
		return (Math.Max(la, lb) + 0.05) / (Math.Min(la, lb) + 0.05);
	}

	private static double Luminance(string hex)
	{
		Assert.Matches("^#[0-9A-Fa-f]{6}$", hex);
		double r = Channel(hex[1..3]), g = Channel(hex[3..5]), b = Channel(hex[5..7]);
		return 0.2126 * r + 0.7152 * g + 0.0722 * b;
	}

	private static double Channel(string hex)
	{
		double v = int.Parse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture) / 255.0;
		return v <= 0.04045 ? v / 12.92 : Math.Pow((v + 0.055) / 1.055, 2.4);
	}
}
