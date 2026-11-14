using System.Globalization;
using Moka.Red.Data.Table;

namespace Moka.Red.Showcase.Components.Demos.Data.Table;

// What a database does with a MokaTableRequest, done over rows in memory, so a demo can show a server source that
// applies the whole request: the search, the filters and their join, the quick filter, the groups and sorts, the page,
// and under FacetsFromServer the facets and the quick filters' counts, with totals over every page and per group.
internal sealed class DemoQuery<T>(IReadOnlyList<T> rows, IReadOnlyList<DemoColumn<T>> columns)
{
	private readonly Dictionary<string, DemoColumn<T>> _byTitle = columns.ToDictionary(column => column.Title, StringComparer.Ordinal);

	public MokaTableResult<T> Run(MokaTableRequest request)
	{
		List<T> matching = [.. Ordered(Matching(request), request)];
		return new MokaTableResult<T>
		{
			Items = [.. matching.Skip((request.Page - 1) * request.PageSize).Take(request.PageSize)],
			TotalItems = matching.Count,
			Totals = request.Aggregates.Count > 0 ? TotalsOf(matching, request.Aggregates) : null,
			GroupTotals = request.Groups.Count > 0 ? GroupTotalsOf(matching, request) : null,
			Facets = request.Facets.Count > 0 ? FacetsOf(request) : null,
			QuickFilterCounts = request.QuickFilterCounts.Count > 0
				? request.QuickFilterCounts.ToDictionary(quick => quick.Name, quick => Matching(request with { QuickFilter = quick }).Count(), StringComparer.Ordinal)
				: null
		};
	}

	// A facet counts its own column with every filter but its own, so its other values still say what they would give.
	private IEnumerable<T> Matching(MokaTableRequest request, string? leaveOut = null)
	{
		IEnumerable<T> found = rows;
		if (request.Search is { Length: > 0 } search)
		{
			found = found.Where(row => columns.Any(column => column.Searched && column.Text(row).Contains(search, StringComparison.OrdinalIgnoreCase)));
		}

		List<MokaTableFilter> filters = [.. request.Filters.Where(filter => filter.Column != leaveOut && _byTitle.ContainsKey(filter.Column))];
		if (filters.Count > 0)
		{
			found = request.FilterJoin == MokaFilterJoin.Any
				? found.Where(row => filters.Any(filter => Passes(row, filter)))
				: found.Where(row => filters.All(filter => Passes(row, filter)));
		}

		if (request.QuickFilter is { } quick)
		{
			found = found.Where(row => quick.Filters.All(filter => Passes(row, filter)));
		}

		return found;
	}

	// The groups come first, so each group's rows sit together across pages, then the sorts.
	private IEnumerable<T> Ordered(IEnumerable<T> found, MokaTableRequest request)
	{
		IEnumerable<(DemoColumn<T> Column, bool Ascending)> keys = request.Groups
			.Select(title => (Title: title, Ascending: true))
			.Concat(request.Sorts.Select(sort => (Title: sort.Column, Ascending: sort.Direction != MokaSortDirection.Descending)))
			.Where(key => _byTitle.ContainsKey(key.Title))
			.Select(key => (_byTitle[key.Title], key.Ascending));

		IOrderedEnumerable<T>? ordered = null;
		foreach ((DemoColumn<T> column, bool ascending) in keys)
		{
			Func<T, IComparable> key = column.SortKey;
			ordered = ordered is null
				? ascending ? found.OrderBy(key) : found.OrderByDescending(key)
				: ascending ? ordered.ThenBy(key) : ordered.ThenByDescending(key);
		}

		return ordered ?? found;
	}

	private Dictionary<string, double> TotalsOf(List<T> group, IReadOnlyList<MokaTableAggregate> aggregates)
	{
		Dictionary<string, double> totals = new(StringComparer.Ordinal);
		foreach (MokaTableAggregate aggregate in aggregates)
		{
			if (aggregate.Type == MokaAggregateType.Count)
			{
				totals[aggregate.Column] = group.Count;
			}
			else if (_byTitle.TryGetValue(aggregate.Column, out DemoColumn<T>? column) && column.Number is { } number && group.Count > 0)
			{
				totals[aggregate.Column] = aggregate.Type switch
				{
					MokaAggregateType.Sum => group.Sum(number),
					MokaAggregateType.Average => group.Average(number),
					MokaAggregateType.Min => group.Min(number),
					MokaAggregateType.Max => group.Max(number),
					_ => 0
				};
			}
		}

		return totals;
	}

	private List<MokaTableGroupTotals> GroupTotalsOf(IReadOnlyList<T> matching, MokaTableRequest request)
	{
		List<DemoColumn<T>> levels = [.. request.Groups.Where(_byTitle.ContainsKey).Select(title => _byTitle[title])];
		List<MokaTableGroupTotals> groups = [];
		for (int depth = 1; depth <= levels.Count; depth++)
		{
			List<DemoColumn<T>> path = levels[..depth];
			foreach (IGrouping<string, T> group in matching.GroupBy(row => string.Join('\u001f', path.Select(column => column.Text(row))), StringComparer.Ordinal))
			{
				List<T> members = [.. group];
				groups.Add(new MokaTableGroupTotals([.. group.Key.Split('\u001f')], members.Count) { Totals = TotalsOf(members, request.Aggregates) });
			}
		}

		return groups;
	}

	private Dictionary<string, IReadOnlyList<MokaFacetValue>> FacetsOf(MokaTableRequest request)
	{
		Dictionary<string, IReadOnlyList<MokaFacetValue>> facets = new(StringComparer.Ordinal);
		foreach (string title in request.Facets.Where(_byTitle.ContainsKey))
		{
			DemoColumn<T> column = _byTitle[title];
			facets[title] =
			[
				.. Matching(request, leaveOut: title)
					.GroupBy(column.Text, StringComparer.Ordinal)
					.OrderBy(group => group.Key, StringComparer.OrdinalIgnoreCase)
					.Select(group => new MokaFacetValue(group.Key, group.Key, group.Count()))
			];
		}

		return facets;
	}

	private bool Passes(T row, MokaTableFilter filter)
	{
		DemoColumn<T> column = _byTitle[filter.Column];
		string text = column.Text(row);
		string first = filter.Value ?? string.Empty;
		return filter.Operator switch
		{
			MokaFilterOperator.Contains => text.Contains(first, StringComparison.OrdinalIgnoreCase),
			MokaFilterOperator.DoesNotContain => !text.Contains(first, StringComparison.OrdinalIgnoreCase),
			MokaFilterOperator.StartsWith => text.StartsWith(first, StringComparison.OrdinalIgnoreCase),
			MokaFilterOperator.EndsWith => text.EndsWith(first, StringComparison.OrdinalIgnoreCase),
			MokaFilterOperator.IsEmpty => text.Length == 0,
			MokaFilterOperator.IsNotEmpty => text.Length > 0,
			MokaFilterOperator.Is => column.Compare(row, first) == 0,
			MokaFilterOperator.IsNot => column.Compare(row, first) != 0,
			MokaFilterOperator.IsAnyOf => filter.Values.Any(value => column.Compare(row, value) == 0),
			MokaFilterOperator.IsNoneOf => !filter.Values.Any(value => column.Compare(row, value) == 0),
			MokaFilterOperator.GreaterThan => column.Compare(row, first) > 0,
			MokaFilterOperator.AtLeast => column.Compare(row, first) >= 0,
			MokaFilterOperator.LessThan => column.Compare(row, first) < 0,
			MokaFilterOperator.AtMost => column.Compare(row, first) <= 0,
			MokaFilterOperator.Between => filter.Values.Count > 1 && column.Compare(row, filter.Values[0]) >= 0 && column.Compare(row, filter.Values[1]) <= 0,
			_ => true
		};
	}
}

// A column a DemoQuery can search, filter, sort, group and total, by the title the table names it with. Filter values
// arrive as the table writes them: numbers in the invariant culture, dates in ISO 8601, a day alone matching any time
// on it.
internal sealed record DemoColumn<T>(string Title, Func<T, string> Text)
{
	public Func<T, double>? Number { get; init; }

	public Func<T, DateTimeOffset>? Date { get; init; }

	public bool Searched { get; init; }

	public Func<T, IComparable> SortKey => Number is { } number
		? row => number(row)
		: Date is { } date
			? row => date(row)
			: row => Text(row).ToUpperInvariant();

	// Null when the value can't be read as the column's kind, which then matches no test.
	public int? Compare(T row, string value)
	{
		if (Number is { } number)
		{
			return double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out double parsed) ? number(row).CompareTo(parsed) : null;
		}

		if (Date is { } date)
		{
			if (DateOnly.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateOnly day))
			{
				return DateOnly.FromDateTime(date(row).Date).CompareTo(day);
			}

			return DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTimeOffset moment) ? date(row).CompareTo(moment) : null;
		}

		return string.Compare(Text(row), value, StringComparison.OrdinalIgnoreCase);
	}
}
