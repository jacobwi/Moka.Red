let _listener = null;

/**
 * Parses a shortcut string like "Ctrl+Shift+D" into a descriptor object.
 * @param {string} shortcut
 * @returns {{ ctrl: boolean, shift: boolean, alt: boolean, meta: boolean, key: string }}
 */
function parseShortcut(shortcut) {
	const parts = shortcut.split('+').map(p => p.trim().toLowerCase());
	return {
		ctrl: parts.includes('ctrl'),
		shift: parts.includes('shift'),
		alt: parts.includes('alt'),
		meta: parts.includes('meta'),
		key: parts.filter(p => !['ctrl', 'shift', 'alt', 'meta'].includes(p))[0] || '',
	};
}

/**
 * Checks if a keyboard event matches the parsed shortcut.
 * @param {KeyboardEvent} e
 * @param {{ ctrl: boolean, shift: boolean, alt: boolean, meta: boolean, key: string }} parsed
 * @returns {boolean}
 */
function matchesShortcut(e, parsed) {
	return (
		e.ctrlKey === parsed.ctrl &&
		e.shiftKey === parsed.shift &&
		e.altKey === parsed.alt &&
		e.metaKey === parsed.meta &&
		e.key.toLowerCase() === parsed.key
	);
}

/**
 * Registers a keyboard shortcut that calls ToggleFromJs on the .NET reference.
 * @param {object} dotNetRef - DotNet object reference
 * @param {string} shortcut - Shortcut string, e.g. "Ctrl+Shift+D"
 */
export function registerKeyboardShortcut(dotNetRef, shortcut) {
	dispose();

	const parsed = parseShortcut(shortcut);

	_listener = (e) => {
		if (matchesShortcut(e, parsed)) {
			e.preventDefault();
			dotNetRef.invokeMethodAsync('ToggleFromJs');
		}
	};

	document.addEventListener('keydown', _listener);
}

/**
 * Opens the diagnostics page in a new browser window.
 */
export function openDiagnosticsPage() {
	window.open('/moka-diagnostics', 'moka-diagnostics', 'width=800,height=600');
}

/**
 * Downloads a base64-encoded JSON string as a file.
 * @param {string} jsonBase64 - Base64-encoded JSON content.
 * @param {string} filename - The download filename.
 */
export function downloadJson(jsonBase64, filename) {
	const json = atob(jsonBase64);
	const blob = new Blob([json], { type: 'application/json;charset=utf-8;' });
	const url = URL.createObjectURL(blob);
	const a = document.createElement('a');
	a.href = url;
	a.download = filename;
	document.body.appendChild(a);
	a.click();
	document.body.removeChild(a);
	URL.revokeObjectURL(url);
}

/**
 * Removes the keyboard shortcut listener.
 */
export function dispose() {
	if (_listener) {
		document.removeEventListener('keydown', _listener);
		_listener = null;
	}
}
