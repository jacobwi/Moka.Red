using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web.Virtualization;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Data.Table;

// Remote virtualization: with ServerData and Virtualize the rows load window by window as they scroll into view, the way
// Blazor's Virtualize asks an ItemsProvider, with the whole count from the source, a placeholder row for each row still on
// its way, and the request for a window scrolled past cancelled. The table holds only the window last loaded.
public partial class MokaTable<TItem>
{
	private Virtualize<MokaTableRow<TItem>>? _virtualize;
	private ItemsProviderDelegate<MokaTableRow<TItem>>? _provideRows;

	// Where the window last loaded starts in the whole result set. Rows carry their place in the whole set, so the keys,
	// the cell actions and the screen reader's row numbers mean the same row wherever the window is.
	private int _rowIndexBase;

	// Whether a window has come back since the last reload, so an empty answer can show the empty state.
	private bool _remoteLoaded;

	// Whether the last render drew the Virtualize: one taken out by an empty answer or a failure can't be asked again.
	private bool _virtualizeDrawn;

	// Grouped rows can't be windowed: grouping pages its lines instead, as it wins over Virtualize for rows in memory.
	private bool RemoteRows => ServerData is not null && Virtualize && !IsGrouped;

	// Kept for the component's life, so Virtualize sees the same provider on every render.
	private ItemsProviderDelegate<MokaTableRow<TItem>> RowProvider => _provideRows ??= ProvideRowsAsync;

	private async ValueTask<ItemsProviderResult<MokaTableRow<TItem>>> ProvideRowsAsync(ItemsProviderRequest request)
	{
		Func<MokaTableRequest, Task<MokaTableResult<TItem>>>? source = ServerData;
		if (source is null)
		{
			return new ItemsProviderResult<MokaTableRow<TItem>>([], 0);
		}

		int count = Math.Max(request.Count, 1);
		MokaTableRequest tableRequest = BuildRequest(request.StartIndex / count + 1, count, request.CancellationToken) with
		{
			StartIndex = request.StartIndex
		};

		MokaTableResult<TItem> result;
		try
		{
			result = await source(tableRequest);
		}
#pragma warning disable CA1031 // A window scrolled past is cancelled by Virtualize itself; any other failure is shown with Retry.
		catch (Exception ex) when (ex is not OperationCanceledException)
#pragma warning restore CA1031
		{
			ShowLoadFailure(ex);
			_remoteLoaded = true;
			StateHasChanged();
			await OnLoadFailed.InvokeAsync(ex);
			return new ItemsProviderResult<MokaTableRow<TItem>>([], 0);
		}

		request.CancellationToken.ThrowIfCancellationRequested();
		bool countChanged = !_remoteLoaded || _totalItems != (result.TotalItems ?? _totalItems) || _loadError is not null;
		_loadError = null;
		_remoteLoaded = true;
		_totalKnown = result.TotalItems is not null;
		_hasMore = !_totalKnown && result.Items.Count == count;

		// A source that doesn't count grows the scroll a window at a time while full windows come back.
		int total = result.TotalItems ?? request.StartIndex + result.Items.Count + (_hasMore ? count : 0);
		_totalItems = _pageTotal = total;
		_serverTotals = result.Totals;
		TakeServerValues(result);
		ShowWindow(result.Items, request.StartIndex);

		// The count, the totals and the facets are drawn outside Virtualize, which draws the window itself.
		if (countChanged || result.Totals is not null || result.Facets is not null)
		{
			StateHasChanged();
		}

		return new ItemsProviderResult<MokaTableRow<TItem>>(_displayRows, total);
	}

	private void ShowWindow(IReadOnlyList<TItem> items, int start)
	{
		_rowIndexBase = start;
		CarrySelection(items);
		_displayItems = items;
		_firstRowIndex = start;
		List<MokaTableRow<TItem>> rows = BuildRows(items);
		for (int i = 0; i < rows.Count; i++)
		{
			rows[i] = rows[i] with { Index = start + i };
		}

		_displayRows = rows;
		_shownUnits = rows.Count;
		_indeterminateState = null;
		PruneRowHandlers(start, rows.Count);
	}

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
		_remoteLoaded = false;
		_loadError = null;
		_isLoading = false;
		ForgetDetailPlaces();
		if (_virtualize is not null && _virtualizeDrawn)
		{
			await _virtualize.RefreshDataAsync();
		}
	}
}
