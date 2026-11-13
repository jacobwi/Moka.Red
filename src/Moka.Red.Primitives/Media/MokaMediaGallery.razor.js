/**
 * MokaMediaGallery JS module - moves focus to a tile, for FocusItemAsync. The gallery's lightbox is a MokaLightbox,
 * which has its own module.
 */

/**
 * Focuses the tile at a position in the gallery's items. The tiles are the root's children, in order.
 * @param {HTMLElement} root - The gallery's root element.
 * @param {number} index - The tile's position, from 0.
 */
export function focusItem(root, index) {
	const tile = root?.children[index];
	if (tile instanceof HTMLElement && tile.matches('button.moka-media-gallery-item')) {
		tile.focus();
	}
}
