using System.Text.Json;
using Moka.Red.Tests.Shared;

namespace Moka.Red.Layout.Tests.Components;

// Where MokaBentoGrid.razor.js shows the drop bar and what it tells .NET, run in Node.js over a small stand-in for the
// DOM (Script/bento-board.mjs). On a grid the bar goes by the card's width. In rows that flow every card grows to fill its
// row, so the bar goes by where the dragged card will land. Skipped where Node.js isn't installed.
public sealed class MokaBentoBoardScriptTests
{
	private static readonly Lazy<JsonElement?> Result = new(() => ScriptRunner.Run(
		["tests", "Moka.Red.Layout.Tests", "Components", "Script", "bento-board.mjs"],
		["src", "Moka.Red.Layout", "BentoGrid", "MokaBentoGrid.razor.js"]));

	[Fact]
	public void OnAGrid_ASmallCardTakesThePlaceBesideIt()
	{
		JsonElement drop = Results().GetProperty("grid").GetProperty("besideSmall");

		AssertVertical(drop, 245);
		AssertDropped(drop, "x", "b", true);
	}

	[Fact]
	public void OnAGrid_AFullCardTakesThePlaceAboveIt()
	{
		JsonElement drop = Results().GetProperty("grid").GetProperty("aboveFull");

		AssertHorizontal(drop, 110, 0, 1000);
		AssertDropped(drop, "x", "f", true);
	}

	// A wide card that grew past most of its row still has a card beside it, and the dragged one joins that row.
	[Fact]
	public void InRows_ACardWithOthersInItsRow_TakesThePlaceBesideIt_HoweverWideItGrew()
	{
		JsonElement rows = Results().GetProperty("rows");

		AssertVertical(rows.GetProperty("afterGrownWide"), 663);
		AssertDropped(rows.GetProperty("afterGrownWide"), "x", "a", false);
		AssertVertical(rows.GetProperty("beforeSmall"), 663);
		AssertDropped(rows.GetProperty("beforeSmall"), "x", "b", true);
	}

	// A small card alone in its row spans it, but the dragged card fits beside it, so that is where the bar shows.
	[Fact]
	public void InRows_ACardAloneInItsRow_GoesByTheHalfThePointerIsIn_AndShowsTheDropBesideIt()
	{
		JsonElement rows = Results().GetProperty("rows");

		AssertVertical(rows.GetProperty("afterAlone"), 1010);
		AssertDropped(rows.GetProperty("afterAlone"), "x", "l", false);
		AssertVertical(rows.GetProperty("beforeAlone"), -10);
		AssertDropped(rows.GetProperty("beforeAlone"), "x", "l", true);
	}

	[Fact]
	public void InRows_ACardThatWontJoinTheRow_ShowsTheBarBetweenRows()
	{
		JsonElement rows = Results().GetProperty("rows");

		AssertHorizontal(rows.GetProperty("afterFull"), 230, 0, 1000);
		AssertDropped(rows.GetProperty("afterFull"), "x", "f", false);
		AssertHorizontal(rows.GetProperty("wideAfterFullRow"), 110, 673, 327);
		AssertDropped(rows.GetProperty("wideAfterFullRow"), "w", "b", false);
	}

	[Fact]
	public void Escape_DropsNothing_AndTakesTheCopyAndTheBarAway()
	{
		JsonElement cancelled = Results().GetProperty("rows").GetProperty("cancelled");

		Assert.Empty(cancelled.GetProperty("dropped").EnumerateArray());
		Assert.Equal(0, cancelled.GetProperty("leftOver").GetInt32());
	}

	private static void AssertVertical(JsonElement drop, double x)
	{
		Assert.Equal("vertical", drop.GetProperty("bar").GetString());
		Assert.Equal(x, drop.GetProperty("x").GetDouble());
	}

	private static void AssertHorizontal(JsonElement drop, double y, double left, double width)
	{
		Assert.Equal("horizontal", drop.GetProperty("bar").GetString());
		Assert.Equal(y, drop.GetProperty("y").GetDouble());
		Assert.Equal(left, drop.GetProperty("left").GetDouble());
		Assert.Equal(width, drop.GetProperty("width").GetDouble());
	}

	private static void AssertDropped(JsonElement drop, string id, string target, bool before)
	{
		JsonElement call = Assert.Single(drop.GetProperty("dropped").EnumerateArray());
		Assert.Equal(["Dropped", id, target], call.EnumerateArray().Take(3).Select(e => e.GetString()));
		Assert.Equal(before, call[3].GetBoolean());
		Assert.Equal(0, drop.GetProperty("leftOver").GetInt32());
	}

	private static JsonElement Results()
	{
		Assert.SkipUnless(Result.Value is not null, "Node.js isn't installed, so the board script can't be run.");
		return Result.Value!.Value;
	}
}
