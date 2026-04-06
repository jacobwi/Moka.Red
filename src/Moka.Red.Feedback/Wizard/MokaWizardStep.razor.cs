using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Base;
using Moka.Red.Core.Icons;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Feedback.Wizard;

/// <summary>
///     A single step within a <see cref="MokaWizard" />.
///     Rendered only when its index matches the wizard's active step.
/// </summary>
public partial class MokaWizardStep : MokaComponentBase
{
	/// <summary>The step title displayed in the step indicator.</summary>
	[Parameter]
	[EditorRequired]
	public string Title { get; set; } = string.Empty;

	/// <summary>Optional icon displayed in the step indicator circle.</summary>
	[Parameter]
	public MokaIconDefinition? Icon { get; set; }

	/// <summary>The step body content.</summary>
	[Parameter]
	public RenderFragment? ChildContent { get; set; }

	/// <summary>
	///     Whether this step's content is valid. When false, the wizard prevents advancing past this step.
	///     Defaults to true.
	/// </summary>
	[Parameter]
	public bool IsValid { get; set; } = true;

	/// <summary>The parent wizard component. Cascaded automatically.</summary>
	[CascadingParameter]
	public MokaWizard? Wizard { get; set; }

	/// <summary>The zero-based index of this step within the wizard. Set by the parent.</summary>
	internal int Index { get; set; }

	/// <inheritdoc />
	protected override string RootClass => "moka-wizard-step";

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass(Class)
		.Build();

	/// <inheritdoc />
	protected override void OnInitialized() => Wizard?.RegisterStep(this);
}
