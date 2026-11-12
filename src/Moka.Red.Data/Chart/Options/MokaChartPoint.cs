using Microsoft.AspNetCore.Components;

namespace Moka.Red.Data.Chart;

/// <summary>A point of a chart: which series, which value, and what it reads.</summary>
/// <param name="SeriesKey">The series' key.</param>
/// <param name="SeriesName">The series' name.</param>
/// <param name="Series">The series' position in the chart's full list of series.</param>
/// <param name="Index">The value's position in its series.</param>
/// <param name="Value">The value.</param>
/// <param name="ValueText">The value as the chart writes it.</param>
public sealed record MokaChartPoint(string SeriesKey, string SeriesName, int Series, int Index, double Value, string ValueText)
{
	/// <summary>The category the value belongs to, when the chart has labels.</summary>
	public string? Category { get; init; }

	/// <summary>The value's time, when the chart has times.</summary>
	public DateTimeOffset? Time { get; init; }

	/// <summary>
	///     Where the value sits along the x axis: a scatter point's x, a time as Unix milliseconds, or a category's index.
	///     Null on a chart with no x axis, such as a donut.
	/// </summary>
	public double? X { get; init; }

	/// <summary>A bubble's size, on a scatter chart that draws bubbles.</summary>
	public double? Size { get; init; }

	/// <summary>The bottom of the value's band, when its series has one.</summary>
	public double? Low { get; init; }

	/// <summary>The top of the value's band, when its series has one.</summary>
	public double? High { get; init; }

	/// <summary>The total of its column, for stacked and grouped charts.</summary>
	public double? Total { get; init; }

	/// <summary>Its share of <see cref="Total" />, 0 to 1.</summary>
	public double? Share { get; init; }

	/// <summary>How much it changed since the pinned point, when one is pinned.</summary>
	public double? SincePin { get; init; }

	/// <summary>
	///     On a sankey flow, the key of the node it reaches; <see cref="SeriesKey" /> is the node it leaves. Null for a
	///     sankey node, so a click on a flow and a click on a node can be told apart, and on every other chart.
	/// </summary>
	public string? ToKey { get; init; }

	/// <summary>On a sankey flow, the name of the node it reaches, as the chart writes it. Null wherever <see cref="ToKey" /> is.</summary>
	public string? ToName { get; init; }

	/// <summary>
	///     On a chart whose rows hold several marks, such as a <see cref="MokaStateTimeline" />, the key of the row the point is
	///     in; <see cref="SeriesKey" /> is then its state. Null on every other chart.
	/// </summary>
	public string? RowKey { get; init; }

	/// <summary>
	///     On a chart of parts in rows, the row the point is in, from 0: a heatmap grid's row, a calendar heatmap's day of
	///     the week (Monday 0), a state timeline's row. Null on every other chart.
	/// </summary>
	public int? Row { get; init; }

	/// <summary>
	///     On a heatmap, the column the point is in, from 0: a grid's column, a calendar's week. Null on every other
	///     chart.
	/// </summary>
	public int? Column { get; init; }

	/// <summary>
	///     On a line chart, the value at the same place of the series this one is compared with
	///     (<see cref="MokaLineSeries.CompareTo" />), when it has one and the value isn't a gap.
	/// </summary>
	public double? Comparison { get; init; }

	/// <summary>On a line chart with a <c>Trend</c>, the trend's value at this place, when it has one.</summary>
	public double? Trend { get; init; }
}

/// <summary>What a <c>TooltipTemplate</c> draws: the point or column under the pointer, and the notes that go with it.</summary>
/// <param name="Index">The position along the axis.</param>
/// <param name="Heading">The heading the chart would write, such as the time or category.</param>
/// <param name="Points">One point per visible series, in legend order.</param>
/// <param name="Focus">The point the keyboard or the nearest-point tooltip is on, if any.</param>
/// <param name="PinnedIndex">The pinned position, if one is pinned.</param>
/// <param name="Notes">Lines about thresholds, bands and windows the point is in.</param>
public sealed record MokaChartTooltipContext(
	int Index,
	string? Heading,
	IReadOnlyList<MokaChartPoint> Points,
	MokaChartPoint? Focus,
	int? PinnedIndex,
	IReadOnlyList<string> Notes);

/// <summary>A toolbar control, handed to <c>ToolbarControl</c> to replace or wrap.</summary>
/// <param name="Which">Which control it is.</param>
/// <param name="Enabled">Whether it can be used right now.</param>
/// <param name="Run">What pressing it does, for a control that is a single action.</param>
/// <param name="Default">The control as the chart draws it.</param>
public sealed record MokaChartControlContext(MokaChartControls Which, bool Enabled, Func<Task>? Run, RenderFragment Default);
