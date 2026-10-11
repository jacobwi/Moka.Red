namespace Moka.Red.Feedback.Dialog;

/// <summary>
///     Predefined dialog width sizes.
/// </summary>
public enum MokaDialogSize
{
	/// <summary>Small dialog (360px).</summary>
	Small,

	/// <summary>Medium dialog (520px). Default.</summary>
	Medium,

	/// <summary>Large dialog (720px).</summary>
	Large,

	/// <summary>Full-screen dialog.</summary>
	FullScreen,

	/// <summary>
	///     Wide dialog for a view with its own layout, such as a list beside a detail panel:
	///     1200px by 900px, each kept inside the window. Declared last so FullScreen keeps its value.
	/// </summary>
	ExtraLarge
}
