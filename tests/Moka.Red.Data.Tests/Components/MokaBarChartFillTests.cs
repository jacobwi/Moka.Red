using System.Globalization;
using AngleSharp.Dom;
using Bunit;
using Moka.Red.Core.Enums;
using Moka.Red.Data.Chart;
using Moka.Red.Tests.Shared;

namespace Moka.Red.Data.Tests.Components;

// The bar chart drew in a fixed 600 by 260 viewBox that kept its ratio, so a wide, short panel showed empty bands
// beside it. It now does what MokaLineChart does since 0.3.7: the plot stretches to fill the width, and the labels are
// text beside and under it.
public class MokaBarChartFillTests : BunitContext
{
	private const string Css = "MokaBarChart.razor.css";
	private const string Prefix = "moka-barchart-xtick";

	private static readonly string[] Quarters = ["Q1", "Q2", "Q3", "Q4"];
	private static readonly MokaBarSeries[] Sales = [new() { Name = "Sales", Values = [24, 31, 28, 40] }];

	[Fact]
	public void ThePlot_StretchesToFillItsCell()
	{
		IRenderedComponent<MokaBarChart> cut = Render<MokaBarChart>(p => p.Add(x => x.Series, Sales));

		IElement plot = cut.Find("svg.moka-barchart-plot");
		Assert.Equal("none", plot.GetAttribute("preserveAspectRatio"));
		Assert.Equal("40.00 0 560.00 234.00", plot.GetAttribute("viewBox"));

		IReadOnlyDictionary<string, string> rule = ScopedCss.Declarations(Css, ".moka-barchart-plot");
		Assert.Equal("100%", rule["width"]);
		Assert.Equal("100%", rule["height"]);
	}

	// The frame is the box the old svg had, so a chart that fit before is the same size now, only filled.
	[Fact]
	public void ByDefault_TheBoxKeepsItsProportion_UpToItsSize()
	{
		Assert.Equal("600 / 260", ScopedCss.Declarations(Css, ".moka-barchart-frame")["aspect-ratio"]);
		Assert.Equal("100%", ScopedCss.Declarations(Css, ".moka-barchart-frame")["width"]);
		Assert.Equal("160px", ScopedCss.Declarations(Css, ".moka-barchart--sm .moka-barchart-frame")["max-height"]);
		Assert.Equal("240px", ScopedCss.Declarations(Css, ".moka-barchart--md .moka-barchart-frame")["max-height"]);
		Assert.Equal("320px", ScopedCss.Declarations(Css, ".moka-barchart--lg .moka-barchart-frame")["max-height"]);

		IRenderedComponent<MokaBarChart> cut = Render<MokaBarChart>(p => p.Add(x => x.Series, Sales));
		Assert.Null(cut.Find(".moka-barchart-frame").GetAttribute("style"));
		Assert.Equal("moka-barchart-frame", cut.Find(".moka-barchart-frame").ClassName);
	}

	[Fact]
	public void Height_FixesTheBox_WhateverTheWidth()
	{
		IRenderedComponent<MokaBarChart> cut = Render<MokaBarChart>(p => p
			.Add(x => x.Height, "180px")
			.Add(x => x.Series, Sales));

		IElement frame = cut.Find(".moka-barchart-frame");
		Assert.Equal("height: 180px", frame.GetAttribute("style"));
		Assert.Contains("moka-barchart-frame--fixed", frame.ClassList);

		IReadOnlyDictionary<string, string> fixedRule = ScopedCss.Declarations(Css, ".moka-barchart-frame--fixed");
		Assert.Equal("auto", fixedRule["aspect-ratio"]);
		Assert.Equal("none !important", fixedRule["max-height"]);
	}

	[Fact]
	public void AHeightThatCouldEndItsDeclaration_IsDropped()
	{
		IRenderedComponent<MokaBarChart> cut = Render<MokaBarChart>(p => p
			.Add(x => x.Height, "1px; color: red")
			.Add(x => x.Series, Sales));

		IElement frame = cut.Find(".moka-barchart-frame");
		Assert.Null(frame.GetAttribute("style"));
		Assert.DoesNotContain("moka-barchart-frame--fixed", frame.ClassList);
	}

	// Text in a stretched svg stretches with it, so every label is HTML placed by the same coordinates.
	[Fact]
	public void TheLabels_AreTextOutsideThePlot_PlacedWhereTheirBarsAre()
	{
		IRenderedComponent<MokaBarChart> cut = Render<MokaBarChart>(p => p
			.Add(x => x.YTicks, 1)
			.Add(x => x.Labels, Quarters)
			.Add(x => x.Series, Sales));

		Assert.Empty(cut.FindAll("svg text"));

		IReadOnlyList<IElement> y = cut.FindAll(".moka-barchart-yaxis > .moka-barchart-ytick");
		Assert.Equal(["0", "40"], y.Select(t => t.TextContent));
		Assert.Equal("top: 100%", y[0].GetAttribute("style"));
		Assert.Equal("top: 5.128%", y[1].GetAttribute("style"));
		Assert.Equal("40", cut.Find(".moka-barchart-ysizer").TextContent);
		Assert.Equal("true", cut.Find(".moka-barchart-yaxis").GetAttribute("aria-hidden"));

		// Each label sits under the middle of its slot: slot 137 wide, from 40 in a 560-wide box.
		IReadOnlyList<IElement> x = cut.FindAll(".moka-barchart-xaxis > .moka-barchart-xtick");
		Assert.Equal(Quarters, x.Select(t => t.TextContent));
		Assert.Equal("left: 12.232%", x[0].GetAttribute("style"));
		Assert.Equal("left: 85.625%", x[^1].GetAttribute("style"));
		Assert.Equal("true", cut.Find(".moka-barchart-xaxis").GetAttribute("aria-hidden"));
	}

	[Fact]
	public void TheGridLines_KeepTheirWidth_AndSpanThePlot()
	{
		IRenderedComponent<MokaBarChart> cut = Render<MokaBarChart>(p => p.Add(x => x.Series, Sales));

		IReadOnlyList<IElement> grid = cut.FindAll(".moka-barchart-grid");
		Assert.Equal(5, grid.Count);
		Assert.All(grid, line => Assert.Equal("40.00", line.GetAttribute("x1")));
		Assert.All(grid, line => Assert.Equal("588.00", line.GetAttribute("x2")));
		Assert.Equal("non-scaling-stroke", ScopedCss.Declarations(Css, ".moka-barchart-grid")["vector-effect"]);
	}

	[Fact]
	public void TheBars_AreWhereTheyWere()
	{
		IRenderedComponent<MokaBarChart> cut = Render<MokaBarChart>(p => p
			.Add(x => x.Series, [new MokaBarSeries { Name = "A", Values = [4] }]));

		Assert.Equal(
			"M 122.20 234.00 L 122.20 15.00 Q 122.20 12.00 125.20 12.00 L 502.80 12.00 Q 505.80 12.00 505.80 15.00 L 505.80 234.00 Z",
			cut.Find(".moka-barchart-bar").GetAttribute("d"));
	}

	[Fact]
	public void Stacked_SeriesColors_AndTooltips_StillWork()
	{
		IRenderedComponent<MokaBarChart> cut = Render<MokaBarChart>(p => p
			.Add(x => x.Stacked, true)
			.Add(x => x.Labels, ["Q1", "Q2"])
			.Add(x => x.Series,
			[
				new MokaBarSeries { Name = "Direct", Values = [12, 14] },
				new MokaBarSeries { Name = "Referral", Values = [6, 5], Color = MokaColor.Info }
			]));

		IReadOnlyList<IElement> bars = cut.FindAll(".moka-barchart-bar");
		Assert.Equal(4, bars.Count);
		Assert.Equal(["Q1 · Direct: 12", "Q1 · Referral: 6", "Q2 · Direct: 14", "Q2 · Referral: 5"],
			bars.Select(b => b.QuerySelector("title")!.TextContent));
		Assert.Contains("moka-chart-color-primary", bars[0].ClassList);
		Assert.Contains("moka-chart-color-info", bars[1].ClassList);
		// The second series starts where the first ends, on the same column
		Assert.Equal(bars[0].GetAttribute("d")!.Split(' ')[1], bars[1].GetAttribute("d")!.Split(' ')[1]);
		// Stacked, the axis reaches the tallest pile
		Assert.Equal("19", cut.FindAll(".moka-barchart-ytick")[^1].TextContent);
	}

	[Fact]
	public void TheAccessibleLabel_IsKept()
	{
		IRenderedComponent<MokaBarChart> unnamed = Render<MokaBarChart>(p => p.Add(x => x.Series, Sales));
		Assert.Equal("img", unnamed.Find(".moka-barchart").GetAttribute("role"));
		Assert.Equal("Bar chart", unnamed.Find(".moka-barchart").GetAttribute("aria-label"));

		IRenderedComponent<MokaBarChart> named = Render<MokaBarChart>(p => p
			.Add(x => x.Label, "Sales by quarter")
			.Add(x => x.Series, Sales));
		Assert.Equal("Sales by quarter", named.Find(".moka-barchart").GetAttribute("aria-label"));
	}

	[Fact]
	public void WithoutXLabels_TheFrameHasNoRowForThem()
	{
		IRenderedComponent<MokaBarChart> cut = Render<MokaBarChart>(p => p
			.Add(x => x.ShowXLabels, false)
			.Add(x => x.Series, Sales));

		Assert.Empty(cut.FindAll(".moka-barchart-xaxis"));
		Assert.Contains("moka-barchart-frame--no-x", cut.Find(".moka-barchart-frame").ClassList);
	}

	[Fact]
	public void WithoutSeries_DrawsNoGridOrBars()
	{
		IRenderedComponent<MokaBarChart> cut = Render<MokaBarChart>();

		Assert.Empty(cut.FindAll(".moka-barchart-grid"));
		Assert.Empty(cut.FindAll(".moka-barchart-bar"));
		Assert.Empty(cut.FindAll(".moka-barchart-tick"));
	}

	// The labels no longer shrink with the chart, so they thin with the axis's width as the line chart's do.
	[Fact]
	public void TheXLabels_ThinWithTheAxisWidth()
	{
		Assert.Equal("moka-barchart-x / inline-size", ScopedCss.Declarations(Css, ".moka-barchart-xaxis")["container"]);

		IReadOnlyDictionary<string, int> hidden = ChartAxis.HiddenBelow(Css, "moka-barchart-x");
		Assert.NotEmpty(hidden);
		foreach (int count in new[] { 2, 4, 12, 31, 365 })
		{
			string[] names = [.. Enumerable.Range(0, count).Select(i => $"C{i.ToString(CultureInfo.InvariantCulture)}")];
			IRenderedComponent<MokaBarChart> cut = Render<MokaBarChart>(p => p
				.Add(x => x.Labels, names)
				.Add(x => x.Series, [new MokaBarSeries { Name = "A", Values = [.. Enumerable.Repeat(1d, count)] }]));

			IReadOnlyList<IElement> labels = cut.FindAll(".moka-barchart-xtick");
			Assert.Equal(names[0], labels[0].TextContent);
			Assert.Equal(names[^1], labels[^1].TextContent);
			Assert.Null(ChartAxis.WidthOf(labels[^1], Prefix));
			Assert.All(labels.Take(labels.Count - 1), label =>
				Assert.Contains($"{Prefix}--w{ChartAxis.WidthOf(label, Prefix)!.Value.ToString(CultureInfo.InvariantCulture)}", hidden.Keys));
		}
	}

	[Fact]
	public void BlankLabels_TakeNoPlace_AndStartNoTooltip()
	{
		string[] names = ["Q1", "", "", "Q2", "", ""];
		IRenderedComponent<MokaBarChart> cut = Render<MokaBarChart>(p => p
			.Add(x => x.Labels, names)
			.Add(x => x.Series, [new MokaBarSeries { Name = "A", Values = [.. Enumerable.Repeat(1d, 6)] }]));

		IReadOnlyList<IElement> labels = cut.FindAll(".moka-barchart-xtick");
		Assert.Equal(["Q1", "Q2"], labels.Select(l => l.TextContent));
		Assert.Null(ChartAxis.WidthOf(labels[^1], Prefix));
		Assert.Equal("A: 1", cut.FindAll("title")[1].TextContent);
	}
}
