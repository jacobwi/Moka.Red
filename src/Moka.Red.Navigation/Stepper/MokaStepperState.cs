namespace Moka.Red.Navigation.Stepper;

/// <summary>What a <see cref="MokaStep" /> needs from its <see cref="MokaStepper" /> to render.</summary>
/// <param name="ActiveStep">The active step index.</param>
/// <param name="Linear">Whether steps must be taken in order.</param>
/// <param name="ShowStepNumbers">Whether indicators show the step number.</param>
/// <param name="StepCount">How many steps are registered.</param>
/// <param name="Vertical">Whether the steps stack; then the active step shows its own content.</param>
internal readonly record struct MokaStepperState(int ActiveStep, bool Linear, bool ShowStepNumbers, int StepCount, bool Vertical)
{
	/// <summary>Whether the step at <paramref name="index" /> can be activated from <paramref name="activeStep" />.</summary>
	public static bool CanReach(int index, int activeStep, bool linear) => index >= 0 && (!linear || index <= activeStep + 1);

	/// <summary>Whether the step at <paramref name="index" /> can be activated now.</summary>
	public bool CanReach(int index) => CanReach(index, ActiveStep, Linear);
}
