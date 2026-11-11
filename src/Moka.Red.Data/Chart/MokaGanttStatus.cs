namespace Moka.Red.Data.Chart;

/// <summary>Where a job in a <see cref="MokaGanttChart" /> stands. Each is drawn in its own status color with an icon and a word.</summary>
public enum MokaGanttStatus
{
	/// <summary>Not started yet: an outline in the neutral color, with a clock.</summary>
	Planned,

	/// <summary>Started and not finished: the info color, with a play mark, its bar open at the end.</summary>
	Running,

	/// <summary>Finished well: the success color, with a check.</summary>
	Done,

	/// <summary>Finished badly: the error color, with an alert mark.</summary>
	Failed,

	/// <summary>Not run: a thin muted bar, with a dash.</summary>
	Skipped
}
