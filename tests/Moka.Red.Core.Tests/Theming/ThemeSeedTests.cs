using Moka.Red.Core.Colors;
using Moka.Red.Core.Theming;

namespace Moka.Red.Core.Tests.Theming;

public class ThemeSeedTests
{
	private static readonly string?[] Neutrals = [null, "#111723", "#1C1917", "#14201A", "oklch(0.3 0.06 300)"];

	// Every hue, at a light, a middling and a dark accent, on grey, blue-grey, warm, green and violet neutrals, in both
	// schemes and both contrast levels, with and without high contrast: every pair the library requires passes.
	[Fact]
	public void EverySeed_PassesEveryRequiredPair()
	{
		List<string> failures = [];
		int themes = 0;
		foreach (int hue in Enumerable.Range(0, 12).Select(i => i * 30))
		{
			foreach ((double lightness, double chroma) in new[] { (0.88, 0.15), (0.62, 0.2), (0.35, 0.12) })
			{
				string accent = new MokaOklch(lightness, chroma, hue).ToSrgbInGamut().ToHex();
				foreach (string? neutral in Neutrals)
				{
					foreach (bool dark in new[] { true, false })
					{
						foreach (MokaSeedContrast contrast in Enum.GetValues<MokaSeedContrast>())
						{
							MokaTheme theme = MokaTheme.FromSeed(new MokaThemeSeed { Accent = accent, Neutral = neutral, Contrast = contrast }, dark);
							foreach (MokaTheme variant in new[] { theme, theme.WithHighContrast() })
							{
								themes++;
								MokaContrastReport report = variant.CheckContrast();
								Assert.Empty(report.Unresolved);
								failures.AddRange(report.Failures.Select(failure =>
									$"{accent} on {neutral ?? "grey"}, {(dark ? "dark" : "light")}, {contrast}, high contrast {variant.HighContrast}: " +
									$"{failure.Foreground} on {failure.Background} is {failure.Ratio:0.00}, needs {failure.RequiredRatio}"));
							}
						}
					}
				}
			}
		}

		Assert.Equal(12 * 3 * Neutrals.Length * 2 * 2 * 2, themes);
		Assert.True(failures.Count == 0, string.Join("\n", failures.Take(40)) + $"\n({failures.Count} failures)");
	}

	[Theory]
	[InlineData("#7C3AED")]
	[InlineData("#FFE600")]
	[InlineData("rebeccapurple")]
	[InlineData("oklch(0.7 0.15 150)")]
	public void TheAccent_IsThePrimaryInBothSchemes(string accent)
	{
		string hex = MokaCssColor.Parse(accent).ToHex(uppercase: true);

		Assert.Equal(hex, MokaTheme.FromSeed(new MokaThemeSeed { Accent = accent }, true).Palette.Primary);
		Assert.Equal(hex, MokaTheme.FromSeed(new MokaThemeSeed { Accent = accent }, false).Palette.Primary);
	}

	[Fact]
	public void ASeededTheme_IsTheNewDesign_AndKeepsItsSeed()
	{
		var seed = new MokaThemeSeed { Accent = "#7C3AED", Neutral = "#111723" };
		MokaTheme theme = MokaTheme.FromSeed(seed, dark: true);

		Assert.Equal(MokaLook.Gate, theme.Look);
		Assert.Same(seed, theme.Seed);
		Assert.True(theme.IsDark);
		Assert.Equal(MokaTypography.Gate, theme.Typography);
	}

	// The seed for Red's own accent and graphite neutral lands close to the Red preset; the strong border is the one
	// place it moves on purpose, since a seed holds control lines to 3:1.
	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public void RedsSeed_LandsCloseToRed(bool dark)
	{
		MokaTheme preset = dark ? MokaTheme.Red : MokaTheme.RedLight;
		MokaTheme seeded = MokaTheme.FromSeed(new MokaThemeSeed { Accent = preset.Palette.Primary, Neutral = "#141417" }, dark);

		List<double> distances = [];
		foreach (MokaThemeProperty<MokaPalette> property in MokaTokens.PaletteProperties)
		{
			if (preset.TryResolveColor(property.Token, out MokaSrgb a) && seeded.TryResolveColor(property.Token, out MokaSrgb b))
			{
				distances.Add(100 * a.Over(MokaSrgb.Black).ToOklab().DistanceTo(b.Over(MokaSrgb.Black).ToOklab()));
			}
		}

		double median = distances.Order().ElementAt(distances.Count / 2);
		Assert.True(median < 1, $"The median distance is {median:0.00}.");
		Assert.Equal(preset.Palette.Primary, seeded.Palette.Primary);
		Assert.Equal(preset.Palette.Success, seeded.Palette.Success);
	}

	[Fact]
	public void WithDark_DerivesTheOtherSchemeFromTheSeed_KeepingStyleChoicesAndPins()
	{
		MokaTheme theme = MokaTheme.FromSeed(new MokaThemeSeed { Accent = "#0EA5E9" }, dark: true)
			.WithCorners(MokaCorners.Tight)
			.WithToken("--moka-color-paper", "#FAFAF5");

		MokaTheme light = theme.WithDark(false);

		Assert.False(light.IsDark);
		Assert.Equal(MokaTheme.FromSeed(theme.Seed!, false).Palette, light.Palette);
		Assert.Equal(theme.Spacing, light.Spacing);
		Assert.Equal("#FAFAF5", light.ToTokenMap()["--moka-color-paper"]);
		Assert.Equal(theme.Palette, light.WithDark(true).Palette);
	}

	[Fact]
	public void WithHighContrast_DerivesStrongerTextFromTheSeed_AndBack()
	{
		MokaTheme theme = MokaTheme.FromSeed(new MokaThemeSeed { Accent = "#0EA5E9", Neutral = "#111723" }, dark: true);
		MokaTheme high = theme.WithHighContrast();

		Assert.True(Ratio(high, "--moka-color-on-surface-variant") > Ratio(theme, "--moka-color-on-surface-variant"));
		Assert.True(Ratio(high, "--moka-color-outline") > Ratio(theme, "--moka-color-outline"));
		Assert.Equal(theme.Palette, high.WithHighContrast(false).Palette);
	}

	[Fact]
	public void More_HoldsDerivedTextToSevenToOne()
	{
		MokaTheme theme = MokaTheme.FromSeed(new MokaThemeSeed { Accent = "#0EA5E9", Contrast = MokaSeedContrast.More }, dark: false);

		Assert.All(theme.CheckContrast().Checks.Where(check => check.Foreground == "--moka-color-on-surface-tertiary"),
			check => Assert.True(check.Ratio >= 7, $"{check.Background}: {check.Ratio:0.00}"));
	}

	[Fact]
	public void WithAccent_OnASeededTheme_TakesAnyCssColor()
	{
		MokaTheme theme = MokaTheme.FromSeed(new MokaThemeSeed { Accent = "#0EA5E9" }, dark: true).WithAccent("hsl(330 80% 55%)");

		Assert.Equal(MokaCssColor.Parse("hsl(330 80% 55%)").ToHex(uppercase: true), theme.Palette.Primary);
		Assert.Equal("hsl(330 80% 55%)", theme.Seed!.Accent);
	}

	[Fact]
	public void WithNeutral_OnAPreset_SeedsItFromItsAccent()
	{
		MokaTheme theme = MokaTheme.Gate.WithCorners(MokaCorners.Medium).WithNeutral("#1C1917");

		Assert.Equal(MokaTheme.Gate.Palette.Primary, theme.Palette.Primary);
		Assert.Equal("#1C1917", theme.Seed!.Neutral);
		Assert.Equal(MokaTheme.Gate.WithCorners(MokaCorners.Medium).Spacing, theme.Spacing);
		Assert.NotEqual(MokaTheme.Gate.Palette.Surface, theme.Palette.Surface);
	}

	[Fact]
	public void WithSeed_MovesAnOriginalDesignThemeToTheNewOne()
	{
		MokaTheme theme = MokaTheme.Dark.WithSeed(new MokaThemeSeed { Accent = "#EF5350" });

		Assert.Equal(MokaLook.Gate, theme.Look);
		Assert.Equal(MokaTypography.Gate, theme.Typography);
		Assert.True(theme.IsDark);
	}

	[Fact]
	public void WithSurfaces_OnASeededTheme_TakesThatFamilysNeutral()
	{
		MokaTheme theme = MokaTheme.FromSeed(new MokaThemeSeed { Accent = "#EF5350" }, dark: true).WithSurfaces(MokaSurfaces.BlueGrey);

		Assert.Equal(MokaPalette.Gate.Surface, theme.Seed!.Neutral);
		Assert.Equal(MokaSurfaces.BlueGrey, theme.Surfaces);
	}

	[Fact]
	public void AGivenMeaning_IsKeptWhereItReads()
	{
		MokaTheme theme = MokaTheme.FromSeed(new MokaThemeSeed { Accent = "#0EA5E9", Success = "#22C55E" }, dark: true);

		Assert.Equal("#22C55E", theme.Palette.Success);
	}

	[Fact]
	public void ABadSeed_SaysWhatIsWrong()
	{
		Assert.Throws<ArgumentException>(() => MokaTheme.FromSeed(new MokaThemeSeed { Accent = "not a color" }, true));
		Assert.Throws<ArgumentException>(() => MokaTheme.FromSeed(new MokaThemeSeed { Accent = "#7C3AED80" }, true));
		Assert.Throws<ArgumentException>(() => MokaTheme.FromSeed(new MokaThemeSeed { Accent = "#7C3AED", Neutral = "var(--x)" }, true));
	}

	[Fact]
	public void EqualSeeds_GiveEqualThemes() =>
		Assert.Equal(
			MokaTheme.FromSeed(new MokaThemeSeed { Accent = "#7C3AED", Neutral = "#111723" }, true),
			MokaTheme.FromSeed(new MokaThemeSeed { Accent = "#7C3AED", Neutral = "#111723" }, true));

	private static double Ratio(MokaTheme theme, string token)
	{
		Assert.True(theme.TryResolveColor(token, out MokaSrgb color));
		Assert.True(theme.TryResolveColor("--moka-color-surface", out MokaSrgb surface));
		return MokaContrast.Ratio(color, surface);
	}
}
