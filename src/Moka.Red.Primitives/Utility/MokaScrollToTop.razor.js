/**
 * MokaScrollToTop JS module - scroll position detection and smooth scrolling.
 *
 * Every init() gets its own handle and its own listener, so two buttons on one page do not
 * tear each other down. Callers must pass the handle back to dispose().
 */

const handlers = new Map();
let nextHandle = 1;

/**
 * Starts watching the window scroll position for one component instance.
 * @param {object} dotNetRef - .NET object reference for callbacks.
 * @param {number} showAfter - Scroll offset in pixels before the button shows.
 * @returns {number} Handle to pass to dispose().
 */
export function init(dotNetRef, showAfter) {
    const handle = nextHandle++;

    const onScroll = () => {
        dotNetRef.invokeMethodAsync('OnScrollChanged', window.scrollY > showAfter);
    };

    window.addEventListener('scroll', onScroll, { passive: true });
    handlers.set(handle, onScroll);
    onScroll(); // seed the initial state

    return handle;
}

export function scrollToTop(smooth) {
    window.scrollTo({
        top: 0,
        behavior: smooth ? 'smooth' : 'instant'
    });
}

/**
 * Removes the listener registered by the matching init() call.
 * @param {number} handle - The value init() returned.
 */
export function dispose(handle) {
    const onScroll = handlers.get(handle);
    if (!onScroll) return;

    window.removeEventListener('scroll', onScroll);
    handlers.delete(handle);
}
