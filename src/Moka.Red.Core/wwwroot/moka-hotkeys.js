/**
 * Shortcut parsing, matching and one document listener, shared by everything in the library that binds a key.
 *
 * Matching happens here rather than in .NET: on Blazor Server a keydown that round-tripped before it could be
 * rejected would send every keystroke in the app over the circuit. .NET hears only the presses that match.
 */

import { isApplePlatform, isEditable, modalIsOpen } from './moka-keys.js';

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
		ctrl: mods.has('ctrl') || mods.has('control') || (mod && !isApplePlatform),
		meta: mods.has('meta') || mods.has('cmd') || mods.has('command') || (mod && isApplePlatform),
		alt: mods.has('alt') || mods.has('option'),
		shift: mods.has('shift')
	};

	// Without Ctrl, Cmd or Alt a one-character key is typing ("/", "Shift+K").
	spec.types = !spec.ctrl && !spec.meta && !spec.alt && spec.key.length === 1;
	// A punctuation mark or symbol is named by the character it types, and which keys type it depends on the
	// layout: "?" is Shift+/ in the US and Shift+, in France, "/" is Shift+7 in Germany.
	spec.symbol = /^[\p{P}\p{S}]$/u.test(spec.key);
	return spec;
}

export { isEditable };

/**
 * Whether a press is the shortcut. Every modifier has to match exactly: Ctrl+Shift+K is not Ctrl+K. Letters and
 * digits are also compared by code, because Alt and Shift change what e.key reports for them. A punctuation mark or
 * symbol is compared by the character typed, and Shift counts only when the shortcut names it, since the layout
 * decides whether typing the character takes Shift.
 * @param {KeyboardEvent} e - The press.
 * @param {object} spec - A parsed shortcut.
 * @returns {boolean}
 */
export function matches(e, spec) {
	const shiftMatches = e.shiftKey === spec.shift || (spec.symbol && !spec.shift);
	if (e.ctrlKey !== spec.ctrl || e.metaKey !== spec.meta || e.altKey !== spec.alt || !shiftMatches) {
		return false;
	}

	const key = typeof e.key === 'string' ? e.key.toLowerCase() : '';
	if (spec.symbol) return key === spec.key;

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
		for (const entry of binding.specs) {
			if (!matches(e, entry.spec)) continue;
			if (!firesIn(e.target, entry)) continue;
			// A modal is drawn inside the page, so its keys reach this listener too. The page behind it is out of use.
			if (!entry.allowInModal && modalIsOpen()) continue;

			// A held key repeats, and the browser's own action would take every repeat the shortcut doesn't cancel.
			if (entry.preventDefault) {
				e.preventDefault();
			}

			// A shortcut fires once per press, however long the key is held.
			if (!e.repeat) {
				dotNetRef.invokeMethodAsync('InvokeFromJs', entry.id);
			}

			return;
		}
	};

	_bindings.set(handle, binding);
	document.addEventListener('keydown', binding.listener);
	return handle;
}

// Whether a hotkey fires with focus on this element: everywhere outside a field, and in one as its InFields says.
function firesIn(target, entry) {
	if (entry.inFields === 'always' || !isEditable(target)) return true;
	return entry.inFields !== 'never' && !entry.spec.types;
}

/**
 * Replaces the set a handle listens for. Called whenever the registry changes.
 * @param {number} handle - From bindHotkeys.
 * @param {Array<{id:string, keys:string, inFields:string, allowInModal:boolean, preventDefault:boolean}>} hotkeys - The set.
 *     inFields is 'unless-typing', 'never' or 'always'.
 */
export function setHotkeys(handle, hotkeys) {
	const binding = _bindings.get(handle);
	if (!binding) return;

	binding.specs = (hotkeys || [])
		.map(h => ({
			id: h.id,
			inFields: h.inFields || 'unless-typing',
			allowInModal: !!h.allowInModal,
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
