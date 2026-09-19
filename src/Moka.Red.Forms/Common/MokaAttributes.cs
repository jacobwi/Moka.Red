namespace Moka.Red.Forms.Common;

/// <summary>Helpers for the unmatched attributes a form component passes on to its control.</summary>
internal static class MokaAttributes
{
	/// <summary>
	///     <paramref name="attributes" /> without the one called <paramref name="name" />, for a component
	///     that already renders that attribute from the consumer's value. The control then gets it once,
	///     from one place.
	/// </summary>
	internal static IReadOnlyDictionary<string, object>? Without(
		IReadOnlyDictionary<string, object>? attributes, string name)
	{
		if (attributes is null || !attributes.Keys.Any(key => IsNamed(key, name)))
		{
			return attributes;
		}

		return attributes
			.Where(pair => !IsNamed(pair.Key, name))
			.ToDictionary(pair => pair.Key, pair => pair.Value);
	}

	// HTML attribute names ignore case, and a consumer may write aria-label in any case.
	private static bool IsNamed(string key, string name) => string.Equals(key, name, StringComparison.OrdinalIgnoreCase);
}
