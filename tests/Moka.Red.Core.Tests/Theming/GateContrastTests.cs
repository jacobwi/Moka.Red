using System.Globalization;
using Moka.Red.Core.Theming;

namespace Moka.Red.Core.Tests.Theming;

// Real contrast math, so a new token or accent cannot quietly fail WCAG AA. Written apart from the library's own
// luminance code.
public class GateContrastTests
{
	private const double Text = 4.5;
	private const double Marks = 3;

	[Theory]
	[InlineData("GateLight")]
	[InlineData("GraphiteLight")]
	[InlineData("NearBlackLight")]
	[InlineData("GateLight high contrast")]
	[InlineData("GraphiteLight high contrast")]
	[InlineData("NearBlackLight high contrast")]
	public void LightText_ReadsOnEveryLightSurface(string name)
	{
		MokaPalette palette = Palette(name);

		foreach (string surface in new[] { palette.Surface, palette.SurfaceVariant, palette.Background, palette.Surface2 })
		{
			foreach (string text in new[]
			         {
				         palette.OnSurface, palette.OnSurface2, palette.OnSurfaceVariant, palette.OnSurfaceTertiary
			         })
			{
				AtLeast(Text, $"{name} text", text, surface);
			}

			AtLeast(Marks, $"{name} icon", palette.Icon, surface);
		}
	}

	[Theory]
	[InlineData("Gate")]
	[InlineData("Graphite")]
	[InlineData("NearBlack")]
	[InlineData("GateLight")]
	[InlineData("GraphiteLight")]
	[InlineData("NearBlackLight")]
	[InlineData("Gate high contrast")]
	[InlineData("Graphite high contrast")]
	[InlineData("NearBlack high contrast")]
	[InlineData("GateLight high contrast")]
	[InlineData("GraphiteLight high contrast")]
	[InlineData("NearBlackLight high contrast")]
	public void MainText_ReadsOnCardsPanelsAndThePage(string name)
	{
		MokaPalette palette = Palette(name);

		foreach (string surface in new[] { palette.Surface, palette.SurfaceVariant, palette.Background })
		{
			AtLeast(Text, $"{name} text", palette.OnSurface, surface);
			AtLeast(Text, $"{name} text 2", palette.OnSurface2, surface);
			AtLeast(Text, $"{name} muted", palette.OnSurfaceVariant, surface);
		}
	}

	[Theory]
	[InlineData("GateLight")]
	[InlineData("GraphiteLight")]
	[InlineData("NearBlackLight")]
	[InlineData("GateLight high contrast")]
	[InlineData("GraphiteLight high contrast")]
	[InlineData("NearBlackLight high contrast")]
	public void LightMeanings_ReadAsTextAndOnTheirOwnTints(string name)
	{
		MokaPalette palette = Palette(name);

		foreach (Meaning meaning in Meanings(palette))
		{
			foreach (string surface in new[] { palette.Surface, palette.SurfaceVariant, palette.Background })
			{
				AtLeast(Text, $"{name} {meaning.Name} as text", meaning.Strong, surface);
			}

			AtLeast(Text, $"{name} {meaning.Name} on its tint", meaning.Strong, meaning.Bg);
			AtLeast(Text, $"{name} {meaning.Name} ink", meaning.Ink, meaning.Bg);
			AtLeast(Text, $"{name} {meaning.Name} text", meaning.Text, meaning.Bg);
			AtLeast(Text, $"{name} white on {meaning.Name}", "#FFFFFF", meaning.Strong);
		}

		AtLeast(Text, $"{name} soft error on its tint", palette.ErrorSoft, palette.ErrorBg);
		AtLeast(Text, $"{name} soft error on a card", palette.ErrorSoft, palette.Surface);
	}

	[Fact]
	public void DarkMeanings_ReadOnTheirOwnTints()
	{
		foreach (Meaning meaning in Meanings(MokaPalette.Gate))
		{
			AtLeast(Text, $"{meaning.Name} ink", meaning.Ink, meaning.Bg);
			AtLeast(Text, $"{meaning.Name} text", meaning.Text, meaning.Bg);
			AtLeast(Marks, $"{meaning.Name} on a card", meaning.Strong, MokaPalette.Gate.Surface);
			AtLeast(Text, $"dark text on {meaning.Name}", MokaPalette.Gate.OnPrimary, meaning.Strong);
		}
	}

	[Fact]
	public void TheDefaultAccent_Reads()
	{
		MokaPalette dark = MokaPalette.Gate;
		AtLeast(Text, "dark accent on cards", dark.Primary, dark.Surface);
		AtLeast(Text, "dark accent on the page", dark.Primary, dark.Background);
		AtLeast(Text, "text on a dark button", dark.OnPrimary, dark.Primary);
		AtLeast(7, "dark links on cards", dark.PrimaryLight, dark.Surface);

		MokaPalette light = MokaPalette.GateLight;
		AtLeast(Text, "light accent on the page", light.Primary, light.Background);
		AtLeast(Text, "light accent on panels", light.Primary, light.SurfaceVariant);
		AtLeast(Text, "text on a light button", light.OnPrimary, light.Primary);
		AtLeast(Text, "light links on cards", light.PrimaryLight, light.Surface);
		AtLeast(Text, "light links on panels", light.PrimaryLight, light.SurfaceVariant);
	}

	// Every preset, every hue at full strength and muted down to grey, and colors people bring: a design
	// system's red, a near-black, a near-white, a pastel.
	[Fact]
	public void EveryAccent_ReadsInBothSchemes()
	{
		string darkCard = MokaPalette.Gate.Surface, darkPage = MokaPalette.Gate.Background;
		string darkText = MokaPalette.Gate.OnPrimary;
		string lightCard = MokaPalette.GateLight.Surface, lightPage = MokaPalette.GateLight.Background;
		string lightPanel = MokaPalette.GateLight.SurfaceVariant, lightText = MokaPalette.GateLight.OnPrimary;

		double[] saturations = [1, 0.6, 0.3, 0];
		string[] colors = ["#EF5350", "#0B0B0B", "#FDFDFD", "#FFD1DC", "#FFFF00", "#00FF00", "#3DD68C", "#808080"];

		List<(string Name, MokaAccent Accent)> accents =
		[
			.. MokaAccent.Presets.Select(accent => (accent.Name, accent)),
			.. saturations.SelectMany(saturation => Enumerable.Range(0, 360)
				.Select(hue => ($"Hue {hue} at {saturation}", MokaAccent.FromHue(hue, saturation)))),
			.. colors.Select(color => (color, MokaAccent.FromColor(color)))
		];
		Assert.Equal(MokaAccent.Presets.Count + (4 * 360) + 8, accents.Count);

		foreach ((string name, MokaAccent accent) in accents)
		{
			MokaAccentShades dark = accent.Dark;
			AtLeast(Text, $"{name}, dark accent on cards", dark.Color, darkCard);
			AtLeast(Text, $"{name}, dark accent on the page", dark.Color, darkPage);
			AtLeast(7, $"{name}, dark links on cards", dark.Soft, darkCard);
			AtLeast(Text, $"{name}, text on a dark button", darkText, dark.Color);
			AtLeast(Text, $"{name}, dark selected text on its tint", dark.Soft, dark.Background);
			Assert.True(Luminance(dark.Background) < Luminance("#2A3346") && Luminance(dark.Background) > Luminance(darkPage),
				$"{name}: the dark tint {dark.Background} is not a dark surface.");

			MokaAccentShades light = accent.Light;
			AtLeast(Text, $"{name}, light accent on the page", light.Color, lightPage);
			AtLeast(Text, $"{name}, light accent on panels", light.Color, lightPanel);
			AtLeast(Text, $"{name}, text on a light button", lightText, light.Color);
			AtLeast(Text, $"{name}, light links on cards", light.Soft, lightCard);
			AtLeast(Text, $"{name}, light links on panels", light.Soft, lightPanel);
			AtLeast(Text, $"{name}, light selected text on its tint", light.Soft, light.Background);
			Assert.True(Luminance(light.Background) > Luminance(lightPanel) - 0.06 && Luminance(light.Background) < Luminance(lightCard),
				$"{name}: the light tint {light.Background} is not a pale surface.");
		}
	}

	[Fact]
	public void HighContrast_IsStrongerThanNormal()
	{
		foreach ((MokaPalette normal, MokaPalette high) in new[]
		         {
			         (MokaPalette.Gate, MokaTheme.Gate.WithHighContrast().Palette),
			         (MokaPalette.GateLight, MokaTheme.GateLight.WithHighContrast().Palette),
			         (MokaPalette.Graphite, MokaTheme.Red.WithHighContrast().Palette),
			         (MokaPalette.GraphiteLight, MokaTheme.RedLight.WithHighContrast().Palette)
		         })
		{
			Assert.True(Contrast(high.OnSurface, high.Surface) > Contrast(normal.OnSurface, normal.Surface));
			Assert.True(Contrast(high.OnSurfaceVariant, high.Surface) > Contrast(normal.OnSurfaceVariant, normal.Surface));
			Assert.True(Contrast(high.Outline, high.Surface) > Contrast(normal.Outline, normal.Surface));
		}
	}

	private static MokaPalette Palette(string name) => name switch
	{
		"Gate" => MokaPalette.Gate,
		"GateLight" => MokaPalette.GateLight,
		"Gate high contrast" => MokaTheme.Gate.WithHighContrast().Palette,
		"GateLight high contrast" => MokaTheme.GateLight.WithHighContrast().Palette,
		"Graphite" => MokaPalette.Graphite,
		"GraphiteLight" => MokaPalette.GraphiteLight,
		"Graphite high contrast" => MokaTheme.Red.WithHighContrast().Palette,
		"GraphiteLight high contrast" => MokaTheme.RedLight.WithHighContrast().Palette,
		"NearBlack" => MokaPalette.NearBlack,
		"NearBlackLight" => MokaPalette.NearBlackLight,
		"NearBlack high contrast" => MokaTheme.Matrix.WithHighContrast().Palette,
		"NearBlackLight high contrast" => MokaTheme.MatrixLight.WithHighContrast().Palette,
		_ => throw new ArgumentOutOfRangeException(nameof(name), name, null)
	};

	private static IEnumerable<Meaning> Meanings(MokaPalette palette) =>
	[
		new("success", palette.Success, palette.SuccessBg, palette.SuccessInk, palette.SuccessText),
		new("warning", palette.Warning, palette.WarningBg, palette.WarningInk, palette.WarningText),
		new("error", palette.Error, palette.ErrorBg, palette.ErrorInk, palette.ErrorText),
		new("info", palette.Info, palette.InfoBg, palette.InfoInk, palette.InfoText)
	];

	private static void AtLeast(double target, string what, string a, string b)
	{
		double contrast = Contrast(a, b);
		Assert.True(contrast >= target, $"{what}: {a} on {b} is {contrast:0.00}, needs {target}.");
	}

	private static double Contrast(string a, string b)
	{
		(double la, double lb) = (Luminance(a), Luminance(b));
		return (Math.Max(la, lb) + 0.05) / (Math.Min(la, lb) + 0.05);
	}

	private static double Luminance(string hex)
	{
		Assert.Matches("^#[0-9A-Fa-f]{6}$", hex);

		double Channel(int i)
		{
			double c = int.Parse(hex.AsSpan(1 + (i * 2), 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture) / 255.0;
			return c <= 0.03928 ? c / 12.92 : Math.Pow((c + 0.055) / 1.055, 2.4);
		}

		return (0.2126 * Channel(0)) + (0.7152 * Channel(1)) + (0.0722 * Channel(2));
	}

	private readonly record struct Meaning(string Name, string Strong, string Bg, string Ink, string Text);
}
