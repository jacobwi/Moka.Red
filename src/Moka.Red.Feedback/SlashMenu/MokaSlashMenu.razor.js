// MokaSlashMenu. The host editor keeps focus, so nothing here takes it.

import { revealRow } from '../../Moka.Red.Core/moka-hover.js';

export { trackHover } from '../../Moka.Red.Core/moka-hover.js';

/**
 * Scrolls the list, and only the list, so the row at index shows, with its group's heading when it is the
 * first row of a group.
 * @param {HTMLElement} list - The menu's listbox.
 * @param {number} index - The row's data-moka-index.
 */
export function reveal(list, index) {
	revealRow(list, index, 'moka-slash-menu-group-label');
}
