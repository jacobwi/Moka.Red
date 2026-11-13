using AngleSharp.Dom;
using Bunit;
using Moka.Red.Data.Chart;
using Moka.Red.Tests.Shared;

namespace Moka.Red.Data.Tests.Components;

// The chart drew in a fixed 600 by 260 viewBox that kept its ratio, so a md chart wider than about 554 px scaled to the
// height cap and sat in the middle with empty bands beside it (in a 932 px card). The plot now stretches to
// fill the width, and what mustn't stretch (text, dots, line widths) is kept out of the stretch.
public class MokaLineChartFillTests : BunitContext
{
	private const string Css = "MokaLineChart.razor.css";

	private static readonly MokaLineSeries[] Rates = [new() { Name = "Down", Values = [0, 10, 20, 30, 40] }];

	[Fact]
	public void ThePlot_StretchesToFillItsCell()
	{
		IRenderedComponent<MokaLineChart> cut = Render<MokaLineChart>(p => p.Add(x => x.Series, Rates));

		IElement plot = cut.Find("svg.moka-linechart-plot");
		Assert.Equal("none", plot.GetAttribute("preserveAspectRatio"));
		Assert.Equal("40.00 0 560.00 234.00", plot.GetAttribute("viewBox"));

		IReadOnlyDictionary<string, string> rule = ScopedCss.Declarations(Css, ".moka-linechart-plot");
		Assert.Equal("100%", rule["width"]);
		Assert.Equal("100%", rule["height"]);
	}

	// The frame is the box the old svg had, so a chart that fit before is the same size now, only filled.
	[Fact]
	public void ByDefault_TheBoxKeepsItsProportion_UpToItsSize()
	{
		Assert.Equal("600 / 260", ScopedCss.Declarations(Css, ".moka-linechart-frame")["aspect-ratio"]);
		Assert.Equal("240px", ScopedCss.Declarations(Css, ".moka-linechart--md .moka-linechart-frame")["max-height"]);

		IRenderedComponent<MokaLineChart> cut = Render<MokaLineChart>(p => p.Add(x => x.Series, Rates));
		Assert.Null(cut.Find(".moka-linechart-frame").GetAttribute("style"));
		Assert.DoesNotContain("moka-linechart-frame--fixed", cut.Find(".moka-linechart-frame").ClassList);
	}

	[Fact]
	public void Height_FixesTheBox_WhateverTheWidth()
	{
		IRenderedComponent<MokaLineChart> cut = Render<MokaLineChart>(p => p
			.Add(x => x.Height, "230px")
			.Add(x => x.Series, Rates));

		IElement frame = cut.Find(".moka-linechart-frame");
		Assert.Equal("height: 230px", frame.GetAttribute("style"));
		Assert.Contains("moka-linechart-frame--fixed", frame.ClassList);

		IReadOnlyDictionary<string, string> fixedRule = ScopedCss.Declarations(Css, ".moka-linechart-frame--fixed");
		Assert.Equal("auto", fixedRule["aspect-ratio"]);
		Assert.Equal("none !important", fixedRule["max-height"]);
	}

	[Fact]
	public void AHeightThatCouldEndItsDeclaration_IsDropped()
	{
		IRenderedComponent<MokaLineChart> cut = Render<MokaLineChart>(p => p
			.Add(x => x.Height, "1px; color: red")
			.Add(x => x.Series, Rates));

		IElement frame = cut.Find(".moka-linechart-frame");
		Assert.Null(frame.GetAttribute("style"));
		Assert.DoesNotContain("moka-linechart-frame--fixed", frame.ClassList);
	}

	// Text in a stretched svg stretches with it, so every label is HTML placed by the same coordinates.
	[Fact]
	public void TheLabels_AreTextOutsideThePlot_PlacedWhereTheirLinesAre()
	{
		IRenderedComponent<MokaLineChart> cut = Render<MokaLineChart>(p => p
			.Add(x => x.YTicks, 1)
			.Add(x => x.Labels, ["Mon", "Tue", "Wed", "Thu", "Fri"])
			.Add(x => x.Series, Rates));

		Assert.Empty(cut.FindAll("svg text"));

		IReadOnlyList<IElement> y = cut.FindAll(".moka-linechart-yaxis > .moka-linechart-ytick");
		Assert.Equal(["0", "40"], y.Select(t => t.TextContent));
		Assert.Equal("top: 100%", y[0].GetAttribute("style"));
		Assert.Equal("top: 5.128%", y[1].GetAttribute("style"));

		IReadOnlyList<IElement> x = cut.FindAll(".moka-linechart-xaxis > .moka-linechart-xtick");
		Assert.Equal(["Mon", "Tue", "Wed", "Thu", "Fri"], x.Select(t => t.TextContent));
		Assert.Equal("left: 0%", x[0].GetAttribute("style"));
		Assert.Equal("left: 97.857%", x[^1].GetAttribute("style"));
		Assert.Equal("true", cut.Find(".moka-linechart-yaxis").GetAttribute("aria-hidden"));
	}

	[Fact]
	public void WithoutXLabels_TheFrameHasNoRowForThem()
	{
		IRenderedComponent<MokaLineChart> cut = Render<MokaLineChart>(p => p
			.Add(x => x.ShowXLabels, false)
			.Add(x => x.Series, Rates));

		Assert.Empty(cut.FindAll(".moka-linechart-xaxis"));
		Assert.Contains("moka-linechart-frame--no-x", cut.Find(".moka-linechart-frame").ClassList);
	}

	// A circle in a stretched plot is an ellipse. A zero-length stroke with round caps, drawn in screen pixels, isn't.
	[Fact]
	public void Points_AreRoundStrokesThatDontStretch()
	{
		IRenderedComponent<MokaLineChart> cut = Render<MokaLineChart>(p => p
			.Add(x => x.ShowPoints, true)
			.Add(x => x.Series, Rates));

		Assert.Empty(cut.FindAll("circle"));
		IElement point = cut.FindAll(".moka-linechart-point")[1];
		Assert.Equal(point.GetAttribute("x1"), point.GetAttribute("x2"));
		Assert.Equal(point.GetAttribute("y1"), point.GetAttribute("y2"));

		IReadOnlyDictionary<string, string> dot = ScopedCss.Declarations(Css, ".moka-linechart-point");
		Assert.Equal("round", dot["stroke-linecap"]);
		Assert.Equal("non-scaling-stroke", dot["vector-effect"]);
	}

	[Theory]
	[InlineData(".moka-linechart-line")]
	[InlineData(".moka-linechart-grid")]
	public void LinesKeepTheirWidth_InAStretchedPlot(string selector) =>
		Assert.Equal("non-scaling-stroke", ScopedCss.Declarations(Css, selector)["vector-effect"]);

	[Fact]
	public void TheEmptyText_IsTextOverThePlot()
	{
		IRenderedComponent<MokaLineChart> cut = Render<MokaLineChart>(p => p
			.Add(x => x.EmptyText, "Waiting for traffic")
			.Add(x => x.Series, [new MokaLineSeries { Name = "Down", Values = [double.NaN, double.NaN] }]));

		IElement empty = cut.Find(".moka-linechart-frame > .moka-linechart-empty");
		Assert.Equal("DIV", empty.TagName);
		Assert.Equal("plot", ScopedCss.Declarations(Css, ".moka-linechart-empty")["grid-area"]);
	}
}
