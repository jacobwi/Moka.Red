using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Moka.Red.Data.Table;
using static Moka.Red.Data.Tests.Components.TableKit;

namespace Moka.Red.Data.Tests.Components;

// Sorts set by the parent, sorts set by header clicks, and column comparers.
public class MokaTableSortTests : BunitContext
{
	public MokaTableSortTests() => JSInterop.Mode = JSRuntimeMode.Loose;

	// The rows followed a sort from the parent, but the header icons read the sort descriptors,
	// which only header clicks filled in.
	[Fact]
	public void SortFromTheParent_ShowsOnTheHeader()
	{
		IRenderedComponent<MokaTable<Person>> cut = RenderTable();

		cut.Render(p => p
			.Add(t => t.SortColumn, "Age")
			.Add(t => t.SortDirection, MokaSortDirection.Descending));

		Assert.Equal(["Grace", "Alan", "Ada", "Barbara"], ColumnTexts(cut, 0));
		Assert.NotNull(HeaderCell(cut, "Age").QuerySelector(".moka-table-sort-icon--active"));
	}

	// Client-side rows first load before the columns register, so a sort passed from the start
	// found no column to order by and the rows stayed in their original order.
	[Fact]
	public void SortFromTheParent_OnTheFirstRender_SortsTheRows()
	{
		IRenderedComponent<MokaTable<Person>> cut = RenderTable(p => p
			.Add(t => t.SortColumn, "Age")
			.Add(t => t.SortDirection, MokaSortDirection.Descending));

		Assert.Equal(["Grace", "Alan", "Ada", "Barbara"], ColumnTexts(cut, 0));
		Assert.NotNull(HeaderCell(cut, "Age").QuerySelector(".moka-table-sort-icon--active"));
	}

	// After a header click the rows sorted by the descriptors alone, so a new sort from the parent
	// changed the reported state and nothing on screen.
	[Fact]
	public async Task NewSortFromTheParent_WinsAfterAHeaderClick()
	{
		IRenderedComponent<MokaTable<Person>> cut = RenderTable(p => p
			.Add(t => t.SortColumn, "Age")
			.Add(t => t.SortDirection, MokaSortDirection.Ascending));
		await HeaderCell(cut, "Name").ClickAsync(new MouseEventArgs());
		Assert.Equal(["Ada", "Alan", "Barbara", "Grace"], ColumnTexts(cut, 0));

		cut.Render(p => p
			.Add(t => t.SortColumn, "City")
			.Add(t => t.SortDirection, MokaSortDirection.Descending));

		Assert.Equal(["Paris", "Madrid", "London", "Berlin"], ColumnTexts(cut, 3));
		Assert.NotNull(HeaderCell(cut, "City").QuerySelector(".moka-table-sort-icon--active"));
		Assert.Null(HeaderCell(cut, "Name").QuerySelector(".moka-table-sort-icon--active"));
	}

	// The parent passing the same values back is not a new sort, so an unbound parent re-render
	// keeps the user's header click.
	[Fact]
	public async Task SameSortFromTheParent_KeepsTheHeaderClick()
	{
		IRenderedComponent<MokaTable<Person>> cut = RenderTable(p => p
			.Add(t => t.SortColumn, "Age")
			.Add(t => t.SortDirection, MokaSortDirection.Ascending));
		await HeaderCell(cut, "Name").ClickAsync(new MouseEventArgs());

		cut.Render(p => p
			.Add(t => t.SortColumn, "Age")
			.Add(t => t.SortDirection, MokaSortDirection.Ascending));

		Assert.Equal(["Ada", "Alan", "Barbara", "Grace"], ColumnTexts(cut, 0));
	}

	// @bind-SortColumn hands back what the table just reported. Treating that as a new value made
	// every header click load the rows a second time.
	[Fact]
	public async Task BoundSort_LoadsOncePerHeaderClick()
	{
		int loads = 0;
		IRenderedComponent<BoundSortHost> host = Render<BoundSortHost>(p => p
			.Add(h => h.ServerData, _ =>
			{
				loads++;
				return Task.FromResult(new MokaTableResult<Person> { Items = People, TotalItems = People.Length });
			}));
		IRenderedComponent<MokaTable<Person>> cut = host.FindComponent<MokaTable<Person>>();
		loads = 0;

		await HeaderCell(cut, "Name").ClickAsync(new MouseEventArgs());

		Assert.Equal(1, loads);
		Assert.Equal("Name", host.Instance.SortColumn);
		Assert.Equal(MokaSortDirection.Ascending, host.Instance.SortDirection);
	}

	// The bound values name only the first sorted column. Handing them back must not collapse a
	// multi-column sort to that one column.
	[Fact]
	public async Task BoundSort_KeepsAMultiColumnSort()
	{
		IRenderedComponent<BoundSortHost> host = Render<BoundSortHost>(p => p.Add(h => h.MultiSort, true));
		IRenderedComponent<MokaTable<Person>> cut = host.FindComponent<MokaTable<Person>>();

		await HeaderCell(cut, "Team").ClickAsync(new MouseEventArgs { ShiftKey = true });
		await HeaderCell(cut, "Age").ClickAsync(new MouseEventArgs { ShiftKey = true });
		await HeaderCell(cut, "Age").ClickAsync(new MouseEventArgs { ShiftKey = true });

		Assert.Equal(["Alan", "Ada", "Grace", "Barbara"], ColumnTexts(cut, 0));
		Assert.Equal(2, cut.FindAll(".moka-table-sort-priority").Count);
	}

	// SortComparer was only read after a check that the column has a Field.
	[Fact]
	public async Task SortComparer_SortsAColumnWithoutAField()
	{
		IRenderedComponent<MokaTable<Person>> cut = RenderTable(
			columns:
			[
				Name,
				new Col("Letters", null)
				{
					SortComparer = static (a, b) => a.Name.Length.CompareTo(b.Name.Length),
					CellTemplate = person => b => b.AddContent(0, person.Name.Length)
				}
			]);

		await HeaderCell(cut, "Letters").ClickAsync(new MouseEventArgs());

		Assert.Equal(["Ada", "Alan", "Grace", "Barbara"], ColumnTexts(cut, 0));
	}

	// A multi-column sort ordered every column by its Field.
	[Fact]
	public async Task SortComparer_OrdersItsColumnInAMultiColumnSort()
	{
		IRenderedComponent<MokaTable<Person>> cut = RenderTable(
			p => p.Add(t => t.MultiSort, true),
			[Name with { SortComparer = static (a, b) => string.CompareOrdinal(b.Name, a.Name) }, Team]);

		await HeaderCell(cut, "Team").ClickAsync(new MouseEventArgs { ShiftKey = true });
		await HeaderCell(cut, "Name").ClickAsync(new MouseEventArgs { ShiftKey = true });

		Assert.Equal(["Alan", "Ada", "Grace", "Barbara"], ColumnTexts(cut, 0));
	}

	// Sortable defaults to true, and a column with no Field and no comparer offered a sort that
	// did nothing but light up its icon and raise SortColumnChanged.
	[Fact]
	public async Task ColumnWithNothingToSortBy_IsNotSortable()
	{
		var reported = new List<string?>();
		IRenderedComponent<MokaTable<Person>> cut = RenderTable(
			p => p.Add(t => t.SortColumnChanged, c => reported.Add(c)),
			[Name, new Col("Actions", null) { CellTemplate = _ => b => b.AddContent(0, "edit") }]);
		IElement actions = HeaderCell(cut, "Actions");
		Assert.DoesNotContain("moka-table-header--sortable", actions.ClassList);
		Assert.Empty(actions.QuerySelectorAll("button"));

		await Assert.ThrowsAsync<MissingEventHandlerException>(() => actions.ClickAsync(new MouseEventArgs()));

		Assert.Empty(reported);
		Assert.Empty(HeaderCell(cut, "Actions").QuerySelectorAll(".moka-table-sort-icon"));
	}

	// Under server-side data the source sorts, so the title is all a column needs.
	[Fact]
	public async Task ServerData_ColumnWithoutAField_IsSortable()
	{
		var requests = new List<MokaTableState>();
		IRenderedComponent<MokaTable<Person>> cut = RenderServerTable(requests, [Name, new Col("Custom", null)]);

		await HeaderCell(cut, "Custom").ClickAsync(new MouseEventArgs());

		Assert.Equal("Custom", requests[^1].SortColumn);
	}

	// The state handed to ServerData shared its descriptors with the table, which changes them in
	// place on the next click, so a state the source kept was rewritten behind its back.
	[Fact]
	public async Task ServerData_KeepsTheSortItWasGiven()
	{
		var requests = new List<MokaTableState>();
		IRenderedComponent<MokaTable<Person>> cut = RenderServerTable(requests, [Name, Team], multiSort: true);

		await HeaderCell(cut, "Name").ClickAsync(new MouseEventArgs { ShiftKey = true });
		MokaTableState ascending = requests[^1];
		await HeaderCell(cut, "Name").ClickAsync(new MouseEventArgs { ShiftKey = true });

		Assert.Equal(MokaSortDirection.Ascending, Assert.Single(ascending.SortDescriptors).Direction);
		Assert.Equal(MokaSortDirection.Descending, Assert.Single(requests[^1].SortDescriptors).Direction);
	}

	private IRenderedComponent<MokaTable<Person>> RenderTable(
		Action<ComponentParameterCollectionBuilder<MokaTable<Person>>>? extra = null,
		Col[]? columns = null) =>
		Render<MokaTable<Person>>(p =>
		{
			p.Add(t => t.Items, People);
			p.Add(t => t.ChildContent, columns is null ? DefaultColumns : Columns(columns));
			extra?.Invoke(p);
		});

	private IRenderedComponent<MokaTable<Person>> RenderServerTable(
		List<MokaTableState> requests, Col[] columns, bool multiSort = false) =>
		Render<MokaTable<Person>>(p => p
			.Add(t => t.ServerData, state =>
			{
				requests.Add(state);
				return Task.FromResult(new MokaTableResult<Person> { Items = People, TotalItems = People.Length });
			})
			.Add(t => t.MultiSort, multiSort)
			.Add(t => t.ChildContent, Columns(columns)));

	/// <summary>A parent that binds SortColumn and SortDirection both ways.</summary>
	private sealed class BoundSortHost : ComponentBase
	{
		/// <summary>Rows from a server-side source; the kit's people when not set.</summary>
		[Parameter]
		public Func<MokaTableState, Task<MokaTableResult<Person>>>? ServerData { get; set; }

		/// <summary>Passed on to the table.</summary>
		[Parameter]
		public bool MultiSort { get; set; }

		/// <summary>The bound sort column.</summary>
		public string? SortColumn { get; private set; }

		/// <summary>The bound sort direction.</summary>
		public MokaSortDirection SortDirection { get; private set; }

		/// <inheritdoc />
		protected override void BuildRenderTree(RenderTreeBuilder builder)
		{
			ArgumentNullException.ThrowIfNull(builder);
			builder.OpenComponent<MokaTable<Person>>(0);
			if (ServerData is null)
			{
				builder.AddComponentParameter(1, nameof(MokaTable<Person>.Items), People);
			}
			else
			{
				builder.AddComponentParameter(2, nameof(MokaTable<Person>.ServerData), ServerData);
			}

			builder.AddComponentParameter(3, nameof(MokaTable<Person>.MultiSort), MultiSort);
			builder.AddComponentParameter(4, nameof(MokaTable<Person>.SortColumn), SortColumn);
			builder.AddComponentParameter(5, nameof(MokaTable<Person>.SortColumnChanged),
				EventCallback.Factory.Create<string?>(this, value => SortColumn = value));
			builder.AddComponentParameter(6, nameof(MokaTable<Person>.SortDirection), SortDirection);
			builder.AddComponentParameter(7, nameof(MokaTable<Person>.SortDirectionChanged),
				EventCallback.Factory.Create<MokaSortDirection>(this, value => SortDirection = value));
			builder.AddComponentParameter(8, nameof(MokaTable<Person>.ChildContent), DefaultColumns);
			builder.CloseComponent();
		}
	}
}
