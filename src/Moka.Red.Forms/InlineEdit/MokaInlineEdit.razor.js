import { preventKeys } from '../../Moka.Red.Core/moka-keys.js';

const RULES = [{ selector: 'input', keys: ['Enter', 'Escape'] }];

// What had focus when each field opened: the app's own Rename button, say, for a field it opens itself.
const openers = new WeakMap();

/**
 * Focuses the field, selects the range .NET worked out, and cancels what Enter and Escape would
 * otherwise do, such as submitting a form around the field. Remembers what had focus before, for restoreFocus.
 * @param {HTMLElement} root - The component's root element.
 * @param {HTMLInputElement} input - The field.
 * @param {number} start - Where the selection starts.
 * @param {number} end - Where the selection ends.
 */
export function open(root, input, start, end) {
	if (!input) return;

	preventKeys(root, RULES);
	const opener = document.activeElement;
	if (opener instanceof HTMLElement && opener !== document.body && !root.contains(opener)) {
		openers.set(root, opener);
	} else {
		openers.delete(root);
	}

	input.focus();

	try {
		input.setSelectionRange(start, end);
	} catch {
		// Types such as email have no selection to set.
	}
}

/**
 * Gives focus back to what had it when the field opened, once Enter or Escape has closed a field the app opens itself.
 * Does nothing when that element has left the page or turned disabled.
 * @param {HTMLElement} root - The component's root element.
 */
export function restoreFocus(root) {
	const opener = openers.get(root);
	openers.delete(root);
	if (opener?.isConnected && !opener.matches(':disabled')) {
		opener.focus();
	}
}
