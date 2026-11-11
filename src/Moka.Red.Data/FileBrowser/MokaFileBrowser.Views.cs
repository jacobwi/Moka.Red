using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Moka.Red.Core.Enums;
using Moka.Red.Core.Icons;
using Moka.Red.Core.Utilities;
using Moka.Red.Icons;

namespace Moka.Red.Data.FileBrowser;

public partial class MokaFileBrowser<TItem>
{
	private const float CompactLineEstimate = 30;
	private const float CardLineEstimate = 64;
	private const float GalleryLineEstimate = 150;
	private const float LsLineEstimate = 26;

	// A chip in the gallery takes two cells of the grid its pictures sit in.
	private const int ChipCells = 2;

	private static readonly IReadOnlyList<MokaFileViewMode> DefaultViews = [MokaFileViewMode.List, MokaFileViewMode.Tiles];

	private readonly HashSet<string> _folded = new(StringComparer.Ordinal);

	private List<FileLine> _lines = [];
	private long _largestSize;
	private List<FileGroup> _groups = [];
	private MokaFileGroupBy _groupBy;
	private MokaFileGroupBy _lastGroupBy;
	private MokaFileRowStyle _lastRowStyle;
	private TimeProvider _clock = TimeProvider.System;

	private IReadOnlyList<MokaFileViewMode> OfferedViews => Views is { Count: > 0 } views ? views : DefaultViews;

	private bool IsTerminal => RowStyle == MokaFileRowStyle.Terminal;

	// The views drawn as lines of cells, whose width the script measures.
	private bool IsGridView => _view.Mode is MokaFileViewMode.Tiles or MokaFileViewMode.Compact or MokaFileViewMode.Cards or MokaFileViewMode.Gallery;

	private bool IsGrouped => _groupBy != MokaFileGroupBy.None && GroupsIn(_view.Mode);

	// Lines that differ in length, so the arrows look for the item above or below rather than counting.
	private bool UsesLineKeys => _view.Mode == MokaFileViewMode.Gallery || (IsGrouped && IsGridView);

	// The new views and grouping name their lines, so the script scrolls to the line itself rather than working it out.
	private bool NumbersLines => IsGrouped || _view.Mode is not (MokaFileViewMode.List or MokaFileViewMode.Tiles) || (IsTerminal && _view.Mode == MokaFileViewMode.Tiles);

	private bool DrawsLsTiles => IsTerminal && _view.Mode == MokaFileViewMode.Tiles;

	private bool DrawsLsList => IsTerminal && _view.Mode == MokaFileViewMode.List;

	private bool ShowsColumnHead => _view.Mode == MokaFileViewMode.List && !IsTerminal;

	private float LineEstimate => _view.Mode switch
	{
		MokaFileViewMode.Compact => CompactLineEstimate,
		MokaFileViewMode.Cards => CardLineEstimate,
		MokaFileViewMode.Gallery => GalleryLineEstimate,
		MokaFileViewMode.Tiles when IsTerminal => LsLineEstimate,
		_ => TileRowEstimate
	};

	private string RowStyleClass => RowStyle switch
	{
		MokaFileRowStyle.Zebra => "moka-file-browser--rows-zebra",
		MokaFileRowStyle.Airy => "moka-file-browser--rows-airy",
		MokaFileRowStyle.Outlined => "moka-file-browser--rows-outlined",
		MokaFileRowStyle.Terminal => "moka-file-browser--rows-terminal",
		MokaFileRowStyle.Solid => "moka-file-browser--rows-solid",
		_ => ""
	};

	private static bool GroupsIn(MokaFileViewMode mode) =>
		mode is MokaFileViewMode.List or MokaFileViewMode.Tiles or MokaFileViewMode.Compact or MokaFileViewMode.Cards;

	private static string? ViewClass(MokaFileViewMode mode) => mode switch
	{
		MokaFileViewMode.Compact => "moka-file-browser__list--compact",
		MokaFileViewMode.Columns => "moka-file-browser__list--columns",
		MokaFileViewMode.Gallery => "moka-file-browser__list--gallery",
		MokaFileViewMode.Cards => "moka-file-browser__list--cards",
		_ => null
	};

	private string ViewLabel(MokaFileViewMode mode) => mode switch
	{
		MokaFileViewMode.Tiles => Text.TilesView,
		MokaFileViewMode.Compact => Text.CompactView,
		MokaFileViewMode.Columns => Text.ColumnsView,
		MokaFileViewMode.Gallery => Text.GalleryView,
		MokaFileViewMode.Cards => Text.CardsView,
		_ => Text.ListView
	};

	private static MokaIconDefinition ViewIcon(MokaFileViewMode mode) => mode switch
	{
		MokaFileViewMode.Tiles => MokaIcons.Navigation.Grid,
		MokaFileViewMode.Compact => MokaIcons.Content.RowsDense,
		MokaFileViewMode.Columns => MokaIcons.Content.ColumnsView,
		MokaFileViewMode.Gallery => MokaIcons.Content.Images,
		MokaFileViewMode.Cards => MokaIcons.Navigation.Dashboard,
		_ => MokaIcons.Content.List
	};

	private void ApplyLookParameters()
	{
		if (GroupBy != _lastGroupBy)
		{
			_lastGroupBy = GroupBy;
			if (GroupBy != _groupBy)
			{
				_groupBy = GroupBy;
				RebuildVisible();
			}
		}

		if (RowStyle != _lastRowStyle)
		{
			bool lsChanged = (RowStyle == MokaFileRowStyle.Terminal) != (_lastRowStyle == MokaFileRowStyle.Terminal);
			_lastRowStyle = RowStyle;
			if (lsChanged && _view.Mode == MokaFileViewMode.Tiles)
			{
				_measureColumns = true;
				RebuildLines();
			}
		}

		ApplyPreviewParameter();
	}

	private async Task SetGroupByAsync(string? value)
	{
		MokaFileGroupBy next = string.Equals(value, "date", StringComparison.Ordinal) ? MokaFileGroupBy.Date : MokaFileGroupBy.Kind;
		if (next == _groupBy)
		{
			return;
		}

		_groupBy = next;
		RebuildVisible();
		await GroupByChanged.InvokeAsync(next);
		await RaiseSelectionIfChangedAsync();
	}

	private string GroupByValue => _groupBy == MokaFileGroupBy.Date ? "date" : "kind";

	private async Task ToggleGroupAsync(FileGroup group)
	{
		if (!_folded.Remove(group.Id))
		{
			_folded.Add(group.Id);
		}

		RebuildVisible();
		await RaiseSelectionIfChangedAsync();
	}

	private MokaFileType TypeOf(MokaFileRow<TItem> row) =>
		row.Type ??= FileType?.Invoke(row.Item) ?? MokaFileType.Of(row.Info);

	private void AssignGroups(List<MokaFileRow<TItem>> rows, bool grouped)
	{
		if (!grouped)
		{
			foreach (MokaFileRow<TItem> row in rows)
			{
				row.Group = 0;
			}

			return;
		}

		DateTimeOffset now = _clock.GetLocalNow();
		foreach (MokaFileRow<TItem> row in rows)
		{
			row.Group = _groupBy == MokaFileGroupBy.Kind ? (int)TypeOf(row).Category : (int)DateGroupOf(row.Info.Modified, now);
		}
	}

	private MokaFileDateGroup DateGroupOf(DateTimeOffset? modified, DateTimeOffset now)
	{
		if (modified is not { } when)
		{
			return MokaFileDateGroup.Unknown;
		}

		DateTime day = TimeZoneInfo.ConvertTime(when, _clock.LocalTimeZone).Date;
		int daysAgo = (now.Date - day).Days;
		return daysAgo switch
		{
			<= 0 => MokaFileDateGroup.Today,
			1 => MokaFileDateGroup.Yesterday,
			<= 7 => MokaFileDateGroup.LastWeek,
			<= 30 => MokaFileDateGroup.LastMonth,
			_ => MokaFileDateGroup.Older
		};
	}

	// Rows arrive sorted by heading. Counts and sizes cover a folded group too, and its rows leave the list.
	private List<MokaFileRow<TItem>> GatherGroups(List<MokaFileRow<TItem>> sorted)
	{
		List<FileGroup> groups = [];
		List<MokaFileRow<TItem>> shown = new(sorted.Count);
		FileGroup? current = null;
		foreach (MokaFileRow<TItem> row in sorted)
		{
			if (current is null || current.Order != row.Group)
			{
				string id = FormattableString.Invariant($"{_groupBy}-{row.Group}");
				current = new FileGroup(id, row.Group, GroupTitle(row.Group), _folded.Contains(id));
				groups.Add(current);
			}

			current.Count++;
			if (!row.Info.IsFolder && row.Info.Size is { } size)
			{
				current.Bytes += size;
				current.HasBytes = true;
			}

			if (!current.Folded)
			{
				current.Rows.Add(row);
				shown.Add(row);
			}
		}

		_groups = groups;
		return shown;
	}

	private string GroupTitle(int order) => _groupBy == MokaFileGroupBy.Kind
		? Text.CategoryHeading((MokaFileCategory)order)
		: Text.DateHeading((MokaFileDateGroup)order);

	private string GroupHeadId(FileGroup group) => $"{_idBase}-group-{group.Id}";

	// Each line holds the rows drawn side by side, so the arrow keys and the script's scrolling can find them.
	private void RebuildLines()
	{
		int columns = Math.Max(1, _tileColumns);
		List<FileLine> lines = [];
		if (!IsGridView)
		{
			foreach (MokaFileRow<TItem> row in _visible)
			{
				row.Line = row.Index;
				row.Column = 0;
			}
		}
		else if (_view.Mode == MokaFileViewMode.Gallery)
		{
			int chips = Math.Max(1, columns / ChipCells);
			int folders = _visible.FindIndex(row => !row.Info.IsFolder);
			folders = folders < 0 ? _visible.Count : folders;
			AddLines(lines, _visible, 0, folders, chips, ChipCells, null);
			AddLines(lines, _visible, folders, _visible.Count - folders, columns, 1, null);
		}
		else if (IsGrouped)
		{
			foreach (FileGroup group in _groups)
			{
				group.Lines.Clear();
				AddLines(lines, group.Rows, 0, group.Rows.Count, columns, 1, group);
			}
		}
		else
		{
			AddLines(lines, _visible, 0, _visible.Count, columns, 1, null);
		}

		_lines = lines;
		_largestSize = SizeBars ? LargestSize() : 0;
	}

	private static void AddLines(List<FileLine> lines, List<MokaFileRow<TItem>> rows, int start, int count, int across, int cells, FileGroup? group)
	{
		for (int first = start; first < start + count; first += across)
		{
			List<MokaFileRow<TItem>> slice = rows.GetRange(first, Math.Min(across, start + count - first));
			FileLine line = new(slice, lines.Count, cells);
			for (int i = 0; i < slice.Count; i++)
			{
				slice[i].Line = line.Number;
				slice[i].Column = i;
			}

			lines.Add(line);
			group?.Lines.Add(line);
		}
	}

	// Up and Down keep the place along the line, measured in cells, so a chip over two pictures lands between them.
	private int LineTarget(string key, int current, int pageRows)
	{
		int last = _visible.Count - 1;
		if (current < 0)
		{
			return key is "ArrowUp" or "ArrowLeft" or "End" ? last : 0;
		}

		MokaFileRow<TItem> row = _visible[current];
		int? line = key switch
		{
			"ArrowDown" => row.Line + 1,
			"ArrowUp" => row.Line - 1,
			"PageDown" => row.Line + Math.Max(1, pageRows),
			"PageUp" => row.Line - Math.Max(1, pageRows),
			_ => null
		};

		if (line is not { } wanted)
		{
			return key switch
			{
				"ArrowRight" => Math.Min(last, current + 1),
				"ArrowLeft" => Math.Max(0, current - 1),
				"Home" => 0,
				"End" => last,
				_ => -1
			};
		}

		if (_lines.Count == 0)
		{
			return current;
		}

		FileLine from = _lines[row.Line];
		FileLine to = _lines[Math.Clamp(wanted, 0, _lines.Count - 1)];
		int cell = row.Column * from.Cells;
		return to.Rows[Math.Min(cell / to.Cells, to.Rows.Count - 1)].Index;
	}

	private string? LineAttribute(MokaFileRow<TItem> row) =>
		NumbersLines && !IsGridView ? row.Line.ToString(CultureInfo.InvariantCulture) : null;

	private string? LineNumber(FileLine line) => NumbersLines ? line.Number.ToString(CultureInfo.InvariantCulture) : null;

	private MokaIconDefinition IconFor(MokaFileRow<TItem> row)
	{
		if (row.Info.Icon is { } own)
		{
			return own;
		}

		if (row.Info.IsFolder)
		{
			return MokaIcons.File.Folder;
		}

		if (!TypeColors && _view.Mode != MokaFileViewMode.Gallery)
		{
			return MokaIcons.File.Document;
		}

		return TypeIcon(TypeOf(row));
	}

	private static MokaIconDefinition TypeIcon(MokaFileType type) => type.Category switch
	{
		MokaFileCategory.Folder => MokaIcons.File.Folder,
		MokaFileCategory.Image => MokaIcons.File.FileImage,
		MokaFileCategory.Document when type.Tag is "XLS" or "XLSX" or "ODS" or "CSV" or "TSV" => MokaIcons.File.FileSpreadsheet,
		MokaFileCategory.Document => MokaIcons.File.FileText,
		MokaFileCategory.Code when type.Tag is "JSON" => MokaIcons.File.FileJson,
		MokaFileCategory.Code => MokaIcons.File.FileCode,
		MokaFileCategory.Audio => MokaIcons.File.FileAudio,
		MokaFileCategory.Video => MokaIcons.File.FileVideo,
		MokaFileCategory.Archive => MokaIcons.File.FileArchive,
		MokaFileCategory.Log => MokaIcons.File.FileTerminal,
		_ => MokaIcons.File.Document
	};

	private MokaColor? ColorFor(MokaFileRow<TItem> row)
	{
		// The icon's color is written inline, so on a solid selection only leaving it out lets it take the text's color.
		if (RowStyle == MokaFileRowStyle.Solid && _selection.Contains(row.Key))
		{
			return null;
		}

		if (row.Info.IconColor is { } own)
		{
			return own;
		}

		if (row.Info.IsFolder)
		{
			return MokaColor.Warning;
		}

		return TypeColors && TypeOf(row).Tone is MokaColor.Success or MokaColor.Warning or MokaColor.Error or MokaColor.Info
			? TypeOf(row).Tone
			: null;
	}

	private static string TagClass(MokaFileType type) => new CssBuilder("moka-file-browser__tag")
		.AddClass(type.Tone switch
		{
			MokaColor.Success => "moka-file-browser__tag--success",
			MokaColor.Warning => "moka-file-browser__tag--warning",
			MokaColor.Error => "moka-file-browser__tag--error",
			MokaColor.Info => "moka-file-browser__tag--info",
			_ => "moka-file-browser__tag--neutral"
		})
		.Build();

	private string? TagOf(MokaFileRow<TItem> row) => TypeColors && !row.Info.IsFolder ? TypeOf(row).Tag : null;

	// The largest file showing, which every bar is measured against.
	private long LargestSize()
	{
		long largest = 0;
		foreach (MokaFileRow<TItem> row in _visible)
		{
			if (!row.Info.IsFolder && row.Info.Size is { } size && size > largest)
			{
				largest = size;
			}
		}

		return largest;
	}

	private string? SizeShare(MokaFileRow<TItem> row)
	{
		if (!SizeBars || row.Info.IsFolder || row.Info.Size is not { } size || _largestSize <= 0)
		{
			return null;
		}

		double share = Math.Max(0.03, Math.Min(1, (double)size / _largestSize));
		return share.ToString("0.###", CultureInfo.InvariantCulture);
	}

	private string? SizeBarStyle(MokaFileRow<TItem> row) =>
		SizeShare(row) is { } share ? new StyleBuilder().AddStyle("--_fb-share", share).Build() : null;

	private bool HasSizeBar(MokaFileColumn<TItem> column, MokaFileRow<TItem> row) =>
		SizeBars && column.Id == MokaFileSort.Size && IsBuiltIn(column) && SizeShare(row) is not null;

	private string RowCellClass(MokaFileColumn<TItem> column, MokaFileRow<TItem> row) => HasSizeBar(column, row)
		? CellClass(column) + " moka-file-browser__cell--bar"
		: CellClass(column);

	private string? CellStyle(MokaFileColumn<TItem> column, int index, MokaFileRow<TItem> row) => HasSizeBar(column, row)
		? new StyleBuilder().AddStyle(WidthStyle(column, index)).AddStyle("--_fb-share", SizeShare(row)).Build()
		: WidthStyle(column, index);

	private string TermSizeClass(MokaFileRow<TItem> row) => new CssBuilder("moka-file-browser__term-size")
		.AddClass("moka-file-browser__cell--bar", SizeShare(row) is not null)
		.Build();

	private bool ShowsBuiltIn(string id) => _columns.Any(column => string.Equals(column.Id, id, StringComparison.Ordinal) && IsBuiltIn(column));

	private static string GridLineClass(FileLine line) => new CssBuilder("moka-file-browser__grid-row")
		.AddClass("moka-file-browser__grid-row--chips", line.Cells > 1)
		.Build();

	private string MillerClass => new CssBuilder("moka-file-browser__miller")
		.AddClass("moka-file-browser__miller--top", ParentFolder is null)
		.Build();

	private string UpRowClass(MokaFileInfo info) => new CssBuilder("moka-file-browser__up-row")
		.AddClass("moka-file-browser__up-row--folder", info.IsFolder)
		.AddClass("moka-file-browser__up-row--open", string.Equals(info.Key, CurrentFolder, StringComparison.Ordinal))
		.Build();

	private EventCallback<MouseEventArgs> UpRowClick(MokaFileInfo info) => info.IsFolder
		? EventCallback.Factory.Create<MouseEventArgs>(this, () => GoToAsync(info.Key))
		: default;

	private MokaIconDefinition InfoIcon(MokaFileInfo info) =>
		info.Icon ?? (info.IsFolder ? MokaIcons.File.Folder : TypeColors ? TypeIcon(MokaFileType.Of(info)) : MokaIcons.File.Document);

	private MokaColor? InfoColor(MokaFileInfo info) =>
		info.IconColor ?? (info.IsFolder ? MokaColor.Warning
			: TypeColors && MokaFileType.Of(info).Tone is MokaColor.Success or MokaColor.Warning or MokaColor.Error or MokaColor.Info
				? MokaFileType.Of(info).Tone
				: null);

	private string CardDetail(MokaFileRow<TItem> row)
	{
		string kind = Text.TypeName(TypeOf(row));
		if (row.Info.IsFolder)
		{
			return ItemCountOf(row.Key) is { } count ? $"{kind} · {Text.ItemCount(count)}" : kind;
		}

		return SizeOf(row) is { } size ? $"{kind} · {size}" : kind;
	}

	private bool HasSidePane => Side is not null || FolderTree;

	private string SideLabel => Side is not null ? Text.Side : Text.FolderTree;

	internal sealed class FileGroup(string id, int order, string title, bool folded)
	{
		public string Id { get; } = id;

		public int Order { get; } = order;

		public string Title { get; } = title;

		public bool Folded { get; } = folded;

		public int Count { get; set; }

		public long Bytes { get; set; }

		public bool HasBytes { get; set; }

		public List<MokaFileRow<TItem>> Rows { get; } = [];

		public List<FileLine> Lines { get; } = [];
	}

	internal sealed class FileLine(List<MokaFileRow<TItem>> rows, int number, int cells)
	{
		public List<MokaFileRow<TItem>> Rows { get; } = rows;

		public int Number { get; } = number;

		// How many cells of the grid each item takes.
		public int Cells { get; } = cells;
	}
}
