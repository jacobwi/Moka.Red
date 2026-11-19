using System.Text.Json;
using Moka.Red.Tests.Shared;

namespace Moka.Red.Primitives.Tests.Components;

/// <summary>
///     How MokaLogViewer.razor.js keeps the newest row in view, run in Node.js over a stand-in for a scrolling list
///     (<c>Script/log-follow.mjs</c>): a list 100 px tall over 1,000 px of rows. Skipped where Node.js isn't installed.
/// </summary>
public sealed class MokaLogViewerScriptTests
{
	private static readonly Lazy<JsonElement?> Result = new(() => ScriptRunner.Run(
		["tests", "Moka.Red.Primitives.Tests", "Components", "Script", "log-follow.mjs"],
		["src", "Moka.Red.Primitives", "LogViewer", "MokaLogViewer.razor.js"]));

	private static JsonElement Json()
	{
		JsonElement? result = Result.Value;
		Assert.SkipUnless(result is not null, "Node.js isn't installed, so the log viewer's script can't be run.");
		return result!.Value;
	}

	[Fact]
	public void Following_StartsAtTheNewestRow() =>
		Assert.Equal(900, Json().GetProperty("start").GetDouble());

	[Fact]
	public void AtTheBottom_NewRowsStayInView() =>
		Assert.Equal(950, Json().GetProperty("afterRows").GetDouble());

	// The wheel moves the list before the scroll event says so; a row arriving in between mustn't pull it back.
	[Fact]
	public void AWheelTurnJustBeforeARowArrives_IsNotUndone() =>
		Assert.Equal(500, Json().GetProperty("wheelBeforeRow").GetDouble());

	[Fact]
	public void ScrolledUp_TheListStaysWhereTheReaderIs() =>
		Assert.Equal(500, Json().GetProperty("scrolledUp").GetDouble());

	[Fact]
	public void BackAtTheBottom_TheListFollowsAgain()
	{
		JsonElement json = Json();

		Assert.Equal(json.GetProperty("backAtBottomHeight").GetDouble() - 100, json.GetProperty("backAtBottom").GetDouble());
	}

	[Fact]
	public void FollowingTwice_WatchesTheListOnce() =>
		Assert.Equal(1, Json().GetProperty("watchers").GetInt32());

	[Fact]
	public void Unfollowing_StopsListeningAndWatching()
	{
		JsonElement json = Json();

		Assert.Equal(json.GetProperty("backAtBottom").GetDouble(), json.GetProperty("afterUnfollow").GetDouble());
		Assert.False(json.GetProperty("listening").GetBoolean());
		Assert.Equal(0, json.GetProperty("watchersAfterUnfollow").GetInt32());
	}

	// A zoomed page leaves scrollTop half a pixel short of the bottom, which still counts as the bottom.
	[Fact]
	public void AZoomedPagesFractionalBottom_StillFollows() =>
		Assert.Equal(910, Json().GetProperty("zoomed").GetDouble());
}
