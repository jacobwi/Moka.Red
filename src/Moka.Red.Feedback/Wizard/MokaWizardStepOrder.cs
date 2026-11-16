using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Moka.Red.Feedback.Wizard;

// Sits after the wizard's content, so it renders once every step there has taken its parameters, and settles the step
// order then. QuickGrid's Defer does the same for its columns.
internal sealed class MokaWizardStepOrder : ComponentBase
{
	[Parameter]
	[EditorRequired]
	public MokaWizard Wizard { get; set; } = default!;

	// Razor doesn't find an internal component by its tag, so the wizard's markup opens it through this.
	public static RenderFragment For(MokaWizard wizard) => builder =>
	{
		builder.OpenComponent<MokaWizardStepOrder>(0);
		builder.AddComponentParameter(1, nameof(Wizard), wizard);
		builder.CloseComponent();
	};

	protected override void BuildRenderTree(RenderTreeBuilder builder) => Wizard.FinishCollecting();
}
