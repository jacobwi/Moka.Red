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
 * @param {Array<{selector: ?string, keys: string[], when: ?string, unlessShift: ?boolean, unlessModified: ?boolean, unlessOwned: ?boolean}>} rules
 *   selector: the targets a rule covers, as descendants of root. Null means root itself.
 *   keys: KeyboardEvent.key values to cancel.
 *   when: a selector that has to match something inside root, for rules that depend on state
 *     such as an open list or a highlighted option.
 *   unlessShift: leave the key alone while Shift is held.
 *   unlessModified: leave the key alone while Ctrl, Alt or Meta is held. For a component whose
 *     handler ignores those combinations, so that Alt+Left (back) and the like keep working.
 *   unlessOwned: leave the key alone when something else owns it (see keyOwner): a field, a control that uses the
 *     key, or an open modal. For page-level shortcuts, so Space still presses a button in a dialog.
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
			if (rule.unlessModified && (e.ctrlKey || e.altKey || e.metaKey)) continue;
			if (rule.unlessOwned && keyOwner(e) !== 0) continue;
			if (rule.selector ? !target.matches(rule.selector) : target !== root) continue;
			if (rule.when && !root.querySelector(rule.when)) continue;

			e.preventDefault();
			return;
		}
	});
}


// Elements whose own action Space and Enter trigger.
const ACTIVATES = 'button, a[href], summary, input[type="checkbox"], input[type="radio"], input[type="button"], '
	+ 'input[type="submit"], input[type="reset"], [role="button"], [role="link"], [role="checkbox"], [role="radio"], '
	+ '[role="switch"], [role="menuitem"], [role="menuitemcheckbox"], [role="menuitemradio"], [role="option"], '
	+ '[role="tab"], [role="treeitem"], [role="gridcell"]';

// Widgets that move through their own items with the arrow keys, Home, End and Page Up/Down.
const NAVIGATES = 'input[type="range"], input[type="radio"], select, [role="slider"], [role="spinbutton"], '
	+ '[role="scrollbar"], [role="listbox"], [role="menu"], [role="menubar"], [role="radiogroup"], [role="tablist"], '
	+ '[role="tree"], [role="treegrid"], [role="grid"], [role="combobox"]';

const NAVIGATION_KEYS = new Set(['ArrowUp', 'ArrowDown', 'ArrowLeft', 'ArrowRight', 'Home', 'End', 'PageUp', 'PageDown']);

/**
 * Whether a key press landed in something that takes typing: a text field, a select, a text area or editable
 * content.
 * @param {EventTarget} target - The press's target.
 * @returns {boolean}
 */
export function isEditable(target) {
	if (!(target instanceof Element)) return false;
	if (target.isContentEditable) return true;

	const tag = target.tagName;
	if (tag === 'TEXTAREA' || tag === 'SELECT') return true;
	return tag === 'INPUT'
		&& !/^(button|checkbox|radio|range|color|file|image|reset|submit)$/i.test(target.type);
}

function isModal(element) {
	if (element.getAttribute('aria-modal') === 'true') return true;
	try {
		return element.tagName === 'DIALOG' && element.matches(':modal');
	} catch {
		return false;
	}
}

/**
 * Whether an element sits inside an open modal: a dialog, sheet or drawer that the page behind it can't be used
 * while it shows. A modal is drawn inside the page's markup, so its keys bubble through the page's elements.
 * @param {EventTarget} target - Usually a key press's target.
 * @returns {boolean}
 */
export function inModal(target) {
	for (let node = target instanceof Element ? target : null; node; node = node.parentElement) {
		if (isModal(node)) return true;
	}

	return false;
}

/**
 * Whether a modal is showing anywhere on the page.
 * @returns {boolean}
 */
export function modalIsOpen() {
	for (const element of document.querySelectorAll('[aria-modal="true"], dialog[open]')) {
		if (!isModal(element)) continue;
		if (element.checkVisibility ? element.checkVisibility() : element.getClientRects().length > 0) return true;
	}

	return false;
}

/**
 * Who a key press belongs to before any page-level shortcut: 1 when it was pressed in a field that takes typing,
 * 2 when the focused control uses that key itself (Space and Enter on a button, a link or a check box; the arrows,
 * Home, End and Page Up/Down in a slider, a list, a menu or tabs), 3 when it was pressed inside an open modal, and
 * 0 when it is free for the page. The numbers match MokaKeyOwner in .NET.
 * @param {KeyboardEvent} e - The press.
 * @returns {number}
 */
export function keyOwner(e) {
	const target = e.target;
	if (!(target instanceof Element)) return 0;
	if (isEditable(target)) return 1;

	const plain = !e.ctrlKey && !e.altKey && !e.metaKey;
	if (plain && (e.key === ' ' || e.key === 'Enter') && target.matches(ACTIVATES)) return 2;
	if (plain && NAVIGATION_KEYS.has(e.key) && target.closest(NAVIGATES)) return 2;

	return inModal(target) ? 3 : 0;
}
