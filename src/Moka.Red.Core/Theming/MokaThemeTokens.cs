using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using System.Text;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Core.Theming;

// A theme's tokens, worked out once: the declaration string the provider writes, the values in table order, and a
// lookup by name built the first time something asks for it.
internal sealed class MokaThemeTokens
{
	// A theme built in markup is a new instance on every render, and equal themes write equal tokens, so tokens are also
	// kept by value. Few distinct themes are live at once; a full cache is emptied rather than tracked.
	private const int ByValueCacheSize = 64;

	// Keyed by the instance and released with it: the fast path for a theme seen before.
	private static readonly ConditionalWeakTable<MokaTheme, MokaThemeTokens> ByInstance = new();

	private static readonly ConcurrentDictionary<MokaTheme, MokaThemeTokens> ByValue = new();

	private readonly string?[] _values;
	private ReadOnlyDictionary<string, string>? _map;
	private int[][]? _references;

	private MokaThemeTokens(MokaTheme theme)
	{
		var context = new MokaTokenContext(theme);
		IReadOnlyList<MokaToken> tokens = MokaTokens.All;
		_values = new string?[tokens.Count];
		bool pinned = theme.Pins.Count > 0;
		var css = new StringBuilder(16384);
		for (int i = 0; i < tokens.Count; i++)
		{
			MokaToken token = tokens[i];
			string value = pinned && theme.Pins.TryGetValue(token.Name, out string? pin) ? pin : token.Produce(context);

			// The provider writes these into a <style> element, where a semicolon or a brace in a value would add
			// declarations or whole rules for the page and an open string or bracket would swallow every token after it.
			// A token left out takes its moka.css fallback.
			if (!CssValues.IsSafe(value) || !CssValues.IsSelfContained(value))
			{
				continue;
			}

			_values[i] = value;
			css.Append(token.Name).Append(": ").Append(value).Append("; ");
		}

		Css = css.ToString();
	}

	internal string Css { get; }

	internal IReadOnlyDictionary<string, string> Map => _map ??= BuildMap();

	// For each token, the positions of the tokens its value reads through var().
	private int[][] References => _references ??=
	[
		.. _values.Select(value => value is null
			? []
			: MokaTokens.References(value).Select(MokaTokens.IndexOf).Where(index => index >= 0).ToArray())
	];

	// What a nested provider's root element declares for theme inside a provider for
	// parent. A custom property is inherited as a finished value, so the root declares a token when
	// its value differs from the parent's or reads, directly or through others, one that does. moka.css's dark rules
	// skip a provider's root, so nothing else reaches it.
	internal static string RootDeclarations(MokaTheme theme, MokaTheme parent)
	{
		MokaThemeTokens child = For(theme);
		MokaThemeTokens around = For(parent);
		if (ReferenceEquals(child, around))
		{
			return "";
		}

		IReadOnlyList<MokaToken> tokens = MokaTokens.All;
		bool[] changed = new bool[tokens.Count];
		for (int i = 0; i < tokens.Count; i++)
		{
			changed[i] = !string.Equals(child._values[i], around._values[i], StringComparison.Ordinal);
		}

		bool grew = true;
		while (grew)
		{
			grew = false;
			for (int i = 0; i < tokens.Count; i++)
			{
				if (!changed[i] && child.References[i].Any(j => changed[j]))
				{
					changed[i] = true;
					grew = true;
				}
			}
		}

		var css = new StringBuilder();
		for (int i = 0; i < tokens.Count; i++)
		{
			if (changed[i] && child._values[i] is { } value)
			{
				css.Append(tokens[i].Name).Append(": ").Append(value).Append("; ");
			}
		}

		return css.ToString();
	}

	internal static MokaThemeTokens For(MokaTheme theme)
	{
		if (ByInstance.TryGetValue(theme, out MokaThemeTokens? known))
		{
			return known;
		}

		if (ByValue.Count >= ByValueCacheSize)
		{
			ByValue.Clear();
		}

		MokaThemeTokens tokens = ByValue.GetOrAdd(theme, static t => new MokaThemeTokens(t));
		ByInstance.AddOrUpdate(theme, tokens);
		return tokens;
	}

	private ReadOnlyDictionary<string, string> BuildMap()
	{
		IReadOnlyList<MokaToken> tokens = MokaTokens.All;
		Dictionary<string, string> map = new(tokens.Count, StringComparer.Ordinal);
		for (int i = 0; i < tokens.Count; i++)
		{
			if (_values[i] is { } value)
			{
				map[tokens[i].Name] = value;
			}
		}

		return map.AsReadOnly();
	}
}
