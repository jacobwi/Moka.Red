using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using Moka.Red.Data.Table;

namespace Moka.Red.Data.Tests.Components;

// Enter on a focused cell raises OnRowClick: moka-table.js clicks the cells marked
// data-activates-row, so the keyboard takes the same path as a mouse click on the cell. These tests
// cover which cells are marked, that path, and where focus goes after the inline editor closes.
public class MokaTableKeyboardTests : BunitContext
{
	private const string TableModule = "./_content/Moka.Red.Data/moka-table.js";

	private static readonly Person[] People =
	[
		new("Ada", "Engineering"),
		new("Grace", "Research")
	];

	public MokaTableKeyboardTests() => JSInterop.Mode = JSRuntimeMode.Loose;

	[Fact]
	public void ClickableRows_LetEnterActivateThemFromTheirCells()
	{
		IRenderedComponent<MokaTable<Person>> cut = RenderTable(onRowClick: _ => { });

		IReadOnlyList<IElement> nameCells = cut.FindAll("tbody td[data-col-index='0']");
		Assert.All(nameCells, cell => Assert.Equal("true", cell.GetAttribute("data-activates-row")));
	}

	// Enter already starts editing an editable cell, so it keeps doing that.
	[Fact]
	public void EditableCells_KeepEnterForEditing()
	{
		IRenderedComponent<MokaTable<Person>> cut = RenderTable(onRowClick: _ => { });

		IReadOnlyList<IElement> teamCells = cut.FindAll("tbody td[data-col-index='1']");
		Assert.All(teamCells, cell => Assert.False(cell.HasAttribute("data-activates-row")));
	}

	[Fact]
	public void RowsWithoutOnRowClick_AreNotActivatedByEnter()
	{
		IRenderedComponent<MokaTable<Person>> cut = RenderTable(onRowClick: null);

		Assert.Empty(cut.FindAll("td[data-activates-row]"));
	}

	[Fact]
	public async Task ClickingACell_RaisesOnRowClick_ForItsRow()
	{
		Person? clicked = null;
		IRenderedComponent<MokaTable<Person>> cut = RenderTable(onRowClick: person => clicked = person);

		await cut.FindAll("tbody tr")[1].QuerySelector("td[data-col-index='0']")!.ClickAsync(new MouseEventArgs());

		Assert.Equal(People[1], clicked);
	}

	// The editor's input took focus with it when Enter or Escape removed it, so focus fell to the
	// page and the arrow keys stopped working.
	[Theory]
	[InlineData("Enter")]
	[InlineData("Escape")]
	public async Task ClosingTheEditor_FromTheKeyboard_PutsFocusBackOnTheCell(string key)
	{
		BunitJSModuleInterop module = JSInterop.SetupModule(TableModule);
		IRenderedComponent<MokaTable<Person>> cut = RenderTable(onRowClick: null);

		await cut.InvokeAsync(() => cut.Instance.OnCellFocused(1, 1));
		await cut.InvokeAsync(() => cut.Instance.OnCellKey(1, 1, "Enter"));
		await cut.Find(".moka-table-edit-input").KeyDownAsync(new KeyboardEventArgs { Key = key });

		Assert.Empty(cut.FindAll(".moka-table-edit-input"));
		JSRuntimeInvocation focus = module.VerifyInvoke("focusCell");
		Assert.Equal(1, focus.Arguments[1]);
		Assert.Equal(1, focus.Arguments[2]);
	}

	// A handler on every cell was replaced by every render and sent to the browser, and it also got the
	// keys of controls in a CellTemplate. moka-table.js has the one listener now, and only a key pressed
	// on the cell itself counts.
	[Fact]
	public async Task Cells_HaveNoKeyOrFocusHandlerOfTheirOwn()
	{
		IRenderedComponent<MokaTable<Person>> cut = RenderTable(onRowClick: null);
		IElement cell = cut.Find("tbody td[data-col-index='1']");

		await Assert.ThrowsAsync<MissingEventHandlerException>(() =>
			cell.KeyDownAsync(new KeyboardEventArgs { Key = "Enter" }));
		await Assert.ThrowsAsync<MissingEventHandlerException>(() => cell.FocusInAsync(new FocusEventArgs()));
	}

	[Fact]
	public void TheGridListener_IsGivenTheTable()
	{
		BunitJSModuleInterop module = JSInterop.SetupModule(TableModule);
		module.Mode = JSRuntimeMode.Loose;

		IRenderedComponent<MokaTable<Person>> cut = RenderTable(onRowClick: null);

		JSRuntimeInvocation init = module.VerifyInvoke("initGridKeys");
		DotNetObjectReference<MokaTable<Person>> table =
			Assert.IsType<DotNetObjectReference<MokaTable<Person>>>(init.Arguments[1]);
		Assert.Same(cut.Instance, table.Value);
	}

	// The script finds Enter's cells by this, and leaves every other cell's Enter alone.
	[Fact]
	public void EditableCells_AreMarkedForTheScript()
	{
		IRenderedComponent<MokaTable<Person>> cut = RenderTable(onRowClick: null);

		Assert.All(cut.FindAll("tbody td[data-col-index='1']"), cell => Assert.Equal("true", cell.GetAttribute("data-editable")));
		Assert.All(cut.FindAll("tbody td[data-col-index='0']"), cell => Assert.False(cell.HasAttribute("data-editable")));
	}

	[Fact]
	public async Task SpaceFromTheScript_SelectsTheRow()
	{
		HashSet<Person>? selected = null;
		IRenderedComponent<MokaTable<Person>> cut = Render<MokaTable<Person>>(p => p
			.Add(t => t.Items, People)
			.Add(t => t.Selectable, true)
			.Add(t => t.SelectedItemsChanged, items => selected = items)
			.Add(t => t.ChildContent, Columns));

		await cut.InvokeAsync(() => cut.Instance.OnCellKey(1, 0, " "));

		Assert.NotNull(selected);
		Assert.Equal(People[1], Assert.Single(selected));
	}

	[Fact]
	public async Task EnterFromTheScript_LeavesACellThatIsNotEditableAlone()
	{
		IRenderedComponent<MokaTable<Person>> cut = RenderTable(onRowClick: null);

		await cut.InvokeAsync(() => cut.Instance.OnCellKey(1, 0, "Enter"));

		Assert.Empty(cut.FindAll(".moka-table-edit-input"));
	}

	// The rows can change between the key and its arrival in .NET.
	[Theory]
	[InlineData(99, 0)]
	[InlineData(0, 99)]
	[InlineData(-1, 0)]
	public async Task AKeyForACellThatIsGone_IsIgnored(int row, int col)
	{
		IRenderedComponent<MokaTable<Person>> cut = RenderTable(onRowClick: null);

		await cut.InvokeAsync(() => cut.Instance.OnCellKey(row, col, "Enter"));

		Assert.Empty(cut.FindAll(".moka-table-edit-input"));
		Assert.False(Renderer.UnhandledException.IsCompleted);
	}

	[Fact]
	public async Task AKeyWhileEditing_IsIgnored()
	{
		IRenderedComponent<MokaTable<Person>> cut = RenderTable(onRowClick: null);
		await cut.InvokeAsync(() => cut.Instance.OnCellKey(1, 1, "Enter"));
		string editing = cut.Find(".moka-table-edit-input").GetAttribute("value")!;

		await cut.InvokeAsync(() => cut.Instance.OnCellKey(0, 1, "Enter"));

		Assert.Equal(editing, cut.Find(".moka-table-edit-input").GetAttribute("value"));
	}

	// A handler that failed on a key used to reach the boundary through the event pipeline, and a JS call has none.
	[Fact]
	public async Task ASelectionHandlerThatFailsOnSpace_ReachesTheRenderer()
	{
		IRenderedComponent<MokaTable<Person>> cut = Render<MokaTable<Person>>(p => p
			.Add(t => t.Items, People)
			.Add(t => t.Selectable, true)
			.Add(t => t.SelectedItemsChanged, EventCallback.Factory.Create<HashSet<Person>>(this, FailAsync))
			.Add(t => t.ChildContent, Columns));

		await cut.InvokeAsync(() => cut.Instance.OnCellKey(1, 0, " "));

		Exception error = await Renderer.UnhandledException.WaitAsync(TimeSpan.FromSeconds(5),
			Xunit.TestContext.Current.CancellationToken);
		Assert.Equal("Handler failed", error.Message);
	}

	// The script moved the tab stop between renders, so the next render asks it to agree.
	[Fact]
	public async Task ARenderAfterFocusMoved_PutsTheTabStopWhereItSaysItIs()
	{
		BunitJSModuleInterop module = JSInterop.SetupModule(TableModule);
		module.Mode = JSRuntimeMode.Loose;
		IRenderedComponent<MokaTable<Person>> cut = RenderTable(onRowClick: null);
		Assert.DoesNotContain(module.Invocations, i => i.Identifier == "setTabStop");

		await cut.InvokeAsync(() => cut.Instance.OnCellFocused(1, 0));
		Assert.DoesNotContain(module.Invocations, i => i.Identifier == "setTabStop");

		cut.Render(p => p.Add(t => t.Dense, false));

		JSRuntimeInvocation setTabStop = module.VerifyInvoke("setTabStop");
		Assert.Equal(1, setTabStop.Arguments[1]);
		Assert.Equal(0, setTabStop.Arguments[2]);
		Assert.Equal("0", cut.Find("tbody tr[data-row-index='1'] td[data-col-index='0']").GetAttribute("tabindex"));
	}

	private static async Task FailAsync(HashSet<Person> _)
	{
		await Task.Yield();
		throw new InvalidOperationException("Handler failed");
	}

	private IRenderedComponent<MokaTable<Person>> RenderTable(Action<Person>? onRowClick) =>
		Render<MokaTable<Person>>(p =>
		{
			p.Add(t => t.Items, People).Add(t => t.ChildContent, Columns);
			if (onRowClick is not null)
			{
				p.Add(t => t.OnRowClick, onRowClick);
			}
		});

	private static void Columns(RenderTreeBuilder builder)
	{
		builder.OpenComponent<MokaColumn<Person>>(0);
		builder.AddAttribute(1, nameof(MokaColumn<Person>.Title), "Name");
		builder.AddAttribute(2, nameof(MokaColumn<Person>.Field), (Func<Person, object?>)(x => x.Name));
		builder.CloseComponent();

		builder.OpenComponent<MokaColumn<Person>>(10);
		builder.AddAttribute(11, nameof(MokaColumn<Person>.Title), "Team");
		builder.AddAttribute(12, nameof(MokaColumn<Person>.Field), (Func<Person, object?>)(x => x.Team));
		builder.AddAttribute(13, nameof(MokaColumn<Person>.Editable), true);
		builder.CloseComponent();
	}

	private sealed record Person(string Name, string Team);
}
