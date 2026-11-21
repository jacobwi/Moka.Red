namespace Moka.Red.Layout.Card;

/// <summary>
///     Splits the unmatched attributes of a card whose one action, a button or a link, stretches over it
///     (<see cref="MokaCard" />, <c>MokaGlassCard</c>). What names the card, or describes its link, belongs on the action,
///     since the card itself is a plain element.
/// </summary>
internal static class CardActionAttributes
{
	private static readonly HashSet<string> LinkAttributeNames =
		new(["rel", "download"], StringComparer.OrdinalIgnoreCase);

	/// <summary>The attributes the card's own element keeps.</summary>
	internal static IReadOnlyDictionary<string, object>? ForRoot(
		IReadOnlyDictionary<string, object>? attributes, bool hasAction, bool isLink) =>
		hasAction ? Where(attributes, name => !GoesToAction(name, isLink)) : attributes;

	/// <summary>
	///     The attributes the action takes. With <paramref name="newTabText" /> set, an <c>aria-label</c> among them ends
	///     in it after a comma, since the label replaces the link's content, words for a new tab included, as its name.
	/// </summary>
	internal static Dictionary<string, object>? ForAction(
		IReadOnlyDictionary<string, object>? attributes, bool isLink, string? newTabText)
	{
		Dictionary<string, object>? kept = Where(attributes, name => GoesToAction(name, isLink));
		if (newTabText is null || kept is null)
		{
			return kept;
		}

		string? labelKey = kept.Keys.FirstOrDefault(IsAriaLabel);
		if (labelKey is not null && kept[labelKey] is string label)
		{
			kept[labelKey] = $"{label}, {newTabText}";
		}

		return kept;
	}

	/// <summary>Whether the app gave the card an <c>aria-label</c>, which then names its action.</summary>
	internal static bool HasAriaLabel(IReadOnlyDictionary<string, object>? attributes) =>
		attributes?.Keys.Any(IsAriaLabel) == true;

	private static bool GoesToAction(string name, bool isLink) =>
		IsAriaLabel(name) || (isLink && LinkAttributeNames.Contains(name));

	private static bool IsAriaLabel(string name) => name.Equals("aria-label", StringComparison.OrdinalIgnoreCase);

	private static Dictionary<string, object>? Where(IReadOnlyDictionary<string, object>? attributes, Func<string, bool> keep)
	{
		if (attributes is null)
		{
			return null;
		}

		Dictionary<string, object> kept = attributes
			.Where(attribute => keep(attribute.Key))
			.ToDictionary(attribute => attribute.Key, attribute => attribute.Value);
		return kept.Count > 0 ? kept : null;
	}
}
