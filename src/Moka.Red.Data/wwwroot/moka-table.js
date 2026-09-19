const RESIZE_HANDLER = '_mokaResizeHandler';
const GRID_KEY_HANDLER = '_mokaGridKeyHandler';
const REORDER_DRAG_HANDLERS = '_mokaReorderDragHandlers';
const TAB_STOP_WATCH = '_mokaTabStopWatch';

// What a drag that started in the table can land on, and the classes that mark the spot: the
// dragged header or row lands before the target when it moves back, and after it when it moves on.
const DRAG_KINDS = {
	column: {
		source: 'thead th[draggable="true"]',
		unit: el => el,
		target: 'thead th[draggable="true"]',
		marker: 'moka-table-header--drag-over',
		markerAfter: 'moka-table-header--drag-over-after'
	},
	row: {
		source: 'td.moka-table-cell--reorder[draggable="true"]',
		unit: el => el.closest('tr'),
		target: 'tbody tr[data-row-index]',
		marker: 'moka-table-row--drag-over',
		markerAfter: 'moka-table-row--drag-over-after'
	}
};

// Keys the grid handles itself; left to the browser they scroll the container instead.
const NAV_KEYS = new Set(['ArrowUp', 'ArrowDown', 'ArrowLeft', 'ArrowRight', 'Home', 'End', ' ']);

// A table in a detail row sits inside its parent table's wrapper. Every lookup and every key or
// drag event checks that an element belongs to this table, not to a table nested in it.
const belongsTo = (wrapper, el) => el instanceof Element && el.closest('.moka-table-wrapper') === wrapper;

const ownAll = (wrapper, selector) => [...wrapper.querySelectorAll(selector)].filter(el => belongsTo(wrapper, el));

/**
 * Triggers a CSV download.
 * The payload arrives base64-encoded from .NET; atob yields one char per BYTE, so it has to be
 * turned back into bytes before it reaches the Blob. Handing the raw atob string to the Blob
 * would re-encode each of those chars as UTF-8 and mangle every non-ASCII character.
 * @param {string} csvBase64 - Base64 of the UTF-8 CSV bytes.
 * @param {string} filename - Suggested download filename.
 */
export function downloadCsv(csvBase64, filename) {
	const binary = atob(csvBase64);
	const bytes = new Uint8Array(binary.length);
	for (let i = 0; i < binary.length; i++) {
		bytes[i] = binary.charCodeAt(i);
	}

	// Excel guesses the system codepage without a BOM.
	const bom = new Uint8Array([0xef, 0xbb, 0xbf]);
	const blob = new Blob([bom, bytes], { type: 'text/csv;charset=utf-8' });
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
 * Attaches pointer-drag resizing to every resize handle in the table.
 * Safe to call repeatedly: handles already wired are skipped.
 * @param {object} dotNetRef - .NET callback target.
 * @param {HTMLElement} wrapper - The table wrapper element.
 * @returns {number} How many handles are now wired, so .NET can tell whether the
 *                   header had actually rendered yet.
 */
export function initAllColumnResize(dotNetRef, wrapper) {
	if (!wrapper) return 0;

	const handles = ownAll(wrapper, '.moka-table-resize-handle');
	handles.forEach(handle => {
		if (handle[RESIZE_HANDLER]) return;
		const onPointerDown = e => startResize(e, dotNetRef, handle);
		handle.addEventListener('pointerdown', onPointerDown);
		handle[RESIZE_HANDLER] = onPointerDown;
	});

	return handles.length;
}

function startResize(e, dotNetRef, handle) {
	const th = handle.closest('th');
	if (!th) return;

	e.preventDefault();
	e.stopPropagation();

	const colIndex = parseInt(handle.dataset.colIndex ?? '-1', 10);
	const startX = e.clientX;
	const startWidth = th.offsetWidth;

	document.body.style.userSelect = 'none';
	document.body.style.cursor = 'col-resize';

	const iframes = document.querySelectorAll('iframe');
	iframes.forEach(f => (f.style.pointerEvents = 'none'));

	const widthAt = ev => Math.max(50, startWidth + ev.clientX - startX);

	const onMove = ev => {
		const width = widthAt(ev) + 'px';
		th.style.width = width;
		th.style.minWidth = width;
	};

	const onUp = ev => {
		document.removeEventListener('pointermove', onMove);
		document.removeEventListener('pointerup', onUp);
		document.body.style.userSelect = '';
		document.body.style.cursor = '';
		iframes.forEach(f => (f.style.pointerEvents = ''));

		const finalWidth = widthAt(ev);
		th.style.width = '';
		th.style.minWidth = '';

		// The click that follows the drag would otherwise reach the header and toggle the sort.
		const swallowClick = clickEvent => {
			clickEvent.stopPropagation();
			clickEvent.preventDefault();
		};
		th.addEventListener('click', swallowClick, { capture: true, once: true });
		setTimeout(() => th.removeEventListener('click', swallowClick, { capture: true }), 0);

		if (colIndex >= 0 && Number.isFinite(colIndex)) {
			dotNetRef.invokeMethodAsync('OnColumnResized', colIndex, finalWidth);
		}
	};

	document.addEventListener('pointermove', onMove);
	document.addEventListener('pointerup', onUp);
}

/**
 * Stops arrow/Home/End from scrolling the container while a data cell has focus.
 * The cell's own Blazor keydown handler still runs and does the actual navigation.
 * Enter on a cell marked data-activates-row clicks the cell, which raises the row's OnRowClick
 * the way a mouse click does. Only a key pressed on the cell itself counts, so a button inside
 * the cell still handles its own Enter. A key in a table nested in a detail row bubbles up to
 * this wrapper too; that table handles it, so this one leaves it alone.
 * @param {HTMLElement} wrapper - The table wrapper element.
 */
export function initGridKeys(wrapper) {
	if (!wrapper || wrapper[GRID_KEY_HANDLER]) return;

	const onKeyDown = e => {
		const target = e.target;
		if (!belongsTo(wrapper, target)) return;

		if (e.key === 'Enter') {
			if (e.repeat || e.altKey || e.ctrlKey || e.metaKey || e.shiftKey || e.isComposing) return;
			if (!target.matches('td[data-col-index][data-activates-row="true"]')) return;
			e.preventDefault();
			target.click();
			return;
		}

		if (!NAV_KEYS.has(e.key)) return;
		if (!target.matches('td[data-col-index]')) return;
		e.preventDefault();
	};

	wrapper.addEventListener('keydown', onKeyDown);
	wrapper[GRID_KEY_HANDLER] = onKeyDown;
}

/**
 * Marks where a dragged column header or row would land, and allows the drop there.
 * Blazor handles dragstart, drop and dragend on the elements themselves. dragover fires many
 * times a second, so it is answered here instead of calling .NET each time. Only a drag that
 * started on this table's own header or row handle counts: a file, or a row of a table nested in
 * a detail row, gets no marker and cannot be dropped.
 * @param {HTMLElement} wrapper - The table wrapper element.
 */
export function initReorderDrag(wrapper) {
	if (!wrapper || wrapper[REORDER_DRAG_HANDLERS]) return;

	let kind = null;
	let unit = null;
	let marked = null;
	let markedClass = null;

	const ownMatch = (node, selector) => {
		const el = node instanceof Element ? node.closest(selector) : null;
		return belongsTo(wrapper, el) ? el : null;
	};

	const unmark = () => {
		if (marked) marked.classList.remove(markedClass);
		marked = null;
		markedClass = null;
	};

	const reset = () => {
		unmark();
		kind = null;
		unit = null;
	};

	const onDragStart = e => {
		reset();
		for (const candidate of Object.values(DRAG_KINDS)) {
			const el = ownMatch(e.target, candidate.source);
			if (el) {
				kind = candidate;
				unit = candidate.unit(el);
				break;
			}
		}

		if (!kind || !e.dataTransfer) return;
		e.dataTransfer.effectAllowed = 'move';
		// Firefox starts no drag without data.
		e.dataTransfer.setData('text/plain', '');
	};

	const onDragOver = e => {
		if (!kind) return;
		const target = ownMatch(e.target, kind.target);

		// Nowhere to land, or back over the dragged header or row itself: dropping there moves nothing.
		if (!target || !unit || target === unit) {
			unmark();
			return;
		}

		e.preventDefault();
		if (e.dataTransfer) e.dataTransfer.dropEffect = 'move';

		// Moving on (right, or down) the dragged header or row lands after the target, moving back
		// it lands before it, so the marker goes on that edge.
		const after = (unit.compareDocumentPosition(target) & Node.DOCUMENT_POSITION_FOLLOWING) !== 0;
		const markClass = after ? kind.markerAfter : kind.marker;
		if (target === marked && markClass === markedClass) return;
		unmark();
		marked = target;
		markedClass = markClass;
		marked.classList.add(markClass);
	};

	// dragover stops once the pointer leaves the table, so the marker has to go here.
	const onDragLeave = e => {
		if (!marked) return;
		if (e.relatedTarget instanceof Node && wrapper.contains(e.relatedTarget)) return;
		unmark();
	};

	// Without preventDefault, Firefox opens the drag's text data as a link.
	const onDrop = e => {
		if (kind && ownMatch(e.target, kind.target)) e.preventDefault();
		reset();
	};

	// Also the end of a drag cancelled with Escape or dropped outside the table.
	const onDragEnd = () => reset();

	const listeners = {
		dragstart: onDragStart,
		dragover: onDragOver,
		dragleave: onDragLeave,
		drop: onDrop,
		dragend: onDragEnd
	};

	for (const [type, listener] of Object.entries(listeners)) {
		wrapper.addEventListener(type, listener);
	}

	wrapper[REORDER_DRAG_HANDLERS] = { listeners, reset };
}

/**
 * Stops marking drop targets, once neither columns nor rows can be dragged.
 * @param {HTMLElement} wrapper - The table wrapper element.
 */
export function disposeReorderDrag(wrapper) {
	const handlers = wrapper?.[REORDER_DRAG_HANDLERS];
	if (!handlers) return;

	handlers.reset();
	for (const [type, listener] of Object.entries(handlers.listeners)) {
		wrapper.removeEventListener(type, listener);
	}

	delete wrapper[REORDER_DRAG_HANDLERS];
}

/**
 * Moves DOM focus to a data cell by its row/column index.
 * @param {HTMLElement} wrapper - The table wrapper element.
 * @param {number} rowIndex - Zero-based index among the rendered rows (the current page, or
 *                            every row while the pager is hidden).
 * @param {number} colIndex - Zero-based visible column index.
 */
export function focusCell(wrapper, rowIndex, colIndex) {
	if (!wrapper) return;
	const [cell] = ownAll(wrapper, `tbody tr[data-row-index="${rowIndex}"] td[data-col-index="${colIndex}"]`);
	if (cell) cell.focus();
}

/**
 * Focuses and selects the inline edit input once it has been rendered.
 * @param {HTMLElement} wrapper - The table wrapper element.
 */
export function focusEditInput(wrapper) {
	if (!wrapper) return;
	const [input] = ownAll(wrapper, '.moka-table-edit-input');
	if (!input) return;
	input.focus();
	if (typeof input.select === 'function') input.select();
}

/**
 * Keeps a data cell in the tab order while Virtualize renders only some of the rows. The grid's
 * one tab stop is the cell that last had focus, and once its row scrolls out of the rendered
 * window no rendered cell has tabindex 0, so Tab would skip the table. Until the row comes back,
 * the first rendered cell stands in; it steps back as soon as the remembered cell renders again,
 * and becomes the real tab stop once it takes focus.
 * @param {HTMLElement} wrapper - The table wrapper element.
 */
export function watchTabStop(wrapper) {
	if (!wrapper || wrapper[TAB_STOP_WATCH]) return;

	let standIn = null;

	const check = () => {
		const cells = ownAll(wrapper, 'tbody td[data-col-index]');
		let stops = cells.filter(cell => cell.getAttribute('tabindex') === '0');

		if (standIn && (!standIn.isConnected || (stops.length > 1 && stops.includes(standIn)))) {
			if (standIn.isConnected) standIn.setAttribute('tabindex', '-1');
			stops = stops.filter(cell => cell !== standIn);
			standIn = null;
		}

		if (stops.length === 0 && cells.length > 0) {
			standIn = cells[0];
			standIn.setAttribute('tabindex', '0');
		}
	};

	// Focus makes the stand-in the remembered cell in .NET too, so it is a stand-in no longer.
	const onFocusIn = e => {
		if (e.target === standIn) standIn = null;
	};

	const observer = new MutationObserver(check);
	observer.observe(wrapper, { subtree: true, childList: true, attributes: true, attributeFilter: ['tabindex'] });
	wrapper.addEventListener('focusin', onFocusIn);
	check();

	wrapper[TAB_STOP_WATCH] = { observer, onFocusIn };
}

/**
 * Stops keeping a stand-in tab stop, once the table no longer virtualizes its rows.
 * @param {HTMLElement} wrapper - The table wrapper element.
 */
export function unwatchTabStop(wrapper) {
	const watch = wrapper?.[TAB_STOP_WATCH];
	if (!watch) return;

	watch.observer.disconnect();
	wrapper.removeEventListener('focusin', watch.onFocusIn);
	delete wrapper[TAB_STOP_WATCH];
}

/**
 * Sets the indeterminate flag on a checkbox. It is a property, not an attribute,
 * so Blazor markup cannot express it.
 * @param {HTMLInputElement} element - The checkbox.
 * @param {boolean} value - Whether the box shows the partial state.
 */
export function setIndeterminate(element, value) {
	if (element) element.indeterminate = !!value;
}

/**
 * Detaches every listener this module attached to the table.
 * @param {HTMLElement} wrapper - The table wrapper element.
 */
export function dispose(wrapper) {
	if (!wrapper) return;

	if (wrapper[GRID_KEY_HANDLER]) {
		wrapper.removeEventListener('keydown', wrapper[GRID_KEY_HANDLER]);
		delete wrapper[GRID_KEY_HANDLER];
	}

	disposeReorderDrag(wrapper);
	unwatchTabStop(wrapper);

	ownAll(wrapper, '.moka-table-resize-handle').forEach(handle => {
		if (handle[RESIZE_HANDLER]) {
			handle.removeEventListener('pointerdown', handle[RESIZE_HANDLER]);
			delete handle[RESIZE_HANDLER];
		}
	});
}
