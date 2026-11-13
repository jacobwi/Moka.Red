namespace Moka.Red.Primitives.Media;

/// <summary>
///     What <see cref="MokaMediaGallery.OnItemOpen" /> carries: the item whose tile was pressed and where it sits in
///     <see cref="MokaMediaGallery.Items" />. The position tells apart items that are equal, and opens a
///     <see cref="MokaLightbox" /> at the right image.
/// </summary>
/// <param name="Item">The item whose tile was pressed, the instance the gallery was given.</param>
/// <param name="Index">Its position in the gallery's items, from 0.</param>
public sealed record MokaMediaItemOpenArgs(MokaMediaItem Item, int Index);
