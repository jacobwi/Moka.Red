using Moka.Red.Core.Enums;

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

	/// <summary>Whether to prevent body scrolling when the dialog is open. Defaults to true.</summary>
	public bool PreventScroll { get; set; } = true;
}
