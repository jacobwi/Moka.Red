namespace Moka.Red.Feedback.Dialog;

/// <summary>
///     The effect painted over the page behind the dialog.
/// </summary>
public enum MokaDialogBackdrop
{
	/// <summary>Dimmed page with the standard backdrop blur. Default.</summary>
	Default,

	/// <summary>Dimmed page without blur, for the lightest overlay.</summary>
	Dim,

	/// <summary>Lighter dim with a strong blur, the frosted-glass look.</summary>
	Frosted,

	/// <summary>Heavier dim without blur, for content that must stand out sharply.</summary>
	Dark,

	/// <summary>
	///     No backdrop paint at all. The page stays fully visible; the backdrop still catches
	///     clicks, so the dialog remains modal.
	/// </summary>
	None
}
