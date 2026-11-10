using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Enums;
using Moka.Red.Core.Icons;
using Moka.Red.Core.Theming;

namespace Moka.Red.Feedback.Dialog;

/// <summary>
///     Configuration options for a dialog invoked via <see cref="IMokaDialogService" />.
/// </summary>
public sealed class MokaDialogOptions
{
	/// <summary>Text for the confirm/OK button. Defaults to "OK".</summary>
	public string ConfirmText { get; set; } = "OK";

	/// <summary>Text for the cancel button. Defaults to "Cancel".</summary>
	public string CancelText { get; set; } = "Cancel";

	/// <summary>Color for the confirm button. Defaults to <see cref="MokaColor.Primary" />.</summary>
	public MokaColor ConfirmColor { get; set; } = MokaColor.Primary;

	/// <summary>Whether to show the close (X) button. Defaults to true.</summary>
	public bool ShowCloseButton { get; set; } = true;

	/// <summary>Whether clicking the backdrop closes the dialog. Defaults to true.</summary>
	public bool CloseOnBackdropClick { get; set; } = true;

	/// <summary>Whether pressing Escape closes the dialog. Defaults to true.</summary>
	public bool CloseOnEscape { get; set; } = true;

	/// <summary>Dialog width size. Defaults to <see cref="MokaDialogSize.Medium" />.</summary>
	public MokaDialogSize Size { get; set; } = MokaDialogSize.Medium;

	/// <summary>
	///     Where the dialog appears. <see cref="MokaDialogPosition.Center" /> is the classic centered
	///     modal; the edges anchor a sliding panel. Defaults to Center.
	/// </summary>
	public MokaDialogPosition Position { get; set; } = MokaDialogPosition.Center;

	/// <summary>
	///     The effect painted over the page behind the dialog. Defaults to
	///     <see cref="MokaDialogBackdrop.Default" />.
	/// </summary>
	public MokaDialogBackdrop Backdrop { get; set; } = MokaDialogBackdrop.Default;

	/// <summary>Whether to prevent body scrolling when the dialog is open. Defaults to true.</summary>
	public bool PreventScroll { get; set; } = true;

	/// <summary>Icon shown before the title in the header.</summary>
	public MokaIconDefinition? Icon { get; set; }

	/// <summary>
	///     The tint of the tile the <see cref="Icon" /> sits in under the <see cref="MokaDialogLook.IconLead" /> and
	///     <see cref="MokaDialogLook.Centered" /> looks. Null follows <see cref="ConfirmColor" />, so a confirm with an
	///     Error button shows its icon in the error tint. See <see cref="MokaDialog.IconColor" />.
	/// </summary>
	public MokaColor? IconColor { get; set; }

	/// <summary>
	///     How the dialog is drawn. Null takes the theme's choice (<see cref="MokaTheme.WithDialogs" />). Only the new
	///     design draws the looks; see <see cref="MokaDialog.Look" />.
	/// </summary>
	public MokaDialogLook? Look { get; set; }

	/// <summary>Controls in the header, between the title and the close button.</summary>
	public RenderFragment? HeaderActions { get; set; }

	/// <summary>Whether the body has padding of its own. Defaults to true.</summary>
	public bool BodyPadding { get; set; } = true;

	/// <summary>
	///     Whether the body scrolls. Defaults to true. With it off the body clips and a single child
	///     fills it, for a component that scrolls itself.
	/// </summary>
	public bool ScrollBody { get; set; } = true;

	/// <summary>Width of the dialog, overriding <see cref="Size" />. A CSS length.</summary>
	public string? Width { get; set; }

	/// <summary>Maximum width of the dialog. A CSS length.</summary>
	public string? MaxWidth { get; set; }

	/// <summary>Height of the dialog. A CSS length.</summary>
	public string? Height { get; set; }
}
