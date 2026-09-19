/**
 * Moka.Red body scroll lock, shared across packages.
 *
 * Dialogs and bottom sheets take it through moka-dialog.js, which re-exports these two, and the
 * media gallery's lightbox imports it from its own module. The browser keeps one instance of a
 * module per URL, so they all share one count, and an overlay closing on top of another one
 * leaves the lower overlay's lock in place.
 *
 * Import it with a path relative to your own module (the browser resolves it against that
 * module's URL), or as ./_content/Moka.Red.Core/moka-scroll-lock.js from .NET.
 */

// Reference counted: stacked overlays (a dialog opening a bottom sheet, a lightbox over a dialog)
// each take a lock, and scrolling only comes back once the last of them releases.
let _scrollLockCount = 0;
let _previousOverflow = '';

/** Stops the page behind an overlay from scrolling. Pair every call with one unlockBodyScroll. */
export function lockBodyScroll() {
	if (_scrollLockCount === 0) {
		_previousOverflow = document.body.style.overflow;
		document.body.style.overflow = 'hidden';
	}
	_scrollLockCount++;
}

/** Releases one lock. The page scrolls again when the last lock is released. */
export function unlockBodyScroll() {
	if (_scrollLockCount === 0) return;

	_scrollLockCount--;
	if (_scrollLockCount === 0) {
		document.body.style.overflow = _previousOverflow;
		_previousOverflow = '';
	}
}
