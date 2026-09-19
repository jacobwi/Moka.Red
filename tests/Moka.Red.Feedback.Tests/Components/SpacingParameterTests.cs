using AngleSharp.Dom;
using Bunit;
using Bunit.Rendering;
using Moka.Red.Core.Enums;
using Moka.Red.Feedback.BottomSheet;
using Moka.Red.Feedback.Cheatsheet;
using Moka.Red.Feedback.Dropdown;
using Moka.Red.Feedback.EmptyState;
using Moka.Red.Feedback.Loading;
using Moka.Red.Feedback.NotificationBell;
using Moka.Red.Feedback.Popover;
using Moka.Red.Feedback.Progress;
using Moka.Red.Tests.Shared;
using static Moka.Red.Tests.Shared.SpacingCase;

namespace Moka.Red.Feedback.Tests.Components;

// Margin, Padding and Rounded are declared on every visual component, and these dropped some or
// all of them. Each case sets all three and checks where they land. Without them every style
// attribute has to read as it did before. An overlay has no box in the page, so all three go on
// the panel it shows.
public class SpacingParameterTests : BunitContext
{
	private static readonly Dictionary<string, SpacingCase> Cases = new()
	{
		["BottomSheet"] = new(typeof(MokaBottomSheet), "max-height: 70vh", ("Open", true))
		{
			Root = ".moka-bottom-sheet"
		},
		["Cheatsheet"] = new(typeof(MokaCheatsheet), $"max-width: 720px | {IconStyle("16px")}", ("Open", true))
		{
			Root = ".moka-cheatsheet"
		},
		["Dropdown"] = new(typeof(MokaDropdown), "", ("Open", true))
		{
			Root = ".moka-popover", Box = ".moka-popover-popup"
		},
		["EmptyState"] = new(typeof(MokaEmptyState), "", ("Title", "Nothing here")),
		["LoadingOverlay"] = new(typeof(MokaLoadingOverlay),
			"background: color-mix(in srgb, var(--moka-color-surface) 70%, transparent) | " +
			"color: var(--moka-color-primary) | width: 20px; height: 20px",
			("Loading", true)),
		["NotificationBell"] = new(typeof(MokaNotificationBell),
			$"{IconStyle("20px")} | {IconStyle("28px")}; opacity: 0.3")
		{
			Box = ".moka-notification-bell__dropdown", Open = ".moka-notification-bell__trigger"
		},
		["Popover"] = new(typeof(MokaPopover), "", ("Open", true)) { Box = ".moka-popover-popup" },
		["Progress"] = new(typeof(MokaProgress), "width: 40%", ("Value", 40d)) { RadiusOn = ".moka-progress-track" },
		["Skeleton"] = new(typeof(MokaSkeleton), "width: 100%") { RadiusOn = ".moka-skeleton-line" },
		["SkeletonRectangle"] = new(typeof(MokaSkeleton), "width: 100%; height: 48px",
			("Shape", MokaSkeletonShape.Rectangle)) { RadiusOn = ".moka-skeleton-rect" },
		["Spinner"] = new(typeof(MokaSpinner), "color: var(--moka-color-primary) | width: 20px; height: 20px")
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

	// The dropdown's own root is display: contents, so a margin or Style on it never showed. The
	// popover it wraps is the box in the page, and its panel draws the menu.
	[Fact]
	public void Dropdown_HandsMarginAndStyle_ToThePopoversWrapper()
	{
		IRenderedComponent<MokaDropdown> cut = Render<MokaDropdown>(p => p
			.Add(x => x.Open, true)
			.Add(x => x.MarginValue, "7px")
			.Add(x => x.PaddingValue, "5px")
			.Add(x => x.RoundedValue, "3px")
			.Add(x => x.Style, "color: red"));

		Assert.Null(cut.Find(".moka-dropdown").GetAttribute("style"));
		Assert.Equal("margin: 7px; color: red", cut.Find(".moka-popover").GetAttribute("style"));
		Assert.Equal("padding: 5px; border-radius: 3px", cut.Find(".moka-popover-popup").GetAttribute("style"));
	}

	// A linear bar was 100% wide, so a margin made it wider than its container. It takes
	// moka-fill-width now, which keeps the margin inside, and its own rule sets no width that would
	// win over it. A circular one keeps its own size.
	[Fact]
	public void LinearProgress_FillsItsContainer_WithTheMarginInside()
	{
		IRenderedComponent<MokaProgress> linear = Render<MokaProgress>(p => p
			.Add(x => x.Value, 40d)
			.Add(x => x.MarginValue, "7px"));
		IRenderedComponent<MokaProgress> circular = Render<MokaProgress>(p => p
			.Add(x => x.Value, 40d)
			.Add(x => x.ProgressType, MokaProgressType.Circular));

		Assert.Contains("moka-fill-width", linear.Find(".moka-progress").ClassList);
		Assert.DoesNotContain("moka-fill-width", circular.Find(".moka-progress").ClassList);
		Assert.DoesNotContain("width",
			ScopedCss.Declarations("MokaProgress.razor.css", ".moka-progress--linear").Keys);
	}

	[Fact]
	public void Popover_AppliesStyle_ToItsWrapper()
	{
		IRenderedComponent<MokaPopover> cut = Render<MokaPopover>(p => p.Add(x => x.Style, "color: red"));

		Assert.Equal("color: red", cut.Find(".moka-popover").GetAttribute("style"));
	}

	// The cheatsheet was centered with a transform, which a margin would have pushed off center.
	// A flex wrapper centers it now, so the margin keeps it away from every edge alike.
	[Fact]
	public void Cheatsheet_SitsInACenteringWrapper_BesideItsBackdrop()
	{
		IRenderedComponent<MokaCheatsheet> cut = Render<MokaCheatsheet>(p => p
			.Add(x => x.Open, true)
			.Add(x => x.MarginValue, "7px"));

		IElement wrapper = cut.Find(".moka-cheatsheet-wrapper");
		Assert.Equal(["moka-cheatsheet-backdrop", "moka-cheatsheet"],
			wrapper.Children.Select(child => child.ClassList[0]).ToArray());
		Assert.Null(wrapper.GetAttribute("style"));
		Assert.StartsWith("margin: 7px", cut.Find(".moka-cheatsheet").GetAttribute("style"), StringComparison.Ordinal);

		IReadOnlyDictionary<string, string> wrapperCss = ScopedCss.Declarations("MokaCheatsheet.razor.css", ".moka-cheatsheet-wrapper");
		Assert.Equal("fixed", wrapperCss["position"]);
		Assert.Equal("flex", wrapperCss["display"]);
		Assert.Equal("center", wrapperCss["align-items"]);
		Assert.Equal("center", wrapperCss["justify-content"]);
		IReadOnlyDictionary<string, string> modalCss = ScopedCss.Declarations("MokaCheatsheet.razor.css", ".moka-cheatsheet");
		Assert.DoesNotContain("position", modalCss.Keys);
		Assert.DoesNotContain("transform", modalCss.Keys);
	}

	// A full-screen sheet was 100vh tall inline, so a margin pushed its top off the screen. It
	// stretches to the wrapper in CSS now, and the margin fits inside the screen.
	[Fact]
	public void FullScreenBottomSheet_TakesTheMargin_WithoutAFixedHeight()
	{
		IRenderedComponent<MokaBottomSheet> cut = Render<MokaBottomSheet>(p => p
			.Add(x => x.Open, true)
			.Add(x => x.FullScreen, true)
			.Add(x => x.MarginValue, "7px"));

		IElement sheet = cut.Find(".moka-bottom-sheet");
		Assert.Contains("moka-bottom-sheet--fullscreen", sheet.ClassList);
		Assert.Equal("margin: 7px", sheet.GetAttribute("style"));
		Assert.Equal("stretch",
			ScopedCss.Declarations("MokaBottomSheet.razor.css", ".moka-bottom-sheet--fullscreen")["align-self"]);
	}

	// The radius used to land on the skeleton's root, which draws nothing, and Rounded only switched
	// the rectangle to the large radius whatever size it named.
	[Fact]
	public void Skeleton_RoundsEveryShape_WithTheRadiusItWasGiven()
	{
		IRenderedComponent<MokaSkeleton> cut = Render<MokaSkeleton>(p => p
			.Add(x => x.Shape, MokaSkeletonShape.Card)
			.Add(x => x.Rounded, MokaRounding.Sm));

		Assert.Null(cut.Find(".moka-skeleton").GetAttribute("style"));
		IReadOnlyList<IElement> shapes = cut.FindAll(".moka-skeleton-card-image, .moka-skeleton-line");
		Assert.Equal(4, shapes.Count);
		Assert.All(shapes, shape => Assert.Contains("border-radius: var(--moka-radius-sm)",
			shape.GetAttribute("style"), StringComparison.Ordinal));
	}

	[Theory]
	[InlineData(MokaSkeletonShape.Circle, "width: 40px; height: 40px; border-radius: 0")]
	[InlineData(MokaSkeletonShape.Rectangle, "width: 100%; height: 48px; border-radius: 0")]
	public void Skeleton_RoundedNone_SquaresTheShape(MokaSkeletonShape shape, string expected)
	{
		IRenderedComponent<MokaSkeleton> cut = Render<MokaSkeleton>(p => p
			.Add(x => x.Shape, shape)
			.Add(x => x.Rounded, MokaRounding.None));

		Assert.Equal(expected, cut.Find(".moka-skeleton > div").GetAttribute("style"));
	}
}
