using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Moka.Red.Data.Table;
using Moka.Red.Feedback.Tooltip;
using Moka.Red.Tests.Shared;
using static Moka.Red.Data.Tests.Components.TableKit;

namespace Moka.Red.Data.Tests.Components;

// Column arrangement: the columns part of the state the table raises and takes, the chooser, a heading's edge, and
// fixed columns.
public class MokaTableLayoutTests : BunitContext
{
	public MokaTableLayoutTests() => JSInterop.Mode = JSRuntimeMode.Loose;

	[Fact]
	public void AnArrangement_PutsTheColumnsBack()
	{
		IRenderedComponent<MokaTable<Person>> cut = Table(p => p.Add(t => t.State, Arranged(
			Column("City", width: 240), Column("Name"), Column("Team", hidden: true), Column("Age"))));

		Assert.Equal(["City", "Name", "Age"], HeaderTitles(cut));
		Assert.Contains("width: 240px", HeaderCell(cut, "City").GetAttribute("style"), StringComparison.Ordinal);
	}

	[Fact]
	public void ATitleNoColumnHas_IsIgnored()
	{
		IRenderedComponent<MokaTable<Person>> cut = Table(p => p.Add(t => t.State, Arranged(
			Column("Gone", width: 100), Column("City"), Column("Also gone", hidden: true))));

		Assert.Equal(["City", "Name", "Team", "Age"], HeaderTitles(cut));
	}

	[Fact]
	public void AColumnTheArrangementDoesNotList_GoesAfterTheOnesItDoes()
	{
		IRenderedComponent<MokaTable<Person>> cut = Table(p => p.Add(t => t.State, Arranged(Column("Age"), Column("Team"))));

		Assert.Equal(["Age", "Team", "Name", "City"], HeaderTitles(cut));
	}

	// A column someone showed that starts hidden is only remembered by not being marked hidden.
	[Fact]
	public void TheArrangement_DecidesForEveryColumnItKnows()
	{
		IRenderedComponent<MokaTable<Person>> cut = Table(
			p => p.Add(t => t.State, Arranged(Column("Name"), Column("City"))),
			Name, City with { Visible = false });

		Assert.Equal(["Name", "City"], HeaderTitles(cut));
	}

	[Fact]
	public void AnEmptyArrangement_LeavesTheColumnsAsTheirParametersSay()
	{
		IRenderedComponent<MokaTable<Person>> cut = Table(
			p => p.Add(t => t.State, Arranged()),
			Name, City with { Visible = false });

		Assert.Equal(["Name"], HeaderTitles(cut));
	}

	[Fact]
	public void ANewArrangement_ReplacesTheOldOne()
	{
		IRenderedComponent<MokaTable<Person>> cut = Table(p => p.Add(t => t.State, Arranged(Column("Team", hidden: true))));

		cut.Render(p => p.Add(t => t.State, Arranged(Column("City"), Column("Team"), Column("Name"), Column("Age"))));

		Assert.Equal(["City", "Team", "Name", "Age"], HeaderTitles(cut));
	}

	[Fact]
	public void NoState_LeavesAnArrangementAlone()
	{
		IRenderedComponent<MokaTable<Person>> cut = Table(p => p.Add(t => t.State, Arranged(
			Column("Name"), Column("Team", hidden: true), Column("Age"), Column("City"))));

		cut.Render(p => p.Add(t => t.State, (MokaTableState?)null));

		Assert.Equal(["Name", "Age", "City"], HeaderTitles(cut));
	}

	[Fact]
	public async Task HidingAColumn_RaisesTheArrangement()
	{
		MokaTableState? raised = null;
		IRenderedComponent<MokaTable<Person>> cut = Table(p => p
			.Add(t => t.ShowColumnChooser, true)
			.Add(t => t.StateChanged, s => raised = s));
		await OpenChooser(cut);

		await ChooserBox(cut, "Team").ChangeAsync(new ChangeEventArgs { Value = false });

		Assert.Equal(["Name", "Age", "City"], HeaderTitles(cut));
		Assert.Equal(["Team"], Hidden(raised!));
		Assert.Equal(["Name", "Team", "Age", "City"], Order(raised!));
	}

	[Fact]
	public async Task DraggingAHeader_RaisesTheArrangement()
	{
		MokaTableState? raised = null;
		IRenderedComponent<MokaTable<Person>> cut = Table(p => p
			.Add(t => t.ColumnReorderable, true)
			.Add(t => t.StateChanged, s => raised = s));

		await DragHeaderAsync(cut, "Name", "Age");

		Assert.Equal(["Team", "Age", "Name", "City"], Order(raised!));
	}

	[Fact]
	public async Task ResizingAColumn_RaisesItsWidth_ByTitle()
	{
		MokaTableState? raised = null;
		IRenderedComponent<MokaTable<Person>> cut = Table(p => p.Add(t => t.StateChanged, s => raised = s));

		await cut.InvokeAsync(() => cut.Instance.OnColumnResized(1, 216.4));

		cut.WaitForAssertion(() => Assert.Equal(216, WidthOf(raised!, "Team")), TimeSpan.FromSeconds(10));
		Assert.Contains("216.4px", HeaderCell(cut, "Team").GetAttribute("style"), StringComparison.Ordinal);
	}

	[Fact]
	public async Task HomeOnAnEdge_GivesTheColumnItsWidthBack()
	{
		MokaTableState? raised = null;
		IRenderedComponent<MokaTable<Person>> cut = Table(p => p
			.Add(t => t.State, Arranged(Column("Name"), Column("Team", width: 300), Column("Age"), Column("City")))
			.Add(t => t.StateChanged, s => raised = s));

		await cut.InvokeAsync(() => cut.Instance.OnColumnWidthReset(1));

		cut.WaitForAssertion(() => Assert.All(raised!.Columns, column => Assert.Null(column.Width)), TimeSpan.FromSeconds(10));
		Assert.False(HeaderCell(cut, "Team").HasAttribute("style"));
	}

	[Fact]
	public async Task AWidthThatWasNeverSet_RaisesNothingOnHome()
	{
		int raised = 0;
		IRenderedComponent<MokaTable<Person>> cut = Table(p => p.Add(t => t.StateChanged, _ => raised++));

		await cut.InvokeAsync(() => cut.Instance.OnColumnWidthReset(1));

		Assert.Equal(0, raised);
	}

	[Fact]
	public void AnEdge_TakesNoKeyboard_InATableThatKeepsNothing()
	{
		IRenderedComponent<MokaTable<Person>> cut = Table();

		IElement edge = cut.Find(".moka-table-resize-handle");

		Assert.False(edge.HasAttribute("tabindex"));
		Assert.False(edge.HasAttribute("role"));
		Assert.False(edge.HasAttribute("aria-label"));
	}

	[Fact]
	public void AnEdge_TakesTheKeyboard_InATableWhoseStateIsHeard()
	{
		IRenderedComponent<MokaTable<Person>> cut = Table(p => p.Add(t => t.StateChanged, _ => { }));

		IElement edge = cut.Find(".moka-table-resize-handle");

		Assert.Equal("0", edge.GetAttribute("tabindex"));
		Assert.Equal("separator", edge.GetAttribute("role"));
		Assert.Equal("vertical", edge.GetAttribute("aria-orientation"));
		Assert.Equal("Width of Name: left and right arrows, Home for its own", edge.GetAttribute("aria-label"));
	}

	[Fact]
	public async Task TheStateTheTableRaised_HandedBack_ChangesNothing()
	{
		IRenderedComponent<StateHost> host = Render<StateHost>();
		IRenderedComponent<MokaTable<Person>> cut = host.FindComponent<MokaTable<Person>>();

		await host.InvokeAsync(() => cut.Instance.OnColumnResized(0, 200));

		host.WaitForAssertion(() => Assert.Equal(200, WidthOf(host.Instance.State!, "Name")), TimeSpan.FromSeconds(10));
		Assert.Equal(1, host.Instance.Raised);
		Assert.Contains("width: 200px", HeaderCell(cut, "Name").GetAttribute("style"), StringComparison.Ordinal);
	}

	[Fact]
	public void AStateWithTheSameContent_IsNotNewsToTheTable()
	{
		MokaTableState first = Arranged(Column("City"), Column("Name"), Column("Team"), Column("Age"));
		IRenderedComponent<CountingTable> cut = Render<CountingTable>(p => p
			.Add(t => t.Items, People)
			.Add(t => t.ChildContent, DefaultColumns)
			.Add(t => t.State, first));
		int renders = cut.Instance.Renders;

		cut.Render(p => p.Add(t => t.State, first with { Columns = [.. first.Columns] }));

		Assert.Equal(["City", "Name", "Team", "Age"], cut.FindAll("thead tr:first-child th .moka-table-header-text").Select(e => e.TextContent.Trim()));
		Assert.Equal(renders + 1, cut.Instance.Renders);
	}

	// A column inside an @if can be away when the person saves.
	[Fact]
	public async Task AColumnAwayFromTheTable_KeepsWhatTheStateSaidOfIt()
	{
		MokaTableState? raised = null;
		IRenderedComponent<MokaTable<Person>> cut = Table(p => p
			.Add(t => t.ShowColumnChooser, true)
			.Add(t => t.State, Arranged(Column("Away", hidden: true, width: 90)))
			.Add(t => t.StateChanged, s => raised = s));
		await OpenChooser(cut);

		await ChooserBox(cut, "Team").ChangeAsync(new ChangeEventArgs { Value = false });

		MokaTableColumnState away = Assert.Single(raised!.Columns, column => column.Title == "Away");
		Assert.True(away.Hidden);
		Assert.Equal(90d, away.Width);
	}

	[Fact]
	public void AColumnThatComesLater_TakesItsPlaceFromTheState()
	{
		bool showCity = false;
		RenderFragment columns = builder =>
		{
			Name.Write(builder);
			Team.Write(builder);
			if (showCity)
			{
				builder.OpenRegion(100);
				City.Write(builder);
				builder.CloseRegion();
			}
		};
		IRenderedComponent<MokaTable<Person>> cut = Render<MokaTable<Person>>(p => p
			.Add(t => t.Items, People)
			.Add(t => t.State, Arranged(Column("City"), Column("Team"), Column("Name")))
			.Add(t => t.ChildContent, columns));
		Assert.Equal(["Team", "Name"], HeaderTitles(cut));

		showCity = true;
		cut.Render(p => p.Add(t => t.ChildContent, columns));

		Assert.Equal(["City", "Team", "Name"], HeaderTitles(cut));
	}

	[Fact]
	public void TheChooser_IsOffByDefault()
	{
		IRenderedComponent<MokaTable<Person>> cut = Table();

		Assert.Empty(cut.FindAll(".moka-table-chooser-toggle"));
		Assert.Empty(cut.FindAll(".moka-table-chooser"));
		Assert.Empty(cut.FindAll(".moka-table-toolbar"));
	}

	[Fact]
	public async Task TheColumnsButton_OpensAndClosesThePanel()
	{
		IRenderedComponent<MokaTable<Person>> cut = Table(p => p.Add(t => t.ShowColumnChooser, true));
		IElement toggle = cut.Find(".moka-table-chooser-toggle");
		Assert.Equal("false", toggle.GetAttribute("aria-expanded"));
		Assert.False(toggle.HasAttribute("aria-controls"));
		Assert.Empty(cut.FindAll(".moka-table-chooser"));

		await OpenChooser(cut);

		toggle = cut.Find(".moka-table-chooser-toggle");
		Assert.Equal("true", toggle.GetAttribute("aria-expanded"));
		Assert.Equal(cut.Find(".moka-table-chooser").Id, toggle.GetAttribute("aria-controls"));

		await toggle.ClickAsync(new MouseEventArgs());

		Assert.Empty(cut.FindAll(".moka-table-chooser"));
	}

	[Fact]
	public async Task OpeningThePanel_MovesFocusIn_AndClosingItBringsFocusBack()
	{
		BunitJSModuleInterop module = JSInterop.SetupModule(TableModule);
		module.Mode = JSRuntimeMode.Loose;
		IRenderedComponent<MokaTable<Person>> cut = Table(p => p.Add(t => t.ShowColumnChooser, true));

		await OpenChooser(cut);

		Assert.Equal([".moka-table-chooser input"], (string[])module.Invocations["focusFirst"][0].Arguments[1]!);

		await cut.Find(".moka-table-chooser").KeyDownAsync(new KeyboardEventArgs { Key = "Escape" });

		Assert.Empty(cut.FindAll(".moka-table-chooser"));
		Assert.Equal([".moka-table-chooser-toggle"], (string[])module.Invocations["focusFirst"][1].Arguments[1]!);
	}

	[Fact]
	public async Task DoneClosesThePanel()
	{
		IRenderedComponent<MokaTable<Person>> cut = Table(p => p.Add(t => t.ShowColumnChooser, true));
		await OpenChooser(cut);

		await ChooserFootButton(cut, "Done").ClickAsync(new MouseEventArgs());

		Assert.Empty(cut.FindAll(".moka-table-chooser"));
	}

	[Fact]
	public async Task TheColumnsButton_CountsWhatIsHidden()
	{
		IRenderedComponent<MokaTable<Person>> cut = Table(p => p.Add(t => t.ShowColumnChooser, true));
		Assert.Equal("Columns", cut.Find(".moka-table-chooser-toggle").TextContent.Trim());
		await OpenChooser(cut);

		await ChooserBox(cut, "Team").ChangeAsync(new ChangeEventArgs { Value = false });

		Assert.Equal("Columns (1 hidden)", cut.Find(".moka-table-chooser-toggle").TextContent.Trim());
	}

	[Fact]
	public async Task TheMoveButtons_MoveTheColumnAndRaiseTheArrangement()
	{
		MokaTableState? raised = null;
		List<(int OldIndex, int NewIndex)> moves = [];
		IRenderedComponent<MokaTable<Person>> cut = Table(p => p
			.Add(t => t.ShowColumnChooser, true)
			.Add(t => t.OnColumnReordered, m => moves.Add(m))
			.Add(t => t.StateChanged, s => raised = s));
		await OpenChooser(cut);

		await cut.Find("button[aria-label='Move Team earlier']").ClickAsync(new MouseEventArgs());

		Assert.Equal(["Team", "Name", "Age", "City"], HeaderTitles(cut));
		Assert.Equal(["Team", "Name", "Age", "City"], Order(raised!));
		Assert.Equal([(1, 0)], moves);

		await cut.Find("button[aria-label='Move Team later']").ClickAsync(new MouseEventArgs());

		Assert.Equal(["Name", "Team", "Age", "City"], HeaderTitles(cut));
	}

	// Blazor moves a keyed row by taking it out and putting it back, which drops focus.
	[Fact]
	public async Task TheMoveButtons_KeepFocusOnTheColumn()
	{
		BunitJSModuleInterop module = JSInterop.SetupModule(TableModule);
		module.Mode = JSRuntimeMode.Loose;
		IRenderedComponent<MokaTable<Person>> cut = Table(p => p.Add(t => t.ShowColumnChooser, true));
		await OpenChooser(cut);

		await cut.Find("button[aria-label='Move Team earlier']").ClickAsync(new MouseEventArgs());

		Assert.Equal(
			["button[aria-label=\"Move Team earlier\"]:not([disabled])", "button[aria-label=\"Move Team later\"]:not([disabled])"],
			(string[])module.Invocations["focusFirst"][^1].Arguments[1]!);
	}

	[Fact]
	public async Task AQuoteInATitle_CanNotBreakTheFocusSelector()
	{
		BunitJSModuleInterop module = JSInterop.SetupModule(TableModule);
		module.Mode = JSRuntimeMode.Loose;
		IRenderedComponent<MokaTable<Person>> cut = Table(p => p.Add(t => t.ShowColumnChooser, true), Name, new Col("Say \"hi\"", static x => x.City));
		await OpenChooser(cut);

		await cut.Find("button[aria-label='Move Say \"hi\" earlier']").ClickAsync(new MouseEventArgs());

		Assert.Contains("Move Say \\\"hi\\\" earlier", ((string[])module.Invocations["focusFirst"][^1].Arguments[1]!)[0], StringComparison.Ordinal);
	}

	[Fact]
	public async Task TheMoveButtons_StopAtTheEnds()
	{
		IRenderedComponent<MokaTable<Person>> cut = Table(p => p.Add(t => t.ShowColumnChooser, true));
		await OpenChooser(cut);

		Assert.True(cut.Find("button[aria-label='Move Name earlier']").HasAttribute("disabled"));
		Assert.True(cut.Find("button[aria-label='Move City later']").HasAttribute("disabled"));
		Assert.False(cut.Find("button[aria-label='Move Name later']").HasAttribute("disabled"));
	}

	[Fact]
	public async Task TheMoveButtons_AreNamedByATooltip()
	{
		IRenderedComponent<MokaTable<Person>> cut = Table(p => p.Add(t => t.ShowColumnChooser, true));
		await OpenChooser(cut);

		IconTooltip.AssertNamed(cut.Find("button[aria-label='Move Team earlier']"), "Move Team earlier", MokaTooltipPosition.Top);
	}

	[Fact]
	public async Task PuttingTheColumnsBack_RaisesAnEmptyArrangement()
	{
		MokaTableState? raised = null;
		IRenderedComponent<MokaTable<Person>> cut = Table(
			p => p
				.Add(t => t.ShowColumnChooser, true)
				.Add(t => t.StateChanged, s => raised = s)
				.Add(t => t.State, Arranged(
					Column("City", width: 300), Column("Age"), Column("Team", hidden: true), Column("Name"))));
		await OpenChooser(cut);

		await ChooserFootButton(cut, "Put the columns back as they were").ClickAsync(new MouseEventArgs());

		Assert.Equal(["Name", "Team", "Age", "City"], HeaderTitles(cut));
		Assert.False(HeaderCell(cut, "City").HasAttribute("style"));
		Assert.Empty(raised!.Columns);
	}

	[Fact]
	public async Task PuttingTheColumnsBack_LeavesFocusInThePanel()
	{
		BunitJSModuleInterop module = JSInterop.SetupModule(TableModule);
		module.Mode = JSRuntimeMode.Loose;
		IRenderedComponent<MokaTable<Person>> cut = Table(p => p
			.Add(t => t.ShowColumnChooser, true)
			.Add(t => t.State, Arranged(Column("Name"), Column("Team", hidden: true), Column("Age"), Column("City"))));
		await OpenChooser(cut);

		await ChooserFootButton(cut, "Put the columns back as they were").ClickAsync(new MouseEventArgs());

		Assert.Equal([".moka-table-chooser input"], (string[])module.Invocations["focusFirst"][^1].Arguments[1]!);
	}

	[Fact]
	public async Task ThereIsNothingToPutBack_OnAFreshTable()
	{
		IRenderedComponent<MokaTable<Person>> cut = Table(p => p.Add(t => t.ShowColumnChooser, true));
		await OpenChooser(cut);

		Assert.True(ChooserFootButton(cut, "Put the columns back as they were").HasAttribute("disabled"));
	}

	[Fact]
	public async Task ADraggedWidth_ShowsInThePanelAndCanBeReset()
	{
		MokaTableState? raised = null;
		IRenderedComponent<MokaTable<Person>> cut = Table(p => p
			.Add(t => t.ShowColumnChooser, true)
			.Add(t => t.State, Arranged(Column("Name"), Column("Team", width: 300), Column("Age"), Column("City")))
			.Add(t => t.StateChanged, s => raised = s));
		await OpenChooser(cut);

		IElement reset = cut.FindAll(".moka-table-chooser-item button").First(b => b.TextContent.Contains("300 px", StringComparison.Ordinal));
		await reset.ClickAsync(new MouseEventArgs());

		Assert.All(raised!.Columns, column => Assert.Null(column.Width));
		Assert.False(HeaderCell(cut, "Team").HasAttribute("style"));
	}

	[Fact]
	public async Task AFixedColumn_IsNotInThePanel()
	{
		IRenderedComponent<MokaTable<Person>> cut = Table(
			p => p.Add(t => t.ShowColumnChooser, true),
			Name with { Fixed = true }, Team, Age);
		await OpenChooser(cut);

		Assert.Equal(["Team", "Age"], cut.FindAll(".moka-table-chooser-show span").Select(s => s.TextContent.Trim()));
	}

	[Fact]
	public async Task AFixedColumn_IsNotInTheOldToggle()
	{
		IRenderedComponent<MokaTable<Person>> cut = Table(
			p => p.Add(t => t.ShowColumnToggle, true),
			Name with { Fixed = true }, Team);

		await cut.Find(".moka-table-column-toggle-btn").ClickAsync(new MouseEventArgs());

		Assert.Equal(["Team"], cut.FindAll(".moka-table-column-menu-item").Select(i => i.TextContent.Trim()));
	}

	[Fact]
	public void AFixedColumn_CanNotBeDragged()
	{
		IRenderedComponent<MokaTable<Person>> cut = Table(
			p => p.Add(t => t.ColumnReorderable, true),
			Name with { Fixed = true }, Team);

		Assert.False(HeaderCell(cut, "Name").HasAttribute("draggable"));
		Assert.Equal("true", HeaderCell(cut, "Team").GetAttribute("draggable"));
	}

	[Fact]
	public async Task NothingCanBeDroppedOnAFixedColumn()
	{
		IRenderedComponent<MokaTable<Person>> cut = Table(
			p => p.Add(t => t.ColumnReorderable, true),
			Name with { Fixed = true }, Team);

		await HeaderCell(cut, "Team").DragStartAsync(new DragEventArgs());

		await Assert.ThrowsAsync<MissingEventHandlerException>(() => HeaderCell(cut, "Name").DropAsync(new DragEventArgs()));
	}

	[Fact]
	public async Task OtherColumns_MoveAroundAFixedOne()
	{
		IRenderedComponent<MokaTable<Person>> cut = Table(
			p => p.Add(t => t.ColumnReorderable, true),
			Name, Team with { Fixed = true }, Age, City);

		await DragHeaderAsync(cut, "Name", "City");

		Assert.Equal(["Age", "Team", "City", "Name"], HeaderTitles(cut));
	}

	[Fact]
	public void AnArrangement_MovesOtherColumnsAroundAFixedOne_AndCanNotHideIt()
	{
		IRenderedComponent<MokaTable<Person>> cut = Table(
			p => p.Add(t => t.State, Arranged(Column("City"), Column("Age"), Column("Name"), Column("Team", hidden: true))),
			Name, Team with { Fixed = true }, Age, City);

		Assert.Equal(["City", "Team", "Age", "Name"], HeaderTitles(cut));
	}

	[Fact]
	public void AParentsNewVisible_StillWins()
	{
		IRenderedComponent<MokaTable<Person>> cut = Table(p => p.Add(t => t.State, Arranged(Column("City"), Column("Name"))), Name, City);

		cut.Render(p => p.Add(t => t.ChildContent, Columns(Name with { Visible = false }, City)));

		Assert.Equal(["City"], HeaderTitles(cut));
	}

	[Fact]
	public void States_AreEqualByWhatTheySayOfTheColumns()
	{
		MokaTableState one = Arranged(Column("A", width: 100), Column("B"), Column("C", hidden: true));
		MokaTableState same = Arranged(Column("A", width: 100), Column("B"), Column("C", hidden: true));

		Assert.Equal(one, same);
		Assert.Equal(one.GetHashCode(), same.GetHashCode());
		Assert.NotEqual(one, Arranged(Column("B"), Column("A", width: 100), Column("C", hidden: true)));
		Assert.NotEqual(one, Arranged(Column("A", width: 100), Column("B"), Column("C")));
		Assert.NotEqual(one, Arranged(Column("A", width: 101), Column("B"), Column("C", hidden: true)));
	}

	[Fact]
	public void AnArrangement_RoundTripsThroughJson()
	{
		MokaTableState state = Arranged(Column("A", width: 100), Column("B"), Column("C", hidden: true));

		MokaTableState? back = MokaTableState.FromJson(state.ToJson());

		Assert.Equal(state, back);
	}

	private static MokaTableState Arranged(params MokaTableColumnState[] columns) => new() { Columns = columns };

	private static MokaTableColumnState Column(string title, bool hidden = false, double? width = null) =>
		new(title) { Hidden = hidden, Width = width };

	private static List<string> Order(MokaTableState state) => state.Columns.Select(static column => column.Title).ToList();

	private static List<string> Hidden(MokaTableState state) =>
		state.Columns.Where(static column => column.Hidden).Select(static column => column.Title).ToList();

	private static double WidthOf(MokaTableState state, string title) =>
		state.Columns.FirstOrDefault(column => column.Title == title)?.Width ?? double.NaN;

	private IRenderedComponent<MokaTable<Person>> Table(
		Action<ComponentParameterCollectionBuilder<MokaTable<Person>>>? extra = null,
		params Col[] columns) =>
		Render<MokaTable<Person>>(p =>
		{
			p.Add(t => t.Items, People);
			p.Add(t => t.ChildContent, columns.Length > 0 ? Columns(columns) : DefaultColumns);
			extra?.Invoke(p);
		});

	private static async Task OpenChooser(IRenderedComponent<MokaTable<Person>> cut) =>
		await cut.Find(".moka-table-chooser-toggle").ClickAsync(new MouseEventArgs());

	private static IElement ChooserBox(IRenderedComponent<MokaTable<Person>> cut, string title) =>
		cut.Find($".moka-table-chooser input[aria-label='Show {title}']");

	private static IElement ChooserFootButton(IRenderedComponent<MokaTable<Person>> cut, string text) =>
		cut.FindAll(".moka-table-chooser-foot button").First(b => string.Equals(b.TextContent.Trim(), text, StringComparison.Ordinal));

	private static async Task DragHeaderAsync(IRenderedComponent<MokaTable<Person>> cut, string from, string to)
	{
		await HeaderCell(cut, from).DragStartAsync(new DragEventArgs());
		await HeaderCell(cut, to).DropAsync(new DragEventArgs());
	}

	/// <summary>A parent that binds the state, as an app that stores it does.</summary>
	private sealed class StateHost : ComponentBase
	{
		public MokaTableState? State { get; private set; }

		public int Raised { get; private set; }

		/// <inheritdoc />
		protected override void BuildRenderTree(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder builder)
		{
			ArgumentNullException.ThrowIfNull(builder);
			builder.OpenComponent<MokaTable<Person>>(0);
			builder.AddComponentParameter(1, nameof(MokaTable<Person>.Items), People);
			builder.AddComponentParameter(2, nameof(MokaTable<Person>.State), State);
			builder.AddComponentParameter(3, nameof(MokaTable<Person>.StateChanged),
				EventCallback.Factory.Create<MokaTableState>(this, state =>
				{
					State = state;
					Raised++;
				}));
			builder.AddComponentParameter(4, nameof(MokaTable<Person>.ChildContent), DefaultColumns);
			builder.CloseComponent();
		}
	}
}
