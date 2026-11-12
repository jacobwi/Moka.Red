namespace Moka.Red.Data.Chart;

/// <summary>One row of a <see cref="MokaStateTimeline" />: a thing, such as a service, a device or a link, and the states it was in.</summary>
/// <param name="Name">What the row is, such as "api-gateway".</param>
/// <param name="Spans">When each state started, in any order. Time no span covers is unknown.</param>
public sealed record MokaStateRow(string Name, IReadOnlyList<MokaStateSpan> Spans)
{
	/// <summary>Its identity for highlighting and in <see cref="MokaChartPoint.RowKey" />. Null uses <see cref="Name" />.</summary>
	public string? Key { get; init; }

	/// <summary>A line about the row for the tooltip, such as "10.0.0.5, eu-west".</summary>
	public string? Detail { get; init; }
}

/// <summary>
///     A stretch of time a row of a <see cref="MokaStateTimeline" /> spent in one state. Without an <see cref="End" /> it
///     lasts until the next span of its row without an end starts, and the last one until now, so state changes can be
///     passed as they happened. A span with an end, such as an incident or a maintenance window, sits over whatever runs
///     around it, and the more severe state shows.
/// </summary>
/// <param name="Start">When the state started.</param>
/// <param name="State">
///     The state's key: one of <see cref="MokaStateTimeline.States" />, else a built-in key such as
///     <see cref="MokaStateTimeline.DownKey" />.
/// </param>
public readonly record struct MokaStateSpan(DateTimeOffset Start, string State)
{
	/// <summary>When the state ended. Null lasts until the row's next span without an end, or now.</summary>
	public DateTimeOffset? End { get; init; }

	/// <summary>A line about the span for the tooltip, such as "HTTP 503 from upstream".</summary>
	public string? Detail { get; init; }
}
