using System.Globalization;

namespace Moka.Red.Primitives.Media;

/// <summary>
///     Every word a <see cref="MokaMediaGallery" /> and a <see cref="MokaLightbox" /> write, read aloud or shown. Set the
///     ones that should say something about your own app, or all of them to translate it. Anything holding a name or a
///     count is a function, so the words can go around it.
/// </summary>
public sealed record MokaMediaText
{
	/// <summary>The lightbox's name, read when it opens.</summary>
	public string Viewer { get; init; } = "Image viewer";

	/// <summary>
	///     Where an image sits in the list, from its 1-based position and the count: "Image 3 of 40". The lightbox says it
	///     on every step, and it names a tile with neither alt text nor a caption.
	/// </summary>
	public Func<int, int, string> Position { get; init; } =
		(position, count) => string.Create(CultureInfo.CurrentCulture, $"Image {position} of {count}");

	/// <summary>The lightbox's button to the image before.</summary>
	public string Previous { get; init; } = "Previous image";

	/// <summary>The lightbox's button to the image after.</summary>
	public string Next { get; init; } = "Next image";

	/// <summary>The lightbox's close button.</summary>
	public string Close { get; init; } = "Close";

	/// <summary>The word shown on a tile, and in the lightbox, for an image that can't be shown.</summary>
	public string Failed { get; init; } = "Can't be shown";

	/// <summary>What a screen reader hears for a tile whose thumbnail is on its way, from the tile's name.</summary>
	public Func<string, string> LoadingLabel { get; init; } = name => $"{name}, loading";

	/// <summary>What a screen reader hears for a tile or an image that can't be shown, from its name.</summary>
	public Func<string, string> FailedLabel { get; init; } = name => $"{name}, can't be shown";
}
