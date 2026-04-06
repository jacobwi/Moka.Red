using AngleSharp.Dom;
using Bunit;
using Moka.Red.Navigation.Breadcrumb;

namespace Moka.Red.Navigation.Tests.Components;

public class MokaBreadcrumbTests : BunitContext
{
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
		Assert.Contains("custom-bc", nav.ClassName, StringComparison.Ordinal);
	}
}
