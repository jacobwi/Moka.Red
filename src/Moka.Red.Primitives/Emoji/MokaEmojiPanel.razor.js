// MokaEmojiPanel. The grid lives here, not in Blazor: about 1,900 emoji as components would make every render of the
// panel a long one (blazor/performance/rendering.md, "Avoid thousands of component instances"), and search, hover and
// the arrow keys would each be a round trip (blazor/performance/javascript-interoperability.md, "Avoid excessively
// fine-grained calls"). Blazor draws the frame (search box, tabs, tone button, preview strip) and leaves the scroll
// area empty; this fills it, keeps the highlight and the preview, and calls .NET once per pick.

import { load, search, withTone, drawableVersion, find, bare } from '../moka-emoji.js';

const COMMON = [
	'\u{1F600}', '\u{1F602}', '\u{1F642}', '\u{1F60D}', '\u{1F914}', '\u{1F60E}', '\u{1F605}', '\u{1F62D}',
	'\u{1F44D}', '\u{1F44F}', '\u{1F64F}', '\u{1F4AA}', '\u{1F440}', '\u{1F389}', '\u{1F525}', '\u2764\uFE0F',
	'\u2705', '\u274C', '\u{1F680}', '\u{1F4AF}', '\u{1F44B}', '\u{1F64C}', '\u{1F62E}', '\u{1F634}'
];

const panels = new WeakMap();
const bound = new WeakSet();

/**
 * Binds the panel, or takes new options. Called after every render that changed what the grid shows; the grid
 * redraws only when something it draws changed.
 * @param {HTMLElement} root - The panel's root.
 * @param {object} dotNetRef - Receives OnEmojiPicked(emoji, base, name, shortcode).
 * @param {object} options - What the grid shows, from MokaEmojiPanel.
 */
export function update(root, dotNetRef, options) {
	if (!root) return;

	let state = panels.get(root);
	if (!state) {
		state = { root, q: '', lit: -1, flat: [], rows: [], cols: 0, cellWidth: 0, forced: null, ticket: 0, rendered: '', status: '' };
		panels.set(root, state);
	}

	state.dotNetRef = dotNetRef;
	state.options = options || {};
	attach(state);
	refresh(state);
}

/** Moves focus into the panel: the search box, else the grid, else the first quick pick. */
export function focusStart(root) {
	if (!root) return;
	const target = root.querySelector('.moka-emoji-panel__query') ??
		root.querySelector('.moka-emoji-panel__scroll') ??
		root.querySelector('.moka-emoji-panel__quick, .moka-emoji-panel__more');
	target?.focus({ preventScroll: true });
}

/** Focuses the chosen tone in the tone list and lets the arrows walk it. */
export function focusTone(list) {
	if (!list) return;

	if (!bound.has(list)) {
		bound.add(list);
		list.addEventListener('keydown', event => {
			const buttons = [...list.querySelectorAll('button')];
			const at = buttons.indexOf(document.activeElement);
			let next = -1;
			if (event.key === 'ArrowRight' || event.key === 'ArrowDown') next = (at + 1) % buttons.length;
			else if (event.key === 'ArrowLeft' || event.key === 'ArrowUp') next = (at - 1 + buttons.length) % buttons.length;
			else if (event.key === 'Home') next = 0;
			else if (event.key === 'End') next = buttons.length - 1;
			if (next < 0) return;
			event.preventDefault();
			buttons[next].focus({ preventScroll: true });
		});
	}

	(list.querySelector('[aria-pressed="true"]') ?? list.querySelector('button'))?.focus({ preventScroll: true });
}

/** Lets go of a panel that is closing or going away. */
export function forget(root) {
	const state = root && panels.get(root);
	if (!state) return;
	state.observer?.disconnect();
	state.ticket++;
	panels.delete(root);
}

function attach(state) {
	const { root } = state;

	const grid = root.querySelector('.moka-emoji-panel__scroll');
	if (grid !== state.grid) {
		state.observer?.disconnect();
		state.observer = null;
		state.grid = grid;
		state.rendered = '';
		state.lit = -1;
		state.cols = 0;
		if (grid) {
			bindGrid(state, grid);
			state.observer = new ResizeObserver(() => resized(state));
			state.observer.observe(grid);
		}
	}

	const query = root.querySelector('.moka-emoji-panel__query');
	if (query !== state.query) {
		state.query = query;
		state.q = query ? query.value : '';
		if (query) bindQuery(state, query);
	}

	state.preview = root.querySelector('.moka-emoji-panel__preview');
	state.statusElement = root.querySelector('.moka-emoji-panel__status');
	state.tabs = [...root.querySelectorAll('.moka-emoji-panel__tab')];
	state.tabs.forEach((tab, index) => {
		if (!bound.has(tab)) {
			bound.add(tab);
			bindTab(state, tab);
		}

		if (!tab.hasAttribute('aria-current')) tab.setAttribute('aria-current', 'false');
		if (index === 0 && !state.tabs.some(t => t.tabIndex === 0)) tab.tabIndex = 0;
	});

	for (const quick of root.querySelectorAll('.moka-emoji-panel__quick')) {
		if (bound.has(quick)) continue;
		bound.add(quick);
		quick.addEventListener('click', () => pickGlyph(state, quick.dataset.emoji));
	}
}

async function refresh(state) {
	const ticket = ++state.ticket;
	const { options } = state;
	if (!state.grid) return;

	const source = options.dataSource ?? null;
	if (!state.data || state.source !== source) {
		if (!state.rendered) note(state, options.words?.loading, 'loading');
		try {
			state.data = await load(source);
			state.source = source;
		} catch {
			if (ticket === state.ticket) note(state, options.words?.failed, 'failed');
			return;
		}
	}

	const newest = typeof options.maxVersion === 'number' ? options.maxVersion : await drawableVersion(state.data);
	if (ticket !== state.ticket || !state.grid?.isConnected) return;
	state.newest = newest;
	if (!state.cols) state.cols = columns(state);
	render(state, state.flat[state.lit]);
}

function note(state, text, kind) {
	if (!state.grid) return;
	state.grid.replaceChildren(element('p', `moka-emoji-panel__note moka-emoji-panel__note--${kind}`, text ?? ''));
	state.flat = [];
	state.rows = [];
	state.rendered = '';
}

// Built from elements, never from markup text, so a page that enforces Trusted Types accepts it.
function element(tag, className, text, attributes) {
	const node = document.createElement(tag);
	if (className) node.className = className;
	if (text !== undefined && text !== null) node.textContent = text;
	for (const [name, value] of Object.entries(attributes ?? {})) node.setAttribute(name, value);
	return node;
}

function isList(state) {
	return state.options.shape === 'list';
}

function visible(state) {
	return entry => entry.version <= state.newest;
}

function sections(state) {
	const { data, options } = state;
	const words = options.words || {};
	const shown = visible(state);
	const query = state.q.trim();

	if (query) {
		const found = search(data, query, { keywords: options.keywords !== false, visible: shown });
		return [{ id: 'results', name: count(words, found.length), items: found }];
	}

	const out = [];
	const searchOnly = options.navigation === 'search-only';
	const frequent = (options.frequent || []).map(glyph => find(data, glyph)).filter(entry => entry && shown(entry));
	if (frequent.length) {
		out.push({ id: 'frequent', name: words.frequent ?? '', items: frequent });
	} else if (searchOnly) {
		const common = (options.common?.length ? options.common : COMMON).map(glyph => find(data, glyph)).filter(entry => entry && shown(entry));
		out.push({ id: 'frequent', name: words.common ?? words.frequent ?? '', items: common });
	}

	if (!searchOnly) {
		data.groups.forEach((group, index) => {
			out.push({
				id: group.id,
				name: options.groups?.[index] ?? group.name,
				items: data.emoji.filter(entry => entry.group === index && shown(entry))
			});
		});
	}

	return out;
}

function count(words, n) {
	const text = n === 1 ? words.oneMatch ?? '1 match' : words.matches ?? '{count} matches';
	return text.replace('{count}', String(n));
}

function renderKey(state) {
	const { options } = state;
	return JSON.stringify([
		state.q.trim(), state.cols, state.newest, options.shape, options.navigation, options.tone ?? 0, options.keywords,
		options.frequent, options.common, options.selected ?? '', options.groups, options.words, state.source
	]);
}

function render(state, keep) {
	const { grid, options } = state;
	if (!grid || !state.data) return;

	const key = renderKey(state);
	if (key === state.rendered) return;
	state.rendered = key;

	const list = isList(state);
	const words = options.words || {};
	const tone = options.tone ?? 0;
	const selected = options.selected ? bare(options.selected) : null;
	const id = grid.id || 'moka-emoji';
	const perRow = list ? 1 : Math.max(1, state.cols || 8);
	const keepTop = grid.scrollTop;
	const query = state.q.trim();

	state.flat = [];
	state.rows = [];
	const content = document.createDocumentFragment();
	for (const section of sections(state)) {
		if (!section.items.length) {
			if (query) {
				const empty = element('div', 'moka-emoji-panel__empty');
				empty.append(
					element('b', null, (words.noMatch ?? 'No emoji match "{query}"').replace('{query}', query)),
					element('span', null, words.noMatchHint ?? ''));
				content.append(empty);
			}
			continue;
		}

		const group = element('div', 'moka-emoji-panel__group', null,
			{ 'role': list ? 'group' : 'rowgroup', 'data-group': section.id, 'aria-label': section.name });
		group.append(element('div', 'moka-emoji-panel__head', section.name, { 'aria-hidden': 'true' }));
		for (let at = 0; at < section.items.length; at += perRow) {
			const row = [];
			const line = list ? group : group.appendChild(element('div', 'moka-emoji-panel__row', null, { 'role': 'row' }));
			section.items.slice(at, at + perRow).forEach((entry, column) => {
				const k = state.flat.length;
				const glyph = withTone(entry, tone);
				const chosen = selected !== null && bare(entry.glyph) === selected;
				state.flat.push({ entry, glyph, row: state.rows.length, column, group: section.id });
				row.push(k);
				if (list) {
					const option = element('div', `moka-emoji-panel__option${chosen ? ' is-chosen' : ''}`, null,
						{ 'role': 'option', 'id': `${id}-c${k}`, 'data-k': String(k), 'aria-selected': 'false' });
					option.append(
						element('span', 'moka-emoji-panel__option-glyph', glyph, { 'aria-hidden': 'true' }),
						element('span', 'moka-emoji-panel__option-name', entry.name),
						element('code', 'moka-emoji-panel__option-code', `:${entry.codes[0] ?? ''}:`));
					line.append(option);
				} else {
					const cell = element('div', `moka-emoji-panel__cell${chosen ? ' is-chosen' : ''}`, glyph,
						{ 'role': 'gridcell', 'id': `${id}-c${k}`, 'data-k': String(k), 'aria-label': entry.name });
					if (chosen) cell.setAttribute('aria-selected', 'true');
					line.append(cell);
				}
			});
			state.rows.push(row);
		}

		content.append(group);
	}

	if (options.navigation === 'search-only' && !query && words.searchOnly) {
		content.append(element('p', 'moka-emoji-panel__note',
			words.searchOnly.replace('{count}', String(state.data.emoji.filter(visible(state)).length))));
	}

	grid.replaceChildren(content);
	grid.style.setProperty('--_cols', String(perRow));

	if (keep) {
		grid.scrollTop = keepTop;
		const at = state.flat.findIndex(item => item.entry === keep.entry && item.group === keep.group);
		setLit(state, at >= 0 ? at : Math.min(state.lit, state.flat.length - 1), false);
	} else {
		grid.scrollTop = 0;
		setLit(state, state.flat.length ? 0 : -1, false);
	}

	if (!state.cellWidth && !list) {
		const cell = grid.querySelector('.moka-emoji-panel__cell');
		if (cell) {
			state.cellWidth = cell.getBoundingClientRect().width;
			// The first count was made from an estimate; a real cell can change it, and too many columns spill out.
			const cols = columns(state);
			if (cols !== state.cols) {
				state.cols = cols;
				render(state, state.flat[state.lit]);
				return;
			}
		}
	}

	announce(state, query ? (state.flat.length ? count(words, state.flat.length) : (words.noMatch ?? '').replace('{query}', query)) : '');
	spy(state);
}

function announce(state, text) {
	if (!state.statusElement || state.status === text) return;
	state.status = text;
	state.statusElement.textContent = text;
}

function columns(state) {
	const { grid, options } = state;
	if (isList(state)) return 1;
	if (options.columns > 0) return options.columns;
	if (!grid) return 8;
	const style = getComputedStyle(grid);
	const width = grid.clientWidth - (parseFloat(style.paddingLeft) || 0) - (parseFloat(style.paddingRight) || 0);
	// The cell is 2.125rem in the stylesheet, so the estimate goes by the page's root font size, not the panel's.
	const cell = state.cellWidth || parseFloat(getComputedStyle(document.documentElement).fontSize) * 2.125 || 34;
	return width > 0 ? Math.max(5, Math.floor(width / cell)) : 8;
}

function resized(state) {
	if (!state.grid?.isConnected) {
		state.observer?.disconnect();
		return;
	}

	const cols = columns(state);
	if (cols !== state.cols) {
		state.cols = cols;
		if (state.data) render(state, state.flat[state.lit]);
	} else {
		spy(state);
	}
}

function reveal(state, cell) {
	const { grid } = state;
	const head = cell.closest('.moka-emoji-panel__group')?.querySelector('.moka-emoji-panel__head');
	const pad = head ? head.offsetHeight : 0;
	const top = cell.offsetTop;
	const bottom = top + cell.offsetHeight;
	if (top - pad < grid.scrollTop) grid.scrollTop = Math.max(0, top - pad - 2);
	else if (bottom > grid.scrollTop + grid.clientHeight) grid.scrollTop = bottom - grid.clientHeight + 4;
}

function setLit(state, k, scroll) {
	const { grid, query } = state;
	const list = isList(state);
	const old = grid.querySelector('.is-lit');
	if (old) {
		old.classList.remove('is-lit');
		if (list) old.setAttribute('aria-selected', 'false');
	}

	state.lit = k;
	const cell = k >= 0 ? grid.querySelector(`[data-k="${k}"]`) : null;
	if (cell) {
		cell.classList.add('is-lit');
		if (list) cell.setAttribute('aria-selected', 'true');
		grid.setAttribute('aria-activedescendant', cell.id);
		query?.setAttribute('aria-activedescendant', cell.id);
		if (scroll) reveal(state, cell);
	} else {
		grid.removeAttribute('aria-activedescendant');
		query?.removeAttribute('aria-activedescendant');
	}

	preview(state);
}

function preview(state) {
	const panel = state.preview;
	if (!panel) return;
	const item = state.flat[state.lit];
	const glyph = panel.querySelector('.moka-emoji-panel__preview-glyph');
	const name = panel.querySelector('.moka-emoji-panel__preview-name');
	const code = panel.querySelector('.moka-emoji-panel__preview-code');
	if (glyph) glyph.textContent = item ? item.glyph : '';
	if (name) name.textContent = item ? item.entry.name : (state.options.words?.nothing ?? '');
	if (code) code.textContent = item && item.entry.codes[0] ? `:${item.entry.codes[0]}:` : '';
}

function spy(state) {
	const { grid, tabs } = state;
	if (!tabs?.length || !grid?.isConnected) return;

	let current = null;
	if (!state.q.trim()) {
		const atEnd = grid.scrollTop + grid.clientHeight >= grid.scrollHeight - 2;
		if (atEnd && state.forced) {
			current = state.forced;
		} else {
			for (const group of grid.querySelectorAll('.moka-emoji-panel__group')) {
				if (group.offsetTop <= grid.scrollTop + 2) current = group.dataset.group;
				else break;
			}
		}
	}

	for (const tab of tabs) tab.setAttribute('aria-current', String(tab.dataset.go === current));
}

function rowStep(state, k, delta) {
	const item = state.flat[k];
	const row = item.row + delta;
	if (row < 0 || row >= state.rows.length) return k;
	const cells = state.rows[row];
	return cells[Math.min(item.column, cells.length - 1)];
}

function groupStep(state, k, delta) {
	const groups = [...new Set(state.flat.map(item => item.group))];
	const at = groups.indexOf(state.flat[k].group);
	const first = group => state.flat.findIndex(item => item.group === group);
	if (delta < 0 && first(groups[at]) !== k) return first(groups[at]);
	return first(groups[Math.max(0, Math.min(groups.length - 1, at + delta))]);
}

function inGrid(state, on) {
	state.inGrid = on;
	state.grid?.toggleAttribute('data-moka-keys', on);
}

function keys(state, event, fromSearch) {
	if (event.isComposing) return;

	if (event.key === 'Escape') {
		if (fromSearch && state.q) {
			event.preventDefault();
			event.stopPropagation();
			state.query.value = '';
			state.q = '';
			inGrid(state, false);
			render(state, null);
		}
		return;
	}

	if (event.ctrlKey || event.metaKey || event.altKey) return;

	const n = state.flat.length;
	if (!n) return;

	const list = isList(state);
	const k = state.lit < 0 ? 0 : state.lit;
	const across = !list && (!fromSearch || state.inGrid);
	let next = null;
	switch (event.key) {
		case 'ArrowRight':
			if (!across) return;
			next = Math.min(n - 1, k + 1);
			break;
		case 'ArrowLeft':
			if (!across) return;
			next = Math.max(0, k - 1);
			break;
		case 'ArrowDown':
			if (fromSearch && !list && !state.inGrid) {
				inGrid(state, true);
				next = k;
			} else {
				next = rowStep(state, k, 1);
			}
			break;
		case 'ArrowUp':
			if (fromSearch && !list && !state.inGrid) return;
			if (fromSearch && !list && state.flat[k].row === 0) {
				inGrid(state, false);
				next = k;
			} else {
				next = rowStep(state, k, -1);
			}
			break;
		case 'Home':
			if (fromSearch) return;
			next = 0;
			break;
		case 'End':
			if (fromSearch) return;
			next = n - 1;
			break;
		case 'PageDown':
			next = groupStep(state, k, 1);
			break;
		case 'PageUp':
			next = groupStep(state, k, -1);
			break;
		case 'Enter':
			event.preventDefault();
			pick(state, k);
			return;
		case ' ':
			if (fromSearch) return;
			event.preventDefault();
			pick(state, k);
			return;
		default:
			if (fromSearch && event.key.length === 1) inGrid(state, false);
			return;
	}

	event.preventDefault();
	setLit(state, next, true);
}

function bindGrid(state, grid) {
	grid.addEventListener('keydown', event => keys(state, event, false));
	grid.addEventListener('pointermove', event => {
		const cell = event.target.closest?.('[data-k]');
		if (cell && Number(cell.dataset.k) !== state.lit) setLit(state, Number(cell.dataset.k), false);
	}, { passive: true });
	grid.addEventListener('click', event => {
		const cell = event.target.closest?.('[data-k]');
		if (cell) pick(state, Number(cell.dataset.k));
	});
	grid.addEventListener('focus', () => {
		if (state.lit < 0 && state.flat.length) setLit(state, 0, false);
	});
	grid.addEventListener('scroll', () => {
		if (state.forced && grid.scrollTop + grid.clientHeight < grid.scrollHeight - 2) state.forced = null;
		spy(state);
	}, { passive: true });
}

function bindQuery(state, query) {
	query.addEventListener('input', () => {
		state.q = query.value;
		inGrid(state, false);
		render(state, null);
	});
	query.addEventListener('keydown', event => keys(state, event, true));
	query.addEventListener('blur', () => inGrid(state, false));
}

function bindTab(state, tab) {
	tab.addEventListener('click', () => {
		if (state.q && state.query) {
			state.query.value = '';
			state.q = '';
			render(state, null);
		}

		const group = state.grid?.querySelector(`.moka-emoji-panel__group[data-group="${CSS.escape(tab.dataset.go)}"]`);
		if (!group) return;
		state.forced = tab.dataset.go;
		state.grid.scrollTop = group.offsetTop;
		setLit(state, state.flat.findIndex(item => item.group === tab.dataset.go), false);
		spy(state);
		for (const other of state.tabs) other.tabIndex = -1;
		tab.tabIndex = 0;
	});

	tab.addEventListener('keydown', event => {
		const tabs = state.tabs;
		const at = tabs.indexOf(tab);
		const vertical = tab.closest('[aria-orientation="vertical"]') !== null;
		const forward = vertical ? 'ArrowDown' : 'ArrowRight';
		const back = vertical ? 'ArrowUp' : 'ArrowLeft';
		let next = null;
		if (event.key === forward) next = (at + 1) % tabs.length;
		else if (event.key === back) next = (at - 1 + tabs.length) % tabs.length;
		else if (event.key === 'Home') next = 0;
		else if (event.key === 'End') next = tabs.length - 1;
		if (next === null) return;
		event.preventDefault();
		for (const other of tabs) other.tabIndex = -1;
		tabs[next].tabIndex = 0;
		tabs[next].focus({ preventScroll: true });
	});
}

function pick(state, k) {
	const item = state.flat[k];
	if (item) send(state, item.glyph, item.entry);
}

async function pickGlyph(state, glyph) {
	if (!glyph) return;
	let entry = null;
	try {
		state.data ??= await load(state.options.dataSource ?? null);
		entry = find(state.data, glyph);
	} catch {
		// Without the data the pick still goes through, only without its name.
	}

	send(state, entry ? withTone(entry, state.options.tone ?? 0) : glyph, entry ?? { glyph, name: '', codes: [] });
}

function send(state, glyph, entry) {
	state.dotNetRef?.invokeMethodAsync('OnEmojiPicked', glyph, entry.glyph, entry.name, entry.codes[0] ?? '').catch(() => { });
}
