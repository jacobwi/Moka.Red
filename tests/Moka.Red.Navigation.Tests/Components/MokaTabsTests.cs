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

namespace Moka.Red.Navigation.Tests.Components;

// The plain tabs on a page. MokaTabStrip is the session tab bar: closable, pinnable, reorderable.
public class MokaTabsTests : BunitContext
{
	private const string KeysModule = "./_content/Moka.Red.Core/moka-keys.js";

	private static readonly IReadOnlyList<MokaTabItem> Three =
	[
		new("a", "Alpha"),
		new("b", "Beta"),
		new("c", "Gamma")
	];

	public MokaTabsTests() => JSInterop.Mode = JSRuntimeMode.Loose;

	private IRenderedComponent<MokaTabs> Tabs(
		IReadOnlyList<MokaTabItem>? items = null,
		string? active = "a",
		Action<string>? changed = null) =>
		Render<MokaTabs>(p =>
		{
			p.Add(x => x.Items, items ?? Three)
				.Add(x => x.Label, "Views")
				.Add(x => x.Active, active);
			if (changed is not null)
			{
				p.Add(x => x.ActiveChanged, EventCallback.Factory.Create<string>(this, changed));
			}
		});

	private static void Press(IRenderedComponent<MokaTabs> cut, string key) =>
		cut.Find("[role=tablist]").KeyDown(new KeyboardEventArgs { Key = key });

	[Fact]
	public void TheList_IsNamedAndHoldsItsTabs()
	{
		IRenderedComponent<MokaTabs> cut = Tabs();

		Assert.Equal("Views", cut.Find("[role=tablist]").GetAttribute("aria-label"));
		Assert.Equal(["Alpha", "Beta", "Gamma"], cut.FindAll("[role=tab]").Select(t => t.TextContent.Trim()));
	}

	[Fact]
	public void TheShowingTab_IsSelected_AndHoldsTheOnlyTabStop()
	{
		IRenderedComponent<MokaTabs> cut = Tabs(active: "b");

		IReadOnlyList<IElement> tabs = cut.FindAll("[role=tab]");
		Assert.Equal(["false", "true", "false"], tabs.Select(t => t.GetAttribute("aria-selected")));
		Assert.Equal(["-1", "0", "-1"], tabs.Select(t => t.GetAttribute("tabindex")));
	}

	// With nothing selected Tab still has to reach the list.
	[Fact]
	public void WithNothingSelected_TheFirstTabHoldsTheTabStop()
	{
		IRenderedComponent<MokaTabs> cut = Tabs(active: null);

		Assert.Equal(["0", "-1", "-1"], cut.FindAll("[role=tab]").Select(t => t.GetAttribute("tabindex")));
		Assert.All(cut.FindAll("[role=tab]"), t => Assert.Equal("false", t.GetAttribute("aria-selected")));
	}

	[Fact]
	public void ADisabledFirstTab_DoesNotHoldTheTabStop()
	{
		IRenderedComponent<MokaTabs> cut = Tabs([new("a", "Alpha", Disabled: true), new("b", "Beta")], active: null);

		Assert.Equal(["-1", "0"], cut.FindAll("[role=tab]").Select(t => t.GetAttribute("tabindex")));
	}

	[Fact]
	public void AClick_ShowsTheTab_AndRaisesTheChange()
	{
		string? changed = null;
		IRenderedComponent<MokaTabs> cut = Tabs(changed: v => changed = v);

		cut.FindAll("[role=tab]")[1].Click();

		Assert.Equal("b", changed);
		Assert.Equal("true", cut.FindAll("[role=tab]")[1].GetAttribute("aria-selected"));
	}

	[Fact]
	public void AClickOnTheShowingTab_RaisesNothing()
	{
		int changes = 0;
		IRenderedComponent<MokaTabs> cut = Tabs(changed: _ => changes++);

		cut.FindAll("[role=tab]")[0].Click();

		Assert.Equal(0, changes);
	}

	// Gotcha 9: the user's pick has to survive a parent render that passes the old value again.
	[Fact]
	public void TheUsersPick_SurvivesAParentRender()
	{
		IRenderedComponent<MokaTabs> cut = Tabs();
		cut.FindAll("[role=tab]")[2].Click();

		cut.Render(p => p.Add(x => x.Items, Three).Add(x => x.Label, "Views").Add(x => x.Active, "a"));

		Assert.Equal("true", cut.FindAll("[role=tab]")[2].GetAttribute("aria-selected"));
	}

	[Fact]
	public void ANewActiveFromTheParent_ShowsThatTab()
	{
		IRenderedComponent<MokaTabs> cut = Tabs();

		cut.Render(p => p.Add(x => x.Items, Three).Add(x => x.Label, "Views").Add(x => x.Active, "c"));

		Assert.Equal("true", cut.FindAll("[role=tab]")[2].GetAttribute("aria-selected"));
	}

	[Theory]
	[InlineData("ArrowRight", "a", "b")]
	[InlineData("ArrowRight", "c", "a")]
	[InlineData("ArrowLeft", "b", "a")]
	[InlineData("ArrowLeft", "a", "c")]
	[InlineData("Home", "c", "a")]
	[InlineData("End", "a", "c")]
	public void ArrowKeys_MoveBetweenTabs_AndShowThem(string key, string from, string to)
	{
		string? changed = null;
		IRenderedComponent<MokaTabs> cut = Tabs(active: from, changed: v => changed = v);

		Press(cut, key);

		Assert.Equal(to, changed);
	}

	[Fact]
	public void ArrowKeys_PassOverADisabledTab()
	{
		string? changed = null;
		IRenderedComponent<MokaTabs> cut = Tabs(
			[new("a", "Alpha"), new("b", "Beta", Disabled: true), new("c", "Gamma")],
			changed: v => changed = v);

		Press(cut, "ArrowRight");

		Assert.Equal("c", changed);
	}

	[Theory]
	[InlineData("Tab")]
	[InlineData("a")]
	[InlineData("Enter")]
	public void OtherKeys_DoNothing(string key)
	{
		int changes = 0;
		IRenderedComponent<MokaTabs> cut = Tabs(changed: _ => changes++);

		Press(cut, key);

		Assert.Equal(0, changes);
	}

	[Fact]
	public void AKeyWithAModifier_IsLeftToTheBrowser()
	{
		int changes = 0;
		IRenderedComponent<MokaTabs> cut = Tabs(changed: _ => changes++);

		cut.Find("[role=tablist]").KeyDown(new KeyboardEventArgs { Key = "ArrowRight", AltKey = true });

		Assert.Equal(0, changes);
	}

	[Fact]
	public void MovingWithTheKeyboard_FocusesTheNewTab()
	{
		IRenderedComponent<MokaTabs> cut = Tabs();

		Press(cut, "ArrowRight");

		Assert.Single(JSInterop.Invocations, i => i.Identifier == "Blazor._internal.domWrapper.focus");
	}

	// The list scrolls sideways, and Home and End would jump the page. The rule has to be in the shape
	// moka-keys.js reads: it calls rule.keys.includes, so a bare list of key names throws on every key.
	[Fact]
	public void TheKeysItActsOn_AreCancelledInTheBrowser()
	{
		BunitJSModuleInterop module = JSInterop.SetupModule(KeysModule);
		module.Mode = JSRuntimeMode.Loose;

		Tabs();

		JSRuntimeInvocation call = Assert.Single(module.Invocations, i => i.Identifier == "preventKeys");
		string rules = JsonSerializer.Serialize(call.Arguments[1]);
		using JsonDocument document = JsonDocument.Parse(rules);
		JsonElement rule = Assert.Single(document.RootElement.EnumerateArray());
		Assert.Equal("[role=\"tab\"]", rule.GetProperty("selector").GetString());
		Assert.Contains("ArrowRight", rule.GetProperty("keys").EnumerateArray().Select(k => k.GetString()));
	}

	[Fact]
	public void ATabWithAnAddress_IsALink()
	{
		IRenderedComponent<MokaTabs> cut = Tabs([new("a", "Alpha", Href: "/alpha"), new("b", "Beta")]);

		IElement link = cut.Find("a[role=tab]");
		Assert.Equal("/alpha", link.GetAttribute("href"));
		Assert.Equal("BUTTON", cut.FindAll("[role=tab]")[1].TagName);
	}

	[Fact]
	public void AButtonTab_CantSubmitAForm()
	{
		IRenderedComponent<MokaTabs> cut = Tabs();

		Assert.Equal("button", cut.Find("button[role=tab]").GetAttribute("type"));
	}

	[Theory]
	[InlineData("javascript:alert(1)")]
	[InlineData("data:text/html,<script>alert(1)</script>")]
	public void ATabWithAScriptAddress_IsNotALink(string href)
	{
		IRenderedComponent<MokaTabs> cut = Tabs([new("a", "Alpha", Href: href)]);

		Assert.Empty(cut.FindAll("a"));
		Assert.Equal("BUTTON", cut.Find("[role=tab]").TagName);
	}

	[Fact]
	public void MovingToALinkWithTheKeyboard_GoesToItsAddress()
	{
		BunitNavigationManager navigation = Services.GetRequiredService<BunitNavigationManager>();
		IRenderedComponent<MokaTabs> cut = Tabs([new("a", "Alpha"), new("b", "Beta", Href: "/beta")]);

		Press(cut, "ArrowRight");

		Assert.EndsWith("/beta", navigation.Uri, StringComparison.Ordinal);
	}

	[Fact]
	public void ADisabledTab_IsADisabledButton_EvenWithAnAddress()
	{
		IRenderedComponent<MokaTabs> cut = Tabs([new("a", "Alpha"), new("b", "Beta", Href: "/beta", Disabled: true)]);

		IElement tab = cut.FindAll("[role=tab]")[1];
		Assert.Equal("BUTTON", tab.TagName);
		Assert.True(tab.HasAttribute("disabled"));
		Assert.Contains("moka-tabs__tab--disabled", tab.ClassName, StringComparison.Ordinal);
	}

	[Fact]
	public void ACount_IsABadgeInItsTone()
	{
		IRenderedComponent<MokaTabs> cut = Tabs(
		[
			new("a", "Alpha", 14),
			new("b", "Beta", 2, MokaBadgeTone.Limited),
			new("c", "Gamma")
		]);

		IReadOnlyList<IElement> tabs = cut.FindAll("[role=tab]");
		Assert.Equal("14", tabs[0].QuerySelector(".moka-badge__indicator")!.TextContent.Trim());
		Assert.NotNull(tabs[0].QuerySelector(".moka-badge--tone-count"));
		Assert.NotNull(tabs[1].QuerySelector(".moka-badge--tone-limited"));
		Assert.Null(tabs[2].QuerySelector(".moka-badge"));
	}

	[Fact]
	public void ACountOfZero_StillShows()
	{
		IRenderedComponent<MokaTabs> cut = Tabs([new("a", "Alpha", 0)]);

		Assert.Equal("0", cut.Find(".moka-badge__indicator").TextContent.Trim());
	}

	[Fact]
	public void AnIcon_ComesBeforeTheName()
	{
		IRenderedComponent<MokaTabs> cut = Tabs([new("a", "Alpha", Icon: MokaIcons.Navigation.Home)]);

		Assert.Equal("svg", cut.Find("[role=tab]").FirstElementChild!.LocalName);
	}

	[Fact]
	public void APanel_IsLabelledByTheShowingTab()
	{
		IRenderedComponent<MokaTabs> cut = Render<MokaTabs>(p => p
			.Add(x => x.Items, Three)
			.Add(x => x.Label, "Views")
			.Add(x => x.Active, "b")
			.Add(x => x.Panel, "<p>Content</p>"));

		IElement panel = cut.Find("[role=tabpanel]");
		IElement tab = cut.FindAll("[role=tab]")[1];
		Assert.Equal(tab.Id, panel.GetAttribute("aria-labelledby"));
		Assert.Equal(panel.Id, tab.GetAttribute("aria-controls"));
		Assert.Equal("0", panel.GetAttribute("tabindex"));
		Assert.Equal("Content", panel.TextContent);
	}

	[Fact]
	public void WithoutAPanel_TabsControlNothing()
	{
		IRenderedComponent<MokaTabs> cut = Tabs();

		Assert.Empty(cut.FindAll("[role=tabpanel]"));
		Assert.All(cut.FindAll("[role=tab]"), t => Assert.False(t.HasAttribute("aria-controls")));
	}

	[Fact]
	public void TwoLists_HaveTheirOwnIds()
	{
		IRenderedComponent<MokaTabs> first = Tabs();
		IRenderedComponent<MokaTabs> second = Tabs();

		Assert.NotEqual(first.FindAll("[role=tab]")[0].Id, second.FindAll("[role=tab]")[0].Id);
	}

	[Fact]
	public void ASharedId_RendersWithoutThrowingOnTheNextRender()
	{
		IReadOnlyList<MokaTabItem> repeated = [new("a", "Alpha"), new("a", "Again")];
		IRenderedComponent<MokaTabs> cut = Tabs(repeated);

		cut.Render(p => p.Add(x => x.Items, repeated).Add(x => x.Label, "Views"));

		Assert.Equal(2, cut.FindAll("[role=tab]").Count);
	}

	[Theory]
	[InlineData(false, false, "moka-tabs--underline")]
	[InlineData(true, false, "moka-tabs--segmented")]
	[InlineData(false, true, "moka-tabs--stretch")]
	public void TheLook_IsAClassOnTheRoot(bool segmented, bool stretch, string expected)
	{
		IRenderedComponent<MokaTabs> cut = Render<MokaTabs>(p => p
			.Add(x => x.Items, Three)
			.Add(x => x.Label, "Views")
			.Add(x => x.Segmented, segmented)
			.Add(x => x.Stretch, stretch));

		Assert.Contains(expected, cut.Find(".moka-tabs").ClassName, StringComparison.Ordinal);
	}

	[Fact]
	public void ClassLandsOnTheRoot_AndAttributesOnTheList()
	{
		IRenderedComponent<MokaTabs> cut = Render<MokaTabs>(p => p
			.Add(x => x.Items, Three)
			.Add(x => x.Label, "Views")
			.Add(x => x.Class, "mine")
			.AddUnmatched("data-section", "x"));

		Assert.Contains("mine", cut.Find(".moka-tabs").ClassName, StringComparison.Ordinal);
		Assert.Equal("x", cut.Find("[role=tablist]").GetAttribute("data-section"));
	}

	[Fact]
	public void AnEmptyList_RendersAnEmptyTablist()
	{
		IRenderedComponent<MokaTabs> cut = Tabs([], active: null);

		Press(cut, "ArrowRight");

		Assert.Empty(cut.FindAll("[role=tab]"));
	}
}
