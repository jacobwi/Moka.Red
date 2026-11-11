using Moka.Red.Core.Enums;

namespace Moka.Red.Data.Chart;

/// <summary>A flow's own color, from its link's <c>Color</c> and <c>ColorValue</c>.</summary>
/// <param name="Color">The theme color.</param>
/// <param name="Value">The CSS color, which wins.</param>
internal readonly record struct SankeyTint(MokaColor? Color, string? Value);

/// <summary>A flow between two node keys, after merging and cycle removal.</summary>
/// <param name="From">Where it leaves.</param>
/// <param name="To">Where it arrives.</param>
/// <param name="Value">How much.</param>
/// <param name="Tint">Its own color, kept through a merge only when every flow merged gave the same one.</param>
internal readonly record struct SankeyFlow(string From, string To, double Value, SankeyTint? Tint = null);

/// <summary>Flows between the same two nodes, added up in the order they first appear.</summary>
internal sealed class SankeyMerge
{
	private readonly Dictionary<(string, string), (double Sum, SankeyTint? Tint, bool Mixed)> _merged = [];
	private readonly List<(string From, string To)> _order = [];

	/// <summary>The pairs in the order they first appeared.</summary>
	public IReadOnlyList<(string From, string To)> Order => _order;

	/// <summary>Adds a flow to its pair.</summary>
	/// <param name="from">Where it leaves.</param>
	/// <param name="to">Where it arrives.</param>
	/// <param name="value">How much.</param>
	/// <param name="tint">Its own color.</param>
	public void Add(string from, string to, double value, SankeyTint? tint)
	{
		(string, string) pair = (from, to);
		if (_merged.TryGetValue(pair, out (double Sum, SankeyTint? Tint, bool Mixed) at))
		{
			_merged[pair] = (at.Sum + value, at.Tint, at.Mixed || at.Tint != tint);
		}
		else
		{
			_merged[pair] = (value, tint, false);
			_order.Add(pair);
		}
	}

	/// <summary>The merged flow of a pair.</summary>
	/// <param name="pair">The pair.</param>
	public SankeyFlow Flow((string From, string To) pair)
	{
		(double sum, SankeyTint? tint, bool mixed) = _merged[pair];
		return new SankeyFlow(pair.From, pair.To, sum, mixed ? null : tint);
	}
}

/// <summary>The graph work behind <see cref="MokaSankey" />: merging, dropping cycles, columns and folding.</summary>
internal static class SankeyLayout
{
	/// <summary>The key of the node a column's smallest nodes fold into.</summary>
	/// <param name="column">The column.</param>
	public static string OtherKey(int column) => "\u0001other:" + column.ToString(System.Globalization.CultureInfo.InvariantCulture);

	/// <summary>
	///     Merges flows between the same two nodes, drops the ones that can't be drawn (no value, a node flowing into
	///     itself) and, walking the nodes in the order they first appear, every flow that closes a cycle.
	/// </summary>
	/// <param name="links">The flows as given.</param>
	/// <returns>The flows left, the node keys in first-appearance order, and how many flows closed a cycle.</returns>
	public static (List<SankeyFlow> Flows, List<string> Keys, int Dropped) Acyclic(IEnumerable<SankeyFlow> links)
	{
		ArgumentNullException.ThrowIfNull(links);

		SankeyMerge merged = new();
		List<string> keys = [];
		HashSet<string> seen = new(StringComparer.Ordinal);
		foreach (SankeyFlow link in links)
		{
			if (!double.IsFinite(link.Value) || link.Value <= 0 || string.Equals(link.From, link.To, StringComparison.Ordinal))
			{
				continue;
			}

			if (seen.Add(link.From))
			{
				keys.Add(link.From);
			}

			if (seen.Add(link.To))
			{
				keys.Add(link.To);
			}

			merged.Add(link.From, link.To, link.Value, link.Tint);
		}

		IReadOnlyList<(string From, string To)> order = merged.Order;
		Dictionary<string, List<int>> outgoing = new(StringComparer.Ordinal);
		for (int i = 0; i < order.Count; i++)
		{
			if (!outgoing.TryGetValue(order[i].From, out List<int>? list))
			{
				outgoing[order[i].From] = list = [];
			}

			list.Add(i);
		}

		bool[] dropped = new bool[order.Count];
		Dictionary<string, int> state = new(StringComparer.Ordinal);
		foreach (string key in keys)
		{
			if (!state.ContainsKey(key))
			{
				Visit(key);
			}
		}

		List<SankeyFlow> flows = [];
		for (int i = 0; i < order.Count; i++)
		{
			if (!dropped[i])
			{
				flows.Add(merged.Flow(order[i]));
			}
		}

		return (flows, keys, dropped.Count(d => d));

		void Visit(string key)
		{
			state[key] = 1;
			if (outgoing.TryGetValue(key, out List<int>? edges))
			{
				foreach (int edge in edges)
				{
					string to = order[edge].To;
					int at = state.GetValueOrDefault(to);
					if (at == 1)
					{
						dropped[edge] = true;
					}
					else if (at == 0)
					{
						Visit(to);
					}
				}
			}

			state[key] = 2;
		}
	}

	/// <summary>
	///     Each node's column: one past the furthest node flowing into it, nodes nothing leaves pushed to the last column,
	///     and a fixed column winning over both.
	/// </summary>
	/// <param name="keys">The node keys.</param>
	/// <param name="flows">The flows, without cycles.</param>
	/// <param name="fixedColumn">A node's own column, if it sets one.</param>
	public static Dictionary<string, int> Columns(IReadOnlyList<string> keys, IReadOnlyList<SankeyFlow> flows,
		Func<string, int?> fixedColumn)
	{
		ArgumentNullException.ThrowIfNull(keys);
		ArgumentNullException.ThrowIfNull(flows);
		ArgumentNullException.ThrowIfNull(fixedColumn);

		Dictionary<string, int> column = keys.ToDictionary(key => key, _ => 0, StringComparer.Ordinal);
		Dictionary<string, int> waiting = keys.ToDictionary(key => key, _ => 0, StringComparer.Ordinal);
		HashSet<string> leaves = new(StringComparer.Ordinal);
		foreach (SankeyFlow flow in flows)
		{
			waiting[flow.To]++;
			leaves.Add(flow.From);
		}

		Queue<string> ready = new(keys.Where(key => waiting[key] == 0));
		ILookup<string, SankeyFlow> outgoing = flows.ToLookup(flow => flow.From, StringComparer.Ordinal);
		while (ready.TryDequeue(out string? key))
		{
			foreach (SankeyFlow flow in outgoing[key])
			{
				column[flow.To] = Math.Max(column[flow.To], column[key] + 1);
				if (--waiting[flow.To] == 0)
				{
					ready.Enqueue(flow.To);
				}
			}
		}

		int last = column.Count == 0 ? 0 : column.Values.Max();
		foreach (string key in keys)
		{
			if (!leaves.Contains(key) && flows.Any(flow => string.Equals(flow.To, key, StringComparison.Ordinal)))
			{
				column[key] = last;
			}

			if (fixedColumn(key) is { } own)
			{
				column[key] = Math.Max(0, own);
			}
		}

		return column;
	}

	/// <summary>
	///     Keeps the largest <paramref name="maxPerColumn" /> minus one nodes of a crowded column and folds the rest into
	///     one node per column, merging the flows that now share their ends.
	/// </summary>
	/// <param name="flows">The flows.</param>
	/// <param name="column">Each node's column.</param>
	/// <param name="value">Each node's value.</param>
	/// <param name="maxPerColumn">The most nodes a column keeps, its fold included.</param>
	/// <returns>The flows after folding, and how many nodes each fold holds, by its key.</returns>
	public static (List<SankeyFlow> Flows, Dictionary<string, int> Folded) Fold(IReadOnlyList<SankeyFlow> flows,
		Dictionary<string, int> column, Func<string, double> value, int maxPerColumn)
	{
		ArgumentNullException.ThrowIfNull(flows);
		ArgumentNullException.ThrowIfNull(column);
		ArgumentNullException.ThrowIfNull(value);

		int keep = Math.Max(2, maxPerColumn);
		Dictionary<string, string> into = new(StringComparer.Ordinal);
		Dictionary<string, int> folded = new(StringComparer.Ordinal);
		List<(string Key, int Column)> folds = [];
		foreach (IGrouping<int, string> group in column.Keys.GroupBy(key => column[key]).ToList())
		{
			List<string> nodes = [.. group.OrderByDescending(value).ThenBy(key => key, StringComparer.Ordinal)];
			if (nodes.Count <= keep)
			{
				continue;
			}

			string other = OtherKey(group.Key);
			folded[other] = nodes.Count - (keep - 1);
			folds.Add((other, group.Key));
			foreach (string key in nodes.Skip(keep - 1))
			{
				into[key] = other;
			}
		}

		if (into.Count == 0)
		{
			return ([.. flows], folded);
		}

		foreach ((string key, int at) in folds)
		{
			column[key] = at;
		}

		SankeyMerge merged = new();
		foreach (SankeyFlow flow in flows)
		{
			merged.Add(into.GetValueOrDefault(flow.From, flow.From), into.GetValueOrDefault(flow.To, flow.To), flow.Value, flow.Tint);
		}

		return ([.. merged.Order.Select(merged.Flow)], folded);
	}
}
