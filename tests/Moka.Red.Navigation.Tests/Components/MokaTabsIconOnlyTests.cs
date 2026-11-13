using System.Text.Json;
using AngleSharp.Dom;
using Bunit;
using Bunit.TestDoubles;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Moka.Red.Feedback.Tooltip;
using Moka.Red.Icons;
using Moka.Red.Navigation.Tabs;
using Moka.Red.Primitives.Badge;
using Moka.Red.Tests.Shared;

namespace Moka.Red.Navigation.Tests.Components;

// A phone's tab strip shows icons only. Each such tab keeps its name for screen readers and as a tooltip, as every
// icon-only control in the library does, and the keys and the overflow menu work as before.
public class MokaTabsIconOnlyTests : BunitContext
{
	private const string Css = "MokaTabs.razor.css";
	private const string FitModule = "./_content/Moka.Red.Core/moka-fit.js";

	private static readonly IReadOnlyList<MokaTabItem> Four =
	[
		new("files", "Files", Icon: MokaIcons.File.Folder),
		new("photos", "Photos", 12, MokaBadgeTone.NeedsYou, Icon: MokaIcons.Content.Image),
		new("activity", "Activity"),
		new("shared", "Shared", Href: "/shared", Icon: MokaIcons.Action.Share)
	];

	private readonly BunitJSModuleInterop _fit;

	public MokaTabsIconOnlyTests()
	{
		JSInterop.Mode = JSRuntimeMode.Loose;
		_fit = JSInterop.SetupModule(FitModule);
		_fit.Mode = JSRuntimeMode.Loose;
	}

	[Fact]
	public void Never_IsTheDefault_AndKeepsTheTabsStraightInTheList()
	{
		IRenderedComponent<MokaTabs> cut = Tabs(MokaTabsIconOnly.Never);

		Assert.Equal(MokaTabsIconOnly.Never, cut.Instance.IconOnly);
		Assert.Equal("moka-tabs moka-tabs--underline", cut.Find(".moka-tabs").ClassName);
		Assert.Equal(4, cut.FindAll("[role=tablist] > [role=tab]").Count);
		Assert.Empty(cut.FindAll(".moka-tabs__tip"));
		Assert.All(cut.FindAll("[role=tab]"), tab => Assert.False(tab.HasAttribute("aria-label")));
		Assert.All(cut.FindAll("[role=tab]"), tab => Assert.False(TextOf(tab).HasAttribute("class")));
	}

	[Fact]
	public void Always_PutsEveryTabInATooltip_AndNamesTheOnesWithAnIcon()
	{
		IRenderedComponent<MokaTabs> cut = Tabs(MokaTabsIconOnly.Always);

		Assert.Contains("moka-tabs--icons", cut.Find(".moka-tabs").ClassList);
		Assert.Equal(4, cut.FindAll("[role=tablist] > .moka-tabs__tip > [role=tab]").Count);

		IElement files = Tab(cut, "Files");
		Assert.Equal("Files", files.GetAttribute("aria-label"));
		Assert.Contains("moka-tabs__tab--icon-only", files.ClassList);
		Assert.Equal("Files", TooltipOf(files)?.TextContent.Trim());
		Assert.Null(files.GetAttribute("title"));
	}

	// The badge still shows the count, so the name says it too, as the text and badge read together today.
	[Fact]
	public void ACountedTab_SaysItsCount()
	{
		IRenderedComponent<MokaTabs> cut = Tabs(MokaTabsIconOnly.Always);

		IElement photos = Tab(cut, "Photos");
		Assert.Equal("Photos 12", photos.GetAttribute("aria-label"));
		Assert.Equal("Photos", TooltipOf(photos)?.TextContent.Trim());
		Assert.NotNull(photos.QuerySelector(".moka-badge"));
	}

	// Its name is the only thing it could show.
	[Fact]
	public void ATabWithoutAnIcon_KeepsItsName_AndHasNoTooltip()
	{
		IRenderedComponent<MokaTabs> cut = Tabs(MokaTabsIconOnly.Always);

		IElement activity = Tab(cut, "Activity");
		Assert.False(activity.HasAttribute("aria-label"));
		Assert.DoesNotContain("moka-tabs__tab--icon-only", activity.ClassList);
		Assert.Null(TooltipOf(activity));
	}

	[Fact]
	public void ALinkTab_IsNamedAndTippedToo()
	{
		IRenderedComponent<MokaTabs> cut = Tabs(MokaTabsIconOnly.Always);

		IElement shared = cut.Find("a[role=tab]");
		Assert.Equal("Shared", shared.GetAttribute("aria-label"));
		Assert.Equal("Shared", TooltipOf(shared)?.TextContent.Trim());
	}

	[Fact]
	public void TheTooltips_OpenWhereTheyAreTold()
	{
		IRenderedComponent<MokaTabs> cut = Tabs(MokaTabsIconOnly.Always, p => p.Add(x => x.TooltipPosition, MokaTooltipPosition.Top));

		Assert.All(cut.FindComponents<MokaTooltip>(), tip => Assert.Equal(MokaTooltipPosition.Top, tip.Instance.Position));
	}

	[Fact]
	public void Narrow_IsASizeContainerThatFills()
	{
		IRenderedComponent<MokaTabs> cut = Tabs(MokaTabsIconOnly.Narrow);

		IElement root = cut.Find(".moka-tabs");
		Assert.Contains("moka-tabs--icons-narrow", root.ClassList);
		Assert.Contains("moka-fill-width", root.ClassList);
		Assert.DoesNotContain("moka-tabs--icons", root.ClassList);
		Assert.Equal("moka-tabs / inline-size", ScopedCss.Declarations(Css, ".moka-tabs--icons-narrow")["container"]);
		Assert.Equal("Files", Tab(cut, "Files").GetAttribute("aria-label"));
	}

	[Fact]
	public void TheNames_HideOnlyWhereTheyShould()
	{
		const string always = ".moka-tabs--icons > .moka-tabs__list ::deep > .moka-tabs__tip > .moka-tabs__tab--icon-only > .moka-tabs__text";
		const string narrow = ".moka-tabs--icons-narrow > .moka-tabs__list ::deep > .moka-tabs__tip > .moka-tabs__tab--icon-only > .moka-tabs__text";
		const string quiet = ".moka-tabs.moka-tabs--icons-narrow > .moka-tabs__list ::deep > .moka-tabs__tip > .moka-tooltip-popup";

		Assert.Equal("none", ScopedCss.Declarations(Css, always)["display"]);
		Assert.Equal("none", ScopedCss.Declarations(Css, narrow)["display"]);
		Assert.Equal("none", ScopedCss.Declarations(Css, quiet)["display"]);

		string css = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Css", Css)).ReplaceLineEndings("\n");
		Assert.Contains("@container moka-tabs (width <= 30rem) {\n\t" + narrow, css, StringComparison.Ordinal);
		Assert.Contains("@container moka-tabs (width > 30rem) {\n\t" + quiet, css, StringComparison.Ordinal);
	}

	// Each rule a tab takes reaches it inside its tooltip as well, so nothing looks different but the names.
	[Fact]
	public void EveryTabRule_ReachesATabInATooltip()
	{
		string[] tabRules =
		[
			"", ":hover:not(.moka-tabs__tab--disabled)", ":focus-visible", "--selected", "--disabled"
		];
		foreach (string rule in tabRules)
		{
			IReadOnlyDictionary<string, string> plain = ScopedCss.Declarations(Css, $".moka-tabs > .moka-tabs__list > .moka-tabs__tab{rule}");
			IReadOnlyDictionary<string, string> tipped = ScopedCss.Declarations(Css, $".moka-tabs > .moka-tabs__list ::deep > .moka-tabs__tip > .moka-tabs__tab{rule}");
			Assert.NotEmpty(plain);
			Assert.Equal(plain, tipped);
		}

		Assert.Equal(
			ScopedCss.Declarations(Css, ".moka-tabs--segmented > .moka-tabs__list > .moka-tabs__tab"),
			ScopedCss.Declarations(Css, ".moka-tabs--segmented > .moka-tabs__list ::deep > .moka-tabs__tip > .moka-tabs__tab"));
		Assert.Equal("1 1 0", ScopedCss.Declarations(Css, ".moka-tabs--stretch > .moka-tabs__list ::deep > .moka-tabs__tip")["flex"]);
		Assert.Equal("none", ScopedCss.Declarations(Css, ".moka-tabs--menu > .moka-tabs__list ::deep > [data-moka-dropped]")["display"]);
	}

	[Fact]
	public void TheArrowKeys_StillMoveTheSelection()
	{
		string? picked = null;
		IRenderedComponent<MokaTabs> cut = Tabs(MokaTabsIconOnly.Always,
			p => p.Add(x => x.ActiveChanged, EventCallback.Factory.Create<string>(this, id => picked = id)));

		cut.Find("[role=tablist]").KeyDown(new KeyboardEventArgs { Key = "ArrowRight" });

		Assert.Equal("photos", picked);
		Assert.Equal("true", Tab(cut, "Photos").GetAttribute("aria-selected"));
		Assert.Equal("0", Tab(cut, "Photos").GetAttribute("tabindex"));
	}

	// With tooltips the list's children are the tooltips' elements, so the tab that stays is found inside them.
	[Fact]
	public void TheOverflowScript_KeepsTheTabInsideATooltip()
	{
		Tabs(MokaTabsIconOnly.Always, p => p.Add(x => x.Overflow, MokaTabsOverflow.Menu));

		JsonElement layout = JsonDocument.Parse(JsonSerializer.Serialize(_fit.Invocations["bindFit"][0].Arguments[2])).RootElement;
		Assert.Equal(":has(> [aria-selected=\"true\"]), :has(> [tabindex=\"0\"])", layout.GetProperty("keep").GetString());
		Assert.True(layout.GetProperty("positions").GetBoolean());
	}

	[Fact]
	public void TheOverflowMenu_StillShowsNames()
	{
		IRenderedComponent<MokaTabs> cut = Tabs(MokaTabsIconOnly.Always, p => p.Add(x => x.Overflow, MokaTabsOverflow.Menu));
		cut.InvokeAsync(() => cut.Instance.OnDroppedChanged([1, 3]));

		cut.Find(".moka-popover-trigger").Click();

		Assert.Equal(["Photos", "Shared"], cut.FindAll(".moka-tabs__menu-text").Select(text => text.TextContent));
	}

	// The script keeps the layout it was first bound with, so turning icons on starts it again.
	[Fact]
	public void TurningIconsOn_BindsTheScriptAgainWithTheNewLayout()
	{
		IRenderedComponent<MokaTabs> cut = Tabs(MokaTabsIconOnly.Never, p => p.Add(x => x.Overflow, MokaTabsOverflow.Menu));

		cut.Render(p => p.Add(x => x.Items, Four).Add(x => x.Label, "Views").Add(x => x.Overflow, MokaTabsOverflow.Menu)
			.Add(x => x.IconOnly, MokaTabsIconOnly.Always));

		Assert.Single(_fit.Invocations["unbindFit"]);
		Assert.Equal(2, _fit.Invocations["bindFit"].Count);
	}

	private IRenderedComponent<MokaTabs> Tabs(MokaTabsIconOnly iconOnly, Action<ComponentParameterCollectionBuilder<MokaTabs>>? extra = null) =>
		Render<MokaTabs>(p =>
		{
			p.Add(x => x.Items, Four).Add(x => x.Label, "Views").Add(x => x.Active, "files").Add(x => x.IconOnly, iconOnly);
			extra?.Invoke(p);
		});

	private static IElement Tab(IRenderedComponent<MokaTabs> cut, string text) =>
		cut.FindAll("[role=tab]").Single(tab => TextOf(tab).TextContent == text);

	// The tab's name is its first span; the badge comes after it.
	private static IElement TextOf(IElement tab) => tab.Children.First(child => child.LocalName == "span");

	private static IElement? TooltipOf(IElement tab) =>
		tab.ParentElement?.Children.FirstOrDefault(child => child.ClassList.Contains("moka-tooltip-popup"));
}
