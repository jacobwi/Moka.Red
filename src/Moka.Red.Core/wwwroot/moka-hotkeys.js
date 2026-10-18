/**
 * Shortcut parsing, matching and one document listener, shared by everything in the library that binds a key.
 *
 * Matching happens here rather than in .NET: on Blazor Server a keydown that round-tripped before it could be
 * rejected would send every keystroke in the app over the circuit. .NET hears only the presses that match.
 */

const _isMac = /mac|iphone|ipad|ipod/i.test(navigator.userAgentData?.platform ?? navigator.platform ?? '');

const MODIFIERS = new Set(['mod', 'ctrl', 'control', 'meta', 'cmd', 'command', 'alt', 'option', 'shift']);

/** Live registrations, keyed by the handle bindHotkeys returned. */
const _bindings = new Map();
let _nextHandle = 0;

/**
 * Reads "Mod+K", "Ctrl+Shift+P" or "Alt+Space": modifiers, then one key. Mod is Ctrl, or Cmd on a Mac.
 * Null or empty turns the shortcut off, and so does a modifier this does not know: ignoring it would turn
 * "Win+K" into a bare K.
 * @param {string} text - The shortcut.
 * @param {string} [source] - Name used in the console warning about an unknown modifier.
 * @returns {object|null} The parsed shortcut, or null when there is none to bind.
 */
export function parseShortcut(text, source) {
	if (typeof text !== 'string') return null;

	const parts = text.split('+').map(p => p.trim().toLowerCase()).filter(Boolean);
	if (parts.length === 0) return null;

	const key = parts.pop();
	const unknown = parts.find(p => !MODIFIERS.has(p));
	if (unknown !== undefined) {
		console.warn(`[${source || 'MokaHotkeys'}] Unknown modifier "${unknown}" in shortcut "${text}"; the shortcut is off.`);
		return null;
	}

	const mods = new Set(parts);
	const mod = mods.has('mod');
	const spec = {
		key: key === 'space' ? ' ' : key,
		ctrl: mods.has('ctrl') || mods.has('control') || (mod && !_isMac),
		meta: mods.has('meta') || mods.has('cmd') || mods.has('command') || (mod && _isMac),
		alt: mods.has('alt') || mods.has('option'),
		shift: mods.has('shift')
	};

	// Without Ctrl, Cmd or Alt a one-character key is typing ("/", "Shift+K").
	spec.types = !spec.ctrl && !spec.meta && !spec.alt && spec.key.length === 1;
	return spec;
}

/**
 * Whether a key press landed in something that takes typing. A shortcut that types a character is left to it.
 * @param {EventTarget} target - The press's target.
 * @returns {boolean}
 */
export function isEditable(target) {
	if (!(target instanceof Element)) return false;
	if (target.isContentEditable) return true;

	const tag = target.tagName;
	if (tag === 'TEXTAREA' || tag === 'SELECT') return true;
	return tag === 'INPUT'
		&& !/^(button|checkbox|radio|range|color|file|image|reset|submit)$/i.test(target.type);
}

/**
 * Whether a press is the shortcut. Every modifier has to match exactly: Ctrl+Shift+K is not Ctrl+K. Letters and
 * digits are also compared by code, because Alt and Shift change what e.key reports for them.
 * @param {KeyboardEvent} e - The press.
 * @param {object} spec - A parsed shortcut.
 * @returns {boolean}
 */
export function matches(e, spec) {
	if (e.ctrlKey !== spec.ctrl || e.metaKey !== spec.meta || e.altKey !== spec.alt || e.shiftKey !== spec.shift) {
		return false;
	}

	const key = typeof e.key === 'string' ? e.key.toLowerCase() : '';
	const code = typeof e.code === 'string' ? e.code.toLowerCase() : '';
	return key === spec.key || code === 'key' + spec.key || code === 'digit' + spec.key;
}

/**
 * Starts listening for the registered hotkeys. One listener for the whole set, so a hundred hotkeys cost one
 * handler rather than a hundred.
 * @param {object} dotNetRef - Reference used to call InvokeFromJs with the id of whichever hotkey matched.
 * @returns {number} A handle for setHotkeys and unbindHotkeys.
 */
export function bindHotkeys(dotNetRef) {
	const handle = ++_nextHandle;
	const binding = { specs: [] };

	binding.listener = (e) => {
		// A held key repeats; a shortcut fires once per press, as the palette's own already did.
		if (e.repeat) return;

		for (const entry of binding.specs) {
			if (!matches(e, entry.spec)) continue;
			if (entry.spec.types && !entry.allowInInput && isEditable(e.target)) continue;

			if (entry.preventDefault) {
				e.preventDefault();
			}

			dotNetRef.invokeMethodAsync('InvokeFromJs', entry.id);
			return;
		}
	};

	_bindings.set(handle, binding);
	document.addEventListener('keydown', binding.listener);
	return handle;
}

/**
 * Replaces the set a handle listens for. Called whenever the registry changes.
 * @param {number} handle - From bindHotkeys.
 * @param {Array<{id:string, keys:string, allowInInput:boolean, preventDefault:boolean}>} hotkeys - The set.
 */
export function setHotkeys(handle, hotkeys) {
	const binding = _bindings.get(handle);
	if (!binding) return;

	binding.specs = (hotkeys || [])
		.map(h => ({
			id: h.id,
			allowInInput: !!h.allowInInput,
			preventDefault: h.preventDefault !== false,
			spec: parseShortcut(h.keys, 'MokaHotkeys')
		}))
		.filter(entry => entry.spec !== null);
}

/**
 * Stops listening and forgets the handle.
 * @param {number} handle - From bindHotkeys.
 */
export function unbindHotkeys(handle) {
	const binding = _bindings.get(handle);
	if (!binding) return;

	document.removeEventListener('keydown', binding.listener);
	_bindings.delete(handle);
}
