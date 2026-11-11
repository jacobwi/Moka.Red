/**
 * MokaFileBrowser JS module.
 *
 * Focus stays on the list and .NET owns the cursor, which it names with aria-activedescendant, since
 * Virtualize reuses rows as the list scrolls. This module reads the keys, runs drags, shows the drop
 * layer, keeps the headings over their columns and reports how many tiles fit across. Listeners sit on
 * the root and are shared by every row, and .NET hears only what it acts on. The path field's keys
 * come through here too, so the field's list of folders stays in step with what .NET holds.
 */

import { makeResizable } from '../../Moka.Red.Core/moka-drag.js';

const LIST_KEYS = new Set(['ArrowDown', 'ArrowUp', 'ArrowLeft', 'ArrowRight', 'Home', 'End', 'PageDown', 'PageUp',
	'Enter', ' ', 'Delete', 'Backspace', 'Escape', 'F2', 'F5']);
const CTRL_KEYS = new Set(['a', 'c', 'x', 'v', 'd', 'A', 'C', 'X', 'V', 'D']);
const ACTIVATE_KEYS = new Set(['Enter', ' ']);
const CTRL_ARROWS = new Set(['ArrowDown', 'ArrowUp', 'ArrowLeft', 'ArrowRight', 'Home', 'End']);
const SHIFT_EXTENDS = new Set(['ArrowDown', 'ArrowUp', 'ArrowLeft', 'ArrowRight', 'Home', 'End', 'PageDown', 'PageUp']);
const PATH_KEYS = new Set(['ArrowDown', 'ArrowUp', 'Enter', 'Escape', 'Tab']);

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

// Widgets that use the browser's keys for themselves. Fields never get as far as asking.
const OWN_KEYS = '[role=menu], [role=menubar], [role=tablist], [role=tree], [role=grid], [role=listbox], [role=radiogroup], ' +
	'[role=slider], [role=spinbutton], [role=combobox], [role=textbox], [role=searchbox], [role=dialog], [role=separator]';

const browsers = new WeakMap();

const hasFiles = e => !!e.dataTransfer && Array.from(e.dataTransfer.types || []).includes('Files');
const isFileInput = target => target instanceof HTMLInputElement && target.type === 'file';
const isEditable = target => target instanceof HTMLElement &&
	(target.isContentEditable || ['INPUT', 'TEXTAREA', 'SELECT'].includes(target.tagName));

function listOf(root) {
	return root.querySelector('[data-moka-list]');
}

function lines(list) {
	return list.querySelectorAll('.moka-file-browser__line, .moka-file-browser__tile-row, .moka-file-browser__grid-row');
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

// A group's heading sticks to the top of the list and covers what scrolls under it.
function stickyTop(list) {
	return list.querySelector('.moka-file-browser__group-head')?.offsetHeight ?? 0;
}

// Views whose lines differ in height name each line, and a line on the page is scrolled to as it is.
function revealNamed(list, line) {
	const top = line.getBoundingClientRect().top - list.getBoundingClientRect().top - list.clientTop + list.scrollTop;
	const bottom = top + line.getBoundingClientRect().height;
	const covered = stickyTop(list);
	if (top - covered < list.scrollTop) {
		list.scrollTop = top - covered;
	} else if (bottom > list.scrollTop + list.clientHeight) {
		list.scrollTop = bottom - list.clientHeight;
	}
}

// Worked out from the row height, because the row may not be on the page yet.
function reveal(list, index) {
	if (typeof index !== 'number' || index < 0) return;

	const named = list.querySelector(`[data-moka-line="${index}"]`);
	if (named) {
		revealNamed(list, named);
		return;
	}

	const height = lineHeight(list);
	if (height <= 0) return;

	// The list's own padding sits before the first line and after the last.
	const style = getComputedStyle(list);
	const top = (parseFloat(style.paddingTop) || 0) + index * height;
	const bottom = top + height + (parseFloat(style.paddingBottom) || 0);
	if (top < list.scrollTop) {
		list.scrollTop = index === 0 ? 0 : top;
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

function reachesBrowser(root, list, target) {
	if (root.dataset.mokaFileKeyScope !== 'browser') return false;

	const widget = target.closest(OWN_KEYS);
	return !widget || widget === list || !root.contains(widget);
}

// Arrows walk the folders offered, Tab takes the one lit, Enter goes and Escape puts the crumbs back.
function handlePathKey(state, e) {
	if (!PATH_KEYS.has(e.key) || e.altKey || e.ctrlKey || e.metaKey) return;
	if (e.shiftKey && e.key !== 'Enter') return;
	if (e.key === 'Tab' && !e.target.hasAttribute('aria-activedescendant')) return;

	e.preventDefault();
	if (e.key === 'Escape') e.stopPropagation();
	state.dotNetRef.invokeMethodAsync('HandlePathKey', e.key, e.target.value).catch(() => { });
}

function handleKey(root, state, e) {
	if (e.isComposing || e.defaultPrevented || !(e.target instanceof Element)) return;
	if (e.target.closest('[data-moka-file-browser]') !== root) return;

	const list = listOf(root);
	const ctrl = e.ctrlKey || e.metaKey;

	if (e.target.matches('[data-moka-file-path]')) {
		handlePathKey(state, e);
		return;
	}

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

	if (!list || e.altKey || (e.target !== list && !reachesBrowser(root, list, e.target))) return;

	// A focused button or link keeps its own Enter and Space. The root itself is only focused when an app makes it so.
	if (ACTIVATE_KEYS.has(e.key) && e.target !== list && e.target !== root) return;

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

// A press in the folders under the path field would take focus from the field and close it before the click lands.
function keepPathFocus(e) {
	if (e.target instanceof Element && e.target.closest('[data-moka-path-area] [data-moka-popup]')) {
		e.preventDefault();
	}
}

function bindPath(root, state, signal) {
	root.addEventListener('mousedown', keepPathFocus, { signal });
	root.addEventListener('focusout', e => {
		if (!(e.target instanceof Element) || !e.target.matches('[data-moka-file-path]')) return;

		const area = e.target.closest('[data-moka-path-area]');
		if (e.relatedTarget instanceof Node && area?.contains(e.relatedTarget)) return;
		state.dotNetRef.invokeMethodAsync('HandlePathLeft').catch(() => { });
	}, { signal });
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

// The room a list's scrollbar takes, from the observer's own sizes because offsetWidth and clientWidth are rounded.
// Nothing for an overlay scrollbar or a folder that fits, and the room for a gutter an app has reserved.
function scrollbarWidth(list, entry) {
	const style = getComputedStyle(list);
	const edges = parseFloat(style.paddingLeft) + parseFloat(style.paddingRight) +
		parseFloat(style.borderLeftWidth) + parseFloat(style.borderRightWidth);
	return Math.max(0, entry.borderBoxSize[0].inlineSize - entry.contentBoxSize[0].inlineSize - edges);
}

function alignHead(main, entry) {
	const head = main.querySelector(':scope > .moka-file-browser__head');
	if (!head) return;

	const width = scrollbarWidth(entry.target, entry);
	if (width > 0) {
		head.style.setProperty('--moka-file-browser-gutter', `${width}px`);
	} else {
		head.style.removeProperty('--moka-file-browser-gutter');
	}
}

function bindColumnEdge(state, main, edge) {
	if (edge._mokaResize) return;

	const column = edge.parentElement;
	let press = null;

	const variable = () => edge.dataset.mokaColumnVar;
	const least = () => Number(edge.dataset.mokaColumnMin) || 0;
	const name = () => column.parentElement.querySelector(':scope > .moka-file-browser__name-cell');

	edge.addEventListener('pointerdown', e => {
		if (e.button === 0) {
			press = { width: column.offsetWidth, value: main.style.getPropertyValue(variable()) };
		}
	});

	// A press that moved nothing is half of a double click, and pins no width.
	const reporter = {
		invokeMethodAsync(_, size) {
			const pressed = press;
			press = null;
			if (pressed && Math.abs(size - pressed.width) < 1) {
				if (pressed.value) {
					main.style.setProperty(variable(), pressed.value);
				} else {
					main.style.removeProperty(variable());
				}

				return;
			}

			state.dotNetRef.invokeMethodAsync('HandleColumnResized', edge.dataset.mokaColumn, size).catch(() => { });
		}
	};

	makeResizable(reporter, column, edge, {
		target: 'element',
		callbackMethod: 'HandleColumnResized',
		reverse: getComputedStyle(edge).direction !== 'rtl',
		keyboard: true,
		get min() { return least(); },
		get max() { return column.offsetWidth + Math.max(0, (name()?.offsetWidth ?? 0) - least()); },
		applySize: size => main.style.setProperty(variable(), `${size}px`)
	});
}

// The list and the head come and go with the folder and the view, and only direct children of these two change then,
// so this doesn't run for the rows.
function watchColumns(root, state) {
	const main = root.querySelector(':scope > .moka-file-browser__body > .moka-file-browser__main');
	if (!main) return;

	state.listObserver = new ResizeObserver(entries => alignHead(main, entries[entries.length - 1]));

	const refresh = () => {
		const list = listOf(root);
		state.listObserver.disconnect();
		if (list) state.listObserver.observe(list);

		const head = main.querySelector(':scope > .moka-file-browser__head');
		state.columnsObserver.disconnect();
		state.columnsObserver.observe(main, { childList: true });
		if (head) {
			state.columnsObserver.observe(head, { childList: true });
			head.querySelectorAll(':scope > .moka-file-browser__heading-wrap > [data-moka-column]')
				.forEach(edge => bindColumnEdge(state, main, edge));
		}
	};

	state.columnsObserver = new MutationObserver(refresh);
	refresh();
}

function resetColumnOnDoubleClick(root, state, signal) {
	root.addEventListener('dblclick', e => {
		const edge = e.target instanceof Element ? e.target.closest('[data-moka-column]') : null;
		if (edge && edge.closest('[data-moka-file-browser]') === root) {
			state.dotNetRef.invokeMethodAsync('HandleColumnReset', edge.dataset.mokaColumn).catch(() => { });
		}
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
		observer: null,
		listObserver: null,
		columnsObserver: null
	};
	browsers.set(root, state);

	root.addEventListener('keydown', e => handleKey(root, state, e), { signal });
	root.addEventListener('mousedown', forgetPageSelection, { signal });
	bindDrag(root, state, signal);
	bindFileDrop(root, state, signal);
	bindPath(root, state, signal);
	resetColumnOnDoubleClick(root, state, signal);
	watchColumns(root, state);

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
	state.listObserver?.disconnect();
	state.columnsObserver?.disconnect();
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

/** Focuses the path field that has just appeared, with its text selected so typing replaces it. */
export function openPath(root) {
	const input = root?.querySelector('[data-moka-file-path]');
	if (!input) return;

	input.focus({ preventScroll: true });
	input.select();
	input.scrollLeft = input.scrollWidth;
}

/** Gives focus back to the button beside the crumbs once Escape has closed the path field. */
export function focusPathEdit(root) {
	root?.querySelector('[data-moka-path-edit]')?.focus({ preventScroll: true });
}

/** Opens the file picker of the browser's upload input. */
export function pickFiles(root) {
	root?.querySelector('[data-moka-file-drop] input[type=file]')?.click();
}
