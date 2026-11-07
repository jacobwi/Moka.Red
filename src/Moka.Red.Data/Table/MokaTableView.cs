namespace Moka.Red.Data.Table;

/// <summary>
///     A named set of a page's filters, drawn as a chip over a <see cref="MokaTable{TItem}" />. The table only shows
///     views and reports what happens to them: applying the filters and storing the views is the app's job. Two views
///     are equal when their names and filters match, whichever dictionaries hold them.
/// </summary>
/// <param name="Name">The name on the chip.</param>
/// <param name="Filters">The filters the view puts in force, by whatever keys the page uses.</param>
public sealed record MokaTableView(string Name, IReadOnlyDictionary<string, string> Filters)
{
	/// <inheritdoc />
	public bool Equals(MokaTableView? other) =>
		other is not null && string.Equals(Name, other.Name, StringComparison.Ordinal) && HasSameFilters(other);

	/// <inheritdoc />
	public override int GetHashCode() => HashCode.Combine(StringComparer.Ordinal.GetHashCode(Name), Filters.Count);

	internal bool HasSameFilters(MokaTableView other) =>
		Filters.Count == other.Filters.Count
		&& Filters.All(filter => other.Filters.TryGetValue(filter.Key, out string? value)
		                         && string.Equals(value, filter.Value, StringComparison.Ordinal));
}
