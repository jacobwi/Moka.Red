/**
 * MokaResizable - handle wiring.
 *
 * Single-axis handles delegate to the shared Core drag module, which is re-exported
 * here so the component only ever imports one module (MokaComponentBase caches a
 * single module reference per component). The component passes target: 'element', so
 * a MokaResizable inside a grid sizes itself instead of a grid track.
 *
 * The corner handle needs a two-axis drag. Stacking two makeResizable calls on the
 * same element does not work: moka-drag.js guards against double-attach via
 * splitter._mokaResize, so the second call is a no-op.
 *
 * The relative import resolves against this module's own URL, so it keeps working
 * when the host app is served from a sub-path.
 */

import { makeResizable, removeResizable, resolveLength } from '../../Moka.Red.Core/moka-drag.js';

export { makeResizable, removeResizable };

export function makeCornerResizable(dotNetRef, element, handle, options) {
	if (!element || !handle) return;
	if (handle._mokaCornerResize) return;

	const opts = options || {};
	const callbackMethod = opts.callbackMethod || 'OnCornerResized';

	function onPointerDown(e) {
		if (e.button !== 0) return;
		e.preventDefault();
		e.stopPropagation();

		const startX = e.clientX;
		const startY = e.clientY;
		const startWidth = element.offsetWidth;
		const startHeight = element.offsetHeight;

		// Resolved per drag because rem, % and the viewport units follow the layout. The 50px
		// floor matches the single-axis default in moka-drag.js.
		const minWidth = resolveLength(opts.minWidth, element, true) ?? 50;
		const maxWidth = resolveLength(opts.maxWidth, element, true) ?? Infinity;
		const minHeight = resolveLength(opts.minHeight, element, false) ?? 50;
		const maxHeight = resolveLength(opts.maxHeight, element, false) ?? Infinity;

		document.body.style.userSelect = 'none';
		document.body.style.cursor = 'nwse-resize';

		// Iframes swallow pointer events, which would strand the drag mid-gesture.
		const iframes = document.querySelectorAll('iframe');
		iframes.forEach(f => f.style.pointerEvents = 'none');

		// The minimum wins over the maximum, as it does in CSS.
		function calcWidth(ev) {
			return Math.max(minWidth, Math.min(maxWidth, startWidth + (ev.clientX - startX)));
		}

		function calcHeight(ev) {
			return Math.max(minHeight, Math.min(maxHeight, startHeight + (ev.clientY - startY)));
		}

		// The last position the drag showed. A cancelled pointer has no usable position of its own.
		let last = { clientX: startX, clientY: startY };

		function onPointerMove(ev) {
			last = { clientX: ev.clientX, clientY: ev.clientY };
			element.style.width = calcWidth(ev) + 'px';
			element.style.height = calcHeight(ev) + 'px';
		}

		// pointercancel (the browser took the touch for a gesture) ends the drag too. Without it the
		// listeners stayed, and the next mouse move resized with no button held.
		function onPointerUp(ev) {
			document.removeEventListener('pointermove', onPointerMove);
			document.removeEventListener('pointerup', onPointerUp);
			document.removeEventListener('pointercancel', onPointerUp);

			document.body.style.userSelect = '';
			document.body.style.cursor = '';
			iframes.forEach(f => f.style.pointerEvents = '');

			const end = ev.type === 'pointercancel' ? last : ev;

			// The size stays inline until the component renders it. The component keeps the
			// dragged size itself, so clearing it here would only flash the old size meanwhile.
			// What is reported is the size the element rendered at, since its CSS min and max can
			// stop it short of the pointer.
			element.style.width = calcWidth(end) + 'px';
			element.style.height = calcHeight(end) + 'px';
			const width = element.offsetWidth;
			const height = element.offsetHeight;
			element.style.width = width + 'px';
			element.style.height = height + 'px';

			if (dotNetRef) {
				dotNetRef.invokeMethodAsync(callbackMethod, width, height);
			}
		}

		document.addEventListener('pointermove', onPointerMove);
		document.addEventListener('pointerup', onPointerUp);
		document.addEventListener('pointercancel', onPointerUp);
	}

	handle.addEventListener('pointerdown', onPointerDown);
	handle._mokaCornerResize = {
		destroy: () => {
			handle.removeEventListener('pointerdown', onPointerDown);
			delete handle._mokaCornerResize;
		}
	};
}

export function removeCornerResizable(handle) {
	handle?._mokaCornerResize?.destroy();
}
