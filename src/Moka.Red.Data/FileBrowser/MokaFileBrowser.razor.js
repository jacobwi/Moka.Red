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

function clearDropTarget(state) {
	if (state.dropTarget) {
		state.dropTarget.removeAttribute('data-moka-drop-over');
		state.dropTarget = null;
	}
}

function handleKey(root, state, e) {
	if (e.isComposing || e.defaultPrevented || !(e.target instanceof Element)) return;
	if (e.target.closest('[data-moka-file-browser]') !== root) return;

	const list = listOf(root);
	const ctrl = e.ctrlKey || e.metaKey;

	if (e.target.matches('[data-moka-file-filter]')) {
		if (e.key === 'ArrowDown' && list) {
			e.preventDefault();
			list.focus();
		}
		return;
	}

	if (isEditable(e.target)) return;

	const call = (key, shift) => state.dotNetRef
		.invokeMethodAsync('HandleKey', key, ctrl, shift, e.altKey, list ? pageRows(list) : 1)
		.then(index => { if (list) reveal(list, index); })
		.catch(() => { });

	if (e.altKey && (e.key === 'ArrowLeft' || e.key === 'ArrowRight')) {
		e.preventDefault();
		call(e.key, false);
		return;
	}

	if (e.key === 'F5' && !ctrl) {
		e.preventDefault();
		call(e.key, false);
		return;
	}

	if (!list || e.target !== list || e.altKey) return;

	if (ctrl) {
		if (CTRL_KEYS.has(e.key) || CTRL_ARROWS.has(e.key)) {
			e.preventDefault();
			call(e.key, e.shiftKey);
		}
		return;
	}

	if (LIST_KEYS.has(e.key)) {
		e.preventDefault();
		if (!e.repeat || !['Enter', 'Delete', 'F2', 'Backspace'].includes(e.key)) call(e.key, e.shiftKey);
		return;
	}

	if (e.key === '/') {
		e.preventDefault();
		root.querySelector('[data-moka-file-filter]')?.focus();
		return;
	}

	if (e.key.length === 1) {
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

		state.dragging = row.dataset.mokaKey;
		e.dataTransfer.effectAllowed = 'move';
		e.dataTransfer.setData(MOVE_TYPE, state.dragging);
		e.dataTransfer.setData('text/plain', row.textContent.trim());
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
	bindDrag(root, state, signal);
	bindFileDrop(root, state, signal);

	state.observer = new ResizeObserver(() => measureColumns(root, state));
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

/** Reports the tile columns for a list that has just appeared. */
export function measure(root) {
	const state = root ? browsers.get(root) : null;
	if (state) measureColumns(root, state);
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
