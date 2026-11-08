import { preventKeys } from '../../Moka.Red.Core/moka-keys.js';

const RULES = [{ selector: 'input', keys: ['Enter', 'Escape'] }];

/**
 * Focuses the field, selects the range .NET worked out, and cancels what Enter and Escape would
 * otherwise do, such as submitting a form around the field.
 * @param {HTMLElement} root - The component's root element.
 * @param {HTMLInputElement} input - The field.
 * @param {number} start - Where the selection starts.
 * @param {number} end - Where the selection ends.
 */
export function open(root, input, start, end) {
	if (!input) return;

	preventKeys(root, RULES);
	input.focus();

	try {
		input.setSelectionRange(start, end);
	} catch {
		// Types such as email have no selection to set.
	}
}
