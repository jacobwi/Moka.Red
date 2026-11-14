using Moka.Red.Core.Enums;
using Moka.Red.Layout.DockLayout;

namespace Moka.Red.Layout.Tests.Components;

// The reducer as plain functions: every operation takes a state and gives a new one, and parts it doesn't touch keep
// their instances, which is what lets the layout redraw only the groups that changed.
public class MokaDockStateTests
{
	// Row[ side(240px: explorer, search) | Column[ content | bottom(180px: terminal, problems) ] ]
	private static MokaDockState Ide() => new()
	{
		Root = new MokaDockSplitNode
		{
			Id = "row",
			Direction = MokaDockSplitDirection.Row,
			Children =
			[
				new MokaDockGroupNode { Id = "side", Size = "240px", Panels = ["explorer", "search"] },
				new MokaDockSplitNode
				{
					Id = "column",
					Direction = MokaDockSplitDirection.Column,
					Children =
					[
						new MokaDockGroupNode { Id = "content", Content = true },
						new MokaDockGroupNode { Id = "bottom", Size = "180px", Panels = ["terminal", "problems"] }
					]
				}
			]
		}
	};

	[Fact]
	public void Json_RoundTripsEveryPart()
	{
		MokaDockState state = Ide()
			.FloatPanel("problems", 10, 20, 300, 200)
			.AutoHide("side", MokaDockPosition.Left)
			.Close("terminal")
			.Maximize("content") with { AppVersion = 3 };

		MokaDockState? restored = MokaDockState.FromJson(state.ToJson());

		Assert.NotNull(restored);
		Assert.Equal(state.ToJson(), restored.ToJson());
		Assert.Equal(3, restored.AppVersion);
		Assert.Equal(MokaDockPlace.Floating, restored.Locate("problems").Place);
		Assert.Equal(MokaDockPlace.AutoHidden, restored.Locate("explorer").Place);
		Assert.Equal(MokaDockPlace.Hidden, restored.Locate("terminal").Place);
	}

	[Fact]
	public void Json_WritesVersionAndKinds()
	{
		string json = Ide().ToJson();

		Assert.Contains("\"version\":1", json, StringComparison.Ordinal);
		Assert.Contains("\"kind\":\"split\"", json, StringComparison.Ordinal);
		Assert.Contains("\"direction\":\"Column\"", json, StringComparison.Ordinal);
	}

	[Theory]
	[InlineData(null)]
	[InlineData("")]
	[InlineData("not json")]
	[InlineData("[1, 2]")]
	[InlineData("{\"version\": 99, \"root\": null}")]
	[InlineData("{\"root\": null}")]
	public void FromJson_RefusesWhatItCannotRead(string? json) => Assert.Null(MokaDockState.FromJson(json));

	[Fact]
	public void Move_IntoAnotherGroup_AddsATabAtTheIndex()
	{
		MokaDockState state = Ide().Move("terminal", new MokaDockTarget("side", MokaDockZone.Center, 1));

		MokaDockGroupNode side = Assert.IsType<MokaDockGroupNode>(state.FindGroup("side"));
		Assert.Equal(["explorer", "terminal", "search"], side.Panels);
		Assert.Equal("terminal", side.Shown);
		Assert.Equal("side", state.ActiveGroup);
		Assert.Equal(["problems"], state.FindGroup("bottom")!.Panels);
	}

	[Fact]
	public void Move_ToAGroupsEdge_SplitsAcrossTheParent()
	{
		MokaDockState state = Ide().Move("search", new MokaDockTarget("bottom", MokaDockZone.Right));

		MokaDockSplitNode column = Assert.IsType<MokaDockSplitNode>(state.FindNode("column"));
		MokaDockSplitNode row = Assert.IsType<MokaDockSplitNode>(column.Children[1]);
		Assert.Equal(MokaDockSplitDirection.Row, row.Direction);
		Assert.Equal("180px", row.Size);
		Assert.Equal("bottom", row.Children[0].Id);
		MokaDockGroupNode added = Assert.IsType<MokaDockGroupNode>(row.Children[1]);
		Assert.Equal(["search"], added.Panels);
	}

	[Fact]
	public void Move_AlongTheParentsDirection_AddsASibling()
	{
		MokaDockState state = Ide().Move("terminal", new MokaDockTarget("content", MokaDockZone.Top));

		MokaDockSplitNode column = Assert.IsType<MokaDockSplitNode>(state.FindNode("column"));
		Assert.Equal(3, column.Children.Count);
		Assert.Equal(["terminal"], Assert.IsType<MokaDockGroupNode>(column.Children[0]).Panels);
		Assert.Equal("content", column.Children[1].Id);
	}

	[Fact]
	public void Move_TheLastPanelOut_RemovesTheGroupAndTidiesTheSplit()
	{
		MokaDockState state = Ide()
			.Move("terminal", new MokaDockTarget("side", MokaDockZone.Center))
			.Move("problems", new MokaDockTarget("side", MokaDockZone.Center));

		Assert.Null(state.FindGroup("bottom"));
		MokaDockSplitNode row = Assert.IsType<MokaDockSplitNode>(state.Root);
		Assert.Equal("content", row.Children[1].Id);
		Assert.Null(state.FindNode("column"));
	}

	[Fact]
	public void Move_ToTheLayoutsEdge_WrapsTheRoot()
	{
		MokaDockState state = Ide().Move("problems", new MokaDockTarget(null, MokaDockZone.Top));

		MokaDockSplitNode root = Assert.IsType<MokaDockSplitNode>(state.Root);
		Assert.Equal(MokaDockSplitDirection.Column, root.Direction);
		Assert.Equal(["problems"], Assert.IsType<MokaDockGroupNode>(root.Children[0]).Panels);
		Assert.Equal("25%", root.Children[0].Size);
		Assert.Equal("row", root.Children[1].Id);
	}

	[Fact]
	public void Move_OntoItsOwnGroupsEdge_WhenAlone_ChangesNothing()
	{
		MokaDockState state = Ide().Move("terminal", new MokaDockTarget("side", MokaDockZone.Center));

		Assert.Same(state, state.Move("problems", new MokaDockTarget("bottom", MokaDockZone.Left)));
	}

	[Fact]
	public void MoveGroup_MovesEveryTabInOrder()
	{
		MokaDockState state = Ide().MoveGroup("bottom", new MokaDockTarget("side", MokaDockZone.Center));

		Assert.Equal(["explorer", "search", "terminal", "problems"], state.FindGroup("side")!.Panels);
	}

	[Fact]
	public void Reorder_MovesATabWithinItsGroup()
	{
		MokaDockState state = Ide().Reorder("search", 0);

		Assert.Equal(["search", "explorer"], state.FindGroup("side")!.Panels);
	}

	// A tab dropped on its own strip lands at the insertion point the pointer shows, counted before it leaves.
	[Theory]
	[InlineData(2, new[] { "search", "explorer" })]
	[InlineData(1, new[] { "explorer", "search" })]
	[InlineData(0, new[] { "explorer", "search" })]
	public void Move_WithinItsOwnStrip_Reorders(int index, string[] order)
	{
		MokaDockState state = Ide().Move("explorer", new MokaDockTarget("side", MokaDockZone.Center, index));

		Assert.Equal(order, state.FindGroup("side")!.Panels);
	}

	[Fact]
	public void Close_AndShow_BringThePanelBackWhereItWas()
	{
		MokaDockState closed = Ide().Close("explorer");
		Assert.Equal(MokaDockPlace.Hidden, closed.Locate("explorer").Place);
		Assert.Equal(["search"], closed.FindGroup("side")!.Panels);

		MokaDockState shown = closed.Show("explorer");

		Assert.Equal(["explorer", "search"], shown.FindGroup("side")!.Panels);
		Assert.Empty(shown.Hidden);
	}

	[Fact]
	public void Close_TheLastPanel_ShowsItAgainAtItsGroupsSize()
	{
		MokaDockState state = Ide()
			.Move("problems", new MokaDockTarget("side", MokaDockZone.Center))
			.Close("terminal");
		Assert.Null(state.FindGroup("bottom"));

		MokaDockState shown = state.Show("terminal");

		string group = shown.Locate("terminal").Group!;
		(MokaDockSplitNode column, int index) = MokaDockStateTestsHelp.ParentOf(shown, group);
		Assert.Equal(MokaDockSplitDirection.Column, column.Direction);
		Assert.Equal("180px", column.Children[index].Size);
		Assert.Equal(["terminal"], Assert.IsType<MokaDockGroupNode>(column.Children[index]).Panels);
	}

	[Fact]
	public void Close_WithoutRemembering_ForgetsThePanel()
	{
		MokaDockState state = Ide().Close("terminal", remember: false);

		Assert.Equal(MokaDockPlace.None, state.Locate("terminal").Place);
		Assert.Empty(state.Hidden);
	}

	[Fact]
	public void FloatPanel_TheLastOfItsGroup_TakesTheGroupAlong_AndDocksBackAtItsSize()
	{
		MokaDockState single = Ide().Move("search", new MokaDockTarget("bottom", MokaDockZone.Center));
		MokaDockState floating = single.FloatPanel("explorer", 40, 50, 320, 240);

		MokaDockFloatingWindow window = Assert.Single(floating.Floating);
		Assert.Equal("side", window.Group.Id);
		Assert.Equal((40d, 50d, 320d, 240d), (window.X, window.Y, window.Width, window.Height));
		Assert.Null(floating.FindNode("side"));

		MokaDockState docked = floating.Dock("side");

		Assert.Empty(docked.Floating);
		MokaDockSplitNode row = Assert.IsType<MokaDockSplitNode>(docked.Root);
		Assert.Equal("side", row.Children[0].Id);
		Assert.Equal("240px", row.Children[0].Size);
	}

	[Fact]
	public void FloatPanel_FromAGroupWithOthers_FloatsItAlone()
	{
		MokaDockState state = Ide().FloatPanel("search", 0, 0, 300, 200);

		Assert.Equal(["explorer"], state.FindGroup("side")!.Panels);
		Assert.Equal(["search"], Assert.Single(state.Floating).Group.Panels);
	}

	[Fact]
	public void MoveFloating_BringToFront_AndMaximize_ChangeOnlyTheirWindow()
	{
		MokaDockState state = Ide().FloatPanel("search", 0, 0, 300, 200).FloatPanel("problems", 10, 10, 300, 200);
		string first = state.Floating[0].Group.Id;

		MokaDockState front = state.BringToFront(first);
		Assert.Equal(first, front.Floating[^1].Group.Id);
		Assert.Same(state.Floating[1], front.Floating[0]);

		MokaDockState moved = front.MoveFloating(first, 5, 6, 7, 8);
		Assert.Equal((5d, 6d, 7d, 8d), (moved.Floating[^1].X, moved.Floating[^1].Y, moved.Floating[^1].Width, moved.Floating[^1].Height));
		Assert.True(moved.SetFloatingMaximized(first, true).Floating[^1].Maximized);
	}

	[Fact]
	public void AutoHide_PinsTheGroup_AndDockPutsItBack()
	{
		MokaDockState pinned = Ide().AutoHide("bottom", MokaDockPosition.Bottom);

		MokaDockAutoHidden entry = Assert.Single(pinned.AutoHidden);
		Assert.Equal(MokaDockPosition.Bottom, entry.Side);
		Assert.Equal(["terminal", "problems"], entry.Group.Panels);
		Assert.Null(pinned.FindNode("bottom"));

		MokaDockState back = pinned.Dock("bottom");

		Assert.Empty(back.AutoHidden);
		(MokaDockSplitNode column, int index) = MokaDockStateTestsHelp.ParentOf(back, "bottom");
		Assert.Equal(MokaDockSplitDirection.Column, column.Direction);
		Assert.Equal("content", column.Children[index - 1].Id);
		Assert.Equal("180px", column.Children[index].Size);
	}

	[Fact]
	public void AutoHide_LeavesTheMainAreaWhereItIs()
	{
		MokaDockState state = Ide();

		Assert.Same(state, state.AutoHide("content", MokaDockPosition.Left));
	}

	[Fact]
	public void Resize_SetsTheChildrensSizes()
	{
		MokaDockState state = Ide().Resize("row", ["300px", null]);

		Assert.Equal("300px", Assert.IsType<MokaDockSplitNode>(state.Root).Children[0].Size);
	}

	[Fact]
	public void Maximize_IsDroppedWhenItsGroupGoes()
	{
		MokaDockState state = Ide().Maximize("bottom");
		Assert.Equal("bottom", state.Maximized);

		MokaDockState after = state.AutoHide("bottom", MokaDockPosition.Bottom);

		Assert.Null(after.Maximized);
	}

	[Fact]
	public void TheMainArea_StaysWhenItsLastPanelLeaves()
	{
		MokaDockState state = Ide()
			.Move("terminal", new MokaDockTarget("content", MokaDockZone.Center))
			.Move("terminal", new MokaDockTarget("side", MokaDockZone.Center));

		MokaDockGroupNode content = Assert.IsType<MokaDockGroupNode>(state.FindGroup("content"));
		Assert.True(content.Content);
		Assert.Empty(content.Panels);
	}

	[Fact]
	public void Add_PutsANewPanelInTheMainArea()
	{
		MokaDockState state = Ide().Add("doc", new MokaDockTarget("content", MokaDockZone.Center));

		Assert.Equal(["doc"], state.FindGroup("content")!.Panels);
		Assert.Equal("content", state.ActiveGroup);
	}

	[Fact]
	public void Activate_InOneGroup_KeepsTheOtherGroupsInstances()
	{
		MokaDockState state = Ide();
		MokaDockGroupNode side = state.FindGroup("side")!;

		MokaDockState next = state.Activate("problems");

		Assert.Same(side, next.FindGroup("side"));
		Assert.Equal("problems", next.FindGroup("bottom")!.Active);
		Assert.Same(next, next.Activate("problems"));
	}

	[Fact]
	public void Reconcile_DropsUnknownPanels_AndAddsNewOnesWhereTheMarkupHasThem()
	{
		MokaDockState saved = Ide().Close("terminal", remember: false).Move("search", new MokaDockTarget("bottom", MokaDockZone.Center));
		MokaDockState defaults = Ide();

		MokaDockState fitted = saved.Reconcile(defaults, ["explorer", "search", "terminal"]);

		Assert.Equal(MokaDockPlace.None, fitted.Locate("problems").Place);
		Assert.Equal("bottom", fitted.Locate("terminal").Group);
		Assert.Equal("bottom", fitted.Locate("search").Group);
	}

	// A preset knows nothing of the documents opened from code or the panels another window shows: fitting it to the
	// current state keeps them, so applying it neither drops a document nor draws a panel its window still shows.
	[Fact]
	public void Reconcile_FromTheCurrentState_KeepsWhatIsOpen()
	{
		MokaDockState current = Ide()
			.Add("doc", new MokaDockTarget("content", MokaDockZone.Center))
			.Add("notes", new MokaDockTarget("content", MokaDockZone.Center))
			.Activate("doc")
			.Add("draft", new MokaDockTarget("content", MokaDockZone.Center))
			.FloatPanel("draft", 40, 40, 300, 200)
			.PopOut("problems");
		MokaDockState preset = Ide().MoveGroup("bottom", new MokaDockTarget(null, MokaDockZone.Right)).PopOut("search");
		string[] panels = ["explorer", "search", "terminal", "problems", "doc", "notes", "draft"];

		MokaDockState fitted = preset.Reconcile(Ide(), panels, current);

		Assert.Equal(["doc", "notes", "draft"], fitted.FindGroup("content")!.Panels);
		Assert.Equal("doc", fitted.FindGroup("content")!.Shown);
		Assert.Equal(MokaDockPlace.PoppedOut, fitted.Locate("problems").Place);
		Assert.Equal(new MokaDockTarget("bottom", MokaDockZone.Center, 1), fitted.PoppedOut.Single().Return);
		Assert.Equal("side", fitted.Locate("search").Group);
		Assert.Equal(preset.ActiveGroup, fitted.ActiveGroup);
		Assert.Equal(MokaDockPlace.None, preset.Reconcile(Ide(), panels).Locate("doc").Place);
	}

	[Fact]
	public void Normalize_PutsASplitsOnlyChildInItsPlace()
	{
		MokaDockState state = new()
		{
			Root = new MokaDockSplitNode
			{
				Id = "outer",
				Direction = MokaDockSplitDirection.Row,
				Children = [new MokaDockGroupNode { Id = "a", Panels = ["one"] }, new MokaDockGroupNode { Id = "empty" }]
			}
		};

		MokaDockState tidy = state.Normalize();

		Assert.Equal("a", tidy.Root!.Id);
	}
}

internal static class MokaDockStateTestsHelp
{
	public static (MokaDockSplitNode Parent, int Index) ParentOf(MokaDockState state, string node) => Find(state.Root!, node)!.Value;

	private static (MokaDockSplitNode, int)? Find(MokaDockNode node, string id)
	{
		if (node is not MokaDockSplitNode split)
		{
			return null;
		}

		for (int i = 0; i < split.Children.Count; i++)
		{
			if (split.Children[i].Id == id)
			{
				return (split, i);
			}

			if (Find(split.Children[i], id) is { } found)
			{
				return found;
			}
		}

		return null;
	}
}
