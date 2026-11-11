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

	/// <summary>A fixed lowest value. Null fits the data. On a time axis, a time as Unix milliseconds: see <see cref="MinTime" />.</summary>
	public double? Min { get; init; }

	/// <summary>A fixed highest value. Null fits the data. On a time axis, a time as Unix milliseconds: see <see cref="MaxTime" />.</summary>
	public double? Max { get; init; }

	/// <summary>The axis starts at least this low. Lower data still lowers it. On a time axis, Unix milliseconds.</summary>
	public double? SuggestedMin { get; init; }

	/// <summary>The axis reaches at least this high. Higher data still raises it. On a time axis, Unix milliseconds.</summary>
	public double? SuggestedMax { get; init; }

	/// <summary>
	///     Where a time axis starts, whatever the data: <see cref="Min" /> as a time. A series a minute long on an axis
	///     pinned to the hour draws as a minute of that hour.
	/// </summary>
	public DateTimeOffset? MinTime
	{
		get => AsTime(Min);
		init => Min = AsMilliseconds(value);
	}

	/// <summary>Where a time axis ends, whatever the data: <see cref="Max" /> as a time.</summary>
	public DateTimeOffset? MaxTime
	{
		get => AsTime(Max);
		init => Max = AsMilliseconds(value);
	}

	/// <summary>A time axis starts at least this early; earlier data still moves it: <see cref="SuggestedMin" /> as a time.</summary>
	public DateTimeOffset? SuggestedMinTime
	{
		get => AsTime(SuggestedMin);
		init => SuggestedMin = AsMilliseconds(value);
	}

	/// <summary>A time axis reaches at least this late; later data still moves it: <see cref="SuggestedMax" /> as a time.</summary>
	public DateTimeOffset? SuggestedMaxTime
	{
		get => AsTime(SuggestedMax);
		init => SuggestedMax = AsMilliseconds(value);
	}

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

	/// <summary>
	///     A .NET date format for the ticks of a time axis. A format that can't tell the axis's step apart, such as
	///     <c>HH:mm</c> on a minute of seconds, takes a coarser step, so no two ticks side by side read the same.
	/// </summary>
	public string? TimeFormat { get; init; }

	/// <summary>
	///     A .NET date format for the time a tooltip heading, the table view and an event marker write on a time axis,
	///     such as <c>ddd h:mm tt</c>, whatever the ticks write. Wins over <see cref="Formatter" /> there. Null writes the
	///     time with as much of the date as the span needs, on the clock <see cref="Hour12" /> picks.
	/// </summary>
	public string? TooltipTimeFormat { get; init; }

	/// <summary>
	///     Whether a time axis writes a 12-hour clock, as <c>2 PM</c> and <c>2:30 PM</c>, in its ticks and tooltips. Null
	///     follows <see cref="TimeFormat" /> when it writes an hour, else the culture.
	/// </summary>
	public bool? Hour12 { get; init; }

	/// <summary>Whether the axis runs the other way. Defaults to false.</summary>
	public bool Reverse { get; init; }

	/// <summary>
	///     Whether the axis reaches zero: data that is all positive starts at zero, all negative ends at it. Null takes the
	///     chart's: a value axis reaches zero unless the chart's values sit far from it, as prices do, and an x axis
	///     spans only its data.
	/// </summary>
	public bool? IncludeZero { get; init; }

	private static DateTimeOffset? AsTime(double? milliseconds) =>
		milliseconds is { } value && double.IsFinite(value) ? new MokaChartX(value, true).Time : null;

	private static double? AsMilliseconds(DateTimeOffset? time) =>
		time is { } value ? MokaChartX.FromDateTimeOffset(value).Value : null;
}

/// <summary>A tick along an axis, for <see cref="MokaChartAxis.TickTemplate" />.</summary>
/// <param name="Value">Its value, or the time as Unix milliseconds on a time axis.</param>
/// <param name="Text">What it reads.</param>
/// <param name="Index">Its position among the axis's ticks.</param>
/// <param name="Time">Its time on a time axis.</param>
/// <param name="Level">What it marks on a time axis; <see cref="MokaTimeTickLevel.Major" /> elsewhere.</param>
public sealed record MokaAxisTick(double Value, string Text, int Index, DateTimeOffset? Time, MokaTimeTickLevel Level);
