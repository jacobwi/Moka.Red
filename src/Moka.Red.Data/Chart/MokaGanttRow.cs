namespace Moka.Red.Data.Chart;

/// <summary>One job of a <see cref="MokaGanttChart" />: what it is, when it started, and when it ended or will.</summary>
/// <param name="Name">The job, such as "Backup nas-1".</param>
/// <param name="Start">When it started, or is planned to.</param>
public sealed record MokaGanttRow(string Name, DateTimeOffset Start)
{
	/// <summary>When it ended, or is planned to. Null runs until now.</summary>
	public DateTimeOffset? End { get; init; }

	/// <summary>Where it stands. Defaults to <see cref="MokaGanttStatus.Done" />.</summary>
	public MokaGanttStatus Status { get; init; } = MokaGanttStatus.Done;

	/// <summary>A line about the job for the tooltip, such as "Disk full on /index".</summary>
	public string? Detail { get; init; }

	/// <summary>Its identity for highlighting. Null uses <see cref="Name" />.</summary>
	public string? Key { get; init; }
}
