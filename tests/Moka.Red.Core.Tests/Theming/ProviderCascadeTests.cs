using System.Text;
using AngleSharp.Dom;
using Bunit;
using Moka.Red.Core.Theming;
using Moka.Red.Tests.Shared;

namespace Moka.Red.Core.Tests.Theming;

// A provider's root declares only what would otherwise come out wrong, so these work the cascade out the way a browser
// does (moka.css's :root and .moka-dark rules, the provider's :root rule, the roots' own declarations, inheritance, and
// var() resolved where it is declared) and check every token lands on the theme's own value.
public sealed class ProviderCascadeTests : BunitContext
{
	private static readonly Dictionary<string, MokaTheme> Themes = new(StringComparer.Ordinal)
	{
		["Red"] = MokaTheme.Red,
		["RedLight"] = MokaTheme.RedLight,
		["Gate"] = MokaTheme.Gate,
		["GateLight"] = MokaTheme.GateLight,
		["Crisp"] = MokaTheme.Crisp,
		["Matrix"] = MokaTheme.Matrix,
		["Ember"] = MokaTheme.Ember,
		["Dark"] = MokaTheme.Dark,
		["Light"] = MokaTheme.Light,
		["DarkAccent"] = MokaTheme.Dark.WithAccent("#42a5f5"),
		["Seeded"] = MokaTheme.FromSeed(new MokaThemeSeed { Accent = "#7C3AED", Neutral = "#1C1917" }, true),
		["SeededLight"] = MokaTheme.FromSeed(new MokaThemeSeed { Accent = "#7C3AED", Neutral = "#1C1917" }, false),
		["Pinned"] = MokaTheme.Gate.WithToken("--moka-color-surface", "#202830").WithToken("--moka-color-primary", "#ff8800"),
		["HighContrast"] = MokaTheme.RedLight.WithHighContrast().WithSelection(MokaSelection.Glow)
	};

	public static TheoryData<string, string> Pairs()
	{
		TheoryData<string, string> data = [];
		foreach (string outer in Themes.Keys)
		{
			foreach (string inner in Themes.Keys)
			{
				data.Add(outer, inner);
			}
		}

		return data;
	}

	[Theory]
	[MemberData(nameof(Pairs))]
	public void EveryToken_LandsOnTheThemesValue_InTheOuterAndTheNestedRoot(string outer, string inner)
	{
		MokaTheme outerTheme = Themes[outer];
		MokaTheme innerTheme = Themes[inner];
		IRenderedComponent<MokaThemeProvider> cut = Render<MokaThemeProvider>(p => p
			.Add(x => x.Theme, outerTheme)
			.AddChildContent<MokaThemeProvider>(nested => nested
				.Add(x => x.Theme, innerTheme)
				.AddChildContent("<span>Nested</span>")));

		IReadOnlyList<IElement> roots = cut.FindAll(".moka-root");
		string rootRule = cut.FindAll("style")[0].TextContent;

		Dictionary<string, string> mokaRoot = new(ScopedCss.Declarations("moka.css", ":root"), StringComparer.Ordinal);
		Dictionary<string, string> mokaDark = new(ScopedCss.Declarations("moka.css", ".moka-dark:not(.moka-root)"), StringComparer.Ordinal);

		Dictionary<string, string> page = Compute(Merge(mokaRoot, Declarations(rootRule[":root {".Length..^1])), []);
		Dictionary<string, string> outerRoot = Compute(Merge(Dark(roots[0], mokaDark), Declarations(roots[0].GetAttribute("style"))), page);
		Dictionary<string, string> innerRoot = Compute(Merge(Dark(roots[1], mokaDark), Declarations(roots[1].GetAttribute("style"))), outerRoot);

		AssertLands(outerTheme, outerRoot, $"{outer} as the outer provider");
		AssertLands(innerTheme, innerRoot, $"{inner} inside {outer}");
	}

	// Only :root and the dark rules for markup without a provider set theme tokens in moka.css. A rule on a class a
	// provider's root carries would set tokens the root doesn't declare again.
	[Fact]
	public void MokaCss_SetsThemeTokensOnlyOnRootAndDark()
	{
		string css = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Css", "moka.css"));
		HashSet<string> names = [.. MokaTokens.All.Select(token => token.Name)];
		List<string> elsewhere = [];
		foreach (System.Text.RegularExpressions.Match rule in System.Text.RegularExpressions.Regex.Matches(
			         System.Text.RegularExpressions.Regex.Replace(css, @"/\*.*?\*/", "", System.Text.RegularExpressions.RegexOptions.Singleline),
			         @"(?<selectors>[^{}]+)\{(?<body>[^{}]*)\}"))
		{
			string[] selectors = [.. rule.Groups["selectors"].Value.Split(',').Select(s => s.Trim())];
			if (selectors.All(s => s is ":root" or ".moka-dark:not(.moka-root)"))
			{
				continue;
			}

			elsewhere.AddRange(Declarations(rule.Groups["body"].Value).Keys.Where(names.Contains).Select(name => $"{string.Join(", ", selectors)}: {name}"));
		}

		Assert.Empty(elsewhere);
	}

	// Several outermost providers can share a page, each writing :root, so each root keeps every token.
	[Fact]
	public void TheOuterRoot_DeclaresEveryToken()
	{
		IRenderedComponent<MokaThemeProvider> cut = Render<MokaThemeProvider>(p => p.Add(x => x.Theme, MokaTheme.RedLight));

		Assert.Equal(MokaTheme.RedLight.ToCssVariables(), cut.Find(".moka-root").GetAttribute("style"));
		Assert.Contains(MokaTheme.RedLight.ToCssVariables(), cut.Find("style").TextContent, StringComparison.Ordinal);
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public void ANestedRoot_OfTheSameTheme_DeclaresNothing(bool dark)
	{
		MokaTheme theme = dark ? MokaTheme.Red : MokaTheme.RedLight;
		IRenderedComponent<MokaThemeProvider> cut = Render<MokaThemeProvider>(p => p
			.Add(x => x.Theme, theme)
			.AddChildContent<MokaThemeProvider>(nested => nested.Add(x => x.Theme, theme)));

		Assert.Null(cut.FindAll(".moka-root")[1].GetAttribute("style"));
	}

	[Fact]
	public void ANestedRoot_DeclaresWhatDiffers_AndWhatReadsIt()
	{
		IRenderedComponent<MokaThemeProvider> cut = Render<MokaThemeProvider>(p => p
			.Add(x => x.Theme, MokaTheme.Red)
			.AddChildContent<MokaThemeProvider>(nested => nested.Add(x => x.Theme, MokaTheme.Red.WithToken("--moka-color-background", "#000000"))));

		string style = cut.FindAll(".moka-root")[1].GetAttribute("style")!;
		Assert.Contains("--moka-color-background: #000000; ", style, StringComparison.Ordinal);
		Assert.Contains("--moka-color-backdrop-strong: color-mix(in srgb, var(--moka-color-background) 92%, transparent); ", style, StringComparison.Ordinal);
		Assert.DoesNotContain("--moka-color-surface:", style, StringComparison.Ordinal);
	}

	[Fact]
	public void AnEqualTheme_IsNotWorkedOutAgain()
	{
		IRenderedComponent<MokaThemeProvider> cut = Render<MokaThemeProvider>(p => p.Add(x => x.Theme, MokaTheme.Gate.WithAccent("#00e676")));
		string before = cut.Markup;

		cut.Render(p => p.Add(x => x.Theme, MokaTheme.Gate.WithAccent("#00e676")));

		Assert.Equal(before, cut.Markup);
	}

	private static void AssertLands(MokaTheme theme, Dictionary<string, string> computed, string what)
	{
		Dictionary<string, string> expected = Compute(new Dictionary<string, string>(theme.ToTokenMap(), StringComparer.Ordinal), []);
		List<string> wrong = [.. expected
			.Where(pair => !computed.TryGetValue(pair.Key, out string? value) || value != pair.Value)
			.Select(pair => $"{pair.Key}: wants '{pair.Value}', gets '{computed.GetValueOrDefault(pair.Key)}'")];

		Assert.True(wrong.Count == 0, $"{what}:\n{string.Join("\n", wrong.Take(20))}");
	}

	// moka.css's dark rules skip a provider's root, which these always are.
	private static Dictionary<string, string> Dark(IElement root, Dictionary<string, string> dark) =>
		root.ClassList.Contains("moka-dark") && !root.ClassList.Contains("moka-root") ? dark : [];

	private static Dictionary<string, string> Merge(Dictionary<string, string> under, Dictionary<string, string> over)
	{
		Dictionary<string, string> merged = new(under, StringComparer.Ordinal);
		foreach ((string name, string value) in over)
		{
			merged[name] = value;
		}

		return merged;
	}

	private static Dictionary<string, string> Declarations(string? style)
	{
		Dictionary<string, string> declarations = new(StringComparer.Ordinal);
		foreach (string declaration in (style ?? "").Split(';'))
		{
			int colon = declaration.IndexOf(':', StringComparison.Ordinal);
			if (colon > 0 && declaration[..colon].Trim().StartsWith("--", StringComparison.Ordinal))
			{
				declarations[declaration[..colon].Trim()] = declaration[(colon + 1)..].Trim();
			}
		}

		return declarations;
	}

	// The computed values on one element: its own declarations with var() resolved on it, the rest inherited.
	private static Dictionary<string, string> Compute(Dictionary<string, string> specified, Dictionary<string, string> inherited)
	{
		Dictionary<string, string> computed = new(StringComparer.Ordinal);

		string Value(string name, int depth)
		{
			if (computed.TryGetValue(name, out string? known))
			{
				return known;
			}

			string value = specified.TryGetValue(name, out string? own) ? Substitute(own, depth) : inherited.GetValueOrDefault(name, "");
			computed[name] = value;
			return value;
		}

		string Substitute(string value, int depth)
		{
			Assert.True(depth < 40, $"var() nests too deep in '{value}'.");
			var result = new StringBuilder();
			int i = 0;
			while (i < value.Length)
			{
				int start = value.IndexOf("var(", i, StringComparison.Ordinal);
				if (start < 0)
				{
					result.Append(value, i, value.Length - i);
					break;
				}

				result.Append(value, i, start - i);
				int level = 0, end = start + 3, comma = -1;
				for (; end < value.Length; end++)
				{
					if (value[end] == '(')
					{
						level++;
					}
					else if (value[end] == ')' && --level == 0)
					{
						break;
					}
					else if (value[end] == ',' && level == 1 && comma < 0)
					{
						comma = end;
					}
				}

				string name = value[(start + 4)..(comma < 0 ? end : comma)].Trim();
				string resolved = specified.ContainsKey(name) || inherited.ContainsKey(name) ? Value(name, depth + 1) : "";
				result.Append(resolved.Length > 0 || comma < 0 ? resolved : Substitute(value[(comma + 1)..end].Trim(), depth + 1));
				i = end + 1;
			}

			return result.ToString();
		}

		foreach (string name in specified.Keys.Concat(inherited.Keys).Distinct(StringComparer.Ordinal).ToList())
		{
			Value(name, 0);
		}

		return computed;
	}
}
