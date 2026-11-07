/**
 * MokaBootScreen: while a full-screen boot screen shows, the page under it is inert, so Tab and a screen
 * reader's cursor stay on the screen, and it doesn't scroll. Everything beside the screen and beside each of
 * its ancestors is made inert, and a MutationObserver does the same to whatever Blazor adds there meanwhile.
 * release() undoes only what hold() did.
 *
 * The scroll lock is Core's shared, counted one, imported relative to this module's own URL.
 */
import { lockBodyScroll, unlockBodyScroll } from '../../Moka.Red.Core/moka-scroll-lock.js';

const holds = new Map();

// The status region beside the screen has to stay readable, and Blazor's own reconnect and error UI and any
// top-layer dialog have to stay usable.
const keep = '[data-moka-boot-live], dialog, [popover], #components-reconnect-modal, #blazor-error-ui, script, style, link, template';

function makeInert(element, made) {
	if (element.nodeType !== Node.ELEMENT_NODE || element.inert || element.matches(keep)) return;
	element.inert = true;
	made.add(element);
}

/** Makes the page inert around a boot screen until release() is called with the same key. */
export function hold(key, screen) {
	if (!screen || holds.has(key)) return;

	const made = new Set();
	const observers = [];
	for (let node = screen; node.parentElement && node !== document.body; node = node.parentElement) {
		const onPath = node;
		const parent = node.parentElement;
		for (const sibling of parent.children) {
			if (sibling !== onPath) makeInert(sibling, made);
		}

		const observer = new MutationObserver(records => {
			for (const record of records) {
				for (const added of record.addedNodes) {
					if (added !== onPath) makeInert(added, made);
				}
			}
		});
		observer.observe(parent, { childList: true });
		observers.push(observer);
	}

	const focused = document.activeElement;
	if (focused && focused !== document.body && !screen.contains(focused) && typeof focused.blur === 'function') {
		focused.blur();
	}

	lockBodyScroll();
	holds.set(key, { made, observers });
}

/** Gives the page back. Safe to call when nothing is held. */
export function release(key) {
	const held = holds.get(key);
	if (!held) return;

	holds.delete(key);
	for (const observer of held.observers) observer.disconnect();
	for (const element of held.made) element.inert = false;
	unlockBodyScroll();
}
