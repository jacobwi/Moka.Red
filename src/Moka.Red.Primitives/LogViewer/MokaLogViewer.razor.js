/**
 * MokaLogViewer JS module: keeps a log's newest row in view while its list is scrolled to the bottom.
 *
 * The scroll position is read here, so scrolling sends nothing to .NET. Scrolling up stops the following, and scrolling
 * back to the bottom starts it again.
 */

const followed = new WeakMap();

function toBottom(list) {
    list.scrollTop = list.scrollHeight;
}

/**
 * Scrolls a log's list to its newest row and keeps it there while the reader stays at the bottom.
 * @param {HTMLElement} list - The element that scrolls the rows.
 */
export function follow(list) {
    if (!list || followed.has(list)) return;

    // The list's scroll height before the latest change. Whether the reader was at the bottom is read from where they
    // are now against it, rather than from a scroll event that may still be on its way, so a wheel turn just before a
    // row arrives isn't undone.
    const state = { height: 0 };
    state.onScroll = () => {
        state.height = list.scrollHeight;
    };
    // New rows, a filter, an opened row and Virtualize resizing its spacers all change what is inside the list. The
    // gap is MDN's test for an element scrolled all the way down: scrollTop can be fractional on a zoomed page, so it
    // is compared with one pixel rather than with zero.
    state.observer = new MutationObserver(() => {
        if (state.height - list.clientHeight - list.scrollTop < 1) toBottom(list);
        state.height = list.scrollHeight;
    });

    list.addEventListener('scroll', state.onScroll, { passive: true });
    state.observer.observe(list, { childList: true, subtree: true, characterData: true, attributes: true });
    followed.set(list, state);
    toBottom(list);
    state.height = list.scrollHeight;
}

/**
 * Stops following, as when the component's AutoScroll is turned off or the component goes away.
 * @param {HTMLElement} list - The element follow() was given.
 */
export function unfollow(list) {
    const state = list ? followed.get(list) : undefined;
    if (!state) return;

    state.observer.disconnect();
    list.removeEventListener('scroll', state.onScroll);
    followed.delete(list);
}
