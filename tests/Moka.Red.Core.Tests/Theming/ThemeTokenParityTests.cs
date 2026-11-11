using System.Text.RegularExpressions;
using Moka.Red.Core.Theming;
using Moka.Red.Tests.Shared;

namespace Moka.Red.Core.Tests.Theming;

// A token lives in the palette, in ToCssVariables() and in moka.css. Miss moka.css and it only breaks for
// components drawn without a provider, where nobody is looking.
public partial class ThemeTokenParityTests
{
	[Fact]
	public void EveryThemeToken_HasAFallbackInMokaCss()
	{
		IReadOnlyDictionary<string, string> root = ScopedCss.Declarations("moka.css", ":root");

		List<string> missing = CssDeclarations.PropertyNames(MokaTheme.Light.ToCssVariables())
			.Where(token => !root.ContainsKey(token))
			.ToList();

		Assert.True(missing.Count == 0, $"Not in moka.css: {string.Join(", ", missing)}");
	}

	[Fact]
	public void EveryFallbackInMokaCss_IsAThemeToken()
	{
		List<string> tokens = CssDeclarations.PropertyNames(MokaTheme.Light.ToCssVariables());

		List<string> extra = ScopedCss.Declarations("moka.css", ":root").Keys
			.Where(token => !tokens.Contains(token))
			.ToList();

		Assert.True(extra.Count == 0, $"Only in moka.css: {string.Join(", ", extra)}");
	}

	// A custom property resolves its var() where it is declared and inherits the result, so a token that reads
	// one .moka-dark changes would keep the light value under .moka-dark unless it is declared there too.
	[Fact]
	public void EveryTokenReadingADarkToken_IsDeclaredAgainUnderDark()
	{
		IReadOnlyDictionary<string, string> root = ScopedCss.Declarations("moka.css", ":root");
		IReadOnlyDictionary<string, string> dark = ScopedCss.Declarations("moka.css", ".moka-dark");

		List<string> missing = root
			.Where(token => !dark.ContainsKey(token.Key) && Reference().Matches(token.Value)
				.Any(reference => dark.ContainsKey(reference.Groups["name"].Value)))
			.Select(token => token.Key)
			.ToList();

		Assert.True(missing.Count == 0, $"Not declared under .moka-dark: {string.Join(", ", missing)}");
	}

	// The scoped rule outranks anything moka.css could say about the root, so the size has to come through a
	// token for a look to change it.
	[Fact]
	public void TheRoot_ReadsItsTextFromTheTokens()
	{
		IReadOnlyDictionary<string, string> root = ScopedCss.Declarations("MokaThemeProvider.razor.css", ".moka-root");

		Assert.Equal("var(--moka-font-family)", root["font-family"]);
		Assert.Equal("var(--moka-font-size-root)", root["font-size"]);
		Assert.Equal("var(--moka-color-on-background)", root["color"]);
		Assert.Equal("var(--moka-color-background)", root["background-color"]);
	}

	[Fact]
	public void TheRootSize_Is13OnMatrixAnd14OnGate()
	{
		Assert.Contains("--moka-font-size-root: var(--moka-font-size-base);", MokaTheme.Dark.ToCssVariables(), StringComparison.Ordinal);
		Assert.Contains("--moka-font-size-root: var(--moka-font-size-md);", MokaTheme.Gate.ToCssVariables(), StringComparison.Ordinal);
		Assert.Contains("--moka-font-size-root: var(--moka-font-size-md);", MokaTheme.GateLight.ToCssVariables(), StringComparison.Ordinal);
	}

	[Fact]
	public void TheGateRules_SetTheAccentAndScheme()
	{
		IReadOnlyDictionary<string, string> gate = ScopedCss.Declarations("moka.css", ".moka-gate");

		Assert.Equal("var(--moka-color-primary)", gate["accent-color"]);
		Assert.Equal("light", gate["color-scheme"]);
		Assert.Equal("dark", ScopedCss.Declarations("moka.css", ".moka-gate.moka-dark")["color-scheme"]);
	}

	[Theory]
	[InlineData("moka-fonts.css", 6)]
	[InlineData("moka-flags.css", 1)]
	public void TheFontStylesheets_NameOnlyFilesThatShip(string stylesheet, int count)
	{
		string css = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Css", stylesheet));

		List<string> files = FontFile().Matches(css).Select(match => match.Groups["path"].Value).ToList();

		Assert.Equal(count, files.Count);
		Assert.All(files, file => Assert.True(File.Exists(Path.Combine(AppContext.BaseDirectory, "Css", file)), $"{file} does not ship."));
	}

	// LoadFonts and LoadFlagFont each link one stylesheet, so the flag face lives in one and the text fonts in the
	// other: an app turning off the text fonts keeps flags, and one with both on doesn't declare the face twice.
	[Fact]
	public void TheFlagFace_IsOnlyInTheFlagStylesheet()
	{
		string fonts = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Css", "moka-fonts.css"));
		string flags = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Css", "moka-flags.css"));

		Assert.DoesNotContain("font-family: 'Twemoji Country Flags'", fonts, StringComparison.Ordinal);
		Assert.Single(FontFile().Matches(flags));
		Assert.Contains("font-family: 'Twemoji Country Flags'", flags, StringComparison.Ordinal);
		Assert.DoesNotContain("'Inter'", flags, StringComparison.Ordinal);
	}

	// The SIL Open Font License asks for its text to travel with the fonts.
	[Theory]
	[InlineData("inter")]
	[InlineData("space-grotesk")]
	[InlineData("jetbrains-mono")]
	public void EveryFontFamily_ShipsItsLicense(string family) =>
		Assert.True(File.Exists(Path.Combine(AppContext.BaseDirectory, "Css", "fonts", $"{family}.OFL.txt")));

	// CC BY 4.0 asks for credit, a link to the license and a word on changes wherever the artwork goes.
	[Fact]
	public void TheFlagFont_ShipsItsLicenseAndAttribution()
	{
		string fonts = Path.Combine(AppContext.BaseDirectory, "Css", "fonts");
		string notice = File.ReadAllText(Path.Combine(fonts, "twemoji-country-flags.NOTICE.txt"));

		Assert.True(File.Exists(Path.Combine(fonts, "twemoji-country-flags.LICENSE.txt")));
		Assert.Contains("Twitter, Inc and other contributors", notice, StringComparison.Ordinal);
		Assert.Contains("https://creativecommons.org/licenses/by/4.0/", notice, StringComparison.Ordinal);
	}

	// A flag font covering more than the regional indicators and tags would be fetched for ordinary text.
	[Fact]
	public void TheFlagFont_IsFetchedOnlyForFlags()
	{
		string css = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Css", "moka-flags.css"));
		int face = css.IndexOf("'Twemoji Country Flags'", StringComparison.Ordinal);
		string block = css[face..css.IndexOf('}', face)];

		Assert.Contains("unicode-range: U+1F1E6-1F1FF, U+1F3F4, U+E0062-E0063, U+E0065, U+E0067, U+E006C, U+E006E, U+E0073-E0074, U+E0077, U+E007F;", block, StringComparison.Ordinal);
	}

	[GeneratedRegex(@"var\((?<name>--[a-z0-9-]+)")]
	private static partial Regex Reference();

	[GeneratedRegex(@"url\('(?<path>[^']+)'\)")]
	private static partial Regex FontFile();
}
