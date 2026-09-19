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
	private int _activeStep;
	private int? _lastActiveStep;

	/// <summary>The wizard step content (must contain <see cref="MokaWizardStep" /> children).</summary>
	[Parameter]
	public RenderFragment? ChildContent { get; set; }

	/// <summary>The zero-based index of the currently active step. Two-way bindable.</summary>
	[Parameter]
	public int ActiveStep { get; set; }

	/// <summary>Callback invoked when the active step changes.</summary>
	[Parameter]
	public EventCallback<int> ActiveStepChanged { get; set; }

	/// <summary>Whether to show the step indicator bar at the top. Defaults to true.</summary>
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

	/// <summary>Callback invoked when the Finish button is clicked on the last step.</summary>
	[Parameter]
	public EventCallback OnFinish { get; set; }

	/// <summary>Callback invoked when the active step changes (receives the new step index).</summary>
	[Parameter]
	public EventCallback<int> OnStepChange { get; set; }

	/// <summary>Text for the Finish button. Defaults to "Finish".</summary>
	[Parameter]
	public string FinishText { get; set; } = "Finish";

	/// <summary>Text for the Next button. Defaults to "Next".</summary>
	[Parameter]
	public string NextText { get; set; } = "Next";

	/// <summary>Text for the Previous button. Defaults to "Previous".</summary>
	[Parameter]
	public string PreviousText { get; set; } = "Previous";

	/// <inheritdoc />
	protected override string RootClass => "moka-wizard";

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
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
		}
	}

	/// <summary>Whether the step at <paramref name="index" /> is the one showing.</summary>
	internal bool IsActiveStep(int index) => index == _activeStep;

	/// <summary>Registers a child step. Called by <see cref="MokaWizardStep.OnInitialized" />.</summary>
	internal void RegisterStep(MokaWizardStep step)
	{
		if (!_steps.Contains(step))
		{
			step.Index = _steps.Count;
			_steps.Add(step);
			StateHasChanged();
		}
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

		_activeStep = index;

		if (ActiveStepChanged.HasDelegate)
		{
			await ActiveStepChanged.InvokeAsync(index);
		}

		if (OnStepChange.HasDelegate)
		{
			await OnStepChange.InvokeAsync(index);
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
}
