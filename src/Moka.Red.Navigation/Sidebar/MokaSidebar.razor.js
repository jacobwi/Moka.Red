/**
 * MokaSidebar JS module - focus handling for an overlay sidebar.
 *
 * An open overlay sidebar covers the page on a backdrop, so it works like a modal dialog: opening
 * moves focus into it and keeps Tab inside it, and closing hands focus back to where it was.
 * Escape itself is handled in .NET. Dialogs get this from moka-dialog.js, but Navigation does not
 * reference Feedback, so the sidebar carries its own small version.
 *
 * The state lives in a WeakMap keyed by the sidebar, and the listener on the sidebar itself, so a
 * sidebar that leaves the page takes both with it and there is nothing to dispose.
 *
 * The page scroll lock is Core's shared, counted one, re-exported here so the sidebar's single
 * module serves every call. .NET pairs the calls itself, since a sidebar removed while open is
 * gone from the page by the time it is disposed.
 */

export { lockBodyScroll, unlockBodyScroll } from '../../Moka.Red.Core/moka-scroll-lock.js';

// Everything that can be in the tab order. A negative tabindex takes any of them out.
const FOCUSABLE = [
	'a[href]',
	'button:not([disabled])',
	'input:not([disabled]):not([type="hidden"])',
	'select:not([disabled])',
	'textarea:not([disabled])',
	'summary',
	'[contenteditable]:not([contenteditable="false"])',
	'[tabindex]'
].join(', ');

// Sidebar -> { onKeyDown, previous } while it is open as an overlay.
const openSidebars = new WeakMap();

function tabbableWithin(sidebar) {
	return Array.from(sidebar.querySelectorAll(FOCUSABLE))
		.filter(el => (!el.hasAttribute('tabindex') || el.tabIndex >= 0)
			&& !!(el.offsetWidth || el.offsetHeight || el.getClientRects().length));
}

// Tab and Shift+Tab go to the next or previous element in the tab order, counted in document order
// from the focused element, and wrap at the ends. Counting from focus keeps this right when focus
// sits on the sidebar itself or on an element taken out of the tab order.
function moveFocus(sidebar, backwards) {
	const tabbable = tabbableWithin(sidebar);
	if (tabbable.length === 0) {
		sidebar.focus();
		return;
	}

	const from = document.activeElement;
	const ordered = backwards ? tabbable.slice().reverse() : tabbable;
	const side = backwards ? Node.DOCUMENT_POSITION_PRECEDING : Node.DOCUMENT_POSITION_FOLLOWING;
	const next = from && sidebar.contains(from)
		? ordered.find(el => from.compareDocumentPosition(el) & side)
		: null;
	(next || ordered[0]).focus();
}

/**
 * Moves focus into an overlay sidebar that has just opened, and keeps Tab inside it until it
 * closes. Focus goes to an element marked autofocus or data-autofocus, else the first control,
 * else the sidebar itself. Content that already took focus inside the sidebar keeps it.
 * @param {HTMLElement} sidebar - The sidebar's nav element.
 */
export function openOverlay(sidebar) {
	if (!sidebar || openSidebars.has(sidebar)) return;

	const onKeyDown = e => {
		if (e.key !== 'Tab' || e.altKey || e.ctrlKey || e.metaKey || e.defaultPrevented) return;

		e.preventDefault();
		moveFocus(sidebar, e.shiftKey);
	};

	const active = document.activeElement;
	sidebar.addEventListener('keydown', onKeyDown);
	openSidebars.set(sidebar, { onKeyDown, previous: sidebar.contains(active) ? null : active });

	if (sidebar.contains(active)) return;

	const requested = sidebar.querySelector('[autofocus], [data-autofocus]');
	(requested || tabbableWithin(sidebar)[0] || sidebar).focus();
}

/**
 * Lets go of Tab and hands focus back to where it was before the sidebar opened. Focus only moves
 * when it is still in the sidebar, or nowhere because the closed sidebar went inert, so a close
 * that happens while the user is elsewhere does not pull focus away.
 * @param {HTMLElement} sidebar - The sidebar's nav element.
 * @param {boolean} [returnFocus=true] - False when the sidebar stays on screen, as an inline one.
 */
export function closeOverlay(sidebar, returnFocus = true) {
	const state = sidebar && openSidebars.get(sidebar);
	if (!state) return;

	sidebar.removeEventListener('keydown', state.onKeyDown);
	openSidebars.delete(sidebar);
	if (!returnFocus) return;

	const active = document.activeElement;
	const focusHere = !active || active === document.body || sidebar.contains(active);
	if (focusHere && state.previous && state.previous.isConnected) {
		state.previous.focus();
	}
}
