namespace Moka.Red.Feedback.JobList;

/// <summary>Where a job in a <see cref="MokaJobList" /> has got to.</summary>
public enum MokaJobState
{
	/// <summary>Waiting for its turn.</summary>
	Queued,

	/// <summary>Going now.</summary>
	Running,

	/// <summary>Finished, and it worked.</summary>
	Done,

	/// <summary>Stopped by a problem. <see cref="MokaJob.Error" /> says which.</summary>
	Failed,

	/// <summary>Stopped by someone.</summary>
	Canceled
}
