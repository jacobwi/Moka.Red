using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Sections;

namespace Moka.Red.Feedback.Dialog;

/// <summary>
///     Puts buttons in the footer of the dialog a component was opened in by
///     <see cref="IMokaDialogService.ShowComponentAsync{TComponent}" />, where a confirm's buttons go, so they take the
///     footer's divider, padding and the dialog look's styling. Place it anywhere in the component's markup. Its content
///     belongs to the component: it reads the component's fields and cascading values and redraws whenever the component
///     does, so a Save button can turn busy or disabled. Put the main action last, as the dialog's own footers do. Outside
///     such a dialog it draws its content where it stands.
/// </summary>
/// <remarks>
///     Built on Blazor's sections (<see cref="SectionContent" />), so with two of them in one dialog the footer shows the
///     one rendered last.
/// </remarks>
public sealed class MokaDialogActions : ComponentBase
{
	/// <summary>The buttons for the footer.</summary>
	[Parameter]
	public RenderFragment? ChildContent { get; set; }

	[CascadingParameter]
	private MokaDialogContext? Dialog { get; set; }

	/// <inheritdoc />
	protected override void BuildRenderTree(RenderTreeBuilder builder)
	{
		ArgumentNullException.ThrowIfNull(builder);
		if (Dialog is null)
		{
			builder.AddContent(0, ChildContent);
			return;
		}

		builder.OpenComponent<SectionContent>(1);
		builder.AddComponentParameter(2, nameof(SectionContent.SectionId), Dialog.ActionsSection);
		builder.AddComponentParameter(3, nameof(SectionContent.ChildContent), ChildContent);
		builder.CloseComponent();
	}
}
