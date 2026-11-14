using System.ComponentModel.DataAnnotations;
using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Moka.Red.Data.Table;
using Moka.Red.Feedback.Dialog;
using static Moka.Red.Data.Tests.Components.TableKit;

namespace Moka.Red.Data.Tests.Table;

// Real editing: editors that fit the column, checks before a save, cell and row modes, adding, deleting with a confirm,
// undo, and the keys. The table hands every change to OnRowSave and changes none of the app's objects.
public class MokaTableEditingTests : BunitContext
{
	private static readonly Gadget[] Gadgets =
	[
		new("Lamp", 12.5, new DateTimeOffset(2026, 3, 1, 9, 30, 0, TimeSpan.FromHours(1)), true, "Shipped", 3, Shade.Light),
		new("Desk", 240, new DateTimeOffset(2025, 11, 20, 14, 0, 0, TimeSpan.Zero), false, "Ordered", 4, Shade.Dark)
	];

	private readonly List<MokaTableRowSave<Gadget>> _saves = [];

	public MokaTableEditingTests() => JSInterop.Mode = JSRuntimeMode.Loose;

	// ── Editors per type ──

	[Theory]
	[InlineData("Name", "input[type='text']:not([inputmode])")]
	[InlineData("Price", "input[inputmode='numeric']")]
	[InlineData("Made", ".moka-datepicker-input-wrapper")]
	[InlineData("Stock", "input[type='checkbox']:not([role])")]
	[InlineData("Status", "select")]
	[InlineData("Stars", "[role='slider']")]
	[InlineData("Shade", "select")]
	public async Task EachColumn_OpensTheEditorThatFitsIt(string column, string editor)
	{
		IRenderedComponent<MokaTable<Gadget>> cut = RenderGadgets();

		await cut.InvokeAsync(() => cut.Instance.StartEditAsync(Gadgets[0], column));

		IElement cell = cut.Find("td[data-editing]");
		Assert.NotNull(cell.QuerySelector(editor));
		Assert.Equal("cell", cell.GetAttribute("data-editing"));
		Assert.Equal(column, cell.QuerySelector("[aria-label]")!.GetAttribute("aria-label"));
	}

	[Fact]
	public async Task ASwitchLook_OpensASwitch()
	{
		IRenderedComponent<MokaTable<Gadget>> cut = RenderGadgets(switchLook: true);

		await cut.InvokeAsync(() => cut.Instance.StartEditAsync(Gadgets[0], "Stock"));

		Assert.NotNull(cut.Find("td[data-editing] input[role='switch']"));
	}

	[Fact]
	public async Task ANumber_IsSavedAsTheColumnsType_ThroughItsSetter()
	{
		IRenderedComponent<MokaTable<Gadget>> cut = RenderGadgets();

		await cut.InvokeAsync(() => cut.Instance.StartEditAsync(Gadgets[0], "Price"));
		await cut.Find("td[data-editing] input").InputAsync(new ChangeEventArgs { Value = "19.75" });
		await cut.InvokeAsync(() => cut.Instance.OnEditorKey("Enter"));

		MokaTableRowSave<Gadget> save = Assert.Single(_saves);
		Assert.Equal(new MokaTableCellChange("Price", 12.5, 19.75), Assert.Single(save.Changes));
		Assert.Equal(19.75, save.Updated.Price);
		Assert.Equal(12.5, Gadgets[0].Price);
		Assert.Same(Gadgets[0], save.Item);
	}

	[Fact]
	public async Task AYesOrNo_IsSavedAsABool()
	{
		IRenderedComponent<MokaTable<Gadget>> cut = RenderGadgets();

		await cut.InvokeAsync(() => cut.Instance.StartEditAsync(Gadgets[0], "Stock"));
		await cut.Find("td[data-editing] input[type='checkbox']").ChangeAsync(new ChangeEventArgs { Value = false });
		await cut.InvokeAsync(() => cut.Instance.OnEditorKey("Enter"));

		Assert.False(Assert.Single(_saves).Updated.InStock);
		Assert.Equal(false, Assert.Single(_saves[0].Changes).NewValue);
	}

	[Fact]
	public async Task AStatus_OffersItsChoices_AndSavesThePick()
	{
		IRenderedComponent<MokaTable<Gadget>> cut = RenderGadgets();

		await cut.InvokeAsync(() => cut.Instance.StartEditAsync(Gadgets[0], "Status"));
		Assert.Equal(["Ordered", "Shipped"], cut.FindAll("td[data-editing] option").Select(static o => o.TextContent.Trim()));
		await cut.Find("td[data-editing] select").ChangeAsync(new ChangeEventArgs { Value = OptionValue(cut, "Ordered") });
		await cut.InvokeAsync(() => cut.Instance.OnEditorKey("Enter"));

		Assert.Equal("Ordered", Assert.Single(_saves).Updated.Status);
	}

	[Fact]
	public async Task AnEnum_OffersItsNames_AndSavesTheValue()
	{
		IRenderedComponent<MokaTable<Gadget>> cut = RenderGadgets();

		await cut.InvokeAsync(() => cut.Instance.StartEditAsync(Gadgets[0], "Shade"));
		Assert.Equal(["Light", "Dark"], cut.FindAll("td[data-editing] option").Select(static o => o.TextContent.Trim()));
		await cut.Find("td[data-editing] select").ChangeAsync(new ChangeEventArgs { Value = OptionValue(cut, "Dark") });
		await cut.InvokeAsync(() => cut.Instance.OnEditorKey("Enter"));

		Assert.Equal(Shade.Dark, Assert.Single(_saves).Updated.Shade);
	}

	[Fact]
	public async Task Stars_AreSavedAsTheRating()
	{
		IRenderedComponent<MokaTable<Gadget>> cut = RenderGadgets();

		await cut.InvokeAsync(() => cut.Instance.StartEditAsync(Gadgets[0], "Stars"));
		await cut.Find("td[data-editing] [role='slider']").KeyDownAsync(new KeyboardEventArgs { Key = "ArrowRight" });
		await cut.InvokeAsync(() => cut.Instance.OnEditorKey("Enter"));

		Assert.Equal(4.0, Assert.Single(_saves).Updated.Stars);
	}

	[Fact]
	public async Task TextThatDoesNotRead_KeepsTheEditOpenWithAMessage()
	{
		IRenderedComponent<MokaTable<Person>> cut = RenderPeople(Age with { Editable = true, Editor = MokaColumnEditor.Text });

		await cut.InvokeAsync(() => cut.Instance.OnCellKey(0, 1, "Enter"));
		await cut.Find("td[data-editing] input").InputAsync(new ChangeEventArgs { Value = "forty" });
		await cut.InvokeAsync(() => cut.Instance.OnEditorKey("Enter"));

		Assert.Empty(_peopleSaves);
		Assert.Contains("Enter a valid value for Age", cut.Find("td[data-editing]").TextContent, StringComparison.Ordinal);
	}

	[Fact]
	public async Task AnEditTemplate_DrawsTheAppsOwnEditor()
	{
		RenderFragment<MokaCellEdit<Person>> template = edit => builder =>
		{
			builder.OpenElement(0, "input");
			builder.AddAttribute(1, "class", "own-editor");
			builder.AddAttribute(2, "aria-label", edit.Label);
			builder.AddAttribute(3, "value", edit.Value);
			builder.AddAttribute(4, "onchange", EventCallback.Factory.Create<ChangeEventArgs>(this, e => edit.SetValue(e.Value)));
			builder.CloseElement();
		};
		IRenderedComponent<MokaTable<Person>> cut = Render<MokaTable<Person>>(p => p
			.Add(t => t.Items, People)
			.Add(t => t.OnRowSave, save => _peopleSaves.Add(save))
			.Add(t => t.ChildContent, (RenderFragment)(builder =>
			{
				builder.OpenComponent<MokaColumn<Person>>(0);
				builder.AddComponentParameter(1, nameof(MokaColumn<Person>.Title), "Name");
				builder.AddComponentParameter(2, nameof(MokaColumn<Person>.Field), (Func<Person, object?>)(static x => x.Name));
				builder.AddComponentParameter(3, nameof(MokaColumn<Person>.Editable), true);
				builder.AddComponentParameter(4, nameof(MokaColumn<Person>.EditTemplate), template);
				builder.AddComponentParameter(5, nameof(MokaColumn<Person>.SetField),
					(Func<Person, object?, Person>)(static (p, v) => p with { Name = (string)v! }));
				builder.CloseComponent();
			})));

		await cut.InvokeAsync(() => cut.Instance.OnCellKey(0, 0, "F2"));
		await cut.Find(".own-editor").ChangeAsync(new ChangeEventArgs { Value = "Ada L." });
		await cut.InvokeAsync(() => cut.Instance.OnEditorKey("Enter"));

		Assert.Equal("Ada L.", Assert.Single(_peopleSaves).Updated.Name);
	}

	// ── Checks ──

	[Fact]
	public async Task AColumnsCheck_StopsTheSave_UntilTheValueIsFixed()
	{
		IRenderedComponent<MokaTable<Person>> cut = RenderPeople(Age with
		{
			Editable = true,
			SetField = static (p, v) => p with { Age = (int)v! },
			Validate = static (_, v) => v is int age && age > 120 ? "Too old" : null
		});

		await cut.InvokeAsync(() => cut.Instance.OnCellKey(0, 1, "Enter"));
		await cut.Find("td[data-editing] input").InputAsync(new ChangeEventArgs { Value = "200" });
		Assert.Contains("Too old", cut.Find("td[data-editing]").TextContent, StringComparison.Ordinal);
		await cut.InvokeAsync(() => cut.Instance.OnEditorKey("Enter"));
		Assert.Empty(_peopleSaves);
		Assert.NotNull(cut.Find("td[data-editing]"));

		await cut.Find("td[data-editing] input").InputAsync(new ChangeEventArgs { Value = "50" });
		await cut.InvokeAsync(() => cut.Instance.OnEditorKey("Enter"));
		Assert.Equal(50, Assert.Single(_peopleSaves).Updated.Age);
	}

	[Fact]
	public async Task DataAnnotations_ThroughTheValidators_FileTheirMessagesUnderTheCell()
	{
		List<MokaTableRowSave<Pet>> saves = [];
		Pet[] pets = [new("Rex", 4)];
		IRenderedComponent<MokaTable<Pet>> cut = Render<MokaTable<Pet>>(p => p
			.Add(t => t.Items, pets)
			.Add(t => t.EditMode, MokaTableEditMode.Row)
			.Add(t => t.OnRowSave, save => saves.Add(save))
			.Add(t => t.Validators, builder =>
			{
				builder.OpenComponent<DataAnnotationsValidator>(0);
				builder.CloseComponent();
			})
			.Add(t => t.ChildContent, (RenderFragment)(builder =>
			{
				builder.OpenComponent<MokaColumn<Pet>>(0);
				builder.AddComponentParameter(1, nameof(MokaColumn<Pet>.Title), "Name");
				builder.AddComponentParameter(2, nameof(MokaColumn<Pet>.Field), (Func<Pet, object?>)(static x => x.Name));
				builder.AddComponentParameter(3, nameof(MokaColumn<Pet>.Editable), true);
				builder.AddComponentParameter(4, nameof(MokaColumn<Pet>.SetField),
					(Func<Pet, object?, Pet>)(static (p, v) => p with { Name = (string?)v ?? "" }));
				builder.CloseComponent();
				builder.OpenComponent<MokaNumberColumn<Pet>>(10);
				builder.AddComponentParameter(11, nameof(MokaNumberColumn<Pet>.Title), "Age");
				builder.AddComponentParameter(12, nameof(MokaNumberColumn<Pet>.Value), (Func<Pet, double?>)(static x => x.Age));
				builder.AddComponentParameter(13, nameof(MokaNumberColumn<Pet>.Editable), true);
				builder.AddComponentParameter(14, nameof(MokaNumberColumn<Pet>.SetValue),
					(Func<Pet, double?, Pet>)(static (p, v) => p with { Age = (int)(v ?? 0) }));
				builder.CloseComponent();
			})));

		await cut.InvokeAsync(() => cut.Instance.StartEditAsync(pets[0]));
		Assert.Equal(2, cut.FindAll("td[data-editing='row']").Count);
		await cut.FindAll("td[data-editing] input")[0].InputAsync(new ChangeEventArgs { Value = "" });
		await cut.FindAll("td[data-editing] input")[1].InputAsync(new ChangeEventArgs { Value = "99" });
		await cut.InvokeAsync(() => cut.Instance.SaveEditAsync());

		Assert.Empty(saves);
		string cells = string.Concat(cut.FindAll("td[data-editing]").Select(static td => td.TextContent));
		Assert.Contains("The Name field is required.", cells, StringComparison.Ordinal);
		Assert.Contains("The field Age must be between 0 and 30.", cells, StringComparison.Ordinal);
		Assert.Equal("Rex", pets[0].Name);
	}

	[Fact]
	public async Task AMessageFromTheAppsSave_KeepsTheRowOpenWithIt()
	{
		IRenderedComponent<MokaTable<Person>> cut = Render<MokaTable<Person>>(p => p
			.Add(t => t.Items, People)
			.Add(t => t.OnRowSave, save => save.AddError("Age", "Someone else changed this"))
			.Add(t => t.ChildContent, Columns(Name, Age with { Editable = true })));

		await cut.InvokeAsync(() => cut.Instance.OnCellKey(0, 1, "Enter"));
		await cut.Find("td[data-editing] input").InputAsync(new ChangeEventArgs { Value = "41" });
		await cut.InvokeAsync(() => cut.Instance.OnEditorKey("Enter"));

		Assert.Contains("Someone else changed this", cut.Find("td[data-editing]").TextContent, StringComparison.Ordinal);
	}

	[Fact]
	public async Task ASaveThatThrows_ShowsWhyOverTheRow_AndKeepsItOpen()
	{
		IRenderedComponent<MokaTable<Person>> cut = Render<MokaTable<Person>>(p => p
			.Add(t => t.Items, People)
			.Add(t => t.EditMode, MokaTableEditMode.Row)
			.Add(t => t.OnRowSave, _ => throw new InvalidOperationException("The server is away"))
			.Add(t => t.ChildContent, Columns(Name with { Editable = true }, Age with { Editable = true })));

		await cut.InvokeAsync(() => cut.Instance.StartEditAsync(People[0]));
		await cut.FindAll("td[data-editing] input")[0].InputAsync(new ChangeEventArgs { Value = "Ada L." });
		bool saved = await cut.InvokeAsync(() => cut.Instance.SaveEditAsync());

		Assert.False(saved);
		Assert.Contains("The server is away", cut.Find(".moka-table-edit-problems [role='alert']").TextContent, StringComparison.Ordinal);
		Assert.True(cut.Instance.IsEditing);
	}

	// ── Modes and keys ──

	[Fact]
	public async Task RowMode_OpensEveryEditableCell_AndSavesThemTogether()
	{
		IRenderedComponent<MokaTable<Person>> cut = Render<MokaTable<Person>>(p => p
			.Add(t => t.Items, People)
			.Add(t => t.EditMode, MokaTableEditMode.Row)
			.Add(t => t.OnRowSave, save => _peopleSaves.Add(save))
			.Add(t => t.ChildContent, Columns(
				Name with { Editable = true, SetField = static (p, v) => p with { Name = (string)v! } },
				Team,
				Age with { Editable = true, SetField = static (p, v) => p with { Age = (int)v! } })));

		await cut.InvokeAsync(() => cut.Instance.OnCellKey(1, 2, "F2"));
		Assert.Equal(2, cut.FindAll("td[data-editing='row']").Count);
		await cut.FindAll("td[data-editing] input")[0].InputAsync(new ChangeEventArgs { Value = "Grace H." });
		await cut.FindAll("td[data-editing] input")[1].InputAsync(new ChangeEventArgs { Value = "46" });
		await cut.InvokeAsync(() => cut.Instance.OnEditorKey("Enter"));

		MokaTableRowSave<Person> save = Assert.Single(_peopleSaves);
		Assert.Equal(["Name", "Age"], save.Changes.Select(static c => c.Column));
		Assert.Equal(new Person("Grace H.", "Research", 46, "Paris"), save.Updated);
	}

	[Fact]
	public async Task Escape_PutsTheEditBack_AndSavesNothing()
	{
		IRenderedComponent<MokaTable<Person>> cut = RenderPeople(Age with { Editable = true });

		await cut.InvokeAsync(() => cut.Instance.OnCellKey(0, 1, "Enter"));
		await cut.Find("td[data-editing] input").InputAsync(new ChangeEventArgs { Value = "99" });
		await cut.InvokeAsync(() => cut.Instance.OnEditorKey("Escape"));

		Assert.Empty(cut.FindAll("td[data-editing]"));
		Assert.Empty(_peopleSaves);
		Assert.Equal("36", cut.Find("tbody tr[data-row-index='0'] td[data-col-index='1']").TextContent.Trim());
	}

	[Fact]
	public async Task Tab_SavesTheCell_AndOpensTheNextEditableOne()
	{
		IRenderedComponent<MokaTable<Person>> cut = Render<MokaTable<Person>>(p => p
			.Add(t => t.Items, People)
			.Add(t => t.OnRowSave, save => _peopleSaves.Add(save))
			.Add(t => t.ChildContent, Columns(Name with { Editable = true }, Team, Age with { Editable = true })));

		await cut.InvokeAsync(() => cut.Instance.OnCellKey(0, 0, "Enter"));
		await cut.Find("td[data-editing] input").InputAsync(new ChangeEventArgs { Value = "Ada L." });
		await cut.InvokeAsync(() => cut.Instance.OnEditorKey("Tab"));

		Assert.Single(_peopleSaves);
		IElement open = cut.Find("td[data-editing]");
		Assert.Equal("2", open.GetAttribute("data-col-index"));

		await cut.InvokeAsync(() => cut.Instance.OnEditorKey("Tab"));
		Assert.Equal("1", cut.Find("td[data-editing]").ParentElement!.GetAttribute("data-row-index"));
		Assert.Equal("0", cut.Find("td[data-editing]").GetAttribute("data-col-index"));

		await cut.InvokeAsync(() => cut.Instance.OnEditorKey("Shift+Tab"));
		Assert.Equal("0", cut.Find("td[data-editing]").ParentElement!.GetAttribute("data-row-index"));
		Assert.Equal("2", cut.Find("td[data-editing]").GetAttribute("data-col-index"));
	}

	[Fact]
	public async Task LeavingTheCell_SavesIt()
	{
		IRenderedComponent<MokaTable<Person>> cut = RenderPeople(Age with { Editable = true });

		await cut.InvokeAsync(() => cut.Instance.OnCellKey(0, 1, "Enter"));
		await cut.Find("td[data-editing] input").InputAsync(new ChangeEventArgs { Value = "37" });
		await cut.InvokeAsync(() => cut.Instance.OnEditorLeft(0, 1));

		Assert.Single(_peopleSaves);
		Assert.Empty(cut.FindAll("td[data-editing]"));
	}

	// Tab saves the cell and opens the next before the focusout of the cell it left arrives from the browser. That late
	// call names the old cell, so it leaves the new editor open.
	[Fact]
	public async Task LeavingACellThatIsNoLongerOpen_KeepsTheNextEditorOpen()
	{
		IRenderedComponent<MokaTable<Person>> cut = Render<MokaTable<Person>>(p => p
			.Add(t => t.Items, People)
			.Add(t => t.OnRowSave, save => _peopleSaves.Add(save))
			.Add(t => t.ChildContent, Columns(Name with { Editable = true }, Team, Age with { Editable = true })));

		await cut.InvokeAsync(() => cut.Instance.OnCellKey(0, 0, "Enter"));
		await cut.Find("td[data-editing] input").InputAsync(new ChangeEventArgs { Value = "Ada L." });
		await cut.InvokeAsync(() => cut.Instance.OnEditorKey("Tab"));
		await cut.InvokeAsync(() => cut.Instance.OnEditorLeft(0, 0));

		Assert.Single(_peopleSaves);
		Assert.Equal("2", cut.Find("td[data-editing]").GetAttribute("data-col-index"));
	}

	[Fact]
	public async Task DoubleClick_OpensTheCell()
	{
		IRenderedComponent<MokaTable<Person>> cut = RenderPeople(Age with { Editable = true });

		await cut.Find("tbody tr[data-row-index='1'] td[data-col-index='1']").DoubleClickAsync(new MouseEventArgs());

		Assert.NotNull(cut.Find("tbody tr[data-row-index='1'] td[data-editing]"));
	}

	[Fact]
	public async Task OpeningTheEditor_FocusesIt()
	{
		BunitJSModuleInterop module = JSInterop.SetupModule(TableModule);
		module.Mode = JSRuntimeMode.Loose;
		IRenderedComponent<MokaTable<Person>> cut = RenderPeople(Age with { Editable = true });

		await cut.InvokeAsync(() => cut.Instance.OnCellKey(0, 1, "F2"));

		Assert.Equal(1, module.VerifyInvoke("focusEditInput").Arguments[1]);
	}

	// An EditForm around the table keeps its fields: the editors don't join its EditContext and mark it changed.
	[Fact]
	public async Task AnEditFormAroundTheTable_KeepsItsOwnFields()
	{
		EditContext outer = new(new object());
		IRenderedComponent<CascadingValue<EditContext>> host = Render<CascadingValue<EditContext>>(p => p
			.Add(c => c.Value, outer)
			.Add(c => c.IsFixed, true)
			.AddChildContent<MokaTable<Person>>(t => t
				.Add(x => x.Items, People)
				.Add(x => x.ChildContent, Columns(Name, Age with { Editable = true }))));
		IRenderedComponent<MokaTable<Person>> cut = host.FindComponent<MokaTable<Person>>();

		await cut.InvokeAsync(() => cut.Instance.OnCellKey(0, 1, "Enter"));
		await cut.Find("td[data-editing] input").InputAsync(new ChangeEventArgs { Value = "50" });

		Assert.False(outer.IsModified());
	}

	// ── Adding and deleting ──

	[Theory]
	[InlineData(MokaTableAddPosition.Top, 0)]
	[InlineData(MokaTableAddPosition.Bottom, -1)]
	public async Task Add_OpensANewRow_AtItsPlace_AndSavesItAsNew(MokaTableAddPosition position, int at)
	{
		IRenderedComponent<MokaTable<Person>> cut = Render<MokaTable<Person>>(p => p
			.Add(t => t.Items, People)
			.Add(t => t.NewItem, static () => new Person("", "Engineering", 30, "Oslo"))
			.Add(t => t.AddRowPosition, position)
			.Add(t => t.OnRowSave, save => _peopleSaves.Add(save))
			.Add(t => t.ChildContent, Columns(
				Name with { Editable = true, SetField = static (p, v) => p with { Name = (string)v! } }, Team)));

		await cut.Find("button.moka-table-add").ClickAsync(new MouseEventArgs());
		IReadOnlyList<IElement> rows = cut.FindAll("tbody > tr");
		IElement added = at >= 0 ? rows[at] : rows[^1];
		Assert.True(added.HasAttribute("data-new-row"));
		Assert.Equal("Engineering", added.QuerySelectorAll("td")[1].TextContent.Trim());

		await added.QuerySelector("td[data-editing] input")!.InputAsync(new ChangeEventArgs { Value = "Linus" });
		await cut.InvokeAsync(() => cut.Instance.OnEditorKey("Enter"));

		MokaTableRowSave<Person> save = Assert.Single(_peopleSaves);
		Assert.True(save.IsNew);
		Assert.Equal(MokaTableSaveKind.Add, save.Kind);
		Assert.Equal(new Person("Linus", "Engineering", 30, "Oslo"), save.Updated);
		Assert.Empty(cut.FindAll("tr[data-new-row]"));
	}

	[Fact]
	public async Task Delete_AsksFirst_ThenRaisesOnRowDelete()
	{
		FakeDialogs dialogs = new() { Answer = true };
		Services.AddSingleton<IMokaDialogService>(dialogs);
		List<Person> deleted = [];
		IRenderedComponent<MokaTable<Person>> cut = RenderWithEditColumn(deleted);

		await PressAsync(cut, 1, "Delete");

		Assert.Equal("Delete this row?", dialogs.Title);
		Assert.Equal("Grace will be deleted.", dialogs.Message);
		Assert.Equal(People[1], Assert.Single(deleted));
	}

	[Fact]
	public async Task Delete_ThatIsRefused_DeletesNothing()
	{
		Services.AddSingleton<IMokaDialogService>(new FakeDialogs { Answer = false });
		List<Person> deleted = [];
		IRenderedComponent<MokaTable<Person>> cut = RenderWithEditColumn(deleted);

		await PressAsync(cut, 0, "Delete");

		Assert.Empty(deleted);
	}

	[Fact]
	public async Task Delete_WithoutAConfirm_DeletesAtOnce()
	{
		FakeDialogs dialogs = new();
		Services.AddSingleton<IMokaDialogService>(dialogs);
		List<Person> deleted = [];
		IRenderedComponent<MokaTable<Person>> cut = RenderWithEditColumn(deleted, confirm: false);

		await PressAsync(cut, 0, "Delete");

		Assert.Null(dialogs.Title);
		Assert.Equal(People[0], Assert.Single(deleted));
	}

	[Fact]
	public async Task TheEditColumn_ShowsEditAndDelete_ThenSaveAndCancel()
	{
		IRenderedComponent<MokaTable<Person>> cut = RenderWithEditColumn([]);
		IElement row = cut.Find("tbody tr[data-row-index='0']");
		Assert.Equal(["Edit", "Delete"], row.QuerySelectorAll(".moka-actions-btn").Select(static b => b.GetAttribute("aria-label")));

		await PressAsync(cut, 0, "Edit");

		row = cut.Find("tbody tr[data-row-index='0']");
		Assert.Equal(["Save", "Cancel"], row.QuerySelectorAll(".moka-actions-btn").Select(static b => b.GetAttribute("aria-label")));
		await PressAsync(cut, 0, "Cancel");
		Assert.False(cut.Instance.IsEditing);
	}

	// ── Undo ──

	[Fact]
	public async Task Undo_PutsBackTheValuesTheLastSaveChanged()
	{
		List<Person> people = [.. People];
		IRenderedComponent<MokaTable<Person>> cut = null!;
		cut = Render<MokaTable<Person>>(p => p
			.Add(t => t.Items, people)
			.Add(t => t.ItemName, static x => x.Name)
			.Add(t => t.OnRowSave, save =>
			{
				_peopleSaves.Add(save);
				people[people.FindIndex(x => x.City == save.Item.City)] = save.Updated;
			})
			.Add(t => t.ChildContent, Columns(Name, Age with { Editable = true, SetField = static (p, v) => p with { Age = (int)v! } })));

		await cut.InvokeAsync(() => cut.Instance.OnCellKey(0, 1, "Enter"));
		await cut.Find("td[data-editing] input").InputAsync(new ChangeEventArgs { Value = "40" });
		await cut.InvokeAsync(() => cut.Instance.OnEditorKey("Enter"));
		Assert.True(cut.Instance.CanUndo);
		Assert.Equal("Changes to Ada saved.", cut.Find(".moka-table-undo-text").TextContent.Trim());

		await cut.InvokeAsync(() => cut.Instance.OnCellKey(0, 0, "Undo"));

		Assert.Equal(2, _peopleSaves.Count);
		MokaTableRowSave<Person> undo = _peopleSaves[1];
		Assert.Equal(MokaTableSaveKind.Undo, undo.Kind);
		Assert.Equal(new MokaTableCellChange("Age", 40, 36), Assert.Single(undo.Changes));
		Assert.Equal(36, undo.Updated.Age);
		Assert.False(cut.Instance.CanUndo);
		Assert.Equal(36, people[0].Age);
	}

	private readonly List<MokaTableRowSave<Person>> _peopleSaves = [];

	// What the browser sends for a choice: the option's own value, which may be its position in the list.
	private static string OptionValue<T>(IRenderedComponent<MokaTable<T>> cut, string text) =>
		cut.FindAll("td[data-editing] option").First(o => o.TextContent.Trim() == text).GetAttribute("value")!;

	// A button of an edit column is heard by the table's one click listener in the browser, which calls OnCellAction.
	private static Task PressAsync(IRenderedComponent<MokaTable<Person>> cut, int row, string label)
	{
		string action = cut.Find($"tbody tr[data-row-index='{row}'] button[aria-label='{label}']").GetAttribute("data-moka-act")!;
		return cut.InvokeAsync(() => cut.Instance.OnCellAction(row.ToString(System.Globalization.CultureInfo.InvariantCulture), action, 0, 0, 0));
	}

	private IRenderedComponent<MokaTable<Person>> RenderPeople(Col editable) =>
		Render<MokaTable<Person>>(p => p
			.Add(t => t.Items, People)
			.Add(t => t.OnRowSave, save => _peopleSaves.Add(save))
			.Add(t => t.ChildContent, Columns(Name, editable)));

	private IRenderedComponent<MokaTable<Person>> RenderWithEditColumn(List<Person> deleted, bool confirm = true) =>
		Render<MokaTable<Person>>(p => p
			.Add(t => t.Items, People)
			.Add(t => t.ItemName, static x => x.Name)
			.Add(t => t.EditMode, MokaTableEditMode.Row)
			.Add(t => t.ConfirmDelete, confirm)
			.Add(t => t.OnRowSave, save => _peopleSaves.Add(save))
			.Add(t => t.OnRowDelete, item => deleted.Add(item))
			.Add(t => t.ChildContent, (RenderFragment)(builder =>
			{
				Columns(Name with { Editable = true }, Age)(builder);
				builder.OpenComponent<MokaEditColumn<Person>>(100);
				builder.CloseComponent();
			})));

	private IRenderedComponent<MokaTable<Gadget>> RenderGadgets(bool switchLook = false) =>
		Render<MokaTable<Gadget>>(p => p
			.Add(t => t.Items, Gadgets)
			.Add(t => t.OnRowSave, save => _saves.Add(save))
			.Add(t => t.ChildContent, (RenderFragment)(builder => GadgetColumns(builder, switchLook))));

	private static void GadgetColumns(RenderTreeBuilder builder, bool switchLook)
	{
		builder.OpenComponent<MokaColumn<Gadget>>(0);
		builder.AddComponentParameter(1, "Title", "Name");
		builder.AddComponentParameter(2, "Field", (Func<Gadget, object?>)(static x => x.Name));
		builder.AddComponentParameter(3, "Editable", true);
		builder.AddComponentParameter(4, "SetField", (Func<Gadget, object?, Gadget>)(static (g, v) => g with { Name = (string)v! }));
		builder.CloseComponent();

		builder.OpenComponent<MokaNumberColumn<Gadget>>(10);
		builder.AddComponentParameter(11, "Title", "Price");
		builder.AddComponentParameter(12, "Value", (Func<Gadget, double?>)(static x => x.Price));
		builder.AddComponentParameter(13, "Editable", true);
		builder.AddComponentParameter(14, "SetValue", (Func<Gadget, double?, Gadget>)(static (g, v) => g with { Price = v ?? 0 }));
		builder.CloseComponent();

		builder.OpenComponent<MokaDateColumn<Gadget>>(20);
		builder.AddComponentParameter(21, "Title", "Made");
		builder.AddComponentParameter(22, "Value", (Func<Gadget, DateTimeOffset?>)(static x => x.Made));
		builder.AddComponentParameter(23, "Editable", true);
		builder.AddComponentParameter(24, "SetValue", (Func<Gadget, DateTimeOffset?, Gadget>)(static (g, v) => g with { Made = v ?? g.Made }));
		builder.CloseComponent();

		builder.OpenComponent<MokaYesNoColumn<Gadget>>(30);
		builder.AddComponentParameter(31, "Title", "Stock");
		builder.AddComponentParameter(32, "Value", (Func<Gadget, bool?>)(static x => x.InStock));
		builder.AddComponentParameter(33, "Editable", true);
		builder.AddComponentParameter(34, "SetValue", (Func<Gadget, bool?, Gadget>)(static (g, v) => g with { InStock = v ?? false }));
		builder.AddComponentParameter(35, "Look", switchLook ? MokaYesNoLook.Switch : MokaYesNoLook.Check);
		builder.CloseComponent();

		builder.OpenComponent<MokaStatusColumn<Gadget>>(40);
		builder.AddComponentParameter(41, "Title", "Status");
		builder.AddComponentParameter(42, "Status", (Func<Gadget, string?>)(static x => x.Status));
		builder.AddComponentParameter(43, "Editable", true);
		builder.AddComponentParameter(44, "SetStatus", (Func<Gadget, string?, Gadget>)(static (g, v) => g with { Status = v ?? "" }));
		builder.CloseComponent();

		builder.OpenComponent<MokaRatingColumn<Gadget>>(50);
		builder.AddComponentParameter(51, "Title", "Stars");
		builder.AddComponentParameter(52, "Value", (Func<Gadget, double?>)(static x => x.Stars));
		builder.AddComponentParameter(53, "Editable", true);
		builder.AddComponentParameter(54, "SetValue", (Func<Gadget, double?, Gadget>)(static (g, v) => g with { Stars = v ?? 0 }));
		builder.CloseComponent();

		builder.OpenComponent<MokaColumn<Gadget>>(60);
		builder.AddComponentParameter(61, "Title", "Shade");
		builder.AddComponentParameter(62, "Field", (Func<Gadget, object?>)(static x => x.Shade));
		builder.AddComponentParameter(63, "Editable", true);
		builder.AddComponentParameter(64, "SetField", (Func<Gadget, object?, Gadget>)(static (g, v) => g with { Shade = (Shade)v! }));
		builder.CloseComponent();
	}

	internal enum Shade
	{
		Light,
		Dark
	}

	internal sealed record Gadget(string Name, double Price, DateTimeOffset Made, bool InStock, string Status, double Stars, Shade Shade);

	internal sealed record Pet([property: Required] string Name, [property: Range(0, 30)] int Age);

	// Answers every confirm the same way and runs the work it was given, as the real dialog does on OK.
	private sealed class FakeDialogs : IMokaDialogService
	{
		public bool Answer { get; init; }

		public string? Title { get; private set; }

		public string? Message { get; private set; }

#pragma warning disable CS0067 // The table listens to none of these.
		public event Action<MokaDialogRequest>? OnDialogRequested;

		public event Action? OnDialogClosed;
#pragma warning restore CS0067

		public IReadOnlyList<MokaDialogRequest> OpenDialogs => [];

		public async Task<bool> ConfirmAsync(string message, string? title = null, Action<MokaDialogOptions>? configure = null)
		{
			Message = message;
			Title = title;
			MokaDialogOptions options = new();
			configure?.Invoke(options);
			if (Answer && options.OnConfirmAsync is { } work)
			{
				await work(CancellationToken.None);
			}

			return Answer;
		}

		public Task<string?> PromptAsync(string message, string? title = null, string? defaultValue = null) => Task.FromResult<string?>(null);

		public Task<string?> PromptAsync(string message, string? title, string? defaultValue, Action<MokaDialogOptions>? configure) =>
			Task.FromResult<string?>(null);

		public Task ShowAsync(string title, RenderFragment content, Action<MokaDialogOptions>? configure = null) => Task.CompletedTask;

		public Task<object?> ShowComponentAsync<TComponent>(string title, Action<Dictionary<string, object>>? parameters = null,
			Action<MokaDialogOptions>? configure = null) where TComponent : IComponent => Task.FromResult<object?>(null);

		public void Close(bool result = false)
		{
		}

		public void CloseWithResult(object? result)
		{
		}

		public void Close(MokaDialogRequest request, bool result = false)
		{
		}

		public void CloseWithResult(MokaDialogRequest request, object? result)
		{
		}

		public void CloseAll()
		{
		}
	}
}
