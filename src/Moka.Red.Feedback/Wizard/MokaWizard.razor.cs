using System.Globalization;
using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Base;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Feedback.Wizard;

/// <summary>
///     A multi-step form wizard with step indicator, navigation buttons, and validation.
///     Wrap <see cref="MokaWizardStep" /> children inside this component.
/// </summary>
public partial class MokaWizard : MokaComponentBase
{
	private readonly List<MokaWizardStep> _steps = [];
	private readonly List<MokaWizardStep> _seen = [];
	private bool _collecting;
	private bool _stepsChanged;
	private int _activeStep;
	private int? _lastActiveStep;
	private int _reached;
	private bool _disposed;

	/// <summary>The wizard step content (must contain <see cref="MokaWizardStep" /> children).</summary>
	[Parameter]
	public RenderFragment? ChildContent { get; set; }

	/// <summary>The zero-based index of the currently active step. Two-way bindable.</summary>
	[Parameter]
	public int ActiveStep { get; set; }

	/// <summary>Callback invoked when the active step changes.</summary>
	[Parameter]
	public EventCallback<int> ActiveStepChanged { get; set; }

	/// <summary>Whether to show the step indicator bar at the top, or the rail with <see cref="Rail" />. Defaults to true.</summary>
	[Parameter]
	public bool ShowStepIndicator { get; set; } = true;

	/// <summary>Whether to show Previous/Next/Finish navigation buttons. Defaults to true.</summary>
	[Parameter]
	public bool ShowNavigation { get; set; } = true;

	/// <summary>
	///     When true, steps must be completed in order - the user cannot skip ahead.
	///     Defaults to false.
	/// </summary>
	[Parameter]
	public bool Linear { get; set; }

	/// <summary>
	///     Draws the steps in a rail down the side, with what each decided so far
	///     (<see cref="MokaWizardStep.Decided" />) and a mark on a problem (<see cref="MokaWizardStep.HasProblem" />).
	///     Every step reached can be clicked, whatever <see cref="Linear" /> says. The buttons sit in a bar that stays at
	///     the bottom while the wizard scrolls, and under 900 px a progress bar replaces the rail.
	/// </summary>
	[Parameter]
	public bool Rail { get; set; }

	/// <summary>Whether the bar shows a Skip button, on every step but the last. Only with <see cref="Rail" />.</summary>
	[Parameter]
	public bool ShowSkip { get; set; }

	/// <summary>Text for the Skip button. Defaults to "Skip".</summary>
	[Parameter]
	public string SkipText { get; set; } = "Skip";

	/// <summary>Callback invoked when Skip is clicked, before the wizard moves on. Skip ignores the step's validity.</summary>
	[Parameter]
	public EventCallback OnSkip { get; set; }

	/// <summary>The name of the rail's step list and of the progress bar. Defaults to "Steps".</summary>
	[Parameter]
	public string RailLabel { get; set; } = "Steps";

	/// <summary>The word a screen reader hears before the title of a step with a problem. Defaults to "Problem".</summary>
	[Parameter]
	public string ProblemText { get; set; } = "Problem";

	/// <summary>
	///     Writes the count over the progress bar from the step's number and the number of steps. Defaults to
	///     "Step 3 of 5".
	/// </summary>
	[Parameter]
	public Func<int, int, string>? StepCountText { get; set; }

	/// <summary>Callback invoked when the Finish button is clicked on the last step.</summary>
	[Parameter]
	public EventCallback OnFinish { get; set; }

	/// <summary>Callback invoked when the active step changes (receives the new step index).</summary>
	[Parameter]
	public EventCallback<int> OnStepChange { get; set; }

	/// <summary>Text for the Finish button. Defaults to "Finish".</summary>
	[Parameter]
	public string FinishText { get; set; } = "Finish";

	/// <summary>Text for the Next button. Defaults to "Next", or "Continue" with <see cref="Rail" />.</summary>
	[Parameter]
	public string? NextText { get; set; }

	/// <summary>Text for the Previous button. Defaults to "Previous", or "Back" with <see cref="Rail" />.</summary>
	[Parameter]
	public string? PreviousText { get; set; }

	/// <inheritdoc />
	protected override string RootClass => "moka-wizard";

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass("moka-wizard--rail", Rail)
		.AddClass(Class)
		.Build();

	/// <inheritdoc />
	protected override string? CssStyle => new StyleBuilder()
		.AddStyle(Style)
		.Build();

	/// <summary>Total number of registered steps.</summary>
	private int StepCount => _steps.Count;

	/// <summary>Whether the current step is the first.</summary>
	private bool IsFirstStep => _activeStep == 0;

	/// <summary>Whether the current step is the last.</summary>
	private bool IsLastStep => _activeStep >= StepCount - 1;

	/// <summary>Whether the current step's content is valid.</summary>
	private bool CurrentStepValid => _activeStep < _steps.Count && _steps[_activeStep].IsValid;

	private string ResolvedNextText => NextText ?? (Rail ? "Continue" : "Next");

	private string ResolvedPreviousText => PreviousText ?? (Rail ? "Back" : "Previous");

	private string? ActiveStepTitle => _activeStep < _steps.Count ? _steps[_activeStep].Title : null;

	private double ProgressValue => StepCount == 0 ? 0 : (double)Math.Min(_activeStep + 1, StepCount) / StepCount * 100;

	/// <inheritdoc />
	protected override bool ShouldRender() => true;

	/// <inheritdoc />
	protected override void OnParametersSet()
	{
		base.OnParametersSet();

		// ActiveStep only moves the wizard when the parent passes a new value. Copying it on every
		// parent render sent an unbound wizard back to the step it started on.
		if (_lastActiveStep != ActiveStep)
		{
			_lastActiveStep = ActiveStep;
			_activeStep = ActiveStep;
			_reached = Math.Max(_reached, ActiveStep);
		}
	}

	/// <summary>Whether the step at <paramref name="index" /> is the one showing.</summary>
	internal bool IsActiveStep(int index) => index == _activeStep;

	// The step list is rebuilt on every render in the order the steps take their parameters, which is their order in
	// this wizard's content, so a step that comes back takes its place again. This is how QuickGrid collects its columns
	// (StartCollectingColumns, AddColumn, and a Defer component after the child content).
	private void StartCollecting()
	{
		_seen.Clear();
		_collecting = true;
	}

	internal void SawStep(MokaWizardStep step)
	{
		if (_collecting && !_seen.Contains(step))
		{
			_seen.Add(step);
		}
	}

	/// <summary>
	///     Adds a step that started outside a render of this wizard's content, such as one inside another component that
	///     rendered later. It goes last; the steps collected on later renders keep it in its place.
	/// </summary>
	internal void RegisterStep(MokaWizardStep step)
	{
		if (_collecting || _steps.Contains(step))
		{
			return;
		}

		step.Index = _steps.Count;
		_steps.Add(step);
		Refresh();
	}

	// A step the render didn't reach (one inside a component that didn't render again) stays after the step it followed.
	internal void FinishCollecting()
	{
		if (!_collecting)
		{
			return;
		}

		_collecting = false;
		List<MokaWizardStep> order = [.. _seen];
		MokaWizardStep? previous = null;
		foreach (MokaWizardStep step in _steps)
		{
			if (!_seen.Contains(step))
			{
				order.Insert(previous is null ? 0 : order.IndexOf(previous) + 1, step);
			}

			previous = step;
		}

		if (!order.SequenceEqual(_steps))
		{
			ApplyOrder(order);
		}
		else if (_stepsChanged)
		{
			_stepsChanged = false;
			StateHasChanged();
		}
	}

	// The step showing stays showing wherever it moved to, and a bound parent hears its new position.
	private void ApplyOrder(List<MokaWizardStep> order)
	{
		MokaWizardStep? showing = _activeStep < _steps.Count ? _steps[_activeStep] : null;
		MokaWizardStep? furthest = _reached < _steps.Count ? _steps[_reached] : null;

		_steps.Clear();
		_steps.AddRange(order);
		for (int i = 0; i < _steps.Count; i++)
		{
			_steps[i].Index = i;
		}

		int active = showing is null ? _activeStep : _steps.IndexOf(showing);
		_reached = Math.Max(furthest is null ? _reached : _steps.IndexOf(furthest), active);
		bool moved = active != _activeStep;
		_activeStep = active;
		_stepsChanged = false;
		StateHasChanged();

		if (moved && ActiveStepChanged.HasDelegate)
		{
			RunDetached(() => ActiveStepChanged.InvokeAsync(active));
		}
	}

	/// <summary>
	///     Forgets a step the page no longer renders. The steps after it move up, and the step on screen stays on screen:
	///     when the removed one came before it, the wizard's position moves back by one and a bound parent hears it.
	/// </summary>
	internal void UnregisterStep(MokaWizardStep step)
	{
		int index = _steps.IndexOf(step);
		if (index < 0 || _disposed)
		{
			return;
		}

		_steps.RemoveAt(index);
		for (int i = index; i < _steps.Count; i++)
		{
			_steps[i].Index = i;
		}

		int active = index < _activeStep ? _activeStep - 1 : Math.Min(_activeStep, Math.Max(_steps.Count - 1, 0));
		_reached = Math.Max(index < _reached ? _reached - 1 : _reached, active);
		bool moved = active != _activeStep;
		_activeStep = active;
		Refresh();

		if (moved && ActiveStepChanged.HasDelegate)
		{
			RunDetached(() => ActiveStepChanged.InvokeAsync(active));
		}
	}

	/// <inheritdoc />
	protected override ValueTask DisposeAsyncCore()
	{
		_disposed = true;
		return base.DisposeAsyncCore();
	}

	// The steps take their parameters only after this component has rendered, so whatever it drew from a step (the
	// indicator, the rail, whether Next and Finish are enabled) was what the step said before. A step whose state
	// changed asks for another render, which Blazor runs in the same batch.
	internal void StepChanged() => Refresh();

	// While the content renders, the render that ends the collection draws once for every change.
	private void Refresh()
	{
		if (_collecting)
		{
			_stepsChanged = true;
			return;
		}

		StateHasChanged();
	}

	private async Task GoToStep(int index)
	{
		if (index < 0 || index >= StepCount)
		{
			return;
		}

		if (Linear && index > _activeStep && !CurrentStepValid)
		{
			return;
		}

		await MoveToStep(index);
	}

	private async Task MoveToStep(int index)
	{
		_activeStep = index;
		_reached = Math.Max(_reached, index);

		if (ActiveStepChanged.HasDelegate)
		{
			await ActiveStepChanged.InvokeAsync(index);
		}

		if (OnStepChange.HasDelegate)
		{
			await OnStepChange.InvokeAsync(index);
		}
	}

	private async Task GoToReachedStep(int index)
	{
		if (index != _activeStep && index <= _reached && index < StepCount)
		{
			await MoveToStep(index);
		}
	}

	private async Task PreviousStep() => await GoToStep(_activeStep - 1);

	private async Task NextStep()
	{
		if (!CurrentStepValid)
		{
			return;
		}

		await GoToStep(_activeStep + 1);
	}

	private async Task Skip()
	{
		if (OnSkip.HasDelegate)
		{
			await OnSkip.InvokeAsync();
		}

		if (!IsLastStep)
		{
			await MoveToStep(_activeStep + 1);
		}
	}

	private async Task Finish()
	{
		if (!CurrentStepValid)
		{
			return;
		}

		if (OnFinish.HasDelegate)
		{
			await OnFinish.InvokeAsync();
		}
	}

	private string ProgressCountText()
	{
		int number = Math.Min(_activeStep + 1, StepCount);
		return StepCountText?.Invoke(number, StepCount)
		       ?? string.Create(CultureInfo.CurrentCulture, $"Step {number} of {StepCount}");
	}

	private RailState RailStateOf(int index, MokaWizardStep step)
	{
		if (index == _activeStep)
		{
			return RailState.Now;
		}

		if (step.HasProblem)
		{
			return RailState.Problem;
		}

		return index < _reached ? RailState.Done : RailState.Later;
	}

	private static string RailStepClass(RailState state) => new CssBuilder("moka-wizard-rail-step")
		.AddClass("moka-wizard-rail-step--now", state == RailState.Now)
		.AddClass("moka-wizard-rail-step--problem", state == RailState.Problem)
		.AddClass("moka-wizard-rail-step--done", state == RailState.Done)
		.AddClass("moka-wizard-rail-step--later", state == RailState.Later)
		.Build();

	private string StepIndicatorClass(int index)
	{
		return new CssBuilder("moka-wizard-indicator-step")
			.AddClass("moka-wizard-indicator-step--active", index == _activeStep)
			.AddClass("moka-wizard-indicator-step--completed", index < _activeStep)
			.AddClass("moka-wizard-indicator-step--upcoming", index > _activeStep)
			.Build();
	}

	private string ConnectorClass(int index) => new CssBuilder("moka-wizard-indicator-connector")
		.AddClass("moka-wizard-indicator-connector--active", index <= _activeStep)
		.Build();

	private bool CanClickStep(int index)
	{
		if (!Linear)
		{
			return true;
		}

		return index <= _activeStep;
	}

	private enum RailState
	{
		Later,
		Done,
		Now,
		Problem
	}
}
