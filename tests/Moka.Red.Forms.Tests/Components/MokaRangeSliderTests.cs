using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Moka.Red.Forms.Slider;

namespace Moka.Red.Forms.Tests.Components;

public class MokaRangeSliderTests : BunitContext
{
	// A thumb dragged past the other one is held back. When it was already held there, the held value
	// matched the last render, Blazor sent the browser nothing, and the thumb stayed where it was
	// dragged. The input has to render the dragged value once and then the held one, which is the
	// update that moves the thumb back.
	[Fact]
	public async Task AStartThumbDraggedPastTheEnd_IsPutBack()
	{
		List<double> changes = [];
		IRenderedComponent<MokaRangeSlider> cut = RenderSlider(20, 80, starts: changes);

		await Start(cut).InputAsync(new ChangeEventArgs { Value = "95" });
		Assert.Equal("80", Start(cut).GetAttribute("value"));

		List<string?> rendered = [];
		cut.OnMarkupUpdated += (_, _) => rendered.Add(Start(cut).GetAttribute("value"));
		await Start(cut).InputAsync(new ChangeEventArgs { Value = "97" });

		Assert.Contains("97", rendered);
		Assert.Equal("80", rendered[^1]);
		Assert.Equal([80, 80], changes);
		Assert.Equal("80", cut.Find(".moka-range-slider-value").TextContent);
	}

	[Fact]
	public async Task AnEndThumbDraggedPastTheStart_IsPutBack()
	{
		IRenderedComponent<MokaRangeSlider> cut = RenderSlider(20, 80);

		await End(cut).InputAsync(new ChangeEventArgs { Value = "5" });
		Assert.Equal("20", End(cut).GetAttribute("value"));

		List<string?> rendered = [];
		cut.OnMarkupUpdated += (_, _) => rendered.Add(End(cut).GetAttribute("value"));
		await End(cut).InputAsync(new ChangeEventArgs { Value = "3" });

		Assert.Contains("3", rendered);
		Assert.Equal("20", rendered[^1]);
	}

	// Only the start thumb can move once both sit at Max, but the end one is drawn on top and caught
	// every drag.
	[Fact]
	public void WithBothThumbsAtMax_TheStartThumbIsOnTop()
	{
		IRenderedComponent<MokaRangeSlider> atMax = RenderSlider(100, 100);
		IRenderedComponent<MokaRangeSlider> apart = RenderSlider(90, 100);

		Assert.Contains("moka-range-slider-input--on-top", Start(atMax).ClassList);
		Assert.DoesNotContain("moka-range-slider-input--on-top", Start(apart).ClassList);
	}

	[Fact]
	public void TheLabel_NamesTheGroup_AndEachThumbCarriesItsOwnName()
	{
		IRenderedComponent<MokaRangeSlider> cut = Render<MokaRangeSlider>(p => p.Add(x => x.Label, "Price"));

		IElement label = cut.Find("label.moka-field-label");
		IElement group = cut.Find(".moka-range-slider");
		Assert.Equal("group", group.GetAttribute("role"));
		Assert.Equal(label.Id, group.GetAttribute("aria-labelledby"));
		Assert.Equal("Price start", Start(cut).GetAttribute("aria-label"));
		Assert.Equal("Price end", End(cut).GetAttribute("aria-label"));

		// Clicking the label moves focus to the first thumb.
		Assert.Equal(Start(cut).Id, label.GetAttribute("for"));
	}

	[Fact]
	public void WithoutALabel_TheThumbsTakeTheConsumersName()
	{
		IRenderedComponent<MokaRangeSlider> named = Render<MokaRangeSlider>(p => p.AddUnmatched("aria-label", "Budget"));
		IRenderedComponent<MokaRangeSlider> unnamed = Render<MokaRangeSlider>();

		Assert.Equal("Budget", named.Find(".moka-range-slider").GetAttribute("aria-label"));
		Assert.Equal("Budget start", Start(named).GetAttribute("aria-label"));
		Assert.Equal("Budget end", End(named).GetAttribute("aria-label"));
		Assert.Equal("Range start", Start(unnamed).GetAttribute("aria-label"));
		Assert.False(unnamed.Find(".moka-range-slider").HasAttribute("aria-labelledby"));
	}

	[Fact]
	public void IdAndExtraAttributes_LandOnTheRoot()
	{
		IRenderedComponent<MokaRangeSlider> cut = Render<MokaRangeSlider>(p => p
			.Add(x => x.Id, "price-range")
			.AddUnmatched("data-test", "range"));

		IElement root = cut.Find(".moka-range-slider");
		Assert.Equal("price-range", root.Id);
		Assert.Equal("range", root.GetAttribute("data-test"));
	}

	[Fact]
	public void TheValues_AreJoinedWithTo()
	{
		IRenderedComponent<MokaRangeSlider> cut = RenderSlider(20, 80);

		Assert.Equal("to", cut.Find(".moka-range-slider-separator").TextContent);
	}

	// Where the thumbs meet, the end one was always on top and caught every drag, so in the upper
	// half the pair could not be pulled apart downwards with the mouse.
	[Theory]
	[InlineData(70, 70, true)]
	[InlineData(30, 30, false)]
	[InlineData(40, 70, false)]
	public void WhereTheThumbsMeet_TheOneWithRoomToMoveIsOnTop(double start, double end, bool startOnTop)
	{
		IRenderedComponent<MokaRangeSlider> cut = RenderSlider(start, end);

		Assert.Equal(startOnTop, Start(cut).ClassList.Contains("moka-range-slider-input--on-top"));
	}

	private IRenderedComponent<MokaRangeSlider> RenderSlider(double start, double end, List<double>? starts = null) =>
		Render<MokaRangeSlider>(p => p
			.Add(x => x.ValueStart, start)
			.Add(x => x.ValueEnd, end)
			.Add(x => x.ValueStartChanged, value => starts?.Add(value)));

	private static IElement Start(IRenderedComponent<MokaRangeSlider> cut) =>
		cut.Find(".moka-range-slider-input--start");

	private static IElement End(IRenderedComponent<MokaRangeSlider> cut) =>
		cut.Find(".moka-range-slider-input--end");
}
