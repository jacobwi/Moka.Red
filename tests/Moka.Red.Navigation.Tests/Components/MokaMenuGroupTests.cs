using System.Reflection;
using AngleSharp.Dom;
using Bunit;
using Bunit.TestDoubles;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Moka.Red.Navigation.Menu;
using Moka.Red.Tests.Shared;

namespace Moka.Red.Navigation.Tests.Components;

// A group opened from max-height: 0 to 500px, so a long one was cut off with its last links still in the tab order, and
// a deep link to a page inside a group left the group closed, hiding the current page.
public class MokaMenuGroupTests : BunitContext
{
	private const string Css = "MokaMenu.razor.css";

	private BunitNavigationManager Navigation => Services.GetRequiredService<BunitNavigationManager>();

	[Fact]
	public void ALongGroup_RendersEveryLink_InsideTheClipTheGridRowShrinks()
	{
		IRenderedComponent<MokaMenu> cut = Render<MokaMenu>(p => p.AddChildContent<MokaMenuItem>(group =>
		{
			group.Add(x => x.Text, "Reports").Add(x => x.Expanded, true);
			for (int i = 0; i < 40; i++)
			{
				int n = i;
				group.AddChildContent<MokaMenuItem>(child => child.Add(x => x.Text, $"Report {n}").Add(x => x.Href, $"reports/{n}"));
			}
		}));

		IElement group = cut.Find(".moka-menu-item__submenu");
		IElement clip = Assert.Single(group.Children);
		Assert.Contains("moka-menu-item__submenu-clip", clip.ClassList);
		Assert.Equal(40, clip.QuerySelectorAll("a.moka-menu-item").Length);
		Assert.Contains("moka-menu-item__submenu--open", group.ClassList);
		Assert.False(group.HasAttribute("inert"));
	}

	[Fact]
	public void AGroup_OpensToItsOwnHeight_ThroughAGridRow()
	{
		IReadOnlyDictionary<string, string> closed = ScopedCss.Declarations(Css, "::deep .moka-menu-item__submenu");
		IReadOnlyDictionary<string, string> open = ScopedCss.Declarations(Css, "::deep .moka-menu-item__submenu--open");
		IReadOnlyDictionary<string, string> clip = ScopedCss.Declarations(Css, "::deep .moka-menu-item__submenu-clip");

		Assert.Equal("grid", closed["display"]);
		Assert.Equal("0fr", closed["grid-template-rows"]);
		Assert.Equal("grid-template-rows var(--moka-transition-normal)", closed["transition"]);
		Assert.Equal("1fr", open["grid-template-rows"]);
		Assert.Equal("0", clip["min-height"]);
		Assert.Equal("hidden", clip["overflow"]);
	}

	[Fact]
	public void NoRule_CapsAGroupsHeight()
	{
		foreach (string selector in new[] { "::deep .moka-menu-item__submenu", "::deep .moka-menu-item__submenu--open", "::deep .moka-menu-item__submenu-clip" })
		{
			Assert.DoesNotContain("max-height", ScopedCss.Declarations(Css, selector).Keys);
		}
	}

	[Fact]
	public void ADeepLink_OpensTheGroupAroundTheCurrentPage_OnFirstRender()
	{
		Navigation.NavigateTo("reports/monthly");

		IRenderedComponent<MokaMenu> cut = Render<MokaMenu>(p => p.AddChildContent<MokaMenuItem>(group => group
			.Add(x => x.Text, "Reports")
			.AddChildContent<MokaMenuItem>(child => Link(child, "Monthly", "reports/monthly"))));

		AssertOpen(cut, true);
		Assert.Equal("page", cut.Find("a.moka-menu-item").GetAttribute("aria-current"));
	}

	[Fact]
	public void MovingToAPageInsideAGroup_OpensIt()
	{
		Navigation.NavigateTo("home");
		IRenderedComponent<MokaMenu> cut = Render<MokaMenu>(p => p.AddChildContent<MokaMenuItem>(group => group
			.Add(x => x.Text, "Reports")
			.AddChildContent<MokaMenuItem>(child => Link(child, "Monthly", "reports/monthly"))));
		AssertOpen(cut, false);

		Navigation.NavigateTo("reports/monthly");

		cut.WaitForAssertion(() => AssertOpen(cut, true));
	}

	[Fact]
	public void EveryGroupAroundTheCurrentPage_Opens()
	{
		Navigation.NavigateTo("reports/2024/q1");

		IRenderedComponent<MokaMenu> cut = Render<MokaMenu>(p => p.AddChildContent<MokaMenuItem>(outer => outer
			.Add(x => x.Text, "Reports")
			.AddChildContent<MokaMenuItem>(inner => inner
				.Add(x => x.Text, "2024")
				.AddChildContent<MokaMenuItem>(child => Link(child, "First quarter", "reports/2024/q1")))));

		IReadOnlyList<IElement> buttons = cut.FindAll("button.moka-menu-item");
		Assert.All(buttons, button => Assert.Equal("true", button.GetAttribute("aria-expanded")));
		Assert.All(cut.FindAll(".moka-menu-item__submenu"), group => Assert.False(group.HasAttribute("inert")));
	}

	[Fact]
	public void AnActiveItem_OpensItsGroup_WithoutListeningForPages()
	{
		int before = Listeners();

		IRenderedComponent<MokaMenu> cut = Render<MokaMenu>(p => p.AddChildContent<MokaMenuItem>(group => group
			.Add(x => x.Text, "Reports")
			.AddChildContent<MokaMenuItem>(child => child.Add(x => x.Text, "Monthly").Add(x => x.Href, "reports/monthly").Add(x => x.Active, true))));

		AssertOpen(cut, true);
		Assert.Equal(before, Listeners());
	}

	[Fact]
	public void AnItemTurningActive_OpensItsGroup()
	{
		IRenderedComponent<MokaMenuItem> cut = Render<MokaMenuItem>(p => p
			.Add(x => x.Text, "Reports")
			.AddChildContent<MokaMenuItem>(child => child.Add(x => x.Text, "Monthly").Add(x => x.Href, "reports/monthly")));
		Assert.Equal("false", cut.Find("button").GetAttribute("aria-expanded"));

		cut.FindComponent<MokaMenuItem>().Render(p => p.Add(x => x.Active, true));

		Assert.Equal("true", cut.Find("button").GetAttribute("aria-expanded"));
	}

	// Items with neither Match nor Active never light, so they add nothing: no listener, and no group opens.
	[Fact]
	public void ItemsThatNeverLight_AddNoListener_AndOpenNothing()
	{
		int before = Listeners();

		IRenderedComponent<MokaMenu> cut = Render<MokaMenu>(p => p.AddChildContent<MokaMenuItem>(group => group
			.Add(x => x.Text, "Reports")
			.AddChildContent<MokaMenuItem>(child => child.Add(x => x.Text, "Monthly").Add(x => x.Href, "reports/monthly"))
			.AddChildContent<MokaMenuItem>(child => child.Add(x => x.Text, "Yearly").Add(x => x.OnClick, _ => { }))));

		Assert.Equal(before, Listeners());
		AssertOpen(cut, false);
	}

	[Fact]
	public void OpeningForTheCurrentPage_TellsTheBindingOnce()
	{
		Navigation.NavigateTo("reports/monthly");
		List<bool> told = [];

		IRenderedComponent<MokaMenuItem> cut = Render<MokaMenuItem>(p => p
			.Add(x => x.Text, "Reports")
			.Add(x => x.Expanded, false)
			.Add(x => x.ExpandedChanged, value => told.Add(value))
			.AddChildContent<MokaMenuItem>(child => Link(child, "Monthly", "reports/monthly")));

		Assert.Equal([true], told);

		// What @bind-Expanded passes back, and a page change that keeps the same page lit, open nothing again.
		cut.Render(p => p.Add(x => x.Expanded, true));
		Navigation.NavigateTo("reports/monthly?tab=2");
		Assert.Equal([true], told);
		Assert.Equal("true", cut.Find("button").GetAttribute("aria-expanded"));
	}

	[Fact]
	public void AGroupAlreadyOpen_TellsNothing()
	{
		Navigation.NavigateTo("reports/monthly");
		List<bool> told = [];

		Render<MokaMenuItem>(p => p
			.Add(x => x.Text, "Reports")
			.Add(x => x.Expanded, true)
			.Add(x => x.ExpandedChanged, value => told.Add(value))
			.AddChildContent<MokaMenuItem>(child => Link(child, "Monthly", "reports/monthly")));

		Assert.Empty(told);
	}

	// The person's choice wins: a group they closed stays closed while they stay on its page, and opens again when they
	// come back to it. A group they opened is never closed.
	[Fact]
	public async Task AGroupThePersonClosed_StaysClosed_UntilThePageLightsAgain()
	{
		Navigation.NavigateTo("reports/monthly");
		IRenderedComponent<MokaMenu> cut = Render<MokaMenu>(p => p
			.AddChildContent<MokaMenuItem>(group => group
				.Add(x => x.Text, "Reports")
				.AddChildContent<MokaMenuItem>(child => Link(child, "Monthly", "reports/monthly")))
			.AddChildContent<MokaMenuItem>(group => group
				.Add(x => x.Text, "Admin")
				.AddChildContent<MokaMenuItem>(child => Link(child, "Users", "admin/users"))));

		await cut.FindAll("button.moka-menu-item")[1].ClickAsync(new MouseEventArgs());
		await cut.FindAll("button.moka-menu-item")[0].ClickAsync(new MouseEventArgs());
		cut.Render();
		Assert.Equal("false", cut.FindAll("button.moka-menu-item")[0].GetAttribute("aria-expanded"));

		Navigation.NavigateTo("home");
		Navigation.NavigateTo("reports/monthly");

		cut.WaitForAssertion(() =>
		{
			Assert.Equal("true", cut.FindAll("button.moka-menu-item")[0].GetAttribute("aria-expanded"));
			Assert.Equal("true", cut.FindAll("button.moka-menu-item")[1].GetAttribute("aria-expanded"));
		});
	}

	private static void Link(ComponentParameterCollectionBuilder<MokaMenuItem> child, string text, string href) => child
		.Add(x => x.Text, text)
		.Add(x => x.Href, href)
		.Add(x => x.Match, NavLinkMatch.All);

	private static void AssertOpen(IRenderedComponent<MokaMenu> cut, bool open)
	{
		Assert.Equal(open ? "true" : "false", cut.Find("button.moka-menu-item").GetAttribute("aria-expanded"));
		IElement group = cut.Find(".moka-menu-item__submenu");
		Assert.Equal(open, group.ClassList.Contains("moka-menu-item__submenu--open"));
		Assert.Equal(!open, group.HasAttribute("inert"));
	}

	// NavigationManager doesn't say how many handlers it has, so this reads the event's backing field.
	private int Listeners()
	{
		FieldInfo field = typeof(NavigationManager).GetField("_locationChanged", BindingFlags.Instance | BindingFlags.NonPublic)
		                  ?? throw new InvalidOperationException("NavigationManager no longer keeps LocationChanged in _locationChanged.");
		return (field.GetValue(Navigation) as Delegate)?.GetInvocationList().Length ?? 0;
	}
}
