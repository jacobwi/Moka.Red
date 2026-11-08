using System.Globalization;

namespace Moka.Red.Feedback.JobList;

/// <summary>
///     Every word a <see cref="MokaJobList" /> writes. Set the ones that should say something about your own app, such
///     as "transfers" for "jobs", or all of them to translate it. Anything holding a name or a count is a function, so
///     the words can go around it and plurals can follow it.
/// </summary>
public sealed record MokaJobListText
{
	/// <summary>The name a screen reader gives the list.</summary>
	public string ListLabel { get; init; } = "Jobs";

	/// <summary>Shown for a job waiting its turn.</summary>
	public string Queued { get; init; } = "Queued";

	/// <summary>What a screen reader hears from a running job's status dot.</summary>
	public string Running { get; init; } = "Running";

	/// <summary>Shown for a job that finished.</summary>
	public string Done { get; init; } = "Done";

	/// <summary>What a screen reader hears from a failed job's status dot.</summary>
	public string Failed { get; init; } = "Failed";

	/// <summary>Shown for a job someone stopped.</summary>
	public string Canceled { get; init; } = "Canceled";

	/// <summary>The failure line for a job with no <see cref="MokaJob.Error" />.</summary>
	public string NoReason { get; init; } = "It didn't finish.";

	/// <summary>The header button for the jobs that have stopped.</summary>
	public string ClearFinished { get; init; } = "Clear finished";

	/// <summary>Title of the empty state.</summary>
	public string Empty { get; init; } = "No jobs";

	/// <summary>Second line of the empty state, or null for none.</summary>
	public string? EmptyHint { get; init; }

	/// <summary>Button that goes back to the first <c>MaxVisible</c> jobs.</summary>
	public string ShowLess { get; init; } = "Show less";

	/// <summary>What goes between the facts on a job's second line.</summary>
	public string Separator { get; init; } = ", ";

	/// <summary>Names the cancel button, from the job's name. Shown in its tooltip and read by screen readers.</summary>
	public Func<string, string> Cancel { get; init; } = name => $"Cancel {name}";

	/// <summary>Names the retry button, from the job's name. Shown in its tooltip and read by screen readers.</summary>
	public Func<string, string> Retry { get; init; } = name => $"Retry {name}";

	/// <summary>Names the remove button, from the job's name. Shown in its tooltip and read by screen readers.</summary>
	public Func<string, string> Remove { get; init; } = name => $"Remove {name}";

	/// <summary>Button that shows the jobs past <c>MaxVisible</c>, from how many are hidden.</summary>
	public Func<int, string> ShowMore { get; init; } = hidden => string.Create(CultureInfo.CurrentCulture, $"Show {hidden} more");

	/// <summary>The header's summary, such as "2 running, 1 failed".</summary>
	public Func<MokaJobCounts, string> Summary { get; init; } = WriteSummary;

	/// <summary>How done and whole read together, such as "1.2 of 1.9 GB". Done comes first.</summary>
	public Func<string, string, string> Amount { get; init; } = (done, whole) => $"{done} of {whole}";

	/// <summary>How the time left reads, such as "1 min left".</summary>
	public Func<TimeSpan, string> TimeLeft { get; init; } = WriteTimeLeft;

	/// <summary>What a screen reader hears when a job fails.</summary>
	public Func<MokaJob, string> FailureAnnouncement { get; init; } = job =>
		string.IsNullOrWhiteSpace(job.Error) ? $"{job.Name} failed" : $"{job.Name} failed: {job.Error}";

	private static string WriteSummary(MokaJobCounts counts)
	{
		List<string> parts = new(5);
		AddCount(parts, counts.Running, "running");
		AddCount(parts, counts.Queued, "queued");
		AddCount(parts, counts.Failed, "failed");
		AddCount(parts, counts.Canceled, "canceled");
		AddCount(parts, counts.Done, "done");
		return string.Join(", ", parts);
	}

	private static void AddCount(List<string> parts, int count, string word)
	{
		if (count > 0)
		{
			parts.Add(string.Create(CultureInfo.CurrentCulture, $"{count} {word}"));
		}
	}

	// An estimate moves on every tick, so seconds only show once they're all that's left. Rounded up, so it never
	// says "0 s left" while the job is still going.
	private static string WriteTimeLeft(TimeSpan left)
	{
		int seconds = (int)Math.Clamp(Math.Ceiling(left.TotalSeconds), 1, int.MaxValue - 59);
		if (seconds < 60)
		{
			return string.Create(CultureInfo.CurrentCulture, $"{seconds} s left");
		}

		int minutes = (seconds + 59) / 60;
		if (minutes < 60)
		{
			return string.Create(CultureInfo.CurrentCulture, $"{minutes} min left");
		}

		int hours = minutes / 60;
		int rest = minutes % 60;
		return rest == 0
			? string.Create(CultureInfo.CurrentCulture, $"{hours} h left")
			: string.Create(CultureInfo.CurrentCulture, $"{hours} h {rest} min left");
	}
}
