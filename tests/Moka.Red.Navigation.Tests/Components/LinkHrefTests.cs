using Bunit;
using Moka.Red.Navigation.Breadcrumb;
using Moka.Red.Navigation.Menu;

namespace Moka.Red.Navigation.Tests.Components;

// Breadcrumb and menu links took any Href, and a javascript: URL ran script when it was followed.
public class LinkHrefTests : BunitContext
{
	[Theory]
	[InlineData("javascript:alert(document.domain)")]
	[InlineData(" \tJAVASCRIPT:alert(1)")]
	[InlineData("vbscript:msgbox(1)")]
	[InlineData("data:text/html,<script>alert(1)</script>")]
	public void ABreadcrumbWithAScriptUrl_IsText(string href)
	{
		IRenderedComponent<MokaBreadcrumb> cut = Render<MokaBreadcrumb>(p => p
			.AddChildContent<MokaBreadcrumbItem>(item => item
				.Add(x => x.Text, "Home")
				.Add(x => x.Href, href))
			.AddChildContent<MokaBreadcrumbItem>(item => item.Add(x => x.Text, "Settings")));

		Assert.Empty(cut.FindAll("a"));
		Assert.Empty(cut.FindAll("[href]"));
		Assert.Equal("Home", cut.FindAll(".moka-breadcrumb-item__text")[0].TextContent.Trim());
	}

	[Fact]
	public void ABreadcrumbWithAWebUrl_IsALink()
	{
		IRenderedComponent<MokaBreadcrumb> cut = Render<MokaBreadcrumb>(p => p
			.AddChildContent<MokaBreadcrumbItem>(item => item
				.Add(x => x.Text, "Home")
				.Add(x => x.Href, "/"))
			.AddChildContent<MokaBreadcrumbItem>(item => item.Add(x => x.Text, "Settings")));

		Assert.Equal("/", cut.Find("a.moka-breadcrumb-item__link").GetAttribute("href"));
	}

	[Theory]
	[InlineData("javascript:alert(document.domain)")]
	[InlineData("java\r\nscript:alert(1)")]
	[InlineData("data:text/html,<script>alert(1)</script>")]
	public void AMenuItemWithAScriptUrl_IsNotALink(string href)
	{
		IRenderedComponent<MokaMenu> cut = Render<MokaMenu>(p => p
			.AddChildContent<MokaMenuItem>(item => item
				.Add(x => x.Text, "Reports")
				.Add(x => x.Href, href)));

		Assert.Empty(cut.FindAll("a"));
		Assert.Empty(cut.FindAll("[href]"));
	}

	[Fact]
	public void AMenuItemWithAWebUrl_IsALink()
	{
		IRenderedComponent<MokaMenu> cut = Render<MokaMenu>(p => p
			.AddChildContent<MokaMenuItem>(item => item
				.Add(x => x.Text, "Reports")
				.Add(x => x.Href, "/reports")));

		Assert.Equal("/reports", cut.Find("a.moka-menu-item").GetAttribute("href"));
	}
}
