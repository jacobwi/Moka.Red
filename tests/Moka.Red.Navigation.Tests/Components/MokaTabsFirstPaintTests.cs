using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Moka.Red.Navigation.Tabs;
using Moka.Red.Tests.Shared;

namespace Moka.Red.Navigation.Tests.Components;

// The script measures the row only once it is on screen: a round trip after an interactive render on the server, and not
// until the page goes live after a prerender. Until then the browser paints what the stylesheet says, so that state has
// to show whole tabs only, never one cut off at the row's edge. Measured in a browser: the list sat 20 to 40 ms on
// localhost between going on the page and being fitted, overflowing with its last tab cut off.
public class MokaTabsFirstPaintTests : BunitContext
{
	private const string Css = "MokaTabs.razor.css";
	private const string BeforeTheFit = ".moka-tabs--will-fit:not([data-moka-fitted]) > .moka-tabs__list";
	private const string SegmentedBeforeTheFit = ".moka-tabs--segmented.moka-tabs--will-fit:not([data-moka-fitted]) > .moka-tabs__list";

	private static readonly IReadOnlyList<MokaTabItem> Items =
	[
		new("a", "Alpha"),
		new("b", "Beta"),
		new("c", "Gamma")
	];

	public MokaTabsFirstPaintTests() => JSInterop.Mode = JSRuntimeMode.Loose;

	private IRenderedComponent<MokaTabs> Tabs(MokaTabsOverflow overflow, IComponentRenderMode? renderMode = null) =>
		Render<MokaTabs>(p => p
			.Add(x => x.Items, Items)
			.Add(x => x.Label, "Views")
			.Add(x => x.Active, "a")
			.Add(x => x.Overflow, overflow)
			.SetAssignedRenderMode(renderMode));

	[Fact]
	public void AnInteractiveRender_MarksTheRowForTheScript()
	{
		SetRendererInfo(new RendererInfo("Server", true));

		Assert.Contains("moka-tabs--will-fit", Tabs(MokaTabsOverflow.Menu).Find(".moka-tabs").ClassList);
	}

	[Fact]
	public void APrerenderOfAnInteractiveComponent_MarksItToo()
	{
		SetRendererInfo(new RendererInfo("Static", false));

		IRenderedComponent<MokaTabs> cut = Tabs(MokaTabsOverflow.Menu, RenderMode.InteractiveServer);

		Assert.Contains("moka-tabs--will-fit", cut.Find(".moka-tabs").ClassList);
	}

	// No script will ever measure it, so it keeps the rules that let the list scroll.
	[Fact]
	public void AStaticPage_IsNotMarked()
	{
		SetRendererInfo(new RendererInfo("Static", false));

		Assert.Equal("moka-tabs moka-tabs--underline moka-tabs--menu", Tabs(MokaTabsOverflow.Menu).Find(".moka-tabs").ClassName);
	}

	// Tabs that scroll never ask, so a test or a page that renders them needs no renderer info.
	[Fact]
	public void ScrollingTabs_AreNotMarked_AndDoNotAskHowTheyRender() =>
		Assert.Equal("moka-tabs moka-tabs--underline", Tabs(MokaTabsOverflow.Scroll).Find(".moka-tabs").ClassName);

	[Fact]
	public void BeforeTheFit_ATabThatDoesNotFitWrapsToARowTheHeightLeavesOut()
	{
		IReadOnlyDictionary<string, string> list = ScopedCss.Declarations(Css, BeforeTheFit);

		Assert.Equal("wrap", list["flex-wrap"]);
		Assert.Equal("content-box", list["box-sizing"]);
		Assert.Equal("hidden", ScopedCss.Declarations(Css, ".moka-tabs > .moka-tabs__list")["overflow-y"]);
	}

	// A single tab wider than the row has nothing to wrap past, and the script leaves the row unfitted when it can't fit
	// anything, so the list still scrolls sideways.
	[Fact]
	public void BeforeTheFit_TheListStillScrollsSideways()
	{
		Assert.DoesNotContain("overflow-x", ScopedCss.Declarations(Css, BeforeTheFit).Keys);
		Assert.Equal("auto", ScopedCss.Declarations(Css, ".moka-tabs > .moka-tabs__list")["overflow-x"]);
	}

	[Fact]
	public void TheRowIsAsTallAsAnUnderlineTab() =>
		Assert.Equal(
			ScopedCss.Declarations(Css, ".moka-tabs--underline > .moka-tabs__list > .moka-tabs__tab")["height"],
			ScopedCss.Declarations(Css, BeforeTheFit)["height"]);

	// A segmented tab is its text line and padding, or the theme's segment height when that is more. The original design's
	// segment height is a bare 0, which calc() and max() can't take, so it is the minimum.
	[Fact]
	public void TheSegmentedRowIsAsTallAsASegmentedTab()
	{
		IReadOnlyDictionary<string, string> tab = ScopedCss.Declarations(Css, ".moka-tabs--segmented > .moka-tabs__list > .moka-tabs__tab");
		IReadOnlyDictionary<string, string> row = ScopedCss.Declarations(Css, SegmentedBeforeTheFit);
		string verticalPadding = tab["padding"].Split(' ')[0];

		Assert.Equal($"calc({tab["font-size"]} * var(--moka-line-height-base) + 2 * {verticalPadding})", row["height"]);
		Assert.Equal(tab["min-height"], row["min-height"]);
	}

	// The list's padding sits outside the clip's row and inside its box, so a wrapped row must start no higher than the
	// padding ends: the gap between rows is at least the padding.
	[Fact]
	public void ASegmentedListsGap_KeepsTheWrappedRowOutOfItsPadding()
	{
		IReadOnlyDictionary<string, string> list = ScopedCss.Declarations(Css, ".moka-tabs--segmented > .moka-tabs__list");

		Assert.Equal(list["padding"], list["gap"]);
	}

	// Hidden, so the tabs drawn before the fit are the ones the fit keeps beside the button.
	[Fact]
	public void BeforeTheFit_TheButtonsColumnKeepsItsRoom()
	{
		IReadOnlyDictionary<string, string> more = ScopedCss.Declarations(Css,
			".moka-tabs--will-fit:not(.moka-tabs--stretch, [data-moka-fitted], [data-moka-has-more]) > .moka-tabs__more");

		Assert.Equal("flex", more["display"]);
		Assert.Equal("hidden", more["visibility"]);
	}
}
