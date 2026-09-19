/**
 * MokaBottomSheet - drag the handle down to close the sheet.
 *
 * While the handle is dragged the sheet follows the pointer down, never up. Let go far enough
 * down and the component closes the sheet through its usual close path, so OpenChanged fires and
 * focus goes back where it was. Let go short of that and the sheet slides back.
 *
 * The listener lives on the handle and goes away with it: the handle is rendered again every time
 * the sheet opens, and removed when it closes.
 */

// A drag closes the sheet past this share of the sheet's height, or past this many px on a tall
// sheet, whichever comes first.
const CLOSE_SHARE = 0.3;
const CLOSE_DISTANCE = 120;

export function bindDismissDrag(dotNetRef, sheet, handle) {
	if (!sheet || !handle || handle._mokaSheetDrag) return;

	function onPointerDown(e) {
		if (e.button !== 0) return;
		e.preventDefault();

		const startY = e.clientY;
		const threshold = Math.min(sheet.offsetHeight * CLOSE_SHARE, CLOSE_DISTANCE);
		let offset = 0;

		sheet.style.transition = 'none';

		function onPointerMove(ev) {
			offset = Math.max(0, ev.clientY - startY);
			sheet.style.transform = `translateY(${offset}px)`;
		}

		// A cancelled pointer (the browser took the touch for a gesture) never closes the sheet.
		function onPointerUp(ev) {
			document.removeEventListener('pointermove', onPointerMove);
			document.removeEventListener('pointerup', onPointerUp);
			document.removeEventListener('pointercancel', onPointerUp);

			if (ev.type === 'pointerup' && offset > threshold) {
				// The sheet stays where it was dragged to until the component removes it.
				dotNetRef.invokeMethodAsync('OnHandleDraggedDown');
				return;
			}

			sheet.style.transition = 'transform var(--moka-transition-normal)';
			sheet.style.transform = '';
			sheet.addEventListener('transitionend', () => { sheet.style.transition = ''; }, { once: true });
		}

		document.addEventListener('pointermove', onPointerMove);
		document.addEventListener('pointerup', onPointerUp);
		document.addEventListener('pointercancel', onPointerUp);
	}

	handle.addEventListener('pointerdown', onPointerDown);
	handle._mokaSheetDrag = true;
}
