using System.Text.Json;
using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Moka.Red.Data.Table;
using Moka.Red.Feedback.Tooltip;
using Moka.Red.Tests.Shared;
using static Moka.Red.Data.Tests.Components.TableKit;

namespace Moka.Red.Data.Tests.Components;

// Column arrangement: the Layout the table raises and takes, the chooser, a heading's edge, and fixed columns.
public class MokaTableLayoutTests : BunitContext
{
	public MokaTableLayoutTests() => JSInterop.Mode = JSRuntimeMode.Loose;

	[Fact]
	public void ALayout_PutsTheColumnsBack()
	{
		IRenderedComponent<MokaTable<Person>> cut = Table(p => p.Add(t => t.Layout, new MokaTableLayout
		{
			Order = ["City", "Name", "Team", "Age"],
			Hidden = ["Team"],
			Widths = new Dictionary<string, double> { ["City"] = 240 }
		}));

		Assert.Equal(["City", "Name", "Age"], HeaderTitles(cut));
		Assert.Contains("width: 240px", HeaderCell(cut, "City").GetAttribute("style"), StringComparison.Ordinal);
	}

	[Fact]
	public void ATitleNoColumnHas_IsIgnored()
	{
		IRenderedComponent<MokaTable<Person>> cut = Table(p => p.Add(t => t.Layout, new MokaTableLayout
		{
			Order = ["Gone", "City"],
			Hidden = ["Also gone"],
			Widths = new Dictionary<string, double> { ["Gone"] = 100 }
		}));

		Assert.Equal(["City", "Name", "Team", "Age"], HeaderTitles(cut));
	}

	[Fact]
	public void AColumnTheLayoutDoesNotList_GoesAfterTheOnesItDoes()
	{
		IRenderedComponent<MokaTable<Person>> cut = Table(p => p.Add(t => t.Layout, new MokaTableLayout { Order = ["Age", "Team"] }));

		Assert.Equal(["Age", "Team", "Name", "City"], HeaderTitles(cut));
	}

	// A column someone showed that starts hidden is only remembered by not being listed.
	[Fact]
	public void TheHiddenList_DecidesForEveryColumnItKnows()
	{
		IRenderedComponent<MokaTable<Person>> cut = Table(
			p => p.Add(t => t.Layout, new MokaTableLayout { Order = ["Name", "City"] }),
			Name, City with { Visible = false });

		Assert.Equal(["Name", "City"], HeaderTitles(cut));
	}

	[Fact]
	public void AnEmptyLayout_LeavesTheColumnsAsTheirParametersSay()
	{
		IRenderedComponent<MokaTable<Person>> cut = Table(
			p => p.Add(t => t.Layout, new MokaTableLayout()),
			Name, City with { Visible = false });

		Assert.Equal(["Name"], HeaderTitles(cut));
	}

	[Fact]
	public void ANewLayout_ReplacesTheOldOne()
	{
		IRenderedComponent<MokaTable<Person>> cut = Table(p => p.Add(t => t.Layout, new MokaTableLayout { Hidden = ["Team"] }));

		cut.Render(p => p.Add(t => t.Layout, new MokaTableLayout { Order = ["City", "Team", "Name", "Age"] }));

		Assert.Equal(["City", "Team", "Name", "Age"], HeaderTitles(cut));
	}

	[Fact]
	public void NoLayout_LeavesAnArrangementAlone()
	{
		IRenderedComponent<MokaTable<Person>> cut = Table(p => p.Add(t => t.Layout, new MokaTableLayout { Hidden = ["Team"] }));

		cut.Render(p => p.Add(t => t.Layout, (MokaTableLayout?)null));

		Assert.Equal(["Name", "Age", "City"], HeaderTitles(cut));
	}

	[Fact]
	public async Task HidingAColumn_RaisesTheLayout()
	{
		MokaTableLayout? raised = null;
		IRenderedComponent<MokaTable<Person>> cut = Table(p => p
			.Add(t => t.ShowColumnChooser, true)
			.Add(t => t.LayoutChanged, l => raised = l));
		await OpenChooser(cut);

		await ChooserBox(cut, "Team").ChangeAsync(new ChangeEventArgs { Value = false });

		Assert.Equal(["Name", "Age", "City"], HeaderTitles(cut));
		Assert.Equal(["Team"], raised!.Hidden);
		Assert.Equal(["Name", "Team", "Age", "City"], raised.Order);
	}

	[Fact]
	public async Task DraggingAHeader_RaisesTheLayout()
	{
		MokaTableLayout? raised = null;
		IRenderedComponent<MokaTable<Person>> cut = Table(p => p
			.Add(t => t.ColumnReorderable, true)
			.Add(t => t.LayoutChanged, l => raised = l));

		await DragHeaderAsync(cut, "Name", "Age");

		Assert.Equal(["Team", "Age", "Name", "City"], raised!.Order);
	}

	[Fact]
	public async Task ResizingAColumn_RaisesItsWidth_ByTitle()
	{
		MokaTableLayout? raised = null;
		IRenderedComponent<MokaTable<Person>> cut = Table(p => p.Add(t => t.LayoutChanged, l => raised = l));

		await cut.InvokeAsync(() => cut.Instance.OnColumnResized(1, 216.4));

		cut.WaitForAssertion(() => Assert.Equal(216, raised!.Widths["Team"]), TimeSpan.FromSeconds(10));
		Assert.Contains("216.4px", HeaderCell(cut, "Team").GetAttribute("style"), StringComparison.Ordinal);
	}

	[Fact]
	public async Task HomeOnAnEdge_GivesTheColumnItsWidthBack()
	{
		MokaTableLayout? raised = null;
		IRenderedComponent<MokaTable<Person>> cut = Table(p => p
			.Add(t => t.Layout, new MokaTableLayout { Widths = new Dictionary<string, double> { ["Team"] = 300 } })
			.Add(t => t.LayoutChanged, l => raised = l));

		await cut.InvokeAsync(() => cut.Instance.OnColumnWidthReset(1));

		cut.WaitForAssertion(() => Assert.Empty(raised!.Widths), TimeSpan.FromSeconds(10));
		Assert.False(HeaderCell(cut, "Team").HasAttribute("style"));
	}

	[Fact]
	public async Task AWidthThatWasNeverSet_RaisesNothingOnHome()
	{
		int raised = 0;
		IRenderedComponent<MokaTable<Person>> cut = Table(p => p.Add(t => t.LayoutChanged, _ => raised++));

		await cut.InvokeAsync(() => cut.Instance.OnColumnWidthReset(1));

		Assert.Equal(0, raised);
	}

	[Fact]
	public void TheLayout_ReachesPlainResizesOfATableThatDoesNotArrange_NotAtAll()
	{
		IRenderedComponent<MokaTable<Person>> cut = Table();

		IElement edge = cut.Find(".moka-table-resize-handle");

		Assert.False(edge.HasAttribute("tabindex"));
		Assert.False(edge.HasAttribute("role"));
		Assert.False(edge.HasAttribute("aria-label"));
	}

	[Fact]
	public void AnEdge_TakesTheKeyboard_InATableThatArranges()
	{
		IRenderedComponent<MokaTable<Person>> cut = Table(p => p.Add(t => t.LayoutChanged, _ => { }));

		IElement edge = cut.Find(".moka-table-resize-handle");

		Assert.Equal("0", edge.GetAttribute("tabindex"));
		Assert.Equal("separator", edge.GetAttribute("role"));
		Assert.Equal("vertical", edge.GetAttribute("aria-orientation"));
		Assert.Equal("Width of Name: left and right arrows, Home for its own", edge.GetAttribute("aria-label"));
	}

	[Fact]
	public async Task TheLayoutTheTableRaised_HandedBack_ChangesNothing()
	{
		IRenderedComponent<LayoutHost> host = Render<LayoutHost>();
		IRenderedComponent<MokaTable<Person>> cut = host.FindComponent<MokaTable<Person>>();

		await host.InvokeAsync(() => cut.Instance.OnColumnResized(0, 200));

		host.WaitForAssertion(() => Assert.Equal(200, host.Instance.Layout!.Widths["Name"]), TimeSpan.FromSeconds(10));
		Assert.Equal(1, host.Instance.Raised);
		Assert.Contains("width: 200px", HeaderCell(cut, "Name").GetAttribute("style"), StringComparison.Ordinal);
	}

	[Fact]
	public void ALayoutWithTheSameContent_IsNotNewsToTheTable()
	{
		MokaTableLayout first = new() { Order = ["City", "Name", "Team", "Age"] };
		IRenderedComponent<MokaTable<Person>> cut = Table(p => p.Add(t => t.Layout, first));
		int edits = cut.RenderCount;

		cut.Render(p => p.Add(t => t.Layout, first with { Order = [.. first.Order] }));

		Assert.Equal(["City", "Name", "Team", "Age"], HeaderTitles(cut));
		Assert.Equal(edits + 1, cut.RenderCount);
	}

	// A column inside an @if can be away when the person saves.
	[Fact]
	public async Task AColumnAwayFromTheTable_KeepsWhatTheLayoutSaidOfIt()
	{
		MokaTableLayout? raised = null;
		IRenderedComponent<MokaTable<Person>> cut = Table(p => p
			.Add(t => t.ShowColumnChooser, true)
			.Add(t => t.Layout, new MokaTableLayout
			{
				Hidden = ["Away"],
				Widths = new Dictionary<string, double> { ["Away"] = 90 }
			})
			.Add(t => t.LayoutChanged, l => raised = l));
		await OpenChooser(cut);

		await ChooserBox(cut, "Team").ChangeAsync(new ChangeEventArgs { Value = false });

		Assert.Contains("Away", raised!.Hidden);
		Assert.Equal(90, raised.Widths["Away"]);
	}

	[Fact]
	public void AColumnThatComesLater_TakesItsPlaceFromTheLayout()
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
			.Add(t => t.Layout, new MokaTableLayout { Order = ["City", "Team", "Name"] })
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
	public async Task TheMoveButtons_MoveTheColumnAndRaiseTheLayout()
	{
		MokaTableLayout? raised = null;
		List<(int OldIndex, int NewIndex)> moves = [];
		IRenderedComponent<MokaTable<Person>> cut = Table(p => p
			.Add(t => t.ShowColumnChooser, true)
			.Add(t => t.OnColumnReordered, m => moves.Add(m))
			.Add(t => t.LayoutChanged, l => raised = l));
		await OpenChooser(cut);

		await cut.Find("button[aria-label='Move Team earlier']").ClickAsync(new MouseEventArgs());

		Assert.Equal(["Team", "Name", "Age", "City"], HeaderTitles(cut));
		Assert.Equal(["Team", "Name", "Age", "City"], raised!.Order);
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
	public async Task PuttingTheColumnsBack_RaisesAnEmptyLayout()
	{
		MokaTableLayout? raised = null;
		IRenderedComponent<MokaTable<Person>> cut = Table(
			p => p
				.Add(t => t.ShowColumnChooser, true)
				.Add(t => t.LayoutChanged, l => raised = l)
				.Add(t => t.Layout, new MokaTableLayout
				{
					Order = ["City", "Age", "Team", "Name"],
					Hidden = ["Team"],
					Widths = new Dictionary<string, double> { ["City"] = 300 }
				}));
		await OpenChooser(cut);

		await ChooserFootButton(cut, "Put the columns back as they were").ClickAsync(new MouseEventArgs());

		Assert.Equal(["Name", "Team", "Age", "City"], HeaderTitles(cut));
		Assert.False(HeaderCell(cut, "City").HasAttribute("style"));
		Assert.Equal(new MokaTableLayout(), raised);
	}

	[Fact]
	public async Task PuttingTheColumnsBack_LeavesFocusInThePanel()
	{
		BunitJSModuleInterop module = JSInterop.SetupModule(TableModule);
		module.Mode = JSRuntimeMode.Loose;
		IRenderedComponent<MokaTable<Person>> cut = Table(p => p
			.Add(t => t.ShowColumnChooser, true)
			.Add(t => t.Layout, new MokaTableLayout { Hidden = ["Team"] }));
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
		MokaTableLayout? raised = null;
		IRenderedComponent<MokaTable<Person>> cut = Table(p => p
			.Add(t => t.ShowColumnChooser, true)
			.Add(t => t.Layout, new MokaTableLayout { Widths = new Dictionary<string, double> { ["Team"] = 300 } })
			.Add(t => t.LayoutChanged, l => raised = l));
		await OpenChooser(cut);

		IElement reset = cut.FindAll(".moka-table-chooser-item button").First(b => b.TextContent.Contains("300 px", StringComparison.Ordinal));
		await reset.ClickAsync(new MouseEventArgs());

		Assert.Empty(raised!.Widths);
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
	public void ALayout_MovesOtherColumnsAroundAFixedOne_AndCanNotHideIt()
	{
		IRenderedComponent<MokaTable<Person>> cut = Table(
			p => p.Add(t => t.Layout, new MokaTableLayout { Order = ["City", "Age", "Name"], Hidden = ["Team"] }),
			Name, Team with { Fixed = true }, Age, City);

		Assert.Equal(["City", "Team", "Age", "Name"], HeaderTitles(cut));
	}

	[Fact]
	public void AParentsNewVisible_StillWins()
	{
		IRenderedComponent<MokaTable<Person>> cut = Table(p => p.Add(t => t.Layout, new MokaTableLayout { Order = ["City", "Name"] }), Name, City);

		cut.Render(p => p.Add(t => t.ChildContent, Columns(Name with { Visible = false }, City)));

		Assert.Equal(["City"], HeaderTitles(cut));
	}

	[Fact]
	public void Layouts_AreEqualByWhatTheySay()
	{
		MokaTableLayout one = new()
		{
			Order = ["A", "B"],
			Hidden = ["C", "D"],
			Widths = new Dictionary<string, double> { ["A"] = 100 }
		};
		MokaTableLayout same = new()
		{
			Order = ["A", "B"],
			Hidden = ["D", "C"],
			Widths = new Dictionary<string, double> { ["A"] = 100 }
		};

		Assert.Equal(one, same);
		Assert.Equal(one.GetHashCode(), same.GetHashCode());
		Assert.NotEqual(one, one with { Order = ["B", "A"] });
		Assert.NotEqual(one, one with { Hidden = ["C"] });
		Assert.NotEqual(one, one with { Widths = new Dictionary<string, double> { ["A"] = 101 } });
	}

	[Fact]
	public void ALayout_RoundTripsThroughJson()
	{
		MokaTableLayout layout = new()
		{
			Order = ["A", "B"],
			Hidden = ["C"],
			Widths = new Dictionary<string, double> { ["A"] = 100 }
		};

		MokaTableLayout? back = JsonSerializer.Deserialize<MokaTableLayout>(JsonSerializer.Serialize(layout));

		Assert.Equal(layout, back);
	}

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

	/// <summary>A parent that binds the layout, as an app that stores it does.</summary>
	private sealed class LayoutHost : ComponentBase
	{
		public MokaTableLayout? Layout { get; private set; }

		public int Raised { get; private set; }

		/// <inheritdoc />
		protected override void BuildRenderTree(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder builder)
		{
			ArgumentNullException.ThrowIfNull(builder);
			builder.OpenComponent<MokaTable<Person>>(0);
			builder.AddComponentParameter(1, nameof(MokaTable<Person>.Items), People);
			builder.AddComponentParameter(2, nameof(MokaTable<Person>.Layout), Layout);
			builder.AddComponentParameter(3, nameof(MokaTable<Person>.LayoutChanged),
				EventCallback.Factory.Create<MokaTableLayout>(this, layout =>
				{
					Layout = layout;
					Raised++;
				}));
			builder.AddComponentParameter(4, nameof(MokaTable<Person>.ChildContent), DefaultColumns);
			builder.CloseComponent();
		}
	}
}
