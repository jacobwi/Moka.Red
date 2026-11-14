using System.Globalization;

namespace Moka.Red.Data.Table;

/// <summary>
///     What the table does to client-side rows, step by step: search, filter, sort, page, group and total. Pure functions
///     over the rows and the columns' values, so each step is tested on its own and a later step (a server window, a
///     stream) can take the place of any of them.
/// </summary>
internal static class MokaTablePipeline
{
	/// <summary>Rows where any column's text holds the term, ignoring case. A blank term keeps every row.</summary>
	public static IEnumerable<TItem> Search<TItem>(IEnumerable<TItem> items, IReadOnlyList<ITableField<TItem>> fields, string? term)
	{
		if (string.IsNullOrWhiteSpace(term))
		{
			return items;
		}

		ITableField<TItem>[] searched = fields.Where(static field => field.HasValue).ToArray();
		return items.Where(item => Array.Exists(searched,
			field => field.TextOf(item).Contains(term, StringComparison.OrdinalIgnoreCase)));
	}

	/// <summary>Rows that pass every filter. A filter naming no column with a value filters nothing.</summary>
	public static IEnumerable<TItem> Filter<TItem>(
		IEnumerable<TItem> items, IReadOnlyList<ITableField<TItem>> fields, IReadOnlyList<MokaTableFilter> filters)
	{
		foreach (MokaTableFilter filter in filters)
		{
			ITableField<TItem>? field = Find(fields, filter.Column);
			if (field is null || !field.HasValue)
			{
				continue;
			}

			items = items.Where(item => TableFilterMatch.Matches(filter, field.ValueOf(item), field.TextOf(item)));
		}

		return items;
	}

	/// <summary>
	///     Rows that pass the filters as the join says: every filter, or at least one. Filters naming no column with a value
	///     are left out; when none is left every row passes.
	/// </summary>
	public static IEnumerable<TItem> Filter<TItem>(
		IEnumerable<TItem> items, IReadOnlyList<ITableField<TItem>> fields, IReadOnlyList<MokaTableFilter> filters, MokaFilterJoin join)
	{
		if (join == MokaFilterJoin.All)
		{
			return Filter(items, fields, filters);
		}

		List<(MokaTableFilter Filter, ITableField<TItem> Field)> tests = [];
		foreach (MokaTableFilter filter in filters)
		{
			if (Find(fields, filter.Column) is { HasValue: true } field)
			{
				tests.Add((filter, field));
			}
		}

		return tests.Count == 0
			? items
			: items.Where(item => tests.Exists(test => TableFilterMatch.Matches(test.Filter, test.Field.ValueOf(item), test.Field.TextOf(item))));
	}

	/// <summary>
	///     A column's values with how many rows have each, its text from the first row that has it, ordered by that text.
	///     Missing and empty values are left out; values that differ only in case count as one.
	/// </summary>
	public static List<MokaFacetValue> Facet<TItem>(IEnumerable<TItem> items, ITableField<TItem> field)
	{
		Dictionary<string, (string Text, int Count)> counts = new(StringComparer.OrdinalIgnoreCase);
		foreach (TItem item in items)
		{
			string? value = TableFilterMatch.Invariant(field.ValueOf(item));
			if (string.IsNullOrEmpty(value))
			{
				continue;
			}

			counts[value] = counts.TryGetValue(value, out (string Text, int Count) seen)
				? (seen.Text, seen.Count + 1)
				: (field.TextOf(item), 1);
		}

		return counts
			.Select(static pair => new MokaFacetValue(pair.Key, pair.Value.Text, pair.Value.Count))
			.OrderBy(static facet => facet.Text, StringComparer.OrdinalIgnoreCase)
			.ToList();
	}

	/// <summary>
	///     Rows gathered into groups, one level per key, each level's groups in the order their keys first appear and each
	///     group keeping its rows' order. A group's id is its keys joined by <see cref="MokaTableGroup{TItem}.IdSeparator" />.
	/// </summary>
	public static List<TableGroupNode<TItem>> GroupTree<TItem>(IReadOnlyList<TItem> items, IReadOnlyList<Func<TItem, string>> levels) =>
		GroupLevel(items, levels, 0, null, []);

	private static List<TableGroupNode<TItem>> GroupLevel<TItem>(
		IReadOnlyList<TItem> items, IReadOnlyList<Func<TItem, string>> levels, int level, string? parentId, IReadOnlyList<string> parentPath)
	{
		List<TableGroupNode<TItem>> nodes = [];
		if (level >= levels.Count)
		{
			return nodes;
		}

		foreach ((string key, List<TItem> members) in Group(items, levels[level]))
		{
			string[] path = [.. parentPath, key];
			string id = parentId is null ? key : parentId + MokaTableGroup<TItem>.IdSeparator + key;
			TableGroupNode<TItem> node = new(key, id, level, path, members);
			if (level + 1 < levels.Count)
			{
				node.Children = GroupLevel(members, levels, level + 1, id, path);
				foreach (TableGroupNode<TItem> child in node.Children)
				{
					child.Parent = node;
				}
			}

			nodes.Add(node);
		}

		return nodes;
	}

	/// <summary>The rows of the groups in the order the groups are drawn, so each group's rows stand together.</summary>
	public static List<TItem> InGroupOrder<TItem>(IReadOnlyList<TableGroupNode<TItem>> nodes)
	{
		List<TItem> items = [];
		Append(nodes);
		return items;

		void Append(IReadOnlyList<TableGroupNode<TItem>> level)
		{
			foreach (TableGroupNode<TItem> node in level)
			{
				if (node.Children is { } children)
				{
					Append(children);
				}
				else
				{
					items.AddRange(node.Items);
				}
			}
		}
	}

	/// <summary>
	///     Rows in the order the sorts give, first sort first. Each sort orders by the column's comparer when it has one
	///     and by its value otherwise. A sort naming no such column is skipped.
	/// </summary>
	public static IEnumerable<TItem> Sort<TItem>(
		IEnumerable<TItem> items, IReadOnlyList<ITableField<TItem>> fields, IReadOnlyList<MokaTableSort> sorts)
	{
		IOrderedEnumerable<TItem>? ordered = null;
		foreach (MokaTableSort sort in sorts)
		{
			ITableField<TItem>? field = Find(fields, sort.Column);
			if (field is null || sort.Direction == MokaSortDirection.None)
			{
				continue;
			}

			bool descending = sort.Direction == MokaSortDirection.Descending;
			if (field.Comparer is { } compare)
			{
				var comparer = Comparer<TItem>.Create((a, b) => compare(a, b));
				ordered = (ordered, descending) switch
				{
					(null, false) => items.OrderBy(static x => x, comparer),
					(null, true) => items.OrderByDescending(static x => x, comparer),
					(_, false) => ordered.ThenBy(static x => x, comparer),
					_ => ordered.ThenByDescending(static x => x, comparer)
				};
			}
			else if (field.HasValue)
			{
				Func<TItem, object?> value = field.ValueOf;
				ordered = (ordered, descending) switch
				{
					(null, false) => items.OrderBy(value),
					(null, true) => items.OrderByDescending(value),
					(_, false) => ordered.ThenBy(value),
					_ => ordered.ThenByDescending(value)
				};
			}
		}

		return ordered ?? items;
	}

	/// <summary>How many pages the rows fill, at least one.</summary>
	public static int PageCount(int total, int pageSize) =>
		pageSize > 0 ? Math.Max(1, (int)Math.Ceiling(total / (double)pageSize)) : 1;

	/// <summary>The page brought inside the pages the rows fill.</summary>
	public static int ClampPage(int page, int total, int pageSize) => Math.Clamp(page, 1, PageCount(total, pageSize));

	/// <summary>The rows of one page.</summary>
	public static List<TItem> Page<TItem>(IReadOnlyList<TItem> items, int page, int pageSize)
	{
		int size = Math.Max(pageSize, 0);
		int start = (Math.Max(page, 1) - 1) * size;
		return items.Skip(start).Take(size).ToList();
	}

	/// <summary>
	///     Rows gathered under their keys, in the order each key first appears, each group keeping the order its rows
	///     already had: the sort decides the order and grouping only gathers.
	/// </summary>
	public static List<(string Key, List<TRow> Rows)> Group<TRow>(IEnumerable<TRow> rows, Func<TRow, string> keyOf)
	{
		List<(string Key, List<TRow> Rows)> groups = [];
		Dictionary<string, List<TRow>> byKey = new(StringComparer.Ordinal);
		foreach (TRow row in rows)
		{
			string key = keyOf(row) ?? string.Empty;
			if (!byKey.TryGetValue(key, out List<TRow>? members))
			{
				members = [];
				byKey[key] = members;
				groups.Add((key, members));
			}

			members.Add(row);
		}

		return groups;
	}

	/// <summary>
	///     A total over values, skipping missing ones. A count counts what is there; the others read numbers, and number
	///     text in the invariant culture, and are null when there is none.
	/// </summary>
	public static double? Aggregate(IEnumerable<object?> values, MokaAggregateType type)
	{
		if (type == MokaAggregateType.None)
		{
			return null;
		}

		if (type == MokaAggregateType.Count)
		{
			return values.Count(static value => value is not null);
		}

		List<double> numbers = [];
		foreach (object? value in values)
		{
			if (AsNumber(value) is { } number)
			{
				numbers.Add(number);
			}
		}

		if (numbers.Count == 0)
		{
			return null;
		}

		return type switch
		{
			MokaAggregateType.Sum => numbers.Sum(),
			MokaAggregateType.Average => numbers.Average(),
			MokaAggregateType.Min => numbers.Min(),
			MokaAggregateType.Max => numbers.Max(),
			_ => null
		};
	}

	/// <summary>A value as a number, when it is one.</summary>
	public static double? AsNumber(object? value) => value switch
	{
		null => null,
		double d => d,
		float f => f,
		decimal m => (double)m,
		int i => i,
		long l => l,
		short s => s,
		byte b => b,
		uint u => u,
		ulong ul => ul,
		ushort us => us,
		sbyte sb => sb,
		string text when double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out double parsed) => parsed,
		_ => null
	};

	private static ITableField<TItem>? Find<TItem>(IReadOnlyList<ITableField<TItem>> fields, string column)
	{
		foreach (ITableField<TItem> field in fields)
		{
			if (string.Equals(field.Title, column, StringComparison.Ordinal))
			{
				return field;
			}
		}

		return null;
	}
}
