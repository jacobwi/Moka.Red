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
 * Lets the panel's tab row scroll when its tabs don't fit. The row gets data-moka-overflow while they don't, which
 * makes room for an arrow at each end, and data-moka-more-start or data-moka-more-end while tabs are out of view on
 * that side, which shows that side's arrow. The room depends only on whether the row overflows, never on where it is
 * scrolled, so revealing a tab can't move it back under an arrow. An arrow scrolls about a row's width, and the
 * pressed tab is kept in view. All of it stays in the browser.
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

	// A new size can leave the pressed tab under an arrow or past the end, so it's brought back into view.
	const onResize = () => {
		update();
		list.querySelector('[aria-pressed="true"]')?.scrollIntoView({ block: 'nearest', inline: 'nearest' });
		update();
	};

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
