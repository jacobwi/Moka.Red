// A list's hover highlight, kept out of .NET. A mouseenter handler per row made every row a round trip and a
// render of the whole list (200 options: 412 edits and 18 KB for one row), when all .NET needs is to know
// where the mouse is when a key goes down, so an arrow key carries on from there.
//
// .NET hears about it just before the key, through the same pipe, so it has the row by the time it handles the
// key. mousemove rather than mouseenter, so an arrow key scrolling the list under a still mouse moves nothing.
//
// Rows carry data-moka-index. The list has data-moka-pointer while the mouse, not the keyboard, is the one
// pointing at a row, which is what its CSS draws the highlight from.

const lists = new Map();

function handleKeyDown() {
	for (const [list, state] of lists) {
		if (!list.isConnected) {
			lists.delete(list);
			continue;
		}

		list.removeAttribute('data-moka-pointer');
		if (state.index >= 0) {
			state.dotNetRef.invokeMethodAsync('OnItemHovered', state.index).catch(() => { });
			state.index = -1;
		}
	}

	if (lists.size === 0) {
		document.removeEventListener('keydown', handleKeyDown, true);
	}
}

/**
 * Tracks the row under the mouse until the list leaves the page.
 * @param {HTMLElement} list - The element around the rows.
 * @param {object} dotNetRef - Receives OnItemHovered(index) before a key press, if the mouse moved to a row.
 */
export function trackHover(list, dotNetRef) {
	if (!list || lists.has(list)) return;

	if (lists.size === 0) {
		document.addEventListener('keydown', handleKeyDown, true);
	}

	const state = { dotNetRef, index: -1 };
	lists.set(list, state);

	list.addEventListener('mousemove', e => {
		const row = e.target.closest?.('[data-moka-index]');
		list.toggleAttribute('data-moka-pointer', !!row);
		state.index = row ? Number(row.dataset.mokaIndex) : -1;
	}, { passive: true });

	list.addEventListener('mouseleave', () => {
		list.removeAttribute('data-moka-pointer');
		state.index = -1;
	}, { passive: true });
}

/**
 * Scrolls a list, and only the list, so the row at index shows, with the heading over it when it is the first row
 * under one. Element.scrollIntoView would also scroll the page under a list that sits partly out of the window.
 * @param {HTMLElement} list - The scrolling element around the rows.
 * @param {number} index - The row's data-moka-index. The first row scrolls the list to its top.
 * @param {string} [headingClass] - The class of a heading drawn just before the first row of a group.
 */
export function revealRow(list, index, headingClass) {
	if (!list) return;

	if (index <= 0) {
		list.scrollTop = 0;
		return;
	}

	const row = list.querySelector(`[data-moka-index="${index}"]`);
	if (!row) return;

	const heading = row.previousElementSibling;
	const first = headingClass && heading && heading.classList.contains(headingClass) ? heading : row;

	const style = getComputedStyle(list);
	const box = list.getBoundingClientRect();
	const viewTop = box.top + list.clientTop + (parseFloat(style.paddingTop) || 0);
	const viewBottom = box.top + list.clientTop + list.clientHeight - (parseFloat(style.paddingBottom) || 0);

	const top = first.getBoundingClientRect().top;
	const bottom = row.getBoundingClientRect().bottom;

	if (top < viewTop) {
		list.scrollTop -= viewTop - top;
	} else if (bottom > viewBottom) {
		list.scrollTop += bottom - viewBottom;
	}
}
