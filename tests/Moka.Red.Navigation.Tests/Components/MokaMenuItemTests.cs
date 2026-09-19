using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components.Web;
using Moka.Red.Icons;
using Moka.Red.Navigation.Menu;

namespace Moka.Red.Navigation.Tests.Components;

// MokaMenu is site navigation: links, buttons and disclosure buttons that Tab reaches, rather than
// the ARIA menu pattern. Items without a link used to be divs with role="menuitem" outside any
// menu, which the keyboard could not reach.
public class MokaMenuItemTests : BunitContext
{
	[Fact]
	public void AParent_IsADisclosureButton()
	{
		IRenderedComponent<MokaMenu> cut = RenderMenu(item => item
			.Add(x => x.Text, "Users")
			.AddChildContent<MokaMenuItem>(child => child
				.Add(x => x.Text, "All users")
				.Add(x => x.Href, "/users")));

		IElement button = cut.Find("button.moka-menu-item");
		Assert.Equal("button", button.GetAttribute("type"));
		Assert.Equal("false", button.GetAttribute("aria-expanded"));

		IElement group = cut.Find(".moka-menu-item__submenu");
		Assert.Equal(group.Id, button.GetAttribute("aria-controls"));
		Assert.Empty(cut.FindAll("[role=menuitem]"));
	}

	// A collapsed group is only clipped to zero height, so its links stayed in the tab order.
	[Fact]
	public async Task ACollapsedGroup_KeepsItsLinksOutOfReach()
	{
		bool? expanded = null;
		IRenderedComponent<MokaMenu> cut = RenderMenu(item => item
			.Add(x => x.Text, "Users")
			.Add(x => x.ExpandedChanged, value => expanded = value)
			.AddChildContent<MokaMenuItem>(child => child
				.Add(x => x.Text, "All users")
				.Add(x => x.Href, "/users")));

		Assert.True(cut.Find(".moka-menu-item__submenu").HasAttribute("inert"));

		await cut.Find("button.moka-menu-item").ClickAsync(new MouseEventArgs());

		Assert.True(expanded);
		Assert.Equal("true", cut.Find("button.moka-menu-item").GetAttribute("aria-expanded"));
		Assert.False(cut.Find(".moka-menu-item__submenu").HasAttribute("inert"));
	}

	[Fact]
	public async Task AnItemWithOnClick_IsAButton()
	{
		bool clicked = false;
		IRenderedComponent<MokaMenu> cut = RenderMenu(item => item
			.Add(x => x.Text, "Sign out")
			.Add(x => x.OnClick, _ => clicked = true));

		IElement button = cut.Find("button.moka-menu-item");
		Assert.Equal("button", button.GetAttribute("type"));
		Assert.False(button.HasAttribute("role"));

		await button.ClickAsync(new MouseEventArgs());

		Assert.True(clicked);
	}

	// The context-menu key fires on the focused element, so the item has to take focus.
	[Fact]
	public void AnItemWithOnlyAContextMenu_IsAButton()
	{
		IRenderedComponent<MokaMenu> cut = RenderMenu(item => item
			.Add(x => x.Text, "Drafts")
			.Add(x => x.OnContextMenu, _ => { }));

		Assert.NotNull(cut.Find("button.moka-menu-item"));
	}

	[Fact]
	public void AnItemThatDoesNothing_TakesNoFocus()
	{
		IRenderedComponent<MokaMenu> cut = RenderMenu(item => item.Add(x => x.Text, "Label"));

		IElement element = cut.Find(".moka-menu-item");
		Assert.Equal("DIV", element.TagName);
		Assert.False(element.HasAttribute("role"));
		Assert.False(element.HasAttribute("tabindex"));
	}

	[Fact]
	public void TheActiveLink_IsTheCurrentPage()
	{
		IRenderedComponent<MokaMenu> cut = Render<MokaMenu>(p => p
			.AddChildContent<MokaMenuItem>(item => item
				.Add(x => x.Text, "Home")
				.Add(x => x.Href, "/")
				.Add(x => x.Active, true))
			.AddChildContent<MokaMenuItem>(item => item
				.Add(x => x.Text, "Settings")
				.Add(x => x.Href, "/settings")));

		IReadOnlyList<IElement> links = cut.FindAll("a.moka-menu-item");
		Assert.Equal("page", links[0].GetAttribute("aria-current"));
		Assert.False(links[1].HasAttribute("aria-current"));
	}

	// The collapsed menu drops the text, which left icon-only buttons with no name.
	[Fact]
	public void ACollapsedMenu_NamesItsIconOnlyItems()
	{
		IRenderedComponent<MokaMenu> cut = Render<MokaMenu>(p => p
			.Add(x => x.Collapsed, true)
			.AddChildContent<MokaMenuItem>(item => item
				.Add(x => x.Text, "Settings")
				.Add(x => x.Icon, MokaIcons.Action.Settings)
				.Add(x => x.OnClick, _ => { })));

		IElement button = cut.Find("button.moka-menu-item");
		Assert.Empty(button.QuerySelectorAll(".moka-menu-item__text"));
		Assert.Equal("Settings", button.GetAttribute("aria-label"));
	}

	// Blazor passes every parameter again when the parent re-renders, so writing the toggle into
	// the Expanded parameter let the parent's one-way value undo it.
	[Fact]
	public async Task AUserToggle_SurvivesAParentRerender()
	{
		IRenderedComponent<MokaMenuItem> cut = Render<MokaMenuItem>(p => p
			.Add(x => x.Text, "Users")
			.Add(x => x.Expanded, true)
			.AddChildContent<MokaMenuItem>(child => child
				.Add(x => x.Text, "All users")
				.Add(x => x.Href, "/users")));

		await cut.Find(".moka-menu-item--has-children").ClickAsync(new MouseEventArgs());
		cut.Render(p => p.Add(x => x.Expanded, true));

		Assert.False(cut.Find(".moka-menu-item__submenu").ClassList.Contains("moka-menu-item__submenu--open"));
	}

	[Fact]
	public void ANewExpandedValue_FromTheParent_StillApplies()
	{
		IRenderedComponent<MokaMenuItem> cut = Render<MokaMenuItem>(p => p
			.Add(x => x.Text, "Users")
			.AddChildContent<MokaMenuItem>(child => child.Add(x => x.Text, "All users")));

		cut.Render(p => p.Add(x => x.Expanded, true));

		Assert.True(cut.Find(".moka-menu-item__submenu").ClassList.Contains("moka-menu-item__submenu--open"));
	}

	// Disabled is inherited from the base, and the item ignored it: the link still navigated and the
	// button still ran its OnClick.
	[Fact]
	public async Task ADisabledItem_CannotBeUsed()
	{
		int clicks = 0;
		IRenderedComponent<MokaMenu> cut = RenderMenu(item => item
			.Add(x => x.Text, "Delete")
			.Add(x => x.Disabled, true)
			.Add(x => x.OnClick, _ => clicks++));

		IElement button = cut.Find("button.moka-menu-item");
		Assert.True(button.HasAttribute("disabled"));
		Assert.Contains("moka-menu-item--disabled", button.ClassList);

		await button.ClickAsync(new MouseEventArgs());
		Assert.Equal(0, clicks);
	}

	[Fact]
	public void ADisabledLink_LosesItsHref_AndSaysItIsDisabled()
	{
		IRenderedComponent<MokaMenu> cut = RenderMenu(item => item
			.Add(x => x.Text, "Reports")
			.Add(x => x.Href, "/reports")
			.Add(x => x.Disabled, true));

		IElement link = cut.Find("a.moka-menu-item");
		Assert.False(link.HasAttribute("href"));
		Assert.Equal("link", link.GetAttribute("role"));
		Assert.Equal("true", link.GetAttribute("aria-disabled"));
	}

	private IRenderedComponent<MokaMenu> RenderMenu(
		Action<ComponentParameterCollectionBuilder<MokaMenuItem>> item) =>
		Render<MokaMenu>(p => p.AddChildContent<MokaMenuItem>(item));
}
