using Moka.Red.Core.Icons;

namespace Moka.Red.Feedback.JobList;

/// <summary>
///     One job in a <see cref="MokaJobList" />. A record, so a job that hasn't changed compares equal and its row skips
///     the render. Keeping the record for a job that didn't move and making a new equal one cost the same.
/// </summary>
/// <param name="Id">Tells this job from the others, and keeps its row when the list reorders.</param>
/// <param name="Name">What the job is working on, such as a file name.</param>
/// <param name="State">Where the job has got to.</param>
public sealed record MokaJob(string Id, string Name, MokaJobState State = MokaJobState.Queued)
{
	/// <summary>Where it's going or what it's part of, such as "To Videos". Comes first on the second line.</summary>
	public string? Detail { get; init; }

	/// <summary>
	///     How much is done, 0 to 100. Null works it out from <see cref="Bytes" /> and <see cref="TotalBytes" /> when
	///     both are known, and otherwise means there's no number yet, so a short bar slides along the track.
	/// </summary>
	public double? Percent { get; init; }

	/// <summary>Bytes done so far.</summary>
	public long? Bytes { get; init; }

	/// <summary>Bytes the whole job takes, when known.</summary>
	public long? TotalBytes { get; init; }

	/// <summary>How fast it's going, in bytes a second. Shows only while running.</summary>
	public double? BytesPerSecond { get; init; }

	/// <summary>How long is left. Shows only while running.</summary>
	public TimeSpan? TimeRemaining { get; init; }

	/// <summary>What went wrong, in a sentence, for a <see cref="MokaJobState.Failed" /> job.</summary>
	public string? Error { get; init; }

	/// <summary>An icon before the name, such as an arrow for its direction.</summary>
	public MokaIconDefinition? Icon { get; init; }
}
