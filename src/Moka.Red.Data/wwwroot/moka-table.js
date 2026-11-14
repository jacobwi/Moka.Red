import { open as popupOpen, close as popupClose } from '../Moka.Red.Core/moka-popup.js';
import { copyText } from '../Moka.Red.Core/moka-copy.js';

const RESIZE_HANDLER = '_mokaResizeHandler';
const GRID_KEY_HANDLER = '_mokaGridKeyHandler';
const REORDER_DRAG_HANDLERS = '_mokaReorderDragHandlers';
const TAB_STOP_WATCH = '_mokaTabStopWatch';
const PIN_WATCH = '_mokaPinWatch';
const POPUP_KEYS = '_mokaPopupKeys';
const RANGE = '_mokaRange';
const COLUMN_WATCH = '_mokaColumnWatch';
const FOCUS_WAIT = '_mokaFocusWait';
const OPENING_EDITOR = '_mokaOpeningEditor';

// What focusEditInput looks for in a cell's editor.
const EDITOR_CONTROL = 'input:not([type="hidden"]):not([disabled]), select:not([disabled]), textarea:not([disabled]), [role="slider"], button:not([disabled])';

// How long a key waits for a row or column the table has to draw first, scrolled into a virtualized window. Rows a
// server sends window by window can take a few round trips, so the wait is in time rather than in frames.
const DRAW_WAIT_MS = 5000;

// A heading's right edge on the keyboard.
const EDGE_STEP = 16;
const MIN_COLUMN_WIDTH = 50;

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
const NAV_KEYS = new Set(['ArrowUp', 'ArrowDown', 'ArrowLeft', 'ArrowRight', 'Home', 'End', 'PageUp', 'PageDown', ' ']);
const ARROWS = new Set(['ArrowUp', 'ArrowDown', 'ArrowLeft', 'ArrowRight']);

// A table in a detail row sits inside its parent table's wrapper. Every lookup and every key or
// drag event checks that an element belongs to this table, not to a table nested in it.
const belongsTo = (wrapper, el) => el instanceof Element && el.closest('.moka-table-wrapper') === wrapper;

const ownAll = (wrapper, selector) => [...wrapper.querySelectorAll(selector)].filter(el => belongsTo(wrapper, el));

/**
 * Saves a file the table wrote, from a stream .NET hands over, as ASP.NET Core's file download docs do: the bytes are read
 * into a blob and an anchor with download saves it.
 * @param {string} fileName - The name to save it under.
 * @param {object} contentStreamReference - The DotNetStreamReference holding the file.
 */
export async function downloadFile(fileName, contentStreamReference) {
	const arrayBuffer = await contentStreamReference.arrayBuffer();
	const blob = new Blob([arrayBuffer]);
	const url = URL.createObjectURL(blob);
	const anchor = document.createElement('a');
	anchor.href = url;
	anchor.download = fileName ?? '';
	anchor.click();
	anchor.remove();
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

	// What the render wrote. A drag that ends where it began sends .NET nothing, so nothing would write it again.
	const renderedWidth = th.style.width;
	const renderedMinWidth = th.style.minWidth;

	document.body.style.userSelect = 'none';
	document.body.style.cursor = 'col-resize';

	const iframes = document.querySelectorAll('iframe');
	iframes.forEach(f => (f.style.pointerEvents = 'none'));

	const widthAt = ev => Math.max(MIN_COLUMN_WIDTH, startWidth + ev.clientX - startX);

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
		th.style.width = renderedWidth;
		th.style.minWidth = renderedMinWidth;

		// The click that follows the drag would otherwise reach the header and toggle the sort.
		const swallowClick = clickEvent => {
			clickEvent.stopPropagation();
			clickEvent.preventDefault();
		};
		th.addEventListener('click', swallowClick, { capture: true, once: true });
		setTimeout(() => th.removeEventListener('click', swallowClick, { capture: true }), 0);

		// A press that moved nothing is half of a double click, and pins no width.
		if (colIndex >= 0 && Number.isFinite(colIndex) && Math.abs(finalWidth - startWidth) >= 1) {
			dotNetRef.invokeMethodAsync('OnColumnResized', colIndex, finalWidth);
		}
	};

	document.addEventListener('pointermove', onMove);
	document.addEventListener('pointerup', onUp);
}

// The grid's one tab stop. Cells are found by what the render wrote on them, so no state is kept here.
const moveTabStop = (wrapper, cell) => {
	ownAll(wrapper, 'tbody td[data-col-index][tabindex="0"]').forEach(other => {
		if (other !== cell) other.setAttribute('tabindex', '-1');
	});
	if (cell) cell.setAttribute('tabindex', '0');
};

const cellAt = (wrapper, rowIndex, colIndex) =>
	ownAll(wrapper, `tbody tr[data-row-index="${rowIndex}"] td[data-col-index="${colIndex}"]`)[0];

// Where a navigation key goes from this cell, or nothing at the edge of the grid. A row that is not
// rendered (scrolled out of a virtualized window, or in a collapsed group) cannot take focus. A cell
// that is drawn but not shown (a phone's hidden slot, HideOnMobile) can't either, so sideways skips it.
const nextCell = (wrapper, cell, key) => {
	const row = cell.closest('tr');
	const rowIndex = Number(row.dataset.rowIndex);
	const colIndex = Number(cell.dataset.colIndex);
	const rowCells = [...row.querySelectorAll(':scope > td[data-col-index]')]
		.filter(other => other.getClientRects().length > 0);
	const position = rowCells.indexOf(cell);
	if (wrapper[COLUMN_WATCH] && key !== 'ArrowDown' && key !== 'ArrowUp') {
		return windowedStep(wrapper, cell, key)?.cell;
	}

	switch (key) {
		case 'ArrowDown': return cellAt(wrapper, rowIndex + 1, colIndex);
		case 'ArrowUp': return cellAt(wrapper, rowIndex - 1, colIndex);
		case 'ArrowRight': return rowCells[position + 1];
		case 'ArrowLeft': return rowCells[position - 1];
		case 'Home': return rowCells[0];
		case 'End': return rowCells[rowCells.length - 1];
		default: return undefined;
	}
};

const tableOf = wrapper => ownAll(wrapper, 'table.moka-table')[0];

// How many rows the table has to move through: every row of the page, or under remote virtualization of the whole set.
const rowCountOf = wrapper => Number(tableOf(wrapper)?.dataset.rowCount ?? 0);

const columnCountOf = wrapper => Number(tableOf(wrapper)?.getAttribute('aria-colcount') ?? 0);

// The rows a Page Up or Page Down moves: as many as the scroll area shows, less one so a row stays in view.
const pageRows = (wrapper, cell) => {
	const container = tableOf(wrapper)?.parentElement;
	const scrolls = container && container.scrollHeight > container.clientHeight + 1;
	const view = scrolls ? container.clientHeight : window.innerHeight;
	const height = cell.closest('tr').getBoundingClientRect().height || 1;
	return Math.max(1, Math.floor(view / height) - 1);
};

// The last column a row can take focus in: the highest data-col-index among the headings.
const lastColumnOf = wrapper => {
	const heads = ownAll(wrapper, 'thead th[data-col-index], tbody tr[data-row-index] > td[data-col-index]');
	return heads.reduce((last, el) => Math.max(last, Number(el.dataset.colIndex)), 0);
};

/**
 * Focuses a cell, drawing it first when it sits outside a virtualized window: the scroll area moves toward the row, or
 * the column's heading, and the cell takes focus once the render that draws it has landed.
 * @param {HTMLElement} wrapper - The table wrapper element.
 * @param {number} rowIndex - The row's data-row-index.
 * @param {number} colIndex - The cell's data-col-index.
 */
export function focusCellWhenDrawn(wrapper, rowIndex, colIndex) {
	if (!wrapper) return;
	const found = cellAt(wrapper, rowIndex, colIndex);
	if (found) {
		found.focus();
		found.scrollIntoView({ block: 'nearest', inline: 'nearest' });
		return;
	}

	aimAt(wrapper, rowIndex, colIndex);
	waitForCell(wrapper, rowIndex, colIndex);
}

// Scrolls toward a row that isn't drawn and the column's heading. The distance is reckoned from the nearest row that is
// drawn, at its height, so a jump over many rows lands near where Virtualize placed them; Virtualize's own row height
// settles as rows are measured, so the wait aims again from each render until the row is drawn.
function aimAt(wrapper, rowIndex, colIndex) {
	const table = tableOf(wrapper);
	const container = table?.parentElement;
	if (!container) return;
	const drawn = ownAll(wrapper, 'tbody tr[data-row-index]');
	if (drawn.length > 0) {
		const first = Number(drawn[0].dataset.rowIndex);
		const last = Number(drawn[drawn.length - 1].dataset.rowIndex);
		if (rowIndex < first || rowIndex > last) {
			const near = rowIndex < first ? drawn[0] : drawn[drawn.length - 1];
			const box = near.getBoundingClientRect();
			container.scrollTop = scrollTowardRow({
				nearTop: box.top - container.getBoundingClientRect().top + container.scrollTop,
				nearIndex: Number(near.dataset.rowIndex),
				rowHeight: box.height || 36,
				rowIndex,
				view: container.clientHeight - (table.tHead?.getBoundingClientRect().height ?? 0)
			});
		}
	}

	const heading = ownAll(wrapper, `thead th[data-col-index="${colIndex}"]`)[0];
	if (heading) {
		const left = heading.offsetLeft;
		if (left < container.scrollLeft || left + heading.offsetWidth > container.scrollLeft + container.clientWidth) {
			container.scrollLeft = Math.max(0, left - container.clientWidth / 2);
		}
	}
}

/**
 * The scroll that brings a row that isn't drawn to the middle of the view, reckoned from a drawn row near it. Exported
 * for the tests, which run it in Node.
 * @param {{nearTop: number, nearIndex: number, rowHeight: number, rowIndex: number, view: number}} at - Where the drawn
 *     row sits in the scroll, its number and height, the row wanted, and the height of the view under the header.
 * @returns {number} The scroll top.
 */
export function scrollTowardRow({ nearTop, nearIndex, rowHeight, rowIndex, view }) {
	return Math.max(0, nearTop + (rowIndex - nearIndex) * rowHeight - view / 2);
}

// Watches the table for the cell to be drawn. A newer key replaces the wait, and focus someone has put elsewhere in the
// meantime stays there: the cell the key left may have scrolled out of the window, which drops focus to the page.
function waitForCell(wrapper, rowIndex, colIndex) {
	stopWaitingForCell(wrapper);
	const wait = { observer: null, timer: 0 };
	const land = () => {
		const cell = cellAt(wrapper, rowIndex, colIndex);
		if (!cell) {
			aimAt(wrapper, rowIndex, colIndex);
			return;
		}

		stopWaitingForCell(wrapper);
		const active = document.activeElement;
		if (active && active !== document.body && !wrapper.contains(active)) return;
		cell.focus();
		cell.scrollIntoView({ block: 'nearest', inline: 'nearest' });
	};

	wait.observer = new MutationObserver(land);
	wait.observer.observe(wrapper, { childList: true, subtree: true });
	wait.timer = setTimeout(() => stopWaitingForCell(wrapper), DRAW_WAIT_MS);
	wrapper[FOCUS_WAIT] = wait;
}

function stopWaitingForCell(wrapper) {
	const wait = wrapper[FOCUS_WAIT];
	if (!wait) return;
	wait.observer.disconnect();
	clearTimeout(wait.timer);
	delete wrapper[FOCUS_WAIT];
}

// Page Up, Page Down, and Ctrl with Home or End: rows that may not be drawn, so they go through focusCellWhenDrawn.
const farCell = (wrapper, cell, e) => {
	const rowIndex = Number(cell.closest('tr').dataset.rowIndex);
	const colIndex = Number(cell.dataset.colIndex);
	const rows = rowCountOf(wrapper);
	const firstRow = Number(ownAll(wrapper, 'tbody tr[data-row-index]')[0]?.dataset.rowIndex ?? 0);
	const base = rows > 0 && firstRow >= rows ? firstRow : 0;
	const lastRow = Math.max(base, base + rows - 1);
	switch (e.key) {
		case 'PageDown': return [Math.min(lastRow, rowIndex + pageRows(wrapper, cell)), colIndex];
		case 'PageUp': return [Math.max(base, rowIndex - pageRows(wrapper, cell)), colIndex];
		case 'Home': return e.ctrlKey || e.metaKey ? [base, 0] : null;
		case 'End': return e.ctrlKey || e.metaKey ? [lastRow, lastColumnOf(wrapper)] : null;
		default: return null;
	}
};

// The edges that take keys: only a table people arrange makes them focusable.
const ownEdge = (wrapper, node) => {
	const edge = node instanceof Element ? node.closest('.moka-table-resize-handle[tabindex]') : null;
	return belongsTo(wrapper, edge) ? edge : null;
};

const onEdgeKey = (e, edge, dotNetRef) => {
	if (!['ArrowLeft', 'ArrowRight', 'Home'].includes(e.key)) return;
	e.preventDefault();

	const colIndex = Number(edge.dataset.colIndex);
	if (e.key === 'Home') {
		dotNetRef.invokeMethodAsync('OnColumnWidthReset', colIndex);
		return;
	}

	const step = e.key === 'ArrowRight' ? EDGE_STEP : -EDGE_STEP;
	const width = Math.max(MIN_COLUMN_WIDTH, edge.closest('th').offsetWidth + step);
	dotNetRef.invokeMethodAsync('OnColumnResized', colIndex, width);
};

/**
 * The grid's keyboard, in one listener rather than a Blazor handler on every cell, which every
 * render of the table replaced and sent to the browser. Arrows, Home and End move focus here and
 * never reach .NET. Enter on an editable cell and Space on a selectable row call OnCellKey. Enter on
 * a cell marked data-activates-row clicks the cell, which raises the row's OnRowClick the way a
 * mouse click does. Only a key pressed on the cell itself counts, so a control inside the cell keeps
 * its keys. A key in a table nested in a detail row bubbles up to this wrapper too; that table
 * handles it, so this one leaves it alone. Focus moves the tab stop and tells .NET through
 * OnCellFocused, which renders nothing. Left, Right and Home on a focusable heading edge resize its
 * column, and a double click on one gives the column its own width back.
 * @param {HTMLElement} wrapper - The table wrapper element.
 * @param {object} dotNetRef - The table.
 */
export function initGridKeys(wrapper, dotNetRef) {
	if (!wrapper || wrapper[GRID_KEY_HANDLER]) return;

	const onKeyDown = e => {
		const edge = ownEdge(wrapper, e.target);
		if (edge) {
			onEdgeKey(e, edge, dotNetRef);
			return;
		}

		const opening = wrapper[OPENING_EDITOR];
		if (opening && e.target === opening.cell && holdForEditor(e, opening)) return;

		const editing = e.target instanceof Element ? e.target.closest('td[data-editing], tr[data-new-row]') : null;
		if (belongsTo(wrapper, editing)) {
			onEditorKey(e, editing, dotNetRef);
			return;
		}

		const cell = e.target;
		if (!belongsTo(wrapper, cell) || !cell.matches('td[data-col-index]')) return;

		const rowIndex = Number(cell.closest('tr').dataset.rowIndex);
		const colIndex = Number(cell.dataset.colIndex);
		const mod = e.ctrlKey || e.metaKey;

		if (mod && !e.altKey && !e.shiftKey && e.key.toLowerCase() === 'z' && cell.closest('table')?.hasAttribute('data-moka-undo')) {
			e.preventDefault();
			dotNetRef.invokeMethodAsync('OnCellKey', rowIndex, colIndex, 'Undo');
			return;
		}

		if (mod && !e.altKey && e.key.toLowerCase() === 'c' && copyFromGrid(wrapper, cell, dotNetRef)) {
			e.preventDefault();
			return;
		}

		if (e.key === 'F2' && cell.dataset.editable === 'true' && !mod && !e.altKey && !e.shiftKey) {
			e.preventDefault();
			openEditor(wrapper, cell, rowIndex, colIndex, 'F2', dotNetRef);
			return;
		}

		if (e.key === 'Escape' && wrapper[RANGE]?.anchor) {
			clearRange(wrapper);
			return;
		}

		if (e.key === 'Enter') {
			if (e.repeat || e.altKey || e.ctrlKey || e.metaKey || e.shiftKey || e.isComposing) return;

			if (cell.dataset.activatesRow === 'true') {
				e.preventDefault();
				cell.click();
			} else if (cell.dataset.editable === 'true') {
				e.preventDefault();
				openEditor(wrapper, cell, rowIndex, colIndex, e.key, dotNetRef);
			}

			return;
		}

		if (!NAV_KEYS.has(e.key)) return;
		e.preventDefault();

		if (e.key === ' ') {
			if (cell.closest('tr').querySelector(':scope > td.moka-table-cell--checkbox')) {
				dotNetRef.invokeMethodAsync('OnCellKey', rowIndex, colIndex, e.shiftKey ? 'Shift+ ' : e.key);
			}

			return;
		}

		const ranges = wrapper[RANGE];
		if (ranges && ARROWS.has(e.key)) {
			if (e.shiftKey) {
				ranges.anchor ??= { row: rowIndex, col: colIndex };
			} else {
				clearRange(wrapper);
			}
		}

		const far = farCell(wrapper, cell, e);
		if (far) {
			focusCellWhenDrawn(wrapper, far[0], far[1]);
			return;
		}

		const next = nextCell(wrapper, cell, e.key) ?? edgeStep(wrapper, cell, e.key);
		next?.focus();
		if (ranges && e.shiftKey && ARROWS.has(e.key) && next) {
			ranges.focus = { row: Number(next.closest('tr').dataset.rowIndex), col: Number(next.dataset.colIndex) };
			markRange(wrapper);
		}
	};

	// Focus moving off the open cell, by a click elsewhere, saves it in cell mode. A render that removes the editor, or the
	// window losing focus, isn't someone leaving the cell, so the check waits a frame. The call names the cell, so one
	// that lands after Tab or Enter has already moved the edit on leaves the next cell's editor alone.
	const onFocusOut = e => {
		// Keys wait for an editor only while its cell keeps focus.
		const opening = wrapper[OPENING_EDITOR];
		if (opening?.cell === e.target && !(e.relatedTarget instanceof Node && opening.cell.contains(e.relatedTarget))) {
			wrapper[OPENING_EDITOR] = null;
		}

		const editing = e.target instanceof Element ? e.target.closest('td[data-editing="cell"]') : null;
		if (!belongsTo(wrapper, editing)) return;
		if (e.relatedTarget instanceof Node && editing.contains(e.relatedTarget)) return;
		const rowIndex = Number(editing.closest('tr').dataset.rowIndex);
		const colIndex = Number(editing.dataset.colIndex);
		requestAnimationFrame(() => {
			if (!editing.isConnected || editing.dataset.editing !== 'cell' || editing.contains(document.activeElement)) return;
			dotNetRef.invokeMethodAsync('OnEditorLeft', rowIndex, colIndex);
		});
	};

	const onFocusIn = e => {
		const cell = e.target instanceof Element ? e.target.closest('td[data-col-index]') : null;
		if (!belongsTo(wrapper, cell)) return;

		// A cell taking focus, by a click or a key, ends a wait for one still to be drawn.
		stopWaitingForCell(wrapper);

		// Focus moving between controls inside one cell is not a new cell.
		if (e.relatedTarget instanceof Node && cell.contains(e.relatedTarget)) return;

		moveTabStop(wrapper, cell);
		dotNetRef.invokeMethodAsync(
			'OnCellFocused', Number(cell.closest('tr').dataset.rowIndex), Number(cell.dataset.colIndex));
	};

	const onDoubleClick = e => {
		const edge = ownEdge(wrapper, e.target);
		if (edge) dotNetRef.invokeMethodAsync('OnColumnWidthReset', Number(edge.dataset.colIndex));
	};

	// One listener for every control a column kind marks with data-moka-act, in every row: a click, or Enter or Space
	// on the button, which the browser turns into a click. It stops at the table, so the row's own click doesn't run,
	// and tells .NET the row and the action. A keyboard click reports no position and a detail of 0.
	const onClick = e => {
		const control = e.target instanceof Element ? e.target.closest('[data-moka-act]') : null;
		if (!belongsTo(wrapper, control) || control.disabled) return;
		const row = control.closest('tr');
		const ref = row?.dataset.rowIndex ?? row?.dataset.pinnedRow ?? row?.dataset.newRow;
		if (ref === undefined) return;
		e.stopPropagation();
		dotNetRef.invokeMethodAsync('OnCellAction', String(ref), control.dataset.mokaAct, e.clientX, e.clientY, e.detail);
	};

	wrapper.addEventListener('keydown', onKeyDown);
	wrapper.addEventListener('focusin', onFocusIn);
	wrapper.addEventListener('focusout', onFocusOut);
	wrapper.addEventListener('dblclick', onDoubleClick);
	wrapper.addEventListener('click', onClick);
	wrapper[GRID_KEY_HANDLER] = { onKeyDown, onFocusIn, onFocusOut, onDoubleClick, onClick };
}

/**
 * Puts the tab stop on the cell .NET remembers, after a render that followed a move of it here.
 * @param {HTMLElement} wrapper - The table wrapper element.
 * @param {number} rowIndex - The row's data-row-index.
 * @param {number} colIndex - The cell's data-col-index.
 */
export function setTabStop(wrapper, rowIndex, colIndex) {
	if (wrapper) moveTabStop(wrapper, cellAt(wrapper, rowIndex, colIndex));
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
	cellAt(wrapper, rowIndex, colIndex)?.focus();
}

/**
 * Focuses the open editor once it has been drawn, its text selected: the one in the given column, or the row's first.
 * Keys typed on its cell while it was drawn then reach it as if typed there.
 * @param {HTMLElement} wrapper - The table wrapper element.
 * @param {number} colIndex - The column's data-col-index, or -1 for the first editor.
 */
export function focusEditInput(wrapper, colIndex) {
	if (!wrapper) return;
	const opening = wrapper[OPENING_EDITOR];
	wrapper[OPENING_EDITOR] = null;
	const cells = ownAll(wrapper, 'td[data-editing]');
	const cell = cells.find(td => colIndex >= 0 && Number(td.dataset.colIndex) === colIndex) ?? cells[0];
	const control = cell?.querySelector(EDITOR_CONTROL);
	if (!control) return;
	control.focus();
	if (typeof control.select === 'function' && control.type !== 'checkbox') control.select();

	// Keys typed on another cell were meant for an editor that didn't open, such as when the open one couldn't be saved.
	if (opening?.cell === cell) handHeldKeys(wrapper, control, opening);
}

// Enter or F2 on an editable cell. The editor is drawn a round trip later, and keys typed meanwhile would land on the
// cell, so they wait for it until focusEditInput hands them over, focus leaves the cell, or .NET says none opened.
async function openEditor(wrapper, cell, rowIndex, colIndex, key, dotNetRef) {
	const opening = { cell, keys: [], end: null };
	wrapper[OPENING_EDITOR] = opening;
	let opened = false;
	try {
		opened = await dotNetRef.invokeMethodAsync('OnCellKey', rowIndex, colIndex, key);
	} finally {
		if (!opened && wrapper[OPENING_EDITOR] === opening) wrapper[OPENING_EDITOR] = null;
	}
}

/**
 * What a key pressed on a cell whose editor is opening does: 'type' (a character or Backspace) goes into the editor,
 * 'end' (Enter, Escape, Tab) reaches it after the typing, 'stay' is a key that would move focus off the cell or open the
 * editor again, and null is one the cell handles as usual. Exported for the tests, which run it in Node.
 * @param {{ key: string, ctrlKey?: boolean, metaKey?: boolean, altKey?: boolean, shiftKey?: boolean, repeat?: boolean,
 *           isComposing?: boolean, getModifierState?: (key: string) => boolean }} e - The keydown event.
 * @returns {'type' | 'end' | 'stay' | null}
 */
export function heldKeyKind(e) {
	const altGraph = typeof e.getModifierState === 'function' && e.getModifierState('AltGraph');
	if (e.isComposing || !(altGraph || !(e.ctrlKey || e.metaKey || e.altKey))) return null;
	if ([...e.key].length === 1 || e.key === 'Backspace') return 'type';
	const ends = e.key === 'Escape' || e.key === 'Tab' || (e.key === 'Enter' && !e.shiftKey);
	if (ends && !e.repeat) return 'end';
	return ends || e.key === 'F2' || e.key === 'Enter' || NAV_KEYS.has(e.key) ? 'stay' : null;
}

/**
 * Text as it reads after held keys are typed into it over a selection, the way an input edits it.
 * Exported for the tests, which run it in Node.
 * @param {string} value - The text.
 * @param {number} start - Where the selection starts.
 * @param {number} end - Where it ends.
 * @param {string[]} keys - Characters, and 'Backspace'.
 * @returns {{ value: string, caret: number }}
 */
export function typeHeldKeys(value, start, end, keys) {
	for (const key of keys) {
		const from = key === 'Backspace' && start === end ? Math.max(0, start - 1) : start;
		const text = key === 'Backspace' ? '' : key;
		value = value.slice(0, from) + text + value.slice(end);
		start = end = from + text.length;
	}

	return { value, caret: start };
}

// A key held for the opening editor. Keys after the one that ends the typing have no editor left to go to.
function holdForEditor(e, opening) {
	const kind = heldKeyKind(e);
	if (!kind) return false;
	e.preventDefault();
	if (opening.end) return true;
	if (kind === 'type') opening.keys.push(e.key);
	else if (kind === 'end') opening.end = e.key === 'Tab' && e.shiftKey ? 'Shift+Tab' : e.key;
	return true;
}

// The held keys, as the editor would have taken them: text over the selection, the first letters of a list's choice,
// Space on a check box. The key that ended them goes to the grid's own editor keys, and in a whole-row edit Tab moves on
// to the row's next editor as it would.
function handHeldKeys(wrapper, control, opening) {
	const { keys, end } = opening;
	if (end !== 'Escape' && keys.length > 0) {
		if ((control instanceof HTMLTextAreaElement || (control instanceof HTMLInputElement && control.selectionStart !== null)) && !control.readOnly) {
			const typed = typeHeldKeys(control.value, control.selectionStart, control.selectionEnd, keys);
			control.value = typed.value;
			control.setSelectionRange(typed.caret, typed.caret);
			control.dispatchEvent(new Event('input', { bubbles: true }));
		} else if (control instanceof HTMLSelectElement) {
			const typed = keys.filter(key => key !== 'Backspace').join('').toLocaleLowerCase();
			const option = typed ? [...control.options].find(o => !o.disabled && o.text.toLocaleLowerCase().startsWith(typed)) : null;
			if (option && option.value !== control.value) {
				control.value = option.value;
				control.dispatchEvent(new Event('change', { bubbles: true }));
			}
		} else if (control instanceof HTMLInputElement && control.type === 'checkbox' && keys.filter(key => key === ' ').length % 2 === 1) {
			control.click();
		}
	}

	if (!end) return;
	const back = end === 'Shift+Tab';
	if ((end === 'Tab' || back) && opening.cell.dataset.editing !== 'cell') {
		const controls = ownAll(wrapper, 'td[data-editing]').map(td => td.querySelector(EDITOR_CONTROL)).filter(Boolean);
		controls[controls.indexOf(control) + (back ? -1 : 1)]?.focus();
		return;
	}

	control.dispatchEvent(new KeyboardEvent('keydown', { key: back ? 'Tab' : end, shiftKey: back, bubbles: true, cancelable: true }));
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
 * Moves focus to the first control one of the selectors finds, in the order given.
 * @param {HTMLElement} wrapper - The table wrapper element.
 * @param {string[]} selectors - What to try.
 */
export function focusFirst(wrapper, selectors) {
	if (!wrapper) return;
	for (const selector of selectors) {
		const [target] = ownAll(wrapper, selector);
		if (target) {
			target.focus();
			return;
		}
	}
}

/**
 * Sets a row's selection box. After a shift-click the browser has flipped the clicked box, and when
 * the run leaves that row as the markup already had it, Blazor finds nothing to send.
 * @param {HTMLElement} wrapper - The table wrapper element.
 * @param {number} rowIndex - The row's data-row-index.
 * @param {boolean} checked - What the box should say.
 */
export function setRowBox(wrapper, rowIndex, checked) {
	if (!wrapper) return;
	const [box] = ownAll(wrapper, `tbody tr[data-row-index="${rowIndex}"] > td.moka-table-cell--checkbox > input`);
	if (box) box.checked = !!checked;
}

/**
 * Detaches every listener this module attached to the table.
 * @param {HTMLElement} wrapper - The table wrapper element.
 */
export function dispose(wrapper) {
	if (!wrapper) return;

	if (wrapper[GRID_KEY_HANDLER]) {
		wrapper.removeEventListener('keydown', wrapper[GRID_KEY_HANDLER].onKeyDown);
		wrapper.removeEventListener('focusin', wrapper[GRID_KEY_HANDLER].onFocusIn);
		wrapper.removeEventListener('focusout', wrapper[GRID_KEY_HANDLER].onFocusOut);
		wrapper.removeEventListener('dblclick', wrapper[GRID_KEY_HANDLER].onDoubleClick);
		wrapper.removeEventListener('click', wrapper[GRID_KEY_HANDLER].onClick);
		delete wrapper[GRID_KEY_HANDLER];
	}

	stopWaitingForCell(wrapper);
	releasePins(wrapper);
	unbindRanges(wrapper);
	unwatchColumns(wrapper);
	disposeReorderDrag(wrapper);
	unwatchTabStop(wrapper);
	unobserveEnd(wrapper);

	ownAll(wrapper, '.moka-table-resize-handle').forEach(handle => {
		if (handle[RESIZE_HANDLER]) {
			handle.removeEventListener('pointerdown', handle[RESIZE_HANDLER]);
			delete handle[RESIZE_HANDLER];
		}
	});
}

// ── Endless scroll ──
//
// Every table under the endless pager shares one IntersectionObserver, as MokaReveal's elements do. Scrolling sends
// nothing to .NET: the observer tells .NET once when the end of the rows comes into view, then stops watching that
// element until .NET watches it again after the rows it asked for arrive.

const endWatchers = new WeakMap();
let endObserver = null;

function endObserverFor() {
	endObserver ??= new IntersectionObserver(entries => {
		for (const entry of entries) {
			if (!entry.isIntersecting) continue;
			const dotNetRef = endWatchers.get(entry.target);
			if (!dotNetRef) continue;
			endWatchers.delete(entry.target);
			endObserver.unobserve(entry.target);
			dotNetRef.invokeMethodAsync('OnEndReached').catch(() => { });
		}
	}, { rootMargin: '0px 0px 240px 0px' });
	return endObserver;
}

/**
 * Watches the end of a table's rows and calls OnEndReached once when it comes into view. No root is given, so it works
 * whether the table, a wrapper or the page scrolls.
 * @param {HTMLElement} sentinel - The element after the rows.
 * @param {object} dotNetRef - The table.
 */
export function observeEnd(sentinel, dotNetRef) {
	if (!sentinel) return;
	const observer = endObserverFor();
	observer.unobserve(sentinel);
	endWatchers.set(sentinel, dotNetRef);
	observer.observe(sentinel);
}

/**
 * Stops watching the end of a table's rows.
 * @param {HTMLElement} wrapper - The table wrapper element.
 */
export function unobserveEnd(wrapper) {
	if (!wrapper || !endObserver) return;
	ownAll(wrapper, '[data-moka-endless]').forEach(sentinel => {
		endWatchers.delete(sentinel);
		endObserver.unobserve(sentinel);
	});
}

// ── Popups: the column menu and the filter popup ──

const MENU_ITEMS = '[role="menuitem"], [role="menuitemradio"], [role="menuitemcheckbox"]';

/**
 * Shows a popup in the top layer against the control that opened it, found inside the table, and takes its keys:
 * Escape closes it; in a menu the arrows, Home, End and a letter move between items and Tab closes it, as the WAI-ARIA
 * menu button pattern says. Focus goes to the first item, or the dialog's first field. Moving between items sends
 * nothing to .NET.
 * @param {string} key - Unique on the page; closePopup() takes the same key.
 * @param {HTMLElement} panel - The popup, rendered with popover="manual" and data-moka-popup.
 * @param {HTMLElement} backdrop - The outside-click layer under it.
 * @param {HTMLElement} wrapper - The table wrapper element.
 * @param {string} anchorSelector - The control it opened from, inside the table.
 * @param {string} placement - Where it sits against the control, as moka-popup.js names it.
 * @param {object} dotNetRef - The popup's component, whose Dismiss() closes it.
 * @param {boolean} menu - Whether it is a menu rather than a dialog.
 */
export function openPopup(key, panel, backdrop, wrapper, anchorSelector, placement, dotNetRef, menu) {
	if (!panel) return;
	const [anchor] = wrapper && anchorSelector ? ownAll(wrapper, anchorSelector) : [];
	popupOpen(key, panel, anchor ?? null, backdrop ?? null, { placement });

	const items = () => [...panel.querySelectorAll(MENU_ITEMS)].filter(item => !item.disabled);
	const dismiss = e => {
		e.preventDefault();
		e.stopPropagation();
		dotNetRef.invokeMethodAsync('Dismiss');
	};

	const onKeyDown = e => {
		if (e.key === 'Escape') {
			dismiss(e);
			return;
		}

		if (!menu) return;
		if (e.key === 'Tab') {
			dismiss(e);
			return;
		}

		const list = items();
		if (list.length === 0) return;
		const at = list.indexOf(document.activeElement);
		let next;
		switch (e.key) {
			case 'ArrowDown': next = list[(at + 1) % list.length]; break;
			case 'ArrowUp': next = list[(at - 1 + list.length) % list.length]; break;
			case 'Home': next = list[0]; break;
			case 'End': next = list[list.length - 1]; break;
			default:
				if (e.key.length === 1 && !e.ctrlKey && !e.metaKey && !e.altKey) {
					const letter = e.key.toLowerCase();
					next = [...list.slice(at + 1), ...list.slice(0, at + 1)]
						.find(item => item.textContent.trim().toLowerCase().startsWith(letter));
				}
		}

		if (next) {
			e.preventDefault();
			e.stopPropagation();
			next.focus();
		}
	};

	panel.addEventListener('keydown', onKeyDown);
	panel[POPUP_KEYS] = onKeyDown;

	const first = menu
		? items()[0]
		: panel.querySelector('input:not([disabled]), select:not([disabled]), textarea:not([disabled]), button:not([disabled])');
	first?.focus({ preventScroll: true });
}

/**
 * Takes a popup out of the top layer and stops taking its keys.
 * @param {string} key - The key openPopup() took.
 * @param {HTMLElement} panel - The popup.
 */
export function closePopup(key, panel) {
	popupClose(key);
	if (panel?.[POPUP_KEYS]) {
		panel.removeEventListener('keydown', panel[POPUP_KEYS]);
		delete panel[POPUP_KEYS];
	}
}

// ── Pinned columns ──
//
// A pinned cell sits at inset-inline-start (or -end) var(--_pin-N), N its position. The offsets are the widths of the
// pinned columns before it, measured here and written on the table, and measured again by a ResizeObserver whenever a
// pinned column changes width, so .NET calls this once per layout change and scrolling sends it nothing. The scroll
// container carries data-moka-scrolled-start and -end while there is more to see on that side, for the shadows.

const pinnedCells = table => {
	const head = [...table.querySelectorAll(':scope > thead > tr > th[data-pin-at]')];
	if (head.length > 0) {
		return head.map(cell => ({ cell, at: Number(cell.dataset.pinAt), end: cell.classList.contains('moka-table-cell--pin-end') }));
	}

	const row = table.querySelector(':scope > tbody > tr[data-row-index]');
	if (!row) return [];
	return [...row.querySelectorAll(':scope > td.moka-table-cell--pin-start, :scope > td.moka-table-cell--pin-end')]
		.map(cell => ({ cell, at: Number(cell.dataset.colIndex), end: cell.classList.contains('moka-table-cell--pin-end') }));
};

const markEdges = watch => {
	const container = watch.container;
	if (!container) return;
	const scrolled = Math.abs(container.scrollLeft);
	const room = container.scrollWidth - container.clientWidth;
	container.toggleAttribute('data-moka-scrolled-start', scrolled > 0.5);
	container.toggleAttribute('data-moka-scrolled-end', room - scrolled > 0.5);
};

const placePins = watch => {
	const table = watch.table;
	if (!table?.isConnected) return;
	const cells = pinnedCells(table);
	const names = [];
	const write = list => {
		let offset = 0;
		for (const pinned of list) {
			const name = `--_pin-${pinned.at}`;
			table.style.setProperty(name, `${offset}px`);
			names.push(name);
			offset += pinned.cell.getBoundingClientRect().width;
		}
	};

	write(cells.filter(pinned => !pinned.end));
	write(cells.filter(pinned => pinned.end).reverse());
	for (const old of watch.names) {
		if (!names.includes(old)) table.style.removeProperty(old);
	}

	watch.names = names;
	markEdges(watch);
	return cells;
};

/**
 * Measures the pinned columns' offsets and keeps them right as widths change. Call it after a render that changed
 * which columns are pinned, their order, their widths or the table element itself.
 * @param {HTMLElement} wrapper - The table wrapper element.
 */
export function measurePins(wrapper) {
	if (!wrapper) return;
	const [table] = ownAll(wrapper, 'table.moka-table');
	if (!table) return;

	let watch = wrapper[PIN_WATCH];
	if (!watch) {
		watch = { table: null, container: null, names: [], onScroll: null, observer: null };
		watch.observer = new ResizeObserver(() => placePins(watch));
		watch.onScroll = () => markEdges(watch);
		wrapper[PIN_WATCH] = watch;
	}

	const container = table.parentElement;
	if (watch.container !== container) {
		watch.container?.removeEventListener('scroll', watch.onScroll);
		container?.addEventListener('scroll', watch.onScroll, { passive: true });
		watch.container = container;
	}

	watch.table = table;
	watch.observer.disconnect();
	const cells = placePins(watch) ?? [];
	cells.forEach(pinned => watch.observer.observe(pinned.cell));
	if (container) watch.observer.observe(container);
}

/**
 * Stops keeping the pinned offsets, once no column is pinned or the table goes away.
 * @param {HTMLElement} wrapper - The table wrapper element.
 */
export function releasePins(wrapper) {
	const watch = wrapper?.[PIN_WATCH];
	if (!watch) return;
	watch.observer.disconnect();
	watch.container?.removeEventListener('scroll', watch.onScroll);
	watch.container?.removeAttribute('data-moka-scrolled-start');
	watch.container?.removeAttribute('data-moka-scrolled-end');
	for (const name of watch.names) watch.table?.style.removeProperty(name);
	delete wrapper[PIN_WATCH];
}

/**
 * The width a column needs to show every value on one line: the widest of its heading and its cells, padding
 * included. The cells are measured unwrapped and put back as they were.
 * @param {HTMLElement} wrapper - The table wrapper element.
 * @param {number} position - The column's data-col-index.
 * @param {string} headSelector - The column's heading, inside the table.
 * @returns {number} The width in pixels, or 0 when nothing was found.
 */
export function fitColumn(wrapper, position, headSelector) {
	if (!wrapper) return 0;
	const cells = ownAll(wrapper, `tbody tr[data-row-index] > td[data-col-index="${position}"]`);
	const [head] = headSelector ? ownAll(wrapper, headSelector) : [];
	if (head) cells.push(head);
	if (cells.length === 0) return 0;

	const range = document.createRange();
	let widest = 0;
	for (const cell of cells) {
		const wrap = cell.style.whiteSpace;
		cell.style.whiteSpace = 'nowrap';
		range.selectNodeContents(cell);
		const style = getComputedStyle(cell);
		const frame = parseFloat(style.paddingLeft) + parseFloat(style.paddingRight)
			+ parseFloat(style.borderLeftWidth) + parseFloat(style.borderRightWidth);
		widest = Math.max(widest, range.getBoundingClientRect().width + frame);
		cell.style.whiteSpace = wrap;
	}

	return Math.ceil(widest) + 1;
}

// ── Kept state ──

/**
 * Reads a table's kept state from local storage.
 * @param {string} key - The storage key.
 * @returns {string|null} The kept JSON, or null when there is none or storage is blocked.
 */
export function readState(key) {
	try {
		return window.localStorage.getItem(key);
	} catch {
		return null;
	}
}

/**
 * Keeps a table's state in local storage. Blocked or full storage keeps nothing, and the table works on.
 * @param {string} key - The storage key.
 * @param {string} json - The state as JSON.
 */
export function writeState(key, json) {
	try {
		window.localStorage.setItem(key, json);
	} catch {
		// Private windows and full storage refuse; the state then lasts as long as the page.
	}
}

// ── Editors ──

// Keys in an open editor. Values reach .NET on each input event, before the key that follows them, so a save sees what
// was typed. A key the editor already handled itself (a date picker closing its calendar) is left alone.
function onEditorKey(e, editing, dotNetRef) {
	if (e.defaultPrevented || e.isComposing) return;
	if (e.key === 'Escape') {
		e.preventDefault();
		dotNetRef.invokeMethodAsync('OnEditorKey', 'Escape');
	} else if (e.key === 'Enter' && !e.shiftKey && !e.altKey && !e.ctrlKey && !e.metaKey && !(e.target instanceof HTMLTextAreaElement)) {
		e.preventDefault();
		dotNetRef.invokeMethodAsync('OnEditorKey', 'Enter');
	} else if (e.key === 'Tab' && editing.dataset.editing === 'cell') {
		e.preventDefault();
		dotNetRef.invokeMethodAsync('OnEditorKey', e.shiftKey ? 'Shift+Tab' : 'Tab');
	}
}

// Sideways under column virtualization goes by column number, since the row's cells skip the columns out of view: the
// first shown cell that way, or the first column that isn't drawn. A drawn cell that isn't shown (a phone's hidden slot)
// is stepped over, as it is without the window.
function windowedStep(wrapper, cell, key) {
	const row = cell.closest('tr');
	const cellOf = i => row.querySelector(`:scope > td[data-col-index="${i}"]`);
	const target = stepAcrossColumns(i => {
		const other = cellOf(i);
		return other ? other.getClientRects().length > 0 : undefined;
	}, Number(cell.dataset.colIndex), key, lastColumnOf(wrapper));
	return target?.shown !== undefined ? { cell: cellOf(target.shown) } : target;
}

/**
 * Where a sideways key goes among columns by number: the first shown column that way, or the first that isn't drawn.
 * Exported for the tests, which run it in Node.
 * @param {(colIndex: number) => boolean | undefined} shownAt - Whether a column's cell is shown, or undefined when it
 *                                                              isn't drawn.
 * @param {number} colIndex - The column the key was pressed in.
 * @param {string} key - ArrowRight, ArrowLeft, Home or End.
 * @param {number} last - The last column's number.
 * @returns {{shown: number} | {missing: number} | null} The column to focus, the column to draw first, or nothing.
 */
export function stepAcrossColumns(shownAt, colIndex, key, last) {
	const [from, step] = key === 'ArrowRight' ? [colIndex + 1, 1]
		: key === 'ArrowLeft' ? [colIndex - 1, -1]
		: key === 'Home' ? [0, 1]
		: key === 'End' ? [last, -1]
		: [-1, 0];
	for (let i = from; step !== 0 && i >= 0 && i <= last; i += step) {
		const shown = shownAt(i);
		if (shown === undefined) return { missing: i };
		if (shown) return { shown: i };
	}

	return null;
}

// A column outside a virtualized column window isn't drawn: the key scrolls to it and focuses its cell once drawn.
function edgeStep(wrapper, cell, key) {
	if (!wrapper[COLUMN_WATCH]) return undefined;
	const missing = windowedStep(wrapper, cell, key)?.missing;
	if (missing !== undefined) focusCellWhenDrawn(wrapper, Number(cell.closest('tr').dataset.rowIndex), missing);
	return undefined;
}

// ── Copying ──

// Writes text .NET is still working out, inside the key press that asked for it: a ClipboardItem takes the answer as a
// promise, as moka-copy.js does for a button, so Safari and Firefox take the copy too.
function writeClipboard(answer, near) {
	if (typeof ClipboardItem === 'function' && navigator.clipboard?.write && window.isSecureContext) {
		try {
			const blob = answer.then(text => new Blob([text ?? ''], { type: 'text/plain' }));
			navigator.clipboard.write([new ClipboardItem({ 'text/plain': blob })])
				.catch(() => answer.then(text => text && copyText(text, near)));
			return;
		} catch {
			// Chromium before 98 takes no promise in a ClipboardItem.
		}
	}

	answer.then(text => text && copyText(text, near));
}

// Ctrl+C on a cell: the marked block, else the picked rows when the table copies them, else the cell under a table that
// marks blocks. Returns whether it copied.
function copyFromGrid(wrapper, cell, dotNetRef) {
	const range = rangeOf(wrapper);
	const table = cell.closest('table');
	let answer = null;
	if (range) {
		answer = dotNetRef.invokeMethodAsync('CopyCellsText', range.top, range.left, range.bottom, range.right);
	} else if (table?.hasAttribute('data-moka-copy-rows')) {
		answer = dotNetRef.invokeMethodAsync('CopySelectedRowsText');
	} else if (wrapper[RANGE]) {
		const rowIndex = Number(cell.closest('tr').dataset.rowIndex);
		const colIndex = Number(cell.dataset.colIndex);
		answer = dotNetRef.invokeMethodAsync('CopyCellsText', rowIndex, colIndex, rowIndex, colIndex);
	}

	if (!answer) return false;
	writeClipboard(answer, cell);
	return true;
}

// ── Blocks of cells ──
//
// One pointerdown listener on the document serves every table that marks blocks. The block is kept here and drawn as
// attributes on the cells (data-moka-range, aria-selected), which no render writes, so .NET hears nothing until a copy
// or a paste. Rows a virtualized table draws later are marked by a MutationObserver on its body.

const rangeTables = new Set();
let rangePointerBound = false;

const cellCoords = cell => ({ row: Number(cell.closest('tr').dataset.rowIndex), col: Number(cell.dataset.colIndex) });

function rangeOf(wrapper) {
	const state = wrapper[RANGE];
	if (!state?.anchor || !state.focus) return null;
	if (state.anchor.row === state.focus.row && state.anchor.col === state.focus.col) return null;
	return {
		top: Math.min(state.anchor.row, state.focus.row),
		bottom: Math.max(state.anchor.row, state.focus.row),
		left: Math.min(state.anchor.col, state.focus.col),
		right: Math.max(state.anchor.col, state.focus.col)
	};
}

function markRange(wrapper) {
	const range = rangeOf(wrapper);
	for (const cell of ownAll(wrapper, 'tbody tr[data-row-index] > td[data-col-index]')) {
		const { row, col } = cellCoords(cell);
		const inside = !!range && row >= range.top && row <= range.bottom && col >= range.left && col <= range.right;
		if (inside) {
			if (!cell.hasAttribute('data-moka-range')) {
				cell.setAttribute('data-moka-range', '');
				cell.setAttribute('aria-selected', 'true');
			}
		} else if (cell.hasAttribute('data-moka-range')) {
			cell.removeAttribute('data-moka-range');
			cell.removeAttribute('aria-selected');
		}
	}
}

function clearRange(wrapper) {
	const state = wrapper[RANGE];
	if (!state) return;
	state.anchor = null;
	state.focus = null;
	markRange(wrapper);
}

function onRangePointerDown(e) {
	if (e.button !== 0 || e.pointerType === 'touch') return;
	const cell = e.target instanceof Element ? e.target.closest('tbody tr[data-row-index] > td[data-col-index]') : null;
	const wrapper = cell?.closest('.moka-table-wrapper');
	const state = wrapper?.[RANGE];
	if (!state || cell.hasAttribute('data-editing')) return;
	if (e.target.closest('button, a, input, select, textarea, label, [data-moka-act], [contenteditable]')) return;

	const at = cellCoords(cell);
	if (e.shiftKey && state.anchor) {
		state.focus = at;
		markRange(wrapper);
		e.preventDefault();
		return;
	}

	state.anchor = at;
	state.focus = at;
	markRange(wrapper);

	const onMove = move => {
		const over = move.target instanceof Element ? move.target.closest('tbody tr[data-row-index] > td[data-col-index]') : null;
		if (!over || over.closest('.moka-table-wrapper') !== wrapper) return;
		const next = cellCoords(over);
		if (next.row === state.focus.row && next.col === state.focus.col) return;
		if (!wrapper.classList.contains('moka-table-wrapper--ranging')) {
			wrapper.classList.add('moka-table-wrapper--ranging');
			window.getSelection()?.removeAllRanges();
		}

		state.focus = next;
		markRange(wrapper);
	};

	const onUp = () => {
		document.removeEventListener('pointermove', onMove);
		document.removeEventListener('pointerup', onUp);
		document.removeEventListener('pointercancel', onUp);
		wrapper.classList.remove('moka-table-wrapper--ranging');
	};

	document.addEventListener('pointermove', onMove);
	document.addEventListener('pointerup', onUp);
	document.addEventListener('pointercancel', onUp);
}

/**
 * Lets people mark blocks of cells with the mouse or Shift and the arrows, copy them, and paste a block into the cells.
 * @param {HTMLElement} wrapper - The table wrapper element.
 * @param {object} dotNetRef - The table, whose PasteCells takes a paste.
 */
export function bindRanges(wrapper, dotNetRef) {
	if (!wrapper || wrapper[RANGE]) return;
	const state = { anchor: null, focus: null, observer: null, onPaste: null };

	// A paste on a cell (not in an editor) goes to the block, or to the focused cell.
	state.onPaste = e => {
		const cell = e.target instanceof Element ? e.target.closest('td[data-col-index]') : null;
		if (!belongsTo(wrapper, cell) || cell.hasAttribute('data-editing') || !cell.closest('tr[data-row-index]')) return;
		const text = e.clipboardData?.getData('text/plain');
		if (!text) return;
		e.preventDefault();
		const range = rangeOf(wrapper);
		const { row, col } = cellCoords(cell);
		dotNetRef.invokeMethodAsync('PasteCells',
			range?.top ?? row, range?.left ?? col, range?.bottom ?? row, range?.right ?? col, text);
	};

	state.observer = new MutationObserver(() => {
		if (state.anchor) markRange(wrapper);
	});
	state.observer.observe(wrapper, { childList: true, subtree: true });
	wrapper.addEventListener('paste', state.onPaste);
	wrapper[RANGE] = state;
	rangeTables.add(wrapper);

	if (!rangePointerBound) {
		rangePointerBound = true;
		document.addEventListener('pointerdown', onRangePointerDown, true);
	}
}

/**
 * Stops marking blocks of cells.
 * @param {HTMLElement} wrapper - The table wrapper element.
 */
export function unbindRanges(wrapper) {
	const state = wrapper?.[RANGE];
	if (!state) return;
	clearRange(wrapper);
	state.observer.disconnect();
	wrapper.removeEventListener('paste', state.onPaste);
	delete wrapper[RANGE];
	rangeTables.delete(wrapper);
	if (rangeTables.size === 0 && rangePointerBound) {
		rangePointerBound = false;
		document.removeEventListener('pointerdown', onRangePointerDown, true);
	}
}

// ── Column virtualization ──
//
// The header keeps every heading. Their places are measured here, once and again when a size changes, and scrolling
// sideways tells .NET the columns in view, with a few beyond each edge, only when that set changes: one call every few
// columns, none while the view stays among the columns drawn.

const COLUMN_OVERSCAN = 3;

function columnWindow(watch) {
	const table = tableOf(watch.wrapper);
	const container = table?.parentElement;
	if (!table || !container) return;
	const heads = ownAll(watch.wrapper, 'thead th[data-col-index]');
	if (heads.length === 0) return;

	let startWidth = 0;
	let endWidth = 0;
	const free = [];
	for (const head of heads) {
		if (head.classList.contains('moka-table-cell--pin-start')) startWidth += head.offsetWidth;
		else if (head.classList.contains('moka-table-cell--pin-end')) endWidth += head.offsetWidth;
		else free.push(head);
	}

	if (free.length === 0) return;
	const scrolled = Math.abs(container.scrollLeft);
	const from = scrolled + startWidth;
	const to = scrolled + container.clientWidth - endWidth;
	let first = -1;
	let last = -1;
	free.forEach((head, i) => {
		const left = head.offsetLeft;
		const right = left + head.offsetWidth;
		if (right > from && left < to) {
			if (first < 0) first = i;
			last = i;
		}
	});

	if (first < 0) {
		first = 0;
		last = 0;
	}

	const firstIndex = Number(free[Math.max(0, first - COLUMN_OVERSCAN)].dataset.colIndex);
	const lastIndex = Number(free[Math.min(free.length - 1, last + COLUMN_OVERSCAN)].dataset.colIndex);
	if (firstIndex === watch.first && lastIndex === watch.last) return;
	watch.first = firstIndex;
	watch.last = lastIndex;
	watch.dotNetRef.invokeMethodAsync('OnColumnWindow', firstIndex, lastIndex);
}

/**
 * Tells .NET which columns are in view as the table scrolls sideways or changes size, for column virtualization.
 * @param {HTMLElement} wrapper - The table wrapper element.
 * @param {object} dotNetRef - The table, whose OnColumnWindow takes the columns to draw.
 */
export function watchColumns(wrapper, dotNetRef) {
	if (!wrapper || wrapper[COLUMN_WATCH]) return;
	const watch = { wrapper, dotNetRef, first: -1, last: -1, frame: 0, container: null, observer: null, onScroll: null };
	watch.onScroll = () => {
		if (watch.frame) return;
		watch.frame = requestAnimationFrame(() => {
			watch.frame = 0;
			columnWindow(watch);
		});
	};

	const table = tableOf(wrapper);
	watch.container = table?.parentElement ?? null;
	watch.container?.addEventListener('scroll', watch.onScroll, { passive: true });
	watch.observer = new ResizeObserver(watch.onScroll);
	if (watch.container) watch.observer.observe(watch.container);
	if (table?.tHead) watch.observer.observe(table.tHead);
	wrapper[COLUMN_WATCH] = watch;
	columnWindow(watch);
}

/**
 * Stops telling .NET which columns are in view.
 * @param {HTMLElement} wrapper - The table wrapper element.
 */
export function unwatchColumns(wrapper) {
	const watch = wrapper?.[COLUMN_WATCH];
	if (!watch) return;
	if (watch.frame) cancelAnimationFrame(watch.frame);
	watch.observer.disconnect();
	watch.container?.removeEventListener('scroll', watch.onScroll);
	delete wrapper[COLUMN_WATCH];
}
