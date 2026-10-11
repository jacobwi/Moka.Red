/**
 * MokaSplitPane - measurement of the first pane, and the handle drag.
 *
 * The pane is reached through the root ElementReference rather than an id lookup:
 * the Id parameter is caller-controlled, and two panes sharing an Id would
 * otherwise measure each other.
 */

/** Live ResizeObservers, keyed by the root they watch. */
const observers = new WeakMap();

/** Teardown for the drag in progress, keyed by the root being dragged. */
const drags = new WeakMap();

export function measureFirstPane(root, horizontal) {
	if (!root) return 0;

	const first = root.querySelector('.moka-split-pane-first');
	if (!first) return 0;

	const rect = first.getBoundingClientRect();
	return horizontal ? rect.width : rect.height;
}

/**
 * Watches the root for size changes so a split first laid out hidden, or at another size, measures
 * again instead of keeping a stale pixel size that every later drag starts from.
 * @param {HTMLElement} root - The split's root element.
 * @param {object} dotNetRef - Reference used to call OnRootResized.
 */
export function observeSize(root, dotNetRef) {
	if (!root || typeof ResizeObserver === 'undefined') return;

	stopObserving(root);

	// The observer fires once on observe; .NET has just measured, so that first one is noise.
	let settled = false;
	const observer = new ResizeObserver(() => {
		if (!settled) {
			settled = true;
			return;
		}

		dotNetRef.invokeMethodAsync('OnRootResized');
	});

	observer.observe(root);
	observers.set(root, observer);
}

/**
 * Stops watching a root. Safe to call for a root that was never observed.
 * @param {HTMLElement} root - The split's root element.
 */
export function stopObserving(root) {
	const observer = root && observers.get(root);
	if (observer) {
		observer.disconnect();
		observers.delete(root);
	}
}

/**
 * Runs a handle drag in the browser, writing the size straight to the root's custom property. The
 * alternative, a .NET handler per pointermove, re-renders everything inside both panes on every
 * move. .NET hears the result once, when the drag ends.
 * @param {HTMLElement} root - The split's root element.
 * @param {boolean} horizontal - Whether the split is side by side.
 * @param {number} startPointer - Pointer coordinate along the split's axis when the drag started.
 * @param {number} startSize - First pane size in pixels when the drag started.
 * @param {object} dotNetRef - Reference used to call OnDragFinished.
 * @returns {boolean} True once the drag is running, so .NET can stop handling pointer moves.
 */
export function beginDrag(root, horizontal, startPointer, startSize, dotNetRef) {
	if (!root) return false;

	endDrag(root);

	const axis = horizontal ? 'clientX' : 'clientY';
	let size = startSize;

	function onMove(e) {
		const next = startSize + (e[axis] - startPointer);
		if (next > 0) {
			size = next;
			root.style.setProperty('--moka-split-size', `${next}px`);
		}
	}

	function onUp() {
		endDrag(root);
		dotNetRef.invokeMethodAsync('OnDragFinished', size);
	}

	// On the window, not the handle: a fast drag leaves the handle behind, and the pointer has to
	// keep being followed until it is released anywhere on the page.
	window.addEventListener('pointermove', onMove);
	window.addEventListener('pointerup', onUp);
	window.addEventListener('pointercancel', onUp);

	drags.set(root, () => {
		window.removeEventListener('pointermove', onMove);
		window.removeEventListener('pointerup', onUp);
		window.removeEventListener('pointercancel', onUp);
	});

	return true;
}

/**
 * Detaches the listeners of the drag in progress on a root, if there is one.
 * @param {HTMLElement} root - The split's root element.
 */
export function endDrag(root) {
	const teardown = root && drags.get(root);
	if (teardown) {
		teardown();
		drags.delete(root);
	}
}
