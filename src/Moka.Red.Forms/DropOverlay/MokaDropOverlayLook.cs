namespace Moka.Red.Forms.DropOverlay;

/// <summary>
///     How a <see cref="MokaDropOverlay" /> covers the window while files are dragged over it. A drop anywhere on the
///     window counts in every look; they differ in how much of the page they hide.
/// </summary>
public enum MokaDropOverlayLook
{
	/// <summary>The page fades almost away behind a centred panel with a dashed accent border. The default.</summary>
	Standard,

	/// <summary>
	///     A real dim, so the page still shows, and a solid card saying how many files and where they go, with the accepted
	///     types and the size limit.
	/// </summary>
	Card,

	/// <summary>The page stays readable. An accent glow runs round the window's edge and a pill at the top says what a drop does.</summary>
	Frame,

	/// <summary>A small target in the bottom corner and nothing else changes. A drop anywhere still counts; the badge only says so.</summary>
	Corner,

	/// <summary>The whole window turns accent with large words. Impossible to miss, and it hides the page completely.</summary>
	Wash,

	/// <summary>An accent outline round the page and a dark banner at the bottom naming where the files go.</summary>
	Banner
}
