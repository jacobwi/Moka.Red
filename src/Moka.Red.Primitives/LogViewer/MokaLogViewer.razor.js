/**
 * MokaLogViewer JS module: keeps a log's newest row in view while the reader is at the newest end, and keeps the row
 * the reader is on in place when rows arrive above it.
 *
 * The scroll position is read here, so scrolling sends nothing to .NET. Scrolling away from the newest end stops the
 * following, and scrolling back to it starts it again.
 */

const watched = new WeakMap();

// The gap is MDN's test for an element scrolled all the way down: scrollTop can be fractional on a zoomed page, so it
// is compared with one pixel rather than with zero.
function atBottom(list, height) {
    return height - list.clientHeight - list.scrollTop < 1;
}

function toNewest(list, state) {
    list.scrollTop = state.newestFirst ? 0 : list.scrollHeight;
}

// How tall the rows the viewer says arrived above its first row are. Rows are drawn in place of the ones before them,
// so after the change the first `count` rows are the new ones. Measured from their boxes rather than offsetTop, which
// rounds to whole pixels and would let a reader's row creep with every arrival.
function heightAbove(list, count, rowHeight) {
    if (rowHeight > 0) return count * rowHeight;
    const rows = list.querySelectorAll(':scope > .moka-log-viewer__row');
    return rows.length > count ? rows[count].getBoundingClientRect().top - rows[0].getBoundingClientRect().top : 0;
}

// The viewer writes data-moka-above as "n:count" each time it puts count rows above the first one it had. Read once
// per new value, so a later render, or Virtualize drawing other rows, doesn't move the list again.
function rowsAbove(list, state) {
    const token = list.getAttribute('data-moka-above');
    if (token === state.token) return 0;
    state.token = token;
    const count = token ? Number(token.slice(token.indexOf(':') + 1)) : 0;
    return count > 0 ? heightAbove(list, count, state.rowHeight) : 0;
}

// The browser snaps scrollTop to device pixels, so moving by each arrival from where it landed would let the reader's
// row creep by a fraction of a pixel every time. The exact place is kept, and used while the list is still where it
// was put; once the reader scrolls, where they are counts.
function moveBy(list, state, by) {
    const base = state.placed !== null && Math.abs(list.scrollTop - state.placed) < 1 ? state.exact : list.scrollTop;
    state.exact = base + by;
    list.scrollTop = state.exact;
    state.placed = list.scrollTop;
}

function changed(list, state) {
    const above = rowsAbove(list, state);
    const following = state.follow && (state.newestFirst ? list.scrollTop < 1 : atBottom(list, state.height));
    if (following) {
        toNewest(list, state);
    } else if (above > 0) {
        moveBy(list, state, above);
    }
    state.height = list.scrollHeight;
}

/**
 * Watches a log's list. With follow it starts at the newest row and keeps it in view while the reader stays at that
 * end; either way, rows the viewer puts above the first row keep the reader's rows where they are.
 * @param {HTMLElement} list - The element that scrolls the rows.
 * @param {{follow: boolean, newestFirst: boolean, rowHeight: number}} options - Whether to follow, which end is the
 * newest, and the row height under Virtualize (0 when rows are measured).
 */
export function watch(list, options) {
    if (!list) return;

    const existing = watched.get(list);
    if (existing) {
        const startsFollowing = options.follow && (!existing.follow || existing.newestFirst !== options.newestFirst);
        existing.follow = options.follow;
        existing.newestFirst = options.newestFirst;
        existing.rowHeight = options.rowHeight;
        if (startsFollowing) toNewest(list, existing);
        existing.height = list.scrollHeight;
        return;
    }

    // The list's scroll height before the latest change. Whether the reader was at the bottom is read from where they
    // are now against it, rather than from a scroll event that may still be on its way, so a wheel turn just before a
    // row arrives isn't undone.
    const state = {
        follow: options.follow,
        newestFirst: options.newestFirst,
        rowHeight: options.rowHeight,
        height: 0,
        token: list.getAttribute('data-moka-above'),
        placed: null,
        exact: 0
    };
    state.onScroll = () => {
        state.height = list.scrollHeight;
    };
    // New rows, a filter, an opened row and Virtualize resizing its spacers all change what is inside the list.
    state.observer = new MutationObserver(() => changed(list, state));

    list.addEventListener('scroll', state.onScroll, { passive: true });
    state.observer.observe(list, { childList: true, subtree: true, characterData: true, attributes: true });
    watched.set(list, state);
    if (state.follow) toNewest(list, state);
    state.height = list.scrollHeight;
}

/**
 * Stops watching, as when the component no longer follows or keeps place, or goes away.
 * @param {HTMLElement} list - The element watch() was given.
 */
export function unwatch(list) {
    const state = list ? watched.get(list) : undefined;
    if (!state) return;

    state.observer.disconnect();
    list.removeEventListener('scroll', state.onScroll);
    watched.delete(list);
}
