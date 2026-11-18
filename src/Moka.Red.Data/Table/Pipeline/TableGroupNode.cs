namespace Moka.Red.Data.Table;

/// <summary>One group of rows at one level of a grouped table, with the groups inside it when there is a level below.</summary>
/// <typeparam name="TItem">The row data type.</typeparam>
internal sealed class TableGroupNode<TItem>(string key, string id, int level, IReadOnlyList<string> path, List<TItem> items)
{
	/// <summary>The group's own key.</summary>
	public string Key { get; } = key;

	/// <summary>Its keys and its parents', joined, which names it in the state.</summary>
	public string Id { get; } = id;

	/// <summary>How deep it sits, 0 for the outermost groups.</summary>
	public int Level { get; } = level;

	/// <summary>Its keys and its parents', outermost first.</summary>
	public IReadOnlyList<string> Path { get; } = path;

	/// <summary>Every row of the group, in order: across every page for rows in memory, the page's for a server.</summary>
	public List<TItem> Items { get; } = items;

	/// <summary>The groups inside it, or null at the last level.</summary>
	public List<TableGroupNode<TItem>>? Children { get; set; }

	/// <summary>The group it sits in, or null at the first level.</summary>
	public TableGroupNode<TItem>? Parent { get; set; }

	/// <summary>How many rows the whole group holds, as far as anyone can tell.</summary>
	public int Count { get; set; } = items.Count;

	/// <summary>Totals a server sent for the whole group, by column title.</summary>
	public IReadOnlyDictionary<string, double>? ServerTotals { get; set; }

	/// <summary>Under grouped windows, the place in the whole result of the group's first row.</summary>
	public int FirstRow { get; set; }

	/// <summary>Under grouped windows, the line of a last-level group's first row, or -1 while a heading over it is folded.</summary>
	public int RowsLine { get; set; } = -1;
}
