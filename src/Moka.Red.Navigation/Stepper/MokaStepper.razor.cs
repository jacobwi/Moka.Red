using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Navigation.Stepper;

/// <summary>
///     A step-by-step navigation component that guides users through a multi-step process.
///     Contains <see cref="MokaStep" /> children.
/// </summary>
public partial class MokaStepper
{
	private readonly List<MokaStep> _steps = [];

	/// <summary>Child step elements.</summary>
	[Parameter]
	public RenderFragment? ChildContent { get; set; }

	/// <summary>Currently active step index (zero-based). Two-way bindable.</summary>
	[Parameter]
	public int ActiveStep { get; set; }

	/// <summary>Callback when <see cref="ActiveStep" /> changes.</summary>
	[Parameter]
	public EventCallback<int> ActiveStepChanged { get; set; }

	/// <summary>Layout orientation. Default Horizontal.</summary>
	[Parameter]
	public MokaStepperOrientation Orientation { get; set; } = MokaStepperOrientation.Horizontal;

	/// <summary>When true, steps must be completed in order.</summary>
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

	/// <inheritdoc />
	protected override bool ShouldRender() => true;

	internal void RegisterStep(MokaStep step)
	{
		if (!_steps.Contains(step))
		{
			_steps.Add(step);
			StateHasChanged();
		}
	}

	internal void UnregisterStep(MokaStep step)
	{
		if (_steps.Remove(step))
		{
			StateHasChanged();
		}
	}

	internal int GetStepIndex(MokaStep step) => _steps.IndexOf(step);

	internal bool IsActiveStep(MokaStep step) => GetStepIndex(step) == ActiveStep;

	internal bool IsLastStep(MokaStep step) => _steps.Count > 0 && _steps[^1] == step;

	internal async Task SetActiveStep(int index)
	{
		if (Linear && index > ActiveStep + 1)
		{
			return;
		}

		ActiveStep = index;
		if (ActiveStepChanged.HasDelegate)
		{
			await ActiveStepChanged.InvokeAsync(ActiveStep);
		}
	}
}
