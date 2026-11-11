// MokaEmojiTypeahead. Watches a text field for a colon and a few letters and asks .NET to open MokaSlashMenu with the
// matches, searched here so typing sends one call per change of the query, not per key. A whole code such as :tada:
// turns into its emoji as the closing colon is typed, without a round trip. While the menu is open, the arrows, Enter,
// Tab and Escape go to the menu instead of the field.

import { load, search, withTone, drawableVersion } from '../../Moka.Red.Primitives/moka-emoji.js';

const watchers = new WeakMap();
const QUERY = /(^|[\s(\[{"'])(:([a-z0-9_+-]*))$/i;
const WHOLE = /(^|[\s(\[{"'])(:([a-z0-9_+-]+):)$/i;
const MENU_KEYS = ['ArrowDown', 'ArrowUp', 'Enter', 'Tab'];

/**
 * Starts watching, or takes new options.
 * @param {HTMLElement} root - The typeahead's wrapper.
 * @param {HTMLElement|null} target - The field, or an element holding it; null looks inside the wrapper.
 * @param {object} dotNetRef - Receives OnQuery(query, rows), OnClosed() and OnKey(key).
 * @param {object} options - dataSource, maxVersion, tone, keywords, max, minLength, convert, disabled.
 */
export function watch(root, target, dotNetRef, options) {
	if (!root) return;

	let state = watchers.get(root);
	if (!state) {
		state = { root, open: false, count: 0, closedAt: null, ticket: 0 };
		watchers.set(root, state);
	}

	state.dotNetRef = dotNetRef;
	state.options = options || {};

	const field = findField(target) ?? findField(root);
	if (field !== state.field) {
		state.unbind?.();
		state.field = field;
		state.unbind = field ? bind(state, field) : null;
	}

	if (state.options.disabled) close(state);
}

/** Stops watching. */
export function unwatch(root) {
	const state = root && watchers.get(root);
	if (!state) return;
	state.unbind?.();
	watchers.delete(root);
}

/** Replaces the colon and what follows it at the caret with the emoji and a space, as typing would. */
export function insert(root, glyph) {
	const state = root && watchers.get(root);
	const field = state?.field;
	if (!field || !glyph) return;

	const caret = field.selectionStart ?? field.value.length;
	const match = field.value.slice(0, caret).match(QUERY);
	const start = match ? caret - match[2].length : state.range?.start ?? caret;
	const end = match ? caret : state.range?.end ?? caret;
	state.open = false;
	state.count = 0;
	field.setRangeText(`${glyph} `, start, end, 'end');
	field.dispatchEvent(new Event('input', { bubbles: true }));
	field.focus({ preventScroll: true });
}

/** Names the menu's listbox and its lit option on the field, or takes them away. */
export function describe(root, listboxId, activeId) {
	const field = root && watchers.get(root)?.field;
	if (!field) return;
	set(field, 'aria-controls', listboxId);
	set(field, 'aria-activedescendant', activeId);
	set(field, 'aria-autocomplete', listboxId ? 'list' : null);
}

/** .NET closed the menu (Escape or a pick went through it), so the keys belong to the field again. */
export function closed(root) {
	const state = root && watchers.get(root);
	if (!state) return;
	if (state.open && state.range) state.closedAt = state.range.start;
	state.open = false;
	state.count = 0;
}

function set(field, name, value) {
	if (value) field.setAttribute(name, value);
	else field.removeAttribute(name);
}

function findField(element) {
	if (!element) return null;
	if (element.matches?.('textarea, input')) return element;
	return element.querySelector?.('textarea, input[type="text"], input[type="search"], input:not([type])') ?? null;
}

function bind(state, field) {
	const onInput = () => update(state, true);
	const onMove = () => update(state, false);
	const onKey = event => key(state, event);
	const onKeyUp = event => {
		if (['ArrowLeft', 'ArrowRight', 'Home', 'End'].includes(event.key)) update(state, false);
	};
	const onBlur = () => setTimeout(() => {
		if (document.activeElement !== field) close(state);
	}, 150);

	field.addEventListener('input', onInput);
	field.addEventListener('click', onMove);
	field.addEventListener('keyup', onKeyUp);
	field.addEventListener('keydown', onKey, true);
	field.addEventListener('blur', onBlur);

	return () => {
		field.removeEventListener('input', onInput);
		field.removeEventListener('click', onMove);
		field.removeEventListener('keyup', onKeyUp);
		field.removeEventListener('keydown', onKey, true);
		field.removeEventListener('blur', onBlur);
	};
}

function key(state, event) {
	if (!state.open || event.isComposing) return;

	if (event.key === 'Escape') {
		event.preventDefault();
		event.stopImmediatePropagation();
		state.closedAt = state.range?.start ?? null;
		close(state);
		return;
	}

	if (!state.count || !MENU_KEYS.includes(event.key) || event.ctrlKey || event.metaKey || event.altKey) return;
	if (event.key === 'Tab' && event.shiftKey) return;

	event.preventDefault();
	event.stopImmediatePropagation();
	state.dotNetRef?.invokeMethodAsync('OnKey', event.key).catch(() => { });
}

function close(state) {
	state.ticket++;
	if (!state.open) return;
	state.open = false;
	state.count = 0;
	state.dotNetRef?.invokeMethodAsync('OnClosed').catch(() => { });
}

async function data(state) {
	const loaded = await load(state.options.dataSource ?? null);
	const newest = typeof state.options.maxVersion === 'number' ? state.options.maxVersion : await drawableVersion(loaded);
	return { loaded, visible: entry => entry.version <= newest };
}

async function update(state, typed) {
	const { field, options } = state;
	if (!field || options.disabled) return;

	const ticket = ++state.ticket;
	const caret = field.selectionStart;
	if (caret === null || caret !== field.selectionEnd) {
		close(state);
		return;
	}

	const before = field.value.slice(0, caret);
	const whole = typed && options.convert !== false ? before.match(WHOLE) : null;
	const match = whole ? null : before.match(QUERY);
	if (!whole && (!match || match[3].length < (options.minLength ?? 2))) {
		if (!match) state.closedAt = null;
		close(state);
		return;
	}

	let found;
	try {
		found = await data(state);
	} catch {
		return;
	}

	if (ticket !== state.ticket || field.selectionStart !== caret || field.value.slice(0, caret) !== before) return;

	if (whole) {
		const entry = found.loaded.byCode.get(whole[3].toLowerCase());
		close(state);
		if (entry && found.visible(entry)) {
			state.closedAt = null;
			field.setRangeText(withTone(entry, options.tone ?? 0), caret - whole[2].length, caret, 'end');
			field.dispatchEvent(new Event('input', { bubbles: true }));
		}

		return;
	}

	const query = match[3];
	const start = caret - match[2].length;
	if (state.closedAt === start) return;

	const rows = search(found.loaded, query, { keywords: options.keywords !== false, visible: found.visible, limit: options.max ?? 8 });
	state.range = { start, end: caret };
	state.open = true;
	state.count = rows.length;
	state.dotNetRef?.invokeMethodAsync('OnQuery', query,
		rows.map(entry => [withTone(entry, options.tone ?? 0), entry.name, entry.codes[0] ?? '', entry.keywords.join(' ')]))
		.catch(() => { });
}
