using System.Text.Json;
using AngleSharp.Dom;
using Bunit;
using Bunit.TestDoubles;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Moka.Red.Icons;
using Moka.Red.Navigation.Tabs;
using Moka.Red.Primitives.Badge;
using Moka.Red.Tests.Shared;

namespace Moka.Red.Navigation.Tests.Components;

// What runs out of room in a browser is measured there and checked by hand. These cover what the browser is given,
// and what the tabs do once it has reported which ones didn't fit.
public class MokaTabsOverflowTests : BunitContext
{
	private const string Css = "MokaTabs.razor.css";
	private const string Module = "./_content/Moka.Red.Core/moka-fit.js";

	private static readonly IReadOnlyList<MokaTabItem> Five =
	[
		new("a", "Alpha"),
		new("b", "Beta"),
		new("c", "Gamma", 7, MokaBadgeTone.NeedsYou, Icon: MokaIcons.Navigation.Home),
		new("d", "Delta", Disabled: true),
		new("e", "Epsilon", Href: "/epsilon")
	];

	private readonly BunitJSModuleInterop _module;

	public MokaTabsOverflowTests()
	{
		JSInterop.Mode = JSRuntimeMode.Loose;
		_module = JSInterop.SetupModule(Module);
		_module.Mode = JSRuntimeMode.Loose;
	}

	private IRenderedComponent<MokaTabs> Tabs(
		IReadOnlyList<MokaTabItem>? items = null,
		string? active = "a",
		Action<string>? changed = null,
		MokaTabsOverflow overflow = MokaTabsOverflow.Menu) =>
		Render<MokaTabs>(p =>
		{
			p.Add(x => x.Items, items ?? Five)
				.Add(x => x.Label, "Views")
				.Add(x => x.Active, active)
				.Add(x => x.Overflow, overflow);
			if (changed is not null)
			{
				p.Add(x => x.ActiveChanged, EventCallback.Factory.Create<string>(this, changed));
			}
		});

	private static void Drop(IRenderedComponent<MokaTabs> cut, params int[] positions) =>
		cut.InvokeAsync(() => cut.Instance.OnDroppedChanged(positions));

	private static void OpenMenu(IRenderedComponent<MokaTabs> cut) =>
		cut.Find(".moka-popover-trigger").Click();

	private JsonElement BoundLayout()
	{
		string json = JsonSerializer.Serialize(_module.Invocations["bindFit"][0].Arguments[2]);
		return JsonDocument.Parse(json).RootElement;
	}

	[Fact]
	public void ScrollIsTheDefault()
	{
		IRenderedComponent<MokaTabs> cut = Render<MokaTabs>(p => p.Add(x => x.Items, Five).Add(x => x.Label, "Views"));

		Assert.Equal(MokaTabsOverflow.Scroll, cut.Instance.Overflow);
		Assert.Equal("moka-tabs moka-tabs--underline", cut.Find(".moka-tabs").ClassName);
		Assert.Empty(cut.FindAll(".moka-tabs__more"));
		Assert.Empty(_module.Invocations["bindFit"]);
	}

	[Fact]
	public void ScrollKeepsItsRules()
	{
		IReadOnlyDictionary<string, string> list = ScopedCss.Declarations(Css, ".moka-tabs > .moka-tabs__list");

		Assert.Equal("auto", list["overflow-x"]);
		Assert.Equal("flex", list["display"]);
		Assert.Equal("flex", ScopedCss.Declarations(Css, ".moka-tabs")["display"]);
	}

	[Fact]
	public void Menu_IsAClassOnTheRoot()
	{
		IRenderedComponent<MokaTabs> cut = Tabs();

		Assert.Contains("moka-tabs--menu", cut.Find(".moka-tabs").ClassList);
	}

	[Fact]
	public void TheList_IsWatchedOnce()
	{
		IRenderedComponent<MokaTabs> cut = Tabs();

		cut.Render(p => p.Add(x => x.Items, Five).Add(x => x.Label, "Views").Add(x => x.Overflow, MokaTabsOverflow.Menu).Add(x => x.Active, "b"));

		JSRuntimeInvocation bind = Assert.Single(_module.Invocations["bindFit"]);
		Assert.IsType<ElementReference>(bind.Arguments[0]);
		Assert.NotNull(bind.Arguments[1]);
	}

	[Fact]
	public void TheScript_IsToldTheOpenTabStaysAndToReportWhere()
	{
		Tabs();

		JsonElement layout = BoundLayout();
		Assert.Equal(":scope > .moka-tabs__list", Assert.Single(layout.GetProperty("sides").EnumerateArray()).GetString());
		Assert.Equal(":scope > .moka-tabs__more [data-moka-more]", layout.GetProperty("more").GetString());
		Assert.Equal("[aria-selected=\"true\"], [tabindex=\"0\"]", layout.GetProperty("keep").GetString());
		Assert.Contains("aria-selected", layout.GetProperty("watch").EnumerateArray().Select(a => a.GetString()));
		Assert.True(layout.GetProperty("positions").GetBoolean());
		Assert.True(layout.GetProperty("flexible").GetBoolean());
	}

	[Fact]
	public void AChangeOfLook_MeasuresAgain()
	{
		IRenderedComponent<MokaTabs> cut = Tabs();

		cut.Render(p => p.Add(x => x.Items, Five).Add(x => x.Label, "Views").Add(x => x.Overflow, MokaTabsOverflow.Menu).Add(x => x.Segmented, true));

		Assert.Equal(2, _module.Invocations["bindFit"].Count);
	}

	[Fact]
	public void GoingBackToScroll_StopsWatchingAndEmptiesTheMenu()
	{
		IRenderedComponent<MokaTabs> cut = Tabs();
		Drop(cut, 3, 4);

		cut.Render(p => p.Add(x => x.Items, Five).Add(x => x.Label, "Views").Add(x => x.Overflow, MokaTabsOverflow.Scroll));

		Assert.Single(_module.Invocations["unbindFit"]);
		Assert.Empty(cut.FindAll(".moka-tabs__more"));
	}

	[Fact]
	public async Task TheList_StopsBeingWatchedWhenItGoes()
	{
		IRenderedComponent<MokaTabs> cut = Tabs();

		await cut.Instance.DisposeAsync();

		Assert.Single(_module.Invocations["unbindFit"]);
	}

	// A tablist may hold only tabs, so a menu button inside one would be read out wrongly.
	[Fact]
	public void TheButton_IsOutsideTheTablist()
	{
		IRenderedComponent<MokaTabs> cut = Tabs();

		Assert.Null(cut.Find("[role=tablist]").QuerySelector("[data-moka-more]"));
		Assert.NotNull(cut.Find(".moka-tabs > .moka-tabs__more button[data-moka-more]"));
	}

	[Fact]
	public void TheButton_SaysHowMany()
	{
		IRenderedComponent<MokaTabs> cut = Tabs();

		Drop(cut, 3, 4);

		IElement button = cut.Find("[data-moka-more]");
		Assert.Equal("+2", button.TextContent);
		Assert.Equal("2 more tabs", button.GetAttribute("aria-label"));
		Assert.Equal("button", button.GetAttribute("type"));
	}

	[Fact]
	public void TheButtonsName_SaysOneWhenThereIsOne()
	{
		IRenderedComponent<MokaTabs> cut = Tabs();

		Drop(cut, 4);

		Assert.Equal("1 more tab", cut.Find("[data-moka-more]").GetAttribute("aria-label"));
	}

	[Fact]
	public void TheButtonsName_CanBeReplaced()
	{
		IRenderedComponent<MokaTabs> cut = Render<MokaTabs>(p => p
			.Add(x => x.Items, Five)
			.Add(x => x.Label, "Views")
			.Add(x => x.Overflow, MokaTabsOverflow.Menu)
			.Add(x => x.MoreLabel, count => $"{count} de plus"));

		Drop(cut, 4);

		Assert.Equal("1 de plus", cut.Find("[data-moka-more]").GetAttribute("aria-label"));
	}

	[Fact]
	public void TheButton_ClaimsNoNumberBeforeTheScriptHasCounted()
	{
		IRenderedComponent<MokaTabs> cut = Tabs();

		Assert.Equal("+", cut.Find("[data-moka-more]").TextContent);
	}

	[Fact]
	public void TheMenu_ListsOnlyTheTabsThatDidNotFit_InOrder()
	{
		IRenderedComponent<MokaTabs> cut = Tabs();
		Drop(cut, 2, 4);

		OpenMenu(cut);

		Assert.Equal(["Gamma7", "Epsilon"], cut.FindAll("[role=menuitem]").Select(i => i.TextContent.Replace("\n", "", StringComparison.Ordinal).Replace("\t", "", StringComparison.Ordinal).Trim()));
	}

	[Fact]
	public void AMenuItem_KeepsTheTabsIconAndCount()
	{
		IRenderedComponent<MokaTabs> cut = Tabs();
		Drop(cut, 2);

		OpenMenu(cut);

		IElement item = cut.Find("[role=menuitem]");
		Assert.NotNull(item.QuerySelector("svg"));
		Assert.Equal("7", item.QuerySelector(".moka-badge__indicator")!.TextContent.Trim());
		Assert.NotNull(item.QuerySelector(".moka-badge--tone-needs-you"));
	}

	[Fact]
	public void ADisabledTab_IsDisabledInTheMenu()
	{
		IRenderedComponent<MokaTabs> cut = Tabs();
		Drop(cut, 3);

		OpenMenu(cut);

		Assert.Equal("true", cut.Find("[role=menuitem]").GetAttribute("aria-disabled"));
	}

	[Fact]
	public void PickingFromTheMenu_ShowsTheTab()
	{
		string? changed = null;
		IRenderedComponent<MokaTabs> cut = Tabs(changed: v => changed = v);
		Drop(cut, 1);
		OpenMenu(cut);

		cut.Find("[role=menuitem]").Click();

		Assert.Equal("b", changed);
		Assert.Equal("true", cut.FindAll("[role=tab]")[1].GetAttribute("aria-selected"));
		Assert.Empty(cut.FindAll("[role=menu]"));
	}

	[Fact]
	public void ADisabledMenuItem_DoesNothing()
	{
		int changes = 0;
		IRenderedComponent<MokaTabs> cut = Tabs(changed: _ => changes++);
		Drop(cut, 3);
		OpenMenu(cut);

		cut.Find("[role=menuitem]").Click();

		Assert.Equal(0, changes);
	}

	[Fact]
	public void ALinkInTheMenu_IsARealLink()
	{
		IRenderedComponent<MokaTabs> cut = Tabs();
		Drop(cut, 4);
		OpenMenu(cut);

		IElement link = cut.Find("[role=menuitem] a");

		Assert.Equal("/epsilon", link.GetAttribute("href"));
		Assert.Equal("-1", link.GetAttribute("tabindex"));
		Assert.Equal("true", link.GetAttribute("aria-hidden"));
	}

	[Fact]
	public void AScriptAddress_IsNotALinkInTheMenu()
	{
		IRenderedComponent<MokaTabs> cut = Tabs([new("a", "Alpha"), new("b", "Beta", Href: "javascript:alert(1)")]);
		Drop(cut, 1);
		OpenMenu(cut);

		Assert.Empty(cut.FindAll("[role=menuitem] a"));
	}

	[Fact]
	public void ADisabledLink_IsNotALinkInTheMenu()
	{
		IRenderedComponent<MokaTabs> cut = Tabs([new("a", "Alpha"), new("b", "Beta", Href: "/beta", Disabled: true)]);
		Drop(cut, 1);
		OpenMenu(cut);

		Assert.Empty(cut.FindAll("[role=menuitem] a"));
	}

	// Enter and Space click the row, not the link on it, and report no click count.
	[Fact]
	public void TheKeyboardOnALink_GoesToItsAddress()
	{
		BunitNavigationManager navigation = Services.GetRequiredService<BunitNavigationManager>();
		string? changed = null;
		IRenderedComponent<MokaTabs> cut = Tabs(changed: v => changed = v);
		Drop(cut, 4);
		OpenMenu(cut);

		cut.Find("[role=menuitem]").Click(new MouseEventArgs { Detail = 0 });

		Assert.EndsWith("/epsilon", navigation.Uri, StringComparison.Ordinal);
		Assert.Equal("e", changed);
	}

	// The link itself takes a mouse click, and Blazor's own link handling goes there.
	[Fact]
	public void TheMouseOnALink_LeavesTheGoingToTheLink()
	{
		BunitNavigationManager navigation = Services.GetRequiredService<BunitNavigationManager>();
		string before = navigation.Uri;
		string? changed = null;
		IRenderedComponent<MokaTabs> cut = Tabs(changed: v => changed = v);
		Drop(cut, 4);
		OpenMenu(cut);

		cut.Find("[role=menuitem]").Click(new MouseEventArgs { Detail = 1 });

		Assert.Equal(before, navigation.Uri);
		Assert.Equal("e", changed);
	}

	// Opening the address in a new tab leaves this page where it was.
	[Theory]
	[InlineData(true, false, false)]
	[InlineData(false, true, false)]
	[InlineData(false, false, true)]
	public void AModifiedClickOnALink_DoesNotSwitchTheTab(bool ctrl, bool shift, bool meta)
	{
		int changes = 0;
		IRenderedComponent<MokaTabs> cut = Tabs(changed: _ => changes++);
		Drop(cut, 4);
		OpenMenu(cut);

		cut.Find("[role=menuitem]").Click(new MouseEventArgs { Detail = 1, CtrlKey = ctrl, ShiftKey = shift, MetaKey = meta });

		Assert.Equal(0, changes);
	}

	[Fact]
	public void TheMenu_ClosesOnceNothingIsInIt()
	{
		IRenderedComponent<MokaTabs> cut = Tabs();
		Drop(cut, 4);
		OpenMenu(cut);

		Drop(cut);

		Assert.Empty(cut.FindAll("[role=menu]"));
	}

	[Theory]
	[InlineData("ArrowRight", "a", "c")]
	[InlineData("ArrowLeft", "a", "e")]
	[InlineData("End", "a", "e")]
	[InlineData("Home", "e", "a")]
	public void ArrowKeys_PassOverTabsThatDidNotFit(string key, string from, string to)
	{
		string? changed = null;
		IRenderedComponent<MokaTabs> cut = Tabs(
			[new("a", "Alpha"), new("b", "Beta"), new("c", "Gamma"), new("d", "Delta"), new("e", "Epsilon")],
			active: from,
			changed: v => changed = v);
		Drop(cut, 1, 3);

		cut.Find("[role=tablist]").KeyDown(new KeyboardEventArgs { Key = key });

		Assert.Equal(to, changed);
	}

	[Fact]
	public void APositionPastTheEnd_IsIgnored()
	{
		IRenderedComponent<MokaTabs> cut = Tabs();

		Drop(cut, 4, 9);

		Assert.Equal("+1", cut.Find("[data-moka-more]").TextContent);
	}

	[Fact]
	public void AShorterList_TakesItsTabsOutOfTheMenu()
	{
		IRenderedComponent<MokaTabs> cut = Tabs();
		Drop(cut, 3, 4);

		cut.Render(p => p.Add(x => x.Items, Five.Take(4).ToList()).Add(x => x.Label, "Views").Add(x => x.Overflow, MokaTabsOverflow.Menu));

		Assert.Equal("+1", cut.Find("[data-moka-more]").TextContent);
	}

	[Fact]
	public void ADroppedTab_TakesNoPlaceAndNoFocus() =>
		Assert.Equal("none", ScopedCss.Declarations(Css, ".moka-tabs--menu > .moka-tabs__list > [data-moka-dropped]")["display"]);

	[Fact]
	public void TheButton_ShowsOnlyOnceSomethingDidNotFit()
	{
		Assert.Equal("none", ScopedCss.Declarations(Css, ".moka-tabs--menu > .moka-tabs__more")["display"]);
		Assert.Equal("flex", ScopedCss.Declarations(Css, ".moka-tabs--menu[data-moka-has-more] > .moka-tabs__more")["display"]);
	}

	[Fact]
	public void OnceMeasured_TheListStopsScrolling() =>
		Assert.Equal("hidden", ScopedCss.Declarations(Css, ".moka-tabs--menu[data-moka-fitted] > .moka-tabs__list")["overflow-x"]);

	[Fact]
	public void BeforeItIsMeasured_TheListScrolls() =>
		Assert.False(ScopedCss.Declarations(Css, ".moka-tabs--menu").ContainsKey("overflow-x"));

	[Fact]
	public void ThePanel_SpansBothColumns() =>
		Assert.Equal("1 / -1", ScopedCss.Declarations(Css, ".moka-tabs--menu > .moka-tabs__panel")["grid-column"]);

	[Fact]
	public void ASegmentedList_HugsItsTabs_AndTheButtonSitsBesideIt()
	{
		IReadOnlyDictionary<string, string> rules = ScopedCss.Declarations(Css, ".moka-tabs--menu.moka-tabs--segmented:not(.moka-tabs--stretch)");

		Assert.Equal("minmax(0, max-content) auto", rules["grid-template-columns"]);
		Assert.Equal("start", rules["justify-content"]);
	}

	[Fact]
	public void TheLineUnderTheTabs_RunsUnderTheButtonToo() =>
		Assert.Equal(
			ScopedCss.Declarations(Css, ".moka-tabs--underline > .moka-tabs__list")["box-shadow"],
			ScopedCss.Declarations(Css, ".moka-tabs--underline.moka-tabs--menu > .moka-tabs__more")["box-shadow"]);
}
