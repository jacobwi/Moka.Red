using System.Text.RegularExpressions;
using AngleSharp.Dom;
using Bunit;
using Bunit.TestDoubles;
using Moka.Red.Icons;
using Moka.Red.Navigation.AppShell;
using Moka.Red.Tests.Shared;

namespace Moka.Red.Navigation.Tests.Components;

// The menu's heading was one plain string, so an app couldn't put the server it's signed in to under the name in
// mono. HeadingDetail adds that line and HeadingContent draws the top; a plain Heading keeps its markup.
public partial class MokaAccountCardHeadingTests : BunitContext
{
	private const string Css = "MokaAccountCard.razor.css";
	private static readonly MokaAccountItem Settings = new("Settings", MokaIcons.Action.Settings, "/settings");

	public MokaAccountCardHeadingTests()
	{
		JSInterop.Mode = JSRuntimeMode.Loose;
		BunitJSModuleInterop keys = JSInterop.SetupModule("./_content/Moka.Red.Core/moka-keys.js");
		keys.Mode = JSRuntimeMode.Loose;
	}

	[Fact]
	public void AHeadingAlone_KeepsItsMarkup()
	{
		IRenderedComponent<MokaAccountCard> cut = Open(p => p.Add(x => x.Heading, "BC, owner"));

		IElement heading = cut.Find(".moka-account-card__heading");
		Assert.Equal("""<span class="moka-account-card__heading" aria-hidden="true">BC, owner</span>""",
			ScopeAttribute().Replace(heading.OuterHtml, ""));
	}

	[Fact]
	public void HeadingDetail_IsASecondLineUnderTheHeading()
	{
		IRenderedComponent<MokaAccountCard> cut = Open(p => p
			.Add(x => x.Heading, "BC, owner")
			.Add(x => x.HeadingDetail, "ark.home.example:8443"));

		IElement heading = cut.Find(".moka-account-card__heading");
		Assert.Contains("moka-account-card__heading--two-lines", heading.ClassList);
		Assert.Equal("true", heading.GetAttribute("aria-hidden"));
		Assert.Equal("BC, owner", heading.QuerySelector(".moka-account-card__heading-text")?.TextContent);
		Assert.Equal("ark.home.example:8443", heading.QuerySelector(".moka-account-card__heading-detail")?.TextContent);
		Assert.Equal(heading.ClassName, cut.Find("[role=menu]").FirstElementChild?.ClassName);
	}

	[Fact]
	public void HeadingDetail_WorksWithoutAHeading()
	{
		IRenderedComponent<MokaAccountCard> cut = Open(p => p.Add(x => x.HeadingDetail, "ark.home.example"));

		Assert.Empty(cut.FindAll(".moka-account-card__heading-text"));
		Assert.Equal("ark.home.example", cut.Find(".moka-account-card__heading-detail").TextContent);
	}

	[Fact]
	public void TheDetail_IsMono()
	{
		IReadOnlyDictionary<string, string> detail = ScopedCss.Declarations(Css, ".moka-account-card__heading-detail");

		Assert.Equal("var(--moka-font-family-mono)", detail["font-family"]);
		Assert.Equal("nowrap", detail["white-space"]);
		Assert.Equal("ellipsis", detail["text-overflow"]);
	}

	[Fact]
	public void HeadingContent_WinsOverBoth_AndStaysHiddenFromScreenReaders()
	{
		IRenderedComponent<MokaAccountCard> cut = Open(p => p
			.Add(x => x.Heading, "BC, owner")
			.Add(x => x.HeadingDetail, "ignored")
			.Add(x => x.HeadingContent, "<b id=\"own\">Signed in</b>"));

		IElement heading = cut.Find(".moka-account-card__heading");
		Assert.Contains("moka-account-card__heading--content", heading.ClassList);
		Assert.Equal("true", heading.GetAttribute("aria-hidden"));
		Assert.NotNull(heading.QuerySelector("#own"));
		Assert.Empty(cut.FindAll(".moka-account-card__heading-detail"));
		Assert.Single(cut.FindAll(".moka-account-card__heading"));
	}

	// The heading isn't a row, so the keys still start on the first row.
	[Fact]
	public void TheRows_AreTheSameWithADetail()
	{
		IRenderedComponent<MokaAccountCard> cut = Open(p => p.Add(x => x.HeadingDetail, "ark.home.example"));

		Assert.Equal(["Settings"], cut.FindAll("[role=menuitem]").Select(row => row.TextContent.Trim()));
	}

	private IRenderedComponent<MokaAccountCard> Open(Action<ComponentParameterCollectionBuilder<MokaAccountCard>> parameters)
	{
		IRenderedComponent<MokaAccountCard> cut = Render<MokaAccountCard>(p =>
		{
			p.Add(x => x.Name, "BC").Add(x => x.Items, [Settings]);
			parameters(p);
		});
		cut.Find(".moka-account-card__card").Click();
		return cut;
	}

	[GeneratedRegex(@" b-\w+(="""")?")]
	private static partial Regex ScopeAttribute();
}
