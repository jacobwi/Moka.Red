using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Charts;

namespace Moka.Red.Data.Chart;

/// <summary>
///     How one axis of a chart is drawn: its scale, ends, ticks, units, labels and grid. Every property left null takes
///     the chart's look. Keep <see cref="Formatter" /> and <see cref="TickTemplate" /> as fields or methods: a lambda
///     that captures a local is a new value every render, and the chart then draws again each time.
/// </summary>
public sealed record MokaChartAxis
{
	/// <summary>Whether the axis is drawn. Defaults to true.</summary>
	public bool Visible { get; init; } = true;

	/// <summary>A title along the axis.</summary>
	public string? Title { get; init; }

	/// <summary>How values are spread along the axis. Defaults to <see cref="MokaAxisScale.Auto" />.</summary>
	public MokaAxisScale Scale { get; init; } = MokaAxisScale.Auto;

	/// <summary>Which side of the plot the axis is on. Null takes the look's.</summary>
	public MokaAxisSide? Side { get; init; }

	/// <summary>A fixed lowest value. Null fits the data.</summary>
	public double? Min { get; init; }

	/// <summary>A fixed highest value. Null fits the data.</summary>
	public double? Max { get; init; }

	/// <summary>The axis starts at least this low. Lower data still lowers it.</summary>
	public double? SuggestedMin { get; init; }

	/// <summary>The axis reaches at least this high. Higher data still raises it.</summary>
	public double? SuggestedMax { get; init; }

	/// <summary>How many divisions to aim for. Null takes the chart's.</summary>
	public int? TickCount { get; init; }

	/// <summary>Ticks to draw instead of working them out.</summary>
	public IReadOnlyList<double>? TickValues { get; init; }

	/// <summary>Whether ticks are rounded to round steps. Null takes the look's.</summary>
	public bool? Nice { get; init; }

	/// <summary>The units the ticks are written and rounded in.</summary>
	public MokaChartUnit? Unit { get; init; }

	/// <summary>Whether each tick carries the unit. False moves it into the axis title. Defaults to true.</summary>
	public bool ShowUnit { get; init; } = true;

	/// <summary>Whether numbers from ten thousand up are written short, as 12K. Null takes the look's.</summary>
	public bool? Compact { get; init; }

	/// <summary>A .NET format string for the ticks.</summary>
	public string? Format { get; init; }

	/// <summary>Writes a tick's value. Wins over <see cref="Format" /> and <see cref="Unit" />.</summary>
	public Func<double, string>? Formatter { get; init; }

	/// <summary>Where the tick labels sit. Null takes the look's.</summary>
	public MokaTickPlacement? TickLabels { get; init; }

	/// <summary>The lines behind the data along this axis. Null takes the look's.</summary>
	public MokaChartGrid? Grid { get; init; }

	/// <summary>What labels that don't fit do. Null takes the look's.</summary>
	public MokaAxisLabelFit? Fit { get; init; }

	/// <summary>How many lines a wrapped label may take. Defaults to 2.</summary>
	public int WrapLines { get; init; } = 2;

	/// <summary>The longest a label may be, in em. Longer ones are cut, with the whole text in a tooltip.</summary>
	public double? MaxLabelEm { get; init; }

	/// <summary>The least width a category gets with <see cref="MokaAxisLabelFit.Scroll" />, in em. Null is 2.5.</summary>
	public double? MinCategoryEm { get; init; }

	/// <summary>Draws each tick. Its <see cref="MokaAxisTick.Text" /> still decides how much room the tick takes.</summary>
	public RenderFragment<MokaAxisTick>? TickTemplate { get; init; }

	/// <summary>A .NET date format for the ticks of a time axis.</summary>
	public string? TimeFormat { get; init; }

	/// <summary>Whether the axis runs the other way. Defaults to false.</summary>
	public bool Reverse { get; init; }

	/// <summary>
	///     Whether the axis reaches zero: data that is all positive starts at zero, all negative ends at it. Null takes the
	///     chart's: a value axis reaches zero unless the chart's values sit far from it, as prices do, and an x axis
	///     spans only its data.
	/// </summary>
	public bool? IncludeZero { get; init; }
}

/// <summary>A tick along an axis, for <see cref="MokaChartAxis.TickTemplate" />.</summary>
/// <param name="Value">Its value, or the time as Unix milliseconds on a time axis.</param>
/// <param name="Text">What it reads.</param>
/// <param name="Index">Its position among the axis's ticks.</param>
/// <param name="Time">Its time on a time axis.</param>
/// <param name="Level">What it marks on a time axis; <see cref="MokaTimeTickLevel.Major" /> elsewhere.</param>
public sealed record MokaAxisTick(double Value, string Text, int Index, DateTimeOffset? Time, MokaTimeTickLevel Level);
