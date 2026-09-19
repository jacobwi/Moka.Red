/**
 * MokaScrollToTop JS module - watches a scroll position and scrolls back to the top.
 *
 * The position is the window's, or that of the element a CSS selector names, for layouts where
 * the page content scrolls inside a panel. Every init() gets its own handle and listener, so two
 * buttons on one page do not tear each other down. Callers pass the handle back to scrollToTop()
 * and dispose().
 */

const watchers = new Map();
let nextHandle = 1;

/**
 * Starts watching a scroll position for one component instance.
 * @param {object} dotNetRef - .NET object reference for callbacks.
 * @param {number} showAfter - Scroll offset in pixels past which the button shows.
 * @param {string|null} selector - CSS selector of the element that scrolls, or null for the window.
 * @returns {number} Handle to pass to scrollToTop() and dispose().
 */
export function init(dotNetRef, showAfter, selector) {
    const handle = nextHandle++;
    const watcher = {
        dotNetRef,
        showAfter,
        selector: selector || null,
        element: null,
        visible: null,
        onScroll: null
    };
    watchers.set(handle, watcher);

    if (!watcher.selector) {
        watcher.onScroll = () => report(watcher, window.scrollY);
        window.addEventListener('scroll', watcher.onScroll, { passive: true });
        report(watcher, window.scrollY);
        return handle;
    }

    if (!isValidSelector(watcher.selector)) {
        console.warn(`MokaScrollToTop: "${watcher.selector}" is not a valid CSS selector, so the button never shows.`);
        report(watcher, Number.NEGATIVE_INFINITY);
        return handle;
    }

    // Scroll events do not bubble, but a capturing listener on the document still receives them.
    // Matching the target on each event keeps working after a re-render replaces the element.
    watcher.onScroll = event => {
        const target = event.target;
        if (target instanceof Element && target.matches(watcher.selector)) {
            watcher.element = target;
            report(watcher, target.scrollTop);
        }
    };
    document.addEventListener('scroll', watcher.onScroll, { capture: true, passive: true });

    // Without the element there is nothing to scroll, so the button stays hidden until it scrolls.
    const element = document.querySelector(watcher.selector);
    watcher.element = element;
    report(watcher, element ? element.scrollTop : Number.NEGATIVE_INFINITY);
    return handle;
}

/**
 * Scrolls the watched element, or the window, back to the top.
 * @param {number} handle - The value init() returned.
 * @param {boolean} smooth - Whether to animate the scroll.
 */
export function scrollToTop(handle, smooth) {
    const watcher = watchers.get(handle);
    const target = watcher ? targetOf(watcher) : window;
    target?.scrollTo({ top: 0, behavior: smooth ? 'smooth' : 'instant' });
}

/**
 * Removes the listener registered by the matching init() call.
 * @param {number} handle - The value init() returned.
 */
export function dispose(handle) {
    const watcher = watchers.get(handle);
    if (!watcher) return;

    if (watcher.onScroll) {
        if (watcher.selector) {
            document.removeEventListener('scroll', watcher.onScroll, { capture: true });
        } else {
            window.removeEventListener('scroll', watcher.onScroll);
        }
    }

    watchers.delete(handle);
}

// Calls .NET only when the button should appear or disappear, not on every scroll event, which
// on Blazor Server would be a message per frame while scrolling.
function report(watcher, offset) {
    const visible = offset > watcher.showAfter;
    if (visible === watcher.visible) return;

    watcher.visible = visible;
    watcher.dotNetRef.invokeMethodAsync('OnScrollChanged', visible);
}

function targetOf(watcher) {
    if (!watcher.selector) return window;
    if (watcher.element?.isConnected) return watcher.element;
    return isValidSelector(watcher.selector) ? document.querySelector(watcher.selector) : null;
}

function isValidSelector(selector) {
    try {
        document.createDocumentFragment().querySelector(selector);
        return true;
    } catch {
        return false;
    }
}
