/**
 * MokaMediaGallery JS module - focus handling and the scroll lock for the lightbox, which is a
 * modal dialog.
 *
 * Opening moves focus into the lightbox, keeps Tab inside it and locks the page's scrolling.
 * Closing gives the lock back and hands focus to the thumbnail of the image on show. Escape and
 * the arrow keys themselves are handled in .NET.
 *
 * The lock is Core's, the one dialogs and bottom sheets take through moka-dialog.js. The relative
 * import resolves against this module's own URL to the same module, so they share one count and
 * a lightbox closing over a dialog leaves the dialog's lock in place.
 */

import { lockBodyScroll, unlockBodyScroll } from '../../Moka.Red.Core/moka-scroll-lock.js';

const FOCUSABLE = 'button:not([disabled]), [href], input:not([disabled]), [tabindex]:not([tabindex="-1"])';

// Keys that would scroll the page behind the lightbox. The .NET handler still gets them.
const SCROLL_KEYS = ['ArrowLeft', 'ArrowRight', 'ArrowUp', 'ArrowDown', 'PageUp', 'PageDown', 'Home', 'End'];

// Gallery root -> MutationObserver of its open lightbox. A gallery in here also holds one scroll
// lock, given back when it leaves.
const openLightboxes = new WeakMap();

function trapTab(lightbox, e) {
	const focusable = Array.from(lightbox.querySelectorAll(FOCUSABLE));
	if (focusable.length === 0) {
		e.preventDefault();
		return;
	}

	const first = focusable[0];
	const last = focusable[focusable.length - 1];
	const active = document.activeElement;

	if (e.shiftKey && (active === first || active === lightbox)) {
		e.preventDefault();
		last.focus();
	} else if (!e.shiftKey && active === last) {
		e.preventDefault();
		first.focus();
	}
}

// Stops watching the gallery's lightbox and gives back its scroll lock, once per open.
function release(root) {
	const observer = openLightboxes.get(root);
	if (!observer) return;

	observer.disconnect();
	openLightboxes.delete(root);
	unlockBodyScroll();
}

/**
 * Takes focus into a lightbox that has just opened, keeps it there, and locks the page's scrolling.
 * @param {HTMLElement} root - The gallery's root element.
 * @param {HTMLElement} lightbox - The lightbox element, role="dialog".
 */
export function openLightbox(root, lightbox) {
	if (!root || !lightbox) return;

	// Opened again without a close in between: the gallery already holds a lock, and a second one
	// would never be given back.
	const previous = openLightboxes.get(root);
	if (previous) {
		previous.disconnect();
	} else {
		lockBodyScroll();
	}

	lightbox.addEventListener('keydown', e => {
		if (e.key === 'Tab') {
			trapTab(lightbox, e);
			return;
		}

		// With Alt, Ctrl or Cmd these are browser shortcuts (Alt+Left is back), so they stay.
		if (e.altKey || e.ctrlKey || e.metaKey) return;

		if (SCROLL_KEYS.includes(e.key) || (e.key === ' ' && e.target === lightbox)) {
			e.preventDefault();
		}
	});

	// The previous and next buttons disappear at either end. When the one that had focus goes,
	// focus returns to the lightbox, where Escape and the arrows still reach the .NET handler.
	const observer = new MutationObserver(() => {
		if (lightbox.isConnected && !lightbox.contains(document.activeElement)) {
			lightbox.focus({ preventScroll: true });
		}
	});
	observer.observe(lightbox, { childList: true, subtree: true });
	openLightboxes.set(root, observer);

	lightbox.focus({ preventScroll: true });
}

/**
 * Called after the lightbox has left the page. Scrolling comes back, and since focus went with the
 * lightbox, it moves to the thumbnail of the image that was on show.
 * @param {HTMLElement} root - The gallery's root element.
 * @param {number} index - The index of the image the lightbox showed last.
 */
export function closeLightbox(root, index) {
	if (!root) return;

	release(root);

	const active = document.activeElement;
	if (active && active !== document.body) return;

	root.querySelectorAll('.moka-media-gallery-item')[index]?.focus();
}

/**
 * Called when the gallery goes away with its lightbox still open, so it never renders closed.
 * Gives back the scroll lock and leaves focus alone, since the thumbnails are going too.
 * @param {HTMLElement} root - The gallery's root element.
 */
export function disposeLightbox(root) {
	if (root) release(root);
}
