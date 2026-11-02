/**
 * MokaImageCompare - the divider drag.
 *
 * The drag runs in the browser, writing the position straight to the root's custom property. The
 * alternative, a .NET handler per pointermove, re-renders both images on every move. .NET hears
 * the result once, when the drag ends.
 */

/** Teardown for the drag in progress, keyed by the root being dragged. */
const drags = new WeakMap();

/**
 * Starts watching for divider drags on a compare root.
 * @param {HTMLElement} root - The compare's root element.
 * @param {HTMLElement} handle - The divider handle.
 * @param {boolean} horizontal - Whether the divider runs top to bottom.
 * @param {object} dotNetRef - Reference used to call OnDragged.
 */
export function enableDrag(root, handle, horizontal, dotNetRef) {
	if (!root || !handle) return;

	disableDrag(root);

	/** @param {PointerEvent} down */
	const onDown = (down) => {
		down.preventDefault();
		handle.setPointerCapture(down.pointerId);

		const rect = root.getBoundingClientRect();
		const axis = horizontal ? 'clientX' : 'clientY';
		const extent = horizontal ? rect.width : rect.height;
		const origin = horizontal ? rect.left : rect.top;

		/** @param {PointerEvent} move */
		const onMove = (move) => {
			if (extent <= 0) return;
			const pct = Math.min(100, Math.max(0, (move[axis] - origin) / extent * 100));
			root.style.setProperty('--moka-compare-pos', pct + '%');
		};

		const onUp = (up) => {
			handle.removeEventListener('pointermove', onMove);
			handle.removeEventListener('pointerup', onUp);
			handle.removeEventListener('pointercancel', onUp);

			const value = root.style.getPropertyValue('--moka-compare-pos');
			const parsed = parseFloat(value);
			if (!Number.isNaN(parsed)) {
				dotNetRef.invokeMethodAsync('OnDragged', Math.round(parsed * 100) / 100);
			}
		};

		handle.addEventListener('pointermove', onMove);
		handle.addEventListener('pointerup', onUp);
		handle.addEventListener('pointercancel', onUp);
		onMove(down);
	};

	handle.addEventListener('pointerdown', onDown);
	drags.set(root, () => handle.removeEventListener('pointerdown', onDown));
}

/**
 * Stops watching a root. Safe to call for a root that never watched.
 * @param {HTMLElement} root - The compare's root element.
 */
export function disableDrag(root) {
	const teardown = root && drags.get(root);
	if (teardown) {
		teardown();
		drags.delete(root);
	}
}
