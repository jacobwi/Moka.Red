/**
 * MokaOnboarding JS module - measures the element a tour step points at, keeps the measurement
 * current while the page scrolls or resizes, and traps focus in the step card.
 *
 * The selector is passed as data. The component used to build a script string around it for
 * eval, which a content security policy without 'unsafe-eval' blocks and which a quote or a
 * backslash in the selector could break out of.
 */

// The component caches a single module (MokaComponentBase), so the dialog focus trap is
// re-exported from here rather than imported separately.
export { trapFocus, releaseFocus } from '../moka-dialog.js';

/**
 * The target's box and the viewport size, or null when nothing matches. A target outside the
 * viewport is scrolled into view first, so the spotlight has something to point at.
 * @param {string} selector - A CSS selector for the step's target.
 */
export function measureTarget(selector) {
	let element = null;
	try {
		element = document.querySelector(selector);
	} catch {
		return null;
	}

	if (!element) return null;

	let r = element.getBoundingClientRect();
	const outside = r.bottom < 0 || r.right < 0 || r.top > window.innerHeight || r.left > window.innerWidth
		|| r.top < 0 || r.bottom > window.innerHeight;
	if (outside) {
		element.scrollIntoView({ block: 'center', inline: 'nearest' });
		r = element.getBoundingClientRect();
	}

	return {
		top: r.top,
		left: r.left,
		width: r.width,
		height: r.height,
		right: r.right,
		bottom: r.bottom,
		viewportWidth: window.innerWidth,
		viewportHeight: window.innerHeight
	};
}

const watchers = new Map();
let nextWatcher = 1;

/**
 * Calls the component's OnViewportChanged, at most once a frame, while the page scrolls or the
 * window resizes, so the spotlight follows its target.
 * @param {DotNetObject} dotNetRef
 * @returns {number} A handle for unwatchViewport.
 */
export function watchViewport(dotNetRef) {
	let queued = false;
	const onChange = () => {
		if (queued) return;
		queued = true;
		requestAnimationFrame(() => {
			queued = false;
			dotNetRef.invokeMethodAsync('OnViewportChanged').catch(() => { });
		});
	};

	// Capture, so a scroll inside any container moves the spotlight too.
	window.addEventListener('scroll', onChange, { capture: true, passive: true });
	window.addEventListener('resize', onChange, { passive: true });

	const handle = nextWatcher++;
	watchers.set(handle, onChange);
	return handle;
}

/**
 * Stops a watch started with watchViewport.
 * @param {number} handle
 */
export function unwatchViewport(handle) {
	const onChange = watchers.get(handle);
	if (!onChange) return;

	window.removeEventListener('scroll', onChange, { capture: true });
	window.removeEventListener('resize', onChange);
	watchers.delete(handle);
}
