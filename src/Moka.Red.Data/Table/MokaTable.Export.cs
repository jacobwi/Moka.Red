using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Moka.Red.Data.Common;

namespace Moka.Red.Data.Table;

// Exporting every matching row as CSV or an Excel workbook, copying rows for a spreadsheet, and the totals under the rows.
public partial class MokaTable<TItem>
{
	private const string CopyModule = "./_content/Moka.Red.Core/moka-copy.js";

	private readonly string _exportMenuKey = $"moka-table-export-menu-{Guid.NewGuid():N}";
	private bool _showExportMenu;
	private bool _exportMenuShown;
	private ElementReference _exportMenuRef;
	private ElementReference _exportToggleRef;
	private ElementReference _copyRowsRef;

	// What the copy button says for a moment after a copy: null, copied or refused.
	private bool? _rowsCopied;

	/// <summary>Whether to show an export button in the toolbar. Default false.</summary>
	[Parameter]
	public bool Exportable { get; set; }

	/// <summary>
	///     The files the export button offers. Default <see cref="MokaTableExportFormats.Csv" />. With more than one it opens a
	///     menu; <see cref="MokaTableExportFormats.Excel" /> writes an .xlsx workbook with typed cells, written by the table
	///     itself.
	/// </summary>
	[Parameter]
	public MokaTableExportFormats ExportFormats { get; set; } = MokaTableExportFormats.Csv;

	/// <summary>
	///     The name an exported file is saved under, without its extension, which the format adds. Null, the default, is
	///     <c>export-</c> and the date and time.
	/// </summary>
	[Parameter]
	public string? ExportFileName { get; set; }

	/// <summary>
	///     With <see cref="Selectable" />, a Copy button copies the picked rows, and Ctrl+C on a cell copies them, as
	///     tab-separated text with the headings, which pastes into a spreadsheet as its own cells. The button sits in the
	///     bulk bar when the table has <see cref="BulkActions" />, else in the selection bar, which shows while rows are
	///     picked even without <see cref="SelectionActions" />. Default false. The copy is made inside the click, so Safari
	///     and Firefox take it too.
	/// </summary>
	[Parameter]
	public bool CopySelectedRows { get; set; }

	/// <summary>
	///     Callback to generate export data. Receives every row matching the current search and
	///     column filters (not just the current page), and the format picked. Under server-side data the table issues a
	///     dedicated ServerData request sized to cover the whole result set; check
	///     <see cref="MokaTableExportContext{TItem}.IsCompleteSet" /> to see whether the server
	///     returned all of it. Without it the table writes the file itself.
	/// </summary>
	[Parameter]
	public EventCallback<MokaTableExportContext<TItem>> OnExport { get; set; }

	private bool HasAggregates => _columns.Exists(c => c.Aggregate != MokaAggregateType.None);

	private bool ExportMenu => (ExportFormats & MokaTableExportFormats.Csv) != 0 && (ExportFormats & MokaTableExportFormats.Excel) != 0;

	private MokaTableExportFormat OnlyFormat =>
		(ExportFormats & MokaTableExportFormats.Csv) == 0 && (ExportFormats & MokaTableExportFormats.Excel) != 0
			? MokaTableExportFormat.Excel
			: MokaTableExportFormat.Csv;

	private Task HandleExport()
	{
		if (ExportMenu)
		{
			_showExportMenu = !_showExportMenu;
			return Task.CompletedTask;
		}

		return ExportAsync(OnlyFormat);
	}

	/// <summary>
	///     Exports every row the search and filters keep, in the sort order, as the export button does: through
	///     <see cref="OnExport" /> when set, else as a file the browser saves.
	/// </summary>
	/// <param name="format">The file to write.</param>
	/// <returns>A task that completes once the file is handed to the browser.</returns>
	public async Task ExportAsync(MokaTableExportFormat format)
	{
		_showExportMenu = false;
		(IReadOnlyList<TItem> items, bool complete) = await GetExportItemsAsync();
		List<MokaColumn<TItem>> columns = ExportColumns();

		if (OnExport.HasDelegate)
		{
			await OnExport.InvokeAsync(new MokaTableExportContext<TItem>
			{
				Items = items,
				Columns = _columns.Where(IsColumnVisible).ToList(),
				IsCompleteSet = complete,
				Format = format,
				FileName = FileNameFor(format)
			});
			return;
		}

		using MemoryStream file = format == MokaTableExportFormat.Excel ? WriteWorkbook(items, columns) : WriteCsv(items, columns);
		await DownloadAsync(file, FileNameFor(format));
	}

	// The visible columns with something to export, in the order they stand.
	private List<MokaColumn<TItem>> ExportColumns() =>
		VisibleColumnsInOrder().Select(static entry => entry.Col).Where(static col => col.HasValue).ToList();

	private string FileNameFor(MokaTableExportFormat format)
	{
		string name = string.IsNullOrWhiteSpace(ExportFileName)
			? string.Create(CultureInfo.InvariantCulture, $"export-{DateTime.Now:yyyyMMdd-HHmmss}")
			: ExportFileName.Trim();
		return name + (format == MokaTableExportFormat.Excel ? ".xlsx" : ".csv");
	}

	// Server mode pages, so "all rows" means asking the source for one page big enough to hold
	// the whole result set instead of quietly exporting whatever is on screen.
	private async Task<(IReadOnlyList<TItem> Items, bool Complete)> GetExportItemsAsync()
	{
		if (ServerData is null)
		{
			return (_filteredItems, true);
		}

		MokaTableResult<TItem> result = await ServerData(BuildRequest(1, FullSetPageSize));
		return (result.Items, result.TotalItems is not { } total || result.Items.Count >= total);
	}

	// UTF-8 with a byte order mark, without which Excel reads the file in the system's code page.
	private static MemoryStream WriteCsv(IReadOnlyList<TItem> items, List<MokaColumn<TItem>> columns)
	{
		StringBuilder csv = new();
		csv.AppendLine(string.Join(",", columns.Select(static c => MokaCsv.Escape(c.Title ?? ""))));
		foreach (TItem item in items)
		{
			csv.AppendLine(string.Join(",", columns.Select(c => MokaCsv.Escape(c.ExportTextOf(item)))));
		}

		MemoryStream stream = new();
		byte[] bom = Encoding.UTF8.GetPreamble();
		stream.Write(bom, 0, bom.Length);
		byte[] bytes = Encoding.UTF8.GetBytes(csv.ToString());
		stream.Write(bytes, 0, bytes.Length);
		stream.Position = 0;
		return stream;
	}

	// The rows, and under them the footer's totals as numbers, over every exported row or as a server sent them.
	private MemoryStream WriteWorkbook(IReadOnlyList<TItem> items, List<MokaColumn<TItem>> columns)
	{
		IEnumerable<IReadOnlyList<object?>> rows =
			items.Select(item => (IReadOnlyList<object?>)columns.Select(col => col.ExportValueOf(item)).ToList());
		if (columns.Exists(static col => col.Aggregate != MokaAggregateType.None))
		{
			rows = rows.Append(columns.Select(col => (object?)AggregateValue(col, items)).ToList());
		}

		return MokaXlsx.Write(Title ?? Label ?? Words.SheetName, columns.Select(static col => col.Title ?? string.Empty).ToList(), rows);
	}

	// The file goes to the browser as a stream it reads into a blob, the way ASP.NET Core's file download docs show, not
	// as one long string in a message.
	private async Task DownloadAsync(Stream file, string fileName)
	{
		using DotNetStreamReference content = new(file, leaveOpen: true);
		await InvokeTableJsAsync("downloadFile", fileName, content);
	}

	// ── Copying ──

	private bool ShowCopyRows => CopySelectedRows && Selectable && _selectedItems.Count > 0;

	private string CopyRowsText => _rowsCopied switch
	{
		true => Words.CopiedRows,
		false => Words.CopyFailed,
		null => Words.CopyRows(_selectedItems.Count)
	};

	// The picked rows as a spreadsheet block with the headings, the ones on screen first.
	private string SelectedRowsText()
	{
		List<MokaColumn<TItem>> columns = ExportColumns();
		List<IReadOnlyList<string>> rows = [columns.Select(static col => col.Title ?? string.Empty).ToList()];
		rows.AddRange(PickedInDisplayOrder().Select(item => (IReadOnlyList<string>)columns.Select(col => col.ExportTextOf(item)).ToList()));
		return MokaTsv.Write(rows);
	}

	// The copy button asked, inside its click, for the text: moka-copy.js writes it to the clipboard as it arrives.
	private async Task AnswerCopyRowsAsync() =>
		await SafeModuleInvokeVoidAsync(CopyModule, "answerCopy", _copyRowsRef, SelectedRowsText());

	private void NoteRowsCopied(bool copied) => _rowsCopied = copied;

	/// <summary>
	///     Called from JS for Ctrl+C on a cell with rows picked and no block of cells marked: the picked rows as a
	///     spreadsheet block with the headings.
	/// </summary>
	/// <returns>The text, or null when nothing is picked or copying rows is off.</returns>
	[JSInvokable]
	public string? CopySelectedRowsText() => CopySelectedRows && _selectedItems.Count > 0 ? SelectedRowsText() : null;

	// ── Aggregates ──

	private string AggregateLabel(MokaColumn<TItem> col) => col.Aggregate switch
	{
		MokaAggregateType.Sum => Words.Sum,
		MokaAggregateType.Average => Words.Average,
		MokaAggregateType.Count => Words.Count,
		MokaAggregateType.Min => Words.Min,
		MokaAggregateType.Max => Words.Max,
		_ => ""
	};

	// The footer's total: what a server sent for the whole result set, else the rows the table has.
	private string ComputeAggregate(MokaColumn<TItem> col)
	{
		if (col.Aggregate == MokaAggregateType.None)
		{
			return "";
		}

		return FormatAggregate(col, AggregateValue(col, LoadedItems));
	}

	// What a server sent for the whole result set, else the total over the rows given.
	private double? AggregateValue(MokaColumn<TItem> col, IReadOnlyList<TItem> rows)
	{
		if (col.Aggregate == MokaAggregateType.None)
		{
			return null;
		}

		if (col.Title is not null && _serverTotals is { } sent && sent.TryGetValue(col.Title, out double whole))
		{
			return whole;
		}

		return col.HasValue ? MokaTablePipeline.Aggregate(rows.Select(col.ValueOf), col.Aggregate) : null;
	}

	private static string FormatAggregate(MokaColumn<TItem> col, double? total)
	{
		if (total is not { } value)
		{
			return "";
		}

		return col.Aggregate == MokaAggregateType.Count
			? ((int)value).ToString(col.AggregateFormat ?? "", CultureInfo.CurrentCulture).TrimEnd()
			: value.ToString(col.AggregateFormat ?? "N2", CultureInfo.CurrentCulture);
	}

	// In the top layer like the column menu, so the table's rounded frame can't cut it off.
	private async Task SyncExportMenuAsync()
	{
		bool open = _showExportMenu && Exportable && ExportMenu && ShowToolbar && !SelectionBarShown;
		if (_exportMenuShown == open)
		{
			return;
		}

		_exportMenuShown = open;
		if (open)
		{
			await SafeModuleInvokeVoidAsync(PopupModule, "open", _exportMenuKey, _exportMenuRef, _exportToggleRef, null,
				new Dictionary<string, object?> { ["placement"] = "bottom-end" });
		}
		else
		{
			await SafeModuleInvokeVoidAsync(PopupModule, "close", _exportMenuKey);
		}
	}
}
