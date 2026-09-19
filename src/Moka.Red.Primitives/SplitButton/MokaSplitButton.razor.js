/**
 * MokaSplitButton JS module: the WAI-ARIA menu button pattern for the dropdown half.
 *
 * The items are the consumer's markup (usually MokaDropdownItem, role="menuitem"), so focus moves
 * between them here. Opening and closing go through the toggle's own click, the same path as the
 * mouse.
 */

const ITEM = '[role="menuitem"]';
const boundRoots = new WeakSet();

function toggleOf(root) {
    return root.querySelector('.moka-split-btn__toggle');
}

function menuOf(root) {
    return root.querySelector('.moka-split-btn__dropdown');
}

function itemsOf(menu) {
    return Array.from(menu.querySelectorAll(ITEM)).filter(item => item.getAttribute('aria-disabled') !== 'true');
}

// With no enabled item the menu itself takes focus, so Escape still works.
function focusEdge(menu, last) {
    const items = itemsOf(menu);
    const item = last ? items[items.length - 1] : items[0];
    if (item) {
        item.focus();
    } else {
        menu.focus();
    }
}

/**
 * Starts the menu button behaviour for one split button. Safe to call again.
 * @param {HTMLElement} root - The split button's outer element.
 */
export function bindSplitButton(root) {
    if (!root || boundRoots.has(root)) return;
    boundRoots.add(root);

    // Where focus goes when the menu opens from the keyboard. A mouse open leaves focus on the
    // toggle, so no item looks picked.
    let focusOnOpen = null;
    // The item that took the Space keydown. Like a button, it activates on the keyup.
    let spaceTarget = null;
    let wasOpen = !!menuOf(root);

    const close = () => {
        if (menuOf(root)) toggleOf(root)?.click();
    };

    root.addEventListener('keydown', e => {
        if (e.isComposing) return;

        const toggle = toggleOf(root);
        const menu = menuOf(root);

        if (e.target === toggle) {
            if (e.key === 'ArrowDown' || e.key === 'ArrowUp') {
                e.preventDefault();
                const last = e.key === 'ArrowUp';
                if (menu) {
                    focusEdge(menu, last);
                } else {
                    focusOnOpen = last ? 'last' : 'first';
                    toggle.click();
                }
            } else if ((e.key === 'Enter' || e.key === ' ') && !menu) {
                // The button's own click opens it; this only decides where focus lands.
                focusOnOpen = 'first';
            } else if (e.key === 'Escape' && menu) {
                e.preventDefault();
                close();
            }
            return;
        }

        if (!menu || !menu.contains(e.target)) return;

        const items = itemsOf(menu);
        const current = e.target.closest(ITEM);
        const index = items.indexOf(current);

        switch (e.key) {
            case 'ArrowDown':
                e.preventDefault();
                items[(index + 1) % items.length]?.focus();
                break;
            case 'ArrowUp':
                e.preventDefault();
                items[(index - 1 + items.length) % items.length]?.focus();
                break;
            case 'Home':
                e.preventDefault();
                items[0]?.focus();
                break;
            case 'End':
                e.preventDefault();
                items[items.length - 1]?.focus();
                break;
            case 'Escape':
                e.preventDefault();
                toggle?.focus();
                close();
                break;
            case 'Enter':
                if (index >= 0 && !e.repeat) {
                    e.preventDefault();
                    items[index].click();
                }
                break;
            case ' ':
                e.preventDefault();
                spaceTarget = index >= 0 ? items[index] : null;
                break;
        }
    });

    root.addEventListener('keyup', e => {
        if (e.key !== ' ') return;

        const target = spaceTarget;
        spaceTarget = null;
        if (target && target === e.target.closest(ITEM)) target.click();
    });

    // Tab out of the menu, or a click anywhere else, closes it.
    root.addEventListener('focusout', e => {
        if (menuOf(root) && !root.contains(e.relatedTarget)) close();
    });

    new MutationObserver(() => {
        const menu = menuOf(root);

        if (menu && !wasOpen) {
            if (focusOnOpen) focusEdge(menu, focusOnOpen === 'last');
            focusOnOpen = null;
        } else if (!menu && wasOpen) {
            // An item chosen from the keyboard had focus and went with the menu.
            const active = document.activeElement;
            if (!active || active === document.body) toggleOf(root)?.focus();
        }

        wasOpen = !!menu;
    }).observe(root, { childList: true, subtree: true });
}
