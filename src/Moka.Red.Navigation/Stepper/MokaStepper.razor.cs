using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Base;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Navigation.Stepper;

/// <summary>
///     A step-by-step navigation component that guides users through a multi-step process.
///     Contains <see cref="MokaStep" /> children.
/// </summary>
public partial class MokaStepper
{
	private readonly MokaChildList<MokaStep> _steps;
	private MokaStepperContent? _contentHost;
	private int _generation;

	// The rendered state. The ActiveStep parameter only seeds it when the parent passes a new
	// value, so a parent re-render with the same one-way value does not undo the user's choice.
	private int _activeStep;
	private int _lastActiveStepParameter;

	/// <summary>Creates the stepper.</summary>
	public MokaStepper()
	{
		_steps = new MokaChildList<MokaStep>(StateHasChanged, StepsChanged, static step => step.Order);
	}

	/// <summary>Child step elements.</summary>
	[Parameter]
	public RenderFragment? ChildContent { get; set; }

	/// <summary>
	///     Currently active step index (zero-based). Two-way bindable. A one-way value sets the
	///     active step only when it changes, so a step the user picked survives a parent re-render.
	/// </summary>
	[Parameter]
	public int ActiveStep { get; set; }

	/// <summary>Callback when the user activates a step.</summary>
	[Parameter]
	public EventCallback<int> ActiveStepChanged { get; set; }

	/// <summary>Layout orientation. Default Horizontal.</summary>
	[Parameter]
	public MokaStepperOrientation Orientation { get; set; } = MokaStepperOrientation.Horizontal;

	/// <summary>When true, steps must be completed in order: only earlier steps and the next one can be activated.</summary>
	[Parameter]
	public bool Linear { get; set; }

	/// <summary>Whether to show step numbers in the indicator. Default true.</summary>
	[Parameter]
	public bool ShowStepNumbers { get; set; } = true;

	/// <inheritdoc />
	protected override string RootClass => "moka-stepper";

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass($"moka-stepper--{MokaEnumHelpers.ToCssClass(Orientation)}")
		.AddClass("moka-stepper--linear", Linear)
		.AddClass(Class)
		.Build();

	private MokaStepperState State =>
		new(_activeStep, Linear, ShowStepNumbers, _steps.Count, Orientation == MokaStepperOrientation.Vertical);

	// Opened from code: Razor markup does not find internal components.
	private RenderFragment ContentHost => builder =>
	{
		builder.OpenComponent<MokaStepperContent>(0);
		builder.AddAttribute(1, nameof(MokaStepperContent.Stepper), this);
		builder.CloseComponent();
	};

	/// <summary>
	///     The active step's content, shown below the steps of a horizontal stepper. A vertical stepper
	///     leaves it to the step, which shows it under its own header.
	/// </summary>
	internal RenderFragment? ActiveStepContent =>
		Orientation == MokaStepperOrientation.Horizontal && _activeStep >= 0 && _activeStep < _steps.Count
			? _steps[_activeStep].ChildContent
			: null;

	/// <inheritdoc />
	protected override bool ShouldRender() => true;

	/// <inheritdoc />
	protected override void OnParametersSet()
	{
		base.OnParametersSet();

		if (ActiveStep != _lastActiveStepParameter)
		{
			_lastActiveStepParameter = ActiveStep;
			_activeStep = ActiveStep;
		}
	}

	// The steps keep the order they stand in the markup, as QuickGrid collects its columns, so a step inside an @if that
	// comes back takes its place again.
	internal void RegisterStep(MokaStep step) => _steps.Add(step);

	internal void UnregisterStep(MokaStep step) => _steps.Remove(step);

	internal void SawStep(MokaStep step) => _steps.Saw(step);

	internal int GetStepIndex(MokaStep step) => _steps.IndexOf(step);

	// The active step stays active wherever it moved to, and a bound parent hears its new position. Steps that show no
	// content of their own take only plain values, which Blazor doesn't hand them again, so they don't say where they
	// stand: when one comes back among them, the steps are created again and report in markup order.
	private void StepsChanged(IReadOnlyList<MokaStep> before)
	{
		if (_steps.Guessed)
		{
			_steps.Clear();
			_generation++;
			return;
		}

		int active = _steps.IndexNow(before, _activeStep);
		if (active == _activeStep)
		{
			return;
		}

		_activeStep = active;
		if (ActiveStepChanged.HasDelegate)
		{
			RunDetached(() => ActiveStepChanged.InvokeAsync(active));
		}
	}

	internal void AttachContentHost(MokaStepperContent host) => _contentHost = host;

	/// <summary>Called by the active step when it gets new parameters, so the content shown is its latest.</summary>
	internal void RefreshContent() => _contentHost?.Refresh();

	internal bool IsLastStep(MokaStep step) => _steps.Count > 0 && _steps[^1] == step;

	internal async Task SetActiveStep(int index)
	{
		if (!MokaStepperState.CanReach(index, _activeStep, Linear))
		{
			return;
		}

		_activeStep = index;
		StateHasChanged();

		if (ActiveStepChanged.HasDelegate)
		{
			await ActiveStepChanged.InvokeAsync(index);
		}
	}
}
