using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Moka.Red.Feedback.Tooltip;
using Moka.Red.Icons;
using Moka.Red.Navigation.Breadcrumb;
using Moka.Red.Primitives.Utility;
using Moka.Red.Tests.Shared;

namespace Moka.Red.Navigation.Tests.Components;

public class MokaBreadcrumbTests : BunitContext
{
	private const string Css = "MokaBreadcrumb.razor.css";

	public MokaBreadcrumbTests() => JSInterop.Mode = JSRuntimeMode.Loose;

	[Fact]
	public void Renders_NavElement()
	{
		IRenderedComponent<MokaBreadcrumb> cut = Render<MokaBreadcrumb>(p => p
			.AddChildContent<MokaBreadcrumbItem>(item => item
				.Add(x => x.Text, "Home")));

		IElement nav = cut.Find("nav");
		Assert.NotNull(nav);
		Assert.Equal("Breadcrumb", nav.GetAttribute("aria-label"));
	}

	[Fact]
	public void Renders_RootClass()
	{
		IRenderedComponent<MokaBreadcrumb> cut = Render<MokaBreadcrumb>(p => p
			.AddChildContent<MokaBreadcrumbItem>(item => item
				.Add(x => x.Text, "Home")));

		IElement el = cut.Find(".moka-breadcrumb");
		Assert.NotNull(el);
	}

	[Fact]
	public void Renders_OrderedList()
	{
		IRenderedComponent<MokaBreadcrumb> cut = Render<MokaBreadcrumb>(p => p
			.AddChildContent<MokaBreadcrumbItem>(item => item
				.Add(x => x.Text, "Home")));

		IElement ol = cut.Find("ol.moka-breadcrumb__list");
		Assert.NotNull(ol);
	}

	[Fact]
	public void Appends_UserClass()
	{
		IRenderedComponent<MokaBreadcrumb> cut = Render<MokaBreadcrumb>(p => p
			.Add(x => x.Class, "custom-bc")
			.AddChildContent<MokaBreadcrumbItem>(item => item
				.Add(x => x.Text, "Home")));

		IElement nav = cut.Find("nav");
		Assert.Contains("custom-bc", nav.ClassList);
	}

	[Fact]
	public void TheDefaults_AddATrailClassAndNoStyle()
	{
		IRenderedComponent<MokaBreadcrumb> cut = RenderTrail();

		IElement nav = cut.Find("nav");
		Assert.Equal("moka-breadcrumb moka-breadcrumb--trail moka-breadcrumb--has-back", nav.ClassName);
		Assert.Null(nav.GetAttribute("style"));
		Assert.Equal("Home", cut.Find("a.moka-breadcrumb-item__link .moka-breadcrumb-item__label").TextContent);
	}

	// The default separator is the look's: CSS shows the slash on the original design and the chevron on the new one.
	[Fact]
	public void TheDefaultSeparator_CarriesBothGlyphs()
	{
		IRenderedComponent<MokaBreadcrumb> cut = RenderTrail();

		IElement separator = cut.Find(".moka-breadcrumb-item__separator");
		Assert.Equal("true", separator.GetAttribute("aria-hidden"));
		Assert.Equal("/", separator.QuerySelector(".moka-breadcrumb__slash")!.TextContent);
		Assert.NotNull(separator.QuerySelector(".moka-breadcrumb__chevron svg"));
	}

	[Fact]
	public void AStringSeparator_IsDrawnOnEveryLook()
	{
		IRenderedComponent<MokaBreadcrumb> cut = RenderTrail(p => p.Add(x => x.Separator, ">"));

		IElement separator = cut.Find(".moka-breadcrumb-item__separator");
		Assert.Equal(">", separator.TextContent.Trim());
		Assert.Null(separator.QuerySelector(".moka-breadcrumb__chevron"));
	}

	[Fact]
	public void SeparatorContent_WinsOverTheText()
	{
		IRenderedComponent<MokaBreadcrumb> cut = RenderTrail(p => p
			.Add(x => x.Separator, ">")
			.Add(x => x.SeparatorContent, (RenderFragment)(b => b.AddMarkupContent(0, "<b class=\"sep\">|</b>"))));

		Assert.Equal("|", cut.Find(".moka-breadcrumb-item__separator .sep").TextContent);
		Assert.DoesNotContain(">", cut.Find(".moka-breadcrumb-item__separator").TextContent, StringComparison.Ordinal);
	}

	[Fact]
	public void IconOnly_ShowsTheIconNamedByTheText()
	{
		IRenderedComponent<MokaBreadcrumb> cut = Render<MokaBreadcrumb>(p => p
			.AddChildContent<MokaBreadcrumbItem>(item => item
				.Add(x => x.Text, "Home")
				.Add(x => x.Href, "/")
				.Add(x => x.Icon, MokaIcons.Navigation.Home)
				.Add(x => x.IconOnly, true))
			.AddChildContent<MokaBreadcrumbItem>(item => item.Add(x => x.Text, "Settings")));

		IElement link = cut.Find("a.moka-breadcrumb-item__link");
		IconTooltip.AssertNamed(link, "Home", MokaTooltipPosition.BottomStart);
		Assert.Contains("moka-breadcrumb-item__link--icon", link.ClassList);
		Assert.Contains("moka-breadcrumb-item--icon-only", cut.Find(".moka-breadcrumb-item").ClassList);
		Assert.Empty(link.QuerySelectorAll(".moka-breadcrumb-item__label"));
		Assert.NotNull(link.QuerySelector("svg"));
	}

	[Fact]
	public void IconOnly_WithoutAnIcon_KeepsTheText()
	{
		IRenderedComponent<MokaBreadcrumb> cut = Render<MokaBreadcrumb>(p => p
			.AddChildContent<MokaBreadcrumbItem>(item => item
				.Add(x => x.Text, "Home")
				.Add(x => x.Href, "/")
				.Add(x => x.IconOnly, true))
			.AddChildContent<MokaBreadcrumbItem>(item => item.Add(x => x.Text, "Settings")));

		IElement link = cut.Find("a.moka-breadcrumb-item__link");
		Assert.Null(link.GetAttribute("aria-label"));
		Assert.Equal("Home", link.TextContent.Trim());
	}

	[Fact]
	public void IconOnly_OnTheCurrentPage_KeepsTheNameForScreenReaders()
	{
		IRenderedComponent<MokaBreadcrumb> cut = Render<MokaBreadcrumb>(p => p
			.AddChildContent<MokaBreadcrumbItem>(item => item
				.Add(x => x.Text, "Home")
				.Add(x => x.Icon, MokaIcons.Navigation.Home)
				.Add(x => x.IconOnly, true)));

		IElement current = cut.Find("[aria-current='page']");
		Assert.Equal("Home", current.QuerySelector(".moka-visually-hidden")!.TextContent);
	}

	[Fact]
	public void ALongName_GetsATooltipWithAllOfIt()
	{
		const string name = "Living room media server, upstairs";
		IRenderedComponent<MokaBreadcrumb> cut = Render<MokaBreadcrumb>(p => p
			.AddChildContent<MokaBreadcrumbItem>(item => item.Add(x => x.Text, "Home").Add(x => x.Href, "/"))
			.AddChildContent<MokaBreadcrumbItem>(item => item.Add(x => x.Text, name).Add(x => x.Href, "/server"))
			.AddChildContent<MokaBreadcrumbItem>(item => item.Add(x => x.Text, "Ports")));

		IElement link = cut.Find("a[href='/server']");
		IElement popup = Assert.Single(link.Closest(".moka-tooltip-trigger")!.Children, c => c.ClassList.Contains("moka-tooltip-popup"));
		Assert.Equal(name, popup.TextContent.Trim());
		Assert.Null(cut.Find("a[href='/']").Closest(".moka-tooltip-trigger"));
	}

	[Fact]
	public void ItemMaxWidth_OfItsOwn_IsWrittenAndCutsOnEveryLook()
	{
		IRenderedComponent<MokaBreadcrumb> cut = RenderTrail(p => p.Add(x => x.ItemMaxWidth, "8rem"));

		IElement nav = cut.Find("nav");
		Assert.Contains("moka-breadcrumb--item-max", nav.ClassList);
		Assert.Equal("--_breadcrumb-item-max: 8rem", nav.GetAttribute("style"));
	}

	[Fact]
	public void ANarrowerItemMaxWidth_GivesShorterNamesATooltip()
	{
		IRenderedComponent<MokaBreadcrumb> cut = Render<MokaBreadcrumb>(p => p
			.Add(x => x.ItemMaxWidth, "8rem")
			.AddChildContent<MokaBreadcrumbItem>(item => item.Add(x => x.Text, "Network settings").Add(x => x.Href, "/n"))
			.AddChildContent<MokaBreadcrumbItem>(item => item.Add(x => x.Text, "Wired")));

		Assert.NotNull(cut.Find("a[href='/n']").Closest(".moka-tooltip-trigger"));
	}

	[Fact]
	public void MoreLabel_ReplacesTheCount()
	{
		IRenderedComponent<MokaBreadcrumb> cut = RenderTrail(p => p
			.Add(x => x.MaxItems, 2)
			.Add(x => x.MoreLabel, "{n} hidden"));

		Assert.Equal("1 hidden", cut.Find(".moka-breadcrumb__more").GetAttribute("aria-label"));
	}

	[Fact]
	public void PathLook_PutsTheListInAScrollingWell()
	{
		IRenderedComponent<MokaBreadcrumb> cut = RenderTrail(p => p.Add(x => x.Look, MokaBreadcrumbLook.Path));

		IElement nav = cut.Find("nav");
		Assert.Contains("moka-breadcrumb--path", nav.ClassList);
		Assert.DoesNotContain("moka-breadcrumb--trail", nav.ClassList);
		Assert.NotNull(cut.Find("nav > .moka-breadcrumb__scroll.moka-hide-scrollbar > ol.moka-breadcrumb__list"));
		Assert.Empty(cut.FindAll(".moka-breadcrumb__back"));
	}

	[Fact]
	public void PathLook_KeepsEveryLevel_WhateverMaxItemsSays()
	{
		IRenderedComponent<MokaBreadcrumb> cut = RenderTrail(p => p
			.Add(x => x.Look, MokaBreadcrumbLook.Path)
			.Add(x => x.MaxItems, 1));

		Assert.Empty(cut.FindAll(".moka-breadcrumb__fold"));
		Assert.Equal(3, cut.FindAll(".moka-breadcrumb-item").Count);
	}

	[Fact]
	public void Root_ComesBeforeTheFirstLevel()
	{
		IRenderedComponent<MokaBreadcrumb> cut = RenderTrail(p => p
			.Add(x => x.Look, MokaBreadcrumbLook.Path)
			.Add(x => x.Root, (RenderFragment)(b => b.AddMarkupContent(0, "<span class=\"host\">mokagate</span>"))));

		IElement first = cut.Find("nav").FirstElementChild!;
		Assert.Contains("moka-breadcrumb__root", first.ClassList);
		Assert.Equal("mokagate", first.TextContent);
	}

	[Fact]
	public void Copyable_CopiesTheSegmentsJoinedBySlashes()
	{
		IRenderedComponent<MokaBreadcrumb> cut = Render<MokaBreadcrumb>(p => p
			.Add(x => x.Look, MokaBreadcrumbLook.Path)
			.Add(x => x.Copyable, true)
			.AddChildContent<MokaBreadcrumbItem>(item => item.Add(x => x.Text, "network").Add(x => x.Href, "/network"))
			.AddChildContent<MokaBreadcrumbItem>(item => item.Add(x => x.Text, "devices").Add(x => x.Href, "/devices"))
			.AddChildContent<MokaBreadcrumbItem>(item => item.Add(x => x.Text, "Office desktop").Add(x => x.Segment, "office-desktop")));

		MokaCopyButton copy = cut.FindComponent<MokaCopyButton>().Instance;
		Assert.Equal("network/devices/office-desktop", copy.Text);
		Assert.True(copy.Quiet);
		Assert.Equal("Copy path", cut.Find(".moka-copy-btn").GetAttribute("aria-label"));
	}

	[Fact]
	public void CopyText_ReplacesTheJoinedPath()
	{
		IRenderedComponent<MokaBreadcrumb> cut = RenderTrail(p => p
			.Add(x => x.Copyable, true)
			.Add(x => x.CopyText, "https://router.local/devices/42"));

		Assert.Equal("https://router.local/devices/42", cut.FindComponent<MokaCopyButton>().Instance.Text);
	}

	[Fact]
	public void NotCopyable_HasNoCopyButton()
	{
		IRenderedComponent<MokaBreadcrumb> cut = RenderTrail(p => p.Add(x => x.Look, MokaBreadcrumbLook.Path));

		Assert.Empty(cut.FindComponents<MokaCopyButton>());
	}

	[Fact]
	public void AnItemOnItsOwn_IsALevelWithNoSeparator()
	{
		IRenderedComponent<MokaBreadcrumbItem> cut = Render<MokaBreadcrumbItem>(p => p
			.Add(x => x.Text, "Home")
			.Add(x => x.Href, "/"));

		Assert.Equal("/", cut.Find("li > a").GetAttribute("href"));
		Assert.Empty(cut.FindAll(".moka-breadcrumb-item__separator"));
	}

	[Fact]
	public void TheStylesheet_HidesTheNarrowPartsAndTheBackLinkUntilABoxIsNarrow()
	{
		Assert.Equal("none", ScopedCss.Declarations(Css, "::deep .moka-breadcrumb__narrow")["display"]);
		Assert.Equal("none", ScopedCss.Declarations(Css, ".moka-breadcrumb__back")["display"]);
		Assert.Equal("none", ScopedCss.Declarations(Css, ".moka-breadcrumb--fold-narrow ::deep .moka-breadcrumb__wide")["display"]);
		Assert.Equal("inline-flex", ScopedCss.Declarations(Css, ".moka-breadcrumb--fold-medium ::deep .moka-breadcrumb__narrow")["display"]);
		Assert.Equal("inline-flex", ScopedCss.Declarations(Css, ".moka-gate .moka-breadcrumb--has-back > .moka-breadcrumb__back")["display"]);
		Assert.Equal("none", ScopedCss.Declarations(Css, ".moka-gate .moka-breadcrumb--has-back > .moka-breadcrumb__list")["display"]);
	}

	[Fact]
	public void TheStylesheet_MakesTheNewTrailASizeContainer_ButNotInACommandBar()
	{
		Assert.Equal("moka-breadcrumb / inline-size", ScopedCss.Declarations(Css, ".moka-gate .moka-breadcrumb--trail")["container"]);
		Assert.Equal("normal", ScopedCss.Declarations(Css, ".moka-gate .moka-command-bar__left .moka-breadcrumb--trail")["container-type"]);
		Assert.Equal("normal", ScopedCss.Declarations(Css, ".moka-command-bar__left .moka-breadcrumb")["container-type"]);
	}

	// On Red the accent reads as a warning, so the new look's links are quiet and the current page is a neutral pill.
	[Fact]
	public void TheStylesheet_KeepsTheAccentOffTheNewLooksLinks()
	{
		IReadOnlyDictionary<string, string> link = ScopedCss.Declarations(Css, ".moka-gate .moka-breadcrumb--trail ::deep .moka-breadcrumb-item__link");
		Assert.Equal("var(--moka-color-on-surface-variant)", link["color"]);
		Assert.Equal("1.75rem", link["height"]);

		IReadOnlyDictionary<string, string> current = ScopedCss.Declarations(
			Css, ".moka-gate .moka-breadcrumb--trail ::deep .moka-breadcrumb-item--active .moka-breadcrumb-item__text");
		Assert.Equal("var(--moka-color-neutral-bg)", current["background"]);
		Assert.Equal("var(--moka-radius-full)", current["border-radius"]);
	}

	private IRenderedComponent<MokaBreadcrumb> RenderTrail(Action<ComponentParameterCollectionBuilder<MokaBreadcrumb>>? extra = null) =>
		Render<MokaBreadcrumb>(p =>
		{
			extra?.Invoke(p);
			p.AddChildContent<MokaBreadcrumbItem>(item => item.Add(x => x.Text, "Home").Add(x => x.Href, "/"))
				.AddChildContent<MokaBreadcrumbItem>(item => item.Add(x => x.Text, "Network").Add(x => x.Href, "/network"))
				.AddChildContent<MokaBreadcrumbItem>(item => item.Add(x => x.Text, "Devices"));
		});
}
