using Bunit;
using Moka.Red.Data.Table;

namespace Moka.Red.Data.Tests.Components;

/// <summary>
///     The first tests this package has had. Several features below were declared as
///     parameters but had no UI behind them at all: the filter row, inline editing,
///     row reordering and the column resize handle were all unreachable.
/// </summary>
public class MokaTableTests : BunitContext
{
	public MokaTableTests() => JSInterop.Mode = JSRuntimeMode.Loose;

	private static readonly Person[] People =
	[
		new("Ada", "Engineering", 36),
		new("Grace", "Engineering", 45),
		new("Alan", "Research", 41)
	];

	[Fact]
	public void Renders_ARowPerItem()
	{
		IRenderedComponent<MokaTable<Person>> cut = RenderTable();

		Assert.Equal(People.Length, cut.FindAll("tbody tr").Count);
	}

	[Fact]
	public void Renders_AHeaderCellPerVisibleColumn()
	{
		IRenderedComponent<MokaTable<Person>> cut = RenderTable();

		Assert.Equal(3, cut.FindAll("thead tr:first-child th").Count);
	}

	[Fact]
	public void HeaderAndBodyCellCounts_Match_WhenRowReorderingIsOn()
	{
		// RowReorderable rendered a header cell with no matching body cell, so every data
		// row was one short of the header.
		IRenderedComponent<MokaTable<Person>> cut = RenderTable(p => p.Add(t => t.RowReorderable, true));

		int headerCells = cut.FindAll("thead tr:first-child th").Count;
		int bodyCells = cut.FindAll("tbody tr:first-child td").Count;

		Assert.Equal(headerCells, bodyCells);
	}

	[Fact]
	public void FilterRow_Renders_WhenShowFiltersIsOn()
	{
		// ApplyColumnFilters ran but nothing could ever populate the filter dictionary.
		IRenderedComponent<MokaTable<Person>> cut = RenderTable(p => p.Add(t => t.ShowFilters, true));

		Assert.NotEmpty(cut.FindAll(".moka-table-filter-row"));
	}

	[Fact]
	public void FilterRow_IsAbsent_ByDefault()
	{
		IRenderedComponent<MokaTable<Person>> cut = RenderTable();

		Assert.Empty(cut.FindAll(".moka-table-filter-row"));
	}

	[Fact]
	public void ResizeHandle_Renders_ForResizableColumns()
	{
		// The JS looked for .moka-table-resize-handle, which existed in no razor or css file.
		IRenderedComponent<MokaTable<Person>> cut = RenderTable();

		Assert.NotEmpty(cut.FindAll(".moka-table-resize-handle"));
	}

	[Fact]
	public void SearchInput_Renders_WhenSearchable()
	{
		IRenderedComponent<MokaTable<Person>> cut = RenderTable(p => p.Add(t => t.Searchable, true));

		Assert.NotNull(cut.Find(".moka-table-search input"));
	}

	[Fact]
	public void SelectionCheckboxes_Render_WhenSelectable()
	{
		IRenderedComponent<MokaTable<Person>> cut = RenderTable(p => p.Add(t => t.Selectable, true));

		// One per row plus the header select-all.
		Assert.Equal(People.Length + 1, cut.FindAll("input[type='checkbox']").Count);
	}

	[Fact]
	public async Task SelectedItemsChanged_HandsBackANewSet()
	{
		// The component used to alias the caller's HashSet and pass the same reference back,
		// so a parent comparing references never saw a change.
		var original = new HashSet<Person>();
		HashSet<Person>? received = null;

		IRenderedComponent<MokaTable<Person>> cut = RenderTable(p => p
			.Add(t => t.Selectable, true)
			.Add(t => t.SelectedItems, original)
			.Add(t => t.SelectedItemsChanged, s => received = s));

		await cut.FindAll("tbody input[type='checkbox']")[0].ChangeAsync(new() { Value = true });

		Assert.NotNull(received);
		Assert.NotSame(original, received);
		Assert.Empty(original);
	}

	[Fact]
	public void EmptyContent_Renders_WhenThereAreNoItems()
	{
		IRenderedComponent<MokaTable<Person>> cut = Render<MokaTable<Person>>(p => p
			.Add(t => t.Items, [])
			.Add(t => t.ChildContent, Columns));

		Assert.NotEmpty(cut.FindAll(".moka-table-empty"));
	}

	[Fact]
	public async Task ServerData_ReceivesTheRequestedPage()
	{
		MokaTableState? seen = null;

		Render<MokaTable<Person>>(p => p
			.Add(t => t.ServerData, state =>
			{
				seen = state;
				return Task.FromResult(new MokaTableResult<Person>
				{
					Items = People,
					TotalItems = People.Length
				});
			})
			.Add(t => t.PageSize, 25)
			.Add(t => t.ChildContent, Columns));

		await Task.Yield();

		Assert.NotNull(seen);
		Assert.Equal(1, seen.Page);
		Assert.Equal(25, seen.PageSize);
	}

	[Fact]
	public void ServerDataState_CarriesColumnFilters()
	{
		// MokaTableState had no filter field, so server-side filtering was impossible.
		var state = new MokaTableState();

		Assert.NotNull(state.ColumnFilters);
		Assert.Empty(state.ColumnFilters);
	}

	[Fact]
	public void ExportContext_DefaultsToACompleteSet()
	{
		var context = new MokaTableExportContext<Person>
		{
			Items = People,
			Columns = []
		};

		Assert.True(context.IsCompleteSet);
	}

	private IRenderedComponent<MokaTable<Person>> RenderTable(
		Action<ComponentParameterCollectionBuilder<MokaTable<Person>>>? extra = null) =>
		Render<MokaTable<Person>>(p =>
		{
			p.Add(t => t.Items, People);
			p.Add(t => t.ChildContent, Columns);
			extra?.Invoke(p);
		});

	private static void Columns(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder builder)
	{
		Column(builder, 0, "Name", static x => x.Name);
		Column(builder, 10, "Team", static x => x.Team);
		Column(builder, 20, "Age", static x => x.Age);
	}

	private static void Column(
		Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder builder,
		int seq,
		string title,
		Func<Person, object?> field)
	{
		builder.OpenComponent<MokaColumn<Person>>(seq);
		builder.AddAttribute(seq + 1, nameof(MokaColumn<Person>.Title), title);
		builder.AddAttribute(seq + 2, nameof(MokaColumn<Person>.Field), field);
		builder.AddAttribute(seq + 3, nameof(MokaColumn<Person>.Filterable), true);
		builder.CloseComponent();
	}

	private sealed record Person(string Name, string Team, int Age);
}
