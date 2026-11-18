using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web.Virtualization;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Data.Table;

// Remote virtualization: with ServerData and Virtualize the rows load window by window as they scroll into view, the way
// Blazor's Virtualize asks an ItemsProvider, with the whole count from the source, a placeholder row for each row still on
// its way, and the request for a window scrolled past cancelled. The table holds only the window last loaded.
//
// Grouped, the windows are of lines, each a group's heading or a row. The first window's group totals say how many rows
// each group of the whole result holds and in what order, so the table works out which rows a window of lines needs and
// asks for those runs, leaving out the rows of folded groups. Group totals that don't cover the whole result in row order
// can't be windowed, and that grouping pages instead.
public partial class MokaTable<TItem>
{
	private Virtualize<RemoteLine>? _virtualize;
	private ItemsProviderDelegate<RemoteLine>? _provideLines;
	private List<RemoteLine> _remoteLines = [];

	// Where the window last loaded starts in the whole result set. Rows carry their place in the whole set, so the keys,
	// the cell actions and the screen reader's row numbers mean the same row wherever the window is.
	private int _rowIndexBase;

	// The window's places for the screen reader: its rows, or under grouped windows its lines.
	private int _windowFirstPlace;
	private int _windowPlaces;

	// Whether a window has come back since the last reload, so an empty answer can show the empty state.
	private bool _remoteLoaded;

	// Whether the last render drew the Virtualize: one taken out by an empty answer or a failure can't be asked again.
	private bool _virtualizeDrawn;

	// Moves with every reload, so rows a run of picks asked for the result before are let go.
	private int _remoteVersion;

	// Grouped windows: whether the windows carry headings, the lines the groups make, and each window row's line.
	private bool _remoteGrouped;
	private List<LineSegment> _segments = [];
	private int _lineCount;
	private Dictionary<int, int>? _lineOfRow;

	// The grouping whose group totals couldn't be windowed. It pages until the grouping changes.
	private List<string>? _refusedGroups;

	private bool RemoteRows => ServerData is not null && Virtualize && !GroupWindowsRefused;

	private bool GroupWindowsRefused =>
		_refusedGroups is { } refused && _groups.Count > 0 && refused.SequenceEqual(_groups, StringComparer.Ordinal);

	// Kept for the component's life, so Virtualize sees the same provider on every render.
	private ItemsProviderDelegate<RemoteLine> LineProvider => _provideLines ??= ProvideLinesAsync;

	private async ValueTask<ItemsProviderResult<RemoteLine>> ProvideLinesAsync(ItemsProviderRequest request)
	{
		if (ServerData is not { } source)
		{
			return new ItemsProviderResult<RemoteLine>([], 0);
		}

		int count = Math.Max(request.Count, 1);
		try
		{
			return _remoteGrouped
				? await ProvideGroupLinesAsync(source, request, count)
				: await ProvideRowLinesAsync(source, request, count);
		}
#pragma warning disable CA1031 // A window scrolled past is cancelled by Virtualize itself; any other failure is shown with Retry.
		catch (Exception ex) when (ex is not OperationCanceledException && !request.CancellationToken.IsCancellationRequested)
#pragma warning restore CA1031
		{
			ShowLoadFailure(ex);
			_remoteLoaded = true;
			StateHasChanged();
			await OnLoadFailed.InvokeAsync(ex);
			return new ItemsProviderResult<RemoteLine>([], 0);
		}
	}

	// The first window after a reload asks for the count, the totals and the facets; the windows after it only for rows.
	private MokaTableRequest WindowRequest(int start, int count, bool rowsOnly, CancellationToken cancellationToken) =>
		BuildRequest(start / Math.Max(count, 1) + 1, count, cancellationToken) with { StartIndex = start, RowsOnly = rowsOnly };

	private bool FirstWindowPending => !_remoteLoaded || _loadError is not null;

	private async Task<ItemsProviderResult<RemoteLine>> ProvideRowLinesAsync(
		Func<MokaTableRequest, Task<MokaTableResult<TItem>>> source, ItemsProviderRequest request, int count)
	{
		bool first = FirstWindowPending;
		MokaTableResult<TItem> result = await source(WindowRequest(request.StartIndex, count, !first, request.CancellationToken));
		request.CancellationToken.ThrowIfCancellationRequested();

		int before = _totalItems;
		_loadError = null;
		_remoteLoaded = true;
		if (first || result.TotalItems is not null)
		{
			_totalKnown = result.TotalItems is not null;
		}

		if (result.TotalItems is { } total)
		{
			_hasMore = false;
			_totalItems = total;
		}
		else if (!_totalKnown)
		{
			// A source that doesn't count grows the scroll a window at a time while full windows come back, and a short
			// one says where the end is.
			_hasMore = result.Items.Count == count;
			int reached = request.StartIndex + result.Items.Count;
			_totalItems = _hasMore ? Math.Max(first ? 0 : before, reached + count) : reached;
		}

		_pageTotal = _totalItems;
		bool valuesChanged = TakeWindowValues(result, first);
		ShowRowWindow(result.Items, request.StartIndex);

		// The count, the totals and the facets are drawn outside Virtualize, which draws the window itself.
		if (first || before != _totalItems || valuesChanged)
		{
			StateHasChanged();
		}

		return new ItemsProviderResult<RemoteLine>(_remoteLines, _totalItems);
	}

	private void ShowRowWindow(IReadOnlyList<TItem> items, int start)
	{
		List<MokaTableRow<TItem>> rows = WindowRows(items, start);
		List<RemoteLine> lines = new(rows.Count);
		foreach (MokaTableRow<TItem> row in rows)
		{
			lines.Add(new RemoteLine(row.Index, null, row));
		}

		_lineOfRow = null;
		_windowFirstPlace = start;
		_windowPlaces = rows.Count;
		TakeWindow(items, rows, lines, start);
	}

	// The window's rows, each carrying its place in the whole result.
	private List<MokaTableRow<TItem>> WindowRows(IReadOnlyList<TItem> items, int start)
	{
		CarrySelection(items);
		List<MokaTableRow<TItem>> rows = BuildRows(items);
		for (int i = 0; i < rows.Count; i++)
		{
			rows[i] = rows[i] with { Index = start + i };
		}

		return rows;
	}

	private void TakeWindow(IReadOnlyList<TItem> items, List<MokaTableRow<TItem>> rows, List<RemoteLine> lines, int start)
	{
		_rowIndexBase = start;
		_displayItems = items;
		_firstRowIndex = start;
		_displayRows = rows;
		_remoteLines = lines;
		_shownUnits = rows.Count;
		_indeterminateState = null;
		PruneRowHandlers(start, rows.Count);
	}

	// ── Grouped windows ──

	private async Task<ItemsProviderResult<RemoteLine>> ProvideGroupLinesAsync(
		Func<MokaTableRequest, Task<MokaTableResult<TItem>>> source, ItemsProviderRequest request, int count)
	{
		Dictionary<int, TItem> known = [];
		bool first = FirstWindowPending;
		if (first)
		{
			MokaTableResult<TItem> head = await source(WindowRequest(0, count, rowsOnly: false, request.CancellationToken));
			request.CancellationToken.ThrowIfCancellationRequested();
			if (!TakeRemoteGroups(head))
			{
				RefuseGroupWindows();
				return new ItemsProviderResult<RemoteLine>([], 0);
			}

			_loadError = null;
			_remoteLoaded = true;
			TakeWindowValues(head, first: true);
			for (int i = 0; i < head.Items.Count; i++)
			{
				known[i] = head.Items[i];
			}
		}
		else
		{
			foreach (MokaTableRow<TItem> row in _displayRows)
			{
				known[row.Index] = row.Item;
			}
		}

		int start = Math.Min(request.StartIndex, _lineCount);
		int end = Math.Min(request.StartIndex + count, _lineCount);
		List<(int First, int Count)> missing = MissingRuns(RowRunsInLines(start, end), known);
		if (missing.Count > 0)
		{
			MokaTableResult<TItem>[] answers = await Task.WhenAll(missing.Select(run =>
				source(WindowRequest(run.First, run.Count, rowsOnly: true, request.CancellationToken))));
			request.CancellationToken.ThrowIfCancellationRequested();
			for (int r = 0; r < missing.Count; r++)
			{
				IReadOnlyList<TItem> items = answers[r].Items;
				for (int i = 0; i < Math.Min(items.Count, missing[r].Count); i++)
				{
					known[missing[r].First + i] = items[i];
				}
			}
		}

		ShowGroupWindow(start, end, known);
		if (first || missing.Count > 0)
		{
			StateHasChanged();
		}

		return new ItemsProviderResult<RemoteLine>(_remoteLines, _lineCount);
	}

	// The lines from start up to end, as far as their rows came: Virtualize draws a placeholder for the rest.
	private void ShowGroupWindow(int start, int end, Dictionary<int, TItem> known)
	{
		List<TItem> items = [];
		List<int> places = [];
		List<(int Line, TableGroupNode<TItem>? Heading, int Row)> picked = [];
		for (int line = start; line < end; line++)
		{
			LineSegment segment = _segments[SegmentAt(line)];
			if (segment.Heading is { } heading)
			{
				picked.Add((line, heading, -1));
				continue;
			}

			int row = segment.FirstRow + line - segment.FirstLine;
			if (!known.TryGetValue(row, out TItem? item))
			{
				break;
			}

			picked.Add((line, null, row));
			items.Add(item);
			places.Add(row);
		}

		CarrySelection(items);
		List<MokaTableRow<TItem>> built = BuildRows(items);
		Dictionary<int, int> lineOfRow = new(built.Count);
		List<RemoteLine> lines = new(picked.Count);
		List<MokaTableRow<TItem>> rows = new(built.Count);
		int next = 0;
		foreach ((int line, TableGroupNode<TItem>? heading, int place) in picked)
		{
			if (heading is not null)
			{
				lines.Add(new RemoteLine(line, heading, default));
				continue;
			}

			MokaTableRow<TItem> row = built[next++] with { Index = place };
			rows.Add(row);
			lineOfRow[place] = line;
			lines.Add(new RemoteLine(line, null, row));
		}

		FillGroupItems(rows);
		_lineOfRow = lineOfRow;
		_windowFirstPlace = start;
		_windowPlaces = lines.Count;
		TakeWindow(items, rows, lines, rows.Count > 0 ? rows[0].Index : 0);
	}

	// A heading's context holds the group's rows the table has: those in the window.
	private void FillGroupItems(List<MokaTableRow<TItem>> rows)
	{
		Clear(_groupTree);
		foreach (MokaTableRow<TItem> row in rows)
		{
			for (TableGroupNode<TItem>? node = LeafOf(row.Index); node is not null; node = node.Parent)
			{
				node.Items.Add(row.Item);
			}
		}

		static void Clear(List<TableGroupNode<TItem>> nodes)
		{
			foreach (TableGroupNode<TItem> node in nodes)
			{
				node.Items.Clear();
				if (node.Children is { } children)
				{
					Clear(children);
				}
			}
		}
	}

	// The groups of the whole result from the group totals the first window brought: every group at the last level, in
	// the order its rows come, adding up to the count. Each group at the last level starts where the one before it ended.
	private bool TakeRemoteGroups(MokaTableResult<TItem> result)
	{
		List<MokaColumn<TItem>> columns = GroupColumns();
		int levels = columns.Count;
		if (levels == 0 || result.TotalItems is not { } total || (result.GroupTotals is null && total > 0))
		{
			return false;
		}

		IReadOnlyList<MokaTableGroupTotals> sent = result.GroupTotals ?? [];

		List<TableGroupNode<TItem>> roots = [];
		Dictionary<string, TableGroupNode<TItem>> byId = new(StringComparer.Ordinal);
		Dictionary<string, IReadOnlyDictionary<string, double>?> outerTotals = new(StringComparer.Ordinal);
		TableGroupNode<TItem>?[] open = new TableGroupNode<TItem>?[levels];
		int row = 0;
		foreach (MokaTableGroupTotals group in sent)
		{
			if (group.Path.Count == 0 || group.Path.Count > levels || group.Count < 0)
			{
				return false;
			}

			if (group.Path.Count < levels)
			{
				outerTotals[string.Join(MokaTableGroup<TItem>.IdSeparator, group.Path)] = group.Totals;
				continue;
			}

			for (int level = 0; level < levels; level++)
			{
				string id = string.Join(MokaTableGroup<TItem>.IdSeparator, group.Path.Take(level + 1));
				if (level < levels - 1 && open[level]?.Id == id)
				{
					continue;
				}

				// A group seen again after another began means the totals don't run in the rows' order.
				if (byId.ContainsKey(id))
				{
					return false;
				}

				TableGroupNode<TItem>? parent = level > 0 ? open[level - 1] : null;
				TableGroupNode<TItem> node = new(group.Path[level], id, level, group.Path.Take(level + 1).ToArray(), [])
				{
					Parent = parent,
					Count = 0,
					FirstRow = row
				};
				if (parent is null)
				{
					roots.Add(node);
				}
				else
				{
					(parent.Children ??= []).Add(node);
				}

				byId[id] = node;
				open[level] = node;
				Array.Clear(open, level + 1, levels - level - 1);
			}

			TableGroupNode<TItem> leaf = open[levels - 1]!;
			leaf.ServerTotals = group.Totals;
			for (TableGroupNode<TItem>? node = leaf; node is not null; node = node.Parent)
			{
				node.Count += group.Count;
			}

			row += group.Count;
		}

		if (row != total)
		{
			return false;
		}

		foreach ((string id, IReadOnlyDictionary<string, double>? totals) in outerTotals)
		{
			if (byId.TryGetValue(id, out TableGroupNode<TItem>? node))
			{
				node.ServerTotals = totals;
			}
		}

		_groupTree = roots;
		_groupTotalTexts.Clear();
		_leaves = Leaves(roots);
		if (!RowsSitInTheirGroups(result.Items, columns))
		{
			return false;
		}

		_totalItems = _pageTotal = total;
		_totalKnown = true;
		_hasMore = false;
		BuildSegments();
		return true;
	}

	// The first window's rows have to belong to the groups their places fall in, or the totals aren't in the rows' order.
	private bool RowsSitInTheirGroups(IReadOnlyList<TItem> items, List<MokaColumn<TItem>> columns)
	{
		for (int i = 0; i < items.Count; i++)
		{
			if (LeafOf(i) is not { } leaf)
			{
				return false;
			}

			for (TableGroupNode<TItem>? node = leaf; node is not null; node = node.Parent)
			{
				if (!string.Equals(columns[node.Level].GroupKeyFor(items[i]), node.Key, StringComparison.Ordinal))
				{
					return false;
				}
			}
		}

		return true;
	}

	// The groups at the last level, in row order, for finding a row's group by its place.
	private List<TableGroupNode<TItem>> _leaves = [];

	private static List<TableGroupNode<TItem>> Leaves(List<TableGroupNode<TItem>> roots)
	{
		List<TableGroupNode<TItem>> leaves = [];
		Add(roots);
		return leaves;

		void Add(List<TableGroupNode<TItem>> nodes)
		{
			foreach (TableGroupNode<TItem> node in nodes)
			{
				if (node.Children is { } children)
				{
					Add(children);
				}
				else
				{
					leaves.Add(node);
				}
			}
		}
	}

	private TableGroupNode<TItem>? LeafOf(int row)
	{
		int low = 0;
		int high = _leaves.Count - 1;
		while (low <= high)
		{
			int mid = (low + high) >>> 1;
			TableGroupNode<TItem> leaf = _leaves[mid];
			if (row < leaf.FirstRow)
			{
				high = mid - 1;
			}
			else if (row >= leaf.FirstRow + leaf.Count)
			{
				low = mid + 1;
			}
			else
			{
				return leaf;
			}
		}

		return null;
	}

	// The lines the groups make: each heading, then the groups or the run of rows under it unless it is folded.
	private void BuildSegments()
	{
		List<LineSegment> segments = [];
		int line = 0;
		foreach (TableGroupNode<TItem> leaf in _leaves)
		{
			leaf.RowsLine = -1;
		}

		Add(_groupTree);
		_segments = segments;
		_lineCount = line;

		void Add(List<TableGroupNode<TItem>> nodes)
		{
			foreach (TableGroupNode<TItem> node in nodes)
			{
				segments.Add(new LineSegment(line++, node, node.FirstRow, 0));
				if (IsGroupCollapsed(node.Id))
				{
					continue;
				}

				if (node.Children is { } children)
				{
					Add(children);
				}
				else if (node.Count > 0)
				{
					node.RowsLine = line;
					segments.Add(new LineSegment(line, null, node.FirstRow, node.Count));
					line += node.Count;
				}
			}
		}
	}

	private int SegmentAt(int line)
	{
		int low = 0;
		int high = _segments.Count - 1;
		while (low < high)
		{
			int mid = (low + high + 1) >>> 1;
			if (_segments[mid].FirstLine <= line)
			{
				low = mid;
			}
			else
			{
				high = mid - 1;
			}
		}

		return low;
	}

	// A row's line, or -1 for a row inside a folded group.
	private int LineOfRow(int row) =>
		LeafOf(row) is { RowsLine: >= 0 } leaf ? leaf.RowsLine + row - leaf.FirstRow : -1;

	// The runs of rows the lines from start up to end hold, one per stretch of rows that follow each other.
	private List<(int First, int Count)> RowRunsInLines(int start, int end)
	{
		List<(int First, int Count)> runs = [];
		if (start >= end || _segments.Count == 0)
		{
			return runs;
		}

		for (int s = SegmentAt(start); s < _segments.Count && _segments[s].FirstLine < end; s++)
		{
			LineSegment segment = _segments[s];
			if (segment.Heading is not null)
			{
				continue;
			}

			int from = Math.Max(start, segment.FirstLine) - segment.FirstLine + segment.FirstRow;
			int to = Math.Min(end, segment.FirstLine + segment.Rows) - segment.FirstLine + segment.FirstRow;
			if (runs.Count > 0 && runs[^1].First + runs[^1].Count == from)
			{
				runs[^1] = (runs[^1].First, runs[^1].Count + to - from);
			}
			else if (to > from)
			{
				runs.Add((from, to - from));
			}
		}

		return runs;
	}

	// The parts of the runs no row is known for yet.
	private static List<(int First, int Count)> MissingRuns(List<(int First, int Count)> runs, Dictionary<int, TItem> known)
	{
		List<(int First, int Count)> missing = [];
		foreach ((int first, int count) in runs)
		{
			int gap = -1;
			for (int row = first; row <= first + count; row++)
			{
				bool absent = row < first + count && !known.ContainsKey(row);
				if (absent && gap < 0)
				{
					gap = row;
				}
				else if (!absent && gap >= 0)
				{
					missing.Add((gap, row - gap));
					gap = -1;
				}
			}
		}

		return missing;
	}

	// The group totals didn't cover the whole result in the rows' order: this grouping pages instead.
	private void RefuseGroupWindows()
	{
		_refusedGroups = [.. _groups];
		_remoteLoaded = false;
		_segments = [];
		_lineCount = 0;
		RunDetached(async () =>
		{
			await LoadDataAsync();
			StateHasChanged();
		});
	}

	// A heading folding or opening changes the lines but not the rows: the window is drawn again from the rows it has.
	private async Task RedrawGroupWindowsAsync()
	{
		BuildSegments();
		if (_virtualize is not null && _virtualizeDrawn && _remoteLoaded && _loadError is null)
		{
			await _virtualize.RefreshDataAsync();
		}
	}

	// ── Both ──

	// Handlers are kept per row index so a render retires none, but a long scroll through a million rows would keep one
	// for every row it passed. Those well outside the window go; a row that comes back gets new ones as it is drawn.
	private void PruneRowHandlers(int start, int count)
	{
		const int Keep = 4096;
		if (_rowHandlers.Count <= Keep)
		{
			return;
		}

		int from = start - count;
		int to = start + 2 * count;
		foreach (int index in _rowHandlers.Keys.Where(index => index < from || index > to).ToList())
		{
			_rowHandlers.Remove(index);
		}
	}

	// Virtualize places every row by one height, the running average it measures, and needs its placeholders to be that
	// height too (the Blazor virtualization docs). A placeholder shorter than a row moved the window it asked for on
	// every render, so each request cancelled the last and none arrived.
	private static string? PlaceholderRowStyle(float size) => new StyleBuilder()
		.AddStyle("height", string.Create(CultureInfo.InvariantCulture, $"{size}px"))
		.Build();

	// A sort, a filter or a search asks for the windows again, from the top.
	private async Task ReloadRemoteRowsAsync()
	{
		_remoteVersion++;
		CancelRemoteRun();
		_remoteLoaded = false;
		_loadError = null;
		_isLoading = false;
		_anchorPlace = -1;
		_remoteGrouped = GroupColumns().Count > 0;
		if (!_remoteGrouped)
		{
			_groupTree = [];
			_segments = [];
			_lineCount = 0;
			_leaves = [];
		}

		ForgetDetailPlaces();
		if (_virtualize is not null && _virtualizeDrawn)
		{
			await _virtualize.RefreshDataAsync();
		}
	}

	/// <summary>One line remote windows draw: a group's heading, or a row.</summary>
	/// <param name="Line">Its place among the lines: the row's place in the whole result when the rows aren't grouped.</param>
	/// <param name="Group">The heading's group, or null for a row.</param>
	/// <param name="Row">The row, for a row.</param>
	private readonly record struct RemoteLine(int Line, TableGroupNode<TItem>? Group, MokaTableRow<TItem> Row);

	/// <summary>A stretch of the lines grouped windows draw: one heading, or the rows of a group at the last level.</summary>
	/// <param name="FirstLine">Its first line.</param>
	/// <param name="Heading">The heading's group, or null for rows.</param>
	/// <param name="FirstRow">The place in the whole result of its first row, or of its group's first row for a heading.</param>
	/// <param name="Rows">How many rows, 0 for a heading.</param>
	private readonly record struct LineSegment(int FirstLine, TableGroupNode<TItem>? Heading, int FirstRow, int Rows);
}
