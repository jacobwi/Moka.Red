using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Moka.Red.Forms.RangeBar;

namespace Moka.Red.Forms.Tests.Components;

// What a chart's range strip needs from the range bar: something drawn in the track, a larger step for Shift, a range
// that moves whole, and a taller track.
public class MokaRangeBarTrackTests : BunitContext
{
	private readonly List<MokaRange> _values = [];

	public MokaRangeBarTrackTests() => JSInterop.Mode = JSRuntimeMode.Loose;

	[Fact]
	public void TrackContent_IsDrawnInsideTheTrack_BehindTheParts()
	{
		RenderFragment overview = builder => builder.AddMarkupContent(0, "<i class=\"overview\"></i>");
		IRenderedComponent<MokaRangeBar> cut = Bar(p => p
			.Add(x => x.TrackContent, overview)
			.Add(x => x.Parts, [new MokaRangePart(0, 10)]));

		IElement track = cut.Find(".moka-range-bar__track");
		Assert.Equal("moka-range-bar__track-content", track.Children[0].ClassName);
		Assert.NotNull(track.Children[0].QuerySelector(".overview"));
		Assert.Equal("true", track.GetAttribute("aria-hidden"));
	}

	[Fact]
	public void WithoutTrackContent_TheTrackIsAsBefore()
	{
		IRenderedComponent<MokaRangeBar> cut = Bar();

		Assert.Empty(cut.FindAll(".moka-range-bar__track-content"));
	}

	[Theory]
	[InlineData("ArrowRight", true, 44)]
	[InlineData("ArrowLeft", true, 0)]
	[InlineData("PageUp", false, 44)]
	[InlineData("ArrowRight", false, 21)]
	public void LargeStep_IsWhatShiftAndThePageKeysMove(string key, bool shift, double expected)
	{
		IRenderedComponent<MokaRangeBar> cut = Bar(p => p.Add(x => x.LargeStep, 24));

		Handle(cut, "start").KeyDown(new KeyboardEventArgs { Key = key, ShiftKey = shift });

		Assert.Equal(expected, _values[^1].Start);
	}

	[Theory]
	[InlineData(null)]
	[InlineData(0d)]
	[InlineData(double.NaN)]
	public void WithoutALargeStep_ShiftMovesTenSteps(double? large)
	{
		IRenderedComponent<MokaRangeBar> cut = Bar(p => p.Add(x => x.LargeStep, large));

		Handle(cut, "start").KeyDown(new KeyboardEventArgs { Key = "ArrowRight", ShiftKey = true });

		Assert.Equal(30, _values[^1].Start);
	}

	[Fact]
	public void MoveRange_LetsTheBrowserDragTheRangeWhole()
	{
		IRenderedComponent<MokaRangeBar> moving = Bar(p => p.Add(x => x.MoveRange, true));
		IRenderedComponent<MokaRangeBar> still = Bar();

		IElement range = moving.Find(".moka-range-bar__range");
		Assert.Equal("range", range.GetAttribute("data-moka-handle"));
		Assert.Contains("moka-range-bar__range--movable", range.ClassName, StringComparison.Ordinal);
		Assert.Null(still.Find(".moka-range-bar__range").GetAttribute("data-moka-handle"));
	}

	[Fact]
	public void ADisabledBar_DoesntOfferTheRangeToDrag()
	{
		IRenderedComponent<MokaRangeBar> cut = Bar(p => p.Add(x => x.MoveRange, true).Add(x => x.Disabled, true));

		Assert.DoesNotContain("--movable", cut.Find(".moka-range-bar__range").ClassName, StringComparison.Ordinal);
	}

	[Fact]
	public async Task ARangeMovedWhole_ReportsBothEnds()
	{
		IRenderedComponent<MokaRangeBar> cut = Bar(p => p.Add(x => x.MoveRange, true));

		await cut.InvokeAsync(() => cut.Instance.OnDragged(30, 70));

		Assert.Equal(new MokaRange(30, 70), _values[^1]);
	}

	[Fact]
	public void TrackHeight_SetsTheTrack_AndSomethingThatIsntALengthIsIgnored()
	{
		IRenderedComponent<MokaRangeBar> tall = Bar(p => p.Add(x => x.TrackHeight, "2.5rem"));
		IRenderedComponent<MokaRangeBar> bare = Bar(p => p.Add(x => x.TrackHeight, "40"));
		IRenderedComponent<MokaRangeBar> wrong = Bar(p => p.Add(x => x.TrackHeight, "1rem; color: red"));

		Assert.Contains("--moka-range-bar-track: 2.5rem", tall.Find(".moka-range-bar__bar").GetAttribute("style"), StringComparison.Ordinal);
		Assert.Contains("moka-range-bar__bar--track", tall.Find(".moka-range-bar__bar").ClassName, StringComparison.Ordinal);
		Assert.Contains("--moka-range-bar-track: 40px", bare.Find(".moka-range-bar__bar").GetAttribute("style"), StringComparison.Ordinal);
		Assert.DoesNotContain("--moka-range-bar-track", wrong.Find(".moka-range-bar__bar").GetAttribute("style"), StringComparison.Ordinal);
		Assert.DoesNotContain("--track", wrong.Find(".moka-range-bar__bar").ClassName, StringComparison.Ordinal);
	}

	private static IElement Handle(IRenderedComponent<MokaRangeBar> cut, string which) =>
		cut.Find($"[data-moka-handle='{which}']");

	private IRenderedComponent<MokaRangeBar> Bar(Action<ComponentParameterCollectionBuilder<MokaRangeBar>>? extra = null) =>
		Render<MokaRangeBar>(p =>
		{
			p.Add(x => x.Start, 20)
				.Add(x => x.End, 60)
				.Add(x => x.Label, "Pool")
				.Add(x => x.ValueChanged, EventCallback.Factory.Create<MokaRange>(this, _values.Add));
			extra?.Invoke(p);
		});
}
