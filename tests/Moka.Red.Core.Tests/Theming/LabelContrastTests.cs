using System.Globalization;
using System.Text.RegularExpressions;
using Moka.Red.Core.Enums;
using Moka.Red.Core.Colors;
using Moka.Red.Core.Theming;

namespace Moka.Red.Core.Tests.Theming;

// Every label color, in every tone, on every preset: ink reads 4.5:1 on its tint, and a dot shows 3:1 on the page. The
// tint is a color-mix over the theme's surface, so it is worked out here the way a browser mixes in srgb.
public sealed partial class LabelContrastTests
{
	private const double Text = 4.5;
	private const double Marks = 3;

	public static TheoryData<string, bool, MokaLabelTone> Themes()
	{
		TheoryData<string, bool, MokaLabelTone> data = [];
		foreach (string preset in new[] { "Red", "RedLight", "Gate", "GateLight", "Crisp", "CrispLight", "Matrix", "MatrixLight" })
		{
			foreach (MokaLabelTone tone in Enum.GetValues<MokaLabelTone>())
			{
				data.Add(preset, false, tone);
				data.Add(preset, true, tone);
			}
		}

		data.Add("Dark", false, MokaLabelTone.Vivid);
		data.Add("Light", false, MokaLabelTone.Vivid);
		return data;
	}

	[Theory]
	[MemberData(nameof(Themes))]
	public void InkReadsOnItsTint_AndOnThePlainSurface(string name, bool highContrast, MokaLabelTone tone)
	{
		MokaTheme theme = Theme(name, highContrast, tone);
		Dictionary<string, string> tokens = LabelColorTests.Tokens(theme.ToCssVariables());

		foreach (MokaLabelColor color in MokaLabelColors.All)
		{
			string key = MokaLabelColors.Key(color);
			string ink = tokens[$"--moka-label-{key}-ink"];
			string tint = Resolve(tokens[$"--moka-label-{key}-bg"], theme.Palette.Surface);

			AtLeast(Text, $"{name} {tone} {key} ink on its tint", ink, tint);
			AtLeast(Text, $"{name} {tone} {key} ink on a card", ink, theme.Palette.Surface);
		}
	}

	[Theory]
	[MemberData(nameof(Themes))]
	public void ADotShows_OnCardsPanelsAndThePage(string name, bool highContrast, MokaLabelTone tone)
	{
		MokaTheme theme = Theme(name, highContrast, tone);
		Dictionary<string, string> tokens = LabelColorTests.Tokens(theme.ToCssVariables());

		foreach (MokaLabelColor color in MokaLabelColors.All)
		{
			string key = MokaLabelColors.Key(color);
			string solid = tokens[$"--moka-label-{key}"];

			foreach (string ground in new[] { theme.Palette.Surface, theme.Palette.SurfaceVariant, theme.Palette.Background })
			{
				AtLeast(Marks, $"{name} {tone} {key} dot", solid, ground);
			}
		}
	}

	[Theory]
	[MemberData(nameof(Themes))]
	public void TheTint_IsSoft(string name, bool highContrast, MokaLabelTone tone)
	{
		MokaTheme theme = Theme(name, highContrast, tone);
		Dictionary<string, string> tokens = LabelColorTests.Tokens(theme.ToCssVariables());

		foreach (MokaLabelColor color in MokaLabelColors.All)
		{
			string key = MokaLabelColors.Key(color);
			double contrast = Contrast(Resolve(tokens[$"--moka-label-{key}-bg"], theme.Palette.Surface), theme.Palette.Surface);
			Assert.True(contrast < 1.8, $"{name} {tone} {key}: the tint stands out {contrast:0.00}:1 from the card, which is not soft.");
		}
	}

	private static MokaTheme Theme(string name, bool highContrast, MokaLabelTone tone)
	{
		MokaTheme theme = LabelColorTests.Preset(name);
		return (highContrast ? theme.WithHighContrast() : theme).WithLabelTone(tone);
	}

	private static string Resolve(string mix, string surface)
	{
		Match match = Mix().Match(mix);
		Assert.True(match.Success, $"{mix} is not a tint over the surface.");

		double weight = double.Parse(match.Groups["weight"].Value, CultureInfo.InvariantCulture) / 100;
		return MokaColorMix.InSrgb(Color(match.Groups["color"].Value), Color(surface), weight).ToHex(uppercase: true);
	}

	private static void AtLeast(double target, string what, string a, string b)
	{
		double contrast = Contrast(a, b);
		Assert.True(contrast >= target, $"{what}: {a} on {b} is {contrast:0.00}, needs {target}.");
	}

	private static double Contrast(string a, string b) => MokaContrast.Ratio(Color(a), Color(b));

	private static MokaSrgb Color(string hex)
	{
		Assert.Matches("^#[0-9A-Fa-f]{6}$", hex);
		return MokaCssColor.Parse(hex);
	}

	[GeneratedRegex(@"^color-mix\(in srgb, (?<color>#[0-9A-Fa-f]{6}) (?<weight>\d+)%, var\(--moka-color-surface\)\)$")]
	private static partial Regex Mix();
}
