using Moka.Red.Core.Theming;
using Moka.Red.ThemeGen.Serialization;

namespace Moka.Red.ThemeGen.Tests.Serialization;

// The import tab shows these messages, so each names what is wrong and where.
public class MokaThemeSerializerErrorTests
{
	[Theory]
	[InlineData("", "The theme JSON is empty.")]
	[InlineData("[]", "The theme JSON must be an object, like the one Export writes.")]
	[InlineData("{\"palette\": null}", "'palette' must be an object.")]
	[InlineData("{\"palette\": {\"primary\": 5}}", "'palette.primary' must be a non-empty string.")]
	[InlineData("{\"isDark\": \"yes\"}", "'isDark' must be true or false.")]
	[InlineData("{\"density\": \"wide\"}", "'density' must be a number.")]
	public void TryFromJson_SaysWhatIsWrong(string json, string expected)
	{
		bool read = MokaThemeSerializer.TryFromJson(json, out MokaTheme? theme, out string? error);

		Assert.False(read);
		Assert.Null(theme);
		Assert.Equal(expected, error);
	}

	// A theme exported by a later version can carry tokens this one does not know. They are skipped
	// and the rest of the theme still imports.
	[Fact]
	public void TryFromJson_SkipsKeysItDoesNotKnow()
	{
		const string json = """
			{
				"isDark": true,
				"accentGlowFuture": "#123456",
				"palette": { "primary": "#00ff00", "primaryFuture": "#ffffff" }
			}
			""";

		bool read = MokaThemeSerializer.TryFromJson(json, out MokaTheme? theme, out string? error);

		Assert.True(read);
		Assert.Null(error);
		Assert.NotNull(theme);
		Assert.True(theme.IsDark);
		Assert.Equal("#00ff00", theme.Palette.Primary);
		Assert.Equal(MokaTheme.Dark.Palette.Secondary, theme.Palette.Secondary);
	}

	// Import took any non-empty string. The theme writes its values into a <style> element, so a brace
	// in a color ended the rule and styled the whole page, and a value of the wrong kind did nothing.
	[Theory]
	[InlineData("{\"palette\": {\"primary\": \"#fff} body { display: none } :root { --x: 1\"}}",
		"'palette.primary' must be a CSS color, like #ef5350 or rgb(239 83 80).")]
	[InlineData("{\"palette\": {\"surface\": \"url(https://example.com/x.png)\"}}",
		"'palette.surface' must be a CSS color, like #ef5350 or rgb(239 83 80).")]
	[InlineData("{\"palette\": {\"onPrimary\": \"12px\"}}",
		"'palette.onPrimary' must be a CSS color, like #ef5350 or rgb(239 83 80).")]
	[InlineData("{\"spacing\": {\"md\": \"red\"}}",
		"'spacing.md' must be a CSS length, like 8px or 0.5rem, or a calc(), min(), max(), clamp() or var() expression.")]
	[InlineData("{\"spacing\": {\"radiusSm\": \"calc(4px; color: red)\"}}",
		"'spacing.radiusSm' must be a CSS length, like 8px or 0.5rem, or a calc(), min(), max(), clamp() or var() expression.")]
	[InlineData("{\"typography\": {\"fontSizeBase\": \"large\"}}",
		"'typography.fontSizeBase' must be a CSS length, like 8px or 0.5rem, or a calc(), min(), max(), clamp() or var() expression.")]
	[InlineData("{\"typography\": {\"fontFamily\": \"Inter} body { display: none\"}}",
		"'typography.fontFamily' cannot contain ; { } < > or \\, or leave a quote, bracket or comment open.")]
	[InlineData("{\"typography\": {\"fontFamily\": \"'Inter, sans-serif\"}}",
		"'typography.fontFamily' cannot contain ; { } < > or \\, or leave a quote, bracket or comment open.")]
	[InlineData("{\"typography\": {\"fontWeightBold\": \"700</style><script>alert(1)</script>\"}}",
		"'typography.fontWeightBold' cannot contain ; { } < > or \\, or leave a quote, bracket or comment open.")]
	public void TryFromJson_RejectsAValueTheThemeCannotUse(string json, string expected)
	{
		bool read = MokaThemeSerializer.TryFromJson(json, out MokaTheme? theme, out string? error);

		Assert.False(read);
		Assert.Null(theme);
		Assert.Equal(expected, error);
	}

	[Fact]
	public void TryFromJson_TakesColorsLengthsAndExpressionsItCanUse()
	{
		const string json = """
			{
				"palette": { "primary": "oklch(62.8% 0.25 29.2)", "surface": "var(--brand-surface, #101015)" },
				"typography": {
					"fontFamily": "'Fira Sans', \"Segoe UI\", sans-serif",
					"fontSizeBase": "clamp(12px, 0.8rem, 15px)",
					"lineHeightBase": "normal",
					"fontWeightBold": "bold"
				},
				"spacing": { "md": "calc(0.5rem * 1.5)", "radiusFull": "9999px", "xs": "var(--gap)" }
			}
			""";

		bool read = MokaThemeSerializer.TryFromJson(json, out MokaTheme? theme, out string? error);

		Assert.True(read, error);
		Assert.Equal("oklch(62.8% 0.25 29.2)", theme!.Palette.Primary);
		Assert.Equal("clamp(12px, 0.8rem, 15px)", theme.Typography.FontSizeBase);
		Assert.Equal("calc(0.5rem * 1.5)", theme.Spacing.Md);
		Assert.Equal("var(--gap)", theme.Spacing.Xs);
	}

	[Fact]
	public void TryFromJson_GivesTheLineOfASyntaxError()
	{
		MokaThemeSerializer.TryFromJson("{\n  \"isDark\": tru\n}", out _, out string? error);

		Assert.NotNull(error);
		Assert.StartsWith("Not valid JSON (line 2, ", error, StringComparison.Ordinal);
	}

	[Fact]
	public void TryFromJson_ReadsWhatToJsonWrites()
	{
		bool read = MokaThemeSerializer.TryFromJson(MokaThemeSerializer.ToJson(MokaTheme.Dark), out MokaTheme? theme,
			out string? error);

		Assert.True(read);
		Assert.Null(error);
		Assert.Equal(MokaTheme.Dark, theme);
	}
}
