using System.Reflection;
using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Moka.Red.Core.Enums;
using Moka.Red.Data.Table;
using Moka.Red.Icons;
using static Moka.Red.Data.Tests.Components.TableKit;

namespace Moka.Red.Data.Tests.Table;

// What the phase three demos showed in a browser: editing a number that can be empty, focus that follows a saved row,
// a focusout that arrives late, totals and focus under column virtualization, open details among rows a server sends
// window by window, placeholders as tall as rows, the edit column's own buttons, and copying rows without a bar.
public class MokaTableBrowserFixTests : BunitContext
{
	private static readonly Bottle[] Bottles = [new("Water", null), new("Juice", 1.5)];

	private readonly List<MokaTableRowSave<Bottle>> _bottleSaves = [];
	private readonly List<MokaTableRowSave<Person>> _saves = [];

	public MokaTableBrowserFixTests() => JSInterop.Mode = JSRuntimeMode.Loose;

	// ── Numbers that can be empty ──

	[Fact]
	public async Task ANumberThatCanBeEmpty_OpensWithAnEmptyBox()
	{
		IRenderedComponent<MokaTable<Bottle>> cut = RenderBottles();

		await cut.InvokeAsync(() => cut.Instance.StartEditAsync(Bottles[0], "Volume"));

		Assert.Equal(string.Empty, cut.Find("td[data-editing] input").GetAttribute("value") ?? string.Empty);
	}

	[Fact]
	public async Task EmptyingANumberThatCanBeEmpty_SavesNull()
	{
		IRenderedComponent<MokaTable<Bottle>> cut = RenderBottles();

		await cut.InvokeAsync(() => cut.Instance.StartEditAsync(Bottles[1], "Volume"));
		await cut.Find("td[data-editing] input").InputAsync(new ChangeEventArgs { Value = string.Empty });
		await cut.InvokeAsync(() => cut.Instance.OnEditorKey("Enter"));

		MokaTableRowSave<Bottle> save = Assert.Single(_bottleSaves);
		Assert.Equal(new MokaTableCellChange("Volume", 1.5, null), Assert.Single(save.Changes));
		Assert.Null(save.Updated.Volume);
	}

	// A plain column of whole numbers can't hold null, so an emptied box is refused rather than saved as anything.
	[Fact]
	public async Task EmptyingANumberThatCantBeEmpty_IsRefused()
	{
		IRenderedComponent<MokaTable<Person>> cut = Render<MokaTable<Person>>(p => p
			.Add(t => t.Items, People)
			.Add(t => t.OnRowSave, save => _saves.Add(save))
			.Add(t => t.ChildContent, Columns(Name, Age with
			{
				Editable = true,
				Editor = MokaColumnEditor.Number,
				SetField = static (x, v) => x with { Age = (int)v! }
			})));

		await cut.InvokeAsync(() => cut.Instance.StartEditAsync(People[0], "Age"));
		await cut.Find("td[data-editing] input").InputAsync(new ChangeEventArgs { Value = string.Empty });
		await cut.InvokeAsync(() => cut.Instance.OnEditorKey("Enter"));

		Assert.Empty(_saves);
		Assert.NotNull(cut.Find("td[data-editing]"));
		Assert.Contains("Enter a valid value for Age", cut.Find("td[data-editing]").TextContent, StringComparison.Ordinal);
	}

	// ── Focus after a save ──

	[Fact]
	public async Task AnAddedRow_TakesFocus_WhereItLands()
	{
		BunitJSModuleInterop module = JSInterop.SetupModule(TableModule);
		module.Mode = JSRuntimeMode.Loose;
		List<Person> people = [.. People];
		IRenderedComponent<MokaTable<Person>> cut = Render<MokaTable<Person>>(p => p
			.Add(t => t.Items, people)
			.Add(t => t.ItemKey, static x => x.Name)
			.Add(t => t.NewItem, static () => new Person("", "New", 0, ""))
			.Add(t => t.AddRowPosition, MokaTableAddPosition.Bottom)
			.Add(t => t.OnRowSave, save => people.Add(save.Updated))
			.Add(t => t.ChildContent, Columns(Name with { Editable = true, SetField = static (x, v) => x with { Name = (string)v! } })));

		await cut.InvokeAsync(() => cut.Instance.AddRowAsync());
		await cut.Find("tr[data-new-row] input").InputAsync(new ChangeEventArgs { Value = "Zed" });
		await cut.InvokeAsync(() => cut.Instance.OnEditorKey("Enter"));

		JSRuntimeInvocation focus = module.Invocations["focusCell"][^1];
		Assert.Equal(people.Count - 1, focus.Arguments[1]);
		Assert.Equal("Zed", DataRows(cut)[^1].QuerySelector("td[data-col-index='0']")!.TextContent.Trim());
	}

	// Under a sort on the edited column the saved row moves; focus goes after it, and Tab opens that row's next cell.
	[Fact]
	public async Task ASaveThatMovesTheRow_KeepsFocusAndTabOnThatRow()
	{
		BunitJSModuleInterop module = JSInterop.SetupModule(TableModule);
		module.Mode = JSRuntimeMode.Loose;
		List<Person> people = [.. People];
		IRenderedComponent<MokaTable<Person>> cut = Render<MokaTable<Person>>(p => p
			.Add(t => t.Items, people)
			.Add(t => t.ItemKey, static x => x.Name)
			.Add(t => t.State, SortedBy("Age", MokaSortDirection.Ascending))
			.Add(t => t.OnRowSave, save => people[people.FindIndex(x => x.Name == save.Item.Name)] = save.Updated)
			.Add(t => t.ChildContent, Columns(Name, Age with
			{
				Editable = true,
				SetField = static (x, v) => x with { Age = Convert.ToInt32(v, System.Globalization.CultureInfo.InvariantCulture) }
			}, City with { Editable = true, SetField = static (x, v) => x with { City = (string)v! } })));
		string youngest = ColumnTexts(cut, 0)[0];

		await cut.InvokeAsync(() => cut.Instance.OnCellKey(0, 1, "Enter"));
		await cut.Find("td[data-editing] input").InputAsync(new ChangeEventArgs { Value = "99" });
		await cut.InvokeAsync(() => cut.Instance.OnEditorKey("Tab"));

		int last = DataRows(cut).Count - 1;
		Assert.Equal(youngest, ColumnTexts(cut, 0)[last]);
		IElement open = cut.Find("td[data-editing]");
		Assert.Equal(last.ToString(System.Globalization.CultureInfo.InvariantCulture), open.ParentElement!.GetAttribute("data-row-index"));
		Assert.Equal("2", open.GetAttribute("data-col-index"));
	}

	// ── Columns in view ──

	[Fact]
	public async Task GroupHeadings_DrawTheTotalsOfTheColumnsInView()
	{
		IRenderedComponent<MokaTable<Person>> cut = WindowTable(p => p.Add(t => t.GroupBy, "Team"));

		IElement heading = cut.Find("tr.moka-table-group-row");
		Assert.Equal(AriaColumns(cut), heading.Children.Sum(static td => int.Parse(td.GetAttribute("colspan") ?? "1", System.Globalization.CultureInfo.InvariantCulture)));
		int drawnTotals = DataRows(cut)[0].QuerySelectorAll("td[data-col-index]").Length - 2;
		Assert.Equal(drawnTotals, heading.QuerySelectorAll(".moka-table-group-total").Length);

		await cut.InvokeAsync(() => cut.Instance.OnColumnWindow(30, 35));

		heading = cut.Find("tr.moka-table-group-row");
		Assert.Equal(AriaColumns(cut), heading.Children.Sum(static td => int.Parse(td.GetAttribute("colspan") ?? "1", System.Globalization.CultureInfo.InvariantCulture)));
		Assert.Equal(6, heading.QuerySelectorAll(".moka-table-group-total").Length);
		Assert.Equal(2, heading.QuerySelectorAll(".moka-table-col-gap").Length);
	}

	// Cells shift places among their siblings as the window moves. Unkeyed, Blazor handed a cell's element, and the
	// component in it, to another column, which is how focus jumped columns in the browser.
	[Fact]
	public async Task AsTheWindowMoves_ACellKeepsItsElement()
	{
		Probe.Seen.Clear();
		IRenderedComponent<MokaTable<Person>> cut = WindowTable(probes: true);

		await cut.InvokeAsync(() => cut.Instance.OnColumnWindow(5, 12));
		await cut.InvokeAsync(() => cut.Instance.OnColumnWindow(8, 15));

		Assert.NotEmpty(Probe.Seen);
		Assert.All(Probe.Seen, probe => Assert.False(probe.Moved, $"A cell of {probe.First} was handed to another column."));
	}

	// ── Rows a server sends window by window ──

	[Fact]
	public async Task AnOpenDetail_PushesTheRowsBelowIt_InAWindowFromAServer()
	{
		IRenderedComponent<MokaTable<Person>> cut = Render<MokaTable<Person>>(p => p
			.Add(t => t.Virtualize, true)
			.Add(t => t.Height, "300px")
			.Add(t => t.Expandable, true)
			.Add(t => t.ItemKey, static x => x.Name)
			.Add(t => t.DetailTemplate, item => builder => builder.AddContent(0, item.City))
			.Add(t => t.ServerData, request => Task.FromResult(new MokaTableResult<Person>
			{
				Items = [.. Enumerable.Range(request.StartIndex, request.PageSize).Select(static i => new Person($"Row {i}", "T", i, "C"))],
				TotalItems = 1000
			}))
			.Add(t => t.ChildContent, DefaultColumns));
		cut.WaitForAssertion(() => Assert.True(DataRows(cut).Count >= 3));
		Assert.Equal("1001", cut.Find("table").GetAttribute("aria-rowcount"));

		await DataRows(cut)[1].QuerySelector(".moka-table-expand-btn")!.ClickAsync(new MouseEventArgs());

		Assert.Equal("1002", cut.Find("table").GetAttribute("aria-rowcount"));
		Assert.Equal("4", cut.Find(".moka-table-detail-row").GetAttribute("aria-rowindex"));
		Assert.Equal(["2", "3", "5"], DataRows(cut).Take(3).Select(static r => r.GetAttribute("aria-rowindex")));
	}

	// Virtualize places rows by one height and needs its placeholders that tall: shorter ones moved the window asked for
	// on every render, so every request cancelled the last.
	[Fact]
	public void PlaceholderRows_AreAsTallAsVirtualizeThinksARowIs()
	{
		IRenderedComponent<MokaTable<Person>> cut = Render<MokaTable<Person>>(p => p
			.Add(t => t.Virtualize, true)
			.Add(t => t.Height, "300px")
			.Add(t => t.VirtualRowHeight, 37.5f)
			.Add(t => t.ServerData, _ => new TaskCompletionSource<MokaTableResult<Person>>().Task)
			.Add(t => t.ChildContent, DefaultColumns));

		cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll(".moka-table-placeholder-row")));
		Assert.All(cut.FindAll(".moka-table-placeholder-row"), row => Assert.Equal("height: 37.5px", row.GetAttribute("style")));
	}

	// ── The edit column ──

	// Its Actions came from the actions column with [EditorRequired], so markup that used it as documented didn't build.
	[Fact]
	public void TheEditColumn_DoesNotRequireActions()
	{
		PropertyInfo actions = typeof(MokaEditColumn<Person>).GetProperty(nameof(MokaEditColumn<Person>.Actions))!;

		Assert.Equal(typeof(MokaEditColumn<Person>), actions.DeclaringType);
		Assert.NotNull(actions.GetCustomAttribute<ParameterAttribute>(inherit: false));
		Assert.Null(actions.GetCustomAttribute<EditorRequiredAttribute>(inherit: false));
	}

	[Fact]
	public void TheEditColumn_DrawsItsButtons_ThenTheAppsOwn_WithoutWritingItsParameter()
	{
		IReadOnlyList<MokaTableAction<Person>> extra = [new("Duplicate", MokaIcons.Content.Copy, static _ => Task.CompletedTask)];
		IRenderedComponent<MokaTable<Person>> cut = Render<MokaTable<Person>>(p => p
			.Add(t => t.Items, People)
			.Add(t => t.OnRowSave, save => _saves.Add(save))
			.Add(t => t.ChildContent, (RenderFragment)(builder =>
			{
				Columns(Name with { Editable = true })(builder);
				builder.OpenComponent<MokaEditColumn<Person>>(50);
				builder.AddComponentParameter(51, "Actions", extra);
				builder.CloseComponent();
			})));

		Assert.Equal(["Edit", "Duplicate"],
			DataRows(cut)[0].QuerySelectorAll(".moka-actions-btn").Select(static b => b.GetAttribute("aria-label")));
		Assert.Same(extra, cut.FindComponent<MokaEditColumn<Person>>().Instance.Actions);
	}

	// ── Copying rows ──

	[Fact]
	public async Task CopyingRowsAlone_ShowsTheSelectionBar_WithCopy()
	{
		IRenderedComponent<MokaTable<Person>> cut = Render<MokaTable<Person>>(p => p
			.Add(t => t.Items, People)
			.Add(t => t.Selectable, true)
			.Add(t => t.CopySelectedRows, true)
			.Add(t => t.ChildContent, DefaultColumns));
		Assert.Empty(cut.FindAll(".moka-table-selection-bar"));

		await cut.FindAll("tbody td.moka-table-cell--checkbox input")[0].ChangeAsync(new ChangeEventArgs { Value = true });

		IElement bar = cut.Find(".moka-table-selection-bar");
		Assert.Contains("Copy row", bar.TextContent, StringComparison.Ordinal);
	}

	// ── The script ──

	// Sideways keys under column virtualization go by column number, and the aim at a row not drawn is reckoned from a
	// drawn row near it. Run in Node against the real moka-table.js.
	[Fact]
	public void TheScript_StepsByColumnNumber_AndAimsFromADrawnRow()
	{
		System.Text.Json.JsonElement? result = ScriptRunner.Run(
			["tests", "Moka.Red.Data.Tests", "Table", "Script", "table-keys.mjs"],
			["src", "Moka.Red.Data", "wwwroot", "moka-table.js"]);
		Assert.SkipUnless(result is not null, "Node.js isn't installed, so the table script can't be run.");
		System.Text.Json.JsonElement json = result!.Value;

		Assert.Equal(3, json.GetProperty("rightInside").GetProperty("shown").GetInt32());
		Assert.Equal(5, json.GetProperty("overHidden").GetProperty("shown").GetInt32());
		Assert.Equal(10, json.GetProperty("rightOutOfView").GetProperty("missing").GetInt32());
		Assert.Equal(58, json.GetProperty("leftOutOfView").GetProperty("missing").GetInt32());
		Assert.Equal(3, json.GetProperty("leftInside").GetProperty("shown").GetInt32());
		Assert.Equal(0, json.GetProperty("home").GetProperty("shown").GetInt32());
		Assert.Equal(59, json.GetProperty("end").GetProperty("shown").GetInt32());
		Assert.Equal(System.Text.Json.JsonValueKind.Null, json.GetProperty("pastTheEnd").ValueKind);
		Assert.Equal(36808, json.GetProperty("aimBelow").GetDouble());
		Assert.Equal(3508, json.GetProperty("aimAbove").GetDouble());
		Assert.Equal(0, json.GetProperty("aimTop").GetDouble());
	}

	private static int AriaColumns(IRenderedComponent<MokaTable<Person>> cut) =>
		int.Parse(cut.Find("table").GetAttribute("aria-colcount")!, System.Globalization.CultureInfo.InvariantCulture);

	private IRenderedComponent<MokaTable<Bottle>> RenderBottles() =>
		Render<MokaTable<Bottle>>(p => p
			.Add(t => t.Items, Bottles)
			.Add(t => t.OnRowSave, save => _bottleSaves.Add(save))
			.Add(t => t.ChildContent, (RenderFragment)(static builder =>
			{
				builder.OpenComponent<MokaColumn<Bottle>>(0);
				builder.AddComponentParameter(1, "Title", "Name");
				builder.AddComponentParameter(2, "Field", (Func<Bottle, object?>)(static x => x.Name));
				builder.CloseComponent();

				builder.OpenComponent<MokaNumberColumn<Bottle>>(10);
				builder.AddComponentParameter(11, "Title", "Volume");
				builder.AddComponentParameter(12, "Value", (Func<Bottle, double?>)(static x => x.Volume));
				builder.AddComponentParameter(13, "Editable", true);
				builder.AddComponentParameter(14, "SetValue", (Func<Bottle, double?, Bottle>)(static (b, v) => b with { Volume = v }));
				builder.CloseComponent();
			})));

	// A pinned name and 40 columns of ages with totals, the first 12 drawn.
	private IRenderedComponent<MokaTable<Person>> WindowTable(
		Action<ComponentParameterCollectionBuilder<MokaTable<Person>>>? configure = null, bool probes = false)
	{
		List<Col> columns = [Name with { Pin = MokaColumnPin.Start }, Team];
		columns.AddRange(Enumerable.Range(1, 40).Select(i => new Col($"C{i}", static x => x.Age)
		{
			Aggregate = MokaAggregateType.Sum,
			CellTemplate = probes ? Probe.For($"C{i}") : null
		}));
		return Render<MokaTable<Person>>(p =>
		{
			p.Add(t => t.Items, People).Add(t => t.VirtualizeColumns, true).Add(t => t.ChildContent, Columns([.. columns]));
			configure?.Invoke(p);
		});
	}

	internal sealed record Bottle(string Name, double? Volume);

	/// <summary>Remembers the column it was first drawn for, and whether a render later gave it another.</summary>
	private sealed class Probe : ComponentBase
	{
		public static readonly List<Probe> Seen = [];

		[Parameter]
		public string Column { get; set; } = string.Empty;

		public string? First { get; private set; }

		public bool Moved { get; private set; }

		public static RenderFragment<Person> For(string column) => _ => builder =>
		{
			builder.OpenComponent<Probe>(0);
			builder.AddComponentParameter(1, nameof(Column), column);
			builder.CloseComponent();
		};

		protected override void OnInitialized() => Seen.Add(this);

		protected override void OnParametersSet()
		{
			First ??= Column;
			Moved |= First != Column;
		}

		protected override void BuildRenderTree(RenderTreeBuilder builder) => builder.AddContent(0, Column);
	}
}
