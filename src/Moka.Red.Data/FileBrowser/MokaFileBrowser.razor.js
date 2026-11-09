/**
 * MokaFileBrowser JS module.
 *
 * Focus stays on the list and .NET owns the cursor, which it names with aria-activedescendant, since
 * Virtualize reuses rows as the list scrolls. This module reads the keys, runs drags, shows the drop
 * layer, and reports how many tiles fit across. Listeners sit on the root and are shared by every row,
 * and .NET hears only what it acts on.
 */

const LIST_KEYS = new Set(['ArrowDown', 'ArrowUp', 'ArrowLeft', 'ArrowRight', 'Home', 'End', 'PageDown', 'PageUp',
	'Enter', ' ', 'Delete', 'Backspace', 'Escape', 'F2', 'F5']);
const CTRL_KEYS = new Set(['a', 'c', 'x', 'v', 'd', 'A', 'C', 'X', 'V', 'D']);
const CTRL_ARROWS = new Set(['ArrowDown', 'ArrowUp', 'ArrowLeft', 'ArrowRight', 'Home', 'End']);
const SHIFT_EXTENDS = new Set(['ArrowDown', 'ArrowUp', 'ArrowLeft', 'ArrowRight', 'Home', 'End', 'PageDown', 'PageUp']);

// The values of MokaFileKeys. A test holds the two together.
const KEY = {
	Move: 1, Toggle: 2, SelectAll: 4, Open: 8, Up: 16, History: 32, Rename: 64, Delete: 128, Copy: 256, Cut: 512, Paste: 1024,
	Duplicate: 2048, Escape: 4096, EscapeDropsHeld: 8192, Refresh: 16384, Filter: 32768, TypeAhead: 65536
};
const PLAIN_KEY = {
	' ': KEY.Toggle, Enter: KEY.Open, Backspace: KEY.Up, F2: KEY.Rename, Delete: KEY.Delete, Escape: KEY.Escape, F5: KEY.Refresh
};
const CTRL_KEY = { a: KEY.SelectAll, c: KEY.Copy, x: KEY.Cut, v: KEY.Paste, d: KEY.Duplicate };

const TYPE_AHEAD_MS = 700;

// A drag that ends outside the page can leave the enter and leave counts uneven. dragover keeps arriving while files
// are over the page, even with the pointer still, so a quiet second means the drag is over.
const QUIET_MS = 1000;

const MOVE_TYPE = 'application/x-moka-file-key';

const browsers = new WeakMap();

const hasFiles = e => !!e.dataTransfer && Array.from(e.dataTransfer.types || []).includes('Files');
const isFileInput = target => target instanceof HTMLInputElement && target.type === 'file';
const isEditable = target => target instanceof HTMLElement &&
	(target.isContentEditable || ['INPUT', 'TEXTAREA', 'SELECT'].includes(target.tagName));

function listOf(root) {
	return root.querySelector('[data-moka-list]');
}

function lines(list) {
	return list.querySelectorAll('.moka-file-browser__line, .moka-file-browser__tile-row');
}

function lineHeight(list) {
	const found = lines(list);
	if (found.length === 0) return 0;
	if (found.length === 1) return found[0].getBoundingClientRect().height;
	return found[1].getBoundingClientRect().top - found[0].getBoundingClientRect().top;
}

function pageRows(list) {
	const height = lineHeight(list);
	return height > 0 ? Math.max(1, Math.floor(list.clientHeight / height) - 1) : 1;
}

// Worked out from the row height, because the row may not be on the page yet.
function reveal(list, index) {
	if (typeof index !== 'number' || index < 0) return;

	const height = lineHeight(list);
	if (height <= 0) return;

	const top = index * height;
	const bottom = top + height;
	if (top < list.scrollTop) {
		list.scrollTop = top;
	} else if (bottom > list.scrollTop + list.clientHeight) {
		list.scrollTop = bottom - list.clientHeight;
	}
}

function measureColumns(root, state) {
	const list = listOf(root);
	if (!list) return;

	const tile = parseFloat(getComputedStyle(list).getPropertyValue('--_fb-tile-width')) || 8;
	const gap = parseFloat(getComputedStyle(list).columnGap) || 0;
	const rootEm = parseFloat(getComputedStyle(document.documentElement).fontSize) || 16;
	const style = getComputedStyle(list);
	const inner = list.clientWidth - parseFloat(style.paddingLeft) - parseFloat(style.paddingRight);
	const columns = Math.max(1, Math.floor((inner + gap) / (tile * rootEm + gap)));
	if (columns !== state.columns) {
		state.columns = columns;
		state.dotNetRef.invokeMethodAsync('HandleColumns', columns).catch(() => { });
	}
}

function applyFit(root) {
	const medium = parseFloat(root.dataset.mokaFitMedium);
	if (Number.isNaN(medium)) {
		root.removeAttribute('data-moka-under-medium');
		root.removeAttribute('data-moka-under-wide');
		return;
	}

	const wide = parseFloat(root.dataset.mokaFitWide);
	const rootEm = parseFloat(getComputedStyle(document.documentElement).fontSize) || 16;
	const style = getComputedStyle(root);
	const width = (root.clientWidth - parseFloat(style.paddingLeft) - parseFloat(style.paddingRight)) / rootEm;
	root.toggleAttribute('data-moka-under-medium', width < medium);
	root.toggleAttribute('data-moka-under-wide', width < wide);
}

function clearDropTarget(state) {
	if (state.dropTarget) {
		state.dropTarget.removeAttribute('data-moka-drop-over');
		state.dropTarget = null;
	}
}

// Which flag a key press asks for, with exactly the modifiers it is bound to. 0 is not one of the browser's shortcuts.
function keyFlag(e, ctrl) {
	if (e.altKey) {
		return !ctrl && !e.shiftKey && (e.key === 'ArrowLeft' || e.key === 'ArrowRight') ? KEY.History : 0;
	}

	if (ctrl) {
		if (e.shiftKey) return 0;
		if (CTRL_ARROWS.has(e.key)) return KEY.Move;
		return CTRL_KEYS.has(e.key) ? CTRL_KEY[e.key.toLowerCase()] : 0;
	}

	if (SHIFT_EXTENDS.has(e.key)) return KEY.Move;
	return e.shiftKey ? 0 : PLAIN_KEY[e.key] ?? 0;
}

const hasTextSelected = () => {
	const selection = window.getSelection();
	return !!selection && !selection.isCollapsed && selection.toString().length > 0;
};

// A click on the list can't collapse the page's selection, because the list is user-select: none. Without this, text
// selected earlier and elsewhere would keep Ctrl+C away from the rows just clicked.
function forgetPageSelection(e) {
	if (e.button === 0 && e.target instanceof Element && e.target.closest('[data-moka-list]') && !isEditable(e.target)) {
		window.getSelection()?.removeAllRanges();
	}
}

function handleKey(root, state, e) {
	if (e.isComposing || e.defaultPrevented || !(e.target instanceof Element)) return;
	if (e.target.closest('[data-moka-file-browser]') !== root) return;

	const list = listOf(root);
	const ctrl = e.ctrlKey || e.metaKey;

	if (e.target.matches('[data-moka-file-filter]')) {
		if (e.key === 'ArrowDown' && list && !ctrl && !e.altKey && !e.shiftKey) {
			e.preventDefault();
			list.focus();
		}
		return;
	}

	if (isEditable(e.target)) return;

	const taken = Number(root.dataset.mokaFileKeys ?? -1);
	const takes = flag => (taken & flag) !== 0;
	const flag = keyFlag(e, ctrl);

	const call = (key, shift) => state.dotNetRef
		.invokeMethodAsync('HandleKey', key, ctrl, shift, e.altKey, list ? pageRows(list) : 1)
		.then(index => { if (list) reveal(list, index); })
		.catch(() => { });

	if (flag === KEY.History || flag === KEY.Refresh) {
		if (takes(flag)) {
			e.preventDefault();
			call(e.key, false);
		}
		return;
	}

	if (!list || e.target !== list || e.altKey) return;

	if (ctrl) {
		if (takes(flag) && !(flag === KEY.Copy && hasTextSelected())) {
			e.preventDefault();
			call(e.key, false);
		}
		return;
	}

	if (LIST_KEYS.has(e.key)) {
		if (takes(flag)) {
			e.preventDefault();
			if (!e.repeat || !['Enter', 'Delete', 'F2', 'Backspace'].includes(e.key)) call(e.key, e.shiftKey);
		}
		return;
	}

	if (e.key === '/') {
		const filter = root.querySelector('[data-moka-file-filter]');
		if (filter && takes(KEY.Filter)) {
			e.preventDefault();
			filter.focus();
		}
		return;
	}

	if (e.key.length === 1 && takes(KEY.TypeAhead)) {
		state.typed += e.key;
		clearTimeout(state.typedTimer);
		state.typedTimer = setTimeout(() => { state.typed = ''; }, TYPE_AHEAD_MS);
		state.dotNetRef.invokeMethodAsync('HandleTypeAhead', state.typed)
			.then(index => reveal(list, index))
			.catch(() => { });
	}
}

function bindDrag(root, state, signal) {
	root.addEventListener('dragstart', e => {
		const row = e.target instanceof Element ? e.target.closest('[data-moka-key][draggable="true"]') : null;
		if (!row || row.closest('[data-moka-file-browser]') !== root) return;

		e.dataTransfer.setData('text/plain', row.dataset.mokaDragText ?? row.textContent.trim());
		if ('mokaDragOnly' in row.dataset) {
			e.dataTransfer.effectAllowed = 'copy';
			return;
		}

		state.dragging = row.dataset.mokaKey;
		e.dataTransfer.effectAllowed = 'move';
		e.dataTransfer.setData(MOVE_TYPE, state.dragging);
	}, { signal });

	root.addEventListener('dragend', () => {
		state.dragging = null;
		clearDropTarget(state);
	}, { signal });

	root.addEventListener('dragover', e => {
		if (!state.dragging || !(e.target instanceof Element)) return;

		const target = e.target.closest('[data-moka-file-target]');
		const valid = target && target.closest('[data-moka-file-browser]') === root && target.dataset.mokaFileTarget !== state.dragging;
		if (!valid) {
			clearDropTarget(state);
			return;
		}

		e.preventDefault();
		e.dataTransfer.dropEffect = 'move';
		if (target !== state.dropTarget) {
			clearDropTarget(state);
			state.dropTarget = target;
			target.setAttribute('data-moka-drop-over', '');
		}
	}, { signal });

	root.addEventListener('dragleave', e => {
		if (state.dropTarget && e.target === state.dropTarget && !state.dropTarget.contains(e.relatedTarget)) {
			clearDropTarget(state);
		}
	}, { signal });

	root.addEventListener('drop', e => {
		const source = state.dragging;
		const target = state.dropTarget?.dataset.mokaFileTarget;
		if (source == null || target == null) return;

		e.preventDefault();
		state.dragging = null;
		clearDropTarget(state);
		state.dotNetRef.invokeMethodAsync('HandleDrop', source, target).catch(() => { });
	}, { signal });
}

function bindFileDrop(root, state, signal) {
	let depth = 0;
	let quiet = 0;

	const hide = () => {
		depth = 0;
		clearTimeout(quiet);
		root.removeAttribute('data-moka-file-drag');
	};

	const keepAlive = () => {
		clearTimeout(quiet);
		quiet = setTimeout(hide, QUIET_MS);
	};

	const takesDrops = () => !!root.querySelector('[data-moka-file-drop]');

	root.addEventListener('dragenter', e => {
		if (state.dragging || !hasFiles(e) || !takesDrops()) return;

		depth++;
		root.setAttribute('data-moka-file-drag', '');
		keepAlive();
	}, { signal });

	root.addEventListener('dragleave', e => {
		if (state.dragging || !hasFiles(e)) return;

		depth = Math.max(0, depth - 1);
		if (depth === 0) hide();
	}, { signal });

	root.addEventListener('dragover', e => {
		if (state.dragging || !hasFiles(e) || !takesDrops()) return;

		keepAlive();
		if (!isFileInput(e.target)) e.preventDefault();
	}, { signal });

	root.addEventListener('drop', e => {
		if (state.dragging) return;

		if (hasFiles(e) && !isFileInput(e.target)) e.preventDefault();
		setTimeout(hide, 0);
	}, { signal });
}

/**
 * Starts one browser's keys, drags and measuring. Calling it again only swaps the .NET reference.
 * @param {HTMLElement} root - The browser's root element.
 * @param {object} dotNetRef - The browser's DotNetObjectReference.
 * @returns {boolean} Whether this is a touch screen, where a tap opens rather than selects.
 */
export function bind(root, dotNetRef) {
	if (!root) return false;

	const known = browsers.get(root);
	if (known) {
		known.dotNetRef = dotNetRef;
		return known.coarse;
	}

	const controller = new AbortController();
	const signal = controller.signal;
	const state = {
		dotNetRef,
		controller,
		coarse: window.matchMedia('(hover: none) and (pointer: coarse)').matches,
		typed: '',
		typedTimer: 0,
		dragging: null,
		dropTarget: null,
		columns: 0,
		observer: null
	};
	browsers.set(root, state);

	root.addEventListener('keydown', e => handleKey(root, state, e), { signal });
	root.addEventListener('mousedown', forgetPageSelection, { signal });
	bindDrag(root, state, signal);
	bindFileDrop(root, state, signal);

	state.observer = new ResizeObserver(() => {
		applyFit(root);
		measureColumns(root, state);
	});
	state.observer.observe(root);

	return state.coarse;
}

/** Stops everything bind started. */
export function unbind(root) {
	const state = root ? browsers.get(root) : null;
	if (!state) return;

	state.controller.abort();
	state.observer?.disconnect();
	clearTimeout(state.typedTimer);
	browsers.delete(root);
}

/** Reports the tile columns for a list that has just appeared, and applies column widths that have just changed. */
export function measure(root) {
	const state = root ? browsers.get(root) : null;
	if (state) {
		applyFit(root);
		measureColumns(root, state);
	}
}

/** Scrolls a line of the list into view, for a row selected from .NET. */
export function revealLine(root, line) {
	const list = root ? listOf(root) : null;
	if (list) reveal(list, line);
}

/** Gives focus back to the list when finishing something took it away, and leaves it alone otherwise. */
export function focusListIfLost(root) {
	const active = document.activeElement;
	if (root && (!active || active === document.body)) {
		listOf(root)?.focus({ preventScroll: true });
	}
}

/** Opens the file picker of the browser's upload input. */
export function pickFiles(root) {
	root?.querySelector('[data-moka-file-drop] input[type=file]')?.click();
}
