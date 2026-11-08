using AngleSharp.Dom;
using Bunit;
using Moka.Red.Data.Chart;
using Moka.Red.Tests.Shared;

namespace Moka.Red.Data.Tests.Components;

// The sparkline's viewBox stretches to its box (preserveAspectRatio="none"), so the last-point circle turned into an
// ellipse and the line got thicker or thinner with the box. The dot is now drawn as MokaLineChart draws its points,
// and every stroke is in screen pixels.
public class MokaSparklineStretchTests : BunitContext
{
	private const string Css = "MokaSparkline.razor.css";

	[Fact]
	public void TheDot_IsAZeroLengthLine_NotACircle()
	{
		IRenderedComponent<MokaSparkline> cut = Render<MokaSparkline>(p => p.Add(x => x.Values, [1d, 4, 2, 5]));

		Assert.Empty(cut.FindAll("circle"));
		IElement dot = cut.Find("line.moka-sparkline-point");
		Assert.Equal("120.00", dot.GetAttribute("x1"));
		Assert.Equal(dot.GetAttribute("x1"), dot.GetAttribute("x2"));
		Assert.Equal("1.00", dot.GetAttribute("y1"));
		Assert.Equal(dot.GetAttribute("y1"), dot.GetAttribute("y2"));
		Assert.Equal("5", dot.QuerySelector("title")!.TextContent);
	}

	[Fact]
	public void TheDot_IsRoundAndKeepsItsSize()
	{
		IReadOnlyDictionary<string, string> dot = ScopedCss.Declarations(Css, ".moka-sparkline-point");

		Assert.Equal("round", dot["stroke-linecap"]);
		Assert.Equal("non-scaling-stroke", dot["vector-effect"]);
		Assert.Equal("var(--moka-chart-color, var(--moka-color-primary))", dot["stroke"]);
	}

	[Theory]
	[InlineData(".moka-sparkline-line")]
	[InlineData(".moka-sparkline-area")]
	public void TheLineAndTheAreaEdge_KeepTheirWidth(string selector) =>
		Assert.Equal("non-scaling-stroke", ScopedCss.Declarations(Css, selector)["vector-effect"]);

	// The circle's radius was the stroke width, but at least 3, so the dot is twice that across.
	[Theory]
	[InlineData(2d, "2.00", "6.00")]
	[InlineData(1d, "1.00", "6.00")]
	[InlineData(4d, "4.00", "8.00")]
	public void TheDot_IsAsWideAsTheOldCircle(double strokeWidth, string line, string dot)
	{
		IRenderedComponent<MokaSparkline> cut = Render<MokaSparkline>(p => p
			.Add(x => x.Values, [1d, 2])
			.Add(x => x.StrokeWidth, strokeWidth));

		Assert.Equal(line, cut.Find(".moka-sparkline-line").GetAttribute("stroke-width"));
		Assert.Equal(dot, cut.Find(".moka-sparkline-point").GetAttribute("stroke-width"));
	}

	[Fact]
	public void WithoutTheLastPoint_ThereIsNoDot()
	{
		IRenderedComponent<MokaSparkline> cut = Render<MokaSparkline>(p => p
			.Add(x => x.Values, [1d, 2])
			.Add(x => x.ShowLastPoint, false));

		Assert.Empty(cut.FindAll(".moka-sparkline-point"));
	}
}
