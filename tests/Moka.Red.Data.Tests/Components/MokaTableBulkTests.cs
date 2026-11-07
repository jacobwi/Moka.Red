using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Moka.Red.Data.Table;
using Moka.Red.Feedback.Tooltip;
using Moka.Red.Tests.Shared;
using static Moka.Red.Data.Tests.Components.TableKit;

namespace Moka.Red.Data.Tests.Components;

// The bar over picked rows, and picking a run.
public class MokaTableBulkTests : BunitContext
{
	private static readonly RenderFragment<IReadOnlyList<Person>> Actions = _ => b => b.AddContent(0, "actions");

	public MokaTableBulkTests() => JSInterop.Mode = JSRuntimeMode.Loose;

	[Fact]
	public void WithoutBulkActions_NothingIsAdded()
	{
		IRenderedComponent<MokaTable<Person>> cut = Table(p => p.Add(t => t.BulkActions, (RenderFragment<IReadOnlyList<Person>>?)null));

		Assert.Empty(cut.FindAll(".moka-table-bulk"));
		Assert.Empty(cut.FindAll("[role='status']"));
		Assert.DoesNotContain("moka-table-wrapper--bulk", cut.Find(".moka-table-wrapper").ClassList);
	}

	[Fact]
	public void TheBar_WaitsForAPick()
	{
		IRenderedComponent<MokaTable<Person>> cut = Table(p => p.Add(t => t.BulkActions, Actions));

		Assert.Empty(cut.FindAll(".moka-table-bulk"));
	}

	// A status region that arrives with its text isn't announced, so it's there, empty, from the start.
	[Fact]
	public void TheStatusRegion_IsThereBeforeThePick()
	{
		IRenderedComponent<MokaTable<Person>> cut = Table(p => p.Add(t => t.BulkActions, Actions));

		Assert.Equal(string.Empty, cut.Find("[role='status']").TextContent);
	}

	[Fact]
	public async Task APick_ShowsTheCountAndTheNames()
	{
		IRenderedComponent<MokaTable<Person>> cut = Table(p => p
			.Add(t => t.BulkActions, Actions)
			.Add(t => t.ItemName, x => x.Name));

		await Tick(cut, 2);
		await Tick(cut, 0);

		Assert.Equal("2 selected", cut.Find(".moka-table-bulk-total").TextContent.Trim());
		Assert.Equal("Ada, Alan", cut.Find(".moka-table-bulk-names").TextContent.Trim());
		Assert.Equal("2 selected", cut.Find("[role='status']").TextContent.Trim());
	}

	[Fact]
	public async Task WithoutItemName_TheBarOnlyCounts()
	{
		IRenderedComponent<MokaTable<Person>> cut = Table(p => p.Add(t => t.BulkActions, Actions));

		await Tick(cut, 1);

		Assert.Empty(cut.FindAll(".moka-table-bulk-names"));
		Assert.Equal("1 selected", cut.Find(".moka-table-bulk-total").TextContent.Trim());
	}

	[Fact]
	public async Task ALongList_SaysHowManyMoreThereAre()
	{
		Person[] many = Enumerable.Range(0, 25).Select(i => new Person($"P{i}", "T", i, "C")).ToArray();
		IRenderedComponent<MokaTable<Person>> cut = Render<MokaTable<Person>>(p => p
			.Add(t => t.Items, many)
			.Add(t => t.PageSize, 25)
			.Add(t => t.Selectable, true)
			.Add(t => t.BulkActions, Actions)
			.Add(t => t.ItemName, x => x.Name)
			.Add(t => t.ChildContent, DefaultColumns));

		await cut.Find("thead input[type=checkbox]").ChangeAsync(new ChangeEventArgs { Value = true });

		string names = cut.Find(".moka-table-bulk-names").TextContent;
		Assert.EndsWith("and 5 more", names, StringComparison.Ordinal);
		Assert.DoesNotContain("P20", names, StringComparison.Ordinal);
	}

	[Fact]
	public async Task TheFragment_GetsThePickedRows_OnScreenFirst()
	{
		IReadOnlyList<Person>? given = null;
		IRenderedComponent<MokaTable<Person>> cut = Render<MokaTable<Person>>(p => p
			.Add(t => t.Items, People)
			.Add(t => t.PageSize, 2)
			.Add(t => t.Selectable, true)
			.Add(t => t.SelectedItems, [People[3]])
			.Add(t => t.BulkActions, (RenderFragment<IReadOnlyList<Person>>)(rows =>
			{
				given = rows;
				return b => b.AddContent(0, "actions");
			}))
			.Add(t => t.ChildContent, DefaultColumns));

		await Tick(cut, 1);

		Assert.Equal([People[1], People[3]], given!);
		Assert.Equal("actions", cut.Find(".moka-table-bulk-actions").TextContent.Trim());
	}

	[Fact]
	public async Task Clear_EmptiesTheSelection_AndTellsTheParent()
	{
		HashSet<Person>? selected = null;
		IRenderedComponent<MokaTable<Person>> cut = Table(p => p
			.Add(t => t.BulkActions, Actions)
			.Add(t => t.SelectedItemsChanged, s => selected = s));
		await Tick(cut, 1);

		await cut.Find(".moka-table-bulk button").ClickAsync(new MouseEventArgs());

		Assert.Empty(selected!);
		Assert.Empty(cut.FindAll(".moka-table-bulk"));
		Assert.Empty(cut.FindAll("tbody input[type=checkbox]:checked"));
	}

	[Fact]
	public void ATableThatIsNotSelectable_HasNoBar()
	{
		IRenderedComponent<MokaTable<Person>> cut = Render<MokaTable<Person>>(p => p
			.Add(t => t.Items, People)
			.Add(t => t.SelectedItems, [People[1]])
			.Add(t => t.BulkActions, Actions)
			.Add(t => t.ChildContent, DefaultColumns));

		Assert.Empty(cut.FindAll(".moka-table-bulk"));
	}

	[Fact]
	public async Task BulkActions_TakesThePlaceOfSelectionActions_AndLeavesTheToolbar()
	{
		IRenderedComponent<MokaTable<Person>> cut = Table(p => p
			.Add(t => t.Title, "People")
			.Add(t => t.BulkActions, Actions)
			.Add(t => t.SelectionActions, (RenderFragment<HashSet<Person>>)(_ => b => b.AddContent(0, "old"))));

		await Tick(cut, 1);

		Assert.Empty(cut.FindAll(".moka-table-selection-bar"));
		Assert.NotEmpty(cut.FindAll(".moka-table-bulk"));
		Assert.NotEmpty(cut.FindAll(".moka-table-toolbar"));
	}

	[Fact]
	public async Task SelectionActions_StillWorksAlone()
	{
		IRenderedComponent<MokaTable<Person>> cut = Table(p => p
			.Add(t => t.SelectionActions, (RenderFragment<HashSet<Person>>)(_ => b => b.AddContent(0, "old"))));

		await Tick(cut, 1);

		Assert.NotEmpty(cut.FindAll(".moka-table-selection-bar"));
		Assert.Empty(cut.FindAll(".moka-table-bulk"));
	}

	[Fact]
	public async Task TheBar_SitsOverTheHeader()
	{
		IRenderedComponent<MokaTable<Person>> cut = Table(p => p.Add(t => t.BulkActions, Actions));

		await Tick(cut, 1);

		IElement bar = cut.Find(".moka-table-bulk");
		Assert.Equal("moka-table-container", bar.NextElementSibling!.ClassName!.Split(' ')[0]);
	}

	[Fact]
	public async Task ShiftClick_PicksTheRunFromTheLastBox()
	{
		HashSet<Person>? selected = null;
		IRenderedComponent<MokaTable<Person>> cut = Table(p => p.Add(t => t.SelectedItemsChanged, s => selected = s));

		await Tick(cut, 0);
		await Tick(cut, 2, shift: true);

		Assert.Equal([People[0], People[1], People[2]], selected!.OrderBy(x => Array.IndexOf(People, x)));
		Assert.Equal(3, cut.FindAll("tbody input[type=checkbox]:checked").Count);
	}

	[Fact]
	public async Task ShiftClick_WorksUpwardsToo()
	{
		IRenderedComponent<MokaTable<Person>> cut = Table();

		await Tick(cut, 3);
		await Tick(cut, 1, shift: true);

		Assert.Equal(3, cut.FindAll("tbody input[type=checkbox]:checked").Count);
		Assert.Empty(DataRows(cut)[0].QuerySelectorAll("input:checked"));
	}

	// The run takes the state of the last box, so unticking and shift-clicking unpicks a run.
	[Fact]
	public async Task ShiftClick_FromAnUntickedBox_UnpicksTheRun()
	{
		IRenderedComponent<MokaTable<Person>> cut = Table(p => p.Add(t => t.SelectedItems, [.. People]));

		await Tick(cut, 0);
		await Tick(cut, 2, shift: true);

		Assert.Single(cut.FindAll("tbody input[type=checkbox]:checked"));
	}

	[Fact]
	public async Task ShiftClick_WithNoLastBox_PicksOneRow()
	{
		IRenderedComponent<MokaTable<Person>> cut = Table();

		await Tick(cut, 2, shift: true);

		Assert.Single(cut.FindAll("tbody input[type=checkbox]:checked"));
	}

	// The browser has flipped the clicked box, and a row the run leaves as it was sends no update, so the box would
	// stay wrong on screen.
	[Fact]
	public async Task ShiftClick_PutsTheClickedBoxRightInTheBrowser()
	{
		BunitJSModuleInterop module = JSInterop.SetupModule(TableModule);
		module.Mode = JSRuntimeMode.Loose;
		IRenderedComponent<MokaTable<Person>> cut = Table(p => p.Add(t => t.SelectedItems, [People[2]]));

		await Tick(cut, 0);
		await Tick(cut, 2, shift: true);

		JSRuntimeInvocation call = Assert.Single(module.Invocations, i => i.Identifier == "setRowBox");
		Assert.Equal(2, call.Arguments[1]);
		Assert.Equal(true, call.Arguments[2]);
	}

	[Fact]
	public async Task ThePlainTick_SendsNoCorrection()
	{
		BunitJSModuleInterop module = JSInterop.SetupModule(TableModule);
		module.Mode = JSRuntimeMode.Loose;
		IRenderedComponent<MokaTable<Person>> cut = Table();

		await Tick(cut, 0);

		Assert.DoesNotContain(module.Invocations, i => i.Identifier == "setRowBox");
	}

	[Fact]
	public async Task SelectAll_ForgetsTheLastBox()
	{
		IRenderedComponent<MokaTable<Person>> cut = Table();
		await Tick(cut, 0);
		await cut.Find("thead input[type=checkbox]").ChangeAsync(new ChangeEventArgs { Value = true });
		await cut.Find("thead input[type=checkbox]").ChangeAsync(new ChangeEventArgs { Value = false });

		await Tick(cut, 2, shift: true);

		Assert.Single(cut.FindAll("tbody input[type=checkbox]:checked"));
	}

	[Fact]
	public async Task ShiftSpace_PicksARunFromTheKeyboard()
	{
		IRenderedComponent<MokaTable<Person>> cut = Table();
		await Tick(cut, 0);

		cut.Instance.OnCellKey(2, 0, "Shift+ ");

		cut.WaitForAssertion(() => Assert.Equal(3, cut.FindAll("tbody input[type=checkbox]:checked").Count), TimeSpan.FromSeconds(10));
	}

	// A list that ticks: the same device with a new rate is a new, unequal record, and its box used to go blank.
	[Fact]
	public async Task UnderAnItemKey_APickedRowStaysPickedWhenItsDataChanges()
	{
		IReadOnlyList<Person>? given = null;
		IRenderedComponent<MokaTable<Person>> cut = Table(p => p
			.Add(t => t.ItemKey, x => x.Name)
			.Add(t => t.BulkActions, (RenderFragment<IReadOnlyList<Person>>)(rows =>
			{
				given = rows;
				return b => b.AddContent(0, "actions");
			})));
		await Tick(cut, 1);

		Person[] ticked = [.. People.Select(x => x with { Age = x.Age + 1 })];
		cut.Render(p => p.Add(t => t.Items, ticked));

		Assert.Single(cut.FindAll("tbody input[type=checkbox]:checked"));
		Assert.Single(DataRows(cut)[1].QuerySelectorAll("input:checked"));
		Assert.Equal([ticked[1]], given!);
	}

	[Fact]
	public async Task UnderAnItemKey_ARunStartsFromTheRowsNewItem()
	{
		IRenderedComponent<MokaTable<Person>> cut = Table(p => p.Add(t => t.ItemKey, x => x.Name));
		await Tick(cut, 0);

		cut.Render(p => p.Add(t => t.Items, [.. People.Select(x => x with { Age = x.Age + 1 })]));
		await Tick(cut, 2, shift: true);

		Assert.Equal(3, cut.FindAll("tbody input[type=checkbox]:checked").Count);
	}

	[Fact]
	public async Task WithoutAnItemKey_ANewItemIsANewRow()
	{
		IRenderedComponent<MokaTable<Person>> cut = Table();
		await Tick(cut, 1);

		cut.Render(p => p.Add(t => t.Items, [.. People.Select(x => x with { Age = x.Age + 1 })]));

		Assert.Empty(cut.FindAll("tbody input[type=checkbox]:checked"));
	}

	// A single selection has no run, so its boxes don't send the click to .NET.
	[Fact]
	public async Task SingleSelect_HasNoShiftClick()
	{
		IRenderedComponent<MokaTable<Person>> cut = Table(p => p.Add(t => t.SingleSelect, true));

		await Assert.ThrowsAsync<MissingEventHandlerException>(() => DataRows(cut)[0].QuerySelector("input")!.ClickAsync(new MouseEventArgs()));
	}

	[Fact]
	public void ItemName_NamesEachBox()
	{
		IRenderedComponent<MokaTable<Person>> cut = Table(p => p.Add(t => t.ItemName, x => x.Name));

		Assert.Equal("Select Ada", DataRows(cut)[0].QuerySelector("input")!.GetAttribute("aria-label"));
	}

	[Fact]
	public void WithoutItemName_TheBoxesStayAsTheyWere()
	{
		IRenderedComponent<MokaTable<Person>> cut = Table();

		Assert.False(DataRows(cut)[0].QuerySelector("input")!.HasAttribute("aria-label"));
	}

	[Fact]
	public async Task ByDefault_TheBarsClear_IsAWord()
	{
		IRenderedComponent<MokaTable<Person>> cut = Table(p => p.Add(t => t.BulkActions, Actions));

		await Tick(cut, 0);

		Assert.Equal("Clear", cut.Find(".moka-table-bulk > button:last-child").TextContent.Trim());
	}

	[Fact]
	public async Task BulkClearIcon_IsANamedCross_InATooltip()
	{
		IRenderedComponent<MokaTable<Person>> cut = Table(p => p
			.Add(t => t.BulkActions, Actions)
			.Add(t => t.BulkClearIcon, true));
		await Tick(cut, 0);

		IElement clear = cut.Find(".moka-table-bulk button[aria-label='Clear the selection']");
		Assert.Equal(string.Empty, clear.TextContent.Trim());
		IconTooltip.AssertNamed(clear, "Clear the selection", MokaTooltipPosition.BottomEnd);

		await clear.ClickAsync(new MouseEventArgs());

		Assert.Empty(cut.FindAll(".moka-table-bulk"));
	}

	private IRenderedComponent<MokaTable<Person>> Table(
		Action<ComponentParameterCollectionBuilder<MokaTable<Person>>>? extra = null) =>
		Render<MokaTable<Person>>(p =>
		{
			p.Add(t => t.Items, People);
			p.Add(t => t.Selectable, true);
			p.Add(t => t.ChildContent, DefaultColumns);
			extra?.Invoke(p);
		});

	// The browser's order for a box: its click, which alone can say Shift was held, and then its change.
	private static async Task Tick(IRenderedComponent<MokaTable<Person>> cut, int row, bool shift = false)
	{
		await DataRows(cut)[row].QuerySelector("input")!.ClickAsync(new MouseEventArgs { ShiftKey = shift });
		await DataRows(cut)[row].QuerySelector("input")!.ChangeAsync(new ChangeEventArgs { Value = true });
	}
}
