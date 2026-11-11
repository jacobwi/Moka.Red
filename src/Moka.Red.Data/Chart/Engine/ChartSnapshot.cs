namespace Moka.Red.Data.Chart;

/// <summary>
///     Copies of what a chart last drew from, so a list the parent changed in place still counts as changed. Compared
///     with <see cref="object.Equals(object, object)" /> per item, so NaN, the gap marker, matches NaN.
/// </summary>
internal static class ChartSnapshot
{
	/// <summary>A copy of a list, or null for null.</summary>
	public static T[]? Copy<T>(IReadOnlyList<T>? items) => items is null ? null : [.. items];

	/// <summary>Whether a list holds the same items, in the same order, as a copy taken earlier.</summary>
	public static bool SameItems<T>(IReadOnlyList<T>? items, IReadOnlyList<T>? snapshot)
	{
		if (items is null || snapshot is null)
		{
			return items is null && snapshot is null;
		}

		if (items.Count != snapshot.Count)
		{
			return false;
		}

		EqualityComparer<T> comparer = EqualityComparer<T>.Default;
		for (int i = 0; i < snapshot.Count; i++)
		{
			if (!comparer.Equals(items[i], snapshot[i]))
			{
				return false;
			}
		}

		return true;
	}

	/// <summary>Whether two lists are the same lists, item for item by reference, without looking inside them.</summary>
	public static bool SameReferences<T>(IReadOnlyList<T>? items, IReadOnlyList<T>? previous) where T : class
	{
		if (items is null || previous is null)
		{
			return items is null && previous is null;
		}

		if (items.Count != previous.Count)
		{
			return false;
		}

		for (int i = 0; i < items.Count; i++)
		{
			if (!ReferenceEquals(items[i], previous[i]))
			{
				return false;
			}
		}

		return true;
	}
}
