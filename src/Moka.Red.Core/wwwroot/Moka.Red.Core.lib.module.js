/**
 * Moka.Red.Core's JavaScript initializer. Blazor loads it at startup and calls the hook for the app's hosting model.
 * It registers @onmokakeydown: a keydown whose arguments say who owns the press, which only the browser can tell.
 * It also starts the copy listener (moka-copy.js) and registers its events: @onmokacopied, how a copy went, and
 * @onmokacopyask, an element asking .NET for the text to copy. On an Apple platform it marks the page with
 * data-moka-platform="apple", so a key cap for a shortcut's Mod key reads Cmd there and Ctrl elsewhere.
 */

import { isApplePlatform, keyOwner, inModal } from './moka-keys.js';
import { listenForCopies } from './moka-copy.js';

function register(blazor) {
	blazor.registerCustomEventType('mokakeydown', {
		browserEventName: 'keydown',
		createEventArgs: e => ({
			type: e.type,
			key: e.key,
			code: e.code,
			location: e.location,
			repeat: e.repeat,
			ctrlKey: e.ctrlKey,
			shiftKey: e.shiftKey,
			altKey: e.altKey,
			metaKey: e.metaKey,
			isComposing: e.isComposing,
			owner: keyOwner(e),
			inModal: inModal(e.target)
		})
	});

	blazor.registerCustomEventType('mokacopied', {
		createEventArgs: e => ({ copied: e.detail?.copied === true })
	});

	blazor.registerCustomEventType('mokacopyask', {
		createEventArgs: () => ({})
	});

	listenForCopies();
	markPlatform();
}

function markPlatform() {
	if (isApplePlatform) {
		document.documentElement.setAttribute('data-moka-platform', 'apple');
	}
}

/** Blazor Web App. An enhanced page update replaces the document's attributes with the server's, so the mark goes back. */
export function afterWebStarted(blazor) {
	register(blazor);
	blazor.addEventListener('enhancedload', markPlatform);
}

/** Blazor Server, Blazor WebAssembly and Blazor Hybrid. */
export function afterStarted(blazor) {
	register(blazor);
}
