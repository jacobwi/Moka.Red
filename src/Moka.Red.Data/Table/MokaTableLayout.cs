namespace Moka.Red.Data.Table;

/// <summary>
///     How someone arranged a <see cref="MokaTable{TItem}" />: the order of its columns, the ones they hid and the
///     widths they dragged, each named by column <c>Title</c>. The table raises it and never stores it, so keeping
///     one per person is the app's job. It holds plain lists, so it serializes to JSON as it is. Two layouts are equal
///     when they say the same thing, whichever instances hold it.
/// </summary>
public sealed record MokaTableLayout
{
	/// <summary>
	///     The titles of the columns that can move, in display order. A title no column has is ignored, and a column
	///     missing from the list goes after the ones that are in it.
	/// </summary>
	public IReadOnlyList<string> Order { get; init; } = [];

	/// <summary>The titles of the hidden columns. Any column left out of this list is shown.</summary>
	public IReadOnlyList<string> Hidden { get; init; } = [];

	/// <summary>Column widths in pixels, by title. A column that isn't listed keeps its own width.</summary>
	public IReadOnlyDictionary<string, double> Widths { get; init; } = new Dictionary<string, double>(StringComparer.Ordinal);

	/// <summary>Says nothing, so the columns are as their parameters set them.</summary>
	internal bool IsEmpty => Order.Count == 0 && Hidden.Count == 0 && Widths.Count == 0;

	/// <inheritdoc />
	public bool Equals(MokaTableLayout? other) =>
		other is not null
		&& Order.SequenceEqual(other.Order, StringComparer.Ordinal)
		&& Hidden.ToHashSet(StringComparer.Ordinal).SetEquals(other.Hidden)
		&& Widths.Count == other.Widths.Count
		&& Widths.All(width => other.Widths.TryGetValue(width.Key, out double value) && value.Equals(width.Value));

	/// <inheritdoc />
	public override int GetHashCode() => HashCode.Combine(Order.Count, Hidden.Count, Widths.Count);
}
