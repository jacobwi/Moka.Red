using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components.Web;
using Moka.Red.Primitives.ImageCompare;

namespace Moka.Red.Primitives.Tests.Components;

/// <summary>
///     The before/after comparison: both images, the divider as a slider, and the position the
///     keyboard and the drag both report.
/// </summary>
public class MokaImageCompareTests : BunitContext
{
	public MokaImageCompareTests() => JSInterop.Mode = JSRuntimeMode.Loose;

	[Fact]
	public void StacksBothImages_WithBadges()
	{
		IRenderedComponent<MokaImageCompare> cut = Render<MokaImageCompare>(p => p
			.Add(x => x.BeforeSrc, "before.png")
			.Add(x => x.AfterSrc, "after.png"));

		Assert.Equal("before.png", cut.Find(".moka-image-compare-before img").GetAttribute("src"));
		Assert.Equal("after.png", cut.Find(".moka-image-compare-after").GetAttribute("src"));
		Assert.Equal("Before", cut.Find(".moka-image-compare-badge--before").TextContent);
		Assert.Equal("After", cut.Find(".moka-image-compare-badge--after").TextContent);
		// The clip path that hides the before image past the divider hangs off this class
		Assert.Contains("moka-image-compare--horizontal", cut.Find(".moka-image-compare").ClassName,
			StringComparison.Ordinal);
	}

	[Fact]
	public void EmptyLabels_TakeTheBadgesOff()
	{
		IRenderedComponent<MokaImageCompare> cut = Render<MokaImageCompare>(p => p
			.Add(x => x.BeforeSrc, "before.png")
			.Add(x => x.AfterSrc, "after.png")
			.Add(x => x.BeforeLabel, string.Empty)
			.Add(x => x.AfterLabel, string.Empty));

		Assert.Empty(cut.FindAll(".moka-image-compare-badge"));
	}

	[Fact]
	public void TheDivider_IsASliderOverTheWholeRange()
	{
		IRenderedComponent<MokaImageCompare> cut = Render<MokaImageCompare>(p => p
			.Add(x => x.BeforeSrc, "before.png")
			.Add(x => x.AfterSrc, "after.png")
			.Add(x => x.Position, 25));

		IElement handle = cut.Find(".moka-image-compare-handle");
		Assert.Equal("slider", handle.GetAttribute("role"));
		Assert.Equal("0", handle.GetAttribute("aria-valuemin"));
		Assert.Equal("100", handle.GetAttribute("aria-valuemax"));
		Assert.Equal("25", handle.GetAttribute("aria-valuenow"));
	}

	[Fact]
	public async Task TheKeyboard_MovesTheDividerInSteps()
	{
		List<double> positions = [];
		IRenderedComponent<MokaImageCompare> cut = Render<MokaImageCompare>(p => p
			.Add(x => x.BeforeSrc, "before.png")
			.Add(x => x.AfterSrc, "after.png")
			.Add(x => x.Position, 50)
			.Add(x => x.PositionChanged, p => positions.Add(p)));

		IElement handle = cut.Find(".moka-image-compare-handle");
		await handle.KeyDownAsync(new KeyboardEventArgs { Key = "ArrowRight" });
		await handle.KeyDownAsync(new KeyboardEventArgs { Key = "ArrowRight", ShiftKey = true });
		await handle.KeyDownAsync(new KeyboardEventArgs { Key = "Home" });

		Assert.Equal([51, 61, 0], positions);
		Assert.Equal("0", cut.Find(".moka-image-compare-handle").GetAttribute("aria-valuenow"));
	}

	// A drag reports once, when it ends - OnDragged is what the JS module calls then.
	[Fact]
	public async Task AFinishedDrag_ReportsWhereItLanded()
	{
		List<double> positions = [];
		IRenderedComponent<MokaImageCompare> cut = Render<MokaImageCompare>(p => p
			.Add(x => x.BeforeSrc, "before.png")
			.Add(x => x.AfterSrc, "after.png")
			.Add(x => x.PositionChanged, p => positions.Add(p)));

		await cut.InvokeAsync(() => cut.Instance.OnDragged(30.5));

		Assert.Equal([30.5], positions);
		Assert.Contains("30.5", cut.Find(".moka-image-compare").GetAttribute("style"), StringComparison.Ordinal);
	}

	[Fact]
	public void APositionOutsideTheRange_IsClampedIntoIt()
	{
		IRenderedComponent<MokaImageCompare> cut = Render<MokaImageCompare>(p => p
			.Add(x => x.BeforeSrc, "before.png")
			.Add(x => x.AfterSrc, "after.png")
			.Add(x => x.Position, 200));

		Assert.Contains("100.00%", cut.Find(".moka-image-compare").GetAttribute("style"), StringComparison.Ordinal);
	}

	[Fact]
	public void VerticalOrientation_TurnsTheDivider()
	{
		IRenderedComponent<MokaImageCompare> cut = Render<MokaImageCompare>(p => p
			.Add(x => x.BeforeSrc, "before.png")
			.Add(x => x.AfterSrc, "after.png")
			.Add(x => x.Orientation, MokaImageCompareOrientation.Vertical));

		Assert.Contains("moka-image-compare--vertical", cut.Find(".moka-image-compare").ClassList);
		Assert.Equal("horizontal", cut.Find(".moka-image-compare-handle").GetAttribute("aria-orientation"));
	}
}
