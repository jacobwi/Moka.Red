using System.Text.RegularExpressions;
using Moka.Red.Core.Theming;
using Moka.Red.Tests.Shared;

namespace Moka.Red.Core.Tests.Theming;

// The token table is the one list of tokens: ToCssVariables writes from it, the serializer reads its property tables, and
// moka.css has to give every token the value the table says, not only the name.
public sealed partial class TokenTableTests
{
	[Fact]
	public void ToCssVariables_WritesTheTableInOrder()
	{
		IEnumerable<string> written = MokaTheme.Gate.ToCssVariables()
			.Split("; ", StringSplitOptions.RemoveEmptyEntries)
			.Select(declaration => declaration[..declaration.IndexOf(':', StringComparison.Ordinal)]);

		Assert.Equal(MokaTokens.All.Select(token => token.Name), written);
	}

	[Fact]
	public void EveryToken_HasAUniqueName_ADescriptionAndAValue()
	{
		Assert.Equal(MokaTokens.All.Count, MokaTokens.All.Select(token => token.Name).Distinct(StringComparer.Ordinal).Count());
		IReadOnlyDictionary<string, string> values = MokaTheme.Red.ToTokenMap();
		Assert.All(MokaTokens.All, token =>
		{
			Assert.StartsWith("--moka-", token.Name, StringComparison.Ordinal);
			Assert.False(string.IsNullOrWhiteSpace(token.Description), token.Name);
			Assert.True(values.ContainsKey(token.Name), token.Name);
		});
	}

	[Fact]
	public void EveryRecordProperty_FeedsAToken()
	{
		HashSet<string> names = [.. MokaTokens.All.Select(token => token.Name)];

		Assert.All(MokaTokens.PaletteProperties, property => Assert.Contains(property.Token, names));
		Assert.All(MokaTokens.TypographyProperties, property => Assert.Contains(property.Token, names));
		Assert.All(MokaTokens.SpacingProperties, property => Assert.Contains(property.Token, names));
		Assert.Equal(typeof(MokaPalette).GetProperties().Count(p => p.PropertyType == typeof(string)), MokaTokens.PaletteProperties.Count);
		Assert.Equal(typeof(MokaTypography).GetProperties().Count(p => p.PropertyType == typeof(string)), MokaTokens.TypographyProperties.Count);
		Assert.Equal(typeof(MokaSpacing).GetProperties().Count(p => p.PropertyType == typeof(string)), MokaTokens.SpacingProperties.Count);
	}

	[Fact]
	public void TheProperties_ReadAndSetTheirRecord()
	{
		MokaThemeProperty<MokaPalette> primary = MokaTokens.PaletteProperties.Single(p => p.Name == nameof(MokaPalette.Primary));

		Assert.Equal(MokaPalette.Gate.Primary, primary.Get(MokaPalette.Gate));
		Assert.Equal("#123456", primary.Set(MokaPalette.Gate, "#123456").Primary);
	}

	[Theory]
	[InlineData("--moka-color-primary", true)]
	[InlineData("--moka-code-keyword", true)]
	[InlineData("--moka-nothing", false)]
	[InlineData(null, false)]
	public void TryFind_FindsByName(string? name, bool found) => Assert.Equal(found, MokaTokens.TryFind(name, out _));

	[Fact]
	public void References_ListsWhatAValueReads() =>
		Assert.Equal(
			["--moka-color-primary", "--moka-color-surface"],
			MokaTokens.References("color-mix(in srgb, var(--moka-color-primary) 20%, var( --moka-color-surface))"));

	// moka.css's :root block is MokaTheme.Light and its .moka-dark block MokaTheme.Dark, for components drawn without a
	// provider. A value that drifts from the theme only shows there, where nobody looks.
	[Fact]
	public void MokaCss_GivesEveryTokenTheTablesFallback()
	{
		IReadOnlyDictionary<string, string> root = ScopedCss.Declarations("moka.css", ":root");
		IReadOnlyDictionary<string, string> dark = ScopedCss.Declarations("moka.css", ".moka-dark:not(.moka-root)");

		List<string> drift = [];
		foreach (MokaToken token in MokaTokens.All)
		{
			if (!root.TryGetValue(token.Name, out string? rootValue) || Normal(rootValue) != Normal(token.Fallback))
			{
				drift.Add($":root {token.Name} is '{rootValue}', the table says '{token.Fallback}'");
			}

			// Declared under .moka-dark exactly when the table has a dark value: a provider's root declares those again, and
			// only those, so one moka.css sets on a dark root without the table knowing would undo the theme there.
			bool declared = dark.TryGetValue(token.Name, out string? darkValue);
			if (token.DarkFallback is null ? declared : !declared || Normal(darkValue!) != Normal(token.DarkFallback))
			{
				drift.Add($".moka-dark {token.Name} is '{darkValue}', the table says '{token.DarkFallback}'");
			}
		}

		Assert.True(drift.Count == 0, string.Join("\n", drift));
	}

	private static string Normal(string value) => Space().Replace(value.Trim(), " ");

	[GeneratedRegex(@"\s+")]
	private static partial Regex Space();
}
