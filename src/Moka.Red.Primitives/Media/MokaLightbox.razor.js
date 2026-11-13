/**
 * MokaLightbox JS module - focus handling and the scroll lock for the lightbox, which is a modal dialog.
 *
 * Opening moves focus into the lightbox, keeps Tab inside it and locks the page's scrolling. Closing gives the lock
 * back and hands focus to what had it before, unless something else took it meanwhile (a gallery focuses the tile of
 * the image last on show). Escape, the arrows, Home and End are handled in .NET; here they only stop scrolling the page.
 *
 * The lock is Core's, the one dialogs and bottom sheets take through moka-dialog.js. The relative import resolves
 * against this module's own URL to the same module, so they share one count, and a lightbox closing over a dialog
 * leaves the dialog's lock in place.
 */

import { lockBodyScroll, unlockBodyScroll } from '../../Moka.Red.Core/moka-scroll-lock.js';
import { keyOwner } from '../../Moka.Red.Core/moka-keys.js';

const FOCUSABLE = 'a[href], button:not([disabled]), input:not([disabled]):not([type="hidden"]), select:not([disabled]), '
	+ 'textarea:not([disabled]), [tabindex]';

// Keys that would scroll the page behind the lightbox. The .NET handler still gets them.
const SCROLL_KEYS = new Set(['ArrowLeft', 'ArrowRight', 'ArrowUp', 'ArrowDown', 'PageUp', 'PageDown', 'Home', 'End']);

// keyOwner's answers for a field and for a control that uses the key itself, such as a slider in the facts.
const OWNED_BY_FIELD = 1;
const OWNED_BY_CONTROL = 2;

// Key -> the open lightbox's listeners, its observer and where focus came from. Each one holds one scroll lock.
const lightboxes = new Map();

function tabbable(dialog) {
	return Array.from(dialog.querySelectorAll(FOCUSABLE)).filter(el => el.tabIndex >= 0
		&& (el.offsetWidth || el.offsetHeight || el.getClientRects().length));
}

function trapTab(dialog, e) {
	const focusable = tabbable(dialog);
	if (focusable.length === 0) {
		e.preventDefault();
		return;
	}

	const first = focusable[0];
	const last = focusable[focusable.length - 1];
	const active = document.activeElement;

	if (e.shiftKey && (active === first || active === dialog || !dialog.contains(active))) {
		e.preventDefault();
		last.focus();
	} else if (!e.shiftKey && (active === last || !dialog.contains(active))) {
		e.preventDefault();
		first.focus();
	}
}

function focusLost() {
	const active = document.activeElement;
	return !active || active === document.body;
}

function detach(state) {
	state.dialog.removeEventListener('keydown', state.onKeyDown);
	state.observer.disconnect();
}

/**
 * Takes focus into a lightbox that has just shown, keeps it there, and locks the page's scrolling.
 * @param {string} key - The lightbox's name for this module; closeLightbox takes the same one.
 * @param {HTMLElement} dialog - The lightbox element, role="dialog".
 */
export function openLightbox(key, dialog) {
	if (!key || !dialog) return;

	// Shown again without a close in between: it already holds a lock and knows where focus came from.
	let state = lightboxes.get(key);
	if (state) {
		detach(state);
	} else {
		lockBodyScroll();
		const active = document.activeElement;
		state = { previous: active && active !== document.body && !dialog.contains(active) ? active : null };
		lightboxes.set(key, state);
	}

	state.dialog = dialog;
	state.onKeyDown = e => {
		if (e.key === 'Tab') {
			if (!e.ctrlKey && !e.altKey && !e.metaKey && !e.defaultPrevented) trapTab(dialog, e);
			return;
		}

		// With Alt, Ctrl or Cmd these are browser shortcuts (Alt+Left is back), so they stay. A field or a control in
		// the facts keeps the keys it uses.
		if (e.altKey || e.ctrlKey || e.metaKey) return;
		const owner = keyOwner(e);
		if (owner === OWNED_BY_FIELD || owner === OWNED_BY_CONTROL) return;

		if (SCROLL_KEYS.has(e.key) || (e.key === ' ' && e.target === dialog)) {
			e.preventDefault();
		}
	};
	dialog.addEventListener('keydown', state.onKeyDown);

	// The previous and next buttons go away at either end. When the one that had focus goes, focus falls to the page;
	// it comes back to the lightbox, where Escape and the arrows still reach .NET.
	state.observer = new MutationObserver(() => {
		if (dialog.isConnected && focusLost()) dialog.focus({ preventScroll: true });
	});
	state.observer.observe(dialog, { childList: true, subtree: true });

	if (!dialog.contains(document.activeElement)) dialog.focus({ preventScroll: true });
}

/**
 * Called after the lightbox has left the page, or when it goes away while showing. Scrolling comes back, and focus,
 * which went with the lightbox, returns to where it was before it opened, unless something has taken it since.
 * @param {string} key - The name openLightbox was given.
 */
export function closeLightbox(key) {
	const state = lightboxes.get(key);
	if (!state) return;

	lightboxes.delete(key);
	detach(state);
	unlockBodyScroll();

	const inside = state.dialog.contains(document.activeElement);
	if ((focusLost() || inside) && state.previous?.isConnected) state.previous.focus();
}
