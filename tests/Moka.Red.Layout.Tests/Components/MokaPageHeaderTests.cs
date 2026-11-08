using AngleSharp.Dom;
using Bunit;
using Moka.Red.Layout.PageHeader;
using Moka.Red.Tests.Shared;

namespace Moka.Red.Layout.Tests.Components;

public class MokaPageHeaderTests : BunitContext
{
	public MokaPageHeaderTests() => JSInterop.Mode = JSRuntimeMode.Loose;

	[Fact]
	public void TheTitle_IsThePagesHeading()
	{
		IRenderedComponent<MokaPageHeader> cut = Render<MokaPageHeader>(p => p.Add(x => x.Title, "Devices"));

		Assert.Equal("Devices", cut.Find("header.moka-page-header h1.moka-page-header__title").TextContent);
	}

	[Fact]
	public void TitleContent_ReplacesTheTitle()
	{
		IRenderedComponent<MokaPageHeader> cut = Render<MokaPageHeader>(p => p
			.Add(x => x.Title, "Devices")
			.Add(x => x.TitleContent, "<h1>Mine</h1><button>Edit</button>"));

		Assert.Empty(cut.FindAll(".moka-page-header__title"));
		Assert.Equal("Mine", cut.Find(".moka-page-header__title-row h1").TextContent);
		Assert.NotNull(cut.Find(".moka-page-header__title-row button"));
	}

	[Fact]
	public void WhatIsSoNow_AlwaysShows_AndWhatItIsFor_CanBeHidden()
	{
		IRenderedComponent<MokaPageHeader> cut = Render<MokaPageHeader>(p => p
			.Add(x => x.Said, "14 known")
			.Add(x => x.Lead, "What the page is for"));

		IReadOnlyList<IElement> leads = cut.FindAll(".moka-page-header__lead");
		Assert.Equal("14 known", leads[0].TextContent);
		Assert.DoesNotContain("moka-about", leads[0].ClassName, StringComparison.Ordinal);
		Assert.Equal("What the page is for", leads[1].TextContent);
		Assert.Contains("moka-about", leads[1].ClassName, StringComparison.Ordinal);
	}

	[Fact]
	public void LeadContent_ReplacesTheLead()
	{
		IRenderedComponent<MokaPageHeader> cut = Render<MokaPageHeader>(p => p
			.Add(x => x.Lead, "plain")
			.Add(x => x.LeadContent, "<a href=\"/more\">More</a>"));

		IElement lead = cut.Find(".moka-page-header__lead.moka-about");
		Assert.Equal("More", lead.TextContent);
	}

	[Fact]
	public void AnEyebrowWithoutAnAddress_IsPlainText()
	{
		IRenderedComponent<MokaPageHeader> cut = Render<MokaPageHeader>(p => p.Add(x => x.Eyebrow, "Network"));

		Assert.Equal("SPAN", cut.Find(".moka-page-header__eyebrow").TagName);
	}

	[Fact]
	public void AnEyebrowWithAnAddress_IsAWayBack()
	{
		IRenderedComponent<MokaPageHeader> cut = Render<MokaPageHeader>(p => p
			.Add(x => x.Eyebrow, "Devices")
			.Add(x => x.EyebrowHref, "/devices"));

		IElement link = cut.Find("a.moka-page-header__eyebrow--back");
		Assert.Equal("/devices", link.GetAttribute("href"));
		Assert.Contains("Devices", link.TextContent, StringComparison.Ordinal);
		Assert.NotNull(link.QuerySelector("svg"));
	}

	[Theory]
	[InlineData("javascript:alert(1)")]
	[InlineData("data:text/html,<script>alert(1)</script>")]
	public void AnEyebrowWithAScriptAddress_IsPlainText(string href)
	{
		IRenderedComponent<MokaPageHeader> cut = Render<MokaPageHeader>(p => p
			.Add(x => x.Eyebrow, "Devices")
			.Add(x => x.EyebrowHref, href));

		Assert.Empty(cut.FindAll("a"));
		Assert.Equal("Devices", cut.Find(".moka-page-header__eyebrow").TextContent);
	}

	[Fact]
	public void EmptyParts_RenderNothing()
	{
		IRenderedComponent<MokaPageHeader> cut = Render<MokaPageHeader>(p => p.Add(x => x.Title, "Devices"));

		Assert.Empty(cut.FindAll(".moka-page-header__media"));
		Assert.Empty(cut.FindAll(".moka-page-header__eyebrow"));
		Assert.Empty(cut.FindAll(".moka-page-header__lead"));
		Assert.Empty(cut.FindAll(".moka-page-header__actions"));
	}

	[Fact]
	public void TheSlots_LandWhereTheyBelong()
	{
		IRenderedComponent<MokaPageHeader> cut = Render<MokaPageHeader>(p => p
			.Add(x => x.Media, "<img alt=\"\"/>")
			.Add(x => x.Actions, "<button>Add</button>")
			.AddChildContent("<span class=\"pill\">Allowed</span>"));

		Assert.NotNull(cut.Find(".moka-page-header__media img"));
		Assert.NotNull(cut.Find(".moka-page-header__actions button"));
		Assert.NotNull(cut.Find(".moka-page-header__titles .pill"));
	}

	[Fact]
	public void TitleInTopBar_KeepsTheHeadingInTheDocument()
	{
		IRenderedComponent<MokaPageHeader> cut = Render<MokaPageHeader>(p => p
			.Add(x => x.Title, "Devices")
			.Add(x => x.TitleInTopBar, true));

		Assert.Contains("moka-page-header--title-in-top-bar", cut.Find("header").ClassName, StringComparison.Ordinal);
		Assert.Equal("Devices", cut.Find("h1").TextContent);
	}

	[Fact]
	public void ClassAndAttributes_LandOnTheRoot()
	{
		IRenderedComponent<MokaPageHeader> cut = Render<MokaPageHeader>(p => p
			.Add(x => x.Class, "mine")
			.Add(x => x.Id, "top")
			.AddUnmatched("data-page", "devices"));

		IElement root = cut.Find("header");
		Assert.Contains("mine", root.ClassName, StringComparison.Ordinal);
		Assert.Equal("top", root.Id);
		Assert.Equal("devices", root.GetAttribute("data-page"));
	}

	// A phone has no room to spare. The rules live in the stylesheet, so read them there.
	[Fact]
	public void OnAPhone_ThePlainEyebrowHides_AndTheActionsTakeTheWidth()
	{
		const string File = "MokaPageHeader.razor.css";

		Assert.Equal("none", ScopedCss.Declarations(File, ".moka-page-header__eyebrow:not(.moka-page-header__eyebrow--back)")["display"]);
		Assert.Equal("1 1 100%", ScopedCss.Declarations(File, ".moka-page-header__actions")["flex"]);
		Assert.Equal("inset(50%)",
			ScopedCss.Declarations(File, ".moka-page-header--title-in-top-bar > .moka-page-header__titles > .moka-page-header__title")["clip-path"]);
	}

	// A window-width query never adapted a header in a narrow panel, and stacked one in a small desktop window.
	[Fact]
	public void TheHeaderStacks_ByItsOwnWidth_AndOnlyTheTopBarFollowsTheWindow()
	{
		const string File = "MokaPageHeader.razor.css";
		string css = System.IO.File.ReadAllText(System.IO.Path.Combine(AppContext.BaseDirectory, "Css", File));

		Assert.Equal("moka-page-header / inline-size", ScopedCss.Declarations(File, ".moka-page-header")["container"]);
		Assert.Contains("@container moka-page-header (max-width: 30rem)", css, StringComparison.Ordinal);
		int media = css.IndexOf("@media (max-width: 720px)", StringComparison.Ordinal);
		Assert.True(media > css.IndexOf("@container", StringComparison.Ordinal));
		Assert.Equal(media, css.LastIndexOf("@media", StringComparison.Ordinal));
		Assert.True(css.IndexOf(".moka-page-header--title-in-top-bar >", StringComparison.Ordinal) > media);
	}

	// A size container takes no width from its content, so it has to ask for the width it's offered.
	[Fact]
	public void TheHeader_FillsItsContainer()
	{
		IRenderedComponent<MokaPageHeader> cut = Render<MokaPageHeader>(p => p.Add(x => x.Title, "Devices"));

		Assert.Contains("moka-fill-width", cut.Find("header").ClassList);
	}
}
