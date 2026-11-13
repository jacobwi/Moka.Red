using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Moka.Red.Navigation.Breadcrumb;
using Moka.Red.Tests.Shared;

namespace Moka.Red.Navigation.Tests.Components;

// A path's well kept its end in view by scrolling, so a lone level wider than the well lost its first letters:
// "isbon trip" for "2026 Lisbon trip". The last level is now capped at the well's width and ends in an ellipsis, named
// by a tooltip, while earlier levels still scroll out behind the fade.
public class MokaBreadcrumbLastLevelTests : BunitContext
{
	private const string Css = "MokaBreadcrumb.razor.css";
	private const string List = ".moka-breadcrumb__scroll > .moka-breadcrumb__list";

	public MokaBreadcrumbLastLevelTests() => JSInterop.Mode = JSRuntimeMode.Loose;

	[Fact]
	public void APath_NamesItsCurrentLevelInATooltip()
	{
		IRenderedComponent<MokaBreadcrumb> cut = Path();

		IElement current = cut.Find("li.moka-breadcrumb-item--active");
		IElement trigger = current.QuerySelector(":scope > .moka-tooltip-trigger")!;
		Assert.NotNull(trigger);
		Assert.Equal("2026 Lisbon trip", trigger.QuerySelector("[role=tooltip]")?.TextContent.Trim());
		Assert.Equal("page", trigger.QuerySelector(".moka-breadcrumb-item__text")?.GetAttribute("aria-current"));
	}

	[Fact]
	public void APath_LeavesItsEarlierLevelsWithoutATooltip()
	{
		IRenderedComponent<MokaBreadcrumb> cut = Path();

		IElement first = cut.FindAll("li.moka-breadcrumb-item")[0];
		Assert.Null(first.QuerySelector(".moka-tooltip-trigger"));
	}

	[Fact]
	public void AWrappingTrail_KeepsItsCurrentPageAsItWas()
	{
		IRenderedComponent<MokaBreadcrumb> cut = Render(p => p.Add(x => x.Look, MokaBreadcrumbLook.Trail));

		Assert.Null(cut.Find("li.moka-breadcrumb-item--active").QuerySelector(".moka-tooltip-trigger"));
	}

	[Fact]
	public void AOneLineTrail_NamesItsCurrentPageInATooltip()
	{
		IRenderedComponent<MokaBreadcrumb> cut = Render(p => p.Add(x => x.Look, MokaBreadcrumbLook.Trail).Add(x => x.Wrap, false));

		Assert.NotNull(cut.Find("li.moka-breadcrumb-item--active").QuerySelector(":scope > .moka-tooltip-trigger"));
	}

	// The list takes the well's width, so a percentage on the last level means the well. Levels that don't fit
	// overflow to the left, where the right-to-left well scrolls; the last level's auto margin keeps a fitting path at
	// the start.
	[Fact]
	public void TheList_IsAsWideAsTheWell_AndOverflowsToTheLeft()
	{
		IReadOnlyDictionary<string, string> list = ScopedCss.Declarations(Css, List);

		Assert.Equal("flex-end", list["justify-content"]);
		Assert.False(list.ContainsKey("width"));
		Assert.Equal("rtl", ScopedCss.Declarations(Css, ".moka-breadcrumb__scroll")["direction"]);
		Assert.Equal("0", ScopedCss.Declarations(Css, $"{List} ::deep > li")["flex-shrink"]);
	}

	[Fact]
	public void TheLastLevel_IsCappedAtTheWell_AndTakesTheSpareRoom()
	{
		IReadOnlyDictionary<string, string> last = ScopedCss.Declarations(Css, $"{List} ::deep > li:last-child");

		Assert.Equal("100%", last["max-width"]);
		Assert.Equal("auto", last["margin-inline-end"]);
	}

	[Fact]
	public void TheLastLevelsName_EndsInAnEllipsis()
	{
		IReadOnlyDictionary<string, string> label = ScopedCss.Declarations(Css, $"{List} ::deep > li:last-child .moka-breadcrumb-item__label");

		Assert.Equal("ellipsis", label["text-overflow"]);
		Assert.Equal("hidden", label["overflow"]);
		Assert.Equal("0", label["min-width"]);
	}

	// The tooltip's trigger and the level's own box sit between the item and its name, so both may shrink.
	[Fact]
	public void EveryBoxAroundTheName_MayShrink()
	{
		string css = File.ReadAllText(System.IO.Path.Combine(AppContext.BaseDirectory, "Css", Css));
		int rule = css.IndexOf($"{List} ::deep > li:last-child :is(.moka-tooltip-trigger, .moka-breadcrumb-item__link, .moka-breadcrumb-item__text) {{", StringComparison.Ordinal);

		Assert.True(rule >= 0);
		string body = css[rule..css.IndexOf('}', rule)];
		Assert.Contains("min-width: 0;", body, StringComparison.Ordinal);
		Assert.Contains("max-width: 100%;", body, StringComparison.Ordinal);
	}

	private IRenderedComponent<MokaBreadcrumb> Path() => Render(p => p.Add(x => x.Look, MokaBreadcrumbLook.Path));

	private IRenderedComponent<MokaBreadcrumb> Render(Action<ComponentParameterCollectionBuilder<MokaBreadcrumb>> extra) =>
		Render<MokaBreadcrumb>(p =>
		{
			extra(p);
			p.AddChildContent<MokaBreadcrumbItem>(item => item.Add(x => x.Text, "Trips").Add(x => x.Href, "/trips"))
				.AddChildContent<MokaBreadcrumbItem>(item => item.Add(x => x.Text, "2026 Lisbon trip"));
		});
}
