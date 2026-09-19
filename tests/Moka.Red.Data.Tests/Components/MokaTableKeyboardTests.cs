using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
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
		IElement TeamCell() => cut.FindAll("tbody tr")[1].QuerySelector("td[data-col-index='1']")!;

		await TeamCell().FocusInAsync(new FocusEventArgs());
		await TeamCell().KeyDownAsync(new KeyboardEventArgs { Key = "Enter" });
		await cut.Find(".moka-table-edit-input").KeyDownAsync(new KeyboardEventArgs { Key = key });

		Assert.Empty(cut.FindAll(".moka-table-edit-input"));
		JSRuntimeInvocation focus = module.VerifyInvoke("focusCell");
		Assert.Equal(1, focus.Arguments[1]);
		Assert.Equal(1, focus.Arguments[2]);
	}

	// The cell's key handler also got the keys of controls in a CellTemplate (gotcha #12).
	[Theory]
	[InlineData("Enter")]
	[InlineData("ArrowDown")]
	[InlineData(" ")]
	public async Task KeysInATemplatesControl_StayWithTheControl(string key)
	{
		BunitJSModuleInterop module = JSInterop.SetupModule(TableModule);
		module.Mode = JSRuntimeMode.Loose;
		int buttonKeys = 0;
		IRenderedComponent<MokaTable<Person>> cut = Render<MokaTable<Person>>(p => p
			.Add(t => t.Items, People)
			.Add(t => t.Selectable, true)
			.Add(t => t.ChildContent, b =>
			{
				b.OpenComponent<MokaColumn<Person>>(0);
				b.AddAttribute(1, nameof(MokaColumn<Person>.Title), "Team");
				b.AddAttribute(2, nameof(MokaColumn<Person>.Editable), true);
				b.AddAttribute(3, nameof(MokaColumn<Person>.CellTemplate), (RenderFragment<Person>)(person => t =>
				{
					t.OpenElement(0, "button");
					t.AddAttribute(1, "class", "row-action");
					t.AddAttribute(2, "onkeydown",
						EventCallback.Factory.Create<KeyboardEventArgs>(this, () => buttonKeys++));
					t.AddContent(3, person.Team);
					t.CloseElement();
				}));
				b.CloseComponent();
			}));

		await cut.FindAll("button.row-action")[0].KeyDownAsync(new KeyboardEventArgs { Key = key });

		Assert.Equal(1, buttonKeys);
		Assert.Empty(cut.FindAll(".moka-table-edit-input"));
		Assert.DoesNotContain(module.Invocations, i => i.Identifier == "focusCell");
		Assert.Empty(cut.FindAll("tbody input[type=checkbox]:checked"));
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
