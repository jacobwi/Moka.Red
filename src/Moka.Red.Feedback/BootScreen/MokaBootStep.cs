namespace Moka.Red.Feedback.BootScreen;

/// <summary>One step of a startup, as <see cref="MokaBootScreen.Steps" /> shows it.</summary>
/// <param name="Label">What the step does: "Load devices".</param>
/// <param name="State">Where the step is.</param>
/// <param name="Detail">A short fact about it: "42 of 118", "router.local".</param>
/// <param name="Elapsed">How long the step took, or has taken so far.</param>
/// <param name="Error">The sentence under a failed step.</param>
public sealed record MokaBootStep(
	string Label,
	MokaBootStepState State = MokaBootStepState.Waiting,
	string? Detail = null,
	TimeSpan? Elapsed = null,
	string? Error = null);

/// <summary>Where a <see cref="MokaBootStep" /> is.</summary>
public enum MokaBootStepState
{
	/// <summary>Not started. A log doesn't print it yet.</summary>
	Waiting,

	/// <summary>Under way. A log puts the cursor on it.</summary>
	Running,

	/// <summary>Finished.</summary>
	Done,

	/// <summary>Finished with something worth knowing.</summary>
	Warning,

	/// <summary>Stopped the startup. The boot screen shows its failed state.</summary>
	Failed,

	/// <summary>Not needed this time.</summary>
	Skipped
}
