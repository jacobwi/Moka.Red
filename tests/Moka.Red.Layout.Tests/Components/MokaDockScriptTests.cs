using System.Text.Json;

namespace Moka.Red.Layout.Tests.Components;

// What MokaDockLayout.razor.js does and tells .NET, run in Node.js over a small stand-in for the DOM
// (Script/dock-layout.mjs). Skipped where Node.js isn't installed.
public sealed class MokaDockScriptTests
{
	private static readonly Lazy<JsonElement?> Result = new(DockScriptRunner.Run);

	[Fact]
	public void APreset_AppliesAsTheViewTransitionsUpdate_AndWaitsOnNoFrame()
	{
		JsonElement animated = Results().GetProperty("transition").GetProperty("animated");

		Assert.Equal(["named true", "ApplyPresetNowAsync Debug", "update done", "returned true"],
			animated.GetProperty("log").EnumerateArray().Select(e => e.GetString()));
		Assert.Equal(0, animated.GetProperty("frames").GetInt32());
		Assert.Equal(1, animated.GetProperty("started").GetInt32());
		Assert.True(animated.GetProperty("namedWhileRunning").GetBoolean());
		Assert.False(animated.GetProperty("namedAfter").GetBoolean());
	}

	[Fact]
	public void APreset_OnAPageThatDrawsNoFrames_SkipsTheTransition_AndStillApplies()
	{
		JsonElement stalled = Results().GetProperty("transition").GetProperty("stalled");

		Assert.Equal(["skipped", "ApplyPresetNowAsync Write", "returned true"],
			stalled.GetProperty("log").EnumerateArray().Select(e => e.GetString()));
		Assert.InRange(stalled.GetProperty("ms").GetInt32(), 100, 2000);
		Assert.False(stalled.GetProperty("namedAfter").GetBoolean());
	}

	[Fact]
	public void APreset_StartsNoTransition_OnAHiddenPage_WithLessMotion_OrWithoutTheApi()
	{
		JsonElement transition = Results().GetProperty("transition");

		foreach (string name in new[] { "hidden", "reduced" })
		{
			Assert.False(transition.GetProperty(name).GetProperty("result").GetBoolean());
			Assert.Equal(2, transition.GetProperty(name).GetProperty("started").GetInt32());
		}

		Assert.False(transition.GetProperty("unsupported").GetBoolean());
	}

	[Fact]
	public void ATabThatBecomesTheOneShowing_ScrollsIntoView_AndAScrolledStripKeepsItsPlace()
	{
		JsonElement reveal = Results().GetProperty("reveal");

		Assert.Equal(0, reveal.GetProperty("atStart").GetDouble());
		Assert.Equal(150, reveal.GetProperty("scrolledByThePerson").GetDouble());
		Assert.Equal(400, reveal.GetProperty("afterDrop").GetDouble());
	}

	[Fact]
	public void Escape_ReachesDotNetWhateverThePageShows_AndIsClaimedOnlyWhenSomethingIsShown()
	{
		JsonElement escape = Results().GetProperty("escape");

		Assert.False(escape.GetProperty("nothingShown").GetProperty("claimed").GetBoolean());
		Assert.Equal(["OnEscape"], escape.GetProperty("nothingShown").GetProperty("calls").EnumerateArray().Select(e => e.GetString()));
		Assert.True(escape.GetProperty("maximized").GetProperty("claimed").GetBoolean());
		Assert.Equal(["OnEscape"], escape.GetProperty("maximized").GetProperty("calls").EnumerateArray().Select(e => e.GetString()));
		Assert.Empty(escape.GetProperty("inMenu").GetProperty("calls").EnumerateArray());
	}

	[Fact]
	public void WithDraggingOff_AFloatingWindowStillMovesByItsHeader_AndNothingDocksElsewhere()
	{
		JsonElement noDrag = Results().GetProperty("noDrag");

		Assert.Equal(0, noDrag.GetProperty("listenersFromRefusedPresses").GetInt32());
		Assert.Equal("200px", noDrag.GetProperty("left").GetString());
		Assert.Equal("80px", noDrag.GetProperty("top").GetString());
		Assert.Equal(["OnFloatMoved"], noDrag.GetProperty("calls").EnumerateArray().Select(e => e.GetString()));
	}

	[Fact]
	public void MeasureMain_GivesTheAreaWindowsFloatOver()
	{
		JsonElement measure = Results().GetProperty("measure");

		Assert.Equal(JsonValueKind.Null, measure.GetProperty("empty").ValueKind);
		Assert.Equal([640d, 180d], measure.GetProperty("main").EnumerateArray().Select(e => e.GetDouble()));
	}

	private static JsonElement Results()
	{
		Assert.SkipUnless(Result.Value is not null, "Node.js isn't installed, so the dock layout script can't be run.");
		return Result.Value!.Value;
	}
}
