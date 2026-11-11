using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Moka.Red.Core.Theming;

namespace Moka.Red.Forms.Common;

/// <summary>
///     What the color inputs accept as a value besides a hex color: a theme token such as
///     <c>var(--moka-color-primary)</c>, which follows the theme, and how such a value becomes a color C# can measure.
/// </summary>
internal static partial class MokaColorValues
{
	// A reference can name another token in turn (outline-strong is outline on the original palettes).
	private const int MaxReferences = 8;

	private static readonly ConditionalWeakTable<MokaTheme, Dictionary<string, string>> Variables = new();

	/// <summary>The theme colors a picker offers, in the order it shows them.</summary>
	internal static IReadOnlyList<MokaThemeColorChoice> ThemeColors { get; } =
	[
		new("Primary", "primary"),
		new("Success", "success"),
		new("Warning", "warning"),
		new("Error", "error"),
		new("Info", "info"),
		new("Surface", "surface"),
		new("On surface", "on-surface"),
		new("Outline", "outline")
	];

	/// <summary>True for <c>var(--moka-color-name)</c>, a color token from the theme, and nothing more.</summary>
	internal static bool IsThemeToken([NotNullWhen(true)] string? value) => value is not null && Token().IsMatch(value);

	/// <summary>
	///     True for a value a color input may write into a style: a hex color, or a theme token. Anything else could
	///     add declarations of its own, so it shows no color.
	/// </summary>
	internal static bool IsShown([NotNullWhen(true)] string? value) => MokaHexColor.IsValid(value) || IsThemeToken(value);

	/// <summary>The value when <see cref="IsShown" /> accepts it, otherwise null.</summary>
	internal static string? ShownOrNull(string? value) => IsShown(value) ? value : null;

	/// <summary>The name of the theme color a token names, such as "Primary", or null for anything else.</summary>
	internal static string? ThemeLabel(string? value)
	{
		if (!IsThemeToken(value))
		{
			return null;
		}

		string name = Token().Match(value).Groups["name"].Value;
		foreach (MokaThemeColorChoice choice in ThemeColors)
		{
			if (choice.Name == name)
			{
				return choice.Label;
			}
		}

		return name;
	}

	/// <summary>
	///     The color a value stands for: a hex or <c>rgb()</c> color as written, a theme token as the theme defines it.
	///     False for a token the theme writes as something C# can't read, such as a <c>color-mix()</c>.
	/// </summary>
	internal static bool TryResolve(string? value, MokaTheme? theme, out MokaRgba color)
	{
		if (MokaColorSpace.TryParse(value, out color))
		{
			return true;
		}

		if (!IsThemeToken(value))
		{
			return false;
		}

		Dictionary<string, string> variables = Variables.GetValue(theme ?? MokaTheme.Default, Read);
		string? current = value;
		for (int i = 0; i < MaxReferences && current is not null; i++)
		{
			if (MokaColorSpace.TryParse(current, out color))
			{
				return true;
			}

			Match reference = Reference().Match(current);
			if (!reference.Success)
			{
				return false;
			}

			current = variables.TryGetValue(reference.Groups["name"].Value, out string? next)
				? next
				: reference.Groups["fallback"].Success ? reference.Groups["fallback"].Value : null;
		}

		color = default;
		return false;
	}

	// ToCssVariables writes "name: value; " pairs, and leaves out any value holding a semicolon.
	private static Dictionary<string, string> Read(MokaTheme theme)
	{
		Dictionary<string, string> variables = new(StringComparer.Ordinal);
		foreach (string pair in theme.ToCssVariables().Split(';', StringSplitOptions.RemoveEmptyEntries))
		{
			int colon = pair.IndexOf(':', StringComparison.Ordinal);
			if (colon > 0)
			{
				variables[pair[..colon].Trim()] = pair[(colon + 1)..].Trim();
			}
		}

		return variables;
	}

	[GeneratedRegex(@"^var\(\s*--moka-color-(?<name>[a-z0-9]+(?:-[a-z0-9]+)*)\s*\)$", RegexOptions.CultureInvariant)]
	private static partial Regex Token();

	[GeneratedRegex(@"^var\(\s*(?<name>--[a-z0-9-]+)\s*(?:,\s*(?<fallback>.+?)\s*)?\)$", RegexOptions.CultureInvariant)]
	private static partial Regex Reference();
}
