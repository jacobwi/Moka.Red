using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Moka.Red.Data.Table;
using static Moka.Red.Data.Tests.Components.TableKit;

namespace Moka.Red.Data.Tests.Components;

// What the table calls its rows, and drawing at least as many as someone asked for.
public class MokaTableItemsNameTests : BunitContext
{
	private static readonly RenderFragment<IReadOnlyList<Person>> Actions = _ => b => b.AddContent(0, "actions");

	public MokaTableItemsNameTests() => JSInterop.Mode = JSRuntimeMode.Loose;

	private static Person[] Many(int count) =>
		Enumerable.Range(1, count).Select(i => new Person($"P{i:000}", "T", i, "C")).ToArray();

	[Fact]
	public void WithoutItemsName_TheWordsStayAboutRows()
	{
		IRenderedComponent<MokaTable<Person>> cut = Table(Many(5));

		Assert.Equal("Showing 2 of 5", cut.Find(".moka-table-foot-count").TextContent.Trim());
		Assert.Equal(["Show 2 more", "Show all"], FootButtons(cut));
		Assert.Equal("Select all rows shown", SelectAll(cut).GetAttribute("aria-label"));
	}

	[Fact]
	public void TheShowMoreFoot_CountsTheRowsByTheirName()
	{
		IRenderedComponent<MokaTable<Person>> cut = Table(Many(312), p => p.Add(t => t.ItemsName, "devices"), pageSize: 50);

		Assert.Equal("50 of 312 devices", cut.Find(".moka-table-foot-count").TextContent.Trim());
		Assert.Equal(["Show 50 more", "Show all 312"], FootButtons(cut));
	}

	[Fact]
	public void TheHeadersBox_NamesTheRows()
	{
		IRenderedComponent<MokaTable<Person>> cut = Table(Many(5), p => p.Add(t => t.ItemsName, "devices"));

		Assert.Equal("Select all devices shown", SelectAll(cut).GetAttribute("aria-label"));
	}

	[Theory]
	[InlineData(true, "Select all devices on this page")]
	[InlineData(false, "Select all devices")]
	public void TheHeadersBox_NamesTheRows_WithAPagerOrWithout(bool pager, string label)
	{
		IRenderedComponent<MokaTable<Person>> cut = Render<MokaTable<Person>>(p => p
			.Add(t => t.Items, Many(5))
			.Add(t => t.Selectable, true)
			.Add(t => t.ShowPagination, pager)
			.Add(t => t.ItemsName, "devices")
			.Add(t => t.ChildContent, DefaultColumns));

		Assert.Equal(label, SelectAll(cut).GetAttribute("aria-label"));
	}

	[Theory]
	[InlineData(null, "Selected rows")]
	[InlineData("devices", "Act on the selected devices")]
	public async Task TheBulkBar_IsNamedForTheRows(string? noun, string label)
	{
		IRenderedComponent<MokaTable<Person>> cut = Table(Many(5), p => p
			.Add(t => t.BulkActions, Actions)
			.Add(t => t.ItemsName, noun));

		await DataRows(cut)[0].QuerySelector("input")!.ChangeAsync(new ChangeEventArgs { Value = true });

		Assert.Equal(label, cut.Find(".moka-table-bulk").GetAttribute("aria-label"));
	}

	[Theory]
	[InlineData(null, "Loading rows")]
	[InlineData("devices", "Loading devices…")]
	public void TheLoadingStatus_NamesTheRows(string? noun, string status)
	{
		IRenderedComponent<MokaTable<Person>> cut = Table(Many(5), p => p
			.Add(t => t.Loading, true)
			.Add(t => t.ItemsName, noun));

		Assert.Equal(status, cut.Find("[role='status']").TextContent);
	}

	// A log page adds older entries to the list it already passed, so the table can't see them by itself.
	[Fact]
	public async Task ShowAtLeast_ReadsAListChangedInPlace_AndDrawsThatMany()
	{
		List<Person> rows = [.. Many(5)];
		IRenderedComponent<MokaTable<Person>> cut = Table(rows);
		rows.AddRange(Many(9).Skip(5));

		await cut.InvokeAsync(() => cut.Instance.ShowAtLeastAsync(7));

		Assert.Equal(7, DataRows(cut).Count);
		Assert.Equal("Showing 7 of 9", cut.Find(".moka-table-foot-count").TextContent.Trim());
	}

	[Fact]
	public async Task ShowAtLeast_NeverDrawsFewerThanAreShown()
	{
		IRenderedComponent<MokaTable<Person>> cut = Table(Many(9));
		await FootButtonElements(cut)[0].ClickAsync(new MouseEventArgs());
		await FootButtonElements(cut)[0].ClickAsync(new MouseEventArgs());

		await cut.InvokeAsync(() => cut.Instance.ShowAtLeastAsync(3));

		Assert.Equal(6, DataRows(cut).Count);
	}

	[Fact]
	public async Task ShowAtLeast_AfterShowAll_KeepsEveryRow()
	{
		List<Person> rows = [.. Many(5)];
		IRenderedComponent<MokaTable<Person>> cut = Table(rows);
		await FootButtonElements(cut)[1].ClickAsync(new MouseEventArgs());
		rows.AddRange(Many(8).Skip(5));

		await cut.InvokeAsync(() => cut.Instance.ShowAtLeastAsync(6));

		Assert.Equal(8, DataRows(cut).Count);
	}

	[Fact]
	public async Task ShowAtLeast_UnderServerData_AsksForThatMany()
	{
		List<MokaTableState> asked = [];
		Person[] all = Many(9);
		IRenderedComponent<MokaTable<Person>> cut = Render<MokaTable<Person>>(p => p
			.Add(t => t.ShowMore, true)
			.Add(t => t.PageSize, 2)
			.Add(t => t.ServerData, state =>
			{
				asked.Add(state);
				return Task.FromResult(new MokaTableResult<Person> { Items = all.Take(state.PageSize).ToList(), TotalItems = all.Length });
			})
			.Add(t => t.ChildContent, DefaultColumns));

		await cut.InvokeAsync(() => cut.Instance.ShowAtLeastAsync(5));

		Assert.Equal(5, asked[^1].PageSize);
		Assert.Equal(5, DataRows(cut).Count);
	}

	[Fact]
	public async Task ShowAtLeast_WithoutShowMore_StillReadsTheListAgain()
	{
		List<Person> rows = [.. Many(3)];
		IRenderedComponent<MokaTable<Person>> cut = Render<MokaTable<Person>>(p => p
			.Add(t => t.Items, rows)
			.Add(t => t.ShowPagination, false)
			.Add(t => t.ChildContent, DefaultColumns));
		rows.Add(new Person("Late", "T", 99, "C"));

		await cut.InvokeAsync(() => cut.Instance.ShowAtLeastAsync(1));

		Assert.Equal(4, DataRows(cut).Count);
	}

	private static IElement SelectAll(IRenderedComponent<MokaTable<Person>> cut) =>
		cut.Find("thead .moka-table-cell--checkbox input");

	private static IReadOnlyList<IElement> FootButtonElements(IRenderedComponent<MokaTable<Person>> cut) =>
		cut.FindAll(".moka-table-foot button");

	private static List<string> FootButtons(IRenderedComponent<MokaTable<Person>> cut) =>
		FootButtonElements(cut).Select(button => button.TextContent.Trim()).ToList();

	private IRenderedComponent<MokaTable<Person>> Table(
		IEnumerable<Person> rows,
		Action<ComponentParameterCollectionBuilder<MokaTable<Person>>>? extra = null,
		int pageSize = 2) =>
		Render<MokaTable<Person>>(p =>
		{
			p.Add(t => t.Items, rows);
			p.Add(t => t.ShowMore, true);
			p.Add(t => t.Selectable, true);
			p.Add(t => t.PageSize, pageSize);
			p.Add(t => t.ChildContent, DefaultColumns);
			extra?.Invoke(p);
		});
}
