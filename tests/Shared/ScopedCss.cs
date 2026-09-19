using System.Text.RegularExpressions;

namespace Moka.Red.Tests.Shared;

/// <summary>
///     Reads a component's scoped stylesheet, or moka.css, for behaviour that lives only in CSS. bUnit
///     renders markup but applies no CSS, so those rules are checked as text. The test project copies
///     the files it needs into a Css folder next to the tests.
/// </summary>
internal static partial class ScopedCss
{
	/// <summary>
	///     The declarations every rule whose selector list holds <paramref name="selector" /> sets, in
	///     file order, so a later rule overrides an earlier one as it would in the browser. Rules inside
	///     a media query count too.
	/// </summary>
	public static IReadOnlyDictionary<string, string> Declarations(string file, string selector)
	{
		Dictionary<string, string> declarations = new(StringComparer.Ordinal);
		foreach ((string property, string value) in AllDeclarations(file, selector))
		{
			declarations[property] = value;
		}

		return declarations;
	}

	/// <summary>
	///     Every value <paramref name="property" /> is given in the rules for <paramref name="selector" />,
	///     in file order, for a fallback chain where a browser keeps the last value it understands.
	/// </summary>
	public static IReadOnlyList<string> Values(string file, string selector, string property) =>
		AllDeclarations(file, selector)
			.Where(declaration => declaration.Property == property)
			.Select(declaration => declaration.Value)
			.ToList();

	private static IEnumerable<(string Property, string Value)> AllDeclarations(string file, string selector)
	{
		string css = Comment().Replace(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Css", file)), "");

		foreach (Match rule in Rule().Matches(css))
		{
			IEnumerable<string> selectors = rule.Groups["selectors"].Value.Split(',').Select(s => s.Trim());
			if (!selectors.Contains(selector, StringComparer.Ordinal))
			{
				continue;
			}

			foreach (string declaration in rule.Groups["body"].Value.Split(';'))
			{
				int colon = declaration.IndexOf(':', StringComparison.Ordinal);
				if (colon > 0)
				{
					yield return (declaration[..colon].Trim(), declaration[(colon + 1)..].Trim());
				}
			}
		}
	}

	[GeneratedRegex(@"/\*.*?\*/", RegexOptions.Singleline)]
	private static partial Regex Comment();

	// The innermost blocks only, so the rules inside a media query are found and the query is not.
	[GeneratedRegex(@"(?<selectors>[^{}]+)\{(?<body>[^{}]*)\}")]
	private static partial Regex Rule();
}
