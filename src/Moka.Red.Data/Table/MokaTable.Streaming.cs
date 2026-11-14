using System.Diagnostics;
using Microsoft.AspNetCore.Components;

namespace Moka.Red.Data.Table;

// Rows from an IAsyncEnumerable: they show as they arrive, gathered into one render a frame rather than one per row, with
// a row under them saying more are on the way and a button that stops reading. The search, filters, sort and pager work
// on whatever has arrived.
public partial class MokaTable<TItem>
{
	// About one frame: rows that arrive faster than this wait for the next render.
	private static readonly TimeSpan StreamFrame = TimeSpan.FromMilliseconds(16);

	private IAsyncEnumerable<TItem>? _stream;
	private List<TItem>? _streamedItems;
	private bool _streaming;
#pragma warning disable CA2213 // Each read owns and disposes its own. This only points at the one running.
	private CancellationTokenSource? _streamCancellation;
#pragma warning restore CA2213

	/// <summary>
	///     Rows that arrive over time, such as the results of a long query or a log being read. They show as they come,
	///     gathered into one render a frame, with a row under them saying how many have come and a Stop button. The search,
	///     filters, sort and pager work on what has arrived. A stream that throws keeps the rows that came, with the failure
	///     and Try again under them, which reads it again from the start. A new stream starts over; the table cancels the old
	///     one. Used when <see cref="Items" /> and <see cref="ServerData" /> are null. Cancellation reaches the stream
	///     through <c>WithCancellation</c>, so an iterator marks its token <c>[EnumeratorCancellation]</c>.
	/// </summary>
	[Parameter]
	public IAsyncEnumerable<TItem>? ItemStream { get; set; }

	/// <summary>Whether rows are still arriving from <see cref="ItemStream" />.</summary>
	public bool IsStreaming => _streaming;

	private IEnumerable<TItem> ClientItems => Items ?? (IEnumerable<TItem>?)_streamedItems ?? [];

	private bool StreamMoreRow => _streaming && _loadError is null;

	private string StreamText => Words.StreamingRows(_streamedItems?.Count ?? 0);

	/// <summary>Stops reading <see cref="ItemStream" />, keeping the rows that came.</summary>
	public void StopStreaming()
	{
		_streamCancellation?.Cancel();
		_streaming = false;
		StateHasChanged();
	}

	// Started from OnParametersSet when the parent passes another stream, and by Retry after one broke.
	private void SyncStream(bool again = false)
	{
		IAsyncEnumerable<TItem>? stream = Items is null && ServerData is null ? ItemStream : null;
		if (ReferenceEquals(stream, _stream) && !again)
		{
			return;
		}

		_streamCancellation?.Cancel();
		_streamCancellation = null;
		_loadError = null;
		_stream = stream;
		_streamedItems = stream is null ? null : [];
		_streaming = stream is not null;
		_isLoading = stream is not null;
		_parameterReloadPending = true;
		if (stream is not null)
		{
			CancellationTokenSource cancellation = new();
			_streamCancellation = cancellation;
			RunDetached(() => ReadStreamAsync(stream, cancellation));
		}
	}

	private async Task ReadStreamAsync(IAsyncEnumerable<TItem> stream, CancellationTokenSource cancellation)
	{
		List<TItem> batch = [];
		long since = Stopwatch.GetTimestamp();
		try
		{
			await using IAsyncEnumerator<TItem> rows = stream.GetAsyncEnumerator(cancellation.Token);
			while (true)
			{
				ValueTask<bool> next = rows.MoveNextAsync();

				// About to wait for the next row: what has arrived shows now.
				if (!next.IsCompleted && batch.Count > 0)
				{
					ShowStreamed(batch, cancellation);
					since = Stopwatch.GetTimestamp();
				}

				if (!await next)
				{
					break;
				}

				batch.Add(rows.Current);
				if (Stopwatch.GetElapsedTime(since) >= StreamFrame)
				{
					ShowStreamed(batch, cancellation);
					since = Stopwatch.GetTimestamp();
					await Task.Yield();
				}
			}

			ShowStreamed(batch, cancellation);
		}
		catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
		{
		}
#pragma warning disable CA1031 // A stream that breaks shows its failure with Retry, which reads it again from the start.
		catch (Exception ex) when (ReferenceEquals(_streamCancellation, cancellation))
#pragma warning restore CA1031
		{
			ShowStreamed(batch, cancellation);
			ShowStreamFailure(ex);
			await OnLoadFailed.InvokeAsync(ex);
		}
		finally
		{
			if (ReferenceEquals(_streamCancellation, cancellation))
			{
				_streaming = false;
				_streamCancellation = null;
				_isLoading = false;
				StateHasChanged();
			}

			cancellation.Dispose();
		}
	}

	// Rows with nothing to search, filter, sort or group join the end without running the rows through the pipeline again.
	private void ShowStreamed(List<TItem> batch, CancellationTokenSource cancellation)
	{
		if (!ReferenceEquals(_streamCancellation, cancellation) || _streamedItems is null)
		{
			batch.Clear();
			return;
		}

		_streamedItems.AddRange(batch);
		bool plain = !FiltersActive && _sorts.Count == 0 && !IsGrouped && CountedColumns().Count == 0;
		if (plain && _filteredItems.Count + batch.Count == _streamedItems.Count)
		{
			_filteredItems.AddRange(batch);
			_totalItems = _filteredItems.Count;
			PageClientRows();
		}
		else
		{
			LoadClientData();
		}

		batch.Clear();
		_isLoading = false;
		StateHasChanged();
	}
}
