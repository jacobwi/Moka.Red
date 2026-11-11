using System.Globalization;
using AngleSharp.Dom;
using Bunit;
using Moka.Red.Data.Chart;

namespace Moka.Red.Data.Tests.Charts;

public sealed class MokaCandlestickChartTests : BunitContext
{
	private static readonly DateTimeOffset Monday = new(2026, 3, 9, 12, 0, 0, TimeSpan.Zero);

	private static readonly MokaCandle[] Week =
	[
		new(Monday, 102.40, 108.00, 101.20, 107.10) { Volume = 1000 },
		new(Monday.AddDays(1), 107.10, 107.50, 103.00, 104.00) { Volume = 3000 },
		new(Monday.AddDays(2), 104.00, 106.00, 103.50, 104.00) { Volume = 2000 }
	];

	public MokaCandlestickChartTests() => this.Static();

	[Fact]
	public void AnUpDay_IsHollow_AndADownDay_Filled()
	{
		IRenderedComponent<MokaCandlestickChart> cut = Render<MokaCandlestickChart>(p => p
			.Add(x => x.Candles, Week)
			.Add(x => x.TimeZone, TimeZoneInfo.Utc));

		IReadOnlyList<IElement> columns = cut.FindAll(".moka-chart-cols > .moka-chart-col");
		Assert.Equal(3, columns.Count);
		IElement upBody = columns[0].QuerySelector(".moka-chart-mark--body")!;
		Assert.Contains("moka-chart-mark--hollow", upBody.ClassList);
		Assert.Contains("moka-chart-mark--up", upBody.ClassList);
		IElement downBody = columns[1].QuerySelector(".moka-chart-mark--body")!;
		Assert.DoesNotContain("moka-chart-mark--hollow", downBody.ClassList);
		Assert.Contains("moka-chart-mark--down", downBody.ClassList);
		Assert.Equal("down", downBody.GetAttribute("data-moka-key"));
		Assert.NotNull(columns[0].QuerySelector(".moka-chart-mark--wick"));
		Assert.Contains("moka-chart-mark--up", columns[2].QuerySelector(".moka-chart-mark--body")!.ClassList);
		Assert.Equal(["Up", "Down"], cut.Instance.SeriesInfo.Select(s => s.Name));
	}

	[Fact]
	public void TheValueAxis_DoesNotReachDownToZero()
	{
		IRenderedComponent<MokaCandlestickChart> cut = Render<MokaCandlestickChart>(p => p.Add(x => x.Candles, Week));

		Assert.InRange(cut.Instance.ColumnPlotModel(0).Scale.Min, 95, 101.2);
		Assert.InRange(cut.Instance.ColumnPlotModel(0).Scale.Max, 108, 115);
	}

	[Fact]
	public void TheTooltip_SaysOpenHighLowAndClose_AndTheChange()
	{
		IRenderedComponent<MokaCandlestickChart> cut = Render<MokaCandlestickChart>(p => p
			.Add(x => x.Candles, Week)
			.Add(x => x.TimeZone, TimeZoneInfo.Utc));

		ChartActiveInfo info = cut.Instance.DescribeActive(0, -1, 0, null)!;
		Assert.Equal(Day(Monday), info.Heading);
		Assert.Equal(["open", "high", "low", "close"], info.Rows.Select(r => r.Name));
		Assert.Equal(Prices(102.40, 108.00, 101.20, 107.10), info.Rows.Select(r => r.ValueText));
		Assert.Equal($"+{Price(4.7)} ({(4.7 / 102.4).ToString("+0.##%;-0.##%;0%", CultureInfo.CurrentCulture)})", info.Notes[0]);
		Assert.True(info.Rows[3].Emphasis);
	}

	[Fact]
	public void MoreCandlesThanRoom_Merge_FirstOpenHighestHighLowestLowLastClose()
	{
		this.Interactive();
		IRenderedComponent<MokaCandlestickChart> cut = Render<MokaCandlestickChart>(p => p
			.Add(x => x.Candles, Week)
			.Add(x => x.TimeZone, TimeZoneInfo.Utc));

		cut.InvokeAsync(() => cut.Instance.OnPlotWidth(6));

		Assert.Equal(2, cut.Instance.DrawnCount(0));
		ChartActiveInfo merged = cut.Instance.DescribeActive(0, -1, 0, null)!;
		Assert.Equal(Prices(102.40, 108.00, 101.20, 104.00), merged.Rows.Select(r => r.ValueText));
		Assert.Equal($"{Day(Monday)} to {Day(Monday.AddDays(1))}", merged.Heading);
	}

	[Fact]
	public void KeepingTime_LeavesAnEmptySlotForAMissingDay()
	{
		MokaCandle[] gap = [Week[0], Week[1], Week[2] with { At = Monday.AddDays(3) }];
		IRenderedComponent<MokaCandlestickChart> gapless = Render<MokaCandlestickChart>(p => p.Add(x => x.Candles, gap));
		IRenderedComponent<MokaCandlestickChart> timed = Render<MokaCandlestickChart>(p => p
			.Add(x => x.Candles, gap)
			.Add(x => x.Gapless, false));

		Assert.Equal(3, gapless.Instance.DrawnCount(0));
		Assert.Equal(4, timed.Instance.DrawnCount(0));
		Assert.Empty(timed.FindAll(".moka-chart-cols > .moka-chart-col")[2].QuerySelectorAll(".moka-chart-mark"));
		Assert.Null(timed.Instance.DescribeActive(0, -1, 2, null));
		Assert.NotNull(timed.Instance.DescribeActive(0, -1, 3, null));
	}

	[Fact]
	public void Volume_RunsAlongTheBottomFifth()
	{
		IRenderedComponent<MokaCandlestickChart> cut = Render<MokaCandlestickChart>(p => p
			.Add(x => x.Candles, Week)
			.Add(x => x.ShowVolume, true));

		IReadOnlyList<IElement> volume = cut.FindAll(".moka-chart-mark--volume");
		Assert.Equal(3, volume.Count);
		Assert.Equal("--_lo: 0%; --_hi: 20%", volume[1].GetAttribute("style"));
		Assert.Equal("--_lo: 0%; --_hi: 6.667%", volume[0].GetAttribute("style"));
	}

	[Fact]
	public void ADayTheLegendHid_IsNotDrawn()
	{
		IRenderedComponent<MokaCandlestickChart> cut = Render<MokaCandlestickChart>(p => p
			.Add(x => x.Candles, Week)
			.Add(x => x.HiddenSeries, ["down"]));

		Assert.Equal(2, cut.FindAll(".moka-chart-mark--body").Count);
	}

	[Fact]
	public void TheTable_ListsEveryCandle()
	{
		IRenderedComponent<MokaCandlestickChart> cut = Render<MokaCandlestickChart>(p => p
			.Add(x => x.Candles, Week)
			.Add(x => x.ShowVolume, true));

		ChartTableModel table = cut.Instance.TableModel!;
		Assert.Equal(["Time", "open", "high", "low", "close", "volume"], table.Headers);
		Assert.Equal(3, table.RowCount);
		Assert.Equal("2026-03-09T12:00:00.0000000+00:00", table.Raw(0, 0));
		Assert.Equal("107.1", table.Raw(0, 4));
	}

	[Fact]
	public void ItsWords_CanBeReplaced()
	{
		IRenderedComponent<MokaCandlestickChart> cut = Render<MokaCandlestickChart>(p => p
			.Add(x => x.Candles, Week)
			.Add(x => x.CandleText, new MokaCandleText { Open = "Eröffnung", Up = "Steigend" }));

		Assert.Equal("Eröffnung", cut.Instance.DescribeActive(0, -1, 0, null)!.Rows[0].Name);
		Assert.Equal("Steigend", cut.Instance.SeriesInfo[0].Name);
	}

	private static string Day(DateTimeOffset at)
	{
		CultureInfo culture = CultureInfo.CurrentCulture;
		string monthDay = culture.DateTimeFormat.MonthDayPattern.Replace("MMMM", "MMM", StringComparison.Ordinal);
		return at.ToString($"ddd {monthDay}", culture);
	}

	// The prices are written to one decimal, the most any of them has.
	private static string Price(double value) => value.ToString("N1", CultureInfo.CurrentCulture);

	private static string[] Prices(params double[] values) => [.. values.Select(Price)];
}
