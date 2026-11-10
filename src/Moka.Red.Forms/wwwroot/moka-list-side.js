// Which side of its field an attached list opened on, for MokaSelect, MokaAutoComplete and MokaTreeSelect in the
// Attached look. The list and the field join into one shape, so the field squares the corners that meet the list.
//
// Core's moka-popup.js flips a list above its field when the window has no room below, through CSS anchor
// positioning or its own coordinates, and neither tells the page which side it chose. So this measures, and puts
// data-moka-above on the host while the list sits above the field. It measures again on scroll and resize, since a
// list can flip while it's open, and calls nothing in .NET.

const watched = new Map();

/**
 * Starts marking the host while the list is above the field. Calling it again with the same key starts over.
 * @param {string} key - Unique on the page; unwatch() takes the same key.
 * @param {HTMLElement} host - The element that gets data-moka-above.
 * @param {HTMLElement} field - The element the list is placed against.
 * @param {HTMLElement} list - The open list.
 */
export function watch(key, host, field, list) {
	unwatch(key);
	if (!host?.isConnected || !field?.isConnected || !list?.isConnected) return;

	const state = { host, field, list, frame: 0 };
	state.onMove = () => {
		if (state.frame) return;
		state.frame = requestAnimationFrame(() => {
			state.frame = 0;
			measure(state);
		});
	};

	window.addEventListener('scroll', state.onMove, { capture: true, passive: true });
	window.addEventListener('resize', state.onMove, { passive: true });
	state.observer = new ResizeObserver(state.onMove);
	state.observer.observe(list);
	state.observer.observe(field);
	watched.set(key, state);

	measure(state);
}

/**
 * Stops watching and takes the mark off the host. Does nothing for a key that isn't watched.
 * @param {string} key - The key watch() was given.
 */
export function unwatch(key) {
	const state = watched.get(key);
	if (!state) return;

	watched.delete(key);
	window.removeEventListener('scroll', state.onMove, true);
	window.removeEventListener('resize', state.onMove);
	state.observer.disconnect();
	cancelAnimationFrame(state.frame);
	state.host.removeAttribute('data-moka-above');
}

function measure(state) {
	if (!state.list.isConnected || !state.field.isConnected) return;

	const above = state.list.getBoundingClientRect().top < state.field.getBoundingClientRect().top;
	state.host.toggleAttribute('data-moka-above', above);
}
