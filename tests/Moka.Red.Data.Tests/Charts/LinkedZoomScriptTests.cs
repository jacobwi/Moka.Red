using System.Text.Json;

namespace Moka.Red.Data.Tests.Charts;

/// <summary>
///     What moka-chart.js tells .NET when charts link their zoom, run in Node.js over a small stand-in for the DOM
///     (<c>Script/linked-zoom.mjs</c>). Skipped where Node.js isn't installed.
/// </summary>
public sealed class LinkedZoomScriptTests
{
	private static readonly Lazy<JsonElement?> Result = new(() => ChartScriptRunner.Run("linked-zoom.mjs"));

	[Fact]
	public void AKeyZoom_TellsItsOwnChartAndTheLinkedOne_TheSameRangeOnce()
	{
		JsonElement calls = Results().GetProperty("key").GetProperty("calls");

		JsonElement own = Assert.Single(calls.GetProperty("a").EnumerateArray());
		JsonElement linked = Assert.Single(calls.GetProperty("b").EnumerateArray());
		Assert.Equal("OnRange", own[0].GetString());
		Assert.Equal(own.GetRawText(), linked.GetRawText());
		Assert.True(own[1].GetDouble() > 1000 && own[2].GetDouble() < 2000);
	}

	[Fact]
	public void ALinkedChart_StretchesWhileTheGestureRuns_AndChartsThatDontMatchHearNothing()
	{
		JsonElement key = Results().GetProperty("key");

		Assert.True(key.GetProperty("stretched").GetBoolean());
		Assert.Empty(key.GetProperty("calls").GetProperty("c").EnumerateArray());
		Assert.Empty(key.GetProperty("calls").GetProperty("d").EnumerateArray());
	}

	[Fact]
	public void AZoomChosenOnTheServer_GoesOnlyToTheOthers_AndSoDoesAReset()
	{
		JsonElement share = Results().GetProperty("share");

		Assert.Empty(share.GetProperty("zoomed").GetProperty("a").EnumerateArray());
		Assert.Equal("[[\"OnRange\",20,40]]", share.GetProperty("zoomed").GetProperty("b").GetRawText());
		Assert.Empty(share.GetProperty("reset").GetProperty("a").EnumerateArray());
		Assert.Equal("[[\"OnRange\",20,40],[\"OnRange\",null,null]]", share.GetProperty("reset").GetProperty("b").GetRawText());
	}

	private static JsonElement Results()
	{
		Assert.SkipUnless(Result.Value is not null, "Node.js isn't installed, so the chart script can't be run.");
		return Result.Value!.Value;
	}
}
