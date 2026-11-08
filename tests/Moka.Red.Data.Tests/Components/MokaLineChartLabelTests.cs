using System.Globalization;
using AngleSharp.Dom;
using Bunit;
using Moka.Red.Data.Chart;
using Moka.Red.Tests.Shared;

namespace Moka.Red.Data.Tests.Components;

// The chart labelled every ceil(count / 12)th category whatever its width, so a narrow chart's labels ran into each
// other and a wide one's were sparse. The x-axis is now a query container: each label names the narrowest axis it fits
// on and hides below it, so the labels thin with the chart's own width without script.
public class MokaLineChartLabelTests : BunitContext
{
	private const string Css = "MokaLineChart.razor.css";
	private const string Prefix = "moka-linechart-xtick";

	private static readonly string[] Days = ["Mon", "Tue", "Wed", "Thu", "Fri"];

	[Fact]
	public void TheXAxis_IsAQueryContainer() =>
		Assert.Equal("moka-linechart-x / inline-size", ScopedCss.Declarations(Css, ".moka-linechart-xaxis")["container"]);

	[Fact]
	public void EveryWidthAClassNames_HasAContainerQuery()
	{
		IReadOnlyDictionary<string, int> hidden = ChartAxis.HiddenBelow(Css, "moka-linechart-x");
		Assert.NotEmpty(hidden);
		foreach ((string name, int width) in hidden)
		{
			Assert.Equal($"{Prefix}--w{width.ToString(CultureInfo.InvariantCulture)}", name);
		}

		HashSet<string> used = [];
		foreach (int count in new[] { 2, 3, 5, 7, 12, 24, 31, 60, 365, 5000 })
		{
			foreach (IElement label in Labels(Render<MokaLineChart>(p => p.Add(x => x.Series, Series(count)))))
			{
				used.UnionWith(label.ClassList.Where(c => c.StartsWith($"{Prefix}--w", StringComparison.Ordinal)));
			}
		}

		Assert.NotEmpty(used);
		Assert.All(used, name => Assert.Contains(name, hidden.Keys));
	}

	// Mon and Fri fit on any axis; Wed comes back at 8em, and Tue and Thu, which pack the axis closest, at 15em.
	[Fact]
	public void FiveShortLabels_ThinFromTheMiddle()
	{
		IRenderedComponent<MokaLineChart> cut = Render<MokaLineChart>(p => p
			.Add(x => x.Labels, Days)
			.Add(x => x.Series, Series(5)));

		IReadOnlyList<IElement> labels = Labels(cut);
		Assert.Equal(Days, labels.Select(l => l.TextContent));
		Assert.Equal([5, 15, 8, 15, null], labels.Select(l => ChartAxis.WidthOf(l, Prefix)));
		Assert.Equal("left: 0%", labels[0].GetAttribute("style"));
		Assert.Equal("left: 97.857%", labels[^1].GetAttribute("style"));
	}

	[Theory]
	[InlineData(2)]
	[InlineData(9)]
	[InlineData(13)]
	[InlineData(40)]
	[InlineData(366)]
	public void TheLastLabel_AlwaysShows(int count)
	{
		string[] names = [.. Enumerable.Range(0, count).Select(i => $"C{i.ToString(CultureInfo.InvariantCulture)}")];
		IRenderedComponent<MokaLineChart> cut = Render<MokaLineChart>(p => p
			.Add(x => x.Labels, names)
			.Add(x => x.Series, Series(count)));

		IElement last = Labels(cut)[^1];
		Assert.Equal(names[^1], last.TextContent);
		Assert.Null(ChartAxis.WidthOf(last, Prefix));
		Assert.Equal(names[0], Labels(cut)[0].TextContent);
	}

	// Every 4th label shows on a narrower axis than every 2nd, and every 2nd on a narrower one than the rest.
	[Fact]
	public void SparserLabels_ShowOnANarrowerAxis()
	{
		IRenderedComponent<MokaLineChart> cut = Render<MokaLineChart>(p => p
			.Add(x => x.Labels, Names(17))
			.Add(x => x.Series, Series(17)));

		Dictionary<string, int?> widths = Labels(cut).ToDictionary(l => l.TextContent, l => ChartAxis.WidthOf(l, Prefix));
		Assert.True(widths["D00"] <= widths["D08"]);
		Assert.True(widths["D08"] < widths["D04"]);
		Assert.True(widths["D04"] < widths["D02"]);
		Assert.True(widths["D02"] < widths["D01"]);
		Assert.Equal(widths["D01"], widths["D03"]);
		Assert.Null(widths["D16"]);
	}

	// D08 belongs with every 8th label, which shows on the narrowest axes, but it sits one step from the last label.
	[Fact]
	public void TheLabelJustBeforeTheLast_HidesWhileTheTwoWouldOverlap()
	{
		IRenderedComponent<MokaLineChart> cut = Render<MokaLineChart>(p => p
			.Add(x => x.Labels, Names(10))
			.Add(x => x.Series, Series(10)));

		Dictionary<string, int?> widths = Labels(cut).ToDictionary(l => l.TextContent, l => ChartAxis.WidthOf(l, Prefix));
		Assert.Equal(widths["D01"], widths["D08"]);
		Assert.True(widths["D08"] > widths["D04"]);
		Assert.Null(widths["D09"]);
	}

	[Fact]
	public void LongerLabels_NeedAWiderAxis()
	{
		IRenderedComponent<MokaLineChart> shortNames = Render<MokaLineChart>(p => p
			.Add(x => x.Labels, Days)
			.Add(x => x.Series, Series(5)));
		IRenderedComponent<MokaLineChart> longNames = Render<MokaLineChart>(p => p
			.Add(x => x.Labels, ["Monday 2 Nov", "Tuesday 3 Nov", "Wednesday 4 Nov", "Thursday 5 Nov", "Friday 6 Nov"])
			.Add(x => x.Series, Series(5)));

		Assert.True(ChartAxis.WidthOf(Labels(longNames)[1], Prefix) > ChartAxis.WidthOf(Labels(shortNames)[1], Prefix));
	}

	// A label that only a very wide axis has room for isn't written, so a long series stays a few labels.
	[Fact]
	public void ALongSeries_WritesAFewLabels_FromTheFirstToTheLast()
	{
		IRenderedComponent<MokaLineChart> cut = Render<MokaLineChart>(p => p
			.Add(x => x.ShowPoints, false)
			.Add(x => x.Series, Series(5000)));

		IReadOnlyList<IElement> labels = Labels(cut);
		Assert.InRange(labels.Count, 2, 40);
		Assert.Equal("0", labels[0].TextContent);
		Assert.Equal("4999", labels[^1].TextContent);
	}

	[Fact]
	public void ASingleCategory_HasOneLabel_ThatAlwaysShows()
	{
		IRenderedComponent<MokaLineChart> cut = Render<MokaLineChart>(p => p
			.Add(x => x.Labels, ["Now"])
			.Add(x => x.Series, Series(1)));

		IElement label = Assert.Single(Labels(cut));
		Assert.Equal("moka-linechart-tick moka-linechart-xtick", label.ClassName);
		Assert.Equal("left: 48.929%", label.GetAttribute("style"));
	}

	[Fact]
	public void TooltipLabel_WritesTheStartOfEachPointsTooltip()
	{
		IRenderedComponent<MokaLineChart> cut = Render<MokaLineChart>(p => p
			.Add(x => x.Labels, ["Mon", "Tue"])
			.Add(x => x.TooltipLabel, i => $"Sample {i.ToString(CultureInfo.InvariantCulture)}")
			.Add(x => x.ShowLastPoint, true)
			.Add(x => x.Series, [new MokaLineSeries { Name = "Down", Values = [10, 20] }]));

		Assert.Equal(["Sample 0 · Down: 10", "Sample 1 · Down: 20"],
			cut.FindAll(".moka-linechart-point title").Select(t => t.TextContent));
		Assert.Equal("Sample 1 · Down: 20", cut.Find(".moka-linechart-last title").TextContent);
		Assert.Equal(["Mon", "Tue"], Labels(cut).Select(l => l.TextContent));
	}

	[Fact]
	public void TooltipLabel_Empty_LeavesTheStartOff()
	{
		IRenderedComponent<MokaLineChart> cut = Render<MokaLineChart>(p => p
			.Add(x => x.Labels, ["Mon", "Tue"])
			.Add(x => x.TooltipLabel, i => i == 0 ? "" : "Later")
			.Add(x => x.Series, [new MokaLineSeries { Name = "Down", Values = [10, 20] }]));

		Assert.Equal(["Down: 10", "Later · Down: 20"], cut.FindAll(".moka-linechart-point title").Select(t => t.TextContent));
	}

	[Fact]
	public void WithoutTooltipLabel_TheAxisLabelStartsTheTooltip()
	{
		IRenderedComponent<MokaLineChart> cut = Render<MokaLineChart>(p => p
			.Add(x => x.Labels, ["Mon"])
			.Add(x => x.Series, [new MokaLineSeries { Name = "Down", Values = [10, 20] }]));

		Assert.Equal(["Mon · Down: 10", "Down: 20"], cut.FindAll(".moka-linechart-point title").Select(t => t.TextContent));
	}

	private static IReadOnlyList<IElement> Labels(IRenderedComponent<MokaLineChart> cut) =>
		cut.FindAll(".moka-linechart-xaxis > .moka-linechart-xtick");

	private static MokaLineSeries[] Series(int count) =>
		[new() { Name = "Down", Values = [.. Enumerable.Range(0, count).Select(i => (double)i)] }];

	private static string[] Names(int count) =>
		[.. Enumerable.Range(0, count).Select(i => $"D{i.ToString("00", CultureInfo.InvariantCulture)}")];
}
