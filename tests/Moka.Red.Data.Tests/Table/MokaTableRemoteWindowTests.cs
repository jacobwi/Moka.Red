using System.Globalization;
using System.Reflection;
using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Moka.Red.Data.Table;
using static Moka.Red.Data.Tests.Components.TableKit;

namespace Moka.Red.Data.Tests.Table;

// Rows a server sends window by window: what later windows ask for, the totals and list values of the whole result,
// picking every matching row and runs past the window, and grouped windows of headings and rows.
public class MokaTableRemoteWindowTests : BunitContext
{
	private const float RowHeight = 36f;

	public MokaTableRemoteWindowTests() => JSInterop.Mode = JSRuntimeMode.Loose;

	// ── What later windows ask for ──

	[Fact]
	public async Task LaterWindows_AskForRowsOnly_AndKeepTheCountAndTotals()
	{
		Source source = new(100_000);
		IRenderedComponent<MokaTable<Person>> cut = Remote(source, Columns(Name, Team, Age with { Aggregate = MokaAggregateType.Sum, AggregateFormat = "0" }));
		cut.WaitForAssertion(() => Assert.Equal("Row 0", ColumnTexts(cut, 0)[0]));
		Assert.False(source.Requests[0].RowsOnly);
		Assert.Equal("12345", cut.Find(".moka-table-aggregate-value").TextContent.Trim());

		await ScrollToAsync(cut, 5_000);

		cut.WaitForAssertion(() => Assert.Contains("Row 5000", ColumnTexts(cut, 0)));
		Assert.True(source.Requests[^1].RowsOnly);
		Assert.Equal("100001", cut.Find("table").GetAttribute("aria-rowcount"));
		Assert.Equal("12345", cut.Find(".moka-table-aggregate-value").TextContent.Trim());
	}

	[Fact]
	public void WithoutAServersTotal_TheFooterLeavesItOut_RatherThanTotallingAWindow()
	{
		Source source = new(1_000) { SendTotals = false };
		IRenderedComponent<MokaTable<Person>> cut = Remote(source, Columns(Name, Age with { Aggregate = MokaAggregateType.Sum }));

		cut.WaitForAssertion(() => Assert.Equal("Row 0", ColumnTexts(cut, 0)[0]));
		Assert.Equal("", cut.Find(".moka-table-aggregate-value").TextContent.Trim());
	}

	[Fact]
	public async Task ListValues_GrowWithTheWindows_WithoutTheSourcesFacets()
	{
		Source source = new(1_000) { TeamOf = static i => i < 500 ? "Early" : "Late" };
		IRenderedComponent<MokaTable<Person>> cut = Remote(source,
			Columns(Name, Team with { Filterable = true, FilterType = MokaColumnFilterType.MultiSelect }));
		cut.WaitForAssertion(() => Assert.Equal("Row 0", ColumnTexts(cut, 0)[0]));
		MokaColumn<Person> team = cut.FindComponents<MokaColumn<Person>>()[1].Instance;
		Assert.Equal(["Early"], cut.Instance.ChoicesFor(team).Select(static v => v.Value));

		await ScrollToAsync(cut, 800);

		cut.WaitForAssertion(() => Assert.Equal(["Early", "Late"], cut.Instance.ChoicesFor(team).Select(static v => v.Value)));
		await ScrollToAsync(cut, 100);
		cut.WaitForAssertion(() => Assert.Contains("Row 100", ColumnTexts(cut, 0)));
		Assert.Equal(["Early", "Late"], cut.Instance.ChoicesFor(team).Select(static v => v.Value));
	}

	[Fact]
	public async Task TheSourcesFacets_AreAskedForOnce_AndKept()
	{
		Source source = new(1_000);
		IRenderedComponent<MokaTable<Person>> cut = Remote(source,
			Columns(Name, Team with { Filterable = true, FilterType = MokaColumnFilterType.MultiSelect }),
			p => p.Add(t => t.FacetsFromServer, true));
		cut.WaitForAssertion(() => Assert.Equal("Row 0", ColumnTexts(cut, 0)[0]));
		MokaColumn<Person> team = cut.FindComponents<MokaColumn<Person>>()[1].Instance;
		Assert.Equal([("Even", 334), ("Odd", 666)], cut.Instance.ChoicesFor(team).Select(static v => (v.Value, v.Count)));

		await ScrollToAsync(cut, 600);

		cut.WaitForAssertion(() => Assert.Contains("Row 600", ColumnTexts(cut, 0)));
		Assert.Equal(1, source.Requests.Count(static r => !r.RowsOnly));
		Assert.Equal([("Even", 334), ("Odd", 666)], cut.Instance.ChoicesFor(team).Select(static v => (v.Value, v.Count)));
	}

	[Fact]
	public async Task ASourceThatDoesntCount_KeepsTheScrollItGrew_WhenScrolledBack()
	{
		Source source = new(100_000) { Counts = false };
		IRenderedComponent<MokaTable<Person>> cut = Remote(source, DefaultColumns);
		cut.WaitForAssertion(() => Assert.Equal("Row 0", ColumnTexts(cut, 0)[0]));
		int first = RowCount(cut);

		await ScrollToAsync(cut, first - 10);
		cut.WaitForAssertion(() => Assert.True(RowCount(cut) > first));
		int grown = RowCount(cut);
		await ScrollToAsync(cut, 0);

		cut.WaitForAssertion(() => Assert.Equal("Row 0", ColumnTexts(cut, 0)[0]));
		Assert.Equal(grown, RowCount(cut));
		Assert.Equal("-1", cut.Find("table").GetAttribute("aria-rowcount"));
	}

	// ── Every matching row ──

	[Fact]
	public async Task SelectAllMatching_PicksEveryMatchingRow_ButThoseUntickedSince()
	{
		Source source = new(100_000);
		List<MokaTableSelection<Person>> seen = [];
		IRenderedComponent<MokaTable<Person>> cut = Remote(source, DefaultColumns, p => p
			.Add(t => t.Selectable, true)
			.Add(t => t.SelectAllMatching, true)
			.Add(t => t.SelectionActions, static _ => static builder => builder.AddContent(0, "Delete"))
			.Add(t => t.SelectionChanged, selection => seen.Add(selection)));
		cut.WaitForAssertion(() => Assert.Equal("Row 0", ColumnTexts(cut, 0)[0]));
		Assert.Equal("Select all matching rows", cut.Find("thead input[type=checkbox]").GetAttribute("aria-label"));

		await cut.Find("thead input[type=checkbox]").ChangeAsync(new ChangeEventArgs { Value = true });

		cut.WaitForAssertion(() => Assert.True(Assert.Single(seen).AllMatching));
		Assert.Equal(100_000, seen[0].Count);
		Assert.Null(seen[0].Matching!.Search);
		Assert.Equal(100_000, seen[0].Matching!.PageSize);
		Assert.All(RowBoxes(cut), static box => Assert.True(box.HasAttribute("checked")));
		Assert.Equal("100,000 selected", cut.Find(".moka-table-selection-count").TextContent.Trim());

		await RowBoxes(cut)[2].ChangeAsync(new ChangeEventArgs { Value = false });

		cut.WaitForAssertion(() => Assert.Equal(2, seen.Count));
		Assert.Equal("Row 2", Assert.Single(seen[1].Except).Name);
		Assert.Equal(99_999, seen[1].Count);
		Assert.Equal("99,999 selected", cut.Find(".moka-table-selection-count").TextContent.Trim());

		await ScrollToAsync(cut, 50_000);

		cut.WaitForAssertion(() => Assert.Contains("Row 50000", ColumnTexts(cut, 0)));
		Assert.All(RowBoxes(cut), static box => Assert.True(box.HasAttribute("checked")));
		Assert.Empty(cut.Instance.CurrentSelection().Items);
		Assert.Equal(99_999, cut.Instance.CurrentSelection().Count);
	}

	[Fact]
	public async Task SelectAllMatching_EndsWithANewSearch()
	{
		Source source = new(1_000);
		List<MokaTableSelection<Person>> seen = [];
		IRenderedComponent<MokaTable<Person>> cut = Remote(source, DefaultColumns, p => p
			.Add(t => t.Selectable, true)
			.Add(t => t.SelectAllMatching, true)
			.Add(t => t.Search, null)
			.Add(t => t.SelectionChanged, selection => seen.Add(selection)));
		cut.WaitForAssertion(() => Assert.Equal("Row 0", ColumnTexts(cut, 0)[0]));
		await cut.Find("thead input[type=checkbox]").ChangeAsync(new ChangeEventArgs { Value = true });
		cut.WaitForAssertion(() => Assert.Single(seen));

		cut.Render(p => p.Add(t => t.Search, "Row"));

		cut.WaitForAssertion(() => Assert.False(seen[^1].AllMatching));
		Assert.Equal(0, seen[^1].Count);
		cut.WaitForAssertion(() => Assert.All(RowBoxes(cut), static box => Assert.False(box.HasAttribute("checked"))));
	}

	[Fact]
	public async Task WithoutSelectAllMatching_SelectAllPicksTheRowsLoaded()
	{
		Source source = new(1_000);
		HashSet<Person>? picked = null;
		IRenderedComponent<MokaTable<Person>> cut = Remote(source, DefaultColumns, p => p
			.Add(t => t.Selectable, true)
			.Add(t => t.SelectedItemsChanged, items => picked = items));
		cut.WaitForAssertion(() => Assert.Equal("Row 0", ColumnTexts(cut, 0)[0]));

		await cut.Find("thead input[type=checkbox]").ChangeAsync(new ChangeEventArgs { Value = true });

		Assert.Equal(DataRows(cut).Count, picked!.Count);
		Assert.False(cut.Instance.CurrentSelection().AllMatching);
	}

	// ── A run past the window ──

	[Fact]
	public async Task ARunPastTheWindow_AsksForTheRowsBetween_AndPicksThem()
	{
		Source source = new(100_000);
		HashSet<Person>? picked = null;
		IRenderedComponent<MokaTable<Person>> cut = Remote(source, DefaultColumns, p => p
			.Add(t => t.Selectable, true)
			.Add(t => t.ItemKey, static x => x.Name)
			.Add(t => t.SelectedItemsChanged, items => picked = items));
		cut.WaitForAssertion(() => Assert.Equal("Row 0", ColumnTexts(cut, 0)[0]));
		await cut.InvokeAsync(() => cut.Instance.OnCellKey(1, 0, " "));

		await ScrollToAsync(cut, 5_000);
		cut.WaitForAssertion(() => Assert.Contains("Row 5000", ColumnTexts(cut, 0)));
		int asked = source.Requests.Count;
		await cut.InvokeAsync(() => cut.Instance.OnCellKey(5_000, 0, "Shift+ "));

		cut.WaitForAssertion(() => Assert.Equal(5_000, picked!.Count));
		MokaTableRequest between = Assert.Single(source.Requests.Skip(asked));
		Assert.True(between.RowsOnly);
		Assert.Equal(1, between.StartIndex);
		Assert.Equal(DataRows(cut).Min(static r => int.Parse(r.GetAttribute("data-row-index")!, CultureInfo.InvariantCulture)) - 1,
			between.PageSize);
		Assert.Contains(picked!, static x => x.Name == "Row 1");
		Assert.Contains(picked!, static x => x.Name == "Row 5000");
		Assert.DoesNotContain(picked!, static x => x.Name == "Row 5001");
	}

	// ── Grouped windows ──

	[Fact]
	public void GroupedRows_ComeInWindowsOfHeadingsAndRows_FromTheGroupTotals()
	{
		Source source = new(10_000) { TeamOf = Teams };
		IRenderedComponent<MokaTable<Person>> cut = Remote(source, DefaultColumns, p => p
			.Add(t => t.GroupBy, "Team"));

		cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll(".moka-table-group-row")));
		IElement table = cut.Find("table");
		Assert.Equal("treegrid", table.GetAttribute("role"));
		Assert.Equal((1 + 3 + 10_000).ToString(CultureInfo.InvariantCulture), table.GetAttribute("aria-rowcount"));
		Assert.Contains("A", cut.Find(".moka-table-group-row").TextContent, StringComparison.Ordinal);
		Assert.Equal("Row 0", ColumnTexts(cut, 0)[0]);
		Assert.Equal("3", DataRows(cut)[0].GetAttribute("aria-rowindex"));
		Assert.Empty(cut.FindAll(".moka-table-pager, .moka-pagination"));
		Assert.Equal(["Team"], source.Requests[0].Groups);
	}

	[Fact]
	public async Task AFoldedGroup_LeavesItsRowsOutOfTheLines_AndTheRequests()
	{
		Source source = new(10_000) { TeamOf = Teams };
		IRenderedComponent<MokaTable<Person>> cut = Remote(source, DefaultColumns, p => p
			.Add(t => t.GroupBy, "Team")
			.Add(t => t.InitiallyCollapsedGroups, ["A"]));
		cut.WaitForAssertion(() => Assert.Equal(3, cut.FindAll(".moka-table-group-row").Count));

		Assert.Equal("Row 40", ColumnTexts(cut, 0)[0]);
		Assert.Equal((1 + 3 + 10_000 - 40).ToString(CultureInfo.InvariantCulture), cut.Find("table").GetAttribute("aria-rowcount"));
		Assert.False(source.Requests[0].RowsOnly);
		Assert.All(source.Requests.Skip(1), static r => Assert.True(r.RowsOnly && r.StartIndex >= 40));
		Assert.Equal("4", DataRows(cut)[0].GetAttribute("aria-rowindex"));

		await ScrollToAsync(cut, 1_000);

		cut.WaitForAssertion(() => Assert.True(FirstRowIndex(cut) > 900));
		Assert.True(source.Requests[^1].RowsOnly);
		Assert.Equal(FirstRowIndex(cut), source.Requests[^1].StartIndex);
	}

	[Fact]
	public async Task FoldingAGroup_DrawsTheLinesAgain_FromTheRowsTheWindowHas()
	{
		Source source = new(10_000) { TeamOf = Teams };
		IRenderedComponent<MokaTable<Person>> cut = Remote(source, DefaultColumns, p => p.Add(t => t.GroupBy, "Team"));
		cut.WaitForAssertion(() => Assert.Equal("Row 0", ColumnTexts(cut, 0)[0]));
		int asked = source.Requests.Count;

		await cut.Find(".moka-table-group-toggle").ClickAsync(new());

		cut.WaitForAssertion(() => Assert.Equal("Row 40", ColumnTexts(cut, 0)[0]));
		Assert.Equal("false", cut.Find(".moka-table-group-toggle").GetAttribute("aria-expanded"));
		Assert.All(source.Requests.Skip(asked), static r => Assert.True(r.RowsOnly && r.StartIndex >= 40));
	}

	[Fact]
	public async Task GroupTotalsThatDontCoverTheResult_PageInstead()
	{
		Source source = new(500) { TeamOf = Teams, WholeGroupTotals = false };
		IRenderedComponent<MokaTable<Person>> cut = Remote(source, DefaultColumns, p => p.Add(t => t.GroupBy, "Team"));

		cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll(".moka-pagination, .moka-table-pager")));
		Assert.NotEmpty(cut.FindAll(".moka-table-group-row"));
		Assert.Empty(cut.FindAll(".moka-table-placeholder-row"));
		await cut.InvokeAsync(() => cut.Instance.ReloadAsync());
		Assert.False(source.Requests[^1].RowsOnly);
		Assert.Equal(1, source.Requests[^1].Page);
	}

	[Fact]
	public void AGroupHeading_ShowsTheServersTotal_AndNothingFromTheWindow()
	{
		Source source = new(10_000) { TeamOf = Teams };
		IRenderedComponent<MokaTable<Person>> cut = Remote(source,
			Columns(Name, Team, Age with { Aggregate = MokaAggregateType.Sum, AggregateFormat = "0" }), p => p.Add(t => t.GroupBy, "Team"));

		cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll(".moka-table-group-total")));
		Assert.Equal("40", cut.FindAll(".moka-table-group-total")[0].TextContent.Trim());
	}

	// The problems row and the headings are drawn into the builder Virtualize hands them, not the table's own, which drew
	// them only while the table rendered and lost them under Virtualize.
	[Fact]
	public async Task ASaveThatThrows_ShowsWhyOverTheRow_InAVirtualizedTable()
	{
		IRenderedComponent<MokaTable<Person>> cut = Render<MokaTable<Person>>(p => p
			.Add(t => t.Items, People)
			.Add(t => t.Virtualize, true)
			.Add(t => t.ShowPagination, false)
			.Add(t => t.Height, "300px")
			.Add(t => t.EditMode, MokaTableEditMode.Row)
			.Add(t => t.OnRowSave, _ => throw new InvalidOperationException("The server is away"))
			.Add(t => t.ChildContent, Columns(Name with { Editable = true }, Age with { Editable = true })));

		await cut.InvokeAsync(() => cut.Instance.StartEditAsync(People[0]));
		await cut.FindAll("td[data-editing] input")[0].InputAsync(new ChangeEventArgs { Value = "Ada L." });
		await cut.InvokeAsync(() => cut.Instance.SaveEditAsync());

		Assert.Contains("The server is away", cut.Find(".moka-table-edit-problems [role='alert']").TextContent, StringComparison.Ordinal);
	}

	[Fact]
	public void RemoteWindows_MarkTheTable_ForTheKeysThatWaitForRows()
	{
		IRenderedComponent<MokaTable<Person>> cut = Remote(new Source(100), DefaultColumns);

		cut.WaitForAssertion(() => Assert.Equal("Row 0", ColumnTexts(cut, 0)[0]));
		Assert.True(cut.Find("table").HasAttribute("data-moka-remote"));
		Assert.Equal("100", cut.Find("table").GetAttribute("data-row-count"));
	}

	// ── Helpers ──

	private static string Teams(int row) => row < 40 ? "A" : row < 45 ? "B" : "C";

	private IRenderedComponent<MokaTable<Person>> Remote(Source source, RenderFragment columns,
		Action<ComponentParameterCollectionBuilder<MokaTable<Person>>>? more = null) =>
		Render<MokaTable<Person>>(p =>
		{
			p.Add(t => t.Virtualize, true)
				.Add(t => t.Height, "300px")
				.Add(t => t.ServerData, source.LoadAsync)
				.Add(t => t.ChildContent, columns);
			more?.Invoke(p);
		});

	private static int RowCount(IRenderedComponent<MokaTable<Person>> cut) =>
		int.Parse(cut.Find("table").GetAttribute("data-row-count")!, CultureInfo.InvariantCulture);

	private static int FirstRowIndex(IRenderedComponent<MokaTable<Person>> cut) =>
		int.Parse(DataRows(cut)[0].GetAttribute("data-row-index")!, CultureInfo.InvariantCulture);

	private static IReadOnlyList<IElement> RowBoxes(IRenderedComponent<MokaTable<Person>> cut) =>
		cut.FindAll("tbody tr[data-row-index] td.moka-table-cell--checkbox input");

	// Scrolls the way Virtualize's script reports it: the spacer before the rows is in view, as tall as the rows above.
	private async Task ScrollToAsync(IRenderedComponent<MokaTable<Person>> cut, int line)
	{
		object reference = JSInterop.Invocations["Blazor._internal.Virtualize.init"][^1].Arguments[0]!;
		object interop = reference.GetType().GetProperty("Value")!.GetValue(reference)!;
		MethodInfo before = interop.GetType().GetMethod("OnSpacerBeforeVisible")!;
		await cut.InvokeAsync(() => before.Invoke(interop, [line * RowHeight, 0f, 300f]));
	}

	// A result of rows "Row 0" upward, ordered by team when grouped, as a database would answer.
	private sealed class Source(int total)
	{
		public List<MokaTableRequest> Requests { get; } = [];

		public Func<int, string> TeamOf { get; init; } = static i => i % 3 == 0 ? "Even" : "Odd";

		public bool SendTotals { get; init; } = true;

		public bool Counts { get; init; } = true;

		public bool WholeGroupTotals { get; init; } = true;

		public Task<MokaTableResult<Person>> LoadAsync(MokaTableRequest request)
		{
			Requests.Add(request);
			int count = request.Search is null ? total : total / 2;
			int first = Math.Min(request.StartIndex, count);
			List<Person> items = Enumerable.Range(first, Math.Min(request.PageSize, count - first))
				.Select(i => request.Search is null ? i : i * 2)
				.Select(i => new Person($"Row {i}", TeamOf(i), i, "City"))
				.ToList();
			bool whole = !request.RowsOnly;
			return Task.FromResult(new MokaTableResult<Person>
			{
				Items = items,
				TotalItems = whole && Counts ? count : null,
				Totals = whole && SendTotals && request.Aggregates.Count > 0 ? new Dictionary<string, double> { ["Age"] = 12_345 } : null,
				Facets = whole && request.Facets.Count > 0
					? new Dictionary<string, IReadOnlyList<MokaFacetValue>>
					{
						["Team"] = [new MokaFacetValue("Even", "Even", (count + 2) / 3), new MokaFacetValue("Odd", "Odd", count - (count + 2) / 3)]
					}
					: null,
				GroupTotals = whole && request.Groups.Count > 0 ? GroupTotals(count, WholeGroupTotals ? count : first + items.Count) : null
			});
		}

		private List<MokaTableGroupTotals> GroupTotals(int count, int upTo)
		{
			List<MokaTableGroupTotals> groups = [];
			int start = 0;
			for (int row = 1; row <= upTo; row++)
			{
				if (row == upTo || TeamOf(row) != TeamOf(start))
				{
					int size = row - start;
					groups.Add(new MokaTableGroupTotals([TeamOf(start)], size)
					{
						Totals = new Dictionary<string, double> { ["Age"] = size }
					});
					start = row;
				}
			}

			return count == 0 ? [] : groups;
		}
	}
}
