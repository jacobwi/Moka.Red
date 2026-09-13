/**
 * Reads scroll position and size of the sentinel's container.
 * @param {HTMLElement} element - The scroll container.
 * @returns {number[]} [scrollTop, scrollHeight, clientHeight]
 */
export function getScrollMetrics(element) {
	if (!element) return [0, 0, 0];
	return [element.scrollTop, element.scrollHeight, element.clientHeight];
}
