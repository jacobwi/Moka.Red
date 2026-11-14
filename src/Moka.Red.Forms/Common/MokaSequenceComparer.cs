namespace Moka.Red.Forms.Common;

/// <summary>
///     Treats two lists with the same items in the same order as the same value, and a null list as an empty one, for
///     an input whose value is a list: a parent that builds a new list with the same items on every render passes the
///     same value.
/// </summary>
/// <typeparam name="T">The type of the items.</typeparam>
internal sealed class MokaSequenceComparer<T> : IEqualityComparer<IEnumerable<T>?>
{
	private readonly IEqualityComparer<T> _items;

	/// <summary>Compares items with <paramref name="items" />, or the type's own equality.</summary>
	/// <param name="items">Decides whether two items are the same.</param>
	public MokaSequenceComparer(IEqualityComparer<T>? items = null) => _items = items ?? EqualityComparer<T>.Default;

	/// <summary>Compares items with the type's own equality.</summary>
	public static MokaSequenceComparer<T> Default { get; } = new();

	/// <inheritdoc />
	public bool Equals(IEnumerable<T>? x, IEnumerable<T>? y) =>
		ReferenceEquals(x, y) || (x ?? []).SequenceEqual(y ?? [], _items);

	/// <inheritdoc />
	public int GetHashCode(IEnumerable<T>? obj)
	{
		HashCode hash = new();
		foreach (T item in obj ?? [])
		{
			hash.Add(item, _items);
		}

		return hash.ToHashCode();
	}
}
