/**
 * Notification panel JS module, shared by MokaNotificationBell and MokaNotificationCenter.
 *
 * The panel opens below its bell and covers what is under it, so it closes once focus leaves the
 * component: Tab past its last control, or a click anywhere else. .NET cannot tell where focus went,
 * since Blazor's focus events carry no relatedTarget. Closing clicks the bell, the same path as the
 * mouse.
 */

const boundRoots = new WeakSet();

/**
 * Closes the panel when focus leaves the component. Safe to call again.
 * @param {HTMLElement} root - The component's root element.
 * @param {HTMLElement} trigger - The bell button, which carries aria-expanded.
 */
export function bindPopup(root, trigger) {
	if (!root || !trigger || boundRoots.has(root)) return;
	boundRoots.add(root);

	const close = () => {
		if (trigger.getAttribute('aria-expanded') === 'true') trigger.click();
	};

	root.addEventListener('focusout', e => {
		if (e.relatedTarget instanceof Node) {
			if (!root.contains(e.relatedTarget)) close();
			return;
		}

		// No relatedTarget: a click on something that takes no focus, the window losing focus, or
		// the focused element being removed, like a dismissed notification's button. Only the
		// first closes the panel; after a removal .NET puts focus somewhere itself. By the next
		// task focus has settled.
		const lost = e.target;
		setTimeout(() => {
			if (!(lost instanceof Node) || !lost.isConnected) return;
			if (document.hasFocus() && !root.contains(document.activeElement)) close();
		});
	});
}

/**
 * Puts focus on an element, for a panel that closed with focus inside it.
 * @param {HTMLElement} element - The element to focus.
 */
export function focusElement(element) {
	element?.focus();
}

/**
 * Focuses the element at an index among a container's matches, or the last one when the index is
 * past the end, or the fallback when there are none. For a list whose focused entry was removed.
 * @param {HTMLElement} container - Where to look.
 * @param {string} selector - Which elements count.
 * @param {number} index - The index the removed element had.
 * @param {HTMLElement} fallback - Focused when nothing matches.
 */
export function focusNth(container, selector, index, fallback) {
	const matches = container ? container.querySelectorAll(selector) : [];
	const target = matches.length > 0 ? matches[Math.min(index, matches.length - 1)] : fallback;
	target?.focus();
}
