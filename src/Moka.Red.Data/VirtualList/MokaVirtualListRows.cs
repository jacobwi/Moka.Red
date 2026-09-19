using System.Collections;

namespace Moka.Red.Data.VirtualList;

/// <summary>An item of a <see cref="MokaVirtualList{TItem}" /> with its index in the whole list.</summary>
/// <typeparam name="TItem">The item type.</typeparam>
/// <param name="Item">The item.</param>
/// <param name="Index">Zero-based index of the item in <see cref="MokaVirtualList{TItem}.Items" />.</param>
internal readonly record struct MokaVirtualListRow<TItem>(TItem Item, int Index);

/// <summary>
///     A read-only view that pairs each item with its index, for <c>Virtualize</c>, whose item
///     template gets the item alone. The option ids, <c>aria-posinset</c> and the active item all
///     need the index. The view reads the source on every access, so an item list changed in place
///     shows up on the next render as it did before, and nothing is copied.
/// </summary>
/// <typeparam name="TItem">The item type.</typeparam>
internal sealed class MokaVirtualListRows<TItem>(IReadOnlyList<TItem> source)
	: IList<MokaVirtualListRow<TItem>>, IReadOnlyList<MokaVirtualListRow<TItem>>
{
	/// <summary>The list this view reads.</summary>
	public IReadOnlyList<TItem> Source { get; } = source;

	/// <inheritdoc cref="ICollection{T}.Count" />
	public int Count => Source.Count;

	/// <inheritdoc />
	public bool IsReadOnly => true;

	/// <inheritdoc cref="IList{T}.this" />
	public MokaVirtualListRow<TItem> this[int index]
	{
		get => new(Source[index], index);
		set => throw new NotSupportedException();
	}

	/// <inheritdoc />
	public IEnumerator<MokaVirtualListRow<TItem>> GetEnumerator()
	{
		for (int i = 0; i < Source.Count; i++)
		{
			yield return new MokaVirtualListRow<TItem>(Source[i], i);
		}
	}

	IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

	/// <inheritdoc />
	public int IndexOf(MokaVirtualListRow<TItem> item) =>
		item.Index >= 0 && item.Index < Source.Count &&
		EqualityComparer<TItem>.Default.Equals(Source[item.Index], item.Item)
			? item.Index
			: -1;

	/// <inheritdoc />
	public bool Contains(MokaVirtualListRow<TItem> item) => IndexOf(item) >= 0;

	/// <inheritdoc />
	public void CopyTo(MokaVirtualListRow<TItem>[] array, int arrayIndex)
	{
		ArgumentNullException.ThrowIfNull(array);
		for (int i = 0; i < Source.Count; i++)
		{
			array[arrayIndex + i] = new MokaVirtualListRow<TItem>(Source[i], i);
		}
	}

	/// <inheritdoc />
	public void Add(MokaVirtualListRow<TItem> item) => throw new NotSupportedException();

	/// <inheritdoc />
	public void Clear() => throw new NotSupportedException();

	/// <inheritdoc />
	public void Insert(int index, MokaVirtualListRow<TItem> item) => throw new NotSupportedException();

	/// <inheritdoc />
	public bool Remove(MokaVirtualListRow<TItem> item) => throw new NotSupportedException();

	/// <inheritdoc />
	public void RemoveAt(int index) => throw new NotSupportedException();
}
