namespace Moka.Red.Feedback.Dialog;

/// <summary>
///     Where focus goes when a <see cref="MokaDialog" /> opens. An element in the dialog marked <c>autofocus</c> or
///     <c>data-autofocus</c> takes it whatever this says.
/// </summary>
public enum MokaDialogInitialFocus
{
	/// <summary>
	///     The first control in the body, else the first action button, else a control in the header; the dialog itself
	///     when it holds nothing but its close button. The close button never takes the first focus, so its tooltip
	///     doesn't cover the dialog's first line.
	/// </summary>
	Content = 0,

	/// <summary>
	///     The first control in the dialog in tab order, which is usually the header's close button. What every dialog
	///     did before this option existed.
	/// </summary>
	FirstControl = 1,

	/// <summary>The dialog itself, so a screen reader reads it from its title and a key reaches it before any control.</summary>
	Dialog = 2
}
