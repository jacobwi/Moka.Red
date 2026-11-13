namespace Moka.Red.Primitives.Media;

/// <summary>
///     Represents a media item for use in <see cref="MokaMediaGallery" /> and <see cref="MokaLightbox" />. A record, so two
///     items with the same values are equal; give each a <see cref="Key" /> to tell identical ones apart.
/// </summary>
public sealed record MokaMediaItem
{
	/// <summary>
	///     Full-resolution image source URL, shown in the lightbox. It can stay empty when the lightbox's
	///     <c>LoadSource</c> fetches the full image on demand; the tile then draws <see cref="ThumbnailSrc" />.
	/// </summary>
	public string Src { get; init; } = "";

	/// <summary>Optional thumbnail source URL. Falls back to <see cref="Src" /> if not set.</summary>
	public string? ThumbnailSrc { get; init; }

	/// <summary>Alt text for accessibility.</summary>
	public string? Alt { get; init; }

	/// <summary>Optional caption displayed in the overlay or lightbox.</summary>
	public string? Caption { get; init; }

	/// <summary>
	///     Identifies the item, such as a file id. It becomes the tile's <c>@key</c>, so a tile keeps its element when the
	///     list is reordered, and it makes two items that otherwise match unequal. A key several items share is ignored
	///     for all of them.
	/// </summary>
	public object? Key { get; init; }

	/// <summary>
	///     Whether its thumbnail is still on its way. The tile draws a skeleton in its place. A tile with no thumbnail
	///     source at all (neither <see cref="ThumbnailSrc" /> nor <see cref="Src" />) draws one too.
	/// </summary>
	public bool Loading { get; init; }

	/// <summary>
	///     Whether the item can't be shown, such as a picture the app couldn't read. The tile draws a struck-out picture
	///     and a word in place of the thumbnail, as it does for a thumbnail that fails to load, and the lightbox shows the
	///     same without reading the item. Wins over <see cref="Loading" />.
	/// </summary>
	public bool Failed { get; init; }
}
