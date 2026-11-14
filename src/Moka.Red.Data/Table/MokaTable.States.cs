using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Data.Table;

// What the body says when it has no rows to draw: a failed load, no rows for the search and filters, or no rows at all.
public partial class MokaTable<TItem>
{
	private Exception? _loadError;

	// A stream that breaks after rows came keeps them, with the failure under them. Any other failure takes their place.
	private bool _failureBelowRows;

	/// <summary>
	///     Your own content for a failed <see cref="ServerData" /> load or a broken <see cref="ItemStream" />, in place of
	///     the message with Retry. It receives the failure and a way to load again.
	/// </summary>
	[Parameter]
	public RenderFragment<MokaTableFailure>? ErrorContent { get; set; }

	/// <summary>
	///     Raised when the latest <see cref="ServerData" /> load fails or the <see cref="ItemStream" /> throws, for logging.
	///     The table shows the failure with Retry either way; a load that a newer one replaced is never reported.
	/// </summary>
	[Parameter]
	public EventCallback<Exception> OnLoadFailed { get; set; }

	/// <summary>
	///     Your own content for a search or filters that leave no row, in place of "No results" with a button that clears
	///     them. <see cref="Empty" /> and <see cref="EmptyContent" /> are for a table with no rows at all.
	/// </summary>
	[Parameter]
	public RenderFragment? NoResults { get; set; }

	/// <summary>Whether the last load failed, and the table shows the failure.</summary>
	public bool HasLoadFailed => _loadError is not null;

	private bool FailureReplacesRows => _loadError is not null && !_failureBelowRows;

	private bool FailureBelowRows => _loadError is not null && _failureBelowRows;

	// The rows that came stay, searched, filtered, sorted and paged as before; with none, the failure stands in their place.
	private void ShowStreamFailure(Exception error)
	{
		_loadError = error;
		_failureBelowRows = _streamedItems is { Count: > 0 };
	}

	private void ShowLoadFailure(Exception error)
	{
		_loadError = error;
		_failureBelowRows = false;
		_totalItems = 0;
		_pageTotal = 0;
		_totalKnown = true;
		_hasMore = false;
		_groupTree = [];
		_displayLines = [];
		SetDisplayItems([], 0);
		_shownUnits = 0;
	}

	private MokaTableFailure Failure => new(_loadError!, RetryAsync);

	private async Task RetryAsync()
	{
		if (_stream is not null)
		{
			SyncStream(again: true);
		}

		await LoadDataAsync();
		StateHasChanged();
	}

	private bool ShowNoResults => FiltersActive && !FailureReplacesRows;

	private string NoResultsText => Words.NoResultsFor(string.IsNullOrWhiteSpace(_searchTerm) ? null : _searchTerm);

	private static string StateCellClass(bool plain) => new CssBuilder("moka-table-state-cell")
		.AddClass("moka-table-state-cell--plain", plain)
		.Build();
}

/// <summary>A failed load of a <see cref="MokaTable{TItem}" />, for its <c>ErrorContent</c>.</summary>
/// <param name="Error">What went wrong.</param>
/// <param name="Retry">Loads the rows again.</param>
public sealed record MokaTableFailure(Exception Error, Func<Task> Retry);
