/**
 * Parses a shortcut string like "Ctrl+Shift+D" into a descriptor object.
 * @param {string} shortcut
 * @returns {{ ctrl: boolean, shift: boolean, alt: boolean, meta: boolean, key: string }}
 */
function parseShortcut(shortcut) {
	const parts = shortcut.split('+').map(p => p.trim().toLowerCase());
	return {
		ctrl: parts.includes('ctrl'),
		shift: parts.includes('shift'),
		alt: parts.includes('alt'),
		meta: parts.includes('meta'),
		key: parts.filter(p => !['ctrl', 'shift', 'alt', 'meta'].includes(p))[0] || '',
	};
}

/**
 * Checks if a keyboard event matches the parsed shortcut.
 * @param {KeyboardEvent} e
 * @param {{ ctrl: boolean, shift: boolean, alt: boolean, meta: boolean, key: string }} parsed
 * @returns {boolean}
 */
function matchesShortcut(e, parsed) {
	return (
		e.ctrlKey === parsed.ctrl &&
		e.shiftKey === parsed.shift &&
		e.altKey === parsed.alt &&
		e.metaKey === parsed.meta &&
		e.key.toLowerCase() === parsed.key
	);
}

/**
 * Listens for the overlay's keys: the shortcut opens or closes the panel, and Escape inside the panel closes it.
 * Each overlay gets its own listener and removes only that one, so an overlay disposed after a new one has
 * registered (a layout swap) leaves the new one's shortcut working.
 * @param {object} dotNetRef - DotNet object reference
 * @param {string} shortcut - Shortcut string, e.g. "Ctrl+Shift+D"
 * @returns {{ dispose: () => void }} What removes the listener.
 */
export function registerKeyboardShortcut(dotNetRef, shortcut) {
	const parsed = parseShortcut(shortcut);

	const listener = (e) => {
		const inPanel = e.target instanceof Element && e.target.closest('.moka-diag-panel') !== null;
		if (matchesShortcut(e, parsed)) {
			e.preventDefault();
			dotNetRef.invokeMethodAsync(inPanel ? 'CloseFromJs' : 'ToggleFromJs');
		} else if (inPanel && e.key === 'Escape' && !e.defaultPrevented) {
			e.preventDefault();
			dotNetRef.invokeMethodAsync('CloseFromJs');
		}
	};

	document.addEventListener('keydown', listener);
	return {
		dispose: () => document.removeEventListener('keydown', listener),
	};
}

/**
 * The WAI-ARIA tabs pattern's keys on a tab list: Left and Right move focus to the previous and next tab, wrapping, Home
 * and End to the first and last, mirrored in a right-to-left list. Activation is manual, as the pattern advises when a
 * tab's panel takes a moment to show: showing one is a round trip to the server and a new panel, so a tab is picked
 * only by Enter, Space or a click, which the tab, a button, handles itself. Moving focus costs no round trip.
 * @param {HTMLElement} list - The element with role="tablist".
 * @param {(tab: HTMLElement) => void} focus - Moves focus to a tab and brings it into view.
 * @returns {() => void} What stops listening.
 */
function listenForTabKeys(list, focus) {
	const onKeyDown = (e) => {
		if (e.altKey || e.ctrlKey || e.metaKey || e.shiftKey) {
			return;
		}

		const tabs = [...list.querySelectorAll('[role="tab"]')];
		const current = tabs.indexOf(e.target instanceof Element ? e.target.closest('[role="tab"]') : null);
		if (current < 0) {
			return;
		}

		const forward = getComputedStyle(list).direction === 'rtl' ? 'ArrowLeft' : 'ArrowRight';
		const back = forward === 'ArrowRight' ? 'ArrowLeft' : 'ArrowRight';
		let next;
		switch (e.key) {
			case forward:
				next = (current + 1) % tabs.length;
				break;
			case back:
				next = (current - 1 + tabs.length) % tabs.length;
				break;
			case 'Home':
				next = 0;
				break;
			case 'End':
				next = tabs.length - 1;
				break;
			default:
				return;
		}

		e.preventDefault();
		focus(tabs[next]);
	};

	list.addEventListener('keydown', onKeyDown);
	return () => list.removeEventListener('keydown', onKeyDown);
}

/**
 * Gives the full page's tab list the tabs pattern's keys (see listenForTabKeys). Its tabs wrap rather than scroll, so
 * the browser's own scroll into view on focus is enough.
 * @param {HTMLElement} list - The element with role="tablist".
 * @returns {{ dispose: () => void }} What stops listening.
 */
export function bindTabList(list) {
	if (!list) {
		return { dispose: () => { } };
	}

	return { dispose: listenForTabKeys(list, (tab) => tab.focus()) };
}

/**
 * Lets the panel's tab row scroll when its tabs don't fit, and gives its tab list the tabs pattern's keys (see
 * listenForTabKeys). The row gets data-moka-overflow while the tabs don't fit, which makes room for an arrow at each
 * end, and data-moka-more-start or data-moka-more-end while tabs are out of view on that side, which shows that side's
 * arrow. The room depends only on whether the row overflows, never on where it is scrolled, so revealing a tab can't
 * move it back under an arrow. An arrow scrolls about a row's width, and the tab showing, a clicked tab and a tab the
 * keys move to are kept in view. All of it stays in the browser.
 * @param {HTMLElement} row - The element holding the arrows and the tab list.
 * @returns {{ dispose: () => void }} What stops watching the row.
 */
export function bindTabRow(row) {
	const list = row?.querySelector('.moka-diag-tabs');
	if (!list) {
		return { dispose: () => { } };
	}

	const behavior = () => matchMedia('(prefers-reduced-motion: reduce)').matches ? 'auto' : 'smooth';

	// Measured against the row, the room the tabs have without the arrows, so tabs that would fit once the arrows go
	// don't keep them. Half a pixel of slack: a zoomed page reports fractional sizes that never quite meet.
	const update = () => {
		const overflow = list.scrollWidth > row.clientWidth + 0.5;
		row.toggleAttribute('data-moka-overflow', overflow);
		const end = list.scrollWidth - list.clientWidth;
		row.toggleAttribute('data-moka-more-start', overflow && list.scrollLeft > 0.5);
		row.toggleAttribute('data-moka-more-end', overflow && list.scrollLeft < end - 0.5);
	};

	const reveal = (tab) => tab?.scrollIntoView({ block: 'nearest', inline: 'nearest', behavior: behavior() });

	const onClick = (e) => {
		const arrow = e.target instanceof Element ? e.target.closest('[data-moka-diag-scroll]') : null;
		if (arrow) {
			const step = Math.max(list.clientWidth * 0.8, 40);
			list.scrollBy({ left: Number(arrow.dataset.mokaDiagScroll) * step, behavior: behavior() });
			return;
		}

		reveal(e.target instanceof Element ? e.target.closest('.moka-diag-tab') : null);
	};

	// A new size can leave the tab showing under an arrow or past the end, so it's brought back into view.
	const onResize = () => {
		update();
		list.querySelector('[aria-selected="true"]')?.scrollIntoView({ block: 'nearest', inline: 'nearest' });
		update();
	};

	// The browser's own scroll on focus would jump; this one follows the reduced-motion setting like the arrows.
	const stopKeys = listenForTabKeys(list, (tab) => {
		tab.focus({ preventScroll: true });
		reveal(tab);
	});

	const resizes = new ResizeObserver(onResize);
	resizes.observe(row);
	resizes.observe(list);
	for (const tab of list.children) {
		resizes.observe(tab);
	}

	list.addEventListener('scroll', update, { passive: true });
	row.addEventListener('click', onClick);

	onResize();

	return {
		dispose: () => {
			resizes.disconnect();
			stopKeys();
			list.removeEventListener('scroll', update);
			row.removeEventListener('click', onClick);
		},
	};
}

/**
 * Opens the diagnostics page in a new browser window.
 * @param {string | null} sessionKey - Key the page uses to find this window's diagnostics data.
 */
export function openDiagnosticsPage(sessionKey) {
	// Relative to <base href>, so an app hosted under a sub-path opens its own page.
	const url = new URL('moka-diagnostics', document.baseURI);
	if (sessionKey) {
		url.searchParams.set('session', sessionKey);
	}

	window.open(url.href, 'moka-diagnostics', 'width=800,height=600');
}

/**
 * Saves streamed JSON as a file.
 * @param {object} content - DotNetStreamReference of the UTF-8 JSON.
 * @param {string} filename - The download filename.
 */
export async function downloadJson(content, filename) {
	const blob = new Blob([await content.arrayBuffer()], { type: 'application/json' });
	const url = URL.createObjectURL(blob);
	const a = document.createElement('a');
	a.href = url;
	a.download = filename;
	document.body.appendChild(a);
	a.click();
	a.remove();
	URL.revokeObjectURL(url);
}
