import { bindFit, unbindFit } from '../../Moka.Red.Core/moka-fit.js';

// Only the actions can leave the row. Anything else an app puts among them keeps its place, takes up room, and never
// moves to the menu, which can only hold menu items.
const layout = {
	sides: [':scope > .moka-selection-bar-actions'],
	more: ':scope > .moka-selection-bar-more [data-moka-more]',
	filler: ':not(.moka-selection-action)',
	flexible: true
};

const DROPPED = 'data-moka-dropped';
const ITEM = '[role="menuitem"]';
const bars = new WeakMap();

function actionsOf(bar) {
	return bar.querySelector(':scope > .moka-selection-bar-actions');
}

function moreButtonOf(bar) {
	return bar.querySelector(':scope > .moka-selection-bar-more [data-moka-more]');
}

function menuOf(bar) {
	return bar.querySelector(':scope > .moka-selection-bar-more [role="menu"]');
}

function droppedIds(actions) {
	return [...actions.children]
		.filter(element => element.hasAttribute(DROPPED))
		.map(element => element.matches('[data-moka-action]') ? element : element.querySelector('[data-moka-action]'))
		.map(element => element?.getAttribute('data-moka-action'))
		.filter(Boolean);
}

function focusEdge(menu, last) {
	const items = [...menu.querySelectorAll(ITEM)];
	const item = last ? items[items.length - 1] : items[0];
	(item ?? menu).focus();
}

// The More button's own click opens and closes the menu, the same path as the mouse. It goes by the button's state,
// not by whether the menu is still in the page, and asks once until .NET has answered: a press outside also moves
// focus out, and a second click would open the menu again.
function close(bar) {
	const state = bars.get(bar);
	const button = moreButtonOf(bar);
	if (state && !state.closing && button?.getAttribute('aria-expanded') === 'true') {
		state.closing = true;
		button.click();
	}
}

/**
 * Keeps a selection bar on one row and tells .NET which actions went to the More menu.
 * @param {HTMLElement} bar The bar's toolbar element.
 * @param {object} reporter A DotNetObjectReference whose OnDroppedChanged hears the ids of the actions in the menu.
 */
export function bindSelectionBar(bar, reporter) {
	if (!bar || bars.has(bar)) {
		return;
	}

	const state = { reported: '', focusOnOpen: null };
	bars.set(bar, state);

	// moka-fit.js reports positions, which stay the same when the actions in them change, so the ids are read here
	// whenever an action leaves or returns, or the row's actions change.
	const actions = actionsOf(bar);
	if (actions) {
		const report = () => {
			const ids = droppedIds(actions);
			const key = ids.join(' ');
			if (key === state.reported) {
				return;
			}

			state.reported = key;
			reporter?.invokeMethodAsync('OnDroppedChanged', ids).catch(() => { });
		};

		state.watch = new MutationObserver(report);
		state.watch.observe(actions, { childList: true, subtree: true, attributes: true, attributeFilter: [DROPPED] });
	}

	state.keydown = e => onKeyDown(bar, state, e);
	// Looked at once the current task is done. Blazor removing the menu with focus in it raises a focusout in the middle
	// of applying the render, before the button's own update to closed, and a click then would open the menu again.
	state.focusout = e => {
		if (bar.contains(e.relatedTarget)) {
			return;
		}

		setTimeout(() => {
			if (!bar.contains(document.activeElement)) {
				close(bar);
			}
		});
	};
	bar.addEventListener('keydown', state.keydown);
	bar.addEventListener('focusout', state.focusout);

	bindFit(bar, null, layout);
}

/**
 * Lets go of a bar, before the render that removes it.
 * @param {HTMLElement} bar The bar's toolbar element.
 */
export function unbindSelectionBar(bar) {
	const state = bar && bars.get(bar);
	if (!state) {
		return;
	}

	bars.delete(bar);
	state.watch?.disconnect();
	stopWatchingOutside(state);
	bar.removeEventListener('keydown', state.keydown);
	bar.removeEventListener('focusout', state.focusout);
	unbindFit(bar);
}

/**
 * Called after the render that opens or closes the menu.
 * @param {HTMLElement} bar The bar's toolbar element.
 * @param {boolean} open Whether the menu is now open.
 */
export function menuShown(bar, open) {
	const state = bar && bars.get(bar);
	if (!state) {
		return;
	}

	state.closing = false;
	if (open) {
		const menu = menuOf(bar);
		if (menu && state.focusOnOpen) {
			focusEdge(menu, state.focusOnOpen === 'last');
		}

		state.focusOnOpen = null;
		watchOutside(bar, state);
		return;
	}

	stopWatchingOutside(state);

	// An item chosen from the keyboard had focus and went with the menu.
	const active = document.activeElement;
	if (!active || active === document.body) {
		moreButtonOf(bar)?.focus();
	}
}

// A press anywhere else closes the menu, also where a click doesn't move focus, as on a Mac.
function watchOutside(bar, state) {
	stopWatchingOutside(state);
	state.outside = e => {
		if (!bar.isConnected) {
			stopWatchingOutside(state);
		} else if (!bar.contains(e.target)) {
			close(bar);
		}
	};
	document.addEventListener('pointerdown', state.outside, true);
}

function stopWatchingOutside(state) {
	if (state.outside) {
		document.removeEventListener('pointerdown', state.outside, true);
		state.outside = null;
	}
}

// The menu button pattern. Items are buttons, so Enter and Space choose one the browser's own way.
function onKeyDown(bar, state, e) {
	if (e.isComposing) {
		return;
	}

	const button = moreButtonOf(bar);
	const menu = menuOf(bar);

	if (e.target === button) {
		if (e.key === 'ArrowDown' || e.key === 'ArrowUp') {
			e.preventDefault();
			const last = e.key === 'ArrowUp';
			if (menu) {
				focusEdge(menu, last);
			} else {
				state.focusOnOpen = last ? 'last' : 'first';
				button.click();
			}
		} else if ((e.key === 'Enter' || e.key === ' ') && !menu) {
			state.focusOnOpen = 'first';
		} else if (e.key === 'Escape' && menu) {
			e.preventDefault();
			close(bar);
		}

		return;
	}

	if (!menu || !menu.contains(e.target)) {
		return;
	}

	const items = [...menu.querySelectorAll(ITEM)];
	const index = items.indexOf(e.target.closest(ITEM));

	switch (e.key) {
		case 'ArrowDown':
			e.preventDefault();
			items[(index + 1) % items.length]?.focus();
			break;
		case 'ArrowUp':
			e.preventDefault();
			items[index < 0 ? items.length - 1 : (index - 1 + items.length) % items.length]?.focus();
			break;
		case 'Home':
			e.preventDefault();
			items[0]?.focus();
			break;
		case 'End':
			e.preventDefault();
			items[items.length - 1]?.focus();
			break;
		case 'Escape':
			e.preventDefault();
			button?.focus();
			close(bar);
			break;
		case 'Tab':
			close(bar);
			break;
	}
}
