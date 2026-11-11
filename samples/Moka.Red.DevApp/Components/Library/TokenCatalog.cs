using System.Text;
using System.Text.RegularExpressions;
using Moka.Red.Core.Theming;

namespace Moka.Red.DevApp.Components.Library;

internal enum TokenKind
{
	Color,
	Shadow,
	Radius,
	Spacing,
	Font,
	Size,
	ZIndex,
	Transition,
	Other
}

internal sealed record TokenInfo(string Name, string Written, TokenKind Kind, bool FromTheme)
{
	public string Short => Name.StartsWith("--moka-", StringComparison.Ordinal) ? Name[7..] : Name;

	public string Reference => $"var({Name})";

	// The sample reads the token through --tok, so the page's stylesheet only ever names that one property.
	public string SampleStyle => $"--tok: var({Name})";
}

// A token is what the theme writes plus what moka.css declares on :root, the same set ThemeTokenParityTests holds
// the two to. moka.css is embedded in the dev app, so its defaults are read here rather than guessed.
internal static partial class TokenCatalog
{
	private static readonly Lazy<Defaults> CssDefaults = new(LoadDefaults);

	public static IReadOnlyList<TokenKind> Kinds { get; } = Enum.GetValues<TokenKind>();

	public static IReadOnlyList<TokenInfo> For(MokaTheme theme)
	{
		var tokens = new List<TokenInfo>();
		var seen = new HashSet<string>(StringComparer.Ordinal);
		foreach ((string name, string value) in Declarations(theme.ToCssVariables()))
		{
			if (seen.Add(name))
			{
				tokens.Add(new TokenInfo(name, value, KindOf(name), true));
			}
		}

		Defaults defaults = CssDefaults.Value;
		foreach ((string name, string value) in defaults.Root)
		{
			if (seen.Add(name))
			{
				string written = theme.IsDark && defaults.Dark.TryGetValue(name, out string? dark) ? dark : value;
				tokens.Add(new TokenInfo(name, written, KindOf(name), false));
			}
		}

		return tokens;
	}

	// Which drawing a sample gets. A length becomes a bar, but a shorthand such as "1.5rem 2rem" is no width.
	public static string SampleOf(TokenInfo token, string value)
	{
		string name = token.Short;
		return token.Kind switch
		{
			TokenKind.Color => "color",
			TokenKind.Shadow => "shadow",
			TokenKind.Radius => "radius",
			TokenKind.Transition => "motion",
			TokenKind.Font when name.StartsWith("font-family", StringComparison.Ordinal) => "family",
			TokenKind.Font when name.StartsWith("font-size", StringComparison.Ordinal) => "size",
			TokenKind.Font when name.StartsWith("font-weight", StringComparison.Ordinal) => "weight",
			TokenKind.Font when name.EndsWith("-case", StringComparison.Ordinal) => "case",
			TokenKind.Font when name.EndsWith("-tracking", StringComparison.Ordinal) => "tracking",
			TokenKind.Spacing or TokenKind.Size when IsLength(value) => "bar",
			_ => "text"
		};
	}

	private static bool IsLength(string value) =>
		value.StartsWith("calc(", StringComparison.Ordinal)
		|| (!value.Contains(' ', StringComparison.Ordinal)
			&& (value.EndsWith("px", StringComparison.Ordinal) || value.EndsWith("rem", StringComparison.Ordinal)
				|| value.EndsWith("em", StringComparison.Ordinal)));

	public static string Label(TokenKind kind) => kind switch
	{
		TokenKind.ZIndex => "Z-index",
		_ => kind.ToString()
	};

	public static TokenKind KindOf(string name)
	{
		string token = name.StartsWith("--moka-", StringComparison.Ordinal) ? name[7..] : name.TrimStart('-');
		if (token.Contains("shadow", StringComparison.Ordinal) || token.StartsWith("focus-ring", StringComparison.Ordinal)
			|| token is "selected-glow" or "selected-bar"
			|| (token.EndsWith("-ring", StringComparison.Ordinal) && !token.StartsWith("color-", StringComparison.Ordinal)))
		{
			return TokenKind.Shadow;
		}

		if (token.StartsWith("color-", StringComparison.Ordinal))
		{
			return TokenKind.Color;
		}

		if (token.StartsWith("radius-", StringComparison.Ordinal))
		{
			return TokenKind.Radius;
		}

		if (token.StartsWith("z-", StringComparison.Ordinal))
		{
			return TokenKind.ZIndex;
		}

		if (token.StartsWith("transition-", StringComparison.Ordinal))
		{
			return TokenKind.Transition;
		}

		if (token.StartsWith("font-", StringComparison.Ordinal) || token.StartsWith("line-height-", StringComparison.Ordinal)
			|| token.EndsWith("-case", StringComparison.Ordinal) || token.EndsWith("-tracking", StringComparison.Ordinal))
		{
			return TokenKind.Font;
		}

		if (token.StartsWith("spacing-", StringComparison.Ordinal) || token.Contains("padding", StringComparison.Ordinal)
			|| token.EndsWith("-gap", StringComparison.Ordinal) || token == "density")
		{
			return TokenKind.Spacing;
		}

		if (token.StartsWith("height-", StringComparison.Ordinal) || token.Contains("height", StringComparison.Ordinal)
			|| token.EndsWith("-width", StringComparison.Ordinal) || token.Contains("blur", StringComparison.Ordinal)
			|| token == "menu-row")
		{
			return TokenKind.Size;
		}

		if (token.EndsWith("-color", StringComparison.Ordinal) || token.EndsWith("-bg", StringComparison.Ordinal)
			|| token.EndsWith("-icon", StringComparison.Ordinal) || token.EndsWith("-edge", StringComparison.Ordinal)
			|| token.EndsWith("-border", StringComparison.Ordinal))
		{
			return TokenKind.Color;
		}

		return TokenKind.Other;
	}

	// ToCssVariables writes "name: value; " and leaves out any value holding a semicolon, so a split on it is exact.
	public static IEnumerable<(string Name, string Value)> Declarations(string css)
	{
		foreach (string part in css.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
		{
			int colon = part.IndexOf(':', StringComparison.Ordinal);
			if (colon <= 0)
			{
				continue;
			}

			string name = part[..colon].Trim();
			if (name.StartsWith("--moka-", StringComparison.Ordinal))
			{
				yield return (name, Squash(part[(colon + 1)..]));
			}
		}
	}

	[GeneratedRegex(@"/\*.*?\*/", RegexOptions.Singleline)]
	private static partial Regex Comment();

	[GeneratedRegex(@"(?<selectors>[^{}]+)\{(?<body>[^{}]*)\}")]
	private static partial Regex Rule();

	[GeneratedRegex(@"\s+")]
	private static partial Regex Space();

	private static string Squash(string value) => Space().Replace(value, " ").Trim();

	private static Defaults LoadDefaults()
	{
		var root = new List<(string Name, string Value)>();
		var dark = new Dictionary<string, string>(StringComparer.Ordinal);
		using Stream? stream = typeof(TokenCatalog).Assembly.GetManifestResourceStream("moka.css");
		if (stream is null)
		{
			return new Defaults(root, dark);
		}

		using var reader = new StreamReader(stream, Encoding.UTF8);
		string css = Comment().Replace(reader.ReadToEnd(), string.Empty);
		foreach (Match rule in Rule().Matches(css))
		{
			string[] selectors = rule.Groups["selectors"].Value
				.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
			bool onRoot = selectors.Contains(":root", StringComparer.Ordinal);
			bool onDark = selectors.Contains(".moka-dark", StringComparer.Ordinal);
			if (!onRoot && !onDark)
			{
				continue;
			}

			foreach ((string name, string value) in Declarations(rule.Groups["body"].Value))
			{
				if (onRoot)
				{
					int at = root.FindIndex(entry => string.Equals(entry.Name, name, StringComparison.Ordinal));
					if (at >= 0)
					{
						root[at] = (name, value);
					}
					else
					{
						root.Add((name, value));
					}
				}

				if (onDark)
				{
					dark[name] = value;
				}
			}
		}

		return new Defaults(root, dark);
	}

	private sealed record Defaults(List<(string Name, string Value)> Root, Dictionary<string, string> Dark);
}
