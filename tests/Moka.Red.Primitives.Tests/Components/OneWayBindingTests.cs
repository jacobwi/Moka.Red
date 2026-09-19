using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Moka.Red.Primitives.Attribute;
using Moka.Red.Primitives.Carousel;
using Moka.Red.Primitives.Chip;
using Moka.Red.Primitives.ColorSwatch;
using Moka.Red.Primitives.Confetti;
using Moka.Red.Primitives.InfiniteCarousel;
using Moka.Red.Primitives.SegmentedControl;
using Moka.Red.Primitives.Tree;
using Moka.Red.Primitives.Utility;

namespace Moka.Red.Primitives.Tests.Components;

// These components used to write the user's change into their own [Parameter]. Blazor passes every
// parameter again whenever the parent renders, so with a one-way value the parent's next render put
// the old value back. Each test changes the state through the UI, re-renders with the same value
// (what an unrelated parent render does), and then checks that a new value still wins.
public class OneWayBindingTests : BunitContext
{
	private static readonly string[] Swatches = ["#ff0000", "#00ff00", "#0000ff"];

	[Fact]
	public async Task TreeItem_StaysExpanded_WhenTheParentPassesTheSameValue()
	{
		IRenderedComponent<MokaTreeItem> cut = Render<MokaTreeItem>(p => p
			.Add(x => x.Text, "src")
			.Add(x => x.Expanded, false)
			.AddChildContent<MokaTreeItem>(child => child.Add(x => x.Text, "app.cs")));

		await cut.Find(".moka-tree-item__toggle").ClickAsync(new MouseEventArgs());
		cut.Render(p => p.Add(x => x.Expanded, false));

		Assert.Equal("true", cut.Find("[role=treeitem]").GetAttribute("aria-expanded"));
		Assert.Single(cut.FindAll(".moka-tree-item__children"));
	}

	[Fact]
	public async Task TreeItem_FollowsANewExpandedFromTheParent()
	{
		IRenderedComponent<MokaTreeItem> cut = Render<MokaTreeItem>(p => p
			.Add(x => x.Text, "src")
			.Add(x => x.Expanded, false)
			.AddChildContent<MokaTreeItem>(child => child.Add(x => x.Text, "app.cs")));

		await cut.Find(".moka-tree-item__toggle").ClickAsync(new MouseEventArgs());
		cut.Render(p => p.Add(x => x.Expanded, true));
		cut.Render(p => p.Add(x => x.Expanded, false));

		Assert.Equal("false", cut.Find("[role=treeitem]").GetAttribute("aria-expanded"));
		Assert.Empty(cut.FindAll(".moka-tree-item__children"));
	}

	[Fact]
	public async Task TreeItem_StaysSelected_WhenTheParentPassesTheSameValue()
	{
		IRenderedComponent<MokaTreeItem> cut = Render<MokaTreeItem>(p => p
			.AddCascadingValue("TreeSelectable", true)
			.Add(x => x.Text, "README.md")
			.Add(x => x.Selected, false));

		await cut.Find(".moka-tree-item__row").ClickAsync(new MouseEventArgs());
		cut.Render(p => p.Add(x => x.Selected, false));

		Assert.Equal("true", cut.Find("[role=treeitem]").GetAttribute("aria-selected"));
	}

	[Fact]
	public async Task TreeItem_FollowsANewSelectedFromTheParent()
	{
		IRenderedComponent<MokaTreeItem> cut = Render<MokaTreeItem>(p => p
			.AddCascadingValue("TreeSelectable", true)
			.Add(x => x.Text, "README.md")
			.Add(x => x.Selected, false));

		await cut.Find(".moka-tree-item__row").ClickAsync(new MouseEventArgs());
		cut.Render(p => p.Add(x => x.Selected, true));
		cut.Render(p => p.Add(x => x.Selected, false));

		Assert.Equal("false", cut.Find("[role=treeitem]").GetAttribute("aria-selected"));
	}

	// The chip only toggles while SelectedChanged is set. A handler that does not feed the value
	// back is still a one-way binding.
	[Fact]
	public async Task Chip_StaysSelected_WhenTheParentPassesTheSameValue()
	{
		bool? reported = null;
		IRenderedComponent<MokaChip> cut = Render<MokaChip>(p => p
			.Add(x => x.Text, "Chip")
			.Add(x => x.Selected, false)
			.Add(x => x.SelectedChanged, EventCallback.Factory.Create<bool>(this, v => reported = v)));

		await cut.Find("button").ClickAsync(new MouseEventArgs());
		cut.Render(p => p.Add(x => x.Selected, false));

		Assert.True(reported);
		Assert.Contains("moka-chip--selected", cut.Find("button").ClassList);
	}

	[Fact]
	public async Task Chip_FollowsANewValueFromTheParent()
	{
		IRenderedComponent<MokaChip> cut = Render<MokaChip>(p => p
			.Add(x => x.Text, "Chip")
			.Add(x => x.Selected, false)
			.Add(x => x.SelectedChanged, EventCallback.Factory.Create<bool>(this, _ => { })));

		await cut.Find("button").ClickAsync(new MouseEventArgs());
		cut.Render(p => p.Add(x => x.Selected, true));
		cut.Render(p => p.Add(x => x.Selected, false));

		Assert.DoesNotContain("moka-chip--selected", cut.Find("button").ClassList);
	}

	[Fact]
	public async Task Attribute_StaysSelected_WhenTheParentPassesTheSameValue()
	{
		IRenderedComponent<MokaAttribute> cut = Render<MokaAttribute>(p => p
			.Add(x => x.Label, "Status")
			.Add(x => x.Selectable, true)
			.Add(x => x.Selected, false));

		await cut.Find("button").ClickAsync(new MouseEventArgs());
		cut.Render(p => p.Add(x => x.Selected, false));

		Assert.Contains("moka-attr--selected", cut.Find("button").ClassList);
	}

	[Fact]
	public async Task Attribute_FollowsANewValueFromTheParent()
	{
		IRenderedComponent<MokaAttribute> cut = Render<MokaAttribute>(p => p
			.Add(x => x.Label, "Status")
			.Add(x => x.Selectable, true)
			.Add(x => x.Selected, false));

		await cut.Find("button").ClickAsync(new MouseEventArgs());
		cut.Render(p => p.Add(x => x.Selected, true));
		cut.Render(p => p.Add(x => x.Selected, false));

		Assert.DoesNotContain("moka-attr--selected", cut.Find("button").ClassList);
	}

	// The pill radius comes from the moka-attr--pill class. The component also wrote Rounded = Full,
	// which stayed behind as an inline radius after Pill went false.
	[Fact]
	public void Attribute_DropsThePillShape_WhenPillTurnsOff()
	{
		IRenderedComponent<MokaAttribute> cut = Render<MokaAttribute>(p => p.Add(x => x.Label, "Tag"));

		cut.Render(p => p.Add(x => x.Pill, false));

		IElement attribute = cut.Find(".moka-attr");
		Assert.DoesNotContain("moka-attr--pill", attribute.ClassList);
		Assert.DoesNotContain("border-radius", attribute.GetAttribute("style") ?? "", StringComparison.Ordinal);
	}

	[Fact]
	public async Task Carousel_StaysOnTheSlide_WhenTheParentPassesTheSameIndex()
	{
		IRenderedComponent<MokaCarousel> cut = RenderCarousel();

		await cut.Find(".moka-carousel-arrow--next").ClickAsync(new MouseEventArgs());
		cut.Render(p => p.Add(x => x.ActiveIndex, 0));

		Assert.Equal(1, ActiveCarouselDot(cut));
		Assert.Contains("translateX(-100%)", cut.Find(".moka-carousel-track").GetAttribute("style"), StringComparison.Ordinal);
	}

	[Fact]
	public async Task Carousel_FollowsANewIndexFromTheParent()
	{
		IRenderedComponent<MokaCarousel> cut = RenderCarousel();

		await cut.Find(".moka-carousel-arrow--next").ClickAsync(new MouseEventArgs());
		cut.Render(p => p.Add(x => x.ActiveIndex, 2));

		Assert.Equal(2, ActiveCarouselDot(cut));
	}

	[Fact]
	public async Task InfiniteCarousel_StaysOnTheSlide_WhenTheParentPassesTheSameIndex()
	{
		IRenderedComponent<MokaInfiniteCarousel> cut = RenderInfiniteCarousel();

		await cut.Find(".moka-infinite-carousel-arrow--next").ClickAsync(new MouseEventArgs());
		cut.Render(p => p.Add(x => x.ActiveIndex, 0));

		Assert.Equal(1, ActiveInfiniteCarouselDot(cut));
	}

	[Fact]
	public async Task InfiniteCarousel_FollowsANewIndexFromTheParent()
	{
		IRenderedComponent<MokaInfiniteCarousel> cut = RenderInfiniteCarousel();

		await cut.Find(".moka-infinite-carousel-arrow--next").ClickAsync(new MouseEventArgs());
		cut.Render(p => p.Add(x => x.ActiveIndex, 2));

		Assert.Equal(2, ActiveInfiniteCarouselDot(cut));
	}

	[Fact]
	public async Task ColorSwatch_KeepsThePick_WhenTheParentPassesTheSameColor()
	{
		IRenderedComponent<MokaColorSwatch> cut = Render<MokaColorSwatch>(p => p
			.Add(x => x.Colors, Swatches)
			.Add(x => x.SelectedColor, "#ff0000"));

		await cut.FindAll(".moka-color-swatch-item")[1].ClickAsync(new MouseEventArgs());
		cut.Render(p => p.Add(x => x.SelectedColor, "#ff0000"));

		Assert.Equal("#00ff00", cut.Find(".moka-color-swatch-preview-text").TextContent);
		Assert.Contains("moka-color-swatch-item--selected", cut.FindAll(".moka-color-swatch-item")[1].ClassList);
	}

	[Fact]
	public async Task ColorSwatch_FollowsANewColorFromTheParent()
	{
		IRenderedComponent<MokaColorSwatch> cut = Render<MokaColorSwatch>(p => p
			.Add(x => x.Colors, Swatches)
			.Add(x => x.SelectedColor, "#ff0000"));

		await cut.FindAll(".moka-color-swatch-item")[1].ClickAsync(new MouseEventArgs());
		cut.Render(p => p.Add(x => x.SelectedColor, "#0000ff"));

		Assert.Equal("#0000ff", cut.Find(".moka-color-swatch-preview-text").TextContent);
	}

	// The burst turns itself off. Reading Active again on every parent render fired it again.
	[Fact]
	public void Confetti_DoesNotFireAgain_WhenTheParentPassesTheSameValue()
	{
		IRenderedComponent<MokaConfetti> cut = RenderConfetti();

		cut.Render(p => p.Add(x => x.Active, true));

		Assert.Empty(cut.FindAll(".moka-confetti-particle"));
	}

	[Fact]
	public void Confetti_FiresAgain_WhenTheParentTurnsItOffAndOn()
	{
		IRenderedComponent<MokaConfetti> cut = RenderConfetti();

		cut.Render(p => p.Add(x => x.Active, false));
		cut.Render(p => p.Add(x => x.Active, true));

		Assert.Equal(5, cut.FindAll(".moka-confetti-particle").Count);
	}

	[Fact]
	public async Task SegmentedControl_KeepsTheChoice_WhenTheParentPassesTheSameValue()
	{
		IRenderedComponent<MokaSegmentedControl> cut = RenderSegmentedControl();

		await cut.FindAll("input[type=radio]")[1].ChangeAsync(new ChangeEventArgs { Value = "b" });
		cut.Render(p => p.Add(x => x.Value, "a"));

		Assert.Equal([false, true, false], cut.FindAll("input[type=radio]").Select(r => r.HasAttribute("checked")));
	}

	[Fact]
	public async Task SegmentedControl_FollowsANewValueFromTheParent()
	{
		IRenderedComponent<MokaSegmentedControl> cut = RenderSegmentedControl();

		await cut.FindAll("input[type=radio]")[1].ChangeAsync(new ChangeEventArgs { Value = "b" });
		cut.Render(p => p.Add(x => x.Value, "c"));

		Assert.Equal([false, false, true], cut.FindAll("input[type=radio]").Select(r => r.HasAttribute("checked")));
	}

	// Without IsDarkChanged the toggle did not even re-render: the base blocks renders that no
	// parameter change started.
	[Fact]
	public async Task ThemeToggle_KeepsTheClick_WhenTheParentPassesTheSameValue()
	{
		IRenderedComponent<MokaThemeToggle> cut = Render<MokaThemeToggle>(p => p.Add(x => x.IsDark, false));

		await cut.Find("button").ClickAsync(new MouseEventArgs());
		Assert.Contains("moka-theme-toggle--dark", cut.Find("button").ClassList);

		cut.Render(p => p.Add(x => x.IsDark, false));

		Assert.Contains("moka-theme-toggle--dark", cut.Find("button").ClassList);
		Assert.Equal("Switch to light mode", cut.Find("button").GetAttribute("title"));
	}

	[Fact]
	public async Task ThemeToggle_FollowsANewValueFromTheParent()
	{
		IRenderedComponent<MokaThemeToggle> cut = Render<MokaThemeToggle>(p => p.Add(x => x.IsDark, false));

		await cut.Find("button").ClickAsync(new MouseEventArgs());
		cut.Render(p => p.Add(x => x.IsDark, true));
		cut.Render(p => p.Add(x => x.IsDark, false));

		Assert.DoesNotContain("moka-theme-toggle--dark", cut.Find("button").ClassList);
	}

	private IRenderedComponent<MokaCarousel> RenderCarousel() => Render<MokaCarousel>(p => p
		.Add(x => x.ActiveIndex, 0)
		.AddChildContent<MokaCarouselSlide>(slide => slide.AddChildContent("One"))
		.AddChildContent<MokaCarouselSlide>(slide => slide.AddChildContent("Two"))
		.AddChildContent<MokaCarouselSlide>(slide => slide.AddChildContent("Three")));

	private static int ActiveCarouselDot(IRenderedComponent<MokaCarousel> cut) =>
		cut.FindAll(".moka-carousel-dot").ToList().FindIndex(d => d.ClassList.Contains("moka-carousel-dot--active"));

	private IRenderedComponent<MokaInfiniteCarousel> RenderInfiniteCarousel() => Render<MokaInfiniteCarousel>(p => p
		.Add(x => x.AutoPlay, false)
		.Add(x => x.ActiveIndex, 0)
		.AddChildContent<MokaInfiniteCarouselSlide>(slide => slide.AddChildContent("One"))
		.AddChildContent<MokaInfiniteCarouselSlide>(slide => slide.AddChildContent("Two"))
		.AddChildContent<MokaInfiniteCarouselSlide>(slide => slide.AddChildContent("Three")));

	private static int ActiveInfiniteCarouselDot(IRenderedComponent<MokaInfiniteCarousel> cut) =>
		cut.FindAll(".moka-infinite-carousel-dot").ToList()
			.FindIndex(d => d.ClassList.Contains("moka-infinite-carousel-dot--active"));

	// Waits for the burst to finish so the component has turned itself off.
	private IRenderedComponent<MokaConfetti> RenderConfetti()
	{
		IRenderedComponent<MokaConfetti> cut = Render<MokaConfetti>(p => p
			.Add(x => x.Active, true)
			.Add(x => x.ParticleCount, 5)
			.Add(x => x.Duration, 1));

		Assert.Equal(5, cut.FindAll(".moka-confetti-particle").Count);
		cut.WaitForAssertion(() => Assert.Empty(cut.FindAll(".moka-confetti-particle")), TimeSpan.FromSeconds(5));
		return cut;
	}

	private IRenderedComponent<MokaSegmentedControl> RenderSegmentedControl() => Render<MokaSegmentedControl>(p => p
		.Add(x => x.Value, "a")
		.AddChildContent<MokaSegment>(s => s.Add(x => x.Value, "a").Add(x => x.Text, "A"))
		.AddChildContent<MokaSegment>(s => s.Add(x => x.Value, "b").Add(x => x.Text, "B"))
		.AddChildContent<MokaSegment>(s => s.Add(x => x.Value, "c").Add(x => x.Text, "C")));
}
