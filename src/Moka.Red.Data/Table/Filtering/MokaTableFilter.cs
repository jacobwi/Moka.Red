using System.Text.Json.Serialization;

namespace Moka.Red.Data.Table;

/// <summary>
///     One filter on a column of a <see cref="MokaTable{TItem}" />. Values are text in the invariant culture: numbers as
///     <c>12.5</c>, dates in ISO 8601 (<c>2026-11-14</c>, or a full time with its offset), yes or no as <c>true</c> or
///     <c>false</c>. The table reads them against each column's values; a server-side data source gets them in
///     <see cref="MokaTableRequest.Filters" /> and applies them itself.
/// </summary>
public sealed record MokaTableFilter
{
	/// <summary>A filter with any number of values.</summary>
	/// <param name="column">The column's <c>Title</c>.</param>
	/// <param name="operator">How the column's value is compared.</param>
	/// <param name="values">What it is compared with: none for the empty tests, two for between, any number for one of.</param>
	[JsonConstructor]
	public MokaTableFilter(string column, MokaFilterOperator @operator, IReadOnlyList<string> values)
	{
		Column = column;
		Operator = @operator;
		Values = values;
	}

	/// <summary>A filter with one value.</summary>
	/// <param name="column">The column's <c>Title</c>.</param>
	/// <param name="operator">How the column's value is compared.</param>
	/// <param name="value">What it is compared with.</param>
	public MokaTableFilter(string column, MokaFilterOperator @operator, string value) : this(column, @operator, [value])
	{
	}

	/// <summary>The column's <c>Title</c>.</summary>
	public string Column { get; init; }

	/// <summary>How the column's value is compared.</summary>
	public MokaFilterOperator Operator { get; init; }

	/// <summary>What the column's value is compared with.</summary>
	public IReadOnlyList<string> Values { get; init; }

	/// <summary>The first value, or null when there is none.</summary>
	[JsonIgnore]
	public string? Value => Values.Count > 0 ? Values[0] : null;

	/// <inheritdoc />
	public bool Equals(MokaTableFilter? other) =>
		other is not null
		&& string.Equals(Column, other.Column, StringComparison.Ordinal)
		&& Operator == other.Operator
		&& Values.SequenceEqual(other.Values, StringComparer.Ordinal);

	/// <inheritdoc />
	public override int GetHashCode() => HashCode.Combine(StringComparer.Ordinal.GetHashCode(Column), Operator, Values.Count);
}
