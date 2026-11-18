using System.Globalization;

namespace Moka.Red.Data.Table;

// The WAI-ARIA grid pattern: the table is a grid, or a treegrid while its rows are grouped, and says how many rows and
// columns the whole result set has and where each drawn one stands in it, so a screen reader counts right under paging,
// virtualization and column virtualization. Without grouping or open details every number is worked out from the row's
// position, so a table of a million rows pays nothing per render for them.
public partial class MokaTable<TItem>
{
	private static readonly string[] IndexTexts = Enumerable.Range(0, 1024)
		.Select(static i => i.ToString(CultureInfo.InvariantCulture))
		.ToArray();

	private int _ariaHeaderRows;
	private int _ariaBodyStart;
	private int _ariaRowCount;

	// Where each drawn row and heading stands, worked out once per change while groups or details shift the count.
	private int[]? _ariaRowIndexes;
	private int[]? _ariaLineIndexes;
	private object? _ariaRowsFor;
	private object? _ariaLinesFor;
	private int _ariaExpandFor = -1;
	private int _ariaStartFor = -1;
	private int _ariaBodyEnd;

	private int _expandVersion;

	// The first line of a grouped page among every line, as the pager counts them.
	private int _ariaFirstLine;

	// Under remote virtualization only the window's rows are known, so an open detail is kept by its row's place in the
	// whole result set, noted as windows show it, and each row's number counts the open details above it.
	private readonly HashSet<int> _detailPlaces = [];
	private object? _detailPlacesFor;
	private int _detailPlacesVersion = -1;

	/// <summary>A number as invariant text, from a cache for the small ones every row writes.</summary>
	internal static string IndexText(int value) =>
		(uint)value < (uint)IndexTexts.Length ? IndexTexts[value] : value.ToString(CultureInfo.InvariantCulture);

	private string GridRole => IsGrouped ? "treegrid" : "grid";

	private string? GridMultiselectable => (Selectable && !SingleSelect) || CellRanges ? "true" : null;

	// The row numbers are hidden from screen readers, so they don't count as a column.
	private int AriaLeadingCells => (ExpandAtStart ? 1 : 0) + (Selectable ? 1 : 0) + (RowReorderable ? 1 : 0);

	private int AriaColumnCount => AriaLeadingCells + VisibleColumnCount + TrailingControlCells;

	private int AriaColumnOf(int position) => AriaLeadingCells + position + 1;

	private int SelectColumnIndex => (ExpandAtStart ? 1 : 0) + 1;

	private bool NewRowAtTop => _edit is { IsNew: true } && AddRowPosition == MokaTableAddPosition.Top;

	private bool NewRowAtBottom => _edit is { IsNew: true } && AddRowPosition == MokaTableAddPosition.Bottom;

	private int ShownPinnedTop => PinnedTop is { Count: > 0 } top && !Loading && !FailureReplacesRows ? top.Count : 0;

	private int ShownPinnedBottom => PinnedBottom is { Count: > 0 } bottom && !Loading && !FailureReplacesRows ? bottom.Count : 0;

	// The stream's row under the rows: more on the way, with Stop.
	private bool StreamMoreShown => StreamMoreRow && !HasNothingToShow && !_isLoading;

	// What sits under the rows: the stream's row, or the failure that stopped it.
	private int BelowRowsCount => FailureBelowRows || StreamMoreShown ? 1 : 0;

	// Called as the render starts, with what it draws above the rows.
	private void PrepareAria(bool groupHeadings, bool filterRow)
	{
		_ariaHeaderRows = DrawHeader ? 1 + (groupHeadings ? 1 : 0) + (filterRow ? 1 : 0) : 0;
		int firstUnit = RemoteRows ? 0 : IsGrouped && ServerData is null ? _ariaFirstLine : _firstRowIndex;
		_ariaBodyStart = _ariaHeaderRows + ShownPinnedTop + (NewRowAtTop ? 1 : 0) + firstUnit;

		bool anyOpen = Expandable && (_expandedKeys.Count > 0 || _expandedItems.Count > 0);
		NoteDetailPlaces(anyOpen && RemoteRows);

		bool details = anyOpen && !RemoteRows;
		if ((IsGrouped && !RemoteRows) || details)
		{
			IndexLines(details);
		}
		else
		{
			_ariaRowIndexes = null;
			_ariaLineIndexes = null;
			_ariaBodyEnd = RemoteRows
				? _ariaBodyStart + RemotePlaces + _detailPlaces.Count
				: _ariaBodyStart + _displayRows.Count;
		}

		int drawnBelow = BelowRowsCount + (NewRowAtBottom ? 1 : 0) + ShownPinnedBottom;
		if (HasNothingToShow || _isLoading || Loading)
		{
			_ariaRowCount = _ariaHeaderRows + ShownPinnedTop + (NewRowAtTop ? 1 : 0) + (StateRowShown ? 1 : 0) + drawnBelow;
			return;
		}

		bool counted = _totalKnown && !_streaming;
		int total = RemoteRows ? RemotePlaces : IsGrouped && ServerData is null ? _pageTotal : _totalItems;
		// What the page draws beyond the units the pager counts: open details, and headings a server's rows or a page of
		// lines repeat.
		int extra = RemoteRows ? _detailPlaces.Count : _ariaBodyEnd - _ariaBodyStart - _shownUnits;
		_ariaRowCount = counted
			? Math.Max(_ariaBodyEnd, _ariaHeaderRows + ShownPinnedTop + (NewRowAtTop ? 1 : 0) + total + Math.Max(extra, 0)) + drawnBelow
			: -1;
	}

	// Each drawn line's place, a heading and an open detail counting as rows. Kept until the rows, the lines, the open
	// details or the start change.
	private void IndexLines(bool details)
	{
		if (ReferenceEquals(_ariaRowsFor, _displayRows) && ReferenceEquals(_ariaLinesFor, _displayLines)
		                                               && _ariaExpandFor == _expandVersion && _ariaStartFor == _ariaBodyStart)
		{
			return;
		}

		_ariaRowsFor = _displayRows;
		_ariaLinesFor = _displayLines;
		_ariaExpandFor = _expandVersion;
		_ariaStartFor = _ariaBodyStart;

		int at = _ariaBodyStart;
		int[] rows = new int[_displayRows.Count];
		if (IsGrouped)
		{
			int[] lines = new int[_displayLines.Count];
			for (int i = 0; i < _displayLines.Count; i++)
			{
				lines[i] = ++at;
				if (_displayLines[i].Group is null && _displayLines[i].Row is var row and >= 0 && row < rows.Length)
				{
					rows[row] = at;
					if (details && IsExpanded(_displayRows[row].Item))
					{
						at++;
					}
				}
			}

			_ariaLineIndexes = lines;
		}
		else
		{
			for (int i = 0; i < rows.Length; i++)
			{
				rows[i] = ++at;
				if (IsExpanded(_displayRows[i].Item))
				{
					at++;
				}
			}

			_ariaLineIndexes = null;
		}

		_ariaRowIndexes = rows;
		_ariaBodyEnd = at;
	}

	// The window's rows as they are, and the open details among them. The places noted for rows outside the window stay
	// until a reload, a collapse of every row or a new state says they may have moved.
	private void NoteDetailPlaces(bool anyOpen)
	{
		if (!anyOpen)
		{
			ForgetDetailPlaces();
			return;
		}

		if (ReferenceEquals(_detailPlacesFor, _displayRows) && _detailPlacesVersion == _expandVersion)
		{
			return;
		}

		_detailPlacesFor = _displayRows;
		_detailPlacesVersion = _expandVersion;
		int first = _windowFirstPlace;
		int end = _windowFirstPlace + _windowPlaces;
		_detailPlaces.RemoveWhere(place => place >= first && place < end);
		foreach (MokaTableRow<TItem> row in _displayRows)
		{
			if (IsExpanded(row.Item))
			{
				_detailPlaces.Add(RowPlace(row));
			}
		}
	}

	private void ForgetDetailPlaces()
	{
		_detailPlaces.Clear();
		_detailPlacesFor = null;
	}

	// A row's number in the whole set: its place, after the rows drawn above the body and the open details above it.
	private int AriaRowNumber(int place)
	{
		int number = _ariaBodyStart + place + 1;
		foreach (int open in _detailPlaces)
		{
			if (open < place)
			{
				number++;
			}
		}

		return number;
	}

	// Under remote windows the places count the rows of the whole result, or grouped, its lines.
	private int RemotePlaces => _remoteGrouped ? _lineCount : _totalItems;

	private int RowPlace(MokaTableRow<TItem> row) =>
		_lineOfRow is { } lines && lines.TryGetValue(row.Index, out int line) ? line : row.Index;

	private string AriaRowIndexOf(MokaTableRow<TItem> row) =>
		IndexText(_ariaRowIndexes is { } rows && row.Index < rows.Length ? rows[row.Index] : AriaRowNumber(RowPlace(row)));

	private string AriaDetailIndexOf(MokaTableRow<TItem> row) =>
		IndexText((_ariaRowIndexes is { } rows && row.Index < rows.Length ? rows[row.Index] : AriaRowNumber(RowPlace(row))) + 1);

	private string PlaceholderRowIndex(int place) => IndexText(AriaRowNumber(place));

	private string? AriaLineIndexOf(int line) =>
		RemoteRows ? IndexText(AriaRowNumber(line))
		: _ariaLineIndexes is { } lines && line < lines.Length ? IndexText(lines[line])
		: null;

	private string? AriaSelectedOf(MokaTableRow<TItem> row) =>
		Selectable ? IsPicked(row.Item) ? "true" : "false" : null;

	// A treegrid row sits one level below its innermost heading.
	private string? AriaLevelOfRow => IsGrouped ? IndexText(_groups.Count + 1) : null;

	private static string AriaLevelOf(TableGroupNode<TItem> node) => IndexText(node.Level + 1);

	private string? AriaGroupExpanded(TableGroupNode<TItem> node) =>
		GroupsCollapsible ? IsGroupCollapsed(node.Id) ? "false" : "true" : null;

	private string PinnedRowIndex(bool top, int index) => top
		? IndexText(_ariaHeaderRows + index + 1)
		: IndexText(_ariaBodyEnd + BelowRowsCount + (NewRowAtBottom ? 1 : 0) + index + 1);

	private string NewRowIndex => NewRowAtTop
		? IndexText(_ariaHeaderRows + ShownPinnedTop + 1)
		: IndexText(_ariaBodyEnd + 1);

	private int StateRowPlace => _ariaHeaderRows + ShownPinnedTop + (NewRowAtTop ? 1 : 0) + 1;

	private string StateRowIndex => IndexText(StateRowPlace);

	// Under the rows and a new row at the bottom, or under the state row that stands for no rows.
	private string BelowRowsIndex => IndexText(HasNothingToShow ? StateRowPlace + 1 : _ariaBodyEnd + (NewRowAtBottom ? 1 : 0) + 1);

	private bool StateRowShown => FailureReplacesRows || HasNothingToShow || (_isLoading && (LoadingContent is not null || SkeletonRows <= 0));
}
