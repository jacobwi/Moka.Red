using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using Moka.Red.Core.Theming;
using Moka.Red.ThemeGen.Serialization;

namespace Moka.Red.ThemeGen.Tests.Serialization;

// JSON and C# export used to write only the 23 core palette colors plus typography and spacing. Reading
// back lost Density, FontScale and the other palette colors (they came back as the dark MokaPalette
// initializer values, so a light theme got SurfaceHover #2a2a2a), {"palette": null} threw a
// NullReferenceException, and the C# export did not escape quotes.
public class MokaThemeSerializerTests
{
	[Fact]
	public void Json_RoundTripsEveryProperty()
	{
		MokaTheme theme = ThemeReflection.EveryPropertySetImportable();

		MokaTheme? restored = MokaThemeSerializer.FromJson(MokaThemeSerializer.ToJson(theme));

		Assert.NotNull(restored);
		ThemeReflection.AssertSameProperties(theme, restored);
	}

	[Fact]
	public async Task CSharp_CompilesBackToEveryProperty()
	{
		MokaTheme theme = ThemeReflection.EveryPropertySet();
		string code = MokaThemeSerializer.ToCSharp(theme);

		ScriptOptions options = ScriptOptions.Default
			.AddReferences(typeof(MokaTheme).Assembly)
			.AddImports("Moka.Red.Core.Theming");
		MokaTheme restored = await CSharpScript.EvaluateAsync<MokaTheme>(code, options,
			cancellationToken: TestContext.Current.CancellationToken);

		ThemeReflection.AssertSameProperties(theme, restored);
	}

	[Fact]
	public void Json_RoundTripsTheLightTheme() =>
		Assert.Equal(MokaTheme.Light, MokaThemeSerializer.FromJson(MokaThemeSerializer.ToJson(MokaTheme.Light)));

	[Fact]
	public void Json_RoundTripsTheDarkTheme() =>
		Assert.Equal(MokaTheme.Dark, MokaThemeSerializer.FromJson(MokaThemeSerializer.ToJson(MokaTheme.Dark)));

	[Fact]
	public void Json_RoundTripsDensityAndFontScale()
	{
		MokaTheme theme = MokaTheme.Light.WithDensity(0.75).WithFontScale(1.15);

		MokaTheme? restored = MokaThemeSerializer.FromJson(MokaThemeSerializer.ToJson(theme));

		Assert.NotNull(restored);
		Assert.Equal(0.75, restored.Density);
		Assert.Equal(1.15, restored.FontScale);
	}

	[Fact]
	public void Json_RoundTripsANonFiniteDensityInsteadOfThrowing()
	{
		MokaTheme theme = MokaTheme.Light.WithDensity(double.NaN).WithFontScale(double.PositiveInfinity);

		MokaTheme? restored = MokaThemeSerializer.FromJson(MokaThemeSerializer.ToJson(theme));

		Assert.NotNull(restored);
		Assert.True(double.IsNaN(restored.Density));
		Assert.Equal(double.PositiveInfinity, restored.FontScale);
	}

	[Fact]
	public void Json_FillsMissingPaletteColorsFromTheMatchingBuiltInPalette()
	{
		// The shape the old export wrote: core colors only, no density or font scale.
		const string json = """
			{
			  "isDark": false,
			  "palette": { "primary": "#123456", "surface": "#ffffff" }
			}
			""";

		MokaTheme? theme = MokaThemeSerializer.FromJson(json);

		Assert.NotNull(theme);
		Assert.Equal("#123456", theme.Palette.Primary);
		Assert.Equal(MokaPalette.Light.SurfaceHover, theme.Palette.SurfaceHover);
		Assert.Equal(MokaPalette.Light.OnSurfaceQuaternary, theme.Palette.OnSurfaceQuaternary);
		Assert.Equal(1.0, theme.Density);
	}

	[Fact]
	public void Json_FillsMissingDarkPaletteColorsFromTheDarkPalette()
	{
		MokaTheme? theme = MokaThemeSerializer.FromJson("""{ "isDark": true, "palette": { "primary": "#123456" } }""");

		Assert.NotNull(theme);
		Assert.True(theme.IsDark);
		Assert.Equal("#123456", theme.Palette.Primary);
		Assert.Equal(MokaPalette.Dark.Surface, theme.Palette.Surface);
		Assert.Equal(MokaPalette.Dark.SurfaceHover, theme.Palette.SurfaceHover);
	}

	[Fact]
	public void Json_MatchesNamesWhateverTheirCase()
	{
		MokaTheme? theme = MokaThemeSerializer.FromJson("""{ "IsDark": true, "Palette": { "Primary": "#123456" } }""");

		Assert.NotNull(theme);
		Assert.True(theme.IsDark);
		Assert.Equal("#123456", theme.Palette.Primary);
	}

	[Theory]
	[InlineData("")]
	[InlineData("   ")]
	[InlineData("null")]
	[InlineData("[]")]
	[InlineData("42")]
	[InlineData("\"theme\"")]
	[InlineData("{")]
	[InlineData("{\"palette\": null}")]
	[InlineData("{\"palette\": 5}")]
	[InlineData("{\"palette\": {\"primary\": null}}")]
	[InlineData("{\"palette\": {\"primary\": 5}}")]
	[InlineData("{\"palette\": {\"primary\": \"  \"}}")]
	[InlineData("{\"typography\": null}")]
	[InlineData("{\"typography\": {\"fontFamily\": null}}")]
	[InlineData("{\"spacing\": []}")]
	[InlineData("{\"spacing\": {\"md\": false}}")]
	[InlineData("{\"isDark\": \"yes\"}")]
	[InlineData("{\"isDark\": null}")]
	[InlineData("{\"density\": \"wide\"}")]
	[InlineData("{\"density\": null}")]
	[InlineData("{\"fontScale\": 1e400}")]
	public void FromJson_ReturnsNullForBadInputWithoutThrowing(string json) =>
		Assert.Null(MokaThemeSerializer.FromJson(json));

	[Fact]
	public void CSharp_EscapesQuotesAndBackslashes()
	{
		MokaTheme theme = MokaTheme.Light with
		{
			Typography = MokaTypography.Default with { FontFamily = "\"Fira Sans\", C:\\fonts" }
		};

		string code = MokaThemeSerializer.ToCSharp(theme);

		Assert.Contains("FontFamily = \"\\\"Fira Sans\\\", C:\\\\fonts\",", code, StringComparison.Ordinal);
	}
}
