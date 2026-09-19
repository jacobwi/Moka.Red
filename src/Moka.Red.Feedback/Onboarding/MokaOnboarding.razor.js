/**
 * MokaOnboarding JS module - measures the element a tour step points at.
 *
 * The selector is passed as data. The component used to build a script string around it for
 * eval, which a content security policy without 'unsafe-eval' blocks and which a quote or a
 * backslash in the selector could break out of.
 */

/**
 * The target's box and the viewport size, or null when nothing matches.
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

	const r = element.getBoundingClientRect();
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
