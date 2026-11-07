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
	private (string Title, string? Decided, bool HasProblem, bool IsValid) _reported;

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

	/// <summary>What this step decided so far, under its title in a <see cref="MokaWizard.Rail" />.</summary>
	[Parameter]
	public string? Decided { get; set; }

	/// <summary>
	///     Puts a mark on this step in a <see cref="MokaWizard.Rail" />, for a problem to come back to. The step showing
	///     keeps its number.
	/// </summary>
	[Parameter]
	public bool HasProblem { get; set; }

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

	/// <inheritdoc />
	protected override void OnParametersSet()
	{
		base.OnParametersSet();

		(string, string?, bool, bool) current = (Title, Decided, HasProblem, IsValid);
		if (current != _reported)
		{
			_reported = current;
			Wizard?.RefreshRail();
		}
	}
}
