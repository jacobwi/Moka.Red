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

	/// <summary>
	///     The <c>aria-describedby</c> for a control that points at elements of its own as well: whatever the
	///     consumer passed in <paramref name="attributes" />, then each of <paramref name="ids" /> that is set.
	///     <see langword="null" /> when there is nothing to name. The control then takes its other attributes
	///     through <see cref="WithoutDescribedBy" />, so it gets the attribute once.
	/// </summary>
	internal static string? DescribedBy(IReadOnlyDictionary<string, object>? attributes, params string?[] ids)
	{
		object? consumers = attributes?.FirstOrDefault(pair => IsNamed(pair.Key, "aria-describedby")).Value;
		IEnumerable<string> parts = ids.OfType<string>();
		if (consumers is string own && !string.IsNullOrWhiteSpace(own))
		{
			parts = parts.Prepend(own);
		}

		string joined = string.Join(' ', parts);
		return joined.Length == 0 ? null : joined;
	}

	/// <summary><paramref name="attributes" /> without the consumer's <c>aria-describedby</c>, which <see cref="DescribedBy" /> carries.</summary>
	internal static IReadOnlyDictionary<string, object>? WithoutDescribedBy(IReadOnlyDictionary<string, object>? attributes) =>
		Without(attributes, "aria-describedby");

	// HTML attribute names ignore case, and a consumer may write aria-label in any case.
	private static bool IsNamed(string key, string name) => string.Equals(key, name, StringComparison.OrdinalIgnoreCase);
}
