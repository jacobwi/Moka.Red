using AngleSharp.Dom;
using Bunit;
using Bunit.Rendering;
using Moka.Red.Navigation.Menu;
using Moka.Red.Navigation.Sidebar;
using Moka.Red.Tests.Shared;

namespace Moka.Red.Navigation.Tests.Components;

// Margin, Padding and Rounded are declared on every visual component, and these dropped some or
// all of them. Each case sets all three and checks where they land. Without them every style
// attribute has to read as it did before.
public class SpacingParameterTests : BunitContext
{
	private static readonly Dictionary<string, SpacingCase> Cases = new()
	{
		["MenuItem"] = new(typeof(MokaMenuItem), "", ("Text", "Home")),
		["MenuItemLink"] = new(typeof(MokaMenuItem), "", ("Text", "Home"), ("Href", "/home")),
		["Sidebar"] = new(typeof(MokaSidebar), "width: 240px"),
		["SidebarOverlay"] = new(typeof(MokaSidebar), "width: 240px", ("Overlay", true)) { Root = "nav" }
	};

	public SpacingParameterTests() => JSInterop.Mode = JSRuntimeMode.Loose;

	public static TheoryData<string> Names => [.. Cases.Keys];

	[Theory]
	[MemberData(nameof(Names))]
	public void SpacingParameters_LandOnTheComponent(string name)
	{
		SpacingCase spacing = Cases[name];
		IRenderedComponent<ContainerFragment> cut = Render(spacing.Render(withSpacing: true));
		spacing.OpenIfNeeded(cut);

		spacing.AssertSpacing(cut);
	}

	[Theory]
	[MemberData(nameof(Names))]
	public void Styles_AreUnchanged_WithoutSpacingParameters(string name)
	{
		SpacingCase spacing = Cases[name];
		IRenderedComponent<ContainerFragment> cut = Render(spacing.Render(withSpacing: false));
		spacing.OpenIfNeeded(cut);

		Assert.Equal(spacing.DefaultStyles, SpacingCase.Styles(cut));
	}

	// A closed sidebar takes no room: at width 0 its padding would still show as a strip and its
	// margin as a gap, and an overlay slid out by its own width would leave its margin on screen.
	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public void ClosedSidebar_DropsItsMarginAndPadding(bool overlay)
	{
		IRenderedComponent<MokaSidebar> cut = Render<MokaSidebar>(p => p
			.Add(x => x.Open, false)
			.Add(x => x.Overlay, overlay)
			.Add(x => x.MarginValue, "7px")
			.Add(x => x.PaddingValue, "5px")
			.Add(x => x.RoundedValue, "3px"));

		Assert.Equal("border-radius: 3px; width: 240px", cut.Find("nav").GetAttribute("style"));

		cut.Render(p => p.Add(x => x.Open, true));

		Assert.Equal("margin: 7px; padding: 5px; border-radius: 3px; width: 240px",
			cut.Find("nav").GetAttribute("style"));
	}

	// An overlay sidebar was 100% of the screen tall, so a top margin pushed its bottom off the
	// screen. Its height comes from top and bottom, which leaves room for the margin, and nothing
	// else sets one.
	[Fact]
	public void OverlaySidebar_IsSizedByTopAndBottom()
	{
		IRenderedComponent<MokaSidebar> cut = Render<MokaSidebar>(p => p.Add(x => x.Overlay, true));
		IReadOnlyDictionary<string, string> css = ScopedCss.Declarations("MokaSidebar.razor.css", ".moka-sidebar--overlay");

		Assert.Equal("0", css["top"]);
		Assert.Equal("0", css["bottom"]);
		Assert.DoesNotContain("height", css.Keys);
		Assert.DoesNotContain("moka-fill-height", cut.Find("nav").ClassList);
	}

	// An inline sidebar was 100% of its parent's height, so a margin made it taller than a parent
	// with a set height. It takes moka-fill-height now, which keeps the margin inside, and its own
	// rule sets no height that would win over it.
	[Fact]
	public void InlineSidebar_FillsItsParentsHeight_WithTheMarginInside()
	{
		IRenderedComponent<MokaSidebar> cut = Render<MokaSidebar>(p => p.Add(x => x.MarginValue, "7px"));

		Assert.Contains("moka-fill-height", cut.Find("nav").ClassList);
		Assert.DoesNotContain("height", ScopedCss.Declarations("MokaSidebar.razor.css", ".moka-sidebar").Keys);
	}

	// The menu was 100% wide, so a margin made it wider than its container.
	[Fact]
	public void Menu_FillsItsContainer_WithTheMarginInside()
	{
		IRenderedComponent<MokaMenu> cut = Render<MokaMenu>(p => p.Add(x => x.MarginValue, "7px"));

		Assert.Contains("moka-fill-width", cut.Find("nav").ClassList);
		Assert.Equal("margin: 7px", cut.Find("nav").GetAttribute("style"));
		Assert.DoesNotContain("width", ScopedCss.Declarations("MokaMenu.razor.css", ".moka-menu").Keys);
	}

	// The menu gave every item width: 100%, so an item's margin pushed it past the menu's edge.
	[Fact]
	public void MenuItem_FillsTheMenu_WithTheMarginInside()
	{
		IRenderedComponent<MokaMenu> cut = Render<MokaMenu>(p => p.AddChildContent<MokaMenuItem>(item => item
			.Add(x => x.Text, "Home")
			.Add(x => x.Href, "/")
			.Add(x => x.MarginValue, "7px")));

		IElement link = cut.Find(".moka-menu-item");
		Assert.Contains("moka-fill-width", link.ClassList);
		Assert.Contains("margin: 7px", link.GetAttribute("style"), StringComparison.Ordinal);
		IReadOnlyDictionary<string, string> rule = ScopedCss.Declarations("MokaMenu.razor.css", "::deep .moka-menu-item");
		Assert.Equal("block", rule["display"]);
		Assert.DoesNotContain("width", rule.Keys);
	}
}
