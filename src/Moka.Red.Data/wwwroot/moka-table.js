const RESIZE_HANDLER = '_mokaResizeHandler';
const GRID_KEY_HANDLER = '_mokaGridKeyHandler';

// Keys the grid handles itself; left to the browser they scroll the container instead.
const NAV_KEYS = new Set(['ArrowUp', 'ArrowDown', 'ArrowLeft', 'ArrowRight', 'Home', 'End', ' ']);

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

	const handles = wrapper.querySelectorAll('.moka-table-resize-handle');
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
 * @param {HTMLElement} wrapper - The table wrapper element.
 */
export function initGridKeys(wrapper) {
	if (!wrapper || wrapper[GRID_KEY_HANDLER]) return;

	const onKeyDown = e => {
		if (!NAV_KEYS.has(e.key)) return;
		const target = e.target;
		if (!target || typeof target.matches !== 'function') return;
		if (!target.matches('td[data-col-index]')) return;
		e.preventDefault();
	};

	wrapper.addEventListener('keydown', onKeyDown);
	wrapper[GRID_KEY_HANDLER] = onKeyDown;
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
	const cell = wrapper.querySelector(
		`tbody tr[data-row-index="${rowIndex}"] td[data-col-index="${colIndex}"]`);
	if (cell) cell.focus();
}

/**
 * Focuses and selects the inline edit input once it has been rendered.
 * @param {HTMLElement} wrapper - The table wrapper element.
 */
export function focusEditInput(wrapper) {
	if (!wrapper) return;
	const input = wrapper.querySelector('.moka-table-edit-input');
	if (!input) return;
	input.focus();
	if (typeof input.select === 'function') input.select();
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

	wrapper.querySelectorAll('.moka-table-resize-handle').forEach(handle => {
		if (handle[RESIZE_HANDLER]) {
			handle.removeEventListener('pointerdown', handle[RESIZE_HANDLER]);
			delete handle[RESIZE_HANDLER];
		}
	});
}
