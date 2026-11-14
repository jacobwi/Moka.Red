using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Moka.Red.Core.Colors;

namespace Moka.Red.Core.Theming;

// Works a token's value out to one color the way a browser would: var() through the token map (with its fallback when
// the token is missing), color-mix() in srgb or oklab with the percentages CSS gives them, and any color MokaCssColor
// reads. What depends on the element, such as currentColor, has no single answer and isn't resolved.
internal sealed class MokaTokenResolver(IReadOnlyDictionary<string, string> tokens)
{
	private const int MaxDepth = 32;

	internal bool TryResolve(string value, out MokaSrgb color) => TryResolve(value.Trim(), 0, out color);

	private bool TryResolve(string value, int depth, out MokaSrgb color)
	{
		color = default;
		if (depth > MaxDepth || value.Length == 0)
		{
			return false;
		}

		if (value.StartsWith("--", StringComparison.Ordinal))
		{
			return tokens.TryGetValue(value, out string? token) && TryResolve(token.Trim(), depth + 1, out color);
		}

		if (Function(value, "var", out string? variable))
		{
			List<string> parts = SplitTopLevel(variable, ',', 2);
			string name = parts[0].Trim();
			return tokens.TryGetValue(name, out string? token)
				? TryResolve(token.Trim(), depth + 1, out color)
				: parts.Count == 2 && TryResolve(parts[1].Trim(), depth + 1, out color);
		}

		if (Function(value, "color-mix", out string? mix))
		{
			return TryMix(mix, depth, out color);
		}

		return MokaCssColor.TryParse(value, out color);
	}

	private bool TryMix(string arguments, int depth, out MokaSrgb color)
	{
		color = default;
		List<string> parts = SplitTopLevel(arguments, ',', int.MaxValue);
		if (parts.Count != 3)
		{
			return false;
		}

		string space = parts[0].Trim();
		bool oklab = space.Equals("in oklab", StringComparison.OrdinalIgnoreCase);
		if (!oklab && !space.Equals("in srgb", StringComparison.OrdinalIgnoreCase))
		{
			return false;
		}

		if (!TryStop(parts[1], depth, out MokaSrgb first, out double? firstPercent) ||
		    !TryStop(parts[2], depth, out MokaSrgb second, out double? secondPercent))
		{
			return false;
		}

		double p1 = firstPercent ?? (secondPercent is { } q ? 100 - q : 50);
		double p2 = secondPercent ?? 100 - p1;
		double sum = p1 + p2;
		if (sum <= 0)
		{
			return false;
		}

		MokaSrgb mixed = oklab ? MokaColorMix.InOklab(first, second, p1 / sum) : MokaColorMix.InSrgb(first, second, p1 / sum);
		color = sum < 100 ? mixed with { Alpha = mixed.Alpha * sum / 100 } : mixed;
		return true;
	}

	// One color in a color-mix(), with its percentage when it has one.
	private bool TryStop(string stop, int depth, out MokaSrgb color, out double? percent)
	{
		percent = null;
		string text = stop.Trim();
		List<string> words = SplitTopLevel(text, ' ', int.MaxValue).Where(word => word.Length > 0).ToList();
		if (words.Count > 1 && words[^1].EndsWith('%') &&
		    double.TryParse(words[^1][..^1], NumberStyles.Float, CultureInfo.InvariantCulture, out double share))
		{
			percent = share;
			text = string.Join(' ', words[..^1]);
		}

		return TryResolve(text, depth + 1, out color);
	}

	private static bool Function(string value, string name, [NotNullWhen(true)] out string? arguments)
	{
		arguments = null;
		if (!value.StartsWith(name, StringComparison.OrdinalIgnoreCase) || value[^1] != ')')
		{
			return false;
		}

		string rest = value[name.Length..].TrimStart();
		if (rest.Length == 0 || rest[0] != '(')
		{
			return false;
		}

		arguments = rest[1..^1];
		return true;
	}

	private static List<string> SplitTopLevel(string value, char separator, int maxParts)
	{
		List<string> parts = [];
		int depth = 0;
		int start = 0;
		for (int i = 0; i < value.Length; i++)
		{
			char c = value[i];
			if (c == '(')
			{
				depth++;
			}
			else if (c == ')')
			{
				depth--;
			}
			else if (c == separator && depth == 0 && parts.Count < maxParts - 1)
			{
				parts.Add(value[start..i]);
				start = i + 1;
			}
		}

		parts.Add(value[start..]);
		return parts;
	}
}
