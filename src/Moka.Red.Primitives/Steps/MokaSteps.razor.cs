using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Enums;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Primitives.Steps;

/// <summary>
///     A lightweight numbered step indicator for multi-step flows.
///     Simpler and more compact than <c>MokaStepper</c> — displays only numbered circles with labels
///     and connecting lines. Use for progress indicators, wizard headers, or onboarding flows.
/// </summary>
public partial class MokaSteps
{
	/// <summary>The ordered list of step labels to display.</summary>
	[Parameter]
	[EditorRequired]
	public IReadOnlyList<string> Steps { get; set; } = [];

	/// <summary>Zero-based index of the current active step. Steps before this are marked completed.</summary>
	[Parameter]
	public int CurrentStep { get; set; }

	/// <summary>Whether step markers are clickable. Defaults to false.</summary>
	[Parameter]
	public bool Clickable { get; set; }

	/// <summary>Callback invoked when a step marker is clicked. Only fires when <see cref="Clickable" /> is true.</summary>
	[Parameter]
	public EventCallback<int> OnStepClick { get; set; }

	/// <inheritdoc />
	protected override string RootClass => "moka-steps";

	private MokaColor ResolvedColor => Color ?? MokaColor.Primary;

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass($"moka-steps--{SizeToKebab(Size)}")
		.AddClass($"moka-steps--{ColorToKebab(ResolvedColor)}")
		.AddClass(Class)
		.Build();

	/// <inheritdoc />
	protected override string? CssStyle => new StyleBuilder()
		.AddStyle("margin", ResolvedMargin)
		.AddStyle("padding", ResolvedPadding)
		.AddStyle(Style)
		.Build();

	private async Task HandleStepClick(int index)
	{
		if (Clickable && OnStepClick.HasDelegate)
		{
			await OnStepClick.InvokeAsync(index);
		}
	}
}
