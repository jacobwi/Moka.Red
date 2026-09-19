/**
 * Moka.Red - Centralized drag/resize/drop utilities.
 * All pointer-based interactions share this module to avoid code duplication.
 *
 * Usage from Blazor:
 *   var module = await JSRuntime.InvokeAsync<IJSObjectReference>("import",
 *     "./_content/Moka.Red.Core/moka-drag.js");
 *   await module.InvokeVoidAsync("makeDraggable", dotNetRef, element, handle, options);
 */

// The keyboard helpers live in moka-keys.js and are re-exported so a component that already
// imports this module can use them: MokaComponentBase caches one module per component.
export { bindActivation, preventKeys } from './moka-keys.js';

// ─── DRAGGABLE ──────────────────────────────────────────────
// Makes an element movable by dragging a handle.
// The element should have position: fixed or absolute, and is placed with left and top. A transform
// that also moves it, such as MokaDialog's translate(-50%, -50%) centering, is dropped on the first
// move and its offset written into left and top, so the element stays under the pointer.
// The callback gets the left and top the drag wrote, and only runs when the element moved.

export function makeDraggable(dotNetRef, element, handle, options) {
	if (!element || !handle) return;
	if (handle._mokaDrag) return; // prevent double-attach

	const opts = options || {};
	const callbackMethod = opts.callbackMethod || 'OnDragMoved';
	const bounds = opts.bounds !== false; // default true - constrain to viewport

	function onPointerDown(e) {
		if (e.button !== 0) return;
		// Don't initiate drag on interactive children (buttons, inputs, links)
		if (e.target.closest('button, input, select, textarea, a, [role="button"]')) return;

		e.preventDefault();

		const rect = element.getBoundingClientRect();
		const offsetX = e.clientX - rect.left;
		const offsetY = e.clientY - rect.top;

		// Where left: 0, top: 0 puts the element on screen, set by the first move. A press that
		// never moves leaves the element and its styles alone.
		let origin = null;

		document.body.style.userSelect = 'none';
		handle.style.cursor = 'grabbing';

		function onPointerMove(e) {
			origin ??= placeByLeftTop(element, rect);

			let x = e.clientX - offsetX;
			let y = e.clientY - offsetY;

			if (bounds) {
				// Keep at least 40px visible on each edge
				x = Math.max(-rect.width + 40, Math.min(window.innerWidth - 40, x));
				y = Math.max(0, Math.min(window.innerHeight - 40, y));
			}

			element.style.left = (x - origin.x) + 'px';
			element.style.top = (y - origin.y) + 'px';
		}

		// A cancelled pointer (the browser took the touch for a gesture) ends the drag where it is.
		// Without it the listeners stayed, and the next mouse move dragged with no button held.
		function onPointerUp() {
			document.removeEventListener('pointermove', onPointerMove);
			document.removeEventListener('pointerup', onPointerUp);
			document.removeEventListener('pointercancel', onPointerUp);
			document.body.style.userSelect = '';
			handle.style.cursor = '';

			if (dotNetRef && origin) {
				dotNetRef.invokeMethodAsync(callbackMethod,
					parseFloat(element.style.left), parseFloat(element.style.top));
			}
		}

		document.addEventListener('pointermove', onPointerMove);
		document.addEventListener('pointerup', onPointerUp);
		document.addEventListener('pointercancel', onPointerUp);
	}

	handle.addEventListener('pointerdown', onPointerDown);
	handle._mokaDrag = {
		destroy: () => {
			handle.removeEventListener('pointerdown', onPointerDown);
			delete handle._mokaDrag;
		}
	};
}

export function removeDraggable(handle) {
	handle?._mokaDrag?.destroy();
}

// Hands the element's placement to left and top alone and keeps it where it is on screen. Returns
// the viewport position of left: 0, top: 0: left and top count from the containing block and the
// margin edge, while the pointer and getBoundingClientRect count from the viewport. It is measured
// rather than read back from the computed left and top, which come rounded and can be a
// percentage. Nothing paints in between, so the element never shows at 0, 0.
function placeByLeftTop(element, onScreen) {
	const style = element.style;
	if (getComputedStyle(element).transform !== 'none') style.transform = 'none';

	style.left = '0px';
	style.top = '0px';
	const zero = element.getBoundingClientRect();
	const origin = { x: zero.left, y: zero.top };

	style.left = (onScreen.left - origin.x) + 'px';
	style.top = (onScreen.top - origin.y) + 'px';
	return origin;
}

// ─── RESIZABLE ──────────────────────────────────────────────
// Makes an element resizable by dragging a splitter/handle.
// Used for dock panel splitters, resizable panels, etc.
//
// options.target picks what the drag sizes:
//   'track' (default) rewrites the grid track the element sits in, which is what a dock panel
//     needs, and sizes the element itself only when it is not a grid item or its track cannot be
//     located.
//   'element' always sizes the element and never touches a grid around it (MokaResizable). The
//     size stays inline after the drag, so there is no flash back before Blazor renders it, and
//     the callback gets the size the element rendered at: its own CSS min and max can still stop
//     it short of the pointer, for instance a calc() limit that resolveLength does not read.
// options.min and options.max are CSS lengths; see resolveLength.
// Only the primary button starts a drag, and the handle needs touch-action: none in its CSS, or a
// touch drag scrolls the page instead.
// options.keyboard makes a focusable splitter work from the keyboard, as a window splitter: the
// arrow keys along its axis resize by 10px (50px with Shift), Home goes to the minimum and End to
// the maximum, or to the size of the containing block when there is none. The size is reported
// through the same callback as a drag, once the key is let go, so a held key does not send a
// report per repeat. The splitter's aria-valuenow, aria-valuemin and aria-valuemax follow in px.

const KEY_STEP = 10;
const KEY_LARGE_STEP = 50;

export function makeResizable(dotNetRef, element, splitter, options) {
	if (!element || !splitter) return;
	if (splitter._mokaResize) return;

	const opts = options || {};
	const direction = opts.direction || 'horizontal'; // 'horizontal' or 'vertical'
	const reverse = opts.reverse || false; // true for right/bottom panels
	const sizesElement = opts.target === 'element';
	const callbackMethod = opts.callbackMethod || 'OnResized';
	const keyboard = opts.keyboard === true;

	const isHorizontal = direction === 'horizontal';
	const templateProp = isHorizontal ? 'gridTemplateColumns' : 'gridTemplateRows';

	const currentSize = () => isHorizontal ? element.offsetWidth : element.offsetHeight;

	// The limits in px. Resolved per resize, not once when attached: rem, % and the viewport units
	// follow the layout. A track's percentage is a share of its grid container.
	function limits() {
		const gridParent = sizesElement ? null : findGridParent(element);
		const percentBasis = gridParent ? boxSize(gridParent, isHorizontal, false) : undefined;
		return {
			gridParent,
			percentBasis,
			minPx: resolveLength(opts.min, element, isHorizontal, percentBasis) ?? 50,
			maxPx: resolveLength(opts.max, element, isHorizontal, percentBasis) ?? Infinity
		};
	}

	// Where End goes: the maximum, or the whole containing block when there is none.
	function endOf({ percentBasis, maxPx }) {
		return Number.isFinite(maxPx) ? maxPx : percentBasis ?? containingBlockSize(element, isHorizontal);
	}

	// What one resize works with, taken when it starts.
	function startResize() {
		const bounds = limits();
		const { gridParent, minPx, maxPx } = bounds;

		// Resolve the dragged track and snapshot the track list once, at the start
		const trackIndex = gridParent ? findTrackIndex(gridParent, element, isHorizontal) : -1;
		const tracks = trackIndex >= 0
			? snapshotTrackList(gridParent, templateProp, trackIndex, isHorizontal)
			: null;

		function apply(size) {
			if (tracks) {
				// Update the parent grid template directly for correct visual feedback
				gridParent.style[templateProp] = setTrack(tracks, trackIndex, size + 'px').join(' ');
			} else if (isHorizontal) {
				element.style.width = size + 'px';
			} else {
				element.style.height = size + 'px';
			}
		}

		return {
			startSize: currentSize(),
			minPx,
			endPx: () => endOf(bounds),
			// The minimum wins over the maximum, as it does in CSS.
			clamp: size => Math.max(minPx, Math.min(maxPx, size)),
			apply,

			// Keeps the grid template, or the element's size in 'element' mode, until Blazor renders
			// the new size over it: clearing it first flashes the old size while that render is on
			// its way. The 'track' fallback clears the element's size as it always has, because the
			// grid owns a dock panel's size. Returns the size to report, which in 'element' mode is
			// the size the element rendered at.
			settle(size) {
				if (tracks) {
					apply(size);
				} else if (sizesElement) {
					apply(size);
					size = currentSize();
					apply(size);
				} else if (isHorizontal) {
					element.style.width = '';
				} else {
					element.style.height = '';
				}

				syncValues();
				return size;
			}
		};
	}

	function report(size) {
		if (dotNetRef) {
			dotNetRef.invokeMethodAsync(callbackMethod, size);
		}
	}

	function syncValues() {
		if (!keyboard) return;

		const bounds = limits();
		const endPx = endOf(bounds);
		splitter.setAttribute('aria-valuenow', String(Math.round(currentSize())));
		splitter.setAttribute('aria-valuemin', String(Math.round(bounds.minPx)));
		if (Number.isFinite(endPx)) {
			splitter.setAttribute('aria-valuemax', String(Math.round(Math.max(bounds.minPx, endPx))));
		} else {
			splitter.removeAttribute('aria-valuemax');
		}
	}

	function onPointerDown(e) {
		if (e.button !== 0) return;
		e.preventDefault();
		e.stopPropagation();

		const startPos = isHorizontal ? e.clientX : e.clientY;
		const resize = startResize();

		document.body.style.userSelect = 'none';
		document.body.style.cursor = isHorizontal ? 'col-resize' : 'row-resize';

		// Disable iframe pointer events during resize
		const iframes = document.querySelectorAll('iframe');
		iframes.forEach(f => f.style.pointerEvents = 'none');

		function calcNewSize(e) {
			const currentPos = isHorizontal ? e.clientX : e.clientY;
			const delta = reverse ? startPos - currentPos : currentPos - startPos;
			return resize.clamp(resize.startSize + delta);
		}

		let lastSize = resize.startSize;

		function onPointerMove(e) {
			lastSize = calcNewSize(e);
			resize.apply(lastSize);
		}

		// A cancelled pointer (the browser took the touch for a gesture) has no usable position, so
		// the drag ends at the last size it showed. Without it the listeners stayed, and the next
		// mouse move resized with no button held.
		function onPointerUp(e) {
			document.removeEventListener('pointermove', onPointerMove);
			document.removeEventListener('pointerup', onPointerUp);
			document.removeEventListener('pointercancel', onPointerUp);

			document.body.style.userSelect = '';
			document.body.style.cursor = '';
			iframes.forEach(f => f.style.pointerEvents = '');

			report(resize.settle(e.type === 'pointercancel' ? lastSize : calcNewSize(e)));
		}

		document.addEventListener('pointermove', onPointerMove);
		document.addEventListener('pointerup', onPointerUp);
		document.addEventListener('pointercancel', onPointerUp);
	}

	// A size from the keyboard waiting to be reported.
	let keyedSize = null;

	function onKeyDown(e) {
		if (e.altKey || e.ctrlKey || e.metaKey) return;

		const step = e.shiftKey ? KEY_LARGE_STEP : KEY_STEP;
		const grow = reverse ? -step : step;
		const deltas = isHorizontal
			? { ArrowRight: grow, ArrowLeft: -grow }
			: { ArrowDown: grow, ArrowUp: -grow };
		if (!Object.hasOwn(deltas, e.key) && e.key !== 'Home' && e.key !== 'End') return;

		// The arrows and Home and End would scroll the page as well.
		e.preventDefault();

		const resize = startResize();
		const target = e.key === 'Home' ? resize.minPx
			: e.key === 'End' ? resize.endPx()
				: resize.startSize + deltas[e.key];
		if (!Number.isFinite(target)) return;

		keyedSize = resize.settle(resize.clamp(target));
	}

	function reportKeyedSize() {
		if (keyedSize === null) return;

		const size = keyedSize;
		keyedSize = null;
		report(size);
	}

	splitter.addEventListener('pointerdown', onPointerDown);
	if (keyboard) {
		splitter.addEventListener('keydown', onKeyDown);
		splitter.addEventListener('keyup', reportKeyedSize);
		splitter.addEventListener('blur', reportKeyedSize);
		splitter.addEventListener('focus', syncValues);
		syncValues();
	}

	splitter._mokaResize = {
		destroy: () => {
			splitter.removeEventListener('pointerdown', onPointerDown);
			splitter.removeEventListener('keydown', onKeyDown);
			splitter.removeEventListener('keyup', reportKeyedSize);
			splitter.removeEventListener('blur', reportKeyedSize);
			splitter.removeEventListener('focus', syncValues);
			delete splitter._mokaResize;
		}
	};
}

export function removeResizable(splitter) {
	splitter?._mokaResize?.destroy();
}

// ─── GRID TRACKS ────────────────────────────────────────────
// makeResizable rewrites one track of the grid template while a splitter moves. A track list
// cannot be split on whitespace: minmax(), min(), calc(), fit-content() and repeat() contain
// spaces, and so do line-name groups like [main start].

// The grid that lays the element out: its parent, looking through display: contents wrappers.
// A grid further up only lays out an ancestor, so resizing one of its tracks would size
// something else.
function findGridParent(element) {
	const parent = layoutParent(element);
	if (!parent) return null;
	const display = getComputedStyle(parent).display;
	return display === 'grid' || display === 'inline-grid' ? parent : null;
}

function layoutParent(element) {
	let parent = element.parentElement;
	while (parent && getComputedStyle(parent).display === 'contents') parent = parent.parentElement;
	return parent;
}

// The dragged element's track: an explicit line number, or the named area it sits in.
function findTrackIndex(gridParent, element, isHorizontal) {
	const cs = getComputedStyle(element);
	const raw = isHorizontal ? cs.gridColumnStart : cs.gridRowStart;
	const line = parseInt(raw, 10);
	if (!isNaN(line)) return line - 1;

	const rows = parseAreaRows(getComputedStyle(gridParent).gridTemplateAreas);
	// The dock layout's left and right areas span every row, so the first row is enough.
	if (isHorizontal) return rows[0]?.indexOf(raw) ?? -1;
	return rows.findIndex(row => row.includes(raw));
}

// The track list the drag rewrites, or null when the track cannot be located and the drag
// should size the element instead. The inline template is what Blazor rendered, so it keeps
// flexible tracks like 1fr. The computed one lists every track in pixels, and is the fallback
// when there is no inline template or it cannot say where the track is.
function snapshotTrackList(gridParent, prop, trackIndex, isHorizontal) {
	const inline = parseTrackList(gridParent.style[prop]);
	if (inline && findTrackToken(inline, trackIndex)) return inline;

	const computed = parseTrackList(getComputedStyle(gridParent)[prop]);
	if (!computed || !findTrackToken(computed, trackIndex)) return null;

	// Give the dock layout's content track back its 1fr, so the rest of the layout still flexes.
	const rows = parseAreaRows(getComputedStyle(gridParent).gridTemplateAreas);
	const contentIndex = isHorizontal
		? rows[0]?.indexOf('content') ?? -1
		: rows.findIndex(row => row.includes('content'));
	const restored = contentIndex >= 0 ? setTrack(computed, contentIndex, '1fr') : null;
	return restored ?? computed;
}

// Splits a track list at top-level whitespace. A line-name group is always a token of its own,
// and a parenthesis that closes back to the top level ends the token, as the CSS tokenizer would.
function splitTrackList(value) {
	const tokens = [];
	let token = '';
	let depth = 0;
	let inNames = false;

	for (const ch of String(value ?? '')) {
		if (inNames) {
			token += ch;
			if (ch === ']') {
				inNames = false;
				tokens.push(token);
				token = '';
			}
		} else if (depth === 0 && ch === '[') {
			if (token) tokens.push(token);
			token = ch;
			inNames = true;
		} else if (depth === 0 && /\s/.test(ch)) {
			if (token) tokens.push(token);
			token = '';
		} else {
			token += ch;
			if (ch === '(') {
				depth++;
			} else if (ch === ')' && depth > 0) {
				depth--;
				if (depth === 0) {
					tokens.push(token);
					token = '';
				}
			}
		}
	}

	if (token) tokens.push(token);
	return tokens;
}

// A grid-template-columns/-rows value as tokens, or null when it has no track list of its own.
function parseTrackList(value) {
	const tokens = splitTrackList(value);
	if (tokens.length === 0) return null;
	const first = tokens[0].toLowerCase();
	return ['none', 'subgrid', 'masonry', 'inherit', 'initial', 'unset', 'revert', 'revert-layer']
		.includes(first) ? null : tokens;
}

// repeat(<count>, <tracks>) as { count, tokens }. count is null when layout decides it
// (auto-fill, auto-fit) or the text does not say (var()).
function parseRepeat(token) {
	const match = /^repeat\(([\s\S]*)\)$/i.exec(token);
	if (!match) return null;

	const args = match[1];
	let depth = 0;
	for (let i = 0; i < args.length; i++) {
		const ch = args[i];
		if (ch === '(') {
			depth++;
		} else if (ch === ')') {
			depth--;
		} else if (ch === ',' && depth === 0) {
			const count = args.slice(0, i).trim();
			return {
				count: /^\d+$/.test(count) ? parseInt(count, 10) : null,
				tokens: splitTrackList(args.slice(i + 1))
			};
		}
	}
	return { count: null, tokens: [] };
}

// How many tracks a token stands for, or null when only layout can tell. A top-level var() can
// hold any number of tracks.
function trackCount(token) {
	if (token.startsWith('[')) return 0;

	const repeat = parseRepeat(token);
	if (!repeat) return /^var\(/i.test(token) ? null : 1;
	if (repeat.count === null) return null;

	let perRepeat = 0;
	for (const inner of repeat.tokens) {
		const count = trackCount(inner);
		if (count === null) return null;
		perRepeat += count;
	}
	return repeat.count * perRepeat;
}

// Finds the token holding track `index`, and the track's offset inside it when that token is a
// repeat(). Null when the index is past the list, or a token before it has no fixed count.
function findTrackToken(tokens, index) {
	let first = 0;
	for (let at = 0; at < tokens.length; at++) {
		const count = trackCount(tokens[at]);
		if (count === null) return null;
		if (index < first + count) return { at, offset: index - first };
		first += count;
	}
	return null;
}

// The track list with track `index` set to `size` and every other track as it was written, or
// null when the track cannot be located.
function setTrack(tokens, index, size) {
	const found = findTrackToken(tokens, index);
	if (!found) return null;

	const repeat = parseRepeat(tokens[found.at]);
	if (!repeat) {
		const updated = tokens.slice();
		updated[found.at] = size;
		return updated;
	}

	// The track sits inside repeat(). Write that repeat() out in full so only this copy changes.
	const expanded = [];
	for (let i = 0; i < repeat.count; i++) expanded.push(...repeat.tokens);
	const inner = setTrack(expanded, found.offset, size);
	return inner && mergeLineNames([...tokens.slice(0, found.at), ...inner, ...tokens.slice(found.at + 1)]);
}

// Two line-name groups side by side are invalid, and writing out a repeat() can produce them:
// repeat(2, [a] 1fr [b]) is [a] 1fr [b a] 1fr [b].
function mergeLineNames(tokens) {
	const merged = [];
	for (const token of tokens) {
		const last = merged[merged.length - 1];
		if (token.startsWith('[') && last?.startsWith('[')) {
			const names = `${last.slice(1, -1)} ${token.slice(1, -1)}`.split(/\s+/).filter(Boolean);
			merged[merged.length - 1] = `[${names.join(' ')}]`;
		} else {
			merged.push(token);
		}
	}
	return merged;
}

// grid-template-areas as rows of cells. Inside a row string, a run of dots is one empty cell and
// anything else between whitespace or dots is a named cell, so "a.b" is three cells.
function parseAreaRows(value) {
	const rows = [];
	const strings = /"([^"]*)"|'([^']*)'/g;
	let match;
	while ((match = strings.exec(String(value ?? ''))) !== null) {
		rows.push((match[1] ?? match[2]).match(/\.+|[^\s.]+/g) ?? []);
	}
	return rows;
}

// ─── SORTABLE ──────────────────────────────────────────────
// Drag-to-reorder items within a single container.

export function initSortable(dotNetRef, container, options) {
	if (!container || container._mokaSortable) return;

	const opts = options || {};
	const horizontal = opts.horizontal || false;
	const useDragHandle = opts.dragHandle || false;
	const itemSelector = opts.itemSelector || ':scope > .moka-sortable-item';
	const callbackMethod = opts.callbackMethod || 'OnSortEnd';

	function getItems() {
		return Array.from(container.querySelectorAll(itemSelector))
			.filter(el => !el.classList.contains('moka-sortable-placeholder'));
	}

	function getInsertIndex(e) {
		const items = getItems();
		for (let i = 0; i < items.length; i++) {
			const rect = items[i].getBoundingClientRect();
			const mid = horizontal ? rect.left + rect.width / 2 : rect.top + rect.height / 2;
			const pos = horizontal ? e.clientX : e.clientY;
			if (pos < mid) return i;
		}
		return items.length;
	}

	function onPointerDown(e) {
		const item = e.target.closest(itemSelector.replace(':scope > ', ''));
		if (!item || item.dataset.disabled === 'true' || !container.contains(item)) return;
		if (useDragHandle && !e.target.closest('.moka-sortable-handle')) return;

		e.preventDefault();

		const items = getItems();
		const startIndex = items.indexOf(item);
		if (startIndex < 0) return;

		const rect = item.getBoundingClientRect();
		const offsetX = e.clientX - rect.left;
		const offsetY = e.clientY - rect.top;

		// Create ghost
		const ghost = item.cloneNode(true);
		ghost.className = 'moka-sortable-ghost';
		ghost.style.cssText = `position:fixed;left:${rect.left}px;top:${rect.top}px;width:${rect.width}px;height:${rect.height}px;z-index:99999;pointer-events:none;margin:0;`;
		document.body.appendChild(ghost);

		// Create placeholder
		const placeholder = document.createElement('div');
		placeholder.className = 'moka-sortable-placeholder';
		placeholder.style.cssText = `width:${rect.width}px;height:${rect.height}px;`;
		item.after(placeholder);
		item.style.display = 'none';

		document.body.style.userSelect = 'none';

		function onPointerMove(e) {
			ghost.style.left = (e.clientX - offsetX) + 'px';
			ghost.style.top = (e.clientY - offsetY) + 'px';

			const newIndex = getInsertIndex(e);
			const currentItems = getItems();
			if (newIndex >= currentItems.length) {
				container.appendChild(placeholder);
			} else {
				container.insertBefore(placeholder, currentItems[newIndex]);
			}
		}

		function onPointerUp(e) {
			document.removeEventListener('pointermove', onPointerMove);
			document.removeEventListener('pointerup', onPointerUp);
			document.body.style.userSelect = '';

			// Restore item
			item.style.display = '';
			placeholder.replaceWith(item);
			ghost.remove();

			const finalItems = getItems();
			const finalIndex = finalItems.indexOf(item);

			if (finalIndex !== startIndex && finalIndex >= 0) {
				dotNetRef.invokeMethodAsync(callbackMethod, startIndex, finalIndex);
			}
		}

		document.addEventListener('pointermove', onPointerMove);
		document.addEventListener('pointerup', onPointerUp);
	}

	container.addEventListener('pointerdown', onPointerDown);
	container._mokaSortable = {
		destroy: () => {
			container.removeEventListener('pointerdown', onPointerDown);
			delete container._mokaSortable;
		}
	};
}

export function removeSortable(container) {
	container?._mokaSortable?.destroy();
}

// ─── CANVAS DRAW ────────────────────────────────────────────
// Pointer-based drawing on a canvas element.
// Used for signature pads, annotation tools, etc.

export function initCanvasDraw(dotNetRef, canvas, options) {
	if (!canvas) return;
	if (canvas._mokaCanvasDraw) return;

	const ctx = canvas.getContext('2d');
	const opts = options || {};

	let drawing = false;
	let paths = [];
	let currentPath = [];

	ctx.strokeStyle = opts.strokeColor || '#000000';
	ctx.lineWidth = opts.strokeWidth || 2;
	ctx.lineCap = 'round';
	ctx.lineJoin = 'round';

	// Fill background
	if (opts.backgroundColor) {
		ctx.fillStyle = opts.backgroundColor;
		ctx.fillRect(0, 0, canvas.width, canvas.height);
	}

	function getCanvasCoords(e) {
		const rect = canvas.getBoundingClientRect();
		const scaleX = canvas.width / rect.width;
		const scaleY = canvas.height / rect.height;
		return {
			x: (e.clientX - rect.left) * scaleX,
			y: (e.clientY - rect.top) * scaleY
		};
	}

	function onPointerDown(e) {
		e.preventDefault();
		drawing = true;
		currentPath = [];
		const { x, y } = getCanvasCoords(e);
		ctx.beginPath();
		ctx.moveTo(x, y);
		currentPath.push({ x, y });
	}

	function onPointerMove(e) {
		if (!drawing) return;
		e.preventDefault();
		const { x, y } = getCanvasCoords(e);
		ctx.lineTo(x, y);
		ctx.stroke();
		currentPath.push({ x, y });
	}

	function onPointerUp() {
		if (!drawing) return;
		drawing = false;
		if (currentPath.length > 0) {
			paths.push([...currentPath]);
			currentPath = [];
			const dataUrl = canvas.toDataURL('image/png');
			const callbackMethod = opts.callbackMethod || 'OnSignatureChanged';
			dotNetRef.invokeMethodAsync(callbackMethod, dataUrl);
		}
	}

	canvas.addEventListener('pointerdown', onPointerDown);
	canvas.addEventListener('pointermove', onPointerMove);
	canvas.addEventListener('pointerup', onPointerUp);
	canvas.addEventListener('pointerleave', onPointerUp);

	canvas._mokaCanvasDraw = {
		paths,
		clear: (bgColor) => {
			ctx.fillStyle = bgColor || opts.backgroundColor || '#ffffff';
			ctx.fillRect(0, 0, canvas.width, canvas.height);
			paths.length = 0;
			currentPath = [];
		},
		undo: (strokeColor, strokeWidth, bgColor) => {
			if (paths.length === 0) return;
			paths.pop();
			ctx.fillStyle = bgColor || opts.backgroundColor || '#ffffff';
			ctx.fillRect(0, 0, canvas.width, canvas.height);
			ctx.strokeStyle = strokeColor || opts.strokeColor || '#000000';
			ctx.lineWidth = strokeWidth || opts.strokeWidth || 2;
			ctx.lineCap = 'round';
			ctx.lineJoin = 'round';
			for (const path of paths) {
				ctx.beginPath();
				ctx.moveTo(path[0].x, path[0].y);
				for (let i = 1; i < path.length; i++) {
					ctx.lineTo(path[i].x, path[i].y);
				}
				ctx.stroke();
			}
			const dataUrl = paths.length > 0 ? canvas.toDataURL('image/png') : null;
			const callbackMethod = opts.callbackMethod || 'OnSignatureChanged';
			dotNetRef.invokeMethodAsync(callbackMethod, dataUrl);
		},
		isEmpty: () => paths.length === 0,
		getDataUrl: () => canvas.toDataURL('image/png'),
		destroy: () => {
			canvas.removeEventListener('pointerdown', onPointerDown);
			canvas.removeEventListener('pointermove', onPointerMove);
			canvas.removeEventListener('pointerup', onPointerUp);
			canvas.removeEventListener('pointerleave', onPointerUp);
			delete canvas._mokaCanvasDraw;
		}
	};
}

export function clearCanvas(canvas, bgColor) {
	canvas?._mokaCanvasDraw?.clear(bgColor);
}

export function undoCanvas(canvas, strokeColor, strokeWidth, bgColor) {
	canvas?._mokaCanvasDraw?.undo(strokeColor, strokeWidth, bgColor);
}

export function getCanvasDataUrl(canvas) {
	return canvas?._mokaCanvasDraw?.getDataUrl() ?? null;
}

export function isCanvasEmpty(canvas) {
	return canvas?._mokaCanvasDraw?.isEmpty() ?? true;
}

export function removeCanvasDraw(canvas) {
	canvas?._mokaCanvasDraw?.destroy();
}

// ─── SCROLL ─────────────────────────────────────────────────
// Utility scroll functions used by multiple components.

export function scrollToBottom(element) {
	if (element) element.scrollTop = element.scrollHeight;
}

export function scrollIntoView(element) {
	element?.scrollIntoView({ behavior: 'smooth', block: 'nearest', inline: 'nearest' });
}

// ─── COLOR ─────────────────────────────────────────────────
// Utilities for color picker: eyedropper, gradient area drag, slider drag.

export function isEyeDropperSupported() {
	return 'EyeDropper' in window;
}

export async function pickColor() {
	if (!window.EyeDropper) return null;
	try {
		const dropper = new EyeDropper();
		const result = await dropper.open();
		return result.sRGBHex;
	} catch { return null; }
}

/**
 * Copies text to the clipboard.
 * Prefers the async Clipboard API and falls back to a hidden textarea + execCommand on
 * insecure origins and older browsers, where navigator.clipboard is missing.
 * @param {string} text - The text to copy.
 * @returns {Promise<boolean>} True when the text reached the clipboard.
 */
export async function copyToClipboard(text) {
	if (navigator.clipboard && window.isSecureContext) {
		try {
			await navigator.clipboard.writeText(text);
			return true;
		} catch {
			// Permission denied or the document was not focused, so try the fallback below.
		}
	}

	const textarea = document.createElement('textarea');
	textarea.value = text;
	textarea.setAttribute('readonly', '');
	textarea.style.cssText = 'position:fixed;top:-9999px;left:-9999px;opacity:0;';
	document.body.appendChild(textarea);

	try {
		textarea.select();
		textarea.setSelectionRange(0, textarea.value.length);
		return document.execCommand('copy');
	} catch {
		return false;
	} finally {
		textarea.remove();
	}
}

export function initColorArea(dotNetRef, element, callbackMethod) {
	if (!element) return;
	if (element._mokaColorArea) return;

	function getPosition(e) {
		const rect = element.getBoundingClientRect();
		const x = Math.max(0, Math.min(1, (e.clientX - rect.left) / rect.width));
		const y = Math.max(0, Math.min(1, (e.clientY - rect.top) / rect.height));
		return { x, y };
	}

	function onPointerDown(e) {
		e.preventDefault();
		element.setPointerCapture(e.pointerId);
		const { x, y } = getPosition(e);
		dotNetRef.invokeMethodAsync(callbackMethod || 'OnColorAreaChanged', x, y);
	}

	function onPointerMove(e) {
		if (!element.hasPointerCapture(e.pointerId)) return;
		const { x, y } = getPosition(e);
		dotNetRef.invokeMethodAsync(callbackMethod || 'OnColorAreaChanged', x, y);
	}

	function onPointerUp(e) {
		if (element.hasPointerCapture(e.pointerId)) {
			element.releasePointerCapture(e.pointerId);
		}
	}

	element.addEventListener('pointerdown', onPointerDown);
	element.addEventListener('pointermove', onPointerMove);
	element.addEventListener('pointerup', onPointerUp);

	element._mokaColorArea = {
		destroy: () => {
			element.removeEventListener('pointerdown', onPointerDown);
			element.removeEventListener('pointermove', onPointerMove);
			element.removeEventListener('pointerup', onPointerUp);
			delete element._mokaColorArea;
		}
	};
}

export function removeColorArea(element) {
	element?._mokaColorArea?.destroy();
}

export function initColorSlider(dotNetRef, element, callbackMethod) {
	if (!element) return;
	if (element._mokaColorSlider) return;

	function getPosition(e) {
		const rect = element.getBoundingClientRect();
		return Math.max(0, Math.min(1, (e.clientX - rect.left) / rect.width));
	}

	function onPointerDown(e) {
		e.preventDefault();
		element.setPointerCapture(e.pointerId);
		const x = getPosition(e);
		dotNetRef.invokeMethodAsync(callbackMethod || 'OnHueSliderChanged', x);
	}

	function onPointerMove(e) {
		if (!element.hasPointerCapture(e.pointerId)) return;
		const x = getPosition(e);
		dotNetRef.invokeMethodAsync(callbackMethod || 'OnHueSliderChanged', x);
	}

	function onPointerUp(e) {
		if (element.hasPointerCapture(e.pointerId)) {
			element.releasePointerCapture(e.pointerId);
		}
	}

	element.addEventListener('pointerdown', onPointerDown);
	element.addEventListener('pointermove', onPointerMove);
	element.addEventListener('pointerup', onPointerUp);

	element._mokaColorSlider = {
		destroy: () => {
			element.removeEventListener('pointerdown', onPointerDown);
			element.removeEventListener('pointermove', onPointerMove);
			element.removeEventListener('pointerup', onPointerUp);
			delete element._mokaColorSlider;
		}
	};
}

export function removeColorSlider(element) {
	element?._mokaColorSlider?.destroy();
}

// ─── LENGTHS ────────────────────────────────────────────────
// Resize limits arrive from .NET as CSS lengths such as "240px", "12rem", "30%" or "50vh".

/**
 * Resolves a CSS length to pixels along one axis.
 * A number or a bare numeric string counts as pixels, and 0 is a length like any other.
 * rem follows the root font size and em the element's own. % is a share of percentBasis when
 * given, otherwise of the element's containing block. vw, vh, vmin, vmax and their d/s/l
 * variants follow the viewport.
 * @returns {number|null} Null when the value is empty or uses syntax this does not read, such as
 *   calc(), min() or var().
 */
export function resolveLength(value, element, horizontal, percentBasis) {
	if (typeof value === 'number') return Number.isFinite(value) ? value : null;
	if (typeof value !== 'string') return null;

	const match = /^\s*([+-]?(?:\d+\.?\d*|\.\d+)(?:e[+-]?\d+)?)\s*([a-z%]*)\s*$/i.exec(value);
	if (!match) return null;

	const amount = parseFloat(match[1]);
	const unit = match[2].toLowerCase();

	if (unit === '' || unit === 'px') return amount;
	if (unit === 'rem') return amount * fontSize(document.documentElement);
	if (unit === 'em') return amount * fontSize(element ?? document.documentElement);
	if (unit === '%') {
		const basis = percentBasis ?? containingBlockSize(element, horizontal);
		return basis === null ? null : amount * basis / 100;
	}

	const viewport = /^[dls]?v(w|h|min|max)$/.exec(unit);
	if (!viewport) return null;
	const width = window.innerWidth;
	const height = window.innerHeight;
	const size = viewport[1] === 'w' ? width
		: viewport[1] === 'h' ? height
			: viewport[1] === 'min' ? Math.min(width, height)
				: Math.max(width, height);
	return amount * size / 100;
}

function fontSize(element) {
	return parseFloat(getComputedStyle(element).fontSize) || 16;
}

// What a percentage width or height on the element resolves against: the viewport for a fixed
// element, the padding box of the positioned ancestor for an absolute one, the grid area for a
// grid item, and the parent's content box otherwise.
function containingBlockSize(element, horizontal) {
	if (!element) return null;

	const position = getComputedStyle(element).position;
	if (position === 'fixed') return horizontal ? window.innerWidth : window.innerHeight;

	const absolute = position === 'absolute';
	const block = (absolute ? element.offsetParent : layoutParent(element)) ?? document.documentElement;
	if (!absolute && findGridParent(element)) return gridAreaSize(element, horizontal);
	return boxSize(block, horizontal, absolute);
}

// A grid item's percentages resolve against its grid area, not the grid container, and no DOM API
// reports the area. So CSS resolves 100% on the element itself for a moment, with the element's
// own limits lifted. Nothing paints in between, so the change is never seen.
function gridAreaSize(element, horizontal) {
	const [size, min, max] = horizontal ? ['width', 'minWidth', 'maxWidth'] : ['height', 'minHeight', 'maxHeight'];
	const style = element.style;
	const saved = [style[size], style[min], style[max]];

	style[size] = '100%';
	style[min] = '0px';
	style[max] = 'none';
	const area = parseFloat(getComputedStyle(element)[size]);
	[style[size], style[min], style[max]] = saved;

	return Number.isFinite(area) ? area : null;
}

// The content box, or the padding box when includePadding is set. clientWidth leaves out the
// border and any scrollbar, as a containing block does.
function boxSize(element, horizontal, includePadding) {
	const size = horizontal ? element.clientWidth : element.clientHeight;
	if (includePadding) return size;

	const cs = getComputedStyle(element);
	const padding = horizontal
		? parseFloat(cs.paddingLeft) + parseFloat(cs.paddingRight)
		: parseFloat(cs.paddingTop) + parseFloat(cs.paddingBottom);
	return size - (padding || 0);
}
