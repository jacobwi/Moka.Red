using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Icons;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Navigation.Stepper;

/// <summary>
///     A single step within a <see cref="MokaStepper" />. Displays a step indicator,
///     title, and optional subtitle. Inside a stepper the header is a button, so the keyboard
///     can reach and activate the step; the active step carries <c>aria-current="step"</c>.
/// </summary>
public partial class MokaStep
{
	private readonly string _generatedId = $"moka-step-{Guid.NewGuid():N}";

	/// <summary>Step content (displayed when the step is active).</summary>
	[Parameter]
	public RenderFragment? ChildContent { get; set; }

	/// <summary>Step title text.</summary>
	[Parameter]
	public string? Title { get; set; }

	/// <summary>Step subtitle text.</summary>
	[Parameter]
	public string? Subtitle { get; set; }

	/// <summary>Custom icon for the step indicator (overrides step number).</summary>
	[Parameter]
	public MokaIconDefinition? Icon { get; set; }

	/// <summary>Whether this step is completed. Shows a checkmark.</summary>
	[Parameter]
	public bool Completed { get; set; }

	/// <summary>Whether this step has an error. Shows error state.</summary>
	[Parameter]
	public bool HasError { get; set; }

	/// <summary>Whether this step is optional. Shows "Optional" label.</summary>
	[Parameter]
	public bool Optional { get; set; }

	/// <summary>Whether this step is disabled. A disabled step cannot be activated and is skipped by Tab.</summary>
	[Parameter]
	public bool Disabled { get; set; }

	[CascadingParameter] private MokaStepper? ParentStepper { get; set; }

	// Cascaded as a value so this step re-renders whenever the active step changes, even when
	// none of its own parameters did.
	[CascadingParameter(Name = "MokaStepperState")]
	private MokaStepperState StepperState { get; set; }

	/// <inheritdoc />
	protected override string RootClass => "moka-step";

	private int StepIndex => ParentStepper?.GetStepIndex(this) ?? 0;
	private bool IsActive => ParentStepper is not null && StepIndex == StepperState.ActiveStep;
	private bool IsLast => ParentStepper?.IsLastStep(this) ?? false;
	private bool ShowNumbers => ParentStepper is null || StepperState.ShowStepNumbers;
	private int StepNumber => StepIndex + 1;
	private bool CanActivate => !Disabled && ParentStepper is not null && StepperState.CanReach(StepIndex);
	private string StatusId => $"{Id ?? _generatedId}-status";

	// The indicator icons are decorative, so the state they show is spelled out for screen readers.
	private string? StatusText => HasError ? "Error" : Completed ? "Completed" : null;

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass("moka-step--active", IsActive)
		.AddClass("moka-step--completed", Completed)
		.AddClass("moka-step--error", HasError)
		.AddClass("moka-step--disabled", Disabled)
		.AddClass("moka-step--optional", Optional)
		.AddClass(Class)
		.Build();

	private string ConnectorClass => new CssBuilder("moka-step__connector")
		.AddClass("moka-step__connector--completed", Completed)
		.Build();

	protected override void OnInitialized() => ParentStepper?.RegisterStep(this);

	/// <inheritdoc />
	protected override void OnParametersSet()
	{
		base.OnParametersSet();

		// A horizontal stepper shows this step's content outside the step, from parameters it may
		// have read before these arrived.
		if (IsActive && !StepperState.Vertical)
		{
			ParentStepper?.RefreshContent();
		}
	}

	/// <inheritdoc />
	protected override async ValueTask DisposeAsyncCore()
	{
		ParentStepper?.UnregisterStep(this);
		await base.DisposeAsyncCore();
	}

	private async Task HandleClick()
	{
		if (CanActivate && ParentStepper is not null)
		{
			await ParentStepper.SetActiveStep(StepIndex);
		}
	}
}
