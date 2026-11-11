namespace Moka.Red.Core.Enums;

/// <summary>
///     Where the pattern of a <c>MokaGridBackground</c> fades out.
/// </summary>
public enum MokaGridFade
{
	/// <summary>
	///     Full behind the middle, gone toward the corners. The original fade, shaped by <c>FadeStart</c> and
	///     <c>FadeEnd</c>.
	/// </summary>
	Center,

	/// <summary>Full along the top edge, gone by the bottom. Sits well at the top of a page.</summary>
	Bottom,

	/// <summary>Full along the bottom edge, gone at the top. For a band above a footer.</summary>
	Top,

	/// <summary>Strong at the edges and clear in the middle, so no lines run behind the content.</summary>
	Frame,

	/// <summary>Faint everywhere and full in a circle that follows the pointer, resting in the centre.</summary>
	Spotlight,

	/// <summary>The pattern runs edge to edge at one strength.</summary>
	None
}
