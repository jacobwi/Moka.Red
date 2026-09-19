/**
 * MokaDropdown JS module - the WAI-ARIA menu button pattern.
 *
 * The menu button is the first focusable element in the consumer's trigger content, so its ARIA
 * state is written here rather than rendered. Opening and closing still go through MokaPopover:
 * a key that opens or closes the menu clicks the trigger area, the same path as the mouse, and
 * Escape reaches the popover's own handler. This module moves focus and keeps it from getting
 * lost.
 */

const ITEM = '[role="menuitem"]';
const FOCUSABLE = 'button, [href], input, select, textarea, [tabindex]:not([tabindex="-1"])';
const boundRoots = new WeakSet();

function triggerAreaOf(root) {
	return root.querySelector('.moka-dropdown__trigger');
}

function triggerOf(root) {
	return triggerAreaOf(root)?.querySelector(FOCUSABLE) ?? null;
}

function menuOf(root) {
	return root.querySelector('.moka-dropdown-menu');
}

// Enabled items of this menu only, so a menu nested in an item keeps its own.
function itemsOf(menu) {
	return Array.from(menu.querySelectorAll(ITEM)).filter(item =>
		item.getAttribute('aria-disabled') !== 'true' && item.closest('.moka-dropdown-menu') === menu);
}

// With no enabled item the menu itself takes focus, so Escape and Tab still work.
function focusEdge(menu, last) {
	const items = itemsOf(menu);
	const item = last ? items[items.length - 1] : items[0];
	(item ?? menu).focus();
}

function toggle(root) {
	triggerAreaOf(root)?.click();
}

// MokaPopover gives its popup role="dialog". Around a menu that only adds an unnamed dialog for
// screen readers to announce, so the popup that holds this menu steps out of the way.
function unwrapPopup(menu) {
	const popup = menu.parentElement;
	if (popup && popup.getAttribute('role') === 'dialog') {
		popup.setAttribute('role', 'none');
	}
}

function syncTrigger(root) {
	const trigger = triggerOf(root);
	if (!trigger) return;

	const menu = menuOf(root);
	trigger.setAttribute('aria-haspopup', 'menu');
	trigger.setAttribute('aria-expanded', menu ? 'true' : 'false');

	if (!menu) {
		trigger.removeAttribute('aria-controls');
		return;
	}

	if (!trigger.id) trigger.id = `${menu.id}-button`;
	trigger.setAttribute('aria-controls', menu.id);
	menu.setAttribute('aria-labelledby', trigger.id);
}

/**
 * Starts the menu button behaviour for one dropdown. Safe to call again.
 * @param {HTMLElement} root - The dropdown's outer element.
 */
export function bindDropdown(root) {
	if (!root || boundRoots.has(root)) return;
	boundRoots.add(root);

	// Where focus goes when the menu opens: its first item, or its last after Up Arrow. A menu
	// opened with the mouse takes focus itself, so no item looks picked; Down Arrow still reaches
	// the first one.
	let openOnLast = false;
	let openedByPointer = false;
	// The item that took the Space keydown. Like a button, it activates on the keyup.
	let spaceTarget = null;
	let wasOpen = !!menuOf(root);

	syncTrigger(root);
	if (wasOpen) unwrapPopup(menuOf(root));

	new MutationObserver(() => {
		const menu = menuOf(root);
		syncTrigger(root);

		if (menu && !wasOpen) {
			unwrapPopup(menu);
			if (openedByPointer) menu.focus();
			else focusEdge(menu, openOnLast);
			openOnLast = openedByPointer = false;
		} else if (!menu && wasOpen) {
			// Focus was in the menu and went with it. The keys and item clicks below move it
			// first; this catches a click outside and a close from code.
			const active = document.activeElement;
			if (!active || active === document.body) triggerOf(root)?.focus();
			openOnLast = openedByPointer = false;
		}

		wasOpen = !!menu;
	}).observe(root, { childList: true, subtree: true });

	root.addEventListener('pointerdown', e => {
		const area = triggerAreaOf(root);
		openedByPointer = !!area && e.target instanceof Node && area.contains(e.target);
	});

	root.addEventListener('keydown', e => {
		if (e.defaultPrevented || e.isComposing || e.altKey || e.ctrlKey || e.metaKey) return;

		const target = e.target;
		const menu = menuOf(root);
		const trigger = triggerOf(root);

		if (target === trigger) {
			// Enter and Space open the menu through the button's own click, which follows.
			openedByPointer = false;

			if (e.key === 'ArrowDown' || e.key === 'ArrowUp') {
				e.preventDefault();
				openOnLast = e.key === 'ArrowUp';
				if (menu) focusEdge(menu, openOnLast);
				else toggle(root);
			} else if (e.key === 'Tab' && menu) {
				toggle(root);
			}
			return;
		}

		if (!menu || !(target instanceof Element) || !menu.contains(target)) return;

		const items = itemsOf(menu);
		const index = items.indexOf(target);

		switch (e.key) {
			case 'ArrowDown':
				e.preventDefault();
				(items[(index + 1) % items.length] ?? menu).focus();
				break;
			case 'ArrowUp':
				e.preventDefault();
				(items[index <= 0 ? items.length - 1 : index - 1] ?? menu).focus();
				break;
			case 'Home':
				e.preventDefault();
				focusEdge(menu, false);
				break;
			case 'End':
				e.preventDefault();
				focusEdge(menu, true);
				break;
			case 'Enter':
				// Cancelled so no keypress reaches the button once focus is back on it.
				e.preventDefault();
				if (index >= 0 && !e.repeat) target.click();
				break;
			case ' ':
				e.preventDefault();
				spaceTarget = index >= 0 ? target : null;
				break;
			case 'Escape':
				// MokaPopover's handler closes the menu.
				trigger?.focus();
				break;
			case 'Tab':
				// Tab moves on from the button, Shift+Tab lands on it.
				if (e.shiftKey) e.preventDefault();
				trigger?.focus();
				toggle(root);
				break;
		}
	});

	root.addEventListener('keyup', e => {
		if (e.key !== ' ') return;

		const target = spaceTarget;
		spaceTarget = null;
		if (target && target === e.target) target.click();
	});

	// An item that closes the menu hands focus back to the button before .NET hears of the click.
	// Focus is then somewhere sensible when the menu disappears, and a dialog the action opens
	// returns focus to the button when it closes.
	root.addEventListener('click', e => {
		const menu = menuOf(root);
		if (!menu || menu.dataset.closeOnSelect !== 'true' || !(e.target instanceof Element)) return;

		const item = e.target.closest(ITEM);
		if (item && menu.contains(item) && item.getAttribute('aria-disabled') !== 'true') {
			triggerOf(root)?.focus();
		}
	});
}
