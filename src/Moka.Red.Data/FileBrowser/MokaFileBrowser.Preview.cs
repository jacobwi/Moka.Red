using System.Globalization;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Data.FileBrowser;

public partial class MokaFileBrowser<TItem>
{
	private const int MostPeekRows = 50;

	private bool _previewOpen = true;
	private bool _lastPreviewOpen = true;
	private bool _previewDrawerOpen;

	private bool ShowsPreviewControl => PreviewPane && _view.Mode != MokaFileViewMode.Columns && HasControl(MokaFileControls.Preview);

	private bool ShowsPreviewPane => PreviewPane && _previewOpen && _view.Mode != MokaFileViewMode.Columns;

	private bool ShowsGroupByControl => IsGrouped && HasControl(MokaFileControls.GroupBy);

	private string PreviewToggleClass => new CssBuilder("moka-file-browser__mode")
		.AddClass("moka-file-browser__mode--on", _previewOpen)
		.Build();

	private void ApplyPreviewParameter()
	{
		if (PreviewOpen != _lastPreviewOpen)
		{
			_lastPreviewOpen = PreviewOpen;
			_previewOpen = PreviewOpen;
		}
	}

	private Task TogglePreviewAsync()
	{
		_previewOpen = !_previewOpen;
		return PreviewOpenChanged.InvokeAsync(_previewOpen);
	}

	private Task OpenPreviewDrawer()
	{
		_previewDrawerOpen = true;
		return Task.CompletedTask;
	}

	private string? ThumbnailOf(MokaFileRow<TItem> row) =>
		Thumbnail is null || row.Info.IsFolder ? null : UrlValues.SafeImageSource(Thumbnail(row.Item));

	private string FolderTitle => Crumbs.Count > 0 ? Crumbs[^1].Title : CurrentFolder;

	private static long FilesBytes(IEnumerable<MokaFileRow<TItem>> rows, out bool any)
	{
		long total = 0;
		any = false;
		foreach (MokaFileRow<TItem> row in rows)
		{
			if (!row.Info.IsFolder && row.Info.Size is { } size)
			{
				total += size;
				any = true;
			}
		}

		return total;
	}

	private string FolderSummaryText()
	{
		IEnumerable<MokaFileRow<TItem>> listed = _rows.Where(row => !row.FilteredOut);
		long bytes = FilesBytes(listed, out bool any);
		return Text.FolderSummary(ListedCount, any ? BytesText(bytes) : null);
	}

	private string? SelectedBytesText()
	{
		long bytes = FilesBytes(_selectedRows, out bool any);
		return any ? BytesText(bytes) : null;
	}

	private string SelectionTotalText()
	{
		long bytes = FilesBytes(_selectedRows, out bool any);
		return any ? Text.SelectionTotal(BytesText(bytes)) : Text.SelectedCount(_selectedRows.Count);
	}

	private IReadOnlyList<MokaFileRow<TItem>> ListedRows => _rows.FindAll(row => !row.FilteredOut);

	private string FolderFileText()
	{
		int folders = 0;
		int files = 0;
		foreach (MokaFileRow<TItem> row in _rows)
		{
			if (row.FilteredOut)
			{
				continue;
			}

			if (row.Info.IsFolder)
			{
				folders++;
			}
			else
			{
				files++;
			}
		}

		return Text.FolderFileCount(folders, files);
	}

	private string? FolderBytesText()
	{
		long bytes = FilesBytes(ListedRows, out bool any);
		return any ? BytesText(bytes) : null;
	}

	// The selection's files against the folder's, as a whole percent and a width for the meter.
	private (int Percent, string Width)? SelectionShare()
	{
		long folder = FilesBytes(ListedRows, out bool anyInFolder);
		long chosen = FilesBytes(_selectedRows, out bool anyChosen);
		if (!anyInFolder || !anyChosen || folder <= 0)
		{
			return null;
		}

		double share = Math.Clamp((double)chosen / folder, 0, 1);
		return ((int)Math.Round(share * 100, MidpointRounding.AwayFromZero), (share * 100).ToString("0.#", CultureInfo.InvariantCulture) + "%");
	}

	private static string? MeterStyle(string width) => new StyleBuilder().AddStyle("width", width).Build();

	private static List<MokaFileInfo> PeekRows(IReadOnlyList<MokaFileInfo> listing)
	{
		List<MokaFileInfo> sorted = [.. listing];
		sorted.Sort((a, b) =>
		{
			int group = (a.IsFolder ? 0 : 1).CompareTo(b.IsFolder ? 0 : 1);
			return group != 0 ? group : MokaFileNames.Compare(a.Name, b.Name);
		});
		return sorted.Count > MostPeekRows ? sorted.GetRange(0, MostPeekRows) : sorted;
	}

	private static string ShortSize(long bytes, bool binary)
	{
		double unit = binary ? 1024 : 1000;
		string[] suffixes = ["", "K", "M", "G", "T", "P"];
		double value = bytes;
		int step = 0;
		while (value >= unit && step < suffixes.Length - 1)
		{
			value /= unit;
			step++;
		}

		return step == 0
			? bytes.ToString(CultureInfo.InvariantCulture)
			: value.ToString(value < 10 ? "0.0" : "0", CultureInfo.InvariantCulture) + suffixes[step];
	}

	private string TerminalSize(MokaFileRow<TItem> row) => row.Info.IsFolder || row.Info.Size is not { } size
		? "-"
		: ByteFormatter is { } format ? format(size) : ShortSize(size, ByteUnits == Feedback.JobProgress.MokaByteUnits.Binary);

	// As ls writes it: the month and day, then the time within half a year and the year before that.
	private string TerminalDate(DateTimeOffset? modified)
	{
		if (modified is not { } when)
		{
			return "";
		}

		DateTime local = TimeZoneInfo.ConvertTime(when, _clock.LocalTimeZone).DateTime;
		DateTime now = _clock.GetLocalNow().DateTime;
		string day = local.ToString("MMM", CultureInfo.InvariantCulture) + " " + local.Day.ToString(CultureInfo.InvariantCulture).PadLeft(2);
		bool recent = local <= now.AddDays(1) && local > now.AddMonths(-6);
		return recent
			? day + " " + local.ToString("HH:mm", CultureInfo.InvariantCulture)
			: day + "  " + local.Year.ToString(CultureInfo.InvariantCulture);
	}

	private string TerminalTotal()
	{
		long bytes = FilesBytes(ListedRows, out _);
		return ShortSize(bytes, ByteUnits == Feedback.JobProgress.MokaByteUnits.Binary);
	}
}
