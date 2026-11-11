// Enter in a hex field takes the color it holds. A .NET handler can't cancel one key without cancelling typing, so
// the browser's own Enter, which would submit a form around the swatch, is cancelled here.
const bound = new WeakSet();

export function bindHexField(input) {
	if (!input || bound.has(input)) return;
	bound.add(input);
	input.addEventListener('keydown', e => {
		if (e.key === 'Enter' && !e.isComposing) e.preventDefault();
	});
}

// The panel opens on the hex of the current color, selected so typing replaces it.
export function focusHexField(input) {
	if (!input) return;
	bindHexField(input);
	input.focus({ preventScroll: true });
	input.select();
}
