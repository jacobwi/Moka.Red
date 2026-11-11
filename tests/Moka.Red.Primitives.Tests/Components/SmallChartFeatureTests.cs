using System.Globalization;
using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Moka.Red.Data.Chart;
using Moka.Red.Primitives.Meter;
using Moka.Red.Primitives.Stat;
using Moka.Red.Tests.Shared;

namespace Moka.Red.Primitives.Tests.Components;

public sealed class MokaSparklineMoveTests : BunitContext
{
	private const string Css = "MokaSparkline.razor.css";

	[Fact]
	public void Muted_GraysTheLine_AndDrawsTheLastStretchAndARingInTheColor()
	{
		IRenderedComponent<MokaSparkline> cut = Render<MokaSparkline>(p => p
			.Add(x => x.Values, [1d, 4, 2, 5])
			.Add(x => x.Muted, true));

		Assert.Contains("moka-sparkline--muted", cut.Find(".moka-sparkline").ClassList);
		Assert.Equal("M 80.00 26.50 L 120.00 1.00", cut.Find(".moka-sparkline-tail").GetAttribute("d"));
		Assert.Equal("10.00", cut.Find(".moka-sparkline-ring").GetAttribute("stroke-width"));
		Assert.Equal("var(--moka-chart-muted, var(--moka-color-on-surface-tertiary))",
			ScopedCss.Declarations(Css, ".moka-sparkline--muted .moka-sparkline-line")["stroke"]);
		Assert.Equal("var(--moka-chart-color, var(--moka-color-primary))",
			ScopedCss.Declarations(Css, ".moka-sparkline-tail")["stroke"]);
	}

	[Fact]
	public void WithoutMuted_ThereIsNoTailAndNoRing()
	{
		IRenderedComponent<MokaSparkline> cut = Render<MokaSparkline>(p => p.Add(x => x.Values, [1d, 4, 2, 5]));

		Assert.Empty(cut.FindAll(".moka-sparkline-tail"));
		Assert.Empty(cut.FindAll(".moka-sparkline-ring"));
	}

	[Fact]
	public void PastFourHundredEighty_ItDrawsTheMinMaxOfTwoHundredFortyRuns_KeepingEveryPeak()
	{
		double[] values = [.. Enumerable.Range(0, 10_000).Select(i => Math.Sin(i / 50d))];
		values[5_003] = 40;
		values[7_777] = -40;
		IRenderedComponent<MokaSparkline> cut = Render<MokaSparkline>(p => p
			.Add(x => x.Values, values)
			.Add(x => x.ShowArea, false));

		string path = cut.Find(".moka-sparkline-line").GetAttribute("d")!;
		int points = path.Split(['M', 'L'], StringSplitOptions.RemoveEmptyEntries).Length;
		Assert.InRange(points, 240, 482);
		Assert.Contains(" 1.00", path, StringComparison.Ordinal);
		Assert.Contains(" 35.00", path, StringComparison.Ordinal);
		Assert.Contains((5_003 * 120d / 9_999).ToString("F2", CultureInfo.InvariantCulture), path, StringComparison.Ordinal);
	}

	[Fact]
	public void AReducedLine_StillBreaksAtItsGaps()
	{
		double[] values = [.. Enumerable.Range(0, 1_000).Select(i => i is >= 400 and < 600 ? double.NaN : i)];
		IRenderedComponent<MokaSparkline> cut = Render<MokaSparkline>(p => p.Add(x => x.Values, values));

		Assert.Equal(2, cut.Find(".moka-sparkline-line").GetAttribute("d")!.Count(c => c == 'M'));
		Assert.Equal("999", cut.Find(".moka-sparkline-point title").TextContent);
	}
}

public sealed class MokaStatSparkTests : BunitContext
{
	private const string Css = "MokaStat.razor.css";

	[Fact]
	public void Spark_DrawsAMutedSparklineBesideTheValue()
	{
		IRenderedComponent<MokaStat> cut = Render<MokaStat>(p => p
			.Add(x => x.Value, "212 ms")
			.Add(x => x.Label, "p95 latency")
			.Add(x => x.Spark, [180d, 220, 205, 212]));

		IElement visual = cut.Find(".moka-stat__visual");
		Assert.Equal("moka-stat__visual moka-stat__visual--spark", visual.ClassName);
		IElement spark = visual.QuerySelector(".moka-sparkline")!;
		Assert.Contains("moka-sparkline--muted", spark.ClassList);
		Assert.Contains("moka-stat__spark", spark.ClassList);
		Assert.Equal("p95 latency, recent values", spark.GetAttribute("aria-label"));
		Assert.Empty(spark.QuerySelectorAll(".moka-sparkline-area"));
	}

	[Fact]
	public void SparkLabel_NamesTheSparkline()
	{
		IRenderedComponent<MokaStat> cut = Render<MokaStat>(p => p
			.Add(x => x.Value, "1.28M")
			.Add(x => x.SparkLabel, "Requests by hour")
			.Add(x => x.Spark, [1d, 2]));

		Assert.Equal("Requests by hour", cut.Find(".moka-sparkline").GetAttribute("aria-label"));
	}

	[Fact]
	public void Visual_WinsOverSpark()
	{
		IRenderedComponent<MokaStat> cut = Render<MokaStat>(p => p
			.Add(x => x.Value, "7")
			.Add(x => x.Spark, [1d, 2])
			.Add(x => x.Visual, (RenderFragment)(b => b.AddMarkupContent(0, "<i>mine</i>"))));

		Assert.Equal("moka-stat__visual", cut.Find(".moka-stat__visual").ClassName);
		Assert.Empty(cut.FindAll(".moka-sparkline"));
	}

	[Theory]
	[InlineData(4.2, "moka-stat__trend moka-stat__trend--up moka-stat__trend--bad", " worse")]
	[InlineData(-8, "moka-stat__trend moka-stat__trend--down moka-stat__trend--good", " better")]
	public void UpIsGoodOff_ColorsTheNews_AndSaysIt(double trend, string classes, string word)
	{
		IRenderedComponent<MokaStat> cut = Render<MokaStat>(p => p
			.Add(x => x.Value, "212 ms")
			.Add(x => x.Trend, trend)
			.Add(x => x.UpIsGood, false));

		IElement trendRow = cut.Find(".moka-stat__trend");
		Assert.Equal(classes, trendRow.ClassName);
		Assert.Equal(word, trendRow.QuerySelector(".moka-visually-hidden")!.TextContent);
	}

	[Fact]
	public void UpIsGood_ByDefault_AddsNothing()
	{
		IRenderedComponent<MokaStat> cut = Render<MokaStat>(p => p
			.Add(x => x.Value, "1.28M")
			.Add(x => x.Trend, 4.2));

		Assert.Equal("moka-stat__trend moka-stat__trend--up", cut.Find(".moka-stat__trend").ClassName);
		Assert.Empty(cut.FindAll(".moka-visually-hidden"));
	}

	[Fact]
	public void TheNewsColors_OutrankTheArrowColors()
	{
		Assert.Equal("var(--moka-color-error)", ScopedCss.Declarations(Css, ".moka-stat__trend.moka-stat__trend--bad")["color"]);
		Assert.Equal("var(--moka-color-success)", ScopedCss.Declarations(Css, ".moka-stat__trend.moka-stat__trend--good")["color"]);
	}
}

public sealed class MokaMeterBulletTests : BunitContext
{
	private const string Css = "MokaMeter.razor.css";

	private static readonly MokaMeterSegment[] Ranges =
		[new(0, 60, "ignored"), new(60, 85, "ignored"), new(85, 100, "ignored")];

	[Fact]
	public void Target_DrawsTheValueOverGrayRanges_WithATickAtTheTarget()
	{
		IRenderedComponent<MokaMeter> cut = Render<MokaMeter>(p => p
			.Add(x => x.Value, 62d)
			.Add(x => x.Target, 70d)
			.Add(x => x.Label, "CPU")
			.Add(x => x.Segments, Ranges));

		Assert.Contains("moka-meter--bullet", cut.Find(".moka-meter").ClassList);
		Assert.Equal("moka-meter__track moka-meter__track--bullet", cut.Find(".moka-meter__track").ClassName);
		IReadOnlyList<IElement> bands = cut.FindAll(".moka-meter__band");
		Assert.Equal(["left: 0%; width: 60%", "left: 60%; width: 25%", "left: 85%; width: 15%"], bands.Select(b => b.GetAttribute("style")));
		Assert.Contains("moka-meter__band--3", bands[2].ClassList);
		Assert.Equal("width: 62%", cut.Find(".moka-meter__fill--bullet").GetAttribute("style"));
		Assert.Equal("left: 70%", cut.Find(".moka-meter__target").GetAttribute("style"));
		Assert.Equal("62, target 70", cut.Find(".moka-meter").GetAttribute("aria-valuetext"));
		Assert.Empty(cut.FindAll(".moka-meter__segments"));
	}

	[Fact]
	public void Target_WithoutSegments_IsOneRange()
	{
		IRenderedComponent<MokaMeter> cut = Render<MokaMeter>(p => p
			.Add(x => x.Value, 40d)
			.Add(x => x.Target, 150d)
			.Add(x => x.Max, 200d));

		Assert.Equal("moka-meter__band moka-meter__band--1", Assert.Single(cut.FindAll(".moka-meter__band")).ClassName);
		Assert.Equal("left: 75%", cut.Find(".moka-meter__target").GetAttribute("style"));
	}

	[Fact]
	public void Row_PutsNameBarAndFigureOnOneLine()
	{
		IRenderedComponent<MokaMeter> cut = Render<MokaMeter>(p => p
			.Add(x => x.Value, 41d)
			.Add(x => x.Max, 64d)
			.Add(x => x.Label, "System")
			.Add(x => x.Figure, "41 of 64 GB")
			.Add(x => x.Layout, MokaMeterLayout.Row));

		IElement root = cut.Find(".moka-meter");
		Assert.Contains("moka-meter--row", root.ClassList);
		Assert.Equal(["moka-meter__name", "moka-meter__track", "moka-meter__value"], root.Children.Select(c => c.ClassName));
		Assert.Equal("41 of 64 GB", cut.Find(".moka-meter__value").TextContent);
		Assert.Empty(cut.FindAll(".moka-meter__header"));
		Assert.Equal("41 of 64 GB", root.GetAttribute("aria-valuetext"));
	}

	[Fact]
	public void PastWarnAt_TheFillTurnsAmber_WithAnIconAndAWord()
	{
		IRenderedComponent<MokaMeter> cut = Render<MokaMeter>(p => p
			.Add(x => x.Value, 3.4)
			.Add(x => x.Max, 4d)
			.Add(x => x.Format, "0.0")
			.Add(x => x.Label, "Backups")
			.Add(x => x.WarnAt, 0.8)
			.Add(x => x.CriticalAt, 0.95));

		IElement root = cut.Find(".moka-meter");
		Assert.Contains("moka-meter--warn", root.ClassList);
		Assert.Equal("Nearly full", cut.Find(".moka-meter__state").TextContent);
		Assert.Single(cut.FindAll(".moka-meter__name svg"));
		Assert.Equal("3.4, Nearly full", root.GetAttribute("aria-valuetext"));
	}

	[Fact]
	public void PastCriticalAt_ItSaysFull_InTheErrorColor()
	{
		IRenderedComponent<MokaMeter> cut = Render<MokaMeter>(p => p
			.Add(x => x.Value, 99d)
			.Add(x => x.WarnAt, 0.8)
			.Add(x => x.CriticalAt, 0.95)
			.Add(x => x.CriticalText, "Out of room"));

		Assert.Contains("moka-meter--critical", cut.Find(".moka-meter").ClassList);
		Assert.Equal("Out of room", cut.Find(".moka-meter__state").TextContent);
		Assert.Equal("var(--moka-color-error)", ScopedCss.Declarations(Css, ".moka-meter.moka-meter--critical .moka-meter__fill")["background"]);
	}

	[Fact]
	public void UnderWarnAt_NothingChanges()
	{
		IRenderedComponent<MokaMeter> cut = Render<MokaMeter>(p => p
			.Add(x => x.Value, 20d)
			.Add(x => x.Label, "Scratch")
			.Add(x => x.WarnAt, 0.8));

		Assert.Equal("moka-meter moka-fill-width moka-meter--md moka-meter--primary", cut.Find(".moka-meter").ClassName);
		Assert.Null(cut.Find(".moka-meter").GetAttribute("aria-valuetext"));
	}

	[Fact]
	public void RowsInAMeterList_ShareItsColumnsThroughSubgrid()
	{
		const string row = ".moka-meter-rows > .moka-meter--row";

		Assert.Equal(["subgrid", "minmax(0, 1fr) max-content"], ScopedCss.Values(Css, row, "grid-template-columns"));
		Assert.Equal("1 / -1", ScopedCss.Declarations(Css, row)["grid-column"]);
	}
}
