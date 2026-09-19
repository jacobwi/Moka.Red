/**
 * MokaVirtualList JS module - keyboard support for a list with OnItemClick, which is a listbox with
 * one tab stop.
 *
 * Focus stays on the list and .NET owns the active item, which it names with
 * aria-activedescendant, since Virtualize reuses item elements as the list scrolls. This module
 * reads the keys, only those pressed on the list itself so content inside an item keeps its own,
 * and scrolls the active item into view.
 */

const NAV_KEYS = new Set(['ArrowDown', 'ArrowUp', 'PageDown', 'PageUp', 'Home', 'End']);
const lists = new WeakMap(); // list -> state

function metrics(list) {
	const itemHeight = parseFloat(list.dataset.itemHeight) || 1;
	const paddingTop = parseFloat(getComputedStyle(list).paddingTop) || 0;
	return {
		itemHeight,
		paddingTop,
		firstVisible: Math.max(0, Math.floor((list.scrollTop - paddingTop) / itemHeight)),
		pageSize: Math.max(1, Math.floor(list.clientHeight / itemHeight) - 1)
	};
}

// Worked out from the fixed item height, because the item may not be rendered yet.
function scrollIndexIntoView(list, index) {
	const { itemHeight, paddingTop } = metrics(list);
	const top = paddingTop + index * itemHeight;
	const bottom = top + itemHeight;

	if (top < list.scrollTop) {
		list.scrollTop = top;
	} else if (bottom > list.scrollTop + list.clientHeight) {
		list.scrollTop = bottom - list.clientHeight;
	}
}

/**
 * Starts keyboard handling for one list. Calling it again only swaps the .NET reference.
 * @param {HTMLElement} list - The element with role="listbox".
 * @param {object} dotNetRef - The list's DotNetObjectReference.
 */
export function bindList(list, dotNetRef) {
	if (!list) return;

	const known = lists.get(list);
	if (known) {
		known.dotNetRef = dotNetRef;
		return;
	}

	const state = { dotNetRef, spaceDown: false };
	lists.set(list, state);

	const move = async key => {
		const { firstVisible, pageSize } = metrics(list);
		let index;
		try {
			index = await state.dotNetRef.invokeMethodAsync('MoveActive', key, firstVisible, pageSize);
		} catch {
			// The component or the circuit is gone.
			return;
		}

		if (typeof index === 'number' && index >= 0) scrollIndexIntoView(list, index);
	};

	const activate = () => state.dotNetRef.invokeMethodAsync('ActivateActive').catch(() => { });

	list.addEventListener('keydown', e => {
		if (e.target !== list || e.defaultPrevented || e.isComposing || e.altKey || e.ctrlKey || e.metaKey) return;

		if (NAV_KEYS.has(e.key)) {
			e.preventDefault();
			move(e.key);
		} else if (e.key === 'Enter') {
			e.preventDefault();
			if (!e.repeat) activate();
		} else if (e.key === ' ') {
			// Like a button, Space activates on the keyup; the keydown would scroll the list.
			e.preventDefault();
			state.spaceDown = true;
		}
	});

	list.addEventListener('keyup', e => {
		if (e.key !== ' ' || !state.spaceDown) return;

		state.spaceDown = false;
		if (e.target === list) activate();
	});

	// Tabbing in makes the first item in view active, so there is something to announce. A click
	// also focuses the list, but it makes the clicked item active itself.
	list.addEventListener('focus', () => {
		if (!list.hasAttribute('aria-activedescendant') && list.matches(':focus-visible')) move('Focus');
	});
}
