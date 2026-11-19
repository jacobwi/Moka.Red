using System.Text.Json;
using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Moka.Red.Icons;
using Moka.Red.Navigation.Tabs;
using Moka.Red.Tests.Shared;

namespace Moka.Red.Navigation.Tests.Components;

// A tablist may own only tabs (WAI-ARIA 1.2), so a group's name sits in an element hidden from screen readers and
// reaches them as each of its tabs' description. The arrows still walk every tab in order, across groups.
public class MokaTabsGroupTests : BunitContext
{
	private const string Css = "MokaTabs.razor.css";
	private const string FitModule = "./_content/Moka.Red.Core/moka-fit.js";

	private static readonly IReadOnlyList<MokaTabItem> Grouped =
	[
		new("overview", "Overview") { Group = "General" },
		new("activity", "Activity") { Group = "General" },
		new("disks", "Disks") { Group = "Storage" },
		new("shares", "Shares", 3) { Group = "Storage" },
		new("wifi", "Wi-Fi") { Group = "Network" },
		new("vpn", "VPN", Href: "/vpn") { Group = "Network" }
	];

	private readonly BunitJSModuleInterop _fit;

	public MokaTabsGroupTests()
	{
		JSInterop.Mode = JSRuntimeMode.Loose;
		_fit = JSInterop.SetupModule(FitModule);
		_fit.Mode = JSRuntimeMode.Loose;
	}

	private IRenderedComponent<MokaTabs> Tabs(
		IReadOnlyList<MokaTabItem>? items = null,
		string? active = "overview",
		Action<string>? changed = null,
		Action<ComponentParameterCollectionBuilder<MokaTabs>>? more = null) =>
		Render<MokaTabs>(p =>
		{
			p.Add(x => x.Items, items ?? Grouped)
				.Add(x => x.Label, "Settings")
				.Add(x => x.Active, active);
			if (changed is not null)
			{
				p.Add(x => x.ActiveChanged, EventCallback.Factory.Create<string>(this, changed));
			}

			more?.Invoke(p);
		});

	private static IReadOnlyList<IElement> Heads(IRenderedComponent<MokaTabs> cut) =>
		cut.FindAll("[role=tablist] > .moka-tabs__group");

	private static string? DescriptionOf(IRenderedComponent<MokaTabs> cut, IElement tab) =>
		tab.GetAttribute("aria-describedby") is { } id ? cut.Find($"#{id}").TextContent.Trim() : null;

	private static void Press(IRenderedComponent<MokaTabs> cut, string key) =>
		cut.Find("[role=tablist]").KeyDown(new KeyboardEventArgs { Key = key });

	private static void Drop(IRenderedComponent<MokaTabs> cut, params int[] positions) =>
		cut.InvokeAsync(() => cut.Instance.OnDroppedChanged(positions));

	private static void OpenMenu(IRenderedComponent<MokaTabs> cut) =>
		cut.Find(".moka-popover-trigger").Click();

	private static string Flat(IElement element) =>
		string.Concat(element.TextContent.Where(c => !char.IsWhiteSpace(c)));

	[Fact]
	public void WithoutGroups_TheListHoldsOnlyTabs_AndNoTabHasADescription()
	{
		IRenderedComponent<MokaTabs> cut = Tabs([new("a", "Alpha"), new("b", "Beta")], active: "a");

		Assert.Equal("moka-tabs moka-tabs--underline", cut.Find(".moka-tabs").ClassName);
		Assert.All(cut.Find("[role=tablist]").Children, child => Assert.Equal("tab", child.GetAttribute("role")));
		Assert.All(cut.FindAll("[role=tab]"), tab => Assert.False(tab.HasAttribute("aria-describedby")));
	}

	[Fact]
	public void EachGroup_StartsWithItsName()
	{
		IRenderedComponent<MokaTabs> cut = Tabs();

		Assert.Equal(["General", "Storage", "Network"], Heads(cut).Select(h => h.TextContent.Trim()));
		Assert.All(Heads(cut), h => Assert.NotNull(h.QuerySelector(".moka-tabs__group-name.moka-micro-label")));
		Assert.Equal(
			["group", "tab", "tab", "group", "tab", "tab", "group", "tab", "tab"],
			cut.Find("[role=tablist]").Children.Select(c => c.ClassList.Contains("moka-tabs__group") ? "group" : c.GetAttribute("role")));
	}

	// What a screen reader finds in the tablist is tabs alone: every other child is out of the accessibility tree.
	[Fact]
	public void TheTablist_OwnsOnlyTabs()
	{
		IRenderedComponent<MokaTabs> cut = Tabs();

		Assert.All(
			cut.Find("[role=tablist]").Children.Where(c => c.GetAttribute("role") != "tab"),
			c => Assert.Equal("true", c.GetAttribute("aria-hidden")));
		Assert.Empty(cut.FindAll("[role=tablist] [role=group], [role=tablist] [role=separator]"));
	}

	[Fact]
	public void EveryTab_IsDescribedByItsGroupsName()
	{
		IRenderedComponent<MokaTabs> cut = Tabs();

		Assert.Equal(
			["General", "General", "Storage", "Storage", "Network", "Network"],
			cut.FindAll("[role=tab]").Select(t => DescriptionOf(cut, t)));
	}

	[Fact]
	public void ALinkTab_IsDescribedByItsGroupToo()
	{
		IRenderedComponent<MokaTabs> cut = Tabs();

		IElement link = cut.Find("a[role=tab]");
		Assert.Equal("/vpn", link.GetAttribute("href"));
		Assert.Equal("Network", DescriptionOf(cut, link));
	}

	[Fact]
	public void TwoLists_HaveTheirOwnGroupIds()
	{
		IRenderedComponent<MokaTabs> first = Tabs();
		IRenderedComponent<MokaTabs> second = Tabs();

		Assert.NotEqual(Heads(first)[0].Id, Heads(second)[0].Id);
		Assert.Equal(3, Heads(first).Select(h => h.Id).Distinct().Count());
	}

	// Tabs in no group need no start before the first group, and only a line after one.
	[Fact]
	public void TabsInNoGroup_HaveNoDescription_AndAfterAGroupStartWithALineOnly()
	{
		IRenderedComponent<MokaTabs> cut = Tabs(
		[
			new("home", "Home"),
			new("disks", "Disks") { Group = "Storage" },
			new("help", "Help")
		], active: "home");

		IReadOnlyList<IElement> heads = Heads(cut);
		Assert.Equal(2, heads.Count);
		Assert.Equal("Storage", heads[0].TextContent.Trim());
		Assert.Equal(string.Empty, heads[1].TextContent.Trim());
		Assert.False(heads[1].HasAttribute("id"));
		Assert.Equal("tab", cut.Find("[role=tablist]").FirstElementChild!.GetAttribute("role"));
		Assert.Equal([null, "Storage", null], cut.FindAll("[role=tab]").Select(t => DescriptionOf(cut, t)));
	}

	[Fact]
	public void AGroupThatComesBack_StartsAgainWithItsName()
	{
		IRenderedComponent<MokaTabs> cut = Tabs(
		[
			new("a", "Alpha") { Group = "One" },
			new("b", "Beta") { Group = "Two" },
			new("c", "Gamma") { Group = "One" }
		], active: "a");

		Assert.Equal(["One", "Two", "One"], Heads(cut).Select(h => h.TextContent.Trim()));
	}

	[Theory]
	[InlineData("ArrowRight", "activity", "disks")]
	[InlineData("ArrowLeft", "disks", "activity")]
	[InlineData("ArrowRight", "shares", "wifi")]
	[InlineData("ArrowRight", "vpn", "overview")]
	[InlineData("ArrowLeft", "overview", "vpn")]
	[InlineData("Home", "wifi", "overview")]
	[InlineData("End", "disks", "vpn")]
	public void ArrowKeys_MoveAcrossGroups_InOrder(string key, string from, string to)
	{
		string? changed = null;
		IRenderedComponent<MokaTabs> cut = Tabs(active: from, changed: v => changed = v);

		Press(cut, key);

		Assert.Equal(to, changed);
	}

	[Fact]
	public void ArrowKeys_PassOverADisabledTab_AtTheStartOfAGroup()
	{
		string? changed = null;
		IRenderedComponent<MokaTabs> cut = Tabs(
		[
			new("a", "Alpha") { Group = "One" },
			new("b", "Beta", Disabled: true) { Group = "Two" },
			new("c", "Gamma") { Group = "Two" }
		], active: "a", changed: v => changed = v);

		Press(cut, "ArrowRight");

		Assert.Equal("c", changed);
	}

	[Fact]
	public void TheShowingTab_HoldsTheOnlyTabStop_InWhicheverGroup()
	{
		IRenderedComponent<MokaTabs> cut = Tabs(active: "shares");

		Assert.Equal(["-1", "-1", "-1", "0", "-1", "-1"], cut.FindAll("[role=tab]").Select(t => t.GetAttribute("tabindex")));
		Assert.All(Heads(cut), h => Assert.False(h.HasAttribute("tabindex")));
	}

	[Fact]
	public void Divider_KeepsTheNameForTheDescription_AndSaysSoOnTheRoot()
	{
		IRenderedComponent<MokaTabs> cut = Tabs(more: p => p.Add(x => x.GroupLook, MokaTabsGroupLook.Divider));

		Assert.Contains("moka-tabs--group-dividers", cut.Find(".moka-tabs").ClassList);
		Assert.Equal("Storage", DescriptionOf(cut, cut.FindAll("[role=tab]")[2]));
	}

	[Fact]
	public void Divider_WithoutGroups_ChangesNothing()
	{
		IRenderedComponent<MokaTabs> cut = Tabs(
			[new("a", "Alpha"), new("b", "Beta")],
			active: "a",
			more: p => p.Add(x => x.GroupLook, MokaTabsGroupLook.Divider));

		Assert.Equal("moka-tabs moka-tabs--underline", cut.Find(".moka-tabs").ClassName);
	}

	[Fact]
	public void IconOnly_KeepsTheNamesBesideTheTooltips_AndTheDescriptions()
	{
		IRenderedComponent<MokaTabs> cut = Tabs(
		[
			new("files", "Files", Icon: MokaIcons.File.Folder) { Group = "Library" },
			new("photos", "Photos", Icon: MokaIcons.Content.Image) { Group = "Library" },
			new("settings", "Settings", Icon: MokaIcons.Action.Settings) { Group = "App" }
		], active: "files", more: p => p.Add(x => x.IconOnly, MokaTabsIconOnly.Always));

		Assert.Equal(
			["group", "tip", "tip", "group", "tip"],
			cut.Find("[role=tablist]").Children.Select(c => c.ClassList.Contains("moka-tabs__group") ? "group" : c.ClassList.Contains("moka-tabs__tip") ? "tip" : "other"));
		IElement settings = cut.Find("[role=tablist] > .moka-tabs__tip [aria-label=Settings]");
		Assert.Equal("App", DescriptionOf(cut, settings));
	}

	[Theory]
	[InlineData(false, false)]
	[InlineData(true, false)]
	[InlineData(false, true)]
	[InlineData(true, true)]
	public void SegmentedAndStretch_KeepTheGroups(bool segmented, bool stretch)
	{
		IRenderedComponent<MokaTabs> cut = Tabs(more: p => p.Add(x => x.Segmented, segmented).Add(x => x.Stretch, stretch));

		Assert.Equal(3, Heads(cut).Count);
		Assert.Equal(6, cut.FindAll("[role=tablist] > [role=tab]").Count);
	}

	[Fact]
	public void ANewList_WithoutGroups_TakesTheGroupsAway()
	{
		IRenderedComponent<MokaTabs> cut = Tabs(more: p => p.Add(x => x.GroupLook, MokaTabsGroupLook.Divider));

		cut.Render(p => p.Add(x => x.Items, [new MokaTabItem("a", "Alpha")]).Add(x => x.Label, "Settings"));

		Assert.Empty(Heads(cut));
		Assert.DoesNotContain("moka-tabs--group-dividers", cut.Find(".moka-tabs").ClassList);
	}

	// The script hides a heading whose tabs all went into the menu, and reports positions in Items, not in the list.
	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public void TheScript_IsToldWhatHeadsAGroup(bool iconOnly)
	{
		Tabs(more: p => p
			.Add(x => x.Overflow, MokaTabsOverflow.Menu)
			.Add(x => x.IconOnly, iconOnly ? MokaTabsIconOnly.Always : MokaTabsIconOnly.Never));

		string json = JsonSerializer.Serialize(_fit.Invocations["bindFit"][0].Arguments[2]);
		JsonElement layout = JsonDocument.Parse(json).RootElement;
		Assert.Equal(".moka-tabs__group", layout.GetProperty("heading").GetString());
		Assert.True(layout.GetProperty("positions").GetBoolean());
	}

	[Fact]
	public void TheMenu_ListsTheTabsThatDidNotFit_UnderTheirGroups()
	{
		IRenderedComponent<MokaTabs> cut = Tabs(more: p => p.Add(x => x.Overflow, MokaTabsOverflow.Menu));
		Drop(cut, 3, 4, 5);

		OpenMenu(cut);

		IReadOnlyList<IElement> groups = cut.FindAll("[role=menu] > [role=group]");
		Assert.Equal(["Storage", "Network"], groups.Select(g => g.GetAttribute("aria-label")));
		Assert.Equal(["Shares3"], groups[0].QuerySelectorAll("[role=menuitem]").Select(Flat));
		Assert.Equal(["Wi-Fi", "VPN"], groups[1].QuerySelectorAll("[role=menuitem]").Select(Flat));

		IElement heading = groups[0].FirstElementChild!;
		Assert.Equal("Storage", heading.TextContent.Trim());
		Assert.Equal("true", heading.GetAttribute("aria-hidden"));
		Assert.Contains("moka-micro-label", heading.ClassList);
		Assert.Equal("+3", cut.Find("[data-moka-more]").TextContent);
	}

	[Fact]
	public void PickingFromAGroupInTheMenu_ShowsTheTab()
	{
		string? changed = null;
		IRenderedComponent<MokaTabs> cut = Tabs(changed: v => changed = v, more: p => p.Add(x => x.Overflow, MokaTabsOverflow.Menu));
		Drop(cut, 4, 5);
		OpenMenu(cut);

		cut.Find("[role=group] [role=menuitem]").Click();

		Assert.Equal("wifi", changed);
	}

	// Tabs that are next to each other in the menu and share a group share a heading, though the tab showing sat
	// between them.
	[Fact]
	public void TheMenu_GathersAGroupsTabs_AroundTheOneThatStayed()
	{
		IRenderedComponent<MokaTabs> cut = Tabs(
		[
			new("overview", "Overview") { Group = "General" },
			new("disks", "Disks") { Group = "Storage" },
			new("shares", "Shares") { Group = "Storage" },
			new("backups", "Backups") { Group = "Storage" }
		], active: "shares", more: p => p.Add(x => x.Overflow, MokaTabsOverflow.Menu));
		Drop(cut, 1, 3);

		OpenMenu(cut);

		IElement group = Assert.Single(cut.FindAll("[role=menu] > [role=group]"));
		Assert.Equal("Storage", group.GetAttribute("aria-label"));
		Assert.Equal(["Disks", "Backups"], group.QuerySelectorAll("[role=menuitem]").Select(Flat));
	}

	[Fact]
	public void InTheMenu_TabsInNoGroupAfterAGroup_StandBehindALine()
	{
		IRenderedComponent<MokaTabs> cut = Tabs(
		[
			new("home", "Home"),
			new("disks", "Disks") { Group = "Storage" },
			new("help", "Help"),
			new("about", "About")
		], active: "home", more: p => p.Add(x => x.Overflow, MokaTabsOverflow.Menu));
		Drop(cut, 1, 2, 3);

		OpenMenu(cut);

		IElement menu = cut.Find("[role=menu]");
		Assert.Equal(
			["group", "separator", "menuitem", "menuitem"],
			menu.Children.Select(c => c.GetAttribute("role")));
	}

	[Fact]
	public void WithoutGroups_TheMenuHoldsOnlyItsItems()
	{
		IRenderedComponent<MokaTabs> cut = Tabs(
			[new("a", "Alpha"), new("b", "Beta"), new("c", "Gamma")],
			active: "a",
			more: p => p.Add(x => x.Overflow, MokaTabsOverflow.Menu));
		Drop(cut, 1, 2);

		OpenMenu(cut);

		Assert.All(cut.Find("[role=menu]").Children, c => Assert.Equal("menuitem", c.GetAttribute("role")));
	}

	[Fact]
	public void ArrowKeys_PassOverGroupsTabsInTheMenu()
	{
		string? changed = null;
		IRenderedComponent<MokaTabs> cut = Tabs(active: "activity", changed: v => changed = v, more: p => p.Add(x => x.Overflow, MokaTabsOverflow.Menu));
		Drop(cut, 2, 3);

		Press(cut, "ArrowRight");

		Assert.Equal("wifi", changed);
	}

	[Fact]
	public void AHeading_TakesNoShareOfAStretchedRow() =>
		Assert.Equal("none", ScopedCss.Declarations(Css, ".moka-tabs > .moka-tabs__list > .moka-tabs__group")["flex"]);

	// The line shows only when something shows before the heading, so the first group, or the first one left, has none.
	[Fact]
	public void TheLine_StandsOnlyAfterSomethingShowing()
	{
		IReadOnlyDictionary<string, string> line = ScopedCss.Declarations(Css, ".moka-tabs > .moka-tabs__list > :not([data-moka-dropped]) ~ .moka-tabs__group::before");

		Assert.Equal("\"\"", line["content"]);
		Assert.Equal("var(--moka-border-width)", line["width"]);
	}

	// The first heading then has nothing to show, and would only add a gap before the first tab.
	[Theory]
	[InlineData(".moka-tabs--group-dividers > .moka-tabs__list > .moka-tabs__group > .moka-tabs__group-name")]
	[InlineData(".moka-tabs--group-dividers > .moka-tabs__list > .moka-tabs__group:first-child")]
	[InlineData(".moka-tabs--icons > .moka-tabs__list > .moka-tabs__group > .moka-tabs__group-name")]
	[InlineData(".moka-tabs--icons > .moka-tabs__list > .moka-tabs__group:first-child")]
	[InlineData(".moka-tabs--icons-narrow > .moka-tabs__list > .moka-tabs__group > .moka-tabs__group-name")]
	[InlineData(".moka-tabs--icons-narrow > .moka-tabs__list > .moka-tabs__group:first-child")]
	public void TheName_TakesNoRoom_WithLinesOnlyOrIconsOnly(string selector) =>
		Assert.Equal("none", ScopedCss.Declarations(Css, selector)["display"]);

	[Fact]
	public void AHeadingTheScriptHid_TakesNoPlace() =>
		Assert.Equal("none", ScopedCss.Declarations(Css, ".moka-tabs--menu > .moka-tabs__list > [data-moka-dropped]")["display"]);
}
