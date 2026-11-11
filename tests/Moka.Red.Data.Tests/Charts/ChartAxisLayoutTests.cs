using System.Globalization;
using System.Text.RegularExpressions;
using Moka.Red.Data.Chart;

namespace Moka.Red.Data.Tests.Charts;

/// <summary>
///     The label layout without script: for crafted label sets, the widths at which an axis leaves each stage and each
///     label hides, worked out by hand; and for many label sets, a simulation of the stylesheet at every width that
///     finds no two shown labels overlapping by the estimates the layout itself uses.
/// </summary>
public sealed partial class ChartAxisLayoutTests
{
	private static readonly (string, double)[] FiveFourLetters =
		[("AAAA", 0), ("BBBB", 0.25), ("CCCC", 0.5), ("DDDD", 0.75), ("EEEE", 1)];

	[Theory]
	[InlineData(0, 5)]
	[InlineData(4.9, 5)]
	[InlineData(5, 5)]
	[InlineData(5.01, 6)]
	[InlineData(119.9, 120)]
	[InlineData(120.01, null)]
	[InlineData(double.PositiveInfinity, null)]
	[InlineData(double.NaN, null)]
	public void WidthFor_RoundsUpToTheTable(double need, int? width) => Assert.Equal(width, ChartAxisLayout.WidthFor(need));

	[Fact]
	public void PairNeed_CountsALabelHeldInFromTheEnd()
	{
		AxisSpot first = new(0, "AAAA", 0, 1.32, 0);
		AxisSpot second = new(1, "BBBB", 0.25, 1.32, 0);

		Assert.Equal(3.64 / 0.25, ChartAxisLayout.PairNeed(first, second, false), 6);
		Assert.Equal((3.64 + 1.32) / 0.25, ChartAxisLayout.PairNeed(first, second, true), 6);
	}

	// Four-letter labels at five points: flat needs 19.84 em (the first label held in from the start) and stagger 9.92 em.
	// At 2.64 em they are too short to be worth turning, so below 24 em the axis staggers and below 10 em they thin flat,
	// each clamped to where the stagger ends.
	[Fact]
	public void Auto_FiveFourLetterLabels_StaggerBelow24_ThenThinFlat()
	{
		CategoryAxisLayout layout = ChartAxisLayout.Categories(FiveFourLetters, MokaAxisLabelFit.Auto, 2, null);

		Assert.StartsWith("moka-chart-plot__x--fit-auto moka-chart-plot__x--stagger-at-w24", layout.AxisClass, StringComparison.Ordinal);
		Assert.DoesNotContain("rotate", layout.AxisClass, StringComparison.Ordinal);
		Assert.DoesNotContain("--_rot-h:", layout.AxisStyle ?? string.Empty, StringComparison.Ordinal);
		int flatBelow = int.Parse(FlatAt().Match(layout.AxisClass).Groups[1].Value, CultureInfo.InvariantCulture);
		Assert.All(layout.Labels, label => Assert.True(WidthOf(label.Class) <= flatBelow, label.Class));
	}

	// Names longer than two rotated footprints still turn: they gain room at an angle that thinning flat would cost.
	[Fact]
	public void Auto_LongNames_RotateBeforeTheyThin()
	{
		(string, double)[] labels = [.. Enumerable.Range(0, 5).Select(i => ($"name-{i}.example", i / 4d))];

		CategoryAxisLayout layout = ChartAxisLayout.Categories(labels, MokaAxisLabelFit.Auto, 2, null);

		Assert.Contains("moka-chart-plot__x--rotate-at-w", layout.AxisClass, StringComparison.Ordinal);
		Assert.Contains("--_rot-h:", layout.AxisStyle, StringComparison.Ordinal);
	}

	[Fact]
	public void Thin_FiveFourLetterLabels_HideByRank_WithNoStages()
	{
		CategoryAxisLayout layout = ChartAxisLayout.Categories(FiveFourLetters, MokaAxisLabelFit.Thin, 2, null);

		Assert.Equal("moka-chart-plot__x--fit-thin", layout.AxisClass);
		Assert.Equal(["--w8", "--w24", "--w10", "--w24", ""], layout.Labels.Select(label => HideOf(label.Class)));
	}

	// Short labels gain nothing from turning: after the stagger they thin flat, each clamped to where the stagger ends.
	[Fact]
	public void Auto_ShortLabels_StaggerThenThinFlat_WithoutRotating()
	{
		(string, double)[] labels = [.. Enumerable.Range(0, 9).Select(i => ($"{i}", i / 8d))];

		CategoryAxisLayout layout = ChartAxisLayout.Categories(labels, MokaAxisLabelFit.Auto, 2, null);

		Assert.DoesNotContain("rotate", layout.AxisClass, StringComparison.Ordinal);
		Assert.Contains("moka-chart-plot__x--stagger-at-w", layout.AxisClass, StringComparison.Ordinal);
		Assert.Contains("moka-chart-plot__x--flat-at-w", layout.AxisClass, StringComparison.Ordinal);
		int flatBelow = int.Parse(FlatAt().Match(layout.AxisClass).Groups[1].Value, CultureInfo.InvariantCulture);
		Assert.All(layout.Labels, label => Assert.True(WidthOf(label.Class) <= flatBelow, label.Class));
	}

	[Fact]
	public void Rotate_GoesStraightFromFlatTo45Degrees()
	{
		CategoryAxisLayout layout = ChartAxisLayout.Categories(FiveFourLetters, MokaAxisLabelFit.Rotate, 2, null);

		Assert.Equal("moka-chart-plot__x--fit-rotate moka-chart-plot__x--rotate-at-w24", layout.AxisClass);
		Assert.All(layout.Labels, label => Assert.Contains("--_rw:", label.Style, StringComparison.Ordinal));
	}

	[Fact]
	public void Stagger_StaggersBelowFlat_ThenThinsFlat()
	{
		CategoryAxisLayout layout = ChartAxisLayout.Categories(FiveFourLetters, MokaAxisLabelFit.Stagger, 2, null);

		Assert.Equal("moka-chart-plot__x--fit-stagger moka-chart-plot__x--stagger-at-w24 moka-chart-plot__x--flat-at-w10",
			layout.AxisClass);
		Assert.All(layout.Labels, label => Assert.True(WidthOf(label.Class) <= 10, label.Class));
	}

	[Fact]
	public void Wrap_WrapsInTheSlot_ThenThinsFlat_AndKeepsTheWholeText()
	{
		(string, double)[] regions = [("North America West", 0.1), ("Europe North", 0.3), ("Asia Pacific East", 0.5), ("Oceania", 0.7), ("Middle East", 0.9)];

		CategoryAxisLayout layout = ChartAxisLayout.Categories(regions, MokaAxisLabelFit.Wrap, 2, null);

		Assert.Matches(@"--fit-wrap moka-chart-plot__x--wrap(-at-w\d+)? moka-chart-plot__x--flat-at-w\d+$", layout.AxisClass);
		Assert.Contains("--_lines: 2", layout.AxisStyle, StringComparison.Ordinal);
		Assert.All(layout.Labels, label => Assert.Contains("--_ds: 0.2", label.Style, StringComparison.Ordinal));
		Assert.All(layout.Labels, label => Assert.True(label.MayBeCut));
	}

	[Fact]
	public void Truncate_NeverHidesALabel_AndKeepsEveryWholeText()
	{
		(string, double)[] labels = [.. Enumerable.Range(0, 40).Select(i => ($"telemetry-{i}.example.com", (i + 0.5) / 40))];

		CategoryAxisLayout layout = ChartAxisLayout.Categories(labels, MokaAxisLabelFit.Truncate, 2, null);

		Assert.Equal(40, layout.Labels.Count);
		Assert.All(layout.Labels, label => Assert.Equal("", HideOf(label.Class)));
		Assert.All(layout.Labels, label => Assert.True(label.MayBeCut));
		Assert.StartsWith("moka-chart-plot__x--fit-truncate moka-chart-plot__x--trunc", layout.AxisClass, StringComparison.Ordinal);
	}

	[Fact]
	public void AnAxisKnownToBeWideEnough_NeverLeavesFlat()
	{
		(string, double)[] labels = [.. Enumerable.Range(0, 80).Select(i => ($"host-{i:000}", (i + 0.5) / 80))];

		CategoryAxisLayout layout = ChartAxisLayout.Categories(labels, MokaAxisLabelFit.Scroll, 2, null, 80 * 6.5);

		Assert.Equal("moka-chart-plot__x--fit-auto", layout.AxisClass);
		Assert.All(layout.Labels, label => Assert.Equal("moka-chart-plot__xtick", label.Class));
	}

	[Fact]
	public void MaxLabelEm_CutsLongLabels_AndCountsThemShort()
	{
		(string, double)[] labels = [("a-very-long-name-indeed.example.com", 0.25), ("short", 0.75)];

		CategoryAxisLayout layout = ChartAxisLayout.Categories(labels, MokaAxisLabelFit.Thin, 2, 6);

		Assert.Contains("--_max: 6em", layout.Labels[0].Style, StringComparison.Ordinal);
		Assert.Contains("--_hw: 3em", layout.Labels[0].Style, StringComparison.Ordinal);
		Assert.True(layout.Labels[0].MayBeCut);
		Assert.False(layout.Labels[1].MayBeCut);
	}

	[Fact]
	public void LongLabels_TurnedAt45_AreCutAt12Em_SoTheAxisStaysShort()
	{
		(string, double)[] labels = [.. Enumerable.Range(0, 12).Select(i => ($"connectivity-check-{i}.ubuntu.example.com", (i + 0.5) / 12))];

		CategoryAxisLayout layout = ChartAxisLayout.Categories(labels, MokaAxisLabelFit.Rotate, 2, null);

		Assert.Contains("--_rot-max: 12em", layout.AxisStyle, StringComparison.Ordinal);
		Assert.Contains("--_rot-h: 9.934em", layout.AxisStyle, StringComparison.Ordinal);
		Assert.All(layout.Labels, label => Assert.True(label.MayBeCut));
	}

	[Fact]
	public void ValueTicks_OnFourDivisions_HideEveryOtherOneUnder8EmTall_AndKeepTheTopLongest()
	{
		string?[] classes = ChartAxisLayout.TickHeightClasses([0, 0.25, 0.5, 0.75, 1], [true, true, true, true, true]);

		Assert.Equal(["moka-chart-plot__ytick--h5", "moka-chart-plot__ytick--h8", "moka-chart-plot__ytick--h5", "moka-chart-plot__ytick--h8", null], classes);
	}

	[Fact]
	public void LogMinorTicks_HideBeforeAnyPowerOfTen()
	{
		double[] values = [1, 2, 5, 10, 20, 50, 100];
		double[] shares = [.. values.Select(v => Math.Log10(v) / 2)];
		bool[] majors = [.. values.Select(v => v is 1 or 10 or 100)];

		string?[] classes = ChartAxisLayout.TickHeightClasses(shares, majors);

		int majorNeed = classes.Where((_, i) => majors[i]).Select(HeightOf).Max();
		Assert.All(classes.Where((_, i) => !majors[i]), minor => Assert.True(HeightOf(minor) >= majorNeed, minor));
	}

	[Fact]
	public void Layers_ShowTogether_AndACrowdedMemberWaitsAlone()
	{
		AxisSpot[] spots =
		[
			new(0, "Dec", 0.5, 1, 0), new(1, "Mon 30", 0.48, 2, 0), new(2, "Mon 7", 0.72, 2, 0), new(3, "Mon 23", 0.25, 2, 0)
		];

		List<AxisLabelSpot> labels = ChartAxisLayout.Flat(spots, null, [-2, 17, 17, 17]);

		Assert.Equal("", HideOf(labels.Single(l => l.Index == 0).Class).Replace("--w5", "", StringComparison.Ordinal));
		Assert.Equal(HideOf(labels.Single(l => l.Index == 2).Class), HideOf(labels.Single(l => l.Index == 3).Class));
		Assert.DoesNotContain(labels, l => l.Index == 1);
	}

	public static TheoryData<int, int> LabelSets()
	{
		TheoryData<int, int> data = [];
		foreach (int count in new[] { 2, 3, 7, 12, 24, 60, 150 })
		{
			foreach (int seed in new[] { 1, 2, 3 })
			{
				data.Add(count, seed);
			}
		}

		return data;
	}

	// The stylesheet's rules, played in C#: at every width, the stage the axis is in and the labels that show, placed as
	// the CSS places them. No two on one row may come nearer than the gap the layout keeps, by its own estimates.
	[Theory]
	[MemberData(nameof(LabelSets))]
	public void AtEveryWidth_NoTwoShownLabelsOverlap(int count, int seed)
	{

		foreach (MokaAxisLabelFit fit in new[] { MokaAxisLabelFit.Auto, MokaAxisLabelFit.Thin, MokaAxisLabelFit.Rotate, MokaAxisLabelFit.Stagger })
		{
			foreach (bool slots in new[] { false, true })
			{
				(string, double)[] labels =
				[
					.. Enumerable.Range(0, count).Select(i => (new string('x', 1 + (((i * 7919) + (seed * 104729)) % (seed == 3 ? 30 : 9))),
						slots ? (i + 0.5) / count : count == 1 ? 0.5 : (double)i / (count - 1)))
				];
				CategoryAxisLayout layout = ChartAxisLayout.Categories(labels, fit, 2, null);
				for (double width = 3; width <= 130; width += 0.25)
				{
					AssertNoOverlap(labels, layout, width, $"{fit} {count} {seed} {(slots ? "slots" : "points")} at {width} em");
				}
			}
		}
	}

	private static void AssertNoOverlap((string Text, double Share)[] labels, CategoryAxisLayout layout, double width, string what)
	{
		string stage = StageAt(layout.AxisClass, width);
		List<(double Low, double High, int Row)> shown = [];
		foreach (AxisLabelSpot label in layout.Labels)
		{
			if (WidthOf(label.Class) is var hide && (hide < 0 || width < hide))
			{
				continue;
			}

			(string text, double share) = labels[label.Index];
			double half = text.Length * ChartAxisLayout.CharEm / 2;
			double reach = ChartAxisLayout.Diagonal * Math.Min(text.Length * ChartAxisLayout.CharEm, 12);
			if (stage == "rotate")
			{
				double anchor = Math.Min(width - 0.45, Math.Max(share * width, reach));
				shown.Add((anchor - ((ChartAxisLayout.RotatedEm + ChartAxisLayout.GapEm) / 2),
					anchor + ((ChartAxisLayout.RotatedEm + ChartAxisLayout.GapEm) / 2), 0));
			}
			else
			{
				double centre = Math.Clamp(share * width, half, Math.Max(half, width - half));
				int row = stage == "stagger" && label.Class.Contains("--odd", StringComparison.Ordinal) ? 1 : 0;
				shown.Add((centre - half - (ChartAxisLayout.GapEm / 2), centre + half + (ChartAxisLayout.GapEm / 2), row));
			}
		}

		foreach (IGrouping<int, (double Low, double High, int Row)> row in shown.GroupBy(s => s.Row))
		{
			(double Low, double High, int Row)[] ordered = [.. row.OrderBy(s => s.Low)];
			for (int i = 1; i < ordered.Length; i++)
			{
				Assert.True(ordered[i].Low >= ordered[i - 1].High - 1e-6, $"{what}: {stage} overlaps at {ordered[i].Low:0.##}");
			}
		}
	}

	// Which stage the stylesheet puts an axis in at a width: the conditional class with the narrowest width the axis is
	// under wins (its block comes later), stages in one block win in file order; unconditional ones lose to any of them.
	private static string StageAt(string axisClass, double width)
	{
		string[] order = ["stagger", "wrap", "trunc", "rotate", "flat"];
		string stage = "flat";
		int best = int.MaxValue;
		int bestOrder = -1;
		foreach (Match match in Stage().Matches(axisClass))
		{
			string name = match.Groups["stage"].Value;
			int at = match.Groups["at"].Success ? int.Parse(match.Groups["at"].Value, CultureInfo.InvariantCulture) : int.MaxValue;
			if (width >= at && match.Groups["at"].Success)
			{
				continue;
			}

			int rank = Array.IndexOf(order, name);
			if (at < best || (at == best && rank > bestOrder))
			{
				best = at;
				bestOrder = rank;
				stage = name;
			}
		}

		return stage;
	}

	private static string HideOf(string classes) =>
		classes.Split(' ').FirstOrDefault(c => c.StartsWith("moka-chart-plot__xtick--w", StringComparison.Ordinal) || c.EndsWith("--hide", StringComparison.Ordinal))
			?.Replace("moka-chart-plot__xtick", "", StringComparison.Ordinal) ?? "";

	private static int WidthOf(string classes)
	{
		string hide = HideOf(classes);
		return hide == "--hide" ? -1 : hide.Length == 0 ? 0 : int.Parse(hide[3..], CultureInfo.InvariantCulture);
	}

	private static int HeightOf(string? cls) => cls is null ? 0 : int.Parse(cls[(cls.LastIndexOf('h') + 1)..], CultureInfo.InvariantCulture);

	[GeneratedRegex(@"moka-chart-plot__x--(?<stage>stagger|wrap|trunc|rotate|flat)(-at-w(?<at>\d+))?(?=\s|$)")]
	private static partial Regex Stage();

	[GeneratedRegex(@"--flat-at-w(\d+)")]
	private static partial Regex FlatAt();
}
