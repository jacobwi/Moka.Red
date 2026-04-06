namespace Moka.Red.Primitives.Media;

/// <summary>
///     Represents a media item for use in <see cref="MokaMediaGallery" />.
/// </summary>
public sealed record MokaMediaItem
{
	/// <summary>Full-resolution image source URL.</summary>
	public required string Src { get; init; }

	/// <summary>Optional thumbnail source URL. Falls back to <see cref="Src" /> if not set.</summary>
	public string? ThumbnailSrc { get; init; }

	/// <summary>Alt text for accessibility.</summary>
	public string? Alt { get; init; }

	/// <summary>Optional caption displayed in the overlay or lightbox.</summary>
	public string? Caption { get; init; }
}
