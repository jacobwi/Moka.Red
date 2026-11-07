/**
 * MokaAppShell JS module - what the rail and the phone's drawer need from the browser.
 *
 * Rows or rail is one attribute, data-moka-rail, and only script sets it: the inline script a static render writes
 * before first paint, then this. It's on <html> rather than the shell's root because Blazor replaces a prerendered
 * shell with a new element when it goes interactive, and <html> it never replaces. It's never set on a phone, so the
 * stylesheets ask one question.
 * A window crossing a width is settled here and never reaches .NET.
 *
 * The focus trap, scroll lock, Escape and backdrop of the drawer are MokaSidebar's. This adds the swipe.
 */

const STORAGE_KEY = 'moka-app-shell-rail';
const PHONE = '(max-width: 720px)';
const NARROW = '(max-width: 1279.98px)';

// A swipe has to go this far left, and be this much more sideways than up or down.
const SWIPE_DISTANCE = 56;
const SWIPE_SLOPE = 1.5;

// Key -> state. A key rather than the element: by the time a shell is disposed its element may be gone from the page,
// and these listeners sit on media queries, which would outlive it.
const shells = new Map();

function kept() {
	try {
		return sessionStorage.getItem(STORAGE_KEY);
	} catch {
		return null;
	}
}

function keep(value) {
	try {
		if (value) {
			sessionStorage.setItem(STORAGE_KEY, value);
		} else {
			sessionStorage.removeItem(STORAGE_KEY);
		}
	} catch {
		// Storage can be blocked. The flip still holds until the page reloads.
	}
}

function applyRail(shell) {
	const choice = kept() ?? shell.getAttribute('data-moka-rail-choice');
	const rail = !matchMedia(PHONE).matches
		&& (choice === 'collapsed' || (choice !== 'expanded' && matchMedia(NARROW).matches));
	document.documentElement.toggleAttribute('data-moka-rail', rail);
}

function showingRail() {
	return document.documentElement.hasAttribute('data-moka-rail');
}

function closeDrawer(state) {
	state.dotNet.invokeMethodAsync('CloseDrawerAsync').catch(() => {});
}

function listenForSwipe(state) {
	const controller = new AbortController();
	const options = { passive: true, signal: controller.signal };
	let start = null;

	state.shell.addEventListener('touchstart', e => {
		start = e.touches.length === 1 ? { x: e.touches[0].clientX, y: e.touches[0].clientY } : null;
	}, options);

	state.shell.addEventListener('touchend', e => {
		if (!start) return;

		const touch = e.changedTouches[0];
		const dx = touch.clientX - start.x;
		const dy = touch.clientY - start.y;
		start = null;
		if (dx < -SWIPE_DISTANCE && Math.abs(dx) > Math.abs(dy) * SWIPE_SLOPE) {
			closeDrawer(state);
		}
	}, options);

	return controller;
}

/**
 * Starts watching the window's width for a shell.
 * @param {string} key - Names this shell in the other calls.
 * @param {HTMLElement} shell - The shell's root.
 * @param {object} dotNet - Reference used to close the drawer.
 * @returns {?string} The flip kept for this tab, 'collapsed' or 'expanded', or null.
 */
export function bindShell(key, shell, dotNet) {
	if (!shell) return null;

	const phone = matchMedia(PHONE);
	const narrow = matchMedia(NARROW);
	const state = { shell, dotNet, swipe: null };

	state.onNarrow = () => applyRail(shell);

	// Grown past a phone with the drawer out: nothing could close it, and the page would stay locked.
	state.onPhone = () => {
		applyRail(shell);
		if (!phone.matches && state.swipe) {
			closeDrawer(state);
		}
	};
	state.stop = () => {
		narrow.removeEventListener('change', state.onNarrow);
		phone.removeEventListener('change', state.onPhone);
		state.swipe?.abort();
	};

	narrow.addEventListener('change', state.onNarrow);
	phone.addEventListener('change', state.onPhone);
	shells.set(key, state);

	applyRail(shell);
	return kept();
}

/**
 * Stops watching.
 * @param {string} key - From bindShell.
 */
export function unbindShell(key) {
	shells.get(key)?.stop();
	shells.delete(key);

	// A page without a shell would otherwise keep drawing account cards and adaptive menus as a rail.
	if (shells.size === 0) {
		document.documentElement.removeAttribute('data-moka-rail');
	}
}

/**
 * The parent chose a rail: it replaces any flip kept for this tab. The choice is on the root by now.
 * @param {string} key - From bindShell.
 */
export function chooseRail(key) {
	const state = shells.get(key);
	if (!state) return;

	keep(null);
	applyRail(state.shell);
}

/**
 * Flips between the rail and the full menu, and keeps that for this tab.
 * @param {string} key - From bindShell.
 * @returns {?string} What it's now, 'collapsed' or 'expanded', or null on a phone, which has no rail to flip.
 */
export function flipRail(key) {
	const state = shells.get(key);
	if (!state || matchMedia(PHONE).matches) return null;

	const next = showingRail() ? 'expanded' : 'collapsed';
	keep(next);
	applyRail(state.shell);
	return next;
}

/**
 * Whether a shell shows the rail right now, for what opens beside the rail rather than above the full menu.
 * @param {HTMLElement} shell - The shell's root.
 * @returns {boolean}
 */
export function showsRail(shell) {
	return !!shell && showingRail();
}

/**
 * Listens for a swipe left only while the drawer is out.
 * @param {string} key - From bindShell.
 * @param {boolean} open - Whether the drawer is out.
 */
export function setDrawerOpen(key, open) {
	const state = shells.get(key);
	if (!state) return;

	state.swipe?.abort();
	state.swipe = open ? listenForSwipe(state) : null;
}
