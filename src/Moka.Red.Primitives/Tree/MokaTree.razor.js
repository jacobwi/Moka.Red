/**
 * MokaTree JS module - keyboard support following the WAI-ARIA tree pattern.
 *
 * One item is in the tab order at a time (a roving tabindex). Up and Down move between the items
 * on screen, Home and End jump to the ends, Right and Left expand, collapse and move between parent
 * and child, Enter or Space act like a click on the row, and the context-menu key reaches the row's
 * handler. Expanding and collapsing still go through the row's own toggle button, so the
 * component's state stays in .NET.
 */

const ITEM = '[role="treeitem"]';
const boundTrees = new WeakSet();

// Collapsed children are not rendered, so every item of this tree in the DOM is on screen. Items
// of a tree nested inside an item belong to that tree.
function itemsOf(tree) {
	return Array.from(tree.querySelectorAll(ITEM)).filter(item => item.closest('[role="tree"]') === tree);
}

function rowOf(item) {
	return item.querySelector(':scope > .moka-tree-item__row');
}

function toggleOf(item) {
	return item.querySelector(':scope > .moka-tree-item__row > .moka-tree-item__toggle');
}

function setTabStop(tree, item) {
	for (const other of itemsOf(tree)) {
		if (other !== item && other.tabIndex === 0) other.tabIndex = -1;
	}

	if (item) item.tabIndex = 0;
}

// Keeps exactly one tab stop as items come and go: the selected item, or else the first.
function ensureTabStop(tree) {
	const items = itemsOf(tree);
	if (items.length === 0 || items.some(item => item.tabIndex === 0)) return;

	setTabStop(tree, items.find(item => item.getAttribute('aria-selected') === 'true') ?? items[0]);
}

function focusItem(tree, item) {
	if (!item) return;

	setTabStop(tree, item);
	item.focus();
}

/**
 * Starts keyboard handling for one tree. Safe to call again.
 * @param {HTMLElement} tree - The element with role="tree".
 */
export function bindTree(tree) {
	if (!tree || boundTrees.has(tree)) return;
	boundTrees.add(tree);

	ensureTabStop(tree);
	new MutationObserver(() => ensureTabStop(tree)).observe(tree, { childList: true, subtree: true });

	// A click focuses the item (it has tabindex -1), and that item becomes the tab stop. A click on
	// the expand toggle focuses the button instead in some browsers, where the arrow keys would stop
	// working, so focus moves on to the item.
	tree.addEventListener('focusin', e => {
		const target = e.target;
		if (!(target instanceof HTMLElement)) return;

		if (target.matches('.moka-tree-item__toggle')) {
			const item = target.closest(ITEM);
			if (item && itemsOf(tree).includes(item)) item.focus({ preventScroll: true });
			return;
		}

		if (target.matches(ITEM) && itemsOf(tree).includes(target)) {
			setTabStop(tree, target);
		}
	});

	// The context-menu key and Shift+F10 fire contextmenu on the focused item, but MokaTreeItem
	// listens on its row, inside the item, which the event never reaches. The row gets its own
	// copy, and the original stops here so nothing above the tree sees the press twice.
	tree.addEventListener('contextmenu', e => {
		const item = e.target;
		if (!(item instanceof HTMLElement) || !item.matches(ITEM) || !itemsOf(tree).includes(item)) return;

		const row = rowOf(item);
		if (!row) return;

		const box = row.getBoundingClientRect();
		const forwarded = new MouseEvent('contextmenu', {
			bubbles: true,
			cancelable: true,
			button: 2,
			clientX: box.left + 8,
			clientY: box.bottom
		});

		e.stopPropagation();
		if (!row.dispatchEvent(forwarded)) e.preventDefault();
	});

	tree.addEventListener('keydown', e => {
		const item = e.target;
		if (!(item instanceof HTMLElement) || !item.matches(ITEM) || e.altKey || e.ctrlKey || e.metaKey) return;

		const items = itemsOf(tree);
		const index = items.indexOf(item);
		if (index < 0) return;

		const expanded = item.getAttribute('aria-expanded');

		switch (e.key) {
			case 'ArrowDown':
				focusItem(tree, items[index + 1]);
				break;
			case 'ArrowUp':
				focusItem(tree, items[index - 1]);
				break;
			case 'Home':
				focusItem(tree, items[0]);
				break;
			case 'End':
				focusItem(tree, items[items.length - 1]);
				break;
			case 'ArrowRight':
				if (expanded === 'false') {
					toggleOf(item)?.click();
				} else if (expanded === 'true') {
					// An expanded item's first child comes straight after it.
					focusItem(tree, items[index + 1]);
				}
				break;
			case 'ArrowLeft':
				if (expanded === 'true') {
					toggleOf(item)?.click();
				} else {
					focusItem(tree, item.parentElement?.closest(ITEM));
				}
				break;
			case 'Enter':
			case ' ':
				rowOf(item)?.click();
				break;
			default:
				return;
		}

		// These keys would otherwise scroll the page.
		e.preventDefault();
	});
}
