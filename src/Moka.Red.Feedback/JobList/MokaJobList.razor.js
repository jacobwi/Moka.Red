/**
 * Puts focus back in the list after an action took away the button that had it. Focus somewhere else
 * means the person has moved on, so it stays there.
 * @param {HTMLElement} list - The component's root element, focused when no rows are left.
 * @param {number} index - The row that now sits where the button was.
 */
export function restoreFocus(list, index) {
	if (!list || (document.activeElement && document.activeElement !== document.body)) return;

	const rows = list.querySelectorAll('.moka-job-list__item');
	(rows[Math.min(index, rows.length - 1)] ?? list).focus();
}
