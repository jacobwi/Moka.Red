/**
 * MokaMenu JS module - what a menu showing only icons needs from the browser.
 *
 * On the rail a group can't open in place, so it opens beside its icon as a flyout: the group's own element, shown in
 * the top layer through Core's moka-popup.js, so its links keep their state and the sidebar's overflow doesn't cut it
 * off. The flyout is a disclosure, as site navigation is (WAI-ARIA's disclosure navigation pattern): the parent is a
 * button with aria-expanded, and its links stay links.
 *
 * Bound while closed, per menu: one keydown listener that opens a group with the arrow pointing at its flyout, and for
 * an adaptive menu a MutationObserver on its ancestors' data-moka-rail, which tells .NET only when the rail comes or
 * goes. Bound only while a flyout is open: its keys, a press outside it and focus leaving it.
 */

import { open as openPopup, close as closePopup } from '../../Moka.Red.Core/moka-popup.js';

// The flyout of a parent that is also a page starts with a link to that page, and a group inside it that is a page as
// well has its arrow button beside its link.
const ROWS = 'a.moka-menu-item[href], a.moka-menu-item__flyout-link[href], button.moka-menu-item:not(:disabled), '
	+ 'button.moka-menu-item__toggle:not(:disabled)';
const RAIL = 'data-moka-rail';

const rails = new WeakMap();
const flyouts = new Map();

// The parent a key opened, whose flyout takes focus when it shows. A mouse or Enter leaves focus on the parent.
let focusOnOpen = null;

function underRail(nav) {
	return !!nav.parentElement?.closest(`[${RAIL}]`);
}

function showsRail(nav) {
	return nav.classList.contains('moka-menu--collapsed')
		|| (nav.classList.contains('moka-menu--adaptive') && underRail(nav));
}

// The flyout opens on the side of its parent with more room, away from the window edge the menu sits against.
function sideOf(anchor) {
	const box = anchor.getBoundingClientRect();
	return box.left + box.width / 2 > document.documentElement.clientWidth / 2 ? 'left' : 'right';
}

function towards(side) {
	return side === 'left' ? 'ArrowLeft' : 'ArrowRight';
}

function back(side) {
	return side === 'left' ? 'ArrowRight' : 'ArrowLeft';
}

function panelOf(button) {
	const id = button.getAttribute('aria-controls');
	return id ? document.getElementById(id) : null;
}

// A closed group inside the flyout is inert, so its links are skipped.
function rows(panel) {
	return [...panel.querySelectorAll(ROWS)].filter(row => !row.closest('[inert]'));
}

function focusEdge(panel, last) {
	const list = rows(panel);
	(last ? list[list.length - 1] : list[0])?.focus();
}

function step(panel, from, delta) {
	const list = rows(panel);
	if (list.length === 0) return;

	const at = list.indexOf(from);
	const next = at < 0 ? (delta > 0 ? 0 : list.length - 1) : (at + delta + list.length) % list.length;
	list[next].focus();
}

function isRailParent(nav, element) {
	return element instanceof HTMLElement
		&& element.classList.contains('moka-menu-item--has-children')
		&& !element.disabled
		&& !element.closest('.moka-menu-item__submenu')
		&& showsRail(nav);
}

function openFromKeyboard(nav, e) {
	if (e.defaultPrevented || e.isComposing || e.altKey || e.ctrlKey || e.metaKey || e.shiftKey) return;

	const button = e.target;
	if (!isRailParent(nav, button) || e.key !== towards(sideOf(button))) return;

	e.preventDefault();
	const panel = panelOf(button);
	if (panel?.matches(':popover-open')) {
		focusEdge(panel, false);
		return;
	}

	// The button's own click opens it, the same way the mouse does.
	focusOnOpen = button;
	button.click();
}

/**
 * Starts listening for the key that opens a group on the rail, and for an adaptive menu, for the rail coming and going.
 * Binding again takes the new reporter.
 * @param {HTMLElement} nav - The menu's root.
 * @param {object|null} reporter - For an adaptive menu, a reference whose OnRailChanged hears the rail come and go.
 * @returns {boolean} Whether the menu sits under an element with data-moka-rail right now.
 */
export function bindRail(nav, reporter) {
	if (!nav) return false;

	let state = rails.get(nav);
	if (!state) {
		state = { observer: null, rail: false };
		state.keydown = e => openFromKeyboard(nav, e);
		nav.addEventListener('keydown', state.keydown);
		rails.set(nav, state);
	}

	state.observer?.disconnect();
	state.observer = null;
	state.rail = underRail(nav);

	if (reporter) {
		// The attribute can sit on any ancestor (MokaAppShell puts it on <html>), and only it counts, so only it is watched.
		state.observer = new MutationObserver(() => {
			const rail = underRail(nav);
			if (rail !== state.rail) {
				state.rail = rail;
				reporter.invokeMethodAsync('OnRailChanged', rail).catch(() => { });
			}
		});
		for (let node = nav.parentElement; node; node = node.parentElement) {
			state.observer.observe(node, { attributes: true, attributeFilter: [RAIL] });
		}
	}

	return state.rail;
}

/**
 * Stops what bindRail started.
 * @param {HTMLElement} nav - The menu's root.
 */
export function unbindRail(nav) {
	const state = nav && rails.get(nav);
	if (!state) return;

	rails.delete(nav);
	state.observer?.disconnect();
	nav.removeEventListener('keydown', state.keydown);
}

function inside(state, element) {
	return !!element && (state.panel.contains(element) || state.anchor.contains(element));
}

// Asks once until .NET has answered: a press outside also moves focus out.
function requestClose(state) {
	if (state.closing) return;

	state.closing = true;
	state.reporter.invokeMethodAsync('CloseFlyoutAsync').catch(() => { });
}

function leave(state) {
	state.anchor.focus();
	requestClose(state);
}

function onPanelKey(state, e) {
	if (e.defaultPrevented || e.isComposing || e.altKey || e.ctrlKey || e.metaKey) return;

	switch (e.key) {
		case 'ArrowDown':
			step(state.panel, e.target, 1);
			break;
		case 'ArrowUp':
			step(state.panel, e.target, -1);
			break;
		case 'Home':
			focusEdge(state.panel, false);
			break;
		case 'End':
			focusEdge(state.panel, true);
			break;
		case 'Escape':
			leave(state);
			break;
		default:
			if (e.key !== back(state.side)) return;
			leave(state);
	}

	e.preventDefault();
}

function onAnchorKey(state, e) {
	if (e.defaultPrevented || e.isComposing || e.altKey || e.ctrlKey || e.metaKey) return;

	if (e.key === 'ArrowDown') {
		e.preventDefault();
		focusEdge(state.panel, false);
	} else if (e.key === 'Escape') {
		e.preventDefault();
		requestClose(state);
	}
}

/**
 * Shows a group's flyout beside its parent, after the render that opened it, and listens while it's open.
 * @param {string} key - Unique on the page; closeFlyout takes the same key.
 * @param {HTMLElement} panel - The group's element.
 * @param {HTMLElement} anchor - The parent's button.
 * @param {object} reporter - A reference whose CloseFlyoutAsync closes it: Escape, the arrow back, a press outside, or
 *     focus leaving it.
 */
export function openFlyout(key, panel, anchor, reporter) {
	if (!panel || !anchor) return;

	const focus = focusOnOpen === anchor;
	focusOnOpen = null;
	stopListening(flyouts.get(key));

	const side = sideOf(anchor);
	openPopup(key, panel, anchor, null, { placement: `${side}-start` });

	const state = { key, panel, anchor, reporter, side, closing: false };
	state.onPanelKey = e => onPanelKey(state, e);
	state.onAnchorKey = e => onAnchorKey(state, e);
	state.onOutside = e => {
		if (!inside(state, e.target)) requestClose(state);
	};

	// Looked at once the current task is done, since Chrome raises focusout while Blazor is still applying a render.
	// Focus that fell to the page, such as from a group inside that closed, hasn't left.
	state.onFocusOut = e => {
		if (inside(state, e.relatedTarget)) return;

		setTimeout(() => {
			const active = document.activeElement;
			if (active && active !== document.body && !inside(state, active)) requestClose(state);
		});
	};

	panel.addEventListener('keydown', state.onPanelKey);
	panel.addEventListener('focusout', state.onFocusOut);
	anchor.addEventListener('keydown', state.onAnchorKey);
	anchor.addEventListener('focusout', state.onFocusOut);
	document.addEventListener('pointerdown', state.onOutside, true);
	flyouts.set(key, state);

	if (focus) focusEdge(panel, false);
}

function stopListening(state) {
	if (!state) return;

	flyouts.delete(state.key);
	state.panel.removeEventListener('keydown', state.onPanelKey);
	state.panel.removeEventListener('focusout', state.onFocusOut);
	state.anchor.removeEventListener('keydown', state.onAnchorKey);
	state.anchor.removeEventListener('focusout', state.onFocusOut);
	document.removeEventListener('pointerdown', state.onOutside, true);
}

/**
 * Takes a flyout out of the top layer, after the render that closed it, and stops listening.
 * @param {string} key - The key openFlyout was given.
 * @param {boolean} returnFocus - Hand focus back to the parent if it was in the flyout or fell to the page with it: a
 *     link followed or a button pressed inside. Not after a press outside or focus leaving, which put it elsewhere.
 */
export function closeFlyout(key, returnFocus) {
	const state = flyouts.get(key);
	stopListening(state);

	const active = document.activeElement;
	const lost = !active || active === document.body || !!state?.panel.contains(active);
	closePopup(key);

	if (returnFocus && lost && state?.anchor.isConnected) {
		state.anchor.focus();
	}
}
