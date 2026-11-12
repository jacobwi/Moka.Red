namespace Moka.Red.Data.Chart;

/// <summary>Every word a <see cref="MokaStateTimeline" /> writes beyond the shared chart words. English by default.</summary>
public sealed record MokaStateTimelineText
{
	/// <summary>The words in English.</summary>
	public static MokaStateTimelineText Default { get; } = new();

	/// <summary>The built-in <see cref="MokaStateTimeline.UpKey" /> state.</summary>
	public string Up { get; init; } = "Up";

	/// <summary>The built-in <see cref="MokaStateTimeline.DegradedKey" /> state.</summary>
	public string Degraded { get; init; } = "Degraded";

	/// <summary>The built-in <see cref="MokaStateTimeline.DownKey" /> state.</summary>
	public string Down { get; init; } = "Down";

	/// <summary>The built-in <see cref="MokaStateTimeline.MaintenanceKey" /> state.</summary>
	public string Maintenance { get; init; } = "Maintenance";

	/// <summary>The built-in <see cref="MokaStateTimeline.UnknownKey" /> state, which time no span covers is in.</summary>
	public string Unknown { get; init; } = "Unknown";

	/// <summary>The share of known time a row was up, in the tooltip and the table.</summary>
	public string Uptime { get; init; } = "Uptime";

	/// <summary>How much of a row's time was unknown, after its uptime, such as "1 h 20 min unknown".</summary>
	public Func<string, string> UnknownTime { get; init; } = time => $"{time} unknown";

	/// <summary>What the line at now is called, before its time.</summary>
	public string Now { get; init; } = "Now";

	/// <summary>The table's heading for a span's state.</summary>
	public string State { get; init; } = "State";

	/// <summary>The table's heading for how long a span lasted.</summary>
	public string Duration { get; init; } = "Duration";
}
