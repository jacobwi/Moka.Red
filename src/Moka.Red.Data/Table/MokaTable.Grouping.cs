using System.Globalization;
using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Data.Table;

// Grouping: rows gathered under headings, one level per grouped column, each heading with its whole group's count and
// totals. Rows in memory are grouped across every page and the pager counts headings and rows alike, as lines; a server
// orders its rows by the groups and sends whole groups' totals.
public partial class MokaTable<TItem>
{
	private List<TableGroupNode<TItem>> _groupTree = [];
	private List<TableLine> _displayLines = [];
	private readonly Dictionary<(TableGroupNode<TItem> Node, MokaColumn<TItem> Col), string> _groupTotalTexts = [];
	private IReadOnlyList<MokaTableGroupTotals>? _serverGroupTotals;

	private List<MokaColumn<TItem>> GroupColumns()
	{
		List<MokaColumn<TItem>> columns = [];
		foreach (string title in _groups)
		{
			if (_columns.Find(col => CanGroupBy(col) && col.Title == title) is { } col && !columns.Contains(col))
			{
				columns.Add(col);
			}
		}

		return columns;
	}

	private bool IsGrouped => _groupTree.Count > 0;

	private bool HasNothingToShow => RemoteRows
		? _remoteLoaded && _totalItems == 0
		: _displayRows.Count == 0 && _displayLines.Count == 0;

	// ── Building the groups ──

	private void BuildTree(IReadOnlyList<TItem> items)
	{
		_groupTotalTexts.Clear();
		List<MokaColumn<TItem>> columns = GroupColumns();
		if (columns.Count == 0)
		{
			_groupTree = [];
			return;
		}

		Func<TItem, string>[] levels = columns.Select(static col => (Func<TItem, string>)col.GroupKeyFor).ToArray();
		_groupTree = MokaTablePipeline.GroupTree(items, levels);
	}

	// Every line of the groups: each heading, then the groups or rows under it unless it is folded.
	private List<FullLine> FlattenTree()
	{
		List<FullLine> lines = [];
		Add(_groupTree);
		return lines;

		void Add(List<TableGroupNode<TItem>> nodes)
		{
			foreach (TableGroupNode<TItem> node in nodes)
			{
				lines.Add(new FullLine(node, default!, false));
				if (IsGroupCollapsed(node.Id))
				{
					continue;
				}

				if (node.Children is { } children)
				{
					Add(children);
				}
				else
				{
					foreach (TItem item in node.Items)
					{
						lines.Add(new FullLine(node, item, true));
					}
				}
			}
		}
	}

	// Some of the lines, drawn: a page that starts inside a group draws that group's headings first, so a row is never
	// shown without the groups it belongs to.
	private void ShowLines(List<FullLine> all, int first, int count)
	{
		List<FullLine> page = all.Skip(first).Take(count).ToList();
		if (page.Count > 0)
		{
			List<FullLine> above = [];
			for (TableGroupNode<TItem>? node = page[0].IsRow ? page[0].Node : page[0].Node.Parent; node is not null; node = node.Parent)
			{
				above.Insert(0, new FullLine(node, default!, false));
			}

			page.InsertRange(0, above);
		}

		List<TItem> items = [];
		foreach (FullLine line in page)
		{
			if (line.IsRow)
			{
				items.Add(line.Item);
			}
		}

		SetDisplayItems(items, first);
		_shownUnits = Math.Min(first + count, all.Count) - first;
		_ariaFirstLine = Math.Max(0, first - (page.Count - Math.Min(count, Math.Max(all.Count - first, 0))));

		List<TableLine> lines = new(page.Count);
		int row = 0;
		foreach (FullLine line in page)
		{
			lines.Add(line.IsRow ? new TableLine(null, row++) : new TableLine(line.Node, -1));
		}

		_displayLines = lines;
	}

	// A server's page, grouped as it came: the server orders the rows by the groups, and whole groups' counts and totals
	// come from what it sent.
	private void GroupServerRows()
	{
		BuildTree(_displayItems);
		if (!IsGrouped)
		{
			_displayLines = [];
			return;
		}

		if (_serverGroupTotals is { } totals)
		{
			Dictionary<string, MokaTableGroupTotals> byId = new(StringComparer.Ordinal);
			foreach (MokaTableGroupTotals group in totals)
			{
				byId.TryAdd(string.Join(MokaTableGroup<TItem>.IdSeparator, group.Path), group);
			}

			Visit(_groupTree);

			void Visit(List<TableGroupNode<TItem>> nodes)
			{
				foreach (TableGroupNode<TItem> node in nodes)
				{
					if (byId.TryGetValue(node.Id, out MokaTableGroupTotals? sent))
					{
						node.Count = sent.Count;
						node.ServerTotals = sent.Totals;
					}

					if (node.Children is { } children)
					{
						Visit(children);
					}
				}
			}
		}

		// The rows in group order, so the rows' positions run down the page as the keys move through them. A folded
		// group's rows keep their positions and are only left out of the lines.
		SetDisplayItems(MokaTablePipeline.InGroupOrder(_groupTree), _firstRowIndex);
		_shownUnits = _displayRows.Count;
		List<TableLine> lines = [];
		int position = 0;
		AddLines(_groupTree, false);
		_displayLines = lines;

		void AddLines(List<TableGroupNode<TItem>> nodes, bool hidden)
		{
			foreach (TableGroupNode<TItem> node in nodes)
			{
				bool folded = hidden || IsGroupCollapsed(node.Id);
				if (!hidden)
				{
					lines.Add(new TableLine(node, -1));
				}

				if (node.Children is { } children)
				{
					AddLines(children, folded);
					continue;
				}

				for (int i = 0; i < node.Items.Count; i++, position++)
				{
					if (!folded)
					{
						lines.Add(new TableLine(null, position));
					}
				}
			}
		}
	}

	// After a heading folds or opens: rows in memory page their lines again; a server's page is drawn again as it is, and
	// remote windows draw their lines again from the rows they hold.
	private async Task RedrawGroupsAsync()
	{
		if (ServerData is null)
		{
			PageClientRows();
		}
		else if (RemoteRows)
		{
			await RedrawGroupWindowsAsync();
		}
		else
		{
			GroupServerRows();
		}
	}

	// ── Folding ──

	private async Task ToggleGroupAsync(string id)
	{
		if (!GroupsCollapsible)
		{
			return;
		}

		bool collapsed = _collapsedGroups.Add(id);
		if (!collapsed)
		{
			_collapsedGroups.Remove(id);
		}

		await RedrawGroupsAsync();
		StateHasChanged();

		if (OnGroupToggled.HasDelegate)
		{
			await OnGroupToggled.InvokeAsync((id, collapsed));
		}

		await NotifyStateChangedAsync();
	}

	/// <summary>Opens every group. Raises <see cref="StateChanged" />.</summary>
	/// <returns>A task that completes once the table has drawn the change.</returns>
	public async Task ExpandAllGroupsAsync()
	{
		if (_collapsedGroups.Count == 0)
		{
			return;
		}

		_collapsedGroups.Clear();
		await RedrawGroupsAsync();
		StateHasChanged();
		await NotifyStateChangedAsync();
	}

	/// <summary>Folds every group, at every level. Raises <see cref="StateChanged" />.</summary>
	/// <returns>A task that completes once the table has drawn the change.</returns>
	public async Task CollapseAllGroupsAsync()
	{
		if (!GroupsCollapsible)
		{
			return;
		}

		Fold(_groupTree);
		await RedrawGroupsAsync();
		StateHasChanged();
		await NotifyStateChangedAsync();

		void Fold(List<TableGroupNode<TItem>> nodes)
		{
			foreach (TableGroupNode<TItem> node in nodes)
			{
				_collapsedGroups.Add(node.Id);
				if (node.Children is { } children)
				{
					Fold(children);
				}
			}
		}
	}

	// Grouping by a column adds a level inside the others; grouping by it again takes it away.
	internal async Task ToggleGroupByAsync(MokaColumn<TItem> col)
	{
		if (!CanGroupBy(col) || col.Title is not { } title)
		{
			return;
		}

		if (!_groups.Remove(title))
		{
			_groups.Add(title);
		}

		_groupsFromParameter = false;
		ResetPaging();
		await LoadDataAsync();
		StateHasChanged();
		await NotifyStateChangedAsync();
	}

	private async Task StopGroupingAsync(string title)
	{
		if (!_groups.Remove(title))
		{
			return;
		}

		_groupsFromParameter = false;
		ResetPaging();
		_focusAfterRender = FocusGroupBar;
		await LoadDataAsync();
		await NotifyStateChangedAsync();
	}

	private static readonly string[] FocusGroupBar = [".moka-table-group-bar button", ".moka-table-menu-btn"];

	private bool ShowGroupBar => ColumnMenu && IsGrouped;

	private string GroupTitles => string.Join(" › ", GroupColumns().Select(static col => col.Title));

	// ── A heading's row ──

	private MokaTableGroup<TItem> GroupContext(TableGroupNode<TItem> node) => new(
		node.Key,
		node.Count,
		IsGroupCollapsed(node.Id),
		node.Items)
	{
		Level = node.Level,
		Path = node.Path,
		Totals = GroupTotalsFor(node)
	};

	private Dictionary<string, string> GroupTotalsFor(TableGroupNode<TItem> node)
	{
		Dictionary<string, string> totals = new(StringComparer.Ordinal);
		foreach (MokaColumn<TItem> col in _columns)
		{
			if (col is { Aggregate: not MokaAggregateType.None, Title: { } title })
			{
				totals[title] = GroupTotalText(node, col);
			}
		}

		return totals;
	}

	// A whole group's total: what the server sent for it, else the group's rows the table has.
	private string GroupTotalText(TableGroupNode<TItem> node, MokaColumn<TItem> col)
	{
		if (_groupTotalTexts.TryGetValue((node, col), out string? text))
		{
			return text;
		}

		// Under grouped windows a group's rows the table has are the window's, so only the server's total shows.
		double? total = col.Title is not null && node.ServerTotals is { } sent && sent.TryGetValue(col.Title, out double value)
			? value
			: col.HasValue && !RemoteRows
				? MokaTablePipeline.Aggregate(node.Items.Select(col.ValueOf), col.Aggregate)
				: null;
		text = FormatAggregate(col, total);
		_groupTotalTexts[(node, col)] = text;
		return text;
	}

	// The cells of a heading's row once a shown column totals anything: one cell for the heading over the control cells and
	// the columns before the first total, then a cell per column. With a total in the first column the heading takes that
	// column's cell, so its total isn't drawn there.
	private int GroupLeadSpan(List<(MokaColumn<TItem> Col, int ColIdx)> columns)
	{
		int firstTotal = columns.FindIndex(static entry => entry.Col.Aggregate != MokaAggregateType.None);
		return firstTotal < 0 ? -1 : Math.Max(1, LeadingControlCells + firstTotal) - LeadingControlCells;
	}

	private static string GroupRowClass(TableGroupNode<TItem> node) => new CssBuilder("moka-table-group-row")
		.AddClass("moka-table-group-row--nested", node.Level > 0)
		.Build();

	private static string? GroupCellStyle(TableGroupNode<TItem> node) => node.Level == 0
		? null
		: new StyleBuilder().AddStyle("--_group-level", node.Level.ToString(CultureInfo.InvariantCulture)).Build();


	/// <summary>One line the body draws: a group's heading, or the row at a position in the rows drawn.</summary>
	/// <param name="Group">The heading's group, or null for a row.</param>
	/// <param name="Row">The row's position among the rows drawn, or -1 for a heading.</param>
	private readonly record struct TableLine(TableGroupNode<TItem>? Group, int Row);

	/// <summary>One line of every group before paging: a heading, or a row under its innermost group.</summary>
	/// <param name="Node">The heading's group, or the row's innermost group.</param>
	/// <param name="Item">The row, for a row.</param>
	/// <param name="IsRow">Whether it is a row.</param>
	private readonly record struct FullLine(TableGroupNode<TItem> Node, TItem Item, bool IsRow);
}
