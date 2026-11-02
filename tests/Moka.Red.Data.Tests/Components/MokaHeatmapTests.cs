using Bunit;
using Moka.Red.Data.Heatmap;

namespace Moka.Red.Data.Tests.Components;

/// <summary>
///     The calendar heatmap: a cell per day, colored by level, with the months and weekdays named.
/// </summary>
public class MokaHeatmapTests : BunitContext
{
	[Fact]
	public void DrawsOneCellPerDay_OfEveryWeek()
	{
		IRenderedComponent<MokaHeatmap> cut = Render<MokaHeatmap>(p => p
			.Add(x => x.Weeks, 10)
			.Add(x => x.EndDate, new DateTime(2026, 10, 28)));

		// Ten week columns of seven days, Monday to Sunday
		Assert.Equal(10 * 7, cut.FindAll(".moka-heatmap-cell").Count);
	}

	[Fact]
	public void DaysWithCounts_TakeDeeperLevels_ThanEmptyDays()
	{
		DateTime end = new(2026, 10, 28); // a Wednesday
		IRenderedComponent<MokaHeatmap> cut = Render<MokaHeatmap>(p => p
			.Add(x => x.Weeks, 4)
			.Add(x => x.EndDate, end)
			.Add(x => x.Data, new Dictionary<DateTime, int> { [end] = 8, [end.AddDays(-1)] = 2 })
			.Add(x => x.MaxValue, 8));

		Assert.Contains("moka-heatmap-cell--l4", cut.Find(".moka-heatmap-grid").InnerHtml, StringComparison.Ordinal);
		Assert.Contains("moka-heatmap-cell--l1", cut.Find(".moka-heatmap-grid").InnerHtml, StringComparison.Ordinal);
		// The busiest day is always the deepest, and an empty one has no level at all
		Assert.Contains("moka-heatmap-cell--l0", cut.Find(".moka-heatmap-grid").InnerHtml, StringComparison.Ordinal);
	}

	[Fact]
	public void DaysPastTheEnd_AreMarkedFuture_AndCarryNoCount()
	{
		// Ends on a Wednesday, so the rest of that week is the future
		IRenderedComponent<MokaHeatmap> cut = Render<MokaHeatmap>(p => p
			.Add(x => x.Weeks, 2)
			.Add(x => x.EndDate, new DateTime(2026, 10, 28)));

		Assert.Equal(4, cut.FindAll(".moka-heatmap-cell--future").Count);
	}

	[Fact]
	public void Cells_CarryTheirCountAndDateAsATooltip()
	{
		DateTime end = new(2026, 10, 28);
		IRenderedComponent<MokaHeatmap> cut = Render<MokaHeatmap>(p => p
			.Add(x => x.Weeks, 1)
			.Add(x => x.EndDate, end)
			.Add(x => x.Data, new Dictionary<DateTime, int> { [end] = 3 }));

		Assert.Contains("3 on", cut.FindAll(".moka-heatmap-cell").Single(c => c.HasAttribute("title") &&
			c.GetAttribute("title")!.Contains("3 on", StringComparison.Ordinal)).GetAttribute("title"), StringComparison.Ordinal);
	}

	[Fact]
	public void WithoutData_EveryDayIsLevelZero()
	{
		IRenderedComponent<MokaHeatmap> cut = Render<MokaHeatmap>(p => p
			.Add(x => x.Weeks, 2)
			.Add(x => x.EndDate, new DateTime(2026, 10, 28)));

		Assert.Equal(14, cut.FindAll(".moka-heatmap-cell--l0").Count);
	}

	[Fact]
	public void Labels_CanBeTakenOff()
	{
		IRenderedComponent<MokaHeatmap> cut = Render<MokaHeatmap>(p => p
			.Add(x => x.Weeks, 1)
			.Add(x => x.EndDate, new DateTime(2026, 10, 28))
			.Add(x => x.ShowMonthLabels, false)
			.Add(x => x.ShowWeekdayLabels, false));

		Assert.Empty(cut.FindAll(".moka-heatmap-months"));
		Assert.Empty(cut.FindAll(".moka-heatmap-weekdays"));
	}
}
