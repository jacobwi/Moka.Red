namespace Moka.Red.Feedback.Internal;

/// <summary>
///     <c>@key</c> values for a list whose identities come from the consumer. Blazor throws on the
///     render after two siblings share a key, so a key several items share is dropped for all of
///     them: those items render unkeyed and are matched by position, and unique keys stay.
/// </summary>
internal static class MokaUniqueKeys
{
	/// <summary>One key per item, in order, with every repeated key replaced by null.</summary>
	/// <param name="items">The items, in the order they render.</param>
	/// <param name="keyOf">The item's key. Compared with the default equality, as Blazor does.</param>
	internal static object?[] For<T>(IReadOnlyList<T> items, Func<T, object?> keyOf)
	{
		var keys = new object?[items.Count];
		var seen = new HashSet<object>();
		HashSet<object>? repeated = null;
		for (int i = 0; i < items.Count; i++)
		{
			object? key = keyOf(items[i]);
			keys[i] = key;
			if (key is not null && !seen.Add(key))
			{
				(repeated ??= []).Add(key);
			}
		}

		if (repeated is not null)
		{
			for (int i = 0; i < keys.Length; i++)
			{
				if (keys[i] is { } key && repeated.Contains(key))
				{
					keys[i] = null;
				}
			}
		}

		return keys;
	}
}
