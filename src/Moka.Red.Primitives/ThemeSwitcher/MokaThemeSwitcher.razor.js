/**
 * MokaThemeSwitcher JS module - keyboard support following the WAI-ARIA menu button pattern.
 *
 * The open list is a menu of menuitemradio items. Up and Down move focus between them (wrapping),
 * Home and End jump to the ends, Enter or Space pick the focused theme, and Escape closes the menu.
 * Down or Up on the closed button opens it, and Tab away closes it. Opening, closing and picking
 * all go through the button's and the items' own click handlers, so the state stays in .NET.
 */

const ITEM = '[role="menuitemradio"]';
const boundSwitchers = new WeakSet();

function triggerOf(root) {
	return root.querySelector(':scope > .moka-theme-switcher__trigger');
}

function itemsOf(root) {
	return Array.from(root.querySelectorAll(ITEM));
}

function isOpen(root) {
	return triggerOf(root)?.getAttribute('aria-expanded') === 'true';
}

// The button's click toggles the menu in .NET, so clicking it is how the menu closes.
function closeMenu(root, refocus) {
	const trigger = triggerOf(root);
	if (!trigger || !isOpen(root)) return;

	if (refocus) trigger.focus();
	trigger.click();
}

/**
 * Focuses the checked theme, or the first one when none is checked. Called once the menu is open.
 * @param {HTMLElement} root - The switcher's root element.
 */
export function focusCheckedItem(root) {
	if (!root) return;

	const items = itemsOf(root);
	const item = items.find(candidate => candidate.getAttribute('aria-checked') === 'true') ?? items[0];
	item?.focus();
}

/**
 * Starts keyboard handling for one switcher. Safe to call again.
 * @param {HTMLElement} root - The switcher's root element.
 */
export function bindSwitcher(root) {
	if (!root || boundSwitchers.has(root)) return;
	boundSwitchers.add(root);

	// The item that took the Space keydown. Like a button, it is picked on the keyup.
	let spaceTarget = null;

	root.addEventListener('keydown', e => {
		if (e.defaultPrevented || e.isComposing || e.altKey || e.ctrlKey || e.metaKey) return;

		const target = e.target;
		const trigger = triggerOf(root);

		// Enter and Space on the button are the browser's: they click it.
		if (target === trigger) {
			if (e.key === 'ArrowDown' || e.key === 'ArrowUp') {
				e.preventDefault();
				// Opening renders the menu, and .NET then focuses the checked item.
				if (isOpen(root)) focusCheckedItem(root);
				else trigger.click();
			} else if (e.key === 'Escape' && isOpen(root)) {
				e.preventDefault();
				closeMenu(root, true);
			}
			return;
		}

		if (!(target instanceof HTMLElement) || !target.matches(ITEM) || !root.contains(target)) return;

		const items = itemsOf(root);
		const index = items.indexOf(target);

		switch (e.key) {
			case 'ArrowDown':
				items[(index + 1) % items.length].focus();
				break;
			case 'ArrowUp':
				items[(index - 1 + items.length) % items.length].focus();
				break;
			case 'Home':
				items[0].focus();
				break;
			case 'End':
				items[items.length - 1].focus();
				break;
			case 'Enter':
				if (!e.repeat) target.click();
				break;
			case ' ':
				spaceTarget = target;
				break;
			case 'Escape':
				closeMenu(root, true);
				break;
			default:
				return;
		}

		// The arrows, Home, End and Space would scroll the page, and Enter could submit a form.
		e.preventDefault();
	});

	root.addEventListener('keyup', e => {
		if (e.key !== ' ') return;

		const target = spaceTarget;
		spaceTarget = null;
		if (target && target === e.target) target.click();
	});

	// A pick closes the menu and takes the focused item with it, so focus moves to the button
	// first. This listener runs before Blazor's, which sits on the document.
	root.addEventListener('click', e => {
		const item = e.target instanceof Element ? e.target.closest(ITEM) : null;
		if (item && root.contains(item)) triggerOf(root)?.focus();
	});

	// Tab, or a click on another control, moves focus out, and the menu closes behind it. Focus
	// with no new owner (a click on empty page, or on the button in Safari, which does not focus
	// buttons) is ignored: the button's own click decides then.
	root.addEventListener('focusout', e => {
		const next = e.relatedTarget;
		if (!(next instanceof Node) || root.contains(next)) return;
		closeMenu(root, false);
	});
}
