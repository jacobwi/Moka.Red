// MokaDockLayout's browser side. Everything the pointer does stays here: splitter drags, tab and group drags with their
// drop indicators, floating window moves and resizes with snapping. .NET hears once per gesture, when it ends. Keys
// that only move focus (F6, the tab strip's arrows, Tab in visual order) never reach .NET either.
//
// One set of listeners on the layout's root handles every gutter, tab, header and window inside it, so a render that
// adds or removes groups binds nothing. Attributes written here (data-moka-dock-narrow, data-moka-overflow, the
// gutters' aria values) are never rendered by .NET, so the two never fight.

import { keySplitter, resolveLength, startSplitterDrag, syncSplitterValues } from '../../Moka.Red.Core/moka-drag.js';

const states = new WeakMap();
const DRAG_START = 4;
const SNAP = 8;
const EDGE_ZONE = 28;
const HOVER_DELAY = 400;
const WINDOW_POLL = 500;
const CAPTURE_WAIT = 150;
const TABBABLE = 'a[href], button:not([disabled]), input:not([disabled]), select:not([disabled]), textarea:not([disabled]), [tabindex]:not([tabindex="-1"]), [contenteditable="true"]';
const supportsAnchors = typeof CSS !== 'undefined' && CSS.supports('anchor-name: --moka');

export function bind(root, ref, options) {
	if (!root) return;
	if (states.has(root)) {
		update(root, options);
		return;
	}

	const state = {
		root, ref, options: options || {}, narrow: null, drag: null, keyed: null, hover: 0,
		windows: new Map(), revealed: new WeakMap(), transitions: 0
	};
	states.set(root, state);

	state.listeners = [
		[root, 'pointerdown', e => onPointerDown(state, e)],
		[root, 'keydown', e => onKeyDown(state, e)],
		[root, 'keyup', e => onKeyUp(state, e)],
		[root, 'dblclick', e => onDoubleClick(state, e)],
		[root, 'focusin', e => onFocusIn(state, e)],
		[root, 'pointerover', e => onPointerOver(state, e)],
		[root, 'pointerout', e => onPointerOut(state, e)],
		[document, 'pointerdown', e => onDocumentDown(state, e), true]
	];
	for (const [target, type, handler, capture] of state.listeners) target.addEventListener(type, handler, capture === true);

	state.resize = new ResizeObserver(() => {
		checkNarrow(state);
		fitStrips(state);
		placeHosts(state);
	});
	state.resize.observe(root);
	state.mutations = new MutationObserver(() => {
		observeStrips(state);
		fitStrips(state);
		placeHosts(state);
	});
	state.mutations.observe(root, { childList: true, subtree: true, attributes: true, attributeFilter: ['hidden', 'style', 'class'] });
	observeStrips(state);
	fitStrips(state);
	placeHosts(state);
	checkNarrow(state);
	ref.invokeMethodAsync('OnPopOutSupport', typeof window.open === 'function');
}

export function update(root, options) {
	const state = states.get(root);
	if (!state) return;
	state.options = options || {};
	state.narrow = null;
	checkNarrow(state);
}

export function unbind(root) {
	const state = states.get(root);
	if (!state) return;
	for (const [target, type, handler, capture] of state.listeners) target.removeEventListener(type, handler, capture === true);
	state.resize.disconnect();
	state.mutations.disconnect();
	state.strips?.disconnect();
	clearTimeout(state.hover);
	endDrag(state, false);
	for (const entry of state.windows.values()) clearInterval(entry.poll);
	states.delete(root);
}

export function readStorage(key) {
	try {
		return localStorage.getItem(key);
	} catch {
		return null;
	}
}

export function writeStorage(key, value) {
	try {
		localStorage.setItem(key, value);
	} catch (e) {
		console.warn('MokaDockLayout: the layout could not be saved.', e);
	}
}

export function removeStorage(key) {
	try {
		localStorage.removeItem(key);
	} catch {
		// Private browsing or storage turned off: there was nothing to remove.
	}
}

// Applies a preset inside a view transition, so the groups glide to their new places. The update callback is .NET's
// apply, which settles once the page shows the new layout, so the browser takes its picture of the new state then.
// The update waits on no frame of its own: a hidden page never gets one, and the browser holds frames back while the
// update runs.
// Resolves once the preset is applied. Returns false, and .NET applies it at once, where there is no view
// transition, the page is hidden or the person asked for less motion.
export async function transition(root, ref, name) {
	const state = states.get(root);
	if (!state || typeof document.startViewTransition !== 'function' || document.visibilityState !== 'visible'
		|| window.matchMedia('(prefers-reduced-motion: reduce)').matches) {
		return false;
	}

	// The attribute names every group and host for the transition (the stylesheet), only while one runs, so the
	// layouts on a page never share a name outside it.
	state.transitions++;
	root.setAttribute('data-moka-dock-transition', '');
	let updating = false;
	const view = document.startViewTransition(() => {
		updating = true;
		return ref.invokeMethodAsync('ApplyPresetNowAsync', name);
	});
	const done = () => {
		if (--state.transitions === 0) root.removeAttribute('data-moka-dock-transition');
	};
	view.finished.then(done, done);
	// The browser skips a transition when another starts or the page is hidden, and ready rejects to say the
	// animation won't run. The preset still applies; a failure to apply it rejects updateCallbackDone, awaited here.
	view.ready.catch(() => {});
	// The browser calls the update once it has its picture of the old layout, at its next frame. A page that draws
	// no frames while it reports itself visible (a pane or window the app keeps out of sight) would never call it, so
	// a transition still waiting after CAPTURE_WAIT is skipped, which applies the preset at once without animating.
	const capture = setTimeout(() => {
		if (!updating) view.skipTransition();
	}, CAPTURE_WAIT);
	try {
		await view.updateCallbackDone;
	} finally {
		clearTimeout(capture);
	}
	return true;
}

// The size of the area floating windows sit in, so a window opened from code can fit it.
export function measureMain(root) {
	const main = root?.querySelector(':scope > .moka-dock-main');
	return main ? [main.clientWidth, main.clientHeight] : null;
}

// Opens a panel's window and watches it: when the person closes it, .NET takes the panel back. Returns false when the
// browser refused (a popup blocker) or has no windows.
export function openWindow(root, url, panel) {
	const state = states.get(root);
	if (!state || typeof window.open !== 'function') return false;

	const win = window.open(url, 'moka-dock-' + panel, 'popup=yes,width=760,height=560');
	if (!win) return false;

	const entry = { win, poll: 0 };
	entry.poll = setInterval(() => {
		if (!win.closed) return;
		clearInterval(entry.poll);
		state.windows.delete(panel);
		state.ref.invokeMethodAsync('OnWindowClosed', panel);
	}, WINDOW_POLL);
	state.windows.set(panel, entry);
	return true;
}

export function closeWindow(root, panel) {
	const entry = states.get(root)?.windows.get(panel);
	if (!entry) return;
	clearInterval(entry.poll);
	states.get(root).windows.delete(panel);
	if (!entry.win.closed) entry.win.close();
}

// The layout's own width against NarrowBelow, not the window's: a layout in a side panel narrows when its panel does.
function checkNarrow(state) {
	const below = state.options.narrowBelow;
	const limit = below ? resolveLength(below, state.root, true) : null;
	const narrow = limit !== null && state.root.clientWidth > 0 && state.root.clientWidth < limit;
	if (narrow === state.narrow) return;

	state.narrow = narrow;
	state.root.toggleAttribute('data-moka-dock-narrow', narrow);
	state.ref.invokeMethodAsync('OnNarrowChanged', narrow);
}

function observeStrips(state) {
	state.strips?.disconnect();
	state.strips = new ResizeObserver(() => fitStrips(state));
	for (const strip of state.root.querySelectorAll('[data-moka-dock-tabs]')) state.strips.observe(strip);
}

// A strip whose tabs don't all fit shows its All tabs button, and scrolls a tab that just became the one showing,
// such as one dropped into its group, into view. It scrolls only when the showing tab changes, so a strip the person
// scrolled keeps its place; a strip with no width yet waits until it has one.
function fitStrips(state) {
	for (const strip of state.root.querySelectorAll('[data-moka-dock-tabs]')) {
		strip.toggleAttribute('data-moka-overflow', strip.scrollWidth > strip.clientWidth + 1);
		const tab = strip.querySelector('[role="tab"][aria-selected="true"]');
		if (!tab || strip.clientWidth === 0 || state.revealed.get(strip) === tab) continue;
		state.revealed.set(strip, tab);
		const view = strip.getBoundingClientRect();
		const box = (tab.closest('.moka-dock-tab-wrap') ?? tab).getBoundingClientRect();
		if (box.left < view.left) strip.scrollLeft -= view.left - box.left;
		else if (box.right > view.right) strip.scrollLeft += box.right - view.right;
	}
}

// Each panel's host is drawn over its group's slot with CSS anchor positioning. Where the browser has none, the host
// is placed from the slot's box instead, again after every change to the layout.
function placeHosts(state) {
	if (supportsAnchors) return;

	const origin = state.root.getBoundingClientRect();
	for (const host of state.root.querySelectorAll('[data-moka-dock-anchor]')) {
		if (host.hidden) continue;
		const slot = state.root.querySelector(`[data-moka-dock-slot="${host.dataset.mokaDockAnchor}"]`);
		if (!slot) continue;
		const box = slot.getBoundingClientRect();
		host.style.left = (box.left - origin.left) + 'px';
		host.style.top = (box.top - origin.top) + 'px';
		host.style.width = box.width + 'px';
		host.style.height = box.height + 'px';
	}
}

function onPointerDown(state, e) {
	if (e.button !== 0) return;
	const target = e.target;
	bringToFront(state, target);

	const gutter = target.closest('[data-moka-dock-gutter]');
	if (gutter && state.root.contains(gutter)) {
		const split = gutter.closest('[data-moka-dock-split]');
		startSplitterDrag(split, gutter, e, splitterOptions(split, gutter),
			sizes => state.ref.invokeMethodAsync('OnSplitResized', split.dataset.mokaDockSplit, sizes));
		return;
	}

	const edge = target.closest('[data-moka-dock-resize]');
	if (edge) {
		startWindowDrag(state, edge.closest('[data-moka-dock-float]'), e, edge.dataset.mokaDockResize);
		return;
	}

	if (state.narrow || !state.options.drag) return;

	const tab = target.closest('[data-moka-dock-tab]');
	if (tab && !target.closest('.moka-dock-tab__close')) {
		armDrag(state, e, 'panel', tab.dataset.mokaDockTab, tab);
		return;
	}

	const grip = target.closest('[data-moka-dock-grip]');
	if (!grip || !grip.dataset.mokaDockGrip || target.closest('button, a, input, select, textarea, [role="tab"], [role="tablist"] > *')) return;

	const floating = grip.closest('[data-moka-dock-float]');
	if (floating) {
		startWindowDrag(state, floating, e, null);
		return;
	}

	armDrag(state, e, 'group', grip.dataset.mokaDockGrip, grip);
}

function splitterOptions(split, gutter) {
	const list = name => (split.dataset[name] || '').split('|').map(v => v || null);
	return {
		direction: split.dataset.mokaDockDir === 'row' ? 'horizontal' : 'vertical',
		index: parseInt(gutter.dataset.mokaDockGutter, 10),
		min: list('mokaDockMin'),
		max: list('mokaDockMax'),
		flex: (split.dataset.mokaDockFlex || '').split('|').map(v => v === '1')
	};
}

function onDoubleClick(state, e) {
	const gutter = e.target.closest('[data-moka-dock-gutter]');
	if (!gutter) return;
	const split = gutter.closest('[data-moka-dock-split]');
	state.ref.invokeMethodAsync('OnSplitReset', split.dataset.mokaDockSplit, parseInt(gutter.dataset.mokaDockGutter, 10));
}

// Moves (edges null) or resizes a floating window, snapping its edges to the layout's and to the other windows'.
function startWindowDrag(state, win, e, edges) {
	if (!win) return;
	e.preventDefault();

	const area = win.offsetParent ?? state.root;
	const bounds = { width: area.clientWidth, height: area.clientHeight };
	const start = { x: e.clientX, y: e.clientY, left: win.offsetLeft, top: win.offsetTop, width: win.offsetWidth, height: win.offsetHeight };
	const others = [...area.querySelectorAll('[data-moka-dock-float]')].filter(w => w !== win)
		.map(w => ({ left: w.offsetLeft, top: w.offsetTop, right: w.offsetLeft + w.offsetWidth, bottom: w.offsetTop + w.offsetHeight }));
	const xs = [0, bounds.width, ...others.flatMap(o => [o.left, o.right])];
	const ys = [0, bounds.height, ...others.flatMap(o => [o.top, o.bottom])];
	let moved = false;

	document.body.style.userSelect = 'none';

	function onMove(ev) {
		const dx = ev.clientX - start.x;
		const dy = ev.clientY - start.y;
		if (!moved && Math.hypot(dx, dy) < DRAG_START) return;
		moved = true;

		let { left, top, width, height } = start;
		if (!edges) {
			left = snap(start.left + dx, width, xs);
			top = snap(start.top + dy, height, ys);
		} else {
			if (edges.includes('e')) width = Math.max(160, snapEdge(start.left + start.width + dx, xs) - left);
			if (edges.includes('s')) height = Math.max(96, snapEdge(start.top + start.height + dy, ys) - top);
			if (edges.includes('w')) {
				left = Math.min(start.left + start.width - 160, snapEdge(start.left + dx, xs));
				width = start.left + start.width - left;
			}
			if (edges.includes('n')) {
				top = Math.min(start.top + start.height - 96, snapEdge(start.top + dy, ys));
				height = start.top + start.height - top;
			}
		}

		left = Math.max(0, Math.min(bounds.width - 64, left));
		top = Math.max(0, Math.min(bounds.height - 40, top));
		Object.assign(win.style, { left: left + 'px', top: top + 'px', width: width + 'px', height: height + 'px' });
		placeHosts(state);
	}

	function onUp() {
		document.removeEventListener('pointermove', onMove);
		document.removeEventListener('pointerup', onUp);
		document.removeEventListener('pointercancel', onUp);
		document.body.style.userSelect = '';
		if (moved) {
			state.ref.invokeMethodAsync('OnFloatMoved', win.dataset.mokaDockFloat,
				win.offsetLeft, win.offsetTop, win.offsetWidth, win.offsetHeight);
		}
	}

	document.addEventListener('pointermove', onMove);
	document.addEventListener('pointerup', onUp);
	document.addEventListener('pointercancel', onUp);
}

// The start or the end of a box within SNAP of a line lands on it.
function snap(start, size, lines) {
	for (const line of lines) {
		if (Math.abs(start - line) <= SNAP) return line;
		if (Math.abs(start + size - line) <= SNAP) return line - size;
	}
	return start;
}

function snapEdge(edge, lines) {
	for (const line of lines) {
		if (Math.abs(edge - line) <= SNAP) return line;
	}
	return edge;
}

function onFocusIn(state, e) {
	const gutter = e.target.closest?.('[data-moka-dock-gutter]');
	if (gutter) {
		const split = gutter.closest('[data-moka-dock-split]');
		syncSplitterValues(split, gutter, splitterOptions(split, gutter));
		return;
	}

	bringToFront(state, e.target);
	mark(state, 'data-moka-dock-focus', groupOf(state, e.target));
}

// Marks the frame of the group that holds focus or the pointer, its content included, which renders apart from the
// frame: the Minimal look shows a header only then.
function mark(state, attribute, group) {
	for (const frame of state.root.querySelectorAll(`[${attribute}]`)) {
		if (frame.dataset.mokaDockGroup !== group) frame.removeAttribute(attribute);
	}
	if (group) state.root.querySelector(`[data-moka-dock-group="${CSS.escape(group)}"]`)?.setAttribute(attribute, '');
}

// A window behind another comes to the front when focus or a press goes into it, its content included.
function bringToFront(state, target) {
	const host = target.closest?.('[data-moka-dock-host]');
	const win = target.closest?.('[data-moka-dock-float]')
		?? (host ? state.root.querySelector(`[data-moka-dock-float="${CSS.escape(host.dataset.mokaDockIn ?? '')}"]`) : null);
	if (!win || !state.root.contains(win)) return;
	const windows = state.root.querySelectorAll('[data-moka-dock-float]');
	if (windows[windows.length - 1] !== win) state.ref.invokeMethodAsync('OnFloatFocused', win.dataset.mokaDockFloat);
}

function armDrag(state, e, kind, id, source) {
	const start = { x: e.clientX, y: e.clientY };
	const pointer = e.pointerId;

	function onMove(ev) {
		if (ev.pointerId !== pointer) return;
		if (!state.drag) {
			if (Math.hypot(ev.clientX - start.x, ev.clientY - start.y) < DRAG_START) return;
			beginDrag(state, kind, id, source);
		}
		moveDrag(state, ev);
	}

	function onUp(ev) {
		document.removeEventListener('pointermove', onMove);
		document.removeEventListener('pointerup', onUp);
		document.removeEventListener('pointercancel', onCancel);
		if (state.drag) dropDrag(state, ev);
	}

	function onCancel() {
		document.removeEventListener('pointermove', onMove);
		document.removeEventListener('pointerup', onUp);
		document.removeEventListener('pointercancel', onCancel);
		endDrag(state, false);
	}

	document.addEventListener('pointermove', onMove);
	document.addEventListener('pointerup', onUp);
	document.addEventListener('pointercancel', onCancel);
}

function beginDrag(state, kind, id, source) {
	const overlay = state.root.querySelector(':scope > [data-moka-dock-drop]');
	if (!overlay) return;

	const sourceGroup = source.closest('[data-moka-dock-group]');
	const ghost = document.createElement('div');
	ghost.className = 'moka-dock-drop__ghost';
	ghost.textContent = kind === 'panel' ? source.textContent.trim() : (sourceGroup?.getAttribute('aria-label') ?? '');
	const preview = document.createElement('div');
	preview.className = 'moka-dock-drop__preview';
	overlay.replaceChildren(preview, ghost);
	showOverlay(overlay);

	const main = state.root.querySelector('.moka-dock-main');
	state.drag = {
		kind,
		id,
		overlay,
		ghost,
		preview,
		compass: [],
		target: null,
		main: main?.getBoundingClientRect() ?? state.root.getBoundingClientRect(),
		groups: [...state.root.querySelectorAll('[data-moka-dock-group]')]
			.filter(g => g.dataset.mokaDockMode === 'docked' || g.dataset.mokaDockMode === 'floating')
			.filter(g => !(kind === 'group' && g === sourceGroup))
			.filter(g => g.getClientRects().length > 0)
			.map(g => ({ id: g.dataset.mokaDockGroup, element: g, floating: g.dataset.mokaDockMode === 'floating' }))
	};
	state.root.setAttribute('data-moka-dock-dragging', '');
	document.body.style.userSelect = 'none';
	if (state.options.indicator === 'compass') buildCompass(state);
}

function showOverlay(overlay) {
	if (typeof overlay.showPopover === 'function') {
		if (!overlay.matches(':popover-open')) overlay.showPopover();
	} else {
		overlay.setAttribute('data-moka-open', '');
	}
}

function hideOverlay(overlay) {
	if (typeof overlay.hidePopover === 'function') {
		if (overlay.matches(':popover-open')) overlay.hidePopover();
	} else {
		overlay.removeAttribute('data-moka-open');
	}
	overlay.replaceChildren();
}

function moveDrag(state, e) {
	const drag = state.drag;
	if (!drag) return;
	drag.ghost.style.left = e.clientX + 12 + 'px';
	drag.ghost.style.top = e.clientY + 12 + 'px';
	drag.target = state.options.indicator === 'compass' ? compassTarget(state, e) : zoneTarget(state, e);
	showPreview(drag);
}

// Zones: near the layout's edge, a new group along that edge. Over a tab strip, a tab at that place. Over a group,
// the quarter nearest the pointer's side splits it, the middle joins it as a tab.
function zoneTarget(state, e) {
	const drag = state.drag;
	const main = drag.main;
	const x = e.clientX;
	const y = e.clientY;
	if (x < main.left || x > main.right || y < main.top || y > main.bottom) return null;

	const group = groupAt(drag, x, y);
	const box = group?.element.getBoundingClientRect();
	const header = group?.element.querySelector(':scope > .moka-dock-panel-header');
	const headerBox = header?.getBoundingClientRect();
	if (group && headerBox && y <= headerBox.bottom) {
		const strip = group.element.querySelector('[data-moka-dock-tabs]');
		return { node: group.id, zone: 'center', index: strip ? tabIndexAt(strip, x) : -1, rect: box };
	}

	const edge = layoutEdge(main, x, y);
	if (edge) return edge;
	if (!group) return null;

	const rx = (x - box.left) / box.width;
	const ry = (y - box.top) / box.height;
	const distances = [['left', rx], ['right', 1 - rx], ['top', ry], ['bottom', 1 - ry]];
	const [zone, distance] = distances.reduce((a, b) => (b[1] < a[1] ? b : a));
	if (distance > 0.25) return { node: group.id, zone: 'center', index: -1, rect: box };
	return { node: group.id, zone, index: -1, rect: half(box, zone) };
}

function layoutEdge(main, x, y) {
	const zones = [
		['left', x - main.left],
		['right', main.right - x],
		['top', y - main.top],
		['bottom', main.bottom - y]
	];
	const [zone, distance] = zones.reduce((a, b) => (b[1] < a[1] ? b : a));
	if (distance > EDGE_ZONE) return null;
	return { node: null, zone, index: -1, rect: strip(main, zone) };
}

function groupAt(drag, x, y) {
	const hit = g => {
		const box = g.element.getBoundingClientRect();
		return x >= box.left && x <= box.right && y >= box.top && y <= box.bottom;
	};
	return drag.groups.filter(g => g.floating).reverse().find(hit) ?? drag.groups.find(g => !g.floating && hit(g));
}

function tabIndexAt(strip, x) {
	const tabs = [...strip.querySelectorAll('[role="tab"]')];
	for (let i = 0; i < tabs.length; i++) {
		const box = tabs[i].getBoundingClientRect();
		if (x < box.left + box.width / 2) return i;
	}
	return tabs.length;
}

function half(box, zone) {
	switch (zone) {
		case 'left': return { left: box.left, top: box.top, width: box.width / 2, height: box.height };
		case 'right': return { left: box.left + box.width / 2, top: box.top, width: box.width / 2, height: box.height };
		case 'top': return { left: box.left, top: box.top, width: box.width, height: box.height / 2 };
		default: return { left: box.left, top: box.top + box.height / 2, width: box.width, height: box.height / 2 };
	}
}

function strip(box, zone) {
	const w = box.width / 4;
	const h = box.height / 4;
	switch (zone) {
		case 'left': return { left: box.left, top: box.top, width: w, height: box.height };
		case 'right': return { left: box.right - w, top: box.top, width: w, height: box.height };
		case 'top': return { left: box.left, top: box.top, width: box.width, height: h };
		default: return { left: box.left, top: box.bottom - h, width: box.width, height: h };
	}
}

// Compass: five targets in the middle of the group under the pointer and four at the layout's edges. Only a target
// under the pointer drops anything.
function buildCompass(state) {
	const drag = state.drag;
	const edges = ['left', 'right', 'top', 'bottom'].map(zone => {
		const element = document.createElement('div');
		element.className = `moka-dock-drop__target moka-dock-drop__target--edge moka-dock-drop__target--${zone}`;
		drag.overlay.append(element);
		return { element, zone, node: null };
	});
	const center = document.createElement('div');
	center.className = 'moka-dock-drop__compass';
	const middle = ['top', 'left', 'center', 'right', 'bottom'].map(zone => {
		const element = document.createElement('div');
		element.className = `moka-dock-drop__target moka-dock-drop__target--${zone}`;
		center.append(element);
		return { element, zone, node: undefined };
	});
	drag.overlay.append(center);
	drag.compass = { edges, center, middle, group: null };

	const main = drag.main;
	const size = 36;
	const place = (element, left, top) => Object.assign(element.style, { left: left + 'px', top: top + 'px' });
	place(edges[0].element, main.left + 8, main.top + main.height / 2 - size / 2);
	place(edges[1].element, main.right - 8 - size, main.top + main.height / 2 - size / 2);
	place(edges[2].element, main.left + main.width / 2 - size / 2, main.top + 8);
	place(edges[3].element, main.left + main.width / 2 - size / 2, main.bottom - 8 - size);
}

function compassTarget(state, e) {
	const drag = state.drag;
	const { edges, center, middle } = drag.compass;
	const x = e.clientX;
	const y = e.clientY;
	const inside = el => {
		const box = el.getBoundingClientRect();
		return x >= box.left && x <= box.right && y >= box.top && y <= box.bottom;
	};

	const group = groupAt(drag, x, y);
	if (group && group !== drag.compass.group) {
		drag.compass.group = group;
		const box = group.element.getBoundingClientRect();
		Object.assign(center.style, { left: box.left + box.width / 2 + 'px', top: box.top + box.height / 2 + 'px', display: '' });
	} else if (!group) {
		drag.compass.group = null;
		center.style.display = 'none';
	}

	for (const edge of edges) {
		const on = inside(edge.element);
		edge.element.toggleAttribute('data-moka-on', on);
		if (on) return { node: null, zone: edge.zone, index: -1, rect: strip(drag.main, edge.zone) };
	}

	for (const target of middle) {
		const on = drag.compass.group && inside(target.element);
		target.element.toggleAttribute('data-moka-on', !!on);
		if (on) {
			const box = drag.compass.group.element.getBoundingClientRect();
			return {
				node: drag.compass.group.id,
				zone: target.zone,
				index: -1,
				rect: target.zone === 'center' ? box : half(box, target.zone)
			};
		}
	}

	return null;
}

function showPreview(drag) {
	const rect = drag.target?.rect;
	drag.preview.toggleAttribute('data-moka-on', !!rect);
	if (!rect) return;
	Object.assign(drag.preview.style, {
		left: rect.left + 'px',
		top: rect.top + 'px',
		width: rect.width + 'px',
		height: rect.height + 'px'
	});
}

function dropDrag(state, e) {
	const drag = state.drag;
	const target = drag.target;
	endDrag(state, true);

	if (target) {
		state.ref.invokeMethodAsync('OnDrop', drag.kind, drag.id, target.node, target.zone, target.index);
		return;
	}

	const main = drag.main;
	const inside = e.clientX >= main.left && e.clientX <= main.right && e.clientY >= main.top && e.clientY <= main.bottom;
	if (inside) {
		const width = Math.min(360, main.width);
		const height = Math.min(280, main.height);
		const x = Math.max(0, Math.min(main.width - width, e.clientX - main.left - 24));
		const y = Math.max(0, Math.min(main.height - height, e.clientY - main.top - 12));
		state.ref.invokeMethodAsync('OnDropFloat', drag.kind, drag.id, x, y, width, height);
	}
}

// Ends a drag. A real drag swallows the click that follows, so the tab it started on isn't also picked.
function endDrag(state, dropped) {
	const drag = state.drag;
	if (!drag) return;
	state.drag = null;
	hideOverlay(drag.overlay);
	state.root.removeAttribute('data-moka-dock-dragging');
	document.body.style.userSelect = '';
	if (dropped) {
		const swallow = ev => {
			ev.stopPropagation();
			ev.preventDefault();
		};
		window.addEventListener('click', swallow, { capture: true, once: true });
		setTimeout(() => window.removeEventListener('click', swallow, { capture: true }), 0);
	}
}

function onPointerOver(state, e) {
	mark(state, 'data-moka-dock-hover', groupOf(state, e.target));
	if (!state.options.hover || state.drag) return;
	const tab = e.target.closest('[data-moka-dock-autohide]');
	if (!tab || tab.getAttribute('aria-expanded') === 'true') return;
	clearTimeout(state.hover);
	state.hover = setTimeout(() => state.ref.invokeMethodAsync('OnFlyoutHover', tab.dataset.mokaDockAutohide), HOVER_DELAY);
}

function onPointerOut(state, e) {
	if (!state.root.contains(e.relatedTarget)) mark(state, 'data-moka-dock-hover', null);
	if (e.target.closest('[data-moka-dock-autohide]')) clearTimeout(state.hover);
}

// A press outside an open flyout, its side bar tab and any menu or popup closes the flyout.
function onDocumentDown(state, e) {
	const flyout = state.root.querySelector('[data-moka-dock-flyout]');
	if (!flyout) return;
	const target = e.target;
	if (flyout.contains(target) || target.closest?.('[data-moka-dock-autohide], [data-moka-popup], [popover]')) return;
	if (target.closest?.('[data-moka-dock-host]')?.dataset.mokaDockIn === flyout.dataset.mokaDockFlyout) return;
	state.ref.invokeMethodAsync('OnFlyoutDismiss');
}

function onKeyDown(state, e) {
	const target = e.target;

	if (e.key === 'Escape') {
		if (state.drag) {
			e.preventDefault();
			endDrag(state, false);
			return;
		}
		if (e.defaultPrevented || target.closest('[role="menu"], [data-moka-popup], [popover], [role="dialog"]')) return;
		// The page can be a round trip behind .NET, as when a maximize just pressed hasn't been drawn yet, so .NET,
		// which holds the state, decides what Escape undoes. The key is claimed only when that is already on screen.
		if (state.root.querySelector('.moka-dock-group--maximized, .moka-dock-float--maximized, [data-moka-dock-flyout]')) {
			e.preventDefault();
		}
		state.ref.invokeMethodAsync('OnEscape');
		return;
	}

	const gutter = target.closest('[data-moka-dock-gutter]');
	if (gutter) {
		const split = gutter.closest('[data-moka-dock-split]');
		if (e.key === 'Enter') {
			e.preventDefault();
			state.ref.invokeMethodAsync('OnSplitReset', split.dataset.mokaDockSplit, parseInt(gutter.dataset.mokaDockGutter, 10));
			return;
		}
		const sizes = keySplitter(split, gutter, e, splitterOptions(split, gutter));
		if (sizes) state.keyed = { split: split.dataset.mokaDockSplit, sizes };
		return;
	}

	if (e.key === 'F6' && !e.ctrlKey && !e.altKey && !e.metaKey) {
		if (cycleGroups(state, e.shiftKey ? -1 : 1)) e.preventDefault();
		return;
	}

	if (e.key === 'Tab' && e.ctrlKey && !e.altKey && !e.metaKey) {
		const group = groupOf(state, target);
		if (group) {
			e.preventDefault();
			state.ref.invokeMethodAsync('OnCycleTab', group, e.shiftKey ? -1 : 1);
		}
		return;
	}

	if (target.matches('[data-moka-dock-tabs] [role="tab"]')) {
		if (tabKeys(state, target, e)) return;
	}

	if (e.key === 'Tab' && !e.ctrlKey && !e.altKey && !e.metaKey) visualTab(state, e);
}

function onKeyUp(state) {
	if (!state.keyed) return;
	const { split, sizes } = state.keyed;
	state.keyed = null;
	state.ref.invokeMethodAsync('OnSplitResized', split, sizes);
}

// The tabs pattern with automatic activation: the arrows, Home and End pick a tab, Delete closes one that can close.
function tabKeys(state, tab, e) {
	if (e.altKey || e.ctrlKey || e.metaKey) return false;
	const tabs = [...tab.closest('[role="tablist"]').querySelectorAll('[role="tab"]')];
	const index = tabs.indexOf(tab);
	let next = -1;
	switch (e.key) {
		case 'ArrowRight': next = (index + 1) % tabs.length; break;
		case 'ArrowLeft': next = (index - 1 + tabs.length) % tabs.length; break;
		case 'Home': next = 0; break;
		case 'End': next = tabs.length - 1; break;
		case 'Delete':
			if (tab.dataset.mokaDockClosable === 'true') {
				e.preventDefault();
				state.ref.invokeMethodAsync('OnCloseKey', tab.dataset.mokaDockTab);
				return true;
			}
			return false;
		default: return false;
	}

	e.preventDefault();
	tabs[next].focus();
	tabs[next].click();
	return true;
}

// F6 and Shift+F6 move between groups, in the order they are drawn, landing on a group's tab or its content.
function cycleGroups(state, delta) {
	const groups = orderedGroups(state);
	if (groups.length === 0) return false;
	const current = groupOf(state, document.activeElement);
	const index = groups.findIndex(g => g.dataset.mokaDockGroup === current);
	const next = groups[(index + delta + groups.length) % groups.length];
	focusGroup(state, next);
	return true;
}

function orderedGroups(state) {
	return [...state.root.querySelectorAll('[data-moka-dock-group]')]
		.filter(g => g.getClientRects().length > 0)
		.sort((a, b) => Number(a.dataset.mokaDockOrder) - Number(b.dataset.mokaDockOrder));
}

function groupOf(state, element) {
	if (!element || !state.root.contains(element)) return null;
	const frame = element.closest('[data-moka-dock-group]');
	if (frame) return frame.dataset.mokaDockGroup;
	return element.closest('[data-moka-dock-host]')?.dataset.mokaDockIn ?? null;
}

function hostOf(state, group) {
	const frame = state.root.querySelector(`[data-moka-dock-group="${CSS.escape(group)}"]`);
	const owned = frame?.getAttribute('aria-owns');
	return owned ? document.getElementById(owned) : null;
}

function focusGroup(state, frame) {
	const tab = frame.querySelector('[role="tab"][aria-selected="true"]');
	if (tab) {
		tab.focus();
		return;
	}
	const host = hostOf(state, frame.dataset.mokaDockGroup);
	const first = host && [...host.querySelectorAll(TABBABLE)].find(visible);
	(first ?? host ?? frame.querySelector(TABBABLE))?.focus();
}

function visible(element) {
	return element.getClientRects().length > 0 && !element.closest('[hidden], [inert]');
}

// Tab follows what the person sees: a group's header, then its content, then the next group. A panel's content
// renders apart from its group, so the document order alone would visit every header first.
function visualTab(state, e) {
	const sequence = [];
	for (const frame of orderedGroups(state)) {
		sequence.push(...[...frame.querySelectorAll(TABBABLE)].filter(visible));
		const host = hostOf(state, frame.dataset.mokaDockGroup);
		if (host && !host.hidden) sequence.push(...[...host.querySelectorAll(TABBABLE)].filter(visible));
	}

	const index = sequence.indexOf(document.activeElement);
	if (index < 0) return;
	const next = sequence[index + (e.shiftKey ? -1 : 1)];
	if (next) {
		e.preventDefault();
		next.focus();
		return;
	}

	const all = [...state.root.querySelectorAll(TABBABLE)].filter(visible);
	const edge = e.shiftKey ? all[0] : all[all.length - 1];
	if (edge && edge !== document.activeElement) edge.focus();
}
