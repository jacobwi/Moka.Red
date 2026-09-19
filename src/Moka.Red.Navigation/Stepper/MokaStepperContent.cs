using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Moka.Red.Navigation.Stepper;

/// <summary>
///     Shows the active step's content below a horizontal <see cref="MokaStepper" />. The steps sit
///     inside cascading values, so this can render before they receive new parameters; the active
///     step then asks it to render again (<see cref="Refresh" />), which never re-renders the steps.
/// </summary>
internal sealed class MokaStepperContent : ComponentBase
{
	/// <summary>The stepper whose active step is shown.</summary>
	[Parameter]
	public MokaStepper? Stepper { get; set; }

	/// <inheritdoc />
	protected override void OnInitialized() => Stepper?.AttachContentHost(this);

	/// <summary>Renders the active step's content again.</summary>
	internal void Refresh() => StateHasChanged();

	/// <inheritdoc />
	protected override void BuildRenderTree(RenderTreeBuilder builder)
	{
		ArgumentNullException.ThrowIfNull(builder);
		if (Stepper?.ActiveStepContent is not { } content)
		{
			return;
		}

		builder.OpenElement(0, "div");
		builder.AddAttribute(1, "class", "moka-stepper__content");
		builder.AddContent(2, content);
		builder.CloseElement();
	}
}
