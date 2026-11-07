using Moka.Red.Icons;
using Moka.Red.Navigation.AppShell;

namespace Moka.Red.Navigation.Tests.Components;

public class MokaNavEntryTests
{
	private static readonly MokaNavEntry[] Entries =
	[
		new(string.Empty, "Home", "/", MokaIcons.Navigation.Home),
		new("Network", "Devices", "/devices", MokaIcons.Action.Search),
		new("Network", "Device groups", "/devices/groups", MokaIcons.Action.Search),
		new("System", "Settings", "/settings", MokaIcons.Action.Settings)
	];

	[Theory]
	[InlineData("", "Home")]
	[InlineData("/", "Home")]
	[InlineData("devices", "Devices")]
	[InlineData("/devices/", "Devices")]
	[InlineData("DEVICES", "Devices")]
	[InlineData("devices/tv", "Devices")]
	[InlineData("devices?tab=all", "Devices")]
	[InlineData("devices#top", "Devices")]
	[InlineData("devices/groups/kids", "Device groups")]
	public void Find_GivesTheEntryAPathBelongsTo(string path, string expected) =>
		Assert.Equal(expected, MokaNavEntry.Find(Entries, path)?.Label);

	[Theory]
	[InlineData("nowhere")]
	[InlineData("devicesandmore")]
	public void Find_GivesNull_ForAPathTheMenuDoesNotList(string path) =>
		Assert.Null(MokaNavEntry.Find(Entries, path));

	[Theory]
	[InlineData("/", "./")]
	[InlineData("/devices", "devices")]
	[InlineData("/settings/general", "settings/general")]
	public void Href_IsRelativeToTheBaseAddress(string route, string expected) =>
		Assert.Equal(expected, new MokaNavEntry(string.Empty, "Screen", route, MokaIcons.Navigation.Home).Href);

	[Fact]
	public void TheDivider_IsTheOnlyAccountItemThatCountsAsOne()
	{
		Assert.True(MokaAccountItem.Divider.IsDivider);
		Assert.False(new MokaAccountItem(string.Empty).IsDivider);
	}
}
