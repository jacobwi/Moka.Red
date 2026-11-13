/**
 * MokaMediaGallery JS module - moves focus to a tile, for FocusItemAsync. The gallery's lightbox is a MokaLightbox,
 * which has its own module.
 */

/**
 * Focuses the tile at a position in the gallery's items. The root's children are the tiles, in order, or with captions
 * the figures that hold them.
 * @param {HTMLElement} root - The gallery's root element.
 * @param {number} index - The tile's position, from 0.
 */
export function focusItem(root, index) {
	const cell = root?.children[index];
	const tile = cell?.matches('figure') ? cell.firstElementChild : cell;
	if (tile instanceof HTMLElement && tile.matches('button.moka-media-gallery-item')) {
		tile.focus();
	}
}
