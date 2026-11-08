namespace Moka.Red.Feedback.JobList;

/// <summary>How many jobs of a <see cref="MokaJobList" /> are in each state, for its summary.</summary>
/// <param name="Queued">Waiting for their turn.</param>
/// <param name="Running">Going now.</param>
/// <param name="Done">Finished, and they worked.</param>
/// <param name="Failed">Stopped by a problem.</param>
/// <param name="Canceled">Stopped by someone.</param>
public readonly record struct MokaJobCounts(int Queued, int Running, int Done, int Failed, int Canceled)
{
	/// <summary>Jobs that have stopped, however they ended. Clear finished is on offer while there are any.</summary>
	public int Finished => Done + Failed + Canceled;
}
