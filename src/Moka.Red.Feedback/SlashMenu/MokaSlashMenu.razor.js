// MokaSlashMenu. The host editor keeps focus, so nothing here takes it.

export { trackHover } from '../../Moka.Red.Core/moka-hover.js';

/**
 * Scrolls the list, and only the list, so the row at index shows, with its group's heading when it is the
 * first row of a group. Element.scrollIntoView would also scroll the page under a menu that sits partly
 * out of the window.
 * @param {HTMLElement} list - The menu's listbox.
 * @param {number} index - The row's data-moka-index.
 */
export function reveal(list, index) {
	if (!list) return;

	if (index <= 0) {
		list.scrollTop = 0;
		return;
	}

	const row = list.querySelector(`[data-moka-index="${index}"]`);
	if (!row) return;

	const label = row.previousElementSibling;
	const first = label && label.classList.contains('moka-slash-menu-group-label') ? label : row;

	const style = getComputedStyle(list);
	const box = list.getBoundingClientRect();
	const viewTop = box.top + list.clientTop + (parseFloat(style.paddingTop) || 0);
	const viewBottom = box.top + list.clientTop + list.clientHeight - (parseFloat(style.paddingBottom) || 0);

	const top = first.getBoundingClientRect().top;
	const bottom = row.getBoundingClientRect().bottom;

	if (top < viewTop) {
		list.scrollTop -= viewTop - top;
	} else if (bottom > viewBottom) {
		list.scrollTop += bottom - viewBottom;
	}
}
