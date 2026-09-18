/**
 * Moka.Red keyboard helpers, shared across packages.
 *
 * Blazor cannot cancel the browser's default action for a single key. `@onkeydown:preventDefault`
 * is all or nothing, so cancelling Space would also cancel Tab and typing. `KeyboardEventArgs`
 * has no target either, so a .NET handler cannot tell a key pressed on an element from one that
 * bubbled out of a button or input inside it. These helpers do those two parts in the browser.
 * The components' own .NET handlers still do the actual work.
 *
 * The listeners live on the bound element and go away with it, so there is nothing to dispose.
 * Binding the same element again replaces its settings.
 */

const activationRoots = new WeakMap(); // root -> selector, or null for the root itself
const keyRules = new WeakMap(); // root -> rules

// The nearest bound ancestor, so each of two nested lists handles only its own rows.
function activationOwner(element) {
	for (let node = element.parentElement; node; node = node.parentElement) {
		if (activationRoots.has(node)) return node;
	}
	return null;
}

function isActivatable(root, element) {
	if (!(element instanceof HTMLElement) || element.tabIndex < 0) return false;
	if (element.getAttribute('aria-disabled') === 'true') return false;

	const selector = activationRoots.get(root);
	if (!selector) return element === root;
	return element.matches(selector) && activationOwner(element) === root;
}

/**
 * Makes Enter and Space click a focused element the way they click a button: Enter on keydown,
 * Space on keyup, with Space's page scroll cancelled. Only the focused element itself counts, so
 * keys pressed in a button or input inside it are left alone. The activation is a click(), so it
 * reaches the component's @onclick exactly like a mouse click.
 * @param {HTMLElement} root - The element to activate, or the container of the elements to activate.
 * @param {string} [selector] - Which descendants of root to activate. Leave it out to activate
 *   root itself. A descendant inside another bound container belongs to that container.
 */
export function bindActivation(root, selector) {
	if (!root) return;

	const alreadyBound = activationRoots.has(root);
	activationRoots.set(root, selector || null);
	if (alreadyBound) return;

	// The element that took the Space keydown. Like a native button, it activates on the keyup.
	let spaceTarget = null;

	root.addEventListener('keydown', e => {
		if (e.defaultPrevented || e.isComposing || e.altKey || e.ctrlKey || e.metaKey) return;
		if (!isActivatable(root, e.target)) return;

		if (e.key === 'Enter') {
			if (!e.repeat) e.target.click();
		} else if (e.key === ' ') {
			e.preventDefault();
			spaceTarget = e.target;
		}
	});

	root.addEventListener('keyup', e => {
		if (e.key !== ' ') return;

		const target = spaceTarget;
		spaceTarget = null;
		if (target && target === e.target) target.click();
	});
}

/**
 * Cancels the browser's default action (page scroll, form submit, a newline) for some keys. The
 * keys are not stopped, so the component's .NET handler still gets them.
 * @param {HTMLElement} root - The component's root element.
 * @param {Array<{selector: ?string, keys: string[], when: ?string, unlessShift: ?boolean}>} rules
 *   selector: the targets a rule covers, as descendants of root. Null means root itself.
 *   keys: KeyboardEvent.key values to cancel.
 *   when: a selector that has to match something inside root, for rules that depend on state
 *     such as an open list or a highlighted option.
 *   unlessShift: leave the key alone while Shift is held.
 */
export function preventKeys(root, rules) {
	if (!root) return;

	const alreadyBound = keyRules.has(root);
	keyRules.set(root, rules || []);
	if (alreadyBound) return;

	root.addEventListener('keydown', e => {
		// Enter also confirms an IME composition, which must go through.
		if (e.isComposing) return;

		const target = e.target;
		if (!(target instanceof Element)) return;

		for (const rule of keyRules.get(root)) {
			if (!rule.keys.includes(e.key)) continue;
			if (rule.unlessShift && e.shiftKey) continue;
			if (rule.selector ? !target.matches(rule.selector) : target !== root) continue;
			if (rule.when && !root.querySelector(rule.when)) continue;

			e.preventDefault();
			return;
		}
	});
}
