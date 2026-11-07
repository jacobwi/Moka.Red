// Floating panels in the browser's top layer, placed against their trigger.
//
// A panel drawn inside its trigger's wrapper is cut off by any ancestor that hides overflow, and painted under any
// later element with a stacking context of its own (a z-index, a transform, an opacity animation). No z-index gets
// it out of either. A manual popover shown with showPopover() sits in the top layer above both, and CSS anchor
// positioning keeps it against its trigger as the page scrolls.
//
// Blazor still owns open and closed. The panel is rendered with popover="manual" and data-moka-popup, .NET calls
// open() after the render that adds it and close() after the one that removes it, and nothing here dismisses a
// panel by itself. The panel stays where Blazor put it in the DOM, so it inherits the theme's tokens and matches
// its scoped CSS as before, and its events bubble to the same ancestors.
//
// Where anchor positioning is missing, the panel gets fixed coordinates from its trigger's box, flipped and shifted
// to stay in the window, and placed again on scroll and resize. Where the Popover API is missing, nothing happens
// and the panel keeps the in-flow position its stylesheet gives it.
//
// A panel that places itself, such as a context menu at the pointer, is opened without an anchor. It only goes to
// the top layer and keeps the fixed coordinates its own styles give it: there a fixed position is always against the
// window, where a transformed, filtered or contained ancestor would otherwise have captured it.

const AREAS = {
	'top': 'top',
	'top-start': 'top span-right',
	'top-end': 'top span-left',
	'bottom': 'bottom',
	'bottom-start': 'bottom span-right',
	'bottom-end': 'bottom span-left',
	'left': 'left',
	'left-start': 'left span-bottom',
	'left-end': 'left span-top',
	'right': 'right',
	'right-start': 'right span-bottom',
	'right-end': 'right span-top'
};

const BLOCK_FIRST = 'flip-block, flip-inline, flip-block flip-inline';
const INLINE_FIRST = 'flip-inline, flip-block, flip-block flip-inline';
const WINDOW_MARGIN = 8;
const STYLE_CHANGES = { attributes: true, attributeFilter: ['style'] };

const supportsPopover = typeof HTMLElement !== 'undefined' && typeof HTMLElement.prototype.showPopover === 'function';
const supportsAnchoring = typeof CSS !== 'undefined' && CSS.supports('anchor-name: --moka') &&
	CSS.supports('position-area: bottom');

const popups = new Map();
const anchorNames = new WeakMap();
let nextName = 0;

/**
 * Shows a panel in the top layer against its anchor. Calling it again with the same key while the panel is open
 * takes the new options.
 * @param {string} key - Unique on the page; close() takes the same key.
 * @param {HTMLElement} panel - The panel, rendered with popover="manual" and data-moka-popup.
 * @param {HTMLElement|null} anchor - The element the panel's stylesheet positioned it against. Null for a panel that
 *     places itself: it is shown in the top layer and nothing here moves it, so options are ignored.
 * @param {HTMLElement|null} backdrop - An outside-click layer, also a manual popover, shown just under the panel
 *     so it still covers everything else.
 * @param {{placement?: string, width?: 'match'|'min'|null, shift?: number, flip?: boolean}} options - placement
 *     is top, bottom, left or right, optionally with -start or -end (bottom by default); width 'match' makes the
 *     panel as wide as the anchor and 'min' at least as wide; shift moves it along the anchor's edge, in pixels;
 *     flip false keeps it on its side when the window has no room there, for a panel with an arrow.
 */
export function open(key, panel, anchor, backdrop, options) {
	anchor ??= null;
	if (!supportsPopover || !panel?.isConnected || (anchor && !anchor.isConnected)) return;

	let state = popups.get(key);
	if (state && (state.panel !== panel || state.anchor !== anchor || state.backdrop !== (backdrop ?? null))) {
		close(key);
		state = undefined;
	}

	if (!state) {
		state = start(key, panel, anchor, backdrop ?? null);
	}

	if (!anchor) {
		show(state);
		return;
	}

	state.placement = AREAS[options?.placement] ? options.placement : 'bottom';
	state.width = options?.width === 'match' || options?.width === 'min' ? options.width : null;
	state.shift = Number(options?.shift) || 0;
	state.flip = options?.flip !== false;

	show(state);
	place(state);
}

/**
 * Takes a panel out of the top layer and undoes what open() did. Does nothing for a key that isn't open.
 * @param {string} key - The key open() was given.
 */
export function close(key) {
	const state = popups.get(key);
	if (!state) return;

	popups.delete(key);
	state.observer?.disconnect();
	state.resizeObserver?.disconnect();
	if (state.onMove) {
		window.removeEventListener('scroll', state.onMove, true);
		window.removeEventListener('resize', state.onMove);
		cancelAnimationFrame(state.frame);
	}

	for (const property of Object.keys(state.written)) {
		state.panel.style.removeProperty(property);
	}

	const names = state.anchor && anchorNames.get(state.anchor);
	if (names) {
		names.delete(state.name);
		writeAnchorNames(state.anchor);
	}

	hide(state.panel);
	hide(state.backdrop);
}

function start(key, panel, anchor, backdrop) {
	const state = { key, panel, anchor, backdrop, name: `--moka-popup-${++nextName}`, styles: {}, written: {} };
	popups.set(key, state);

	if (!anchor) return state;

	if (supportsAnchoring) {
		let names = anchorNames.get(anchor);
		if (!names) anchorNames.set(anchor, names = new Set());
		names.add(state.name);
		writeAnchorNames(anchor);
	} else {
		state.onMove = () => {
			if (state.frame) return;
			state.frame = requestAnimationFrame(() => {
				state.frame = 0;
				place(state);
			});
		};
		window.addEventListener('scroll', state.onMove, { capture: true, passive: true });
		window.addEventListener('resize', state.onMove, { passive: true });
		state.resizeObserver = new ResizeObserver(state.onMove);
		state.resizeObserver.observe(anchor);
		state.resizeObserver.observe(panel);
	}

	// Blazor writes the whole style attribute when a rendered style changes, which would take the placement with it.
	state.observer = new MutationObserver(() => {
		writeStyles(state);
		if (supportsAnchoring) writeAnchorNames(anchor);
		state.observer.takeRecords();
	});
	state.observer.observe(panel, STYLE_CHANGES);
	state.observer.observe(anchor, STYLE_CHANGES);

	return state;
}

function show(state) {
	const { panel, backdrop } = state;
	const backdropShown = !backdrop?.isConnected || backdrop.matches(':popover-open');
	if (panel.matches(':popover-open') && backdropShown) return;

	// The last element shown is the top one, so the backdrop goes first.
	hide(panel);
	hide(backdrop);
	if (backdrop?.isConnected) showPopover(backdrop);
	showPopover(panel);
}

function showPopover(element) {
	try {
		element.showPopover();
	} catch {
		// Not a popover after all, or already gone from the page.
	}
}

function hide(element) {
	if (!element?.isConnected || !element.matches(':popover-open')) return;
	try {
		element.hidePopover();
	} catch {
		// Removed between the check and the call.
	}
}

function place(state) {
	if (!state.panel.isConnected || !state.anchor.isConnected) return;

	if (supportsAnchoring) {
		setStyles(state, anchoredStyles(state));
	} else {
		placeByCoordinates(state);
	}
}

function anchoredStyles(state) {
	const side = state.placement.split('-')[0];
	const styles = {
		'position': 'fixed',
		'top': 'auto',
		'right': 'auto',
		'bottom': 'auto',
		'left': 'auto',
		'transform': 'none',
		'position-anchor': state.name,
		'position-area': AREAS[state.placement],
		'position-try-fallbacks': !state.flip ? 'none' : side === 'left' || side === 'right' ? INLINE_FIRST : BLOCK_FIRST
	};

	if (state.width === 'match') styles['width'] = 'anchor-size(width)';
	if (state.width === 'min') styles['min-width'] = 'anchor-size(width)';
	if (state.shift) styles['translate'] = shiftValue(state);

	return styles;
}

function shiftValue(state) {
	const [side, align] = state.placement.split('-');
	const amount = align === 'end' ? -state.shift : state.shift;
	return side === 'left' || side === 'right' ? `0 ${amount}px` : `${amount}px 0`;
}

function placeByCoordinates(state) {
	const { panel, anchor } = state;
	const [side, align] = state.placement.split('-');
	const vertical = side === 'top' || side === 'bottom';

	// The stylesheet's margin on the anchor's side is the gap; read it before the inline margins replace it.
	if (!state.gaps) {
		const computed = getComputedStyle(panel);
		state.gaps = {
			block: Math.max(parseFloat(computed.marginTop) || 0, parseFloat(computed.marginBottom) || 0),
			inline: Math.max(parseFloat(computed.marginLeft) || 0, parseFloat(computed.marginRight) || 0)
		};
	}

	const gap = vertical ? state.gaps.block : state.gaps.inline;
	const a = anchor.getBoundingClientRect();

	// Measured where it last stood, or at the window's corner the first time, where nothing narrows it.
	const styles = {
		'position': 'fixed',
		'top': state.styles.top ?? '0px',
		'right': 'auto',
		'bottom': 'auto',
		'left': state.styles.left ?? '0px',
		'margin-top': '0px',
		'margin-right': '0px',
		'margin-bottom': '0px',
		'margin-left': '0px',
		'transform': 'none'
	};
	if (state.width === 'match') styles['width'] = `${a.width}px`;
	if (state.width === 'min') styles['min-width'] = `${a.width}px`;
	setStyles(state, styles);

	const width = panel.offsetWidth;
	const height = panel.offsetHeight;
	const viewWidth = document.documentElement.clientWidth;
	const viewHeight = document.documentElement.clientHeight;
	const shift = align === 'end' ? -state.shift : state.shift;
	let x;
	let y;

	if (vertical) {
		const below = a.bottom + gap;
		const above = a.top - gap - height;
		const fitsBelow = below + height <= viewHeight - WINDOW_MARGIN;
		const fitsAbove = above >= WINDOW_MARGIN;
		const roomierBelow = viewHeight - a.bottom >= a.top;
		const useBelow = !state.flip
			? side === 'bottom'
			: side === 'bottom' ? fitsBelow || (!fitsAbove && roomierBelow) : !fitsAbove && (fitsBelow || roomierBelow);
		y = useBelow ? below : above;
		x = align === 'start' ? a.left : align === 'end' ? a.right - width : a.left + (a.width - width) / 2;
		x = clamp(x + shift, viewWidth - width);
	} else {
		const after = a.right + gap;
		const before = a.left - gap - width;
		const fitsAfter = after + width <= viewWidth - WINDOW_MARGIN;
		const fitsBefore = before >= WINDOW_MARGIN;
		const roomierAfter = viewWidth - a.right >= a.left;
		const useAfter = !state.flip
			? side === 'right'
			: side === 'right' ? fitsAfter || (!fitsBefore && roomierAfter) : !fitsBefore && (fitsAfter || roomierAfter);
		x = useAfter ? after : before;
		y = align === 'start' ? a.top : align === 'end' ? a.bottom - height : a.top + (a.height - height) / 2;
		y = clamp(y + shift, viewHeight - height);
	}

	styles['left'] = `${Math.round(x)}px`;
	styles['top'] = `${Math.round(y)}px`;
	setStyles(state, styles);
}

function clamp(position, room) {
	return Math.max(WINDOW_MARGIN, Math.min(position, room - WINDOW_MARGIN));
}

function setStyles(state, styles) {
	for (const property of Object.keys(state.written)) {
		if (!(property in styles)) {
			state.panel.style.removeProperty(property);
			delete state.written[property];
		}
	}

	state.styles = styles;
	writeStyles(state);
	state.observer?.takeRecords();
}

// Writes only what differs from the last write, compared with what the browser read back, since a value comes
// back serialized its own way. A property the browser doesn't know reads back empty and is left alone after that.
function writeStyles(state) {
	const style = state.panel.style;
	for (const [property, value] of Object.entries(state.styles)) {
		const last = state.written[property];
		if (last && last.value === value && style.getPropertyValue(property) === last.read) continue;

		style.setProperty(property, value);
		state.written[property] = { value, read: style.getPropertyValue(property) };
	}
}

function writeAnchorNames(anchor) {
	const names = anchorNames.get(anchor);
	if (!names || names.size === 0) {
		anchor.style.removeProperty('anchor-name');
		anchorNames.delete(anchor);
		return;
	}

	const current = anchor.style.getPropertyValue('anchor-name');
	const missing = [...names].some(name => !current.split(',').some(part => part.trim() === name));
	if (missing) anchor.style.setProperty('anchor-name', [...names].join(', '));
}
