using System.IO.Compression;
using System.Runtime.CompilerServices;
using System.Xml.Linq;
using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using Moka.Red.Data.Common;
using Moka.Red.Data.Table;
using Moka.Red.Primitives.SegmentedControl;
using static Moka.Red.Data.Tests.Components.TableKit;

namespace Moka.Red.Data.Tests.Table;

// The grid pattern, spreadsheet export and copying, blocks of cells, rows loaded window by window or streamed, and
// columns drawn only in view.
public class MokaTablePhaseThreeTests : BunitContext
{
	private static readonly XNamespace Sheet = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";

	public MokaTablePhaseThreeTests() => JSInterop.Mode = JSRuntimeMode.Loose;

	// ── The grid pattern ──

	[Fact]
	public void TheTable_IsAGrid_ThatCountsItsRowsAndColumns()
	{
		IRenderedComponent<MokaTable<Person>> cut = Table(p => p.Add(t => t.Selectable, true));

		IElement table = cut.Find("table");
		Assert.Equal("grid", table.GetAttribute("role"));
		Assert.Equal("5", table.GetAttribute("aria-rowcount"));
		Assert.Equal("5", table.GetAttribute("aria-colcount"));
		Assert.Equal("true", table.GetAttribute("aria-multiselectable"));
		Assert.Equal("1", cut.Find("thead tr").GetAttribute("aria-rowindex"));
		Assert.Equal(["2", "3", "4", "5"], DataRows(cut).Select(static r => r.GetAttribute("aria-rowindex")));
		Assert.Equal(["1", "2", "3", "4", "5"],
			DataRows(cut)[0].QuerySelectorAll("td").Select(static c => c.GetAttribute("aria-colindex")));
		Assert.Equal(["2", "3", "4", "5"],
			cut.FindAll("thead th[data-col-index]").Select(static c => c.GetAttribute("aria-colindex")));
	}

	[Fact]
	public async Task PickedRows_SayTheyAreSelected()
	{
		IRenderedComponent<MokaTable<Person>> cut = Table(p => p.Add(t => t.Selectable, true));

		await cut.InvokeAsync(() => cut.Instance.OnCellKey(1, 0, " "));

		Assert.Equal(["false", "true", "false", "false"], DataRows(cut).Select(static r => r.GetAttribute("aria-selected")));
	}

	[Fact]
	public void UnderPaging_RowIndexesCountFromTheWholeSet()
	{
		Person[] many = Enumerable.Range(0, 25).Select(static i => new Person($"P{i}", "T", i, "C")).ToArray();
		IRenderedComponent<MokaTable<Person>> cut = Render<MokaTable<Person>>(p => p
			.Add(t => t.Items, many)
			.Add(t => t.PageSize, 10)
			.Add(t => t.Page, 2)
			.Add(t => t.ChildContent, DefaultColumns));

		Assert.Equal("26", cut.Find("table").GetAttribute("aria-rowcount"));
		Assert.Equal("12", DataRows(cut)[0].GetAttribute("aria-rowindex"));
		Assert.Equal("21", DataRows(cut)[^1].GetAttribute("aria-rowindex"));
	}

	[Fact]
	public async Task AnOpenDetail_CountsAsARow()
	{
		IRenderedComponent<MokaTable<Person>> cut = Table(p => p
			.Add(t => t.Expandable, true)
			.Add(t => t.DetailTemplate, item => builder => builder.AddContent(0, item.City)));

		await cut.FindAll("tbody tr[data-row-index] .moka-table-expand-btn")[0].ClickAsync(new MouseEventArgs());

		Assert.Equal("6", cut.Find("table").GetAttribute("aria-rowcount"));
		Assert.Equal("3", cut.Find(".moka-table-detail-row").GetAttribute("aria-rowindex"));
		Assert.Equal(["2", "4", "5", "6"], DataRows(cut).Select(static r => r.GetAttribute("aria-rowindex")));
	}

	[Fact]
	public void GroupedRows_MakeATreegrid_WithLevels()
	{
		IRenderedComponent<MokaTable<Person>> cut = Table(p => p
			.Add(t => t.GroupBy, "Team")
			.Add(t => t.ShowPagination, false));

		Assert.Equal("treegrid", cut.Find("table").GetAttribute("role"));
		IReadOnlyList<IElement> headings = cut.FindAll("tr.moka-table-group-row");
		Assert.All(headings, row => Assert.Equal("1", row.GetAttribute("aria-level")));
		Assert.All(DataRows(cut), row => Assert.Equal("2", row.GetAttribute("aria-level")));
		Assert.Equal(["2", "3", "4", "5", "6", "7"],
			cut.FindAll("tbody tr[aria-rowindex]").Select(static r => r.GetAttribute("aria-rowindex")));
		Assert.Equal("7", cut.Find("table").GetAttribute("aria-rowcount"));
	}

	[Fact]
	public void ASourceThatDoesNotCount_SaysTheCountIsUnknown()
	{
		IRenderedComponent<MokaTable<Person>> cut = Render<MokaTable<Person>>(p => p
			.Add(t => t.PageSize, 2)
			.Add(t => t.ServerData, _ => Task.FromResult(new MokaTableResult<Person> { Items = People.Take(2).ToList() }))
			.Add(t => t.ChildContent, DefaultColumns));

		cut.WaitForAssertion(() => Assert.Equal("-1", cut.Find("table").GetAttribute("aria-rowcount")));
	}

	// ── Excel ──

	[Fact]
	public async Task AnExcelExport_IsAWorkbook_WithTypedCells_AFrozenHeader_AndWidths()
	{
		BunitJSModuleInterop module = JSInterop.SetupModule(TableModule);
		module.Mode = JSRuntimeMode.Loose;
		Stock[] stock = [new("Lamp", 12.5, new DateTime(2026, 3, 1, 9, 30, 0), true), new("Desk <oak>", 240, new DateTime(2025, 11, 20), false)];
		IRenderedComponent<MokaTable<Stock>> cut = Render<MokaTable<Stock>>(p => p
			.Add(t => t.Items, stock)
			.Add(t => t.Title, "Stock")
			.Add(t => t.Exportable, true)
			.Add(t => t.ExportFormats, MokaTableExportFormats.Excel)
			.Add(t => t.ExportFileName, "stock-list")
			.Add(t => t.ChildContent, (RenderFragment)(builder =>
			{
				builder.OpenComponent<MokaColumn<Stock>>(0);
				builder.AddComponentParameter(1, "Title", "Name");
				builder.AddComponentParameter(2, "Field", (Func<Stock, object?>)(static s => s.Name));
				builder.CloseComponent();
				builder.OpenComponent<MokaNumberColumn<Stock>>(3);
				builder.AddComponentParameter(4, "Title", "Price");
				builder.AddComponentParameter(5, "Value", (Func<Stock, double?>)(static s => s.Price));
				builder.CloseComponent();
				builder.OpenComponent<MokaColumn<Stock>>(6);
				builder.AddComponentParameter(7, "Title", "Arrived");
				builder.AddComponentParameter(8, "Field", (Func<Stock, object?>)(static s => s.Arrived));
				builder.CloseComponent();
				builder.OpenComponent<MokaYesNoColumn<Stock>>(9);
				builder.AddComponentParameter(10, "Title", "Open");
				builder.AddComponentParameter(11, "Value", (Func<Stock, bool?>)(static s => s.Open));
				builder.CloseComponent();
			})));

		await cut.Find("button[aria-label='Export']").ClickAsync(new MouseEventArgs());

		JSRuntimeInvocation download = module.VerifyInvoke("downloadFile");
		Assert.Equal("stock-list.xlsx", download.Arguments[0]);
		byte[] bytes = ((MemoryStream)((DotNetStreamReference)download.Arguments[1]!).Stream).ToArray();
		(List<string> entries, XDocument workbook, XDocument sheet) = ReadWorkbook(bytes);
		Assert.Contains("[Content_Types].xml", entries);
		Assert.Contains("xl/styles.xml", entries);
		Assert.Equal("Stock", workbook.Descendants(Sheet + "sheet").Single().Attribute("name")!.Value);

		XElement pane = sheet.Descendants(Sheet + "pane").Single();
		Assert.Equal("frozen", pane.Attribute("state")!.Value);
		Assert.Equal("1", pane.Attribute("ySplit")!.Value);
		Assert.Equal(4, sheet.Descendants(Sheet + "col").Count());

		List<XElement> rows = sheet.Descendants(Sheet + "row").ToList();
		Assert.Equal(3, rows.Count);
		Assert.All(rows[0].Elements(Sheet + "c"), c => Assert.Equal("1", c.Attribute("s")!.Value));
		Assert.Equal(["Name", "Price", "Arrived", "Open"], rows[0].Elements(Sheet + "c").Select(c => c.Value));

		XElement[] lamp = rows[1].Elements(Sheet + "c").ToArray();
		Assert.Equal("inlineStr", lamp[0].Attribute("t")!.Value);
		Assert.Equal("Lamp", lamp[0].Value);
		Assert.Null(lamp[1].Attribute("t"));
		Assert.Equal("12.5", lamp[1].Value);
		Assert.Equal("2", lamp[2].Attribute("s")!.Value);
		Assert.Equal(new DateTime(2026, 3, 1, 9, 30, 0).ToOADate(), double.Parse(lamp[2].Value, System.Globalization.CultureInfo.InvariantCulture), 6);
		Assert.Equal("b", lamp[3].Attribute("t")!.Value);
		Assert.Equal("1", lamp[3].Value);

		XElement[] desk = rows[2].Elements(Sheet + "c").ToArray();
		Assert.Equal("Desk <oak>", desk[0].Value);
		Assert.Equal("3", desk[2].Attribute("s")!.Value);
		Assert.Equal("0", desk[3].Value);
	}

	[Fact]
	public async Task AnExcelExport_EndsWithTheFootersTotals_AsNumbers()
	{
		BunitJSModuleInterop module = JSInterop.SetupModule(TableModule);
		module.Mode = JSRuntimeMode.Loose;
		IRenderedComponent<MokaTable<Person>> cut = Render<MokaTable<Person>>(p => p
			.Add(t => t.Items, People)
			.Add(t => t.ChildContent, Columns(Name, Age with { Aggregate = MokaAggregateType.Sum })));

		await cut.InvokeAsync(() => cut.Instance.ExportAsync(MokaTableExportFormat.Excel));

		byte[] bytes = ((MemoryStream)((DotNetStreamReference)module.VerifyInvoke("downloadFile").Arguments[1]!).Stream).ToArray();
		XElement last = ReadWorkbook(bytes).Sheet.Descendants(Sheet + "row").Last();
		XElement total = Assert.Single(last.Elements(Sheet + "c"));
		Assert.Equal("B6", total.Attribute("r")!.Value);
		Assert.Equal("151", total.Value);
	}

	[Fact]
	public void TheWorkbookWriter_NamesColumnsLikeASpreadsheet()
	{
		Assert.Equal("A", MokaXlsx.ColumnName(0));
		Assert.Equal("Z", MokaXlsx.ColumnName(25));
		Assert.Equal("AA", MokaXlsx.ColumnName(26));
		Assert.Equal("AZ", MokaXlsx.ColumnName(51));
		Assert.Equal("BA", MokaXlsx.ColumnName(52));
	}

	[Fact]
	public async Task BothFormats_OpenAMenu()
	{
		BunitJSModuleInterop module = JSInterop.SetupModule(TableModule);
		module.Mode = JSRuntimeMode.Loose;
		IRenderedComponent<MokaTable<Person>> cut = Table(p => p
			.Add(t => t.Exportable, true)
			.Add(t => t.ExportFormats, MokaTableExportFormats.Csv | MokaTableExportFormats.Excel));

		await cut.Find("button[aria-label='Export']").ClickAsync(new MouseEventArgs());
		Assert.Equal(["Export as CSV", "Export as Excel"], cut.FindAll(".moka-table-export-item").Select(static i => i.TextContent));
		await cut.FindAll(".moka-table-export-item")[0].ClickAsync(new MouseEventArgs());

		Assert.EndsWith(".csv", (string)module.VerifyInvoke("downloadFile").Arguments[0]!, StringComparison.Ordinal);
		Assert.Empty(cut.FindAll(".moka-table-export-item"));
	}

	[Fact]
	public async Task OnExport_IsToldTheFormatAndTheFileName()
	{
		MokaTableExportContext<Person>? context = null;
		IRenderedComponent<MokaTable<Person>> cut = Table(p => p
			.Add(t => t.ExportFileName, "people")
			.Add(t => t.OnExport, c => context = c));

		await cut.InvokeAsync(() => cut.Instance.ExportAsync(MokaTableExportFormat.Excel));

		Assert.Equal(MokaTableExportFormat.Excel, context!.Format);
		Assert.Equal("people.xlsx", context.FileName);
		Assert.Equal(4, context.Items.Count);
	}

	// ── Copying ──

	[Fact]
	public async Task CopyingRows_AnswersWithTabSeparatedRows_AndHeadings()
	{
		BunitJSModuleInterop copy = JSInterop.SetupModule("./_content/Moka.Red.Core/moka-copy.js");
		copy.Mode = JSRuntimeMode.Loose;
		Person[] rows = [new("Ada", "Engineering\tCore", 36, "London"), new("=cmd", "Research", 45, "Paris \"Left\"")];
		IRenderedComponent<MokaTable<Person>> cut = Render<MokaTable<Person>>(p => p
			.Add(t => t.Items, rows)
			.Add(t => t.Selectable, true)
			.Add(t => t.CopySelectedRows, true)
			.Add(t => t.BulkActions, _ => _ => { })
			.Add(t => t.ChildContent, DefaultColumns));
		await cut.InvokeAsync(() => cut.Instance.OnCellKey(0, 0, " "));
		await cut.InvokeAsync(() => cut.Instance.OnCellKey(1, 0, " "));

		IElement button = cut.Find(".moka-table-copy-rows");
		Assert.Equal("Copy 2 rows", button.TextContent.Trim());
		Assert.True(button.HasAttribute("data-moka-copy-ask"));
		await button.TriggerEventAsync("onmokacopyask", EventArgs.Empty);

		string text = (string)copy.VerifyInvoke("answerCopy").Arguments[1]!;
		Assert.Equal("Name\tTeam\tAge\tCity\r\nAda\t\"Engineering\tCore\"\t36\tLondon\r\n'=cmd\tResearch\t45\t\"Paris \"\"Left\"\"\"\r\n", text);
		Assert.Equal(text, cut.Instance.CopySelectedRowsText());
		Assert.True(cut.Find("table").HasAttribute("data-moka-copy-rows"));
	}

	// ── Blocks of cells ──

	[Fact]
	public void ABlockOfCells_IsCopiedAsTabSeparatedText_WithoutHeadings()
	{
		IRenderedComponent<MokaTable<Person>> cut = Table(p => p.Add(t => t.CellRanges, true));

		string text = cut.Instance.CopyCellsText(2, 2, 1, 1);

		Assert.Equal("Research\t45\r\nEngineering\t41\r\n", text);
	}

	[Fact]
	public void CellRanges_BindTheBlockScript_AndMarkTheGridMultiselectable()
	{
		BunitJSModuleInterop module = JSInterop.SetupModule(TableModule);
		module.Mode = JSRuntimeMode.Loose;

		IRenderedComponent<MokaTable<Person>> cut = Table(p => p.Add(t => t.CellRanges, true));

		Assert.Same(cut.Instance, ((DotNetObjectReference<MokaTable<Person>>)module.VerifyInvoke("bindRanges").Arguments[1]!).Value);
		Assert.Equal("true", cut.Find("table").GetAttribute("aria-multiselectable"));
	}

	[Fact]
	public async Task PastingABlock_WritesTheEditableCells_ThroughTheSave_WithChecks()
	{
		List<MokaTableRowSave<Person>> saves = [];
		IRenderedComponent<MokaTable<Person>> cut = Render<MokaTable<Person>>(p => p
			.Add(t => t.Items, People)
			.Add(t => t.CellRanges, true)
			.Add(t => t.OnRowSave, save => saves.Add(save))
			.Add(t => t.ChildContent, Columns(
				Name with { Editable = true, SetField = static (p, v) => p with { Name = (string)v! } },
				Team,
				Age with
				{
					Editable = true,
					SetField = static (p, v) => p with { Age = (int)v! },
					Validate = static (_, v) => v is int age && age > 120 ? "Too old" : null
				})));

		await cut.InvokeAsync(() => cut.Instance.PasteCells(0, 0, 0, 0, "Ada L.\tX\t37\r\nGrace H.\tY\tlots\r\nAlan T.\tZ\t500\r\n"));

		Assert.Equal(3, saves.Count);
		Assert.All(saves, save => Assert.Equal(MokaTableSaveKind.Paste, save.Kind));
		Assert.Equal(new Person("Ada L.", "Engineering", 37, "London"), saves[0].Updated);
		Assert.Equal(["Name"], saves[1].Changes.Select(static c => c.Column));
		Assert.Equal(["Name"], saves[2].Changes.Select(static c => c.Column));
		Assert.Equal("Pasted 4 cells; 5 couldn't be pasted.", cut.Find(".moka-table-undo-text").TextContent.Trim());
	}

	[Fact]
	public async Task PastingOneValue_FillsTheMarkedBlock()
	{
		List<MokaTableRowSave<Person>> saves = [];
		IRenderedComponent<MokaTable<Person>> cut = Render<MokaTable<Person>>(p => p
			.Add(t => t.Items, People)
			.Add(t => t.CellRanges, true)
			.Add(t => t.OnRowSave, save => saves.Add(save))
			.Add(t => t.ChildContent, Columns(Name, Age with { Editable = true })));

		await cut.InvokeAsync(() => cut.Instance.PasteCells(1, 1, 3, 1, "50"));

		Assert.Equal(3, saves.Count);
		Assert.All(saves, save => Assert.Equal(50, Assert.Single(save.Changes).NewValue));
	}

	// ── Rows from a server, window by window ──

	[Fact]
	public async Task RemoteVirtualization_AsksForWindows_WithTheCount_AndPlaceholders()
	{
		List<MokaTableRequest> requests = [];
		TaskCompletionSource<MokaTableResult<Person>> pending = new();
		IRenderedComponent<MokaTable<Person>> cut = Render<MokaTable<Person>>(p => p
			.Add(t => t.Virtualize, true)
			.Add(t => t.Height, "300px")
			.Add(t => t.ServerData, request =>
			{
				requests.Add(request);
				return pending.Task;
			})
			.Add(t => t.ChildContent, DefaultColumns));

		cut.WaitForAssertion(() => Assert.NotEmpty(requests));
		Assert.NotEmpty(cut.FindAll(".moka-table-placeholder-row"));
		Assert.Empty(cut.FindAll(".moka-table-pager, .moka-pagination"));

		MokaTableRequest first = requests[0];
		Person[] window = Enumerable.Range(first.StartIndex, Math.Max(first.PageSize, 1))
			.Select(static i => new Person($"Row {i}", "T", i, "C")).ToArray();
		pending.SetResult(new MokaTableResult<Person> { Items = window, TotalItems = 1_000_000 });

		cut.WaitForAssertion(() => Assert.Equal("Row 0", ColumnTexts(cut, 0)[0]));
		Assert.Equal("1000001", cut.Find("table").GetAttribute("aria-rowcount"));
		Assert.Equal("1000000", cut.Find("table").GetAttribute("data-row-count"));
		Assert.Equal("2", DataRows(cut)[0].GetAttribute("aria-rowindex"));
		await Task.CompletedTask;
	}

	[Fact]
	public async Task RemoteVirtualization_PassesVirtualizesCancellation_ToTheSource()
	{
		List<MokaTableRequest> requests = [];
		IRenderedComponent<MokaTable<Person>> cut = Render<MokaTable<Person>>(p => p
			.Add(t => t.Virtualize, true)
			.Add(t => t.Height, "300px")
			.Add(t => t.Searchable, true)
			.Add(t => t.ServerData, request =>
			{
				requests.Add(request);

				// The first window never comes; the one asked for after it does.
				return requests.Count == 1
					? new TaskCompletionSource<MokaTableResult<Person>>().Task
					: Task.FromResult(new MokaTableResult<Person> { Items = People, TotalItems = People.Length });
			})
			.Add(t => t.ChildContent, DefaultColumns));
		cut.WaitForAssertion(() => Assert.NotEmpty(requests));

		await cut.InvokeAsync(() => cut.Instance.ReloadAsync());

		cut.WaitForAssertion(() => Assert.True(requests.Count >= 2));
		Assert.True(requests[0].CancellationToken.IsCancellationRequested);
		Assert.False(requests[^1].CancellationToken.IsCancellationRequested);
	}

	// ── Streamed rows ──

	[Fact]
	public async Task StreamedRows_ShowAsTheyArrive_WithALoadingRow_UntilTheEnd()
	{
		Channel channel = new();
		IRenderedComponent<MokaTable<Person>> cut = Render<MokaTable<Person>>(p => p
			.Add(t => t.ItemStream, channel.Read())
			.Add(t => t.ShowPagination, false)
			.Add(t => t.ChildContent, DefaultColumns));

		channel.Send(People[0]);
		channel.Send(People[1]);
		cut.WaitForAssertion(() => Assert.Equal(["Ada", "Grace"], ColumnTexts(cut, 0)));
		Assert.True(cut.Instance.IsStreaming);
		Assert.Contains("Loading more… 2 so far", cut.Find(".moka-table-stream-row").TextContent, StringComparison.Ordinal);
		Assert.Equal("-1", cut.Find("table").GetAttribute("aria-rowcount"));

		channel.Send(People[2]);
		channel.End();
		cut.WaitForAssertion(() => Assert.Empty(cut.FindAll(".moka-table-stream-row")));
		Assert.Equal(["Ada", "Grace", "Alan"], ColumnTexts(cut, 0));
		Assert.False(cut.Instance.IsStreaming);
		await Task.CompletedTask;
	}

	[Fact]
	public async Task Stop_CancelsTheStream_AndKeepsTheRows()
	{
		Channel channel = new();
		IRenderedComponent<MokaTable<Person>> cut = Render<MokaTable<Person>>(p => p
			.Add(t => t.ItemStream, channel.Read())
			.Add(t => t.ChildContent, DefaultColumns));
		channel.Send(People[0]);
		cut.WaitForAssertion(() => Assert.Single(DataRows(cut)));

		await cut.Find(".moka-table-stream-row button").ClickAsync(new MouseEventArgs());

		cut.WaitForAssertion(() => Assert.True(channel.Cancelled));
		Assert.False(cut.Instance.IsStreaming);
		Assert.Equal(["Ada"], ColumnTexts(cut, 0));
	}

	[Fact]
	public void StreamedRows_AreSortedAsTheyArrive()
	{
		Channel channel = new();
		IRenderedComponent<MokaTable<Person>> cut = Render<MokaTable<Person>>(p => p
			.Add(t => t.ItemStream, channel.Read())
			.Add(t => t.State, SortedBy("Name", MokaSortDirection.Ascending))
			.Add(t => t.ChildContent, DefaultColumns));

		foreach (Person person in People)
		{
			channel.Send(person);
		}

		cut.WaitForAssertion(() => Assert.Equal(["Ada", "Alan", "Barbara", "Grace"], ColumnTexts(cut, 0)));
	}

	// ── Columns in view ──

	[Fact]
	public async Task ColumnVirtualization_DrawsTheWindowAndThePins_WithGapsForTheRest()
	{
		List<Col> columns = [Name with { Pin = MokaColumnPin.Start }];
		columns.AddRange(Enumerable.Range(1, 60).Select(static i => new Col($"C{i}", x => $"{x.Name}{i}")));
		IRenderedComponent<MokaTable<Person>> cut = Render<MokaTable<Person>>(p => p
			.Add(t => t.Items, People)
			.Add(t => t.VirtualizeColumns, true)
			.Add(t => t.ChildContent, Columns([.. columns])));

		Assert.Equal(61, cut.FindAll("thead th[data-col-index]").Count);
		Assert.Contains("moka-table--column-window", cut.Find("table").ClassList);
		IElement row = DataRows(cut)[0];
		Assert.Equal(13, row.QuerySelectorAll("td[data-col-index]").Length);
		IElement gap = row.QuerySelector(".moka-table-col-gap")!;
		Assert.Equal("48", gap.GetAttribute("colspan"));

		await cut.InvokeAsync(() => cut.Instance.OnColumnWindow(30, 35));

		row = DataRows(cut)[0];
		Assert.Equal(["0", "30", "31", "32", "33", "34", "35"],
			row.QuerySelectorAll("td[data-col-index]").Select(static c => c.GetAttribute("data-col-index")));
		Assert.Equal(["29", "25"], row.QuerySelectorAll(".moka-table-col-gap").Select(static g => g.GetAttribute("colspan")));
		Assert.Equal("32", row.QuerySelector("td[data-col-index='31']")!.GetAttribute("aria-colindex"));
	}

	[Fact]
	public void ColumnVirtualization_WatchesTheColumnsThroughTheScript()
	{
		BunitJSModuleInterop module = JSInterop.SetupModule(TableModule);
		module.Mode = JSRuntimeMode.Loose;

		Table(p => p.Add(t => t.VirtualizeColumns, true));

		module.VerifyInvoke("watchColumns");
	}

	// ── Fixes to phase 2 ──

	// The facet panel drew when the load started, before a server's facets arrived, and then waited for the next load.
	[Fact]
	public void FacetsFromAServer_ShowTheLoadTheyCameWith()
	{
		TaskCompletionSource<MokaTableResult<Person>> pending = new();
		IRenderedComponent<MokaTable<Person>> cut = Render<MokaTable<Person>>(p => p
			.Add(t => t.FilterControls, MokaTableFilterControls.Facets)
			.Add(t => t.FacetsFromServer, true)
			.Add(t => t.ChildContent, Columns(Name, Team with { Filterable = true, FilterType = MokaColumnFilterType.Select }))
			.Add(t => t.ServerData, _ => pending.Task));

		pending.SetResult(new MokaTableResult<Person>
		{
			Items = People,
			TotalItems = 40,
			Facets = new Dictionary<string, IReadOnlyList<MokaFacetValue>>
			{
				["Team"] = [new MokaFacetValue("Engineering", "Engineering", 25), new MokaFacetValue("Sales", "Sales", 15)]
			}
		});

		cut.WaitForAssertion(() => Assert.Equal(["25", "15"], cut.FindAll(".moka-table-facet-count").Select(static e => e.TextContent)));
	}

	[Fact]
	public void QuickFiltersPassedLater_AreCountedAtOnce()
	{
		IReadOnlyList<MokaTableQuickFilter> quick =
		[
			new("Engineering", [new MokaTableFilter("Team", MokaFilterOperator.Is, "Engineering")]),
			new("Over 40", [new MokaTableFilter("Age", MokaFilterOperator.GreaterThan, "40")])
		];
		IRenderedComponent<MokaTable<Person>> cut = Table();

		cut.Render(p => p.Add(t => t.QuickFilters, quick));

		Assert.Equal([4, 2, 2], cut.FindComponents<MokaSegment<string>>().Select(static s => s.Instance.Count ?? -1));
	}

	[Fact]
	public void AColumnWithNoTitle_IsNamedForItsKindOrItsPlace()
	{
		IRenderedComponent<MokaTable<Person>> cut = Render<MokaTable<Person>>(p => p
			.Add(t => t.Items, People)
			.Add(t => t.ColumnMenu, true)
			.Add(t => t.ChildContent, (RenderFragment)(builder =>
			{
				Columns(Name, new Col(null, static x => x.Team))(builder);
				builder.OpenComponent<MokaActionsColumn<Person>>(50);
				builder.AddComponentParameter(51, "Actions", (IReadOnlyList<MokaTableAction<Person>>)[]);
				builder.CloseComponent();
			})));

		Assert.Equal(["Name column options", "Options for column 2", "Actions column options"],
			cut.FindAll(".moka-table-menu-btn").Select(static b => b.GetAttribute("aria-label")));
		Assert.DoesNotContain("Unnamed", cut.Markup, StringComparison.Ordinal);
	}

	private IRenderedComponent<MokaTable<Person>> Table(Action<ComponentParameterCollectionBuilder<MokaTable<Person>>>? configure = null) =>
		Render<MokaTable<Person>>(p =>
		{
			p.Add(t => t.Items, People).Add(t => t.ChildContent, DefaultColumns);
			configure?.Invoke(p);
		});

	internal sealed record Stock(string Name, double Price, DateTime Arrived, bool Open);

	// Rows sent one at a time, as a slow source sends them.
	private sealed class Channel
	{
		private readonly System.Threading.Channels.Channel<Person> _rows = System.Threading.Channels.Channel.CreateUnbounded<Person>();

		public bool Cancelled { get; private set; }

		public void Send(Person person) => _rows.Writer.TryWrite(person);

		public void End() => _rows.Writer.TryComplete();

		public async IAsyncEnumerable<Person> Read([EnumeratorCancellation] CancellationToken cancellationToken = default)
		{
			using CancellationTokenRegistration registration = cancellationToken.Register(() => Cancelled = true);
			await foreach (Person person in _rows.Reader.ReadAllAsync(cancellationToken))
			{
				yield return person;
			}
		}
	}

	private static (List<string> Entries, XDocument Workbook, XDocument Sheet) ReadWorkbook(byte[] bytes)
	{
		using ZipArchive zip = new(new MemoryStream(bytes));
		using Stream workbook = zip.GetEntry("xl/workbook.xml")!.Open();
		using Stream sheet = zip.GetEntry("xl/worksheets/sheet1.xml")!.Open();
		return (zip.Entries.Select(static e => e.FullName).ToList(), XDocument.Load(workbook), XDocument.Load(sheet));
	}
}
