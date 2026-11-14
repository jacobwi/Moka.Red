using AngleSharp.Dom;
using Bunit;
using Bunit.TestDoubles;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Moka.Red.Feedback.Tooltip;
using Moka.Red.Icons;
using Moka.Red.Navigation.AppShell;
using Moka.Red.Navigation.Menu;
using Moka.Red.Tests.Shared;

namespace Moka.Red.Navigation.Tests.Components;

// An entry with children is a page that holds a group of screens: in the shell's menu its label is a link with an arrow
// beside it, and its children are listed in its group rather than faked as entries after it.
public class MokaAppShellChildrenTests : BunitContext
{
	private const string Cancelled = "blazor:onclick:preventDefault";
	private const string ShellModule = "./_content/Moka.Red.Navigation/AppShell/MokaAppShell.razor.js";
	private const string MenuModule = "./_content/Moka.Red.Navigation/Menu/MokaMenu.razor.js";

	private static readonly MokaNavEntry[] Entries =
	[
		new(string.Empty, "Home", "/", MokaIcons.Navigation.Home),
		new("Components", "Feedback", "/feedback", MokaIcons.Status.Bell)
		{
			Children =
			[
				new(string.Empty, "Action bar", "/feedback/action-bar", MokaIcons.Action.Save),
				new(string.Empty, "Alert", "/feedback/alert", MokaIcons.Status.Info)
			]
		},
		new("Components", "Data", "/data", MokaIcons.File.Database)
	];

	private readonly BunitJSModuleInterop _menuModule;

	public MokaAppShellChildrenTests()
	{
		JSInterop.Mode = JSRuntimeMode.Loose;
		JSInterop.SetupModule(ShellModule).Mode = JSRuntimeMode.Loose;
		_menuModule = JSInterop.SetupModule(MenuModule);
		_menuModule.Mode = JSRuntimeMode.Loose;
	}

	private BunitNavigationManager Navigation => Services.GetRequiredService<BunitNavigationManager>();

	[Fact]
	public void AnEntryWithChildren_IsALinkWithAnArrow_AndItsGroupHoldsTheChildren()
	{
		Navigation.NavigateTo("data");
		IRenderedComponent<MokaAppShell> cut = Shell();

		IElement components = cut.FindAll(".moka-app-shell__group")[1];
		IElement feedback = Assert.Single(components.QuerySelectorAll("a.moka-menu-item--group-link"));
		Assert.Equal("feedback", feedback.GetAttribute("href"));
		IElement toggle = components.QuerySelector("button.moka-menu-item__toggle")!;
		IconTooltip.AssertNamed(toggle, "Show Feedback", MokaTooltipPosition.Right);

		IElement group = cut.Find($"#{toggle.GetAttribute("aria-controls")}");
		Assert.Equal(
			["feedback/action-bar", "feedback/alert"],
			group.QuerySelectorAll("a.moka-menu-item").Select(link => link.GetAttribute("href")));
		Assert.Equal(
			["Feedback", "Data"],
			components.QuerySelectorAll(":scope > .moka-menu-item-tooltip .moka-menu-item__text").Select(text => text.TextContent));
	}

	[Fact]
	public void OnAChildsPage_TheChildIsCurrent_ItsGroupIsOpen_AndItNamesTheTopBar()
	{
		Navigation.NavigateTo("feedback/action-bar");

		IRenderedComponent<MokaAppShell> cut = Shell();

		Assert.Equal("feedback/action-bar", Assert.Single(cut.FindAll("[aria-current=page]")).GetAttribute("href"));
		Assert.Equal("true", cut.Find("button.moka-menu-item__toggle").GetAttribute("aria-expanded"));
		Assert.Equal("Action bar", cut.Find(".moka-app-shell__bar-title").TextContent);
		Assert.True(ParentLink(cut).HasAttribute(Cancelled));
	}

	[Fact]
	public void OnTheParentsPage_TheParentIsCurrent_AndItsGroupIsOpen()
	{
		Navigation.NavigateTo("feedback");

		IRenderedComponent<MokaAppShell> cut = Shell();

		IElement current = Assert.Single(cut.FindAll("[aria-current=page]"));
		Assert.Contains("moka-menu-item--group-link", current.ClassList);
		Assert.Equal("feedback", current.GetAttribute("href"));
		Assert.Equal("true", cut.Find("button.moka-menu-item__toggle").GetAttribute("aria-expanded"));
		Assert.Equal("Feedback", cut.Find(".moka-app-shell__bar-title").TextContent);
	}

	// Closing the list isn't a pick, so the phone's drawer stays out. Following the link is one, and closes it.
	[Fact]
	public async Task ClosingTheListFromItsName_LeavesTheDrawerOut_AndFollowingTheLinkClosesIt()
	{
		Navigation.NavigateTo("feedback/action-bar");
		IRenderedComponent<MokaAppShell> cut = Shell();
		await cut.Find(".moka-app-shell__topbar button").ClickAsync(new MouseEventArgs());

		await ParentLink(cut).ClickAsync(new MouseEventArgs { Detail = 1 });

		Assert.Equal("false", cut.Find("button.moka-menu-item__toggle").GetAttribute("aria-expanded"));
		Assert.Equal("true", cut.Find(".moka-app-shell__topbar button").GetAttribute("aria-expanded"));
		Assert.False(ParentLink(cut).HasAttribute(Cancelled));

		await ParentLink(cut).ClickAsync(new MouseEventArgs { Detail = 1 });

		Assert.Equal("true", cut.Find("button.moka-menu-item__toggle").GetAttribute("aria-expanded"));
		Assert.Equal("false", cut.Find(".moka-app-shell__topbar button").GetAttribute("aria-expanded"));
	}

	[Fact]
	public void CollapseOnRepeatClick_IsOnByDefault_AndReachesTheMenu()
	{
		Navigation.NavigateTo("feedback");

		Assert.True(Shell().FindComponent<MokaMenu>().Instance.CollapseOnRepeatClick);

		IRenderedComponent<MokaAppShell> off = Shell(p => p.Add(x => x.CollapseOnRepeatClick, false));

		Assert.False(off.FindComponent<MokaMenu>().Instance.CollapseOnRepeatClick);
		Assert.False(ParentLink(off).HasAttribute(Cancelled));
	}

	[Fact]
	public async Task TheArrowsWords_PassThrough()
	{
		IRenderedComponent<MokaAppShell> cut = Shell(p => p
			.Add(x => x.ShowGroupLabel, label => $"More {label} pages")
			.Add(x => x.HideGroupLabel, label => $"Fewer {label} pages"));
		IconTooltip.AssertNamed(cut.Find("button.moka-menu-item__toggle"), "More Feedback pages", MokaTooltipPosition.Right);

		await cut.Find("button.moka-menu-item__toggle").ClickAsync(new MouseEventArgs());

		IconTooltip.AssertNamed(cut.Find("button.moka-menu-item__toggle"), "Fewer Feedback pages", MokaTooltipPosition.Right);
	}

	[Fact]
	public void ChildrenCanHoldChildren()
	{
		MokaNavEntry[] entries =
		[
			new("Components", "Feedback", "/feedback", MokaIcons.Status.Bell)
			{
				Children =
				[
					new(string.Empty, "Menus", "/feedback/menus", MokaIcons.Navigation.Menu)
					{
						Children = [new(string.Empty, "Radial", "/feedback/menus/radial", MokaIcons.Content.PieChart)]
					}
				]
			}
		];
		Navigation.NavigateTo("feedback/menus/radial");

		IRenderedComponent<MokaAppShell> cut = Shell(entries: entries);

		Assert.Equal(2, cut.FindAll("a.moka-menu-item--group-link").Count);
		Assert.All(cut.FindAll("button.moka-menu-item__toggle"), toggle => Assert.Equal("true", toggle.GetAttribute("aria-expanded")));
		Assert.Equal("Radial", cut.Find(".moka-app-shell__bar-title").TextContent);
	}

	// On the rail the entry's icon opens its children beside it, under a link to its own page.
	[Fact]
	public async Task OnTheRail_TheEntrysIconOpensItsChildren_UnderALinkToItsPage()
	{
		_menuModule.Setup<bool>("bindRail", _ => true).SetResult(true);
		Navigation.NavigateTo("feedback/alert");
		IRenderedComponent<MokaAppShell> cut = Shell();

		IElement parent = cut.Find("button.moka-menu-item--has-children");
		Assert.Empty(cut.FindAll("a.moka-menu-item--group-link"));

		await parent.ClickAsync(new MouseEventArgs());

		IElement flyout = cut.Find(".moka-menu-item__flyout");
		IElement head = flyout.QuerySelector("a.moka-menu-item__flyout-link")!;
		Assert.Equal("feedback", head.GetAttribute("href"));
		Assert.Null(head.GetAttribute("aria-current"));
		Assert.Equal("page", flyout.QuerySelector("a[href='feedback/alert']")?.GetAttribute("aria-current"));
	}

	private static IElement ParentLink(IRenderedComponent<MokaAppShell> cut) => cut.Find("a.moka-menu-item--group-link");

	private IRenderedComponent<MokaAppShell> Shell(
		Action<ComponentParameterCollectionBuilder<MokaAppShell>>? parameters = null,
		IReadOnlyList<MokaNavEntry>? entries = null)
	{
		SetRendererInfo(new RendererInfo("Server", true));
		return Render<MokaAppShell>(p =>
		{
			p.Add(x => x.Entries, entries ?? Entries);
			parameters?.Invoke(p);
		});
	}
}
