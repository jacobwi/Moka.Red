using System.Globalization;
using Moka.Red.Data.Chart;

namespace Moka.Red.Data.Tests.Charts;

public sealed class ChartColumnsTests
{
	private static string StyleSheet =>
		File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Css", "MokaChartColumns.razor.css")).ReplaceLineEndings("\n");

	[Theory]
	[InlineData(0.2, 1)]
	[InlineData(1, 1)]
	[InlineData(1.01, 1.5)]
	[InlineData(8.3, 9)]
	[InlineData(119, 120)]
	public void Snap_TakesTheNarrowestWidthTheStylesheetKnows(double need, double snapped) =>
		Assert.Equal(snapped, ChartColumns.Snap(need));

	[Fact]
	public void Snap_GivesUpPastTheWidestWidth() => Assert.Null(ChartColumns.Snap(121));

	[Fact]
	public void TheStylesheet_HasAQueryForEveryWidthAndEveryKind()
	{
		string css = StyleSheet;
		foreach (double width in ChartColumns.Widths)
		{
			string em = width.ToString(CultureInfo.InvariantCulture);
			string name = ChartColumns.Number(width);
			Assert.Contains($"@container moka-chart-col (width < {em}em) {{ .moka-chart-value--w{name} {{", css, StringComparison.Ordinal);
			Assert.Contains($"@container moka-chart-lane (height < {em}em) {{ .moka-chart-value--h{name} {{", css, StringComparison.Ordinal);
			Assert.Contains($"@container moka-chart-lane (width < {em}em) {{ .moka-chart-value--lt{name} {{", css, StringComparison.Ordinal);
			Assert.Contains($"@container moka-chart-lane (width >= {em}em) {{ .moka-chart-value--ge{name} {{", css, StringComparison.Ordinal);
			Assert.Contains($"@container moka-chart-mark (width < {em}em) {{ .moka-chart-mark__label--s{name} {{", css, StringComparison.Ordinal);
		}
	}

	[Fact]
	public void AColumnValue_NamesTheColumnWidthItNeeds_BesideItsNeighbours()
	{
		ChartTip one = ChartColumns.ColumnTip("412K", 0.5, false, 1)!;
		ChartTip two = ChartColumns.ColumnTip("412K", 0.5, false, 2)!;

		Assert.Contains("moka-chart-value--hi", one.Class, StringComparison.Ordinal);
		Assert.Contains("moka-chart-value--w3_5", one.Class, StringComparison.Ordinal);
		Assert.Contains("moka-chart-value--w7", two.Class, StringComparison.Ordinal);
		Assert.Equal("--_at: 50%", one.Style);
	}

	[Fact]
	public void AColumnValue_NeedsATallerPlot_TheNearerItsBarComesToTheTop()
	{
		ChartTip low = ChartColumns.ColumnTip("40", 0.5, false, 1)!;
		ChartTip high = ChartColumns.ColumnTip("40", 0.9, false, 1)!;

		Assert.Contains("moka-chart-value--h3", low.Class, StringComparison.Ordinal);
		Assert.Contains("moka-chart-value--h13_5", high.Class, StringComparison.Ordinal);
		Assert.Null(ChartColumns.ColumnTip("40", 1, false, 1));
	}

	[Fact]
	public void AValueWiderThanAnEndColumn_RunsInwards()
	{
		Assert.Contains("moka-chart-value--from-end", ChartColumns.ColumnTip("46 min", 0.7, false, 0.34, 1)!.Class, StringComparison.Ordinal);
		Assert.Contains("moka-chart-value--from-start", ChartColumns.ColumnTip("46 min", 0.7, false, 0.34, -1)!.Class,
			StringComparison.Ordinal);
		Assert.DoesNotContain("--from-", ChartColumns.ColumnTip("46 min", 0.7, false, 1, 1)!.Class, StringComparison.Ordinal);
	}

	[Fact]
	public void AValueUnderABarBelowZero_ReadsBelowIt() =>
		Assert.Contains("moka-chart-value--lo", ChartColumns.ColumnTip("-9", 0.3, true, 1)!.Class, StringComparison.Ordinal);

	[Fact]
	public void ARowValue_ReadsPastTheBar_OrInsideItsEndWhereThatHasNoRoom()
	{
		List<ChartTip> tips = [];
		ChartColumns.RowTips(tips, "18,240", 0, 0.9, false);

		Assert.Equal(2, tips.Count);
		Assert.Contains("moka-chart-value--hi moka-chart-value--lt48", tips[0].Class, StringComparison.Ordinal);
		Assert.Contains("moka-chart-value--in-hi moka-chart-value--lt6 moka-chart-value--ge48", tips[1].Class, StringComparison.Ordinal);
	}

	[Fact]
	public void AShortRowValue_ReadsOnlyPastTheBar_WhenItsBarIsTooShortToHoldIt()
	{
		List<ChartTip> tips = [];
		ChartColumns.RowTips(tips, "2,284", 0, 0.05, false);

		ChartTip tip = Assert.Single(tips);
		Assert.Contains("moka-chart-value--hi", tip.Class, StringComparison.Ordinal);
	}

	[Fact]
	public void ARowValueLeftOfZero_ReadsBeforeTheBar()
	{
		List<ChartTip> tips = [];
		ChartColumns.RowTips(tips, "-9 ms", 0.45, 0.5, true);

		Assert.Contains("moka-chart-value--lo", tips[0].Class, StringComparison.Ordinal);
		Assert.Equal("--_at: 45%", tips[0].Style);
	}

	[Fact]
	public void AMark_RunsBetweenItsEnds_AndNamesItsRoundedEndAndGap()
	{
		ChartBox mark = ChartColumns.Mark("bar", 0.6, 0.2, "moka-chart-c2", null, "web",
			ChartBoxEnds.RoundHigh | ChartBoxEnds.GapLow, "moka-chart-hatch");

		Assert.Equal("--_lo: 20%; --_hi: 60%", mark.Style);
		Assert.Equal("moka-chart-mark moka-chart-mark--bar moka-chart-c2 moka-chart-mark--round-hi moka-chart-mark--gap-lo moka-chart-hatch",
			mark.Class);
		Assert.Equal("web", mark.Key);
	}

	[Fact]
	public void AMarkWithALabel_IsASizeContainerTheLabelHidesIn()
	{
		ChartBox mark = ChartColumns.WithLabel(ChartColumns.Mark("bar", 0, 0.5, null, null, null, ChartBoxEnds.None), "62%");

		Assert.Contains("moka-chart-mark--labelled", mark.Class, StringComparison.Ordinal);
		Assert.Equal("62%", mark.Label);
		Assert.Contains("moka-chart-mark__label--s3", mark.LabelClass, StringComparison.Ordinal);
		Assert.Contains("container: moka-chart-mark / size", StyleSheet, StringComparison.Ordinal);
	}

	[Fact]
	public void TheMarks_TakeTheLookVariables_AndNoPixelOfTheirOwn()
	{
		string css = StyleSheet;

		Assert.Contains("max-width: var(--moka-chart-bar-max)", css, StringComparison.Ordinal);
		Assert.Contains("gap: var(--moka-chart-bar-gap)", css, StringComparison.Ordinal);
		Assert.Contains("border-top-left-radius: var(--moka-chart-bar-radius)", css, StringComparison.Ordinal);
		Assert.Contains("background: var(--moka-chart-fill, var(--moka-chart-color, var(--moka-color-primary)))", css,
			StringComparison.Ordinal);
		Assert.DoesNotMatch(@"\d+px", css);
	}
}
