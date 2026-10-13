using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Moka.Red.Feedback.Tooltip;
using Moka.Red.Icons;
using Moka.Red.Primitives.Attribute;
using Moka.Red.Primitives.Callout;
using Moka.Red.Primitives.Carousel;
using Moka.Red.Primitives.Chat;
using Moka.Red.Primitives.Chip;
using Moka.Red.Primitives.ColorSwatch;
using Moka.Red.Primitives.Fab;
using Moka.Red.Primitives.InfiniteCarousel;
using Moka.Red.Primitives.Media;
using Moka.Red.Primitives.Notice;
using Moka.Red.Primitives.OrgChart;
using Moka.Red.Primitives.SplitButton;
using Moka.Red.Primitives.TransferList;
using Moka.Red.Primitives.Tree;
using Moka.Red.Primitives.Utility;
using Moka.Red.Tests.Shared;

namespace Moka.Red.Primitives.Tests.Components;

// Every control here shows only an icon (or a colour, or a dot). Those that had a title showed the browser's own pale,
// late hint instead of Moka's; the rest had no hover name at all. Each is named by aria-label and by a MokaTooltip on
// the side its place calls for.
public class IconTooltipTests : BunitContext
{
	private static readonly string[] Swatches = ["#ef5350", "#42a5f5"];

	public IconTooltipTests() => JSInterop.Mode = JSRuntimeMode.Loose;

	[Fact]
	public async Task ThemeToggle_NamesWhatItSwitchesTo()
	{
		IRenderedComponent<MokaThemeToggle> cut = Render<MokaThemeToggle>(p => p.Add(x => x.IsDark, false));

		IconTooltip.AssertNamed(cut.Find("button"), "Switch to dark mode", MokaTooltipPosition.Bottom);

		await cut.Find("button").ClickAsync(new MouseEventArgs());

		IconTooltip.AssertNamed(cut.Find("button"), "Switch to light mode", MokaTooltipPosition.Bottom);
	}

	// A toggle in a side rail or a footer opens its name elsewhere.
	[Fact]
	public void ThemeToggle_TakesItsTooltipsPlace()
	{
		IRenderedComponent<MokaThemeToggle> cut = Render<MokaThemeToggle>(p => p
			.Add(x => x.TooltipPosition, MokaTooltipPosition.Right));

		IconTooltip.AssertNamed(cut.Find("button"), "Switch to dark mode", MokaTooltipPosition.Right);
	}

	// The name used to be a fixed English title; the labels translate both the tooltip and the name.
	[Fact]
	public async Task ThemeToggle_TakesItsLabelsFromParameters()
	{
		IRenderedComponent<MokaThemeToggle> cut = Render<MokaThemeToggle>(p => p
			.Add(x => x.DarkModeLabel, "Dunkles Design")
			.Add(x => x.LightModeLabel, "Helles Design"));

		IconTooltip.AssertNamed(cut.Find("button"), "Dunkles Design", MokaTooltipPosition.Bottom);

		await cut.Find("button").ClickAsync(new MouseEventArgs());

		IconTooltip.AssertNamed(cut.Find("button"), "Helles Design", MokaTooltipPosition.Bottom);
	}

	// Class, Style and Id stay on the button, as before.
	[Fact]
	public void ThemeToggle_KeepsItsClassAndIdOnTheButton()
	{
		IRenderedComponent<MokaThemeToggle> cut = Render<MokaThemeToggle>(p => p
			.Add(x => x.Id, "theme")
			.Add(x => x.Class, "mine"));

		IElement button = cut.Find("button");
		Assert.Equal("theme", button.Id);
		Assert.Contains("mine", button.ClassList);
		Assert.Contains("moka-theme-toggle", button.ClassList);
	}

	[Fact]
	public async Task CopyButton_SaysCopiedInItsTooltipAfterACopy()
	{
		JSInterop.SetupModule("./_content/Moka.Red.Core/moka-drag.js")
			.Setup<bool>("copyToClipboard", _ => true).SetResult(true);
		IRenderedComponent<MokaCopyButton> cut = Render<MokaCopyButton>(p => p.Add(x => x.Text, "abc"));

		IconTooltip.AssertNamed(cut.Find("button"), "Copy", MokaTooltipPosition.Bottom);

		await cut.Find("button").ClickAsync(new MouseEventArgs());

		IconTooltip.AssertNamed(cut.Find("button"), "Copied!", MokaTooltipPosition.Bottom);
	}

	// With content of its own the button shows a label, which stays its name; the tooltip still says what it does.
	[Fact]
	public void CopyButton_WithContent_KeepsItsLabelAsTheName()
	{
		IRenderedComponent<MokaCopyButton> cut = Render<MokaCopyButton>(p => p
			.Add(x => x.Text, "abc")
			.Add(x => x.TooltipPosition, MokaTooltipPosition.Top)
			.AddChildContent("Copy invite link"));

		IElement button = cut.Find("button");
		Assert.False(button.HasAttribute("title"));
		Assert.False(button.HasAttribute("aria-label"));
		IElement popup = cut.Find(".moka-tooltip-popup");
		Assert.Equal("Copy", popup.TextContent.Trim());
		Assert.Contains("moka-tooltip-popup--top", popup.ClassList);
	}

	// Fixed in the corner, so the name opens above it and lines up with the window's edge. The root holds the button's
	// place, so the tooltip's element moves with it.
	[Fact]
	public async Task ScrollToTop_IsNamedAboveAndHeldInPlaceByItsRoot()
	{
		IRenderedComponent<MokaScrollToTop> cut = Render<MokaScrollToTop>(p => p
			.Add(x => x.Id, "top")
			.Add(x => x.Style, "bottom: 16px"));
		await cut.InvokeAsync(() => cut.Instance.OnScrollChanged(true));

		IElement button = cut.Find("button");
		IconTooltip.AssertNamed(button, "Scroll to top", MokaTooltipPosition.TopEnd);
		Assert.Equal("top", button.Id);
		IElement root = cut.Find(".moka-scroll-top");
		Assert.Equal("DIV", root.TagName);
		Assert.Equal("bottom: 16px", root.GetAttribute("style"));
		Assert.Contains("moka-scroll-top", button.Closest(".moka-tooltip-trigger")!.ParentElement!.ClassList);
	}

	[Theory]
	[InlineData(MokaFabPosition.BottomRight, MokaTooltipPosition.TopEnd)]
	[InlineData(MokaFabPosition.BottomLeft, MokaTooltipPosition.TopStart)]
	[InlineData(MokaFabPosition.TopRight, MokaTooltipPosition.BottomEnd)]
	[InlineData(MokaFabPosition.TopLeft, MokaTooltipPosition.BottomStart)]
	public void Fab_OpensItsNameAwayFromItsCorner(MokaFabPosition corner, MokaTooltipPosition position)
	{
		IRenderedComponent<MokaFloatingActionButton> cut = Render<MokaFloatingActionButton>(p => p
			.Add(x => x.Icon, MokaIcons.Action.Add)
			.Add(x => x.Label, "New item")
			.Add(x => x.Position, corner));

		IconTooltip.AssertNamed(cut.Find("button"), "New item", position);
	}

	// Extended, the label shows beside the icon, so there is nothing for a tooltip to add.
	[Fact]
	public void Fab_ExtendedShowsItsLabelAndNoTooltip()
	{
		IRenderedComponent<MokaFloatingActionButton> cut = Render<MokaFloatingActionButton>(p => p
			.Add(x => x.Icon, MokaIcons.Action.Add)
			.Add(x => x.Label, "New item")
			.Add(x => x.Extended, true));

		Assert.Equal("New item", cut.Find(".moka-fab-label").TextContent);
		Assert.Empty(cut.FindAll(".moka-tooltip-popup"));
	}

	// The root takes the corner and a style that moves it; the button keeps the look, the id and the attributes.
	[Fact]
	public void Fab_PlacesItsRootAndDrawsItsButton()
	{
		IRenderedComponent<MokaFloatingActionButton> cut = Render<MokaFloatingActionButton>(p => p
			.Add(x => x.Icon, MokaIcons.Action.Add)
			.Add(x => x.Label, "New item")
			.Add(x => x.Id, "fab")
			.Add(x => x.Style, "position: absolute"));

		IElement root = cut.Find(".moka-fab");
		Assert.Equal("DIV", root.TagName);
		Assert.Contains("moka-fab--bottom-right", root.ClassList);
		Assert.Equal("position: absolute", root.GetAttribute("style"));
		IElement button = cut.Find("button");
		Assert.Equal("fab", button.Id);
		Assert.Contains("moka-fab__button", button.ClassList);
		Assert.Contains("moka-fab--primary", button.ClassList);
	}

	[Fact]
	public void ColorSwatch_NamesEverySwatchAndButton()
	{
		IRenderedComponent<MokaColorSwatch> cut = Render<MokaColorSwatch>(p => p
			.Add(x => x.Colors, Swatches)
			.Add(x => x.AllowCustom, true));

		IReadOnlyList<IElement> swatches = cut.FindAll(".moka-color-swatch-item");
		IconTooltip.AssertNamed(swatches[0], "#ef5350", MokaTooltipPosition.Bottom);
		IconTooltip.AssertNamed(swatches[1], "#42a5f5", MokaTooltipPosition.Bottom);

		IElement add = cut.Find(".moka-color-swatch-add");
		IconTooltip.AssertNamed(add, "Custom color", MokaTooltipPosition.Bottom);
		add.Click();

		IconTooltip.AssertNamed(cut.Find(".moka-color-swatch-custom-ok"), "Use this color", MokaTooltipPosition.Bottom);
		IconTooltip.AssertNamed(cut.Find(".moka-color-swatch-custom-cancel"), "Cancel", MokaTooltipPosition.Bottom);
	}

	// The remove button sits inside the chip's own button. A browser keeps that nesting, since Blazor builds the page
	// in script, but the HTML parser the test reads markup with closes the outer button first, so the tooltip is read
	// on its own.
	[Fact]
	public void Chip_NamesItsRemoveButton()
	{
		IRenderedComponent<MokaChip> cut = Render<MokaChip>(p => p
			.Add(x => x.Text, "react")
			.Add(x => x.Closable, true));

		IRenderedComponent<MokaTooltip> tooltip = cut.FindComponent<MokaTooltip>();
		IconTooltip.AssertNamed(tooltip.Find(".moka-chip__close"), "Remove", MokaTooltipPosition.Bottom);
	}

	[Fact]
	public void Attribute_NamesItsRemoveButton()
	{
		IRenderedComponent<MokaAttribute> cut = Render<MokaAttribute>(p => p
			.Add(x => x.Label, "region")
			.Add(x => x.Dismissible, true));

		IRenderedComponent<MokaTooltip> tooltip = cut.FindComponent<MokaTooltip>();
		IconTooltip.AssertNamed(tooltip.Find(".moka-attr-dismiss"), "Remove", MokaTooltipPosition.Bottom);
	}

	// The close button ends the header row, so its tooltip lines up with the callout's edge, and the tooltip's
	// element takes the push to the end of the row that the button used to take.
	[Fact]
	public void Callout_NamesItsCloseButtonAtTheEndOfTheRow()
	{
		IRenderedComponent<MokaCallout> cut = Render<MokaCallout>(p => p.Add(x => x.Closable, true));

		IElement close = cut.Find(".moka-callout__close");
		IconTooltip.AssertNamed(close, "Close", MokaTooltipPosition.BottomEnd);
		Assert.Equal("button", close.GetAttribute("type"));
		Assert.Contains("moka-callout__close-tip", close.ParentElement!.ClassList);
		Assert.Equal(
			"auto",
			ScopedCss.Declarations("MokaCallout.razor.css", ".moka-callout ::deep .moka-callout__close-tip")["margin-left"]);
	}

	[Theory]
	[InlineData(MokaNoticePosition.Top, MokaTooltipPosition.BottomEnd)]
	[InlineData(MokaNoticePosition.Bottom, MokaTooltipPosition.TopEnd)]
	public void Notice_NamesItsCloseButtonAwayFromTheWindowsEdge(MokaNoticePosition place, MokaTooltipPosition position)
	{
		IRenderedComponent<MokaNotice> cut = Render<MokaNotice>(p => p
			.Add(x => x.Closable, true)
			.Add(x => x.Position, place)
			.AddChildContent("Maintenance tonight"));

		IconTooltip.AssertNamed(cut.Find(".moka-notice__close"), "Close", position);
	}

	[Fact]
	public void Carousel_NamesItsArrowsAndDots()
	{
		IRenderedComponent<MokaCarousel> cut = Render<MokaCarousel>(p => p.AddChildContent(Slides<MokaCarouselSlide>(3)));

		IconTooltip.AssertNamed(cut.Find(".moka-carousel-arrow--prev"), "Previous slide", MokaTooltipPosition.BottomStart);
		IconTooltip.AssertNamed(cut.Find(".moka-carousel-arrow--next"), "Next slide", MokaTooltipPosition.BottomEnd);
		IReadOnlyList<IElement> dots = cut.FindAll(".moka-carousel-dot");
		Assert.Equal(3, dots.Count);
		IconTooltip.AssertNamed(dots[2], "Go to slide 3", MokaTooltipPosition.Top);
	}

	[Theory]
	[InlineData(MokaCarouselDirection.Horizontal, MokaTooltipPosition.BottomStart, MokaTooltipPosition.BottomEnd, MokaTooltipPosition.Top)]
	[InlineData(MokaCarouselDirection.Vertical, MokaTooltipPosition.Top, MokaTooltipPosition.Bottom, MokaTooltipPosition.Left)]
	public void InfiniteCarousel_NamesItsArrowsAndDotsAwayFromTheSlides(
		MokaCarouselDirection direction,
		MokaTooltipPosition previous,
		MokaTooltipPosition next,
		MokaTooltipPosition dot)
	{
		IRenderedComponent<MokaInfiniteCarousel> cut = Render<MokaInfiniteCarousel>(p => p
			.Add(x => x.AutoPlay, false)
			.Add(x => x.Direction, direction)
			.AddChildContent(Slides<MokaInfiniteCarouselSlide>(2)));

		IconTooltip.AssertNamed(cut.Find(".moka-infinite-carousel-arrow--prev"), "Previous slide", previous);
		IconTooltip.AssertNamed(cut.Find(".moka-infinite-carousel-arrow--next"), "Next slide", next);
		IconTooltip.AssertNamed(cut.FindAll(".moka-infinite-carousel-dot")[0], "Go to slide 1", dot);
	}

	// A transform on the element that holds a tooltip makes it the box the fixed popup is placed in, so the arrows
	// and dots are centred with auto margins instead.
	[Theory]
	[InlineData("MokaCarousel.razor.css", ".moka-carousel ::deep .moka-carousel-arrow-tip")]
	[InlineData("MokaCarousel.razor.css", ".moka-carousel-dots")]
	[InlineData("MokaCarousel.razor.css", ".moka-carousel-arrow")]
	[InlineData("MokaInfiniteCarousel.razor.css", ".moka-infinite-carousel ::deep .moka-infinite-carousel-arrow-tip")]
	[InlineData("MokaInfiniteCarousel.razor.css", ".moka-infinite-carousel-dots")]
	[InlineData("MokaInfiniteCarousel.razor.css", ".moka-infinite-carousel--vertical .moka-infinite-carousel-dots")]
	[InlineData("MokaInfiniteCarousel.razor.css", ".moka-infinite-carousel-arrow")]
	public void WhatHoldsATooltip_IsNotTransformed(string file, string selector)
	{
		IReadOnlyDictionary<string, string> rules = ScopedCss.Declarations(file, selector);

		Assert.NotEmpty(rules);
		Assert.False(rules.ContainsKey("transform"), $"{selector} sets transform: {rules.GetValueOrDefault("transform")}");
	}

	[Fact]
	public async Task MediaGallery_NamesTheLightboxButtons()
	{
		IRenderedComponent<MokaMediaGallery> cut = Render<MokaMediaGallery>(p => p
			.Add(x => x.Items, new[]
			{
				new MokaMediaItem { Src = "/a.jpg" },
				new MokaMediaItem { Src = "/b.jpg" },
				new MokaMediaItem { Src = "/c.jpg" }
			}));

		await cut.FindAll(".moka-media-gallery-item")[1].ClickAsync(new MouseEventArgs());

		IconTooltip.AssertNamed(cut.Find(".moka-lightbox-nav--prev"), "Previous image", MokaTooltipPosition.Bottom);
		IconTooltip.AssertNamed(cut.Find(".moka-lightbox-nav--next"), "Next image", MokaTooltipPosition.Bottom);
		IconTooltip.AssertNamed(cut.Find(".moka-lightbox-close"), "Close", MokaTooltipPosition.BottomEnd);
	}

	// The input row sits at the bottom of the chat, so the name opens above it. The button stays named while it is
	// greyed out for an empty message.
	[Fact]
	public void Chat_NamesItsSendButtonAboveTheInput()
	{
		IRenderedComponent<MokaChat> cut = Render<MokaChat>();

		IElement send = cut.Find(".moka-chat__send-btn");
		Assert.True(send.HasAttribute("disabled"));
		IconTooltip.AssertNamed(send, "Send message", MokaTooltipPosition.TopEnd);
	}

	[Theory]
	[InlineData(MokaOrgOrientation.TopDown, MokaTooltipPosition.Bottom)]
	[InlineData(MokaOrgOrientation.LeftRight, MokaTooltipPosition.Right)]
	public void OrgChart_NamesItsToggleTowardItsChildren(MokaOrgOrientation orientation, MokaTooltipPosition position)
	{
		MokaOrgNode<string> root = new("CEO", [new MokaOrgNode<string>("CTO")]);
		RenderFragment<string> card = name => builder => builder.AddContent(0, name);
		IRenderedComponent<MokaOrganizationChart<string>> cut = Render<MokaOrganizationChart<string>>(p => p
			.Add(x => x.Root, root)
			.Add(x => x.ItemTemplate, card)
			.Add(x => x.Collapsible, true)
			.Add(x => x.Orientation, orientation));

		IconTooltip.AssertNamed(cut.Find(".moka-org-node__toggle"), "Collapse", position);
	}

	// The menu opens where the tooltip would, so the arrow shows none while it is open; its name stays.
	[Fact]
	public async Task SplitButton_NamesItsArrowUntilTheMenuOpens()
	{
		RenderFragment menu = builder => builder.AddContent(0, "Save as");
		IRenderedComponent<MokaSplitButton> cut = Render<MokaSplitButton>(p => p
			.AddChildContent("Save")
			.Add(x => x.DropdownContent, menu));

		IconTooltip.AssertNamed(cut.Find(".moka-split-btn__toggle"), "More options", MokaTooltipPosition.BottomEnd);

		await cut.Find(".moka-split-btn__toggle").ClickAsync(new MouseEventArgs());

		Assert.Equal("More options", cut.Find(".moka-split-btn__toggle").GetAttribute("aria-label"));
		Assert.Empty(cut.FindAll(".moka-split-btn__toggle-tip > .moka-tooltip-popup"));
	}

	// The arrow keeps its corners and fill through the tooltip's element it now sits in.
	[Fact]
	public void SplitButton_PassesItsCornersAndFillThroughTheTooltip()
	{
		IReadOnlyDictionary<string, string> tip =
			ScopedCss.Declarations("MokaSplitButton.razor.css", ".moka-split-btn ::deep .moka-split-btn__toggle-tip");

		Assert.Equal("inherit", tip["border-top-right-radius"]);
		Assert.Equal("inherit", tip["border-bottom-right-radius"]);
		Assert.Equal("inherit", tip["background"]);
	}

	// A column between the lists: each name opens toward the list its arrow points at, also while greyed out.
	[Fact]
	public void TransferList_NamesItsArrowsAfterTheirLists()
	{
		IRenderedComponent<MokaTransferList<string>> cut = Render<MokaTransferList<string>>(p => p
			.Add(x => x.AvailableItems, new List<string> { "a" })
			.Add(x => x.SelectedTitle, "Chosen"));

		IReadOnlyList<IElement> arrows = cut.FindAll(".moka-transfer-list-btn");
		Assert.True(arrows[0].HasAttribute("disabled"));
		IconTooltip.AssertNamed(arrows[0], "Move to Chosen", MokaTooltipPosition.Right);
		IconTooltip.AssertNamed(arrows[1], "Move to Available", MokaTooltipPosition.Left);
	}

	// The toggle is hidden from screen readers (the keys and aria-expanded do its work), and so is its tooltip, whose
	// text would otherwise join the item's name while it shows.
	[Fact]
	public void TreeItem_NamesItsToggleBesideTheTree()
	{
		IRenderedComponent<MokaTree> cut = Render<MokaTree>(p => p
			.AddChildContent<MokaTreeItem>(item => item
				.Add(x => x.Text, "src")
				.AddChildContent<MokaTreeItem>(child => child.Add(x => x.Text, "app.cs"))));

		IElement toggle = cut.Find(".moka-tree-item__toggle");
		IconTooltip.AssertNamed(toggle, "Expand", MokaTooltipPosition.Right);
		Assert.Equal("true", toggle.Closest(".moka-tooltip-trigger")!.GetAttribute("aria-hidden"));
	}

	private static RenderFragment Slides<TSlide>(int count)
		where TSlide : IComponent => builder =>
	{
		for (int i = 0; i < count; i++)
		{
			builder.OpenComponent<TSlide>(0);
			builder.AddComponentParameter(1, "ChildContent", (RenderFragment)(slide => slide.AddContent(0, "slide")));
			builder.CloseComponent();
		}
	};
}
