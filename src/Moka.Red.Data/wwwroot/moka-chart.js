// The one script every Moka.Red chart shares: hover, keys, pin, sync groups and linked zoom, brush and wheel zoom, plot
// width buckets and the live hold.
//
// Pointer movement stays here. The script finds the point under the pointer itself and calls .NET only when that point
// changes, one call in flight per chart: while one is out, a newer point replaces the one waiting. .NET answers by
// rendering the tooltip host alone. The crosshair, dots, rings, brush, pin line and zoom hint live in the overlay, an
// element .NET renders empty and never touches again.
//
// Geometry is pulled, not pushed. The surface carries data-moka-version; the script asks for the drawn points the first
// time a pointer or the focus arrives after the version moved, so charts nobody points at cost nothing. Bars and rows
// need no geometry: the column under the pointer is its slot, and their marks carry data-i.
//
// Parts in rows (a heatmap's cells, a state timeline's spans) say how they are numbered on their surface: rows and
// columns, or listed row by row. The arrow keys then step along a row and between rows (stepParts). A regular grid is
// worked out from the surface's attributes; a listed one is pulled once a geometry version, the first time the keys
// need it, as format 2 of the same bytes.
//
// Attributes the script writes are never rendered by .NET, so Blazor's diff never fights them: data-moka-shown and
// style on the tooltip and pin card, data-moka-flag on the pin card, data-moka-on on lifted marks, data-moka-dim on
// marks of other series (a mark with data-moka-with stays lit with the series it names, as a comparison does with its
// own), data-moka-hl, data-moka-holding and data-moka-pick on the root, data-moka-zoomed and
// data-moka-panning on the surface, and a transform on the marks while a zoom gesture is previewed.
//
// A chart whose tooltips are the browser's lets the pointer through its surface to the marks, so their titles show. The
// script then finds the surface from the element under the pointer, and the point the way the chart's own tooltip
// would, for a click, a listener and the keys, drawing nothing for the pointer.
//
// State lives per root in a WeakMap and every listener sits on the root, so a root removed without unbind leaks
// nothing. The few document listeners are installed once and prune what has left the page.
//
// Linked zoom: charts of one sync group that pass the same link key (the kind of x axis they have) follow each other's
// zoom. A gesture's preview stretches every linked chart's marks as it moves, with no call to .NET, and when it is let
// go each chart is told its range once, through its own OnRange. A chart told that way never passes it on, so a parent
// that binds Range on every chart can't start a loop.

const GAP = 65535;
const SCALE = 65534;
const FORMAT = 1;
const PART_FORMAT = 2;
const HEADER = 32;
const WIDTH_STEP = 64;
const TIP_GAP = 12;
const WINDOW_MARGIN = 8;
const LINE_REACH = 24;
const BRUSH_MIN = 6;
const COMMIT_DELAY = 150;
const PREVIEW_LIMIT = 1500;
const HINT_TIME = 1500;
const CLICK_FOCUS = 400;
const ZOOM_STEP = 1.5;
const WHEEL_RATE = 0.0025;
const BRUSH = 1;
const WHEEL = 2;

const roots = new WeakMap();
const groups = new Map();
const touching = new Set();
const brushing = new Set();
const supportsPopover = typeof HTMLElement !== 'undefined' && typeof HTMLElement.prototype.showPopover === 'function';

let documentBound = false;
let sizes = null;
const sized = new WeakMap();
const placed = new Set();
let placeFrame = 0;

/**
 * Binds a chart's root. Called once, on the chart's first interactive render.
 * @param {HTMLElement} root - The chart's root element.
 * @param {object} ref - The chart's DotNetObjectReference.
 * @param {object} options - mode, orientation, pin, keys, sync, zoom, hold, measure, nearestPx, report, select,
 *     highlight, pageStep, words.
 */
export function bind(root, ref, options) {
	if (!root || !ref) return;

	const existing = roots.get(root);
	if (existing) {
		existing.ref = ref;
		apply(existing, options);
		return;
	}

	const state = {
		root,
		ref,
		options: {},
		linkTimer: 0,
		panels: new Map(),
		queue: new Map(),
		sent: new Map(),
		busy: false,
		highlight: null,
		highlightBy: null,
		inside: false,
		focused: false,
		holding: false,
		pointerDownAt: 0,
		wheelBound: false,
		measured: null,
		bucket: 0,
		group: null,
		handlers: null,
		observer: null
	};

	roots.set(root, state);
	listen(state);
	apply(state, options);
	installDocument();
}

/**
 * Takes new options, sent only when they changed. Data changes send nothing.
 * @param {HTMLElement} root - The chart's root element.
 * @param {object} options - As for bind.
 */
export function update(root, options) {
	const state = root ? roots.get(root) : null;
	if (state) apply(state, options);
}

/**
 * Lets the chart go: listeners, groups, observers, and everything the script drew or wrote.
 * @param {HTMLElement} root - The chart's root element.
 */
export function unbind(root) {
	const state = root ? roots.get(root) : null;
	if (!state) return;

	applyHighlight(state, null);
	root.removeAttribute('data-moka-pick');
	roots.delete(root);
	clearTimeout(state.linkTimer);
	leaveGroup(state);
	touching.delete(state);
	brushing.delete(state);
	state.observer?.disconnect();
	if (state.measured) sizes?.unobserve(state.measured);

	for (const [name, handler, opts] of state.handlers) root.removeEventListener(name, handler, opts);
	if (state.wheelBound) root.removeEventListener('wheel', state.onWheel, { passive: false });

	for (const ps of state.panels.values()) {
		ps.rowsObserver?.disconnect();
		ps.rowsObserver = null;
		clearTimeout(ps.commitTimer);
		clearTimeout(ps.previewTimer);
		clearTimeout(ps.hintTimer);
		endPreview(ps);
		const tip = part(ps, 'tip');
		const pin = part(ps, 'pin');
		if (tip) hide(tip);
		if (pin) hide(pin);
		lift(ps, -1);
		if (ps.overlay?.isConnected) ps.overlay.textContent = '';
		ps.surface?.removeAttribute('data-moka-zoomed');
		ps.surface?.removeAttribute('data-moka-panning');
	}

	root.removeAttribute('data-moka-holding');
}

/**
 * Sets the pin after .NET let it go from its own button.
 * @param {HTMLElement} root - The chart's root element.
 * @param {number} panel - The panel.
 * @param {number} drawn - The pinned point, or -1 to clear.
 */
export function setPin(root, panel, drawn) {
	const state = root ? roots.get(root) : null;
	const ps = state ? panelState(state, Number(panel) || 0) : null;
	if (!ps) return;

	ps.pin = drawn >= 0 ? drawn : -1;
	paint(state, ps);
	showTip(state, ps);
	showPin(state, ps);
}

/**
 * Moves the focus to a panel's keyboard grid, such as after the pin card's Unpin button.
 * @param {HTMLElement} root - The chart's root element.
 * @param {number} panel - The panel.
 */
export function focusSurface(root, panel) {
	const state = root ? roots.get(root) : null;
	const ps = state ? panelState(state, Number(panel) || 0) : null;
	const grid = ps ? part(ps, 'grid') : null;
	grid?.focus({ preventScroll: true });
}

/**
 * Hands a zoom chosen on the .NET side, by the range strip, a preset or Reset zoom, to the charts linked with this one.
 * The chart itself already shows it, so only the others are told, each through its own OnRange.
 * @param {HTMLElement} root - The chart's root element.
 * @param {number|null} from - Where the range starts along the x axis, or null to show everything.
 * @param {number|null} to - Where it ends, or null.
 */
export function shareZoom(root, from, to) {
	const state = root ? roots.get(root) : null;
	if (!state) return;
	const range = Number.isFinite(from) && Number.isFinite(to) ? [Math.min(from, to), Math.max(from, to)] : null;
	linkPreview(state, range);
	linkCommit(state, range, 0);
}

/**
 * Reads the geometry bytes .NET sends. Exported for tests.
 * @param {Uint8Array|ArrayBuffer|string|number[]} value - What HoverGeometry answered.
 * @returns {object|null} xMin, xMax, version, count, x and y shares (NaN for a gap), and the finite x positions sorted;
 *     for format 2, a listed part layout: rows, cols, count, version, and each part's row, place and span.
 */
export function decodeGeometry(value) {
	const bytes = asBytes(value);
	if (!bytes || bytes.byteLength < HEADER) return null;

	const view = new DataView(bytes.buffer, bytes.byteOffset, bytes.byteLength);
	if (view.getUint16(0, true) === PART_FORMAT) return decodeParts(view);
	if (view.getUint16(0, true) !== FORMAT) return null;

	const seriesCount = view.getUint16(2, true);
	const count = view.getUint32(4, true);
	let offset = HEADER;
	const read = n => {
		const out = new Float32Array(n);
		for (let i = 0; i < n && offset + 2 <= view.byteLength; i++, offset += 2) {
			const raw = view.getUint16(offset, true);
			out[i] = raw === GAP ? NaN : raw / SCALE;
		}
		return out;
	};

	const geo = {
		xMin: view.getFloat64(8, true),
		xMax: view.getFloat64(16, true),
		version: view.getFloat64(24, true),
		count,
		x: read(count),
		y: [],
		order: null
	};

	for (let s = 0; s < seriesCount; s++) geo.y.push(read(count));

	const order = [];
	for (let i = 0; i < count; i++) if (Number.isFinite(geo.x[i])) order.push(i);
	geo.order = order;
	return geo;
}

// A listed part layout, format 2: each row's first part, then where each part starts and ends along its row. Each row's
// parts are put in the order they sit from left to right, which is the order the keys walk.
function decodeParts(view) {
	const count = view.getUint32(4, true);
	const rows = Math.max(0, Math.round(view.getFloat64(8, true)));
	const layout = {
		kind: 'listed',
		rows,
		cols: Math.max(0, Math.round(view.getFloat64(16, true))),
		count,
		version: view.getFloat64(24, true),
		flow: false,
		rowOf: new Int32Array(count),
		pos: new Int32Array(count),
		from: new Float32Array(count),
		to: new Float32Array(count),
		mid: new Float32Array(count),
		orders: []
	};
	let offset = HEADER;
	const starts = [];
	for (let r = 0; r < rows && offset + 4 <= view.byteLength; r++, offset += 4) starts.push(view.getUint32(offset, true));
	const share = () => {
		const raw = offset + 2 <= view.byteLength ? view.getUint16(offset, true) : GAP;
		offset += 2;
		return raw === GAP ? 0 : raw / SCALE;
	};
	for (let i = 0; i < count; i++) layout.from[i] = share();
	for (let i = 0; i < count; i++) {
		const end = share();
		layout.to[i] = end;
		layout.mid[i] = (layout.from[i] + end) / 2;
	}
	for (let r = 0; r < rows; r++) {
		const first = Math.min(starts[r] ?? count, count);
		const last = Math.min(r + 1 < starts.length ? starts[r + 1] : count, count);
		const order = [];
		for (let i = first; i < last; i++) {
			layout.rowOf[i] = r;
			order.push(i);
		}
		order.sort((a, b) => Math.min(layout.from[a], layout.to[a]) - Math.min(layout.from[b], layout.to[b]));
		order.forEach((part, at) => {
			layout.pos[part] = at;
		});
		layout.orders.push(order);
	}
	return layout;
}

/**
 * Where the arrow keys go on a chart of parts in rows. A pure function of the layout, exported for tests.
 * @param {object} layout - kind (rows, columns or listed), rows, cols, count, flow, and a listed layout's places.
 * @param {number} drawn - The part the keys are on, or -1 for none yet.
 * @param {string} key - The key pressed.
 * @param {boolean} ctrl - Whether Control (or Command) was held, which sends Home and End to the first and last part.
 * @param {boolean} turned - Whether the chart drew its grid on its side, so its rows run down and its columns across.
 * @param {number|null} pageStep - How far Page Up and Page Down go along a row; null is a tenth of it.
 * @returns {number} The part to go to, or -1 when the key isn't one of the grid's.
 */
export function stepParts(layout, drawn, key, ctrl, turned, pageStep) {
	const n = layout?.count ?? 0;
	if (n <= 0 || !isNavigation(key)) return -1;
	if (!(drawn >= 0 && drawn < n)) return key === 'End' && ctrl ? n - 1 : 0;
	if (ctrl && (key === 'Home' || key === 'End')) return key === 'Home' ? 0 : n - 1;

	const swap = turned && layout.kind !== 'listed';
	const place = placeOf(layout, drawn);
	const length = swap ? columnLength(layout, place.col) : rowLength(layout, place.row);
	const page = pageStep > 0 ? pageStep : Math.max(2, Math.round(length / 10));
	const horizontal = delta => (swap ? acrossRows(layout, drawn, delta) : along(layout, drawn, delta));
	const vertical = delta => (swap ? along(layout, drawn, delta)
		: layout.flow ? clampIndex(drawn + delta, n) : acrossRows(layout, drawn, delta));

	switch (key) {
		case 'ArrowRight': return horizontal(1);
		case 'ArrowLeft': return horizontal(-1);
		case 'ArrowDown': return vertical(1);
		case 'ArrowUp': return vertical(-1);
		case 'PageDown': return horizontal(page);
		case 'PageUp': return horizontal(-page);
		case 'Home': return swap ? columnEnd(layout, place.col, false) : partAt(layout, place.row, 0);
		default: return swap ? columnEnd(layout, place.col, true) : partAt(layout, place.row, rowLength(layout, place.row) - 1);
	}
}

// A part's row and its place along the row, from 0.
function placeOf(layout, d) {
	if (layout.kind === 'rows') {
		const c = Math.max(1, layout.cols);
		return { row: Math.floor(d / c), col: d % c };
	}
	if (layout.kind === 'columns') {
		const r = Math.max(1, layout.rows);
		return { row: d % r, col: Math.floor(d / r) };
	}
	return { row: layout.rowOf[d] ?? 0, col: layout.pos[d] ?? 0 };
}

function partAt(layout, row, col) {
	if (row < 0 || row >= layout.rows || col < 0) return -1;
	if (layout.kind === 'rows') {
		const d = row * layout.cols + col;
		return col < layout.cols && d < layout.count ? d : -1;
	}
	if (layout.kind === 'columns') {
		const d = col * layout.rows + row;
		return d < layout.count ? d : -1;
	}
	const order = layout.orders[row];
	return order && col < order.length ? order[col] : -1;
}

function rowLength(layout, row) {
	if (row < 0 || row >= layout.rows) return 0;
	if (layout.kind === 'rows') return Math.max(0, Math.min(layout.cols, layout.count - row * layout.cols));
	if (layout.kind === 'columns') return row < layout.count ? Math.floor((layout.count - 1 - row) / Math.max(1, layout.rows)) + 1 : 0;
	return layout.orders[row]?.length ?? 0;
}

function columnLength(layout, col) {
	let length = 0;
	for (let r = 0; r < layout.rows; r++) if (partAt(layout, r, col) >= 0) length++;
	return length;
}

function columnEnd(layout, col, last) {
	for (let k = 0; k < layout.rows; k++) {
		const d = partAt(layout, last ? layout.rows - 1 - k : k, col);
		if (d >= 0) return d;
	}
	return -1;
}

function along(layout, d, delta) {
	const { row, col } = placeOf(layout, d);
	const found = partAt(layout, row, Math.max(0, Math.min(rowLength(layout, row) - 1, col + delta)));
	return found >= 0 ? found : d;
}

// The part in the row delta rows away, skipping empty rows, that holds the middle of this one, else the nearest to it.
function acrossRows(layout, d, delta) {
	const { row } = placeOf(layout, d);
	const way = delta < 0 ? -1 : 1;
	let target = row;
	for (let r = row + way, left = Math.abs(delta); left > 0 && r >= 0 && r < layout.rows; r += way) {
		if (rowLength(layout, r) > 0) {
			target = r;
			left--;
		}
	}
	if (target === row) return d;
	const middle = layout.kind === 'listed' ? layout.mid[d] : (placeOf(layout, d).col + 0.5) / Math.max(1, layout.cols);
	return nearestInRow(layout, target, middle);
}

function nearestInRow(layout, row, share) {
	const length = rowLength(layout, row);
	if (layout.kind !== 'listed') {
		return partAt(layout, row, Math.min(length - 1, Math.max(0, Math.floor(share * Math.max(1, layout.cols)))));
	}
	let best = -1;
	let distance = Infinity;
	for (const i of layout.orders[row]) {
		if (share >= Math.min(layout.from[i], layout.to[i]) && share <= Math.max(layout.from[i], layout.to[i])) return i;
		const d = Math.abs(layout.mid[i] - share);
		if (d < distance) {
			distance = d;
			best = i;
		}
	}
	return best;
}

function asBytes(value) {
	if (!value) return null;
	if (value instanceof Uint8Array) return value;
	if (value instanceof ArrayBuffer) return new Uint8Array(value);
	if (Array.isArray(value)) return Uint8Array.from(value);
	if (typeof value === 'string') {
		const text = atob(value);
		const out = new Uint8Array(text.length);
		for (let i = 0; i < text.length; i++) out[i] = text.charCodeAt(i);
		return out;
	}
	return null;
}

// Options

function apply(state, options) {
	const before = state.options;
	const next = Object.assign({
		mode: 'none', track: null, orientation: 'columns', hit: false, pass: false, placed: false, pin: false, keys: false,
		sync: null, link: null, zoom: 0, hold: false, measure: false, nearestPx: 32, report: false, select: false,
		highlight: false, pageStep: null, words: {}
	}, options || {});
	// What finds the point under the pointer: the tooltip's own mode, or, on a chart whose tooltips are the browser's,
	// the mode it would take, so a click, a listener and the keys still know the point without drawing a tooltip.
	next.find = next.mode !== 'none' ? next.mode : next.track || 'none';
	state.options = next;

	if (before.find !== next.find || before.orientation !== next.orientation || before.placed !== next.placed) {
		for (const ps of state.panels.values()) {
			ps.active = null;
			ps.geo = null;
			paint(state, ps);
			const tip = part(ps, 'tip');
			if (tip) hide(tip);
		}
	}

	if (before.sync !== next.sync) {
		leaveGroup(state);
		if (next.sync) joinGroup(state, next.sync);
	}

	const wantsWheel = (next.zoom & WHEEL) !== 0;
	if (wantsWheel && !state.wheelBound) {
		state.root.addEventListener('wheel', state.onWheel, { passive: false });
		state.wheelBound = true;
	} else if (!wantsWheel && state.wheelBound) {
		state.root.removeEventListener('wheel', state.onWheel, { passive: false });
		state.wheelBound = false;
	}

	if (!next.hold && state.holding) setHolding(state, false, true);
	if (!next.highlight && state.highlight) applyHighlight(state, null);
	measure(state);
}

function needsDotNet(state) {
	const o = state.options;
	return o.mode !== 'none' || o.keys || o.report;
}

// Listeners, all on the root

function listen(state) {
	const root = state.root;
	const on = (name, handler, opts) => {
		root.addEventListener(name, handler, opts);
		state.handlers.push([name, handler, opts]);
	};
	state.handlers = [];

	on('pointermove', e => pointerMove(state, e));
	on('pointerdown', e => pointerDown(state, e));
	on('pointerup', e => pointerUp(state, e));
	on('pointercancel', e => pointerUp(state, e, true));
	on('pointerover', e => pointerOver(state, e));
	on('pointerout', e => pointerOut(state, e));
	on('click', e => click(state, e));
	on('dblclick', e => doubleClick(state, e));
	on('keydown', e => keyDown(state, e));
	on('focusin', e => focusIn(state, e));
	on('focusout', e => focusOut(state, e));
	state.onWheel = e => wheel(state, e);

	state.observer = new MutationObserver(records => mutated(state, records));
	state.observer.observe(root, { subtree: true, attributes: true, attributeFilter: ['data-moka-version', 'data-moka-for'] });
}

function installDocument() {
	if (documentBound) return;
	documentBound = true;

	// A tap outside a chart puts its touch tooltip away.
	document.addEventListener('pointerdown', e => {
		for (const state of [...touching]) {
			if (!state.root.isConnected || !roots.has(state.root)) {
				touching.delete(state);
				continue;
			}
			if (state.root.contains(e.target)) continue;
			touching.delete(state);
			for (const ps of state.panels.values()) {
				ps.lastPointer = null;
				if (ps.active?.via === 'pointer') clear(state, ps);
			}
		}
	}, true);

	// A card in the top layer is fixed to the window, so it follows its plot when anything scrolls, and steps aside
	// while its point is out of sight.
	const follow = () => {
		if (placeFrame) return;
		placeFrame = requestAnimationFrame(() => {
			placeFrame = 0;
			for (const state of [...placed]) {
				if (!state.root.isConnected || !roots.has(state.root)) {
					placed.delete(state);
					continue;
				}
				for (const ps of state.panels.values()) {
					showPin(state, ps);
					showTip(state, ps);
				}
			}
		});
	};
	window.addEventListener('scroll', follow, { capture: true, passive: true });
	window.addEventListener('resize', follow, { passive: true });

	// Escape cancels a brush wherever the focus is, since a mouse drag doesn't move it.
	document.addEventListener('keydown', e => {
		if (e.key !== 'Escape' || !brushing.size) return;
		for (const state of [...brushing]) {
			brushing.delete(state);
			for (const ps of state.panels.values()) if (ps.gesture?.kind === 'brush') cancelGesture(state, ps);
		}
		e.stopPropagation();
	}, true);
}

function sizer() {
	if (sizes) return sizes;
	sizes = new ResizeObserver(entries => {
		for (const entry of entries) {
			const target = entry.target;
			const handler = sized.get(target);
			if (!target.isConnected || !handler) {
				sizes.unobserve(target);
				sized.delete(target);
				continue;
			}
			handler(entry);
		}
	});
	return sizes;
}

// Panels and their parts

function owner(element) {
	for (let node = element; node; node = node.parentElement) {
		if (roots.has(node)) return node;
	}
	return null;
}

function surfaceFrom(state, target) {
	if (!(target instanceof Element)) return null;
	const o = state.options;
	const surface = target.closest('.moka-chart-surface')
		|| (o.hit ? markSurface(target.closest('[data-i]')) : o.pass ? markSurface(target, true) : null);
	return surface && owner(surface) === state.root ? surface : null;
}

// On a chart of parts, or one the pointer passes through, the pointer is over a mark, not the surface: the panel's
// surface sits beside the marks, in the box both share.
function markSurface(mark, self) {
	for (let node = self ? mark : mark?.parentElement; node; node = node.parentElement) {
		const surface = node.querySelector(':scope > .moka-chart-surface');
		if (surface) return surface;
		if (roots.has(node)) break;
	}
	return null;
}

function markOf(ps, target) {
	const mark = target instanceof Element ? target.closest('[data-i]') : null;
	return mark && ps.surface?.parentElement?.contains(mark) && !ps.surface.contains(mark) ? mark : null;
}

function surfacesOf(state) {
	return [...state.root.querySelectorAll('.moka-chart-surface')].filter(s => owner(s) === state.root);
}

function panelState(state, panel, surface) {
	surface ??= surfacesOf(state).find(s => Number(s.dataset.mokaPanel || 0) === panel) || null;
	let ps = state.panels.get(panel);
	if (!ps) {
		ps = {
			panel, surface: null, overlay: null, parts: {}, geo: null, geoVersion: NaN, pulling: false, missing: false,
			active: null, pin: -1, lastPointer: null, wasActive: -1, gesture: null, preview: null, touches: new Map(),
			commitTimer: 0, previewTimer: 0, hintTimer: 0, swallowClick: false, clickWaiting: null, lifted: [], syncWant: undefined,
			rowsObserver: null, layout: null, layoutVersion: NaN, layoutPulling: false, layoutMissing: false, keyWaiting: null
		};
		state.panels.set(panel, ps);
	}

	if (surface && ps.surface !== surface) {
		ps.surface = surface;
		ps.overlay = surface.querySelector(':scope > .moka-chart-overlay');
		ps.parts = {};
		watchRows(state, ps);
	}

	return ps.surface ? ps : null;
}

// Virtualize draws rows as they scroll in, after the keyboard or the legend already acted on them, and Blazor reuses a
// row's elements for another row by changing their attributes. So the rows' body is watched while the chart is bound:
// a row that arrives or changes lifts when it is the active one and dims with its series. Changes inside the surface
// (the tooltip) don't count.
function watchRows(state, ps) {
	ps.rowsObserver?.disconnect();
	ps.rowsObserver = null;
	const body = ps.surface?.parentElement;
	if (!body?.classList.contains('moka-chart-plot__rbody')) return;
	ps.rowsObserver = new MutationObserver(records => rowsChanged(state, ps, records));
	ps.rowsObserver.observe(body, { childList: true, subtree: true, attributes: true, attributeFilter: ['data-i', 'data-moka-key'] });
}

function rowsChanged(state, ps, records) {
	if (!roots.has(state.root) || !ps.surface?.isConnected) return;
	if (!records.some(record => !ps.surface.contains(record.target))) return;
	if (ps.active) {
		lift(ps, ps.active.drawn);
		// A part the keys reached in a row that wasn't drawn yet has its tooltip placed once the row arrives.
		showTip(state, ps);
	}
	if (state.highlight !== null) applyHighlight(state, state.highlight);
}

function panelFromSurface(state, surface) {
	return surface ? panelState(state, Number(surface.dataset.mokaPanel || 0), surface) : null;
}

const PARTS = {
	tip: ':scope > .moka-chart-tip',
	pin: ':scope > .moka-chart-pin',
	grid: ':scope > .moka-chart-surface__grid',
	series: ':scope > .moka-chart-surface__series'
};

function part(ps, name) {
	if (!ps.surface) return null;
	const cached = ps.parts[name];
	if (cached?.isConnected && cached.parentElement === ps.surface) return cached;
	const found = ps.surface.querySelector(PARTS[name]);
	ps.parts[name] = found;
	return found;
}

function series(ps) {
	const list = part(ps, 'series');
	if (!list) return [];
	return [...list.children].map((el, index) => ({
		index,
		key: el.getAttribute('data-moka-key') ?? String(index),
		with: el.getAttribute('data-moka-with'),
		hidden: el.hasAttribute('data-moka-hidden'),
		color: el.getAttribute('class') || '',
		style: el.getAttribute('style') || ''
	}));
}

function count(ps) {
	if (ps.geo) return ps.geo.count;
	return Math.max(0, Number(ps.surface?.dataset.mokaCount || 0));
}

function extent(ps) {
	const from = Number(ps.surface?.dataset.mokaX0);
	const to = Number(ps.surface?.dataset.mokaX1);
	if (ps.surface?.hasAttribute('data-moka-x0') && Number.isFinite(from) && Number.isFinite(to) && to > from) return [from, to];
	if (ps.geo && ps.geo.xMax > ps.geo.xMin) return [ps.geo.xMin, ps.geo.xMax];
	return null;
}

// The span the plot shows: from its geometry once pulled, else from what .NET wrote on a surface whose zoom is linked,
// so a chart nobody pointed at can still follow a linked zoom.
function viewWindow(ps) {
	if (ps.geo && ps.geo.xMax > ps.geo.xMin) return [ps.geo.xMin, ps.geo.xMax];
	const from = Number(ps.surface?.dataset.mokaW0);
	const to = Number(ps.surface?.dataset.mokaW1);
	if (ps.surface?.hasAttribute('data-moka-w0') && Number.isFinite(from) && Number.isFinite(to) && to > from) return [from, to];
	return extent(ps);
}

// Geometry

function version(ps) {
	return Number(ps.surface?.dataset.mokaVersion ?? NaN);
}

function geometry(state, ps) {
	if (!needsGeometry(state)) return null;

	const current = version(ps);
	if ((ps.geo || ps.missing) && ps.geoVersion >= current) return ps.geo;
	if (ps.pulling) return undefined;

	ps.pulling = true;
	Promise.resolve(state.ref.invokeMethodAsync('HoverGeometry', ps.panel, current))
		.then(answer => {
			ps.pulling = false;
			if (!roots.has(state.root)) return;
			const geo = decodeGeometry(answer);
			ps.geo = geo;
			ps.missing = !geo;
			ps.geoVersion = geo ? Math.max(current, geo.version) : current;
			arrived(state, ps);
		})
		.catch(() => {
			ps.pulling = false;
			ps.clickWaiting = null;
		});
	return undefined;
}

function arrived(state, ps) {
	markZoomed(ps);
	if (ps.syncWant !== undefined) {
		const want = ps.syncWant;
		ps.syncWant = undefined;
		syncTo(state, ps, want);
		return;
	}

	if (ps.active?.via === 'key') {
		const n = count(ps);
		if (ps.active.drawn >= n) ps.active.drawn = Math.max(0, n - 1);
		paint(state, ps);
		showTip(state, ps);
		report(state, ps.panel, ps.active.series, ps.active.drawn, true);
	} else if (ps.pendingFocus) {
		ps.pendingFocus = false;
		focusFirst(state, ps);
	} else if (ps.lastPointer) {
		hover(state, ps, ps.lastPointer.x, ps.lastPointer.y, ps.lastPointer.target);
	}

	// A click that came before the geometry, such as a first tap, acts on the point it found once it arrived.
	if (ps.clickWaiting) {
		const touched = ps.clickWaiting.touched;
		ps.clickWaiting = null;
		if (ps.active && ps.active.via === 'pointer') act(state, ps, ps.active, touched);
	}

	showPin(state, ps);
}

function markZoomed(ps) {
	const span = extent(ps);
	const shown = viewWindow(ps);
	const zoomed = !!(span && shown && ps.geo && (shown[0] > span[0] + 1e-9 * Math.abs(span[1] - span[0])
		|| shown[1] < span[1] - 1e-9 * Math.abs(span[1] - span[0])));
	ps.surface?.toggleAttribute('data-moka-zoomed', zoomed);
	ps.zoomed = zoomed;
}

function shareOf(ps, drawn, columns) {
	if (ps.geo) return ps.geo.x[drawn];
	if (columns) {
		const n = count(ps);
		return n > 0 ? (drawn + 0.5) / n : 0.5;
	}
	const n = count(ps);
	return n <= 1 ? 0.5 : drawn / (n - 1);
}

function yOf(ps, seriesIndex, drawn) {
	const y = ps.geo?.y[seriesIndex];
	return y && drawn < y.length ? y[drawn] : NaN;
}

function nearestX(ps, share) {
	const geo = ps.geo;
	if (!geo) {
		const n = count(ps);
		return n <= 0 ? -1 : Math.max(0, Math.min(n - 1, Math.round(share * (n - 1))));
	}

	const order = geo.order;
	if (!order.length) return -1;
	let lo = 0;
	let hi = order.length - 1;
	while (hi - lo > 1) {
		const mid = (lo + hi) >> 1;
		if (geo.x[order[mid]] < share) lo = mid;
		else hi = mid;
	}
	return Math.abs(geo.x[order[lo]] - share) <= Math.abs(geo.x[order[hi]] - share) ? order[lo] : order[hi];
}

function nearestByValue(ps, value) {
	const geo = ps.geo;
	const shown = viewWindow(ps);
	if (!shown) return -1;
	const share = (value - shown[0]) / (shown[1] - shown[0]);
	if (geo && (share < -0.01 || share > 1.01)) return -1;
	return nearestX(ps, Math.max(0, Math.min(1, share)));
}

function visibleSeries(ps) {
	return series(ps).filter(s => !s.hidden);
}

function latest(ps, seriesIndex) {
	const n = count(ps);
	if (!ps.geo) return n - 1;
	for (let i = n - 1; i >= 0; i--) {
		if (Number.isFinite(ps.geo.x[i]) && Number.isFinite(yOf(ps, seriesIndex, i))) return i;
	}
	return ps.geo.order.length ? ps.geo.order[ps.geo.order.length - 1] : n - 1;
}

// Pointer

function local(ps, x, y) {
	const r = ps.surface.getBoundingClientRect();
	return { r, sx: r.width ? (x - r.left) / r.width : 0, sy: r.height ? (y - r.top) / r.height : 0, px: x - r.left, py: y - r.top };
}

function pointerMove(state, e) {
	const surface = surfaceFrom(state, e.target);
	const gestured = [...state.panels.values()].find(p => p.gesture
		&& (p.gesture.id === e.pointerId || (p.gesture.kind === 'pinch' && p.touches.has(e.pointerId))));
	if (gestured) {
		moveGesture(state, gestured, e);
		return;
	}
	if (!surface || insideCard(e.target)) return;
	const ps = panelFromSurface(state, surface);
	if (!ps) return;

	if (e.pointerType === 'touch') {
		if (ps.touches.has(e.pointerId)) {
			ps.touches.set(e.pointerId, { x: e.clientX, y: e.clientY });
			hover(state, ps, e.clientX, e.clientY, e.target);
		}
		return;
	}

	ps.lastPointer = { x: e.clientX, y: e.clientY, target: e.target };
	hover(state, ps, e.clientX, e.clientY, e.target);
}

function pointerDown(state, e) {
	state.pointerDownAt = performance.now();
	const surface = surfaceFrom(state, e.target);
	if (!surface || insideCard(e.target)) return;
	const ps = panelFromSurface(state, surface);
	if (!ps) return;

	ps.wasActive = ps.active ? ps.active.drawn : -1;
	const zoom = state.options.zoom;

	if (e.pointerType === 'touch') {
		ps.touches.set(e.pointerId, { x: e.clientX, y: e.clientY });
		touching.add(state);
		if (ps.touches.size === 2 && (zoom & WHEEL)) {
			startPinch(state, ps);
			return;
		}
		if (ps.touches.size === 1 && ps.zoomed && (zoom & WHEEL)) {
			startPan(state, ps, e);
			return;
		}
		ps.lastPointer = { x: e.clientX, y: e.clientY, target: e.target };
		hover(state, ps, e.clientX, e.clientY, e.target);
		return;
	}

	if (e.button !== 0) return;
	if ((zoom & BRUSH) && !(e.shiftKey && ps.zoomed && (zoom & WHEEL))) {
		startBrush(state, ps, e);
	} else if (ps.zoomed && (zoom & WHEEL)) {
		startPan(state, ps, e);
	}
}

function pointerUp(state, e, cancelled) {
	for (const ps of state.panels.values()) {
		const g = ps.gesture;
		ps.touches.delete(e.pointerId);
		if (!g) continue;
		if (g.kind === 'pinch' ? ps.touches.size < 2 : g.id === e.pointerId) endGesture(state, ps, cancelled);
	}
}

function pointerOver(state, e) {
	if (state.options.highlight) {
		const legend = e.target instanceof Element ? e.target.closest('[data-moka-legend-key]') : null;
		if (legend && owner(legend) === state.root) {
			highlight(state, legend.getAttribute('data-moka-legend-key'), 'legend');
		}
	}
	if (state.options.hold && !state.inside) {
		state.inside = true;
		setHolding(state, true);
	}
}

function pointerOut(state, e) {
	const to = e.relatedTarget;
	if (state.options.highlight && state.highlightBy === 'legend') {
		const legend = e.target instanceof Element ? e.target.closest('[data-moka-legend-key]') : null;
		if (legend && !(to instanceof Node && legend.contains(to))) highlight(state, null, 'legend');
	}

	if (e.pointerType !== 'touch') {
		const surface = surfaceFrom(state, e.target) || between(state, e.target);
		const through = state.options.hit || state.options.pass;
		const stays = to instanceof Node
			&& (surface?.contains(to) || (through && (surfaceFrom(state, to) || between(state, to)) === surface));
		if (surface && !stays && !insideCard(to)) {
			const ps = panelFromSurface(state, surface);
			if (ps) {
				ps.lastPointer = null;
				if (!ps.gesture && ps.active && ps.active.via !== 'key') clear(state, ps);
				if (state.highlightBy === 'line') highlight(state, null, 'line');
			}
		}
	}

	if (state.options.hold && state.inside && !(to instanceof Node && state.root.contains(to))) {
		state.inside = false;
		setHolding(state, state.focused);
	}
}

// On a grid of parts, such as a heatmap's cells or a timeline's rows, the gaps between parts and the names beside them
// belong to the drawing: the part under the pointer stays while the pointer crosses them, so a 2 px gap between cells
// neither flickers the tooltip nor calls .NET, and it goes once the pointer leaves the drawing.
function between(state, target) {
	if (!state.options.hit || !(target instanceof Element)) return null;
	const surface = markSurface(target, true);
	return surface?.hasAttribute('data-moka-order') && owner(surface) === state.root ? surface : null;
}

function insideCard(target) {
	return target instanceof Element && !!target.closest('.moka-chart-pin, .moka-chart-tip');
}

function hover(state, ps, x, y, target) {
	const o = state.options;
	if (o.find === 'none' || ps.gesture || ps.preview) return;
	const at = local(ps, x, y);
	if (o.hit) {
		const drawn = Number(markOf(ps, target)?.getAttribute('data-i'));
		if (Number.isInteger(drawn) && drawn >= 0) setActive(state, ps, -1, drawn, 'pointer', at);
		else if (ps.active?.via !== 'key') clear(state, ps);
		return;
	}
	if (at.sx < -0.02 || at.sx > 1.02 || at.sy < -0.05 || at.sy > 1.05) {
		if (ps.active?.via !== 'key') clear(state, ps);
		return;
	}

	if (o.orientation === 'rows') {
		const n = count(ps);
		if (n <= 0) return;
		const drawn = Math.max(0, Math.min(n - 1, Math.floor(at.sy * n)));
		setActive(state, ps, -1, drawn, 'pointer', at);
		return;
	}

	if (o.find === 'column' && !o.placed) {
		const n = count(ps);
		if (n <= 0) return;
		const drawn = Math.max(0, Math.min(n - 1, Math.floor(at.sx * n)));
		setActive(state, ps, -1, drawn, 'pointer', at);
		return;
	}

	const geo = geometry(state, ps);
	if (geo === undefined) return;

	if (o.find === 'column') {
		const drawn = nearestX(ps, at.sx);
		if (drawn >= 0) setActive(state, ps, -1, drawn, 'pointer', at);
		return;
	}

	if (o.find === 'nearest') {
		const best = nearestPoint(state, ps, at);
		if (best) setActive(state, ps, best.series, best.drawn, 'pointer', at);
		else if (ps.active?.via !== 'key') clear(state, ps);
		return;
	}

	const drawn = nearestX(ps, at.sx);
	if (drawn < 0) return;
	let seriesIndex = -1;
	if (o.highlight && ps.geo) {
		const close = closestLine(ps, drawn, at);
		highlight(state, close ? close.key : null, 'line');
		if (close) seriesIndex = close.index;
	}
	setActive(state, ps, seriesIndex, drawn, 'pointer', at);
}

function closestLine(ps, drawn, at) {
	let best = null;
	let distance = LINE_REACH;
	for (const s of visibleSeries(ps)) {
		const y = yOf(ps, s.index, drawn);
		if (!Number.isFinite(y)) continue;
		const d = Math.abs((1 - y) * at.r.height - at.py);
		if (d < distance) {
			distance = d;
			best = s;
		}
	}
	return best;
}

function nearestPoint(state, ps, at) {
	const geo = ps.geo;
	if (!geo) return null;
	const reach = state.options.nearestPx || 32;
	const w = at.r.width || 1;
	const h = at.r.height || 1;
	let best = null;
	let limit = reach * reach;
	const consider = (s, x, y, drawn) => {
		if (!Number.isFinite(x) || !Number.isFinite(y)) return;
		const dx = x * w - at.px;
		const dy = (1 - y) * h - at.py;
		const d = dx * dx + dy * dy;
		if (d <= limit) {
			limit = d;
			best = { series: s, drawn };
		}
	};

	for (const s of visibleSeries(ps)) {
		const order = geo.order;
		const from = lowerBound(geo, order, at.sx - reach / w);
		for (let k = from; k < order.length; k++) {
			const i = order[k];
			if (geo.x[i] > at.sx + reach / w) break;
			consider(s.index, geo.x[i], yOf(ps, s.index, i), i);
		}
	}
	return best;
}

function lowerBound(geo, order, share) {
	let lo = 0;
	let hi = order.length;
	while (lo < hi) {
		const mid = (lo + hi) >> 1;
		if (geo.x[order[mid]] < share) lo = mid + 1;
		else hi = mid;
	}
	return lo;
}

function click(state, e) {
	const surface = surfaceFrom(state, e.target);
	if (!surface || insideCard(e.target)) return;
	const ps = panelFromSurface(state, surface);
	if (!ps) return;
	if (ps.swallowClick) {
		ps.swallowClick = false;
		return;
	}
	const touched = e.pointerType === 'touch' || (e.pointerType === '' && touching.has(state));
	const a = ps.active;
	if (!a) {
		if (ps.pulling) ps.clickWaiting = { touched };
		return;
	}

	// A part that is a button or a link does what it does; it doesn't also pin.
	if (state.options.hit && e.target instanceof Element && e.target.closest('[data-i]')?.closest('button, a')) return;
	act(state, ps, a, touched);
}

function act(state, ps, a, touched) {
	if (state.options.pin && (!touched || ps.wasActive === a.drawn)) togglePin(state, ps, a.drawn);
	if (state.options.select) call(state, 'OnSelect', ps.panel, a.series, a.drawn);
}

function doubleClick(state, e) {
	const surface = surfaceFrom(state, e.target);
	if (!surface || !(state.options.zoom & (BRUSH | WHEEL))) return;
	const ps = panelFromSurface(state, surface);
	if (ps?.zoomed) commit(state, ps, null, 0);
}

// The active point

function setActive(state, ps, seriesIndex, drawn, via, at) {
	const before = ps.active;
	ps.active = { series: seriesIndex, drawn, via, py: at ? at.py : null, px: at ? at.px : null };
	const moved = !before || before.drawn !== drawn || before.series !== seriesIndex;

	state.root.toggleAttribute('data-moka-pick', !!state.options.select && via === 'pointer');
	if (moved || before.via !== via) paint(state, ps);
	showTip(state, ps);
	if (state.options.pin) showPin(state, ps);
	if (moved) report(state, ps.panel, seriesIndex, drawn, via === 'key');
	if (moved && via !== 'sync') spread(state, ps, drawn);
}

function clear(state, ps, fromSync) {
	if (!ps.active) return;
	ps.active = null;
	state.root.removeAttribute('data-moka-pick');
	paint(state, ps);
	const tip = part(ps, 'tip');
	if (tip) hide(tip);
	if (state.options.pin) showPin(state, ps);
	report(state, ps.panel, -1, -1, false);
	if (!fromSync) spread(state, ps, -1);
}

function report(state, panel, seriesIndex, drawn, keyboard) {
	if (!needsDotNet(state)) return;
	state.queue.set(panel, { seriesIndex, drawn, keyboard, key: `${seriesIndex}:${drawn}` });
	pump(state);
}

async function pump(state) {
	if (state.busy) return;
	state.busy = true;
	try {
		while (state.queue.size) {
			const [panel, want] = state.queue.entries().next().value;
			state.queue.delete(panel);
			if (state.sent.get(panel) === want.key) continue;
			state.sent.set(panel, want.key);
			try {
				await state.ref.invokeMethodAsync('OnActive', panel, want.seriesIndex, want.drawn, want.keyboard);
			} catch {
				// The circuit went away; the next bind starts over.
			}
			if (!roots.has(state.root)) break;
		}
	} finally {
		state.busy = false;
	}
}

function call(state, method, ...args) {
	Promise.resolve(state.ref.invokeMethodAsync(method, ...args)).catch(() => {});
}

// Drawing in the overlay

function element(ps, name, className) {
	let el = ps.parts[`ov-${name}`];
	if (!el || el.parentElement !== ps.overlay) {
		el = document.createElement('div');
		el.className = className;
		el.hidden = true;
		ps.overlay?.appendChild(el);
		ps.parts[`ov-${name}`] = el;
	}
	return el;
}

function dots(ps, name) {
	let list = ps.parts[`ov-${name}`];
	if (!list || list.some(d => d.parentElement !== ps.overlay)) {
		list?.forEach(d => d.remove());
		list = [];
		ps.parts[`ov-${name}`] = list;
	}
	return list;
}

function placeDots(ps, name, drawn, only, focus, nearest) {
	const list = dots(ps, name);
	const wanted = [];
	if (drawn >= 0 && ps.geo) {
		for (const s of visibleSeries(ps)) {
			if (only >= 0 && s.index !== only) continue;
			const x = ps.geo.x[drawn];
			const y = yOf(ps, s.index, drawn);
			if (!Number.isFinite(x) || !Number.isFinite(y)) continue;
			wanted.push({ s, x, y });
		}
	}

	while (list.length < wanted.length) {
		const dot = document.createElement('div');
		ps.overlay?.appendChild(dot);
		list.push(dot);
	}
	for (let i = 0; i < list.length; i++) {
		const dot = list[i];
		const w = wanted[i];
		if (!w) {
			dot.hidden = true;
			continue;
		}
		const classes = ['moka-chart-ov-dot'];
		if (w.s.color) classes.push(w.s.color);
		if (focus === w.s.index) classes.push('moka-chart-ov-dot--focus');
		if (nearest) classes.push('moka-chart-ov-dot--nearest');
		dot.className = classes.join(' ');
		dot.setAttribute('data-moka-key', w.s.key);
		dot.setAttribute('style', `${w.s.style ? `${w.s.style};` : ''}left:${percent(w.x)};top:${percent(1 - w.y)}`);
		dot.toggleAttribute('data-moka-dim', !!ps.dimKey && w.s.key !== ps.dimKey && w.s.with !== ps.dimKey);
		dot.hidden = false;
	}
}

function percent(share) {
	return `${(share * 100).toFixed(3)}%`;
}

function paint(state, ps) {
	if (!ps.overlay?.isConnected) {
		ps.overlay = ps.surface?.querySelector(':scope > .moka-chart-overlay') || null;
		ps.parts = {};
		if (!ps.overlay) return;
	}

	const o = state.options;
	// Without a tooltip of its own the chart marks only the keyboard's point; the pointer has the browser's titles.
	const a = o.mode === 'none' && ps.active?.via !== 'key' ? null : ps.active;
	const columns = o.find === 'column' || o.orientation === 'rows';
	const cross = element(ps, 'cross', 'moka-chart-ov-cross');
	const ring = element(ps, 'colring', 'moka-chart-ov-colring');
	const pinline = element(ps, 'pinline', 'moka-chart-ov-pinline');
	const pinband = element(ps, 'pinband', 'moka-chart-ov-pinband');

	if (a && !columns && o.find !== 'nearest') {
		const share = shareOf(ps, a.drawn, false);
		cross.hidden = !Number.isFinite(share);
		if (!cross.hidden) cross.style.left = percent(share);
		placeDots(ps, 'dots', a.drawn, -1, a.via === 'key' ? a.series : -1, false);
	} else if (a && o.find === 'nearest') {
		cross.hidden = true;
		placeDots(ps, 'dots', a.drawn, a.series, a.via === 'key' ? a.series : -1, true);
	} else {
		cross.hidden = true;
		placeDots(ps, 'dots', -1, -1, -1, false);
	}

	lift(ps, a && columns ? a.drawn : -1);
	if (a && columns && a.via === 'key' && !o.hit) {
		const box = markBox(ps, a.drawn) || slotBox(state, ps, a.drawn);
		ring.hidden = !box;
		if (box) Object.assign(ring.style, box);
	} else {
		ring.hidden = true;
	}

	const pinned = ps.pin >= 0 && ps.pin < Math.max(count(ps), ps.pin + 1);
	if (pinned && !columns) {
		const share = o.find === 'nearest' ? NaN : shareOf(ps, ps.pin, false);
		pinline.hidden = !Number.isFinite(share);
		if (!pinline.hidden) pinline.style.left = percent(share);
		placeDots(ps, 'pindots', ps.pin, -1, -1, false);
		pinband.hidden = true;
	} else if (pinned && columns && !o.hit) {
		const n = count(ps);
		pinline.hidden = true;
		placeDots(ps, 'pindots', -1, -1, -1, false);
		pinband.hidden = n <= 0;
		if (n > 0) Object.assign(pinband.style, slotBox(state, ps, ps.pin));
	} else {
		pinline.hidden = true;
		pinband.hidden = true;
		placeDots(ps, 'pindots', -1, -1, -1, false);
	}
}

function marksOf(ps, drawn) {
	const scope = ps.surface?.parentElement;
	if (!scope || drawn < 0) return [];
	return [...scope.querySelectorAll(`[data-i="${drawn}"]`)].filter(el => !ps.surface.contains(el));
}

function lift(ps, drawn) {
	for (const el of ps.lifted) el.removeAttribute('data-moka-on');
	ps.lifted = marksOf(ps, drawn);
	for (const el of ps.lifted) el.setAttribute('data-moka-on', '');
}

// Where a drawn point's marks are, in the window's coordinates, or null when none are drawn.
function marksRect(ps, drawn) {
	let left = Infinity;
	let top = Infinity;
	let right = -Infinity;
	let bottom = -Infinity;
	for (const el of marksOf(ps, drawn)) {
		const b = el.getBoundingClientRect();
		if (!b.width && !b.height) continue;
		left = Math.min(left, b.left);
		top = Math.min(top, b.top);
		right = Math.max(right, b.right);
		bottom = Math.max(bottom, b.bottom);
	}
	return Number.isFinite(left) ? { left, top, right, bottom } : null;
}

function markBox(ps, drawn) {
	const rect = marksRect(ps, drawn);
	if (!rect) return null;
	const { left, top, right, bottom } = rect;
	const r = ps.surface.getBoundingClientRect();
	const pad = 4;
	return {
		left: `${Math.round(left - r.left - pad)}px`,
		top: `${Math.round(top - r.top - pad)}px`,
		width: `${Math.round(right - left + 2 * pad)}px`,
		height: `${Math.round(bottom - top + 2 * pad)}px`
	};
}

function slotBox(state, ps, drawn) {
	const n = count(ps);
	if (n <= 0) return null;
	if (state.options.orientation === 'rows') {
		return { left: '0%', width: '100%', top: percent(drawn / n), height: percent(1 / n) };
	}
	if (state.options.placed && ps.geo) {
		// A column at its own place reaches halfway to each neighbour, and as far again past the ends.
		const x = ps.geo.x[drawn];
		const before = drawn > 0 ? x - ps.geo.x[drawn - 1] : NaN;
		const after = drawn < n - 1 ? ps.geo.x[drawn + 1] - x : NaN;
		const half = Math.min(Number.isFinite(before) ? before : Infinity, Number.isFinite(after) ? after : Infinity) / 2;
		if (Number.isFinite(x) && Number.isFinite(half)) {
			return { left: percent(x - half), width: percent(2 * half), top: '0%', height: '100%' };
		}
	}
	return { left: percent(drawn / n), width: percent(1 / n), top: '0%', height: '100%' };
}

// Highlight (I2B): the legend, a line near the pointer, or Up and Down dim every other series.

function highlight(state, key, by) {
	if (key === null && state.highlightBy !== by) return;
	if (state.highlight === key) return;
	state.highlightBy = key === null ? null : by;
	applyHighlight(state, key);
	if (state.options.sync) {
		for (const member of members(state)) if (member !== state && member.options.highlight) applyHighlight(member, key);
	}
}

function applyHighlight(state, key) {
	// Every panel's rows are watched from here on, so rows that scroll in later dim too.
	if (key !== null) for (const surface of surfacesOf(state)) panelFromSurface(state, surface);
	state.highlight = key;
	if (key === null) state.root.removeAttribute('data-moka-hl');
	else state.root.setAttribute('data-moka-hl', key);
	for (const el of state.root.querySelectorAll('[data-moka-key], [data-moka-legend-key]')) {
		if (owner(el) !== state.root) continue;
		const own = el.getAttribute('data-moka-key') ?? el.getAttribute('data-moka-legend-key');
		el.toggleAttribute('data-moka-dim', key !== null && own !== key && el.getAttribute('data-moka-with') !== key);
	}
	for (const ps of state.panels.values()) ps.dimKey = key;
}

// The tooltip and the pin card

function show(el) {
	el.setAttribute('data-moka-shown', '');
	if (supportsPopover && el.hasAttribute('popover') && !el.matches(':popover-open')) {
		try {
			el.showPopover();
		} catch {
			// Not in the document any more.
		}
	}
	const observer = sizer();
	if (!sized.has(el)) {
		sized.set(el, () => {
			const state = roots.get(owner(el));
			const surface = el.parentElement;
			const ps = state && surface ? panelFromSurface(state, surface) : null;
			if (!ps || !el.hasAttribute('data-moka-shown')) return;
			if (el.classList.contains('moka-chart-pin')) placePin(state, ps);
			else placeTip(state, ps);
		});
		observer.observe(el);
	}
}

function hide(el) {
	el.removeAttribute('data-moka-shown');
	el.removeAttribute('data-moka-flag');
	if (supportsPopover && el.matches(':popover-open')) {
		try {
			el.hidePopover();
		} catch {
			// Already gone.
		}
	}
}

function showTip(state, ps) {
	const tip = part(ps, 'tip');
	if (!tip) return;
	const a = ps.active;
	const o = state.options;
	const pinnedHere = o.pin && ps.pin >= 0 && a && ps.pin === a.drawn;
	if (!a || a.via === 'sync' || o.mode === 'readout' || o.mode === 'none' || pinnedHere || ps.preview) {
		hide(tip);
		return;
	}
	if (!tip.hasAttribute('data-moka-shown') && tip.getAttribute('data-moka-for') !== String(a.drawn)) return;
	show(tip);
	placed.add(state);
	placeTip(state, ps);
}

function showPin(state, ps) {
	const card = part(ps, 'pin');
	if (!card) return;
	if (ps.pin < 0 || card.getAttribute('data-moka-for') !== String(ps.pin) || ps.preview) {
		hide(card);
		return;
	}
	card.toggleAttribute('data-moka-flag', !!ps.active && ps.active.drawn !== ps.pin);
	show(card);
	placed.add(state);
	placePin(state, ps);
}

function placeTip(state, ps) {
	const tip = part(ps, 'tip');
	const a = ps.active;
	if (!tip || !a || !tip.hasAttribute('data-moka-shown')) return;
	const o = state.options;
	const r = ps.surface.getBoundingClientRect();
	const card = part(ps, 'pin');
	const cardBottom = card?.hasAttribute('data-moka-shown') ? card.getBoundingClientRect().bottom - r.top + 6 : 0;

	if (o.hit) {
		const rect = a.via === 'pointer' && a.px !== null ? null : marksRect(ps, a.drawn);
		const x = rect ? (rect.left + rect.right) / 2 : r.left + (a.px ?? r.width / 2);
		const y = rect ? (rect.top + rect.bottom) / 2 : r.top + (a.py ?? r.height / 2);
		if (!place(tip, r, x, y, 'side', cardBottom)) hide(tip);
		return;
	}

	if (o.find === 'column' && o.orientation !== 'rows') {
		const marks = marksOf(ps, a.drawn);
		let top = Infinity;
		for (const el of marks) {
			const b = el.getBoundingClientRect();
			if (b.width || b.height) top = Math.min(top, b.top);
		}
		const share = shareOf(ps, a.drawn, true);
		const x = r.left + (Number.isFinite(share) ? share : 0.5) * r.width;
		const y = Number.isFinite(top) ? top : r.top + (a.py ?? r.height / 2);
		if (!place(tip, r, x, y, 'above', cardBottom)) hide(tip);
		return;
	}

	if (o.orientation === 'rows') {
		const n = count(ps);
		const y = r.top + (n > 0 ? (a.drawn + 0.5) / n : 0.5) * r.height;
		const x = r.left + (a.via === 'pointer' && a.px !== null ? a.px : r.width * 0.5);
		if (!place(tip, r, x, y, 'side', 0)) hide(tip);
		return;
	}

	const share = shareOf(ps, a.drawn, false);
	const x = r.left + (Number.isFinite(share) ? share : 0.5) * r.width;
	let y = r.top + (a.py ?? r.height / 2);
	if (a.via !== 'pointer') {
		const focus = a.series >= 0 ? yOf(ps, a.series, a.drawn) : NaN;
		y = r.top + (Number.isFinite(focus) ? 1 - focus : 0.5) * r.height;
	}
	if (!place(tip, r, x, y, 'side', cardBottom)) hide(tip);
}

function placePin(state, ps) {
	const card = part(ps, 'pin');
	if (!card || ps.pin < 0 || !card.hasAttribute('data-moka-shown')) return;
	const r = ps.surface.getBoundingClientRect();
	if (state.options.orientation === 'rows') {
		const n = count(ps);
		const shown = rowsInSight(ps, r);
		const y = r.top + (n > 0 ? (ps.pin + 0.5) / n : 0.5) * r.height;
		if (!shown || y < shown.top || y > shown.bottom || !place(card, shown, shown.right, y, 'row', 0)) hide(card);
		placeTip(state, ps);
		return;
	}
	const columns = state.options.find === 'column';
	const rect = state.options.hit ? marksRect(ps, ps.pin) : null;
	const share = shareOf(ps, ps.pin, columns);
	const x = rect ? (rect.left + rect.right) / 2 : r.left + (Number.isFinite(share) ? share : 0.5) * r.width;
	if (!place(card, r, x, r.top, 'pin', 0)) hide(card);
	placeTip(state, ps);
}

// The part of a row plot in sight: the surface spans every row, but a scroller shows only some of them, under its
// sticky value axis.
function rowsInSight(ps, r) {
	const scroller = rowScroller(ps);
	if (!scroller) return r;
	const box = scroller.getBoundingClientRect();
	const top = Math.max(r.top, box.top + stickyHead(scroller, box));
	const bottom = Math.min(r.bottom, box.bottom);
	return bottom > top ? { left: r.left, right: r.right, top, bottom, width: r.width, height: bottom - top } : null;
}

function rowScroller(ps) {
	for (let node = ps.surface?.parentElement; node; node = node.parentElement) {
		const style = getComputedStyle(node);
		if (/(auto|scroll)/.test(style.overflowY) && node.scrollHeight > node.clientHeight) return node;
	}
	return null;
}

// How much of a scroller's top its sticky row header covers.
function stickyHead(scroller, box) {
	const head = scroller.querySelector('.moka-chart-plot__rhead');
	if (!head || getComputedStyle(head).position !== 'sticky') return 0;
	return Math.max(0, head.getBoundingClientRect().bottom - box.top);
}

// Places a card in window coordinates, inside the window and, where it fits, inside the plot. The top layer takes them
// as they are; without the Popover API the card is absolute in the surface, so they are moved there. A fixed app bar
// reports its height as --moka-appbar-space, which counts as out of sight. Answers false when the plot or the point is
// out of sight, and the caller puts the card away until it comes back.
function place(el, r, ax, ay, kind, minTop) {
	const vw = document.documentElement.clientWidth || window.innerWidth;
	const vh = document.documentElement.clientHeight || window.innerHeight;
	const bar = parseFloat(getComputedStyle(document.documentElement).getPropertyValue('--moka-appbar-space')) || 0;
	if (r.bottom <= bar || r.top >= vh || r.right <= 0 || r.left >= vw) return false;
	if (kind !== 'pin' && (ay < bar || ay > vh || ax < 0 || ax > vw)) return false;
	const w = el.offsetWidth;
	const h = el.offsetHeight;
	let left;
	let top;

	if (kind === 'above') {
		left = ax - w / 2;
		if (w <= r.width) left = Math.min(Math.max(left, r.left), r.right - w);
		top = ay - h - TIP_GAP;
		if (top < bar + WINDOW_MARGIN) top = ay + TIP_GAP;
		if (minTop > 0 && top < r.top + minTop && top + h > r.top) top = r.top + minTop;
	} else if (kind === 'pin') {
		left = ax - w / 2;
		top = Math.max(ay, bar) + 4;
		if (left < r.left) left = Math.min(r.left, r.right - w);
		if (left + w > r.right) left = Math.max(r.left, r.right - w);
	} else if (kind === 'row') {
		left = ax - w;
		top = ay - h / 2;
		if (h <= r.bottom - r.top) top = Math.min(Math.max(top, r.top), r.bottom - h);
	} else {
		left = ax + TIP_GAP;
		if (left + w > Math.min(r.right, vw - WINDOW_MARGIN) && ax - TIP_GAP - w >= Math.max(r.left, WINDOW_MARGIN)) {
			left = ax - TIP_GAP - w;
		}
		const low = r.top + minTop;
		top = ay - h / 2;
		if (h <= r.bottom - low) top = Math.min(Math.max(top, low), r.bottom - h);
	}

	left = Math.max(WINDOW_MARGIN, Math.min(left, vw - w - WINDOW_MARGIN));
	top = Math.max(bar + WINDOW_MARGIN, Math.min(top, vh - h - WINDOW_MARGIN));

	if (!(supportsPopover && el.matches(':popover-open'))) {
		left -= r.left;
		top -= r.top;
	}
	el.style.left = `${Math.round(left)}px`;
	el.style.top = `${Math.round(top)}px`;
	return true;
}

function togglePin(state, ps, drawn) {
	const next = ps.pin === drawn ? -1 : drawn;
	ps.pin = next;
	paint(state, ps);
	showTip(state, ps);
	showPin(state, ps);
	call(state, 'OnPin', ps.panel, next);
}

// .NET rendered: a new geometry version, or tooltip content for a point.

function mutated(state, records) {
	for (const record of records) {
		const target = record.target;
		if (!(target instanceof Element)) continue;
		if (record.attributeName === 'data-moka-version') {
			if (!target.classList.contains('moka-chart-surface') || owner(target) !== state.root) continue;
			const ps = panelFromSurface(state, target);
			if (ps) versionMoved(state, ps);
		} else if (record.attributeName === 'data-moka-for') {
			const surface = target.parentElement;
			if (!surface?.classList.contains('moka-chart-surface') || owner(surface) !== state.root) continue;
			const ps = panelFromSurface(state, surface);
			if (!ps) continue;
			if (target.classList.contains('moka-chart-pin')) showPin(state, ps);
			else showTip(state, ps);
		}
	}
}

function versionMoved(state, ps) {
	endPreview(ps);
	measure(state);
	if (!ps.active && !(ps.pin >= 0) && ps.syncWant === undefined && !ps.lastPointer) {
		ps.geo = null;
		ps.missing = false;
		return;
	}
	const geo = geometry(state, ps);
	if (geo !== undefined) arrived(state, ps);
}

// Keyboard (I4): the grid takes the keys; Left and Right step points, Up and Down change series or rows.

function focusIn(state, e) {
	if (state.options.hold && !state.focused) {
		state.focused = true;
		setHolding(state, true);
	}

	const grid = e.target instanceof Element ? e.target.closest('.moka-chart-surface__grid') : null;
	if (!grid || e.target !== grid) return;
	const ps = panelFromSurface(state, surfaceFrom(state, grid));
	if (!ps) return;
	if (performance.now() - state.pointerDownAt < CLICK_FOCUS) return;
	focusFirst(state, ps);
}

function focusFirst(state, ps) {
	// A listed layout is pulled as the keys arrive, so the first arrow rarely waits for it.
	if (state.options.hit) layoutOf(state, ps);
	if (ps.active && ps.active.via !== 'sync') {
		setActive(state, ps, ps.active.series, ps.active.drawn, 'key');
		return;
	}
	if (needsGeometry(state) && geometry(state, ps) === undefined) {
		ps.pendingFocus = true;
		return;
	}
	const first = visibleSeries(ps)[0];
	const rows = state.options.orientation === 'rows';
	const columns = state.options.find === 'column' || rows;
	const seriesIndex = columns ? -1 : first ? first.index : 0;
	const drawn = rows || state.options.hit ? Math.min(0, count(ps) - 1) : columns ? count(ps) - 1 : latest(ps, seriesIndex);
	if (drawn >= 0) setActive(state, ps, seriesIndex, drawn, 'key');
	if (rows && drawn >= 0) reveal(ps, drawn, count(ps));
}

// Points along lines need where they are drawn; bars in even slots and rows don't, since a slot is its index. Bars at
// their own places, such as at their times, do.
function needsGeometry(state) {
	const o = state.options;
	return o.orientation === 'columns' && !o.hit && (o.find !== 'column' || o.placed);
}

function focusOut(state, e) {
	const to = e.relatedTarget;
	if (state.options.hold && state.focused && !(to instanceof Node && state.root.contains(to))) {
		state.focused = false;
		setHolding(state, state.inside);
	}

	const grid = e.target instanceof Element ? e.target.closest('.moka-chart-surface__grid') : null;
	if (!grid) return;
	const surface = surfaceFrom(state, grid);
	if (to instanceof Node && surface?.contains(to)) return;
	const ps = panelFromSurface(state, surface);
	if (ps?.active?.via === 'key') clear(state, ps);
	if (state.highlightBy === 'key') highlight(state, null, 'key');
}

function keyDown(state, e) {
	const grid = e.target instanceof Element ? e.target.closest('.moka-chart-surface__grid') : null;
	if (!grid || e.target !== grid || e.isComposing) return;
	const surface = surfaceFrom(state, grid);
	const ps = panelFromSurface(state, surface);
	if (!ps) return;
	const o = state.options;

	if ((o.zoom & WHEEL) && !e.ctrlKey && !e.metaKey && !e.altKey && zoomKey(state, ps, e.key)) {
		e.preventDefault();
		return;
	}
	if (o.hit && gridKey(state, ps, e)) return;
	if (e.ctrlKey || e.metaKey || e.altKey) return;

	if (e.key === 'Escape') {
		if (ps.pin >= 0) {
			togglePin(state, ps, ps.pin);
		} else if (ps.active) {
			clear(state, ps);
		} else {
			return;
		}
		e.preventDefault();
		e.stopPropagation();
		return;
	}

	if (needsGeometry(state) && geometry(state, ps) === undefined) {
		if (isNavigation(e.key)) e.preventDefault();
		return;
	}

	const rows = o.orientation === 'rows';
	const columns = o.find === 'column' || rows;
	const n = count(ps);
	if (n <= 0) return;
	const visible = visibleSeries(ps);
	const a = ps.active;
	let seriesIndex = columns ? -1 : a && a.series >= 0 && visible.some(s => s.index === a.series) ? a.series : visible[0]?.index ?? 0;
	let drawn = a ? a.drawn : columns ? n - 1 : latest(ps, seriesIndex);
	const page = o.pageStep > 0 ? o.pageStep : Math.max(2, Math.round(n / 10));
	// Nearest points step along their own series, so a scatter's arrows never land on another series' dot.
	const within = o.find === 'nearest' && !columns && !!ps.geo;
	const step = delta => (columns || !ps.geo ? clampIndex(drawn + delta, n)
		: within ? stepWithin(ps, drawn, delta, seriesIndex) : stepOrdered(ps, drawn, delta));
	const along = rows ? ['ArrowUp', 'ArrowDown'] : ['ArrowLeft', 'ArrowRight'];

	switch (e.key) {
		case along[1]:
			drawn = step(1);
			break;
		case along[0]:
			drawn = step(-1);
			break;
		case 'Home':
			drawn = columns || !ps.geo ? 0 : within ? stepWithin(ps, drawn, -n, seriesIndex) : ps.geo.order[0] ?? 0;
			break;
		case 'End':
			drawn = columns || !ps.geo ? n - 1
				: within ? stepWithin(ps, drawn, n, seriesIndex) : ps.geo.order[ps.geo.order.length - 1] ?? n - 1;
			break;
		case 'PageUp':
			drawn = step(-page);
			break;
		case 'PageDown':
			drawn = step(page);
			break;
		case 'ArrowUp':
		case 'ArrowDown': {
			if (columns) return;
			const at = visible.findIndex(s => s.index === seriesIndex);
			const next = visible[Math.max(0, Math.min(visible.length - 1, at + (e.key === 'ArrowDown' ? 1 : -1)))];
			if (next) seriesIndex = next.index;
			if (o.highlight && next) highlight(state, next.key, 'key');
			if (within && next) drawn = closestIn(ps, seriesIndex, ps.geo.x[drawn]);
			break;
		}
		case 'Enter':
		case ' ':
			if (!a || (!o.pin && !o.select)) return;
			e.preventDefault();
			if (o.pin) togglePin(state, ps, a.drawn);
			if (o.select && e.key === 'Enter') call(state, 'OnSelect', ps.panel, a.series, a.drawn);
			return;
		default:
			return;
	}

	e.preventDefault();
	setActive(state, ps, seriesIndex, drawn, 'key');
	if (rows) reveal(ps, drawn, n);
}

// Parts in rows (a heatmap's cells, a timeline's spans) step by row and by place along the row: Left and Right along it,
// Up and Down between rows to the part over the same place, Home and End to its ends, Control with them to the first and
// last part. A regular layout is worked out from the surface's attributes; a listed one is pulled once a version, and a
// key pressed while it is on its way is played once it arrives. Returns whether it took the key.
function gridKey(state, ps, e) {
	if (!isNavigation(e.key) || e.altKey || ((e.ctrlKey || e.metaKey) && e.key !== 'Home' && e.key !== 'End')) return false;
	const layout = layoutOf(state, ps);
	if (layout === null) return false;
	e.preventDefault();
	const ctrl = e.ctrlKey || e.metaKey;
	if (layout === undefined) {
		ps.keyWaiting = { key: e.key, ctrl };
		return true;
	}
	moveInGrid(state, ps, layout, e.key, ctrl);
	return true;
}

function moveInGrid(state, ps, layout, key, ctrl) {
	const from = ps.active && ps.active.via !== 'sync' ? ps.active.drawn : -1;
	const drawn = stepParts(layout, from, key, ctrl, from >= 0 && turned(ps, from), state.options.pageStep);
	if (drawn < 0) return;
	setActive(state, ps, -1, drawn, 'key');
	revealPart(state, ps, layout, drawn);
}

// Whether the chart drew its grid on its side, as a heatmap narrower than its columns does: its parts say so.
function turned(ps, drawn) {
	const mark = marksOf(ps, drawn)[0];
	return !!mark && getComputedStyle(mark).getPropertyValue('--moka-chart-transpose').trim() === '1';
}

// The surface's own layout of a regular grid, or a listed layout pulled once a geometry version. Undefined while one is
// on its way, null when the chart's parts aren't in rows.
function layoutOf(state, ps) {
	const surface = ps.surface;
	const kind = surface?.dataset.mokaOrder;
	if (!kind) return null;
	if (kind !== 'listed') {
		return {
			kind,
			rows: Math.max(0, Number(surface.dataset.mokaRows) || 0),
			cols: Math.max(0, Number(surface.dataset.mokaCols) || 0),
			count: count(ps),
			flow: surface.hasAttribute('data-moka-flow')
		};
	}

	const current = version(ps);
	if ((ps.layout || ps.layoutMissing) && ps.layoutVersion >= current) return ps.layout;
	if (ps.layoutPulling) return undefined;
	ps.layoutPulling = true;
	Promise.resolve(state.ref.invokeMethodAsync('HoverGeometry', ps.panel, current))
		.then(answer => {
			ps.layoutPulling = false;
			if (!roots.has(state.root)) return;
			const decoded = decodeGeometry(answer);
			ps.layout = decoded?.kind === 'listed' ? decoded : null;
			ps.layoutMissing = !ps.layout;
			ps.layoutVersion = ps.layout ? Math.max(current, decoded.version) : current;
			const waiting = ps.keyWaiting;
			ps.keyWaiting = null;
			if (waiting && ps.layout) moveInGrid(state, ps, ps.layout, waiting.key, waiting.ctrl);
		})
		.catch(() => {
			ps.layoutPulling = false;
			ps.keyWaiting = null;
		});
	return undefined;
}

// A part the keys reached comes into view inside whatever scrolls it within the chart, never the page. One in a row
// that isn't drawn yet scrolls its row in by where the row sits, and lifts once Blazor draws it.
function revealPart(state, ps, layout, drawn) {
	const mark = marksOf(ps, drawn)[0];
	if (mark) {
		scrollWithin(state, mark);
	} else if (layout.kind === 'listed' && layout.rows > 0) {
		reveal(ps, placeOf(layout, drawn).row, layout.rows);
	}
}

function scrollWithin(state, mark) {
	for (let node = mark.parentElement; node && node !== state.root; node = node.parentElement) {
		const style = getComputedStyle(node);
		const scrollsDown = /(auto|scroll)/.test(style.overflowY) && node.scrollHeight > node.clientHeight;
		const scrollsAcross = /(auto|scroll)/.test(style.overflowX) && node.scrollWidth > node.clientWidth;
		if (!scrollsDown && !scrollsAcross) continue;
		const box = node.getBoundingClientRect();
		const b = mark.getBoundingClientRect();
		if (scrollsDown) {
			const top = box.top + stickyHead(node, box);
			if (b.top < top) node.scrollTop -= top - b.top;
			else if (b.bottom > box.top + node.clientHeight) node.scrollTop += b.bottom - box.top - node.clientHeight;
		}
		if (scrollsAcross) {
			const left = box.left + stickyStart(node, box);
			if (b.left < left) node.scrollLeft -= left - b.left;
			else if (b.right > box.left + node.clientWidth) node.scrollLeft += b.right - box.left - node.clientWidth;
		}
	}
}

// How much of a scroller's start the names that stick to it cover, such as a heatmap grid's row names.
function stickyStart(scroller, box) {
	const name = scroller.querySelector('[data-moka-sticky-start]');
	if (!name || getComputedStyle(name).position !== 'sticky') return 0;
	return Math.max(0, name.getBoundingClientRect().right - box.left);
}

function isNavigation(key) {
	return ['ArrowLeft', 'ArrowRight', 'ArrowUp', 'ArrowDown', 'Home', 'End', 'PageUp', 'PageDown'].includes(key);
}

function clampIndex(i, n) {
	return Math.max(0, Math.min(n - 1, i));
}

function stepOrdered(ps, drawn, delta) {
	const order = ps.geo.order;
	if (!order.length) return drawn;
	let at = lowerBound(ps.geo, order, ps.geo.x[drawn]);
	if (order[at] !== drawn) at = Math.min(order.length - 1, at);
	return order[Math.max(0, Math.min(order.length - 1, at + delta))];
}

// Steps |delta| points of one series along x, skipping the points of the others; stays put at either end.
function stepWithin(ps, drawn, delta, seriesIndex) {
	const order = ps.geo.order;
	if (!order.length) return drawn;
	let at = lowerBound(ps.geo, order, ps.geo.x[drawn]);
	while (at < order.length - 1 && order[at] !== drawn && ps.geo.x[order[at]] === ps.geo.x[drawn]) at++;
	if (order[at] !== drawn) return closestIn(ps, seriesIndex, ps.geo.x[drawn]);
	const way = delta < 0 ? -1 : 1;
	let found = drawn;
	for (let left = Math.abs(delta), k = at + way; left > 0 && k >= 0 && k < order.length; k += way) {
		if (Number.isFinite(yOf(ps, seriesIndex, order[k]))) {
			found = order[k];
			left--;
		}
	}
	return found;
}

// The point of one series nearest an x share, for moving the keyboard to another series.
function closestIn(ps, seriesIndex, share) {
	const order = ps.geo.order;
	let best = -1;
	let distance = Infinity;
	for (const i of order) {
		if (!Number.isFinite(yOf(ps, seriesIndex, i))) continue;
		const d = Math.abs(ps.geo.x[i] - (Number.isFinite(share) ? share : 0));
		if (d < distance) {
			distance = d;
			best = i;
		}
	}
	return best < 0 ? 0 : best;
}

// A row out of sight scrolls into view inside whatever scrolls the rows, never the page.
function reveal(ps, drawn, n) {
	const r = ps.surface.getBoundingClientRect();
	const top = r.top + (drawn / n) * r.height;
	const bottom = r.top + ((drawn + 1) / n) * r.height;
	const node = rowScroller(ps);
	if (!node) return;
	const box = node.getBoundingClientRect();
	const clear = box.top + stickyHead(node, box);
	if (top < clear) node.scrollTop -= clear - top;
	else if (bottom > box.bottom) node.scrollTop += bottom - box.bottom;
}

// Sync groups (I6D, I7D): charts in a group, and the panels of one chart, share the crosshair by x value.

function joinGroup(state, name) {
	let set = groups.get(name);
	if (!set) groups.set(name, set = new Set());
	set.add(state);
	state.group = name;
}

function leaveGroup(state) {
	if (!state.group) return;
	const set = groups.get(state.group);
	set?.delete(state);
	if (set && !set.size) groups.delete(state.group);
	state.group = null;
}

function members(state) {
	const set = state.group ? groups.get(state.group) : null;
	if (!set) return [];
	for (const member of [...set]) {
		if (!member.root.isConnected || !roots.has(member.root)) set.delete(member);
	}
	return [...set];
}

function xValue(state, ps, drawn) {
	if (drawn < 0) return null;
	const o = state.options;
	const slots = (o.find === 'column' && !o.placed) || o.orientation === 'rows';
	if (slots || !ps.geo) return { index: drawn };
	const shown = viewWindow(ps);
	const share = ps.geo.x[drawn];
	if (!shown || !Number.isFinite(share)) return { index: drawn };
	return { value: shown[0] + share * (shown[1] - shown[0]) };
}

function spread(state, ps, drawn) {
	const at = xValue(state, ps, drawn);
	for (const other of surfacesOf(state)) {
		const ops = panelFromSurface(state, other);
		if (ops && ops !== ps) syncTo(state, ops, at);
	}
	if (!state.options.sync) return;
	for (const member of members(state)) {
		if (member === state) continue;
		for (const surface of surfacesOf(member)) {
			const mps = panelFromSurface(member, surface);
			if (mps) syncTo(member, mps, at);
		}
	}
}

function syncTo(state, ps, at) {
	if (!at) {
		if (ps.active?.via === 'sync') clear(state, ps, true);
		return;
	}
	if (ps.active && ps.active.via !== 'sync') return;
	if (state.options.mode === 'none' && !state.options.keys) return;

	let drawn;
	if (at.value === undefined) {
		drawn = clampIndex(at.index, count(ps));
	} else {
		const geo = geometry(state, ps);
		if (geo === undefined) {
			ps.syncWant = at;
			return;
		}
		drawn = nearestByValue(ps, at.value);
	}
	if (drawn < 0) {
		if (ps.active?.via === 'sync') clear(state, ps, true);
		return;
	}
	setActive(state, ps, -1, drawn, 'sync');
}

// Live hold (I7A)

function setHolding(state, holding, force) {
	if (!state.options.hold && !force) return;
	if (state.holding === holding) return;
	state.holding = holding;
	state.root.toggleAttribute('data-moka-holding', holding);
	call(state, 'OnHold', holding);
}

// Plot width, for charts that draw fewer points than they have

function measure(state) {
	const surface = state.options.measure ? surfacesOf(state).find(s => s.hasAttribute('data-moka-measure')) : null;
	if (state.measured === surface) return;
	if (state.measured) {
		sizes?.unobserve(state.measured);
		sized.delete(state.measured);
	}
	state.measured = surface || null;
	if (!surface) return;
	sized.set(surface, entry => {
		const width = entry.contentRect.width;
		if (!width) return;
		const bucket = Math.ceil(width / WIDTH_STEP) * WIDTH_STEP;
		if (bucket === state.bucket) return;
		state.bucket = bucket;
		call(state, 'OnPlotWidth', bucket);
	});
	sizer().observe(surface);
}

// Zoom (I3A, I3D): a brush, Ctrl and the wheel, a pinch, a drag to pan, and + - 0. A gesture is previewed by
// stretching the marks, and .NET hears the span once it stops.

function minSpan(ps) {
	const span = extent(ps);
	const shown = viewWindow(ps);
	if (ps.geo && shown && ps.geo.order.length > 1) {
		const finite = ps.geo.order.length;
		return Math.max(4 * (shown[1] - shown[0]) / (finite - 1), span ? (span[1] - span[0]) * 1e-6 : 0);
	}
	return span ? (span[1] - span[0]) / 1000 : 0;
}

function fit(ps, from, to) {
	const span = extent(ps);
	if (!span || !Number.isFinite(from) || !Number.isFinite(to)) return null;
	if (to < from) [from, to] = [to, from];
	const least = minSpan(ps);
	if (to - from < least) {
		const mid = (from + to) / 2;
		from = mid - least / 2;
		to = mid + least / 2;
	}
	const whole = span[1] - span[0];
	if (to - from >= whole * (1 - 1e-9)) return null;
	if (from < span[0]) {
		to += span[0] - from;
		from = span[0];
	}
	if (to > span[1]) {
		from -= to - span[1];
		to = span[1];
	}
	return [from, to];
}

function marksToStretch(ps) {
	const box = ps.surface?.parentElement;
	if (!box) return [];
	return [...box.children].filter(el => el !== ps.surface && (el instanceof SVGElement || el.hasAttribute('data-moka-marks')));
}

function preview(state, ps, range, linked) {
	if (!linked) linkPreview(state, range);
	const shown = viewWindow(ps);
	if (!shown) return;
	const [from, to] = range || extent(ps) || shown;
	ps.preview = [from, to];
	clear(state, ps);
	showPin(state, ps);
	const scale = (shown[1] - shown[0]) / (to - from);
	const shift = (from - shown[0]) / (shown[1] - shown[0]);
	const box = ps.surface.parentElement;
	if (box && ps.overflow === undefined) {
		ps.overflow = box.style.overflow;
		box.style.overflow = 'clip';
	}
	for (const el of marksToStretch(ps)) {
		el.style.transformOrigin = '0 0';
		el.style.transform = `scaleX(${scale}) translateX(${(-shift * 100).toFixed(4)}%)`;
	}
}

function endPreview(ps) {
	if (!ps.preview && ps.overflow === undefined) return;
	clearTimeout(ps.previewTimer);
	ps.preview = null;
	for (const el of marksToStretch(ps)) {
		el.style.removeProperty('transform');
		el.style.removeProperty('transform-origin');
	}
	const box = ps.surface?.parentElement;
	if (box && ps.overflow !== undefined) {
		box.style.overflow = ps.overflow;
		if (!box.getAttribute('style')) box.removeAttribute('style');
	}
	ps.overflow = undefined;
}

function commit(state, ps, range, delay) {
	clearTimeout(ps.commitTimer);
	if (range) preview(state, ps, range);
	else if (ps.zoomed) preview(state, ps, null);
	else linkPreview(state, null);
	linkCommit(state, range, delay);
	ps.commitTimer = setTimeout(() => {
		call(state, 'OnRange', range ? range[0] : null, range ? range[1] : null);
		clearTimeout(ps.previewTimer);
		ps.previewTimer = setTimeout(() => endPreview(ps), PREVIEW_LIMIT);
	}, delay ?? COMMIT_DELAY);
}

// The charts whose zoom follows this one's: the same sync group, the same link key, which says their x axes count alike.
function linked(state) {
	const key = state.options.link;
	if (!key || !state.options.sync) return [];
	return members(state).filter(member => member !== state && member.options.link === key);
}

// Linked charts stretch their marks with this one, so a pan or a pinch moves them all together without .NET.
function linkPreview(state, range) {
	for (const member of linked(state)) {
		for (const surface of surfacesOf(member)) {
			const mps = panelFromSurface(member, surface);
			if (mps && !mps.gesture) preview(member, mps, range, true);
		}
	}
}

// Once the gesture is let go, each linked chart hears the range through its own OnRange, once, and its preview stays
// until its new geometry arrives.
function linkCommit(state, range, delay) {
	for (const member of linked(state)) {
		clearTimeout(member.linkTimer);
		member.linkTimer = setTimeout(() => {
			if (!roots.has(member.root)) return;
			call(member, 'OnRange', range ? range[0] : null, range ? range[1] : null);
			for (const mps of member.panels.values()) {
				clearTimeout(mps.previewTimer);
				mps.previewTimer = setTimeout(() => endPreview(mps), PREVIEW_LIMIT);
			}
		}, delay ?? COMMIT_DELAY);
	}
}

function zoomAround(state, ps, center, factor) {
	const current = ps.preview || viewWindow(ps);
	if (!current) return;
	const range = fit(ps, center - (center - current[0]) * factor, center + (current[1] - center) * factor);
	commit(state, ps, range);
}

function zoomKey(state, ps, key) {
	const current = ps.preview || viewWindow(ps);
	if (!current) return false;
	const share = ps.active ? shareOf(ps, ps.active.drawn, false) : 0.5;
	const center = current[0] + (Number.isFinite(share) ? share : 0.5) * (current[1] - current[0]);
	if (key === '+' || key === '=') zoomAround(state, ps, center, 1 / ZOOM_STEP);
	else if (key === '-' || key === '_') zoomAround(state, ps, center, ZOOM_STEP);
	else if (key === '0') commit(state, ps, null, 0);
	else return false;
	return true;
}

function wheel(state, e) {
	const surface = surfaceFrom(state, e.target);
	const ps = surface ? panelFromSurface(state, surface) : null;
	if (!ps) return;
	if (!(e.ctrlKey || e.metaKey)) {
		const hint = element(ps, 'hint', 'moka-chart-ov-hint');
		hint.textContent = state.options.words?.zoomHint || '';
		hint.hidden = false;
		hint.setAttribute('data-moka-shown', '');
		clearTimeout(ps.hintTimer);
		ps.hintTimer = setTimeout(() => hint.removeAttribute('data-moka-shown'), HINT_TIME);
		return;
	}
	e.preventDefault();
	const current = ps.preview || viewWindow(ps);
	if (!current) return;
	const at = local(ps, e.clientX, e.clientY);
	const pixels = e.deltaMode === 1 ? e.deltaY * 16 : e.deltaMode === 2 ? e.deltaY * 400 : e.deltaY;
	const factor = Math.min(2, Math.max(0.5, Math.exp(pixels * WHEEL_RATE)));
	const center = current[0] + Math.max(0, Math.min(1, at.sx)) * (current[1] - current[0]);
	zoomAround(state, ps, center, factor);
}

function startBrush(state, ps, e) {
	if (!viewWindow(ps)) return;
	const at = local(ps, e.clientX, e.clientY);
	ps.gesture = { kind: 'brush', id: e.pointerId, start: at.sx, end: at.sx, moved: false };
	brushing.add(state);
	capture(ps, e.pointerId);
}

function startPan(state, ps, e) {
	const current = ps.preview || viewWindow(ps);
	if (!current) return;
	ps.gesture = { kind: 'pan', id: e.pointerId, x: e.clientX, window: current.slice(), moved: false };
	capture(ps, e.pointerId);
}

function startPinch(state, ps) {
	const current = ps.preview || viewWindow(ps);
	if (!current) return;
	const [a, b] = [...ps.touches.values()];
	ps.gesture = { kind: 'pinch', id: -1, distance: Math.max(1, Math.abs(a.x - b.x)), mid: (a.x + b.x) / 2, window: current.slice(), moved: false };
	clear(state, ps);
}

function capture(ps, id) {
	try {
		ps.surface.setPointerCapture(id);
	} catch {
		// The pointer already went up.
	}
}

function moveGesture(state, ps, e) {
	const g = ps.gesture;
	if (g.kind === 'brush') {
		const at = local(ps, e.clientX, e.clientY);
		g.end = Math.max(0, Math.min(1, at.sx));
		g.moved ||= Math.abs(g.end - g.start) * at.r.width > BRUSH_MIN;
		if (!g.moved) return;
		clear(state, ps);
		const brush = element(ps, 'brush', 'moka-chart-ov-brush');
		brush.hidden = false;
		brush.style.left = percent(Math.min(g.start, g.end));
		brush.style.width = percent(Math.abs(g.end - g.start));
		return;
	}

	if (g.kind === 'pan') {
		const r = ps.surface.getBoundingClientRect();
		const dx = e.clientX - g.x;
		if (!g.moved && Math.abs(dx) > BRUSH_MIN) {
			g.moved = true;
			ps.surface.setAttribute('data-moka-panning', '');
			clear(state, ps);
		}
		if (!g.moved || !r.width) return;
		const span = extent(ps);
		const length = g.window[1] - g.window[0];
		let from = g.window[0] - (dx / r.width) * length;
		if (span) from = Math.max(span[0], Math.min(span[1] - length, from));
		g.range = [from, from + length];
		preview(state, ps, g.range);
		return;
	}

	if (g.kind === 'pinch') {
		ps.touches.set(e.pointerId, { x: e.clientX, y: e.clientY });
		const [a, b] = [...ps.touches.values()];
		if (!a || !b) return;
		const r = ps.surface.getBoundingClientRect();
		const factor = g.distance / Math.max(1, Math.abs(a.x - b.x));
		const share = r.width ? (g.mid - r.left) / r.width : 0.5;
		const center = g.window[0] + share * (g.window[1] - g.window[0]);
		g.range = fit(ps, center - (center - g.window[0]) * factor, center + (g.window[1] - center) * factor);
		g.moved = true;
		preview(state, ps, g.range);
	}
}

function endGesture(state, ps, cancelled) {
	const g = ps.gesture;
	ps.gesture = null;
	brushing.delete(state);
	ps.surface?.removeAttribute('data-moka-panning');
	const brush = ps.parts['ov-brush'];
	if (brush) brush.hidden = true;
	if (!g || cancelled || !g.moved) {
		if (cancelled) endPreview(ps);
		return;
	}

	ps.swallowClick = true;
	setTimeout(() => {
		ps.swallowClick = false;
	}, 0);

	if (g.kind === 'brush') {
		const shown = viewWindow(ps);
		if (!shown) return;
		const at = s => shown[0] + s * (shown[1] - shown[0]);
		commit(state, ps, fit(ps, at(Math.min(g.start, g.end)), at(Math.max(g.start, g.end))), 0);
		return;
	}
	commit(state, ps, g.range ?? null);
}

function cancelGesture(state, ps) {
	ps.gesture = null;
	ps.surface?.removeAttribute('data-moka-panning');
	const brush = ps.parts['ov-brush'];
	if (brush) brush.hidden = true;
	endPreview(ps);
	ps.swallowClick = true;
	setTimeout(() => {
		ps.swallowClick = false;
	}, 0);
}
