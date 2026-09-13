/**
 * MokaResizable - handle wiring.
 *
 * Single-axis handles delegate to the shared Core drag module, which is re-exported
 * here so the component only ever imports one module (MokaComponentBase caches a
 * single module reference per component).
 *
 * The corner handle needs a two-axis drag. Stacking two makeResizable calls on the
 * same element does not work: moka-drag.js guards against double-attach via
 * splitter._mokaResize, so the second call is a no-op.
 *
 * The relative import resolves against this module's own URL, so it keeps working
 * when the host app is served from a sub-path.
 */

import { makeResizable, removeResizable } from '../../Moka.Red.Core/moka-drag.js';

export { makeResizable, removeResizable };

export function makeCornerResizable(dotNetRef, element, handle, options) {
	if (!element || !handle) return;
	if (handle._mokaCornerResize) return;

	const opts = options || {};
	// 50px floor matches the single-axis default in moka-drag.js.
	const minWidth = parsePx(opts.minWidth) ?? 50;
	const maxWidth = parsePx(opts.maxWidth) ?? Infinity;
	const minHeight = parsePx(opts.minHeight) ?? 50;
	const maxHeight = parsePx(opts.maxHeight) ?? Infinity;
	const callbackMethod = opts.callbackMethod || 'OnCornerResized';

	function onPointerDown(e) {
		e.preventDefault();
		e.stopPropagation();

		const startX = e.clientX;
		const startY = e.clientY;
		const startWidth = element.offsetWidth;
		const startHeight = element.offsetHeight;

		document.body.style.userSelect = 'none';
		document.body.style.cursor = 'nwse-resize';

		// Iframes swallow pointer events, which would strand the drag mid-gesture.
		const iframes = document.querySelectorAll('iframe');
		iframes.forEach(f => f.style.pointerEvents = 'none');

		function calcWidth(ev) {
			return Math.min(maxWidth, Math.max(minWidth, startWidth + (ev.clientX - startX)));
		}

		function calcHeight(ev) {
			return Math.min(maxHeight, Math.max(minHeight, startHeight + (ev.clientY - startY)));
		}

		function onPointerMove(ev) {
			element.style.width = calcWidth(ev) + 'px';
			element.style.height = calcHeight(ev) + 'px';
		}

		function onPointerUp(ev) {
			document.removeEventListener('pointermove', onPointerMove);
			document.removeEventListener('pointerup', onPointerUp);

			document.body.style.userSelect = '';
			document.body.style.cursor = '';
			iframes.forEach(f => f.style.pointerEvents = '');

			const width = calcWidth(ev);
			const height = calcHeight(ev);

			// Hand the size back to Blazor rather than leaving it as an inline override,
			// so a one-way bound Width/Height still wins on the next render.
			element.style.width = '';
			element.style.height = '';

			if (dotNetRef) {
				dotNetRef.invokeMethodAsync(callbackMethod, width, height);
			}
		}

		document.addEventListener('pointermove', onPointerMove);
		document.addEventListener('pointerup', onPointerUp);
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

function parsePx(value) {
	if (!value) return null;
	if (typeof value === 'number') return value;
	const match = String(value).match(/^(\d+(?:\.\d+)?)\s*px$/i);
	return match ? parseFloat(match[1]) : null;
}
