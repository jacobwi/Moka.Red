using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Icons;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Navigation.Stepper;

/// <summary>
///     A single step within a <see cref="MokaStepper" />. Displays a step indicator,
///     title, and optional subtitle.
/// </summary>
public partial class MokaStep
{
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

	/// <summary>Whether this step is disabled.</summary>
	[Parameter]
	public bool Disabled { get; set; }

	[CascadingParameter] private MokaStepper? ParentStepper { get; set; }

	/// <inheritdoc />
	protected override string RootClass => "moka-step";

	private int StepIndex => ParentStepper?.GetStepIndex(this) ?? 0;
	private bool IsActive => ParentStepper?.IsActiveStep(this) ?? false;
	private bool IsLast => ParentStepper?.IsLastStep(this) ?? false;
	private bool ShowNumbers => ParentStepper?.ShowStepNumbers ?? true;
	private int StepNumber => StepIndex + 1;

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass("moka-step--active", IsActive)
		.AddClass("moka-step--completed", Completed)
		.AddClass("moka-step--error", HasError)
		.AddClass("moka-step--disabled", Disabled)
		.AddClass("moka-step--optional", Optional)
		.AddClass(Class)
		.Build();

	protected override void OnInitialized() => ParentStepper?.RegisterStep(this);

	/// <inheritdoc />
	protected override async ValueTask DisposeAsyncCore()
	{
		ParentStepper?.UnregisterStep(this);
		await base.DisposeAsyncCore();
	}

	private async Task HandleClick()
	{
		if (!Disabled && ParentStepper is not null)
		{
			await ParentStepper.SetActiveStep(StepIndex);
		}
	}
}
