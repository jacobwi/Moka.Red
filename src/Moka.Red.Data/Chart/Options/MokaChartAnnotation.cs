using Moka.Red.Core.Enums;

namespace Moka.Red.Data.Chart;

/// <summary>Something drawn over a chart to explain its data: a limit, a target, an event or a window.</summary>
public abstract record MokaChartAnnotation
{
	/// <summary>What it reads beside it and in the tooltip.</summary>
	public string? Label { get; init; }

	/// <summary>The color it is drawn in. Defaults to <see cref="MokaColor.Warning" />.</summary>
	public MokaColor Tone { get; init; } = MokaColor.Warning;

	/// <summary>Its identity among the chart's annotations. Null uses its position.</summary>
	public string? Key { get; init; }
}

/// <summary>A dashed line at a value, labelled at its end. The value axis always reaches it.</summary>
/// <param name="Value">Where the line runs.</param>
public sealed record MokaChartThreshold(double Value) : MokaChartAnnotation;

/// <summary>A soft band between two values, such as a target range.</summary>
/// <param name="Low">The band's bottom.</param>
/// <param name="High">The band's top.</param>
public sealed record MokaChartBand(double Low, double High) : MokaChartAnnotation;

/// <summary>Something that happened, as a marker under the plot with a guide line through it.</summary>
/// <param name="At">When it happened.</param>
public sealed record MokaChartEvent(MokaChartX At) : MokaChartAnnotation
{
	/// <summary>When it ended, for an incident: the span between is shaded.</summary>
	public MokaChartX? To { get; init; }

	/// <summary>The marker's shape. Defaults to <see cref="MokaChartMarker.Diamond" />.</summary>
	public MokaChartMarker Shape { get; init; } = MokaChartMarker.Diamond;

	/// <summary>More about it, in the marker's tooltip.</summary>
	public string? Detail { get; init; }
}

/// <summary>A hatched span that doesn't count, such as a maintenance window.</summary>
/// <param name="From">Where it starts.</param>
/// <param name="To">Where it ends.</param>
public sealed record MokaChartWindow(MokaChartX From, MokaChartX To) : MokaChartAnnotation
{
	/// <summary>Whether values inside it count as gaps, so the line breaks there. Defaults to true.</summary>
	public bool BreakLine { get; init; } = true;
}
