/**
 * MokaReveal JS module: tells .NET when an element comes into view and leaves it, and nothing in between.
 *
 * Every element watched with the same threshold and margin shares one IntersectionObserver, so a page of thumbnails
 * costs the browser one observer rather than one each.
 */

const groups = new Map();
const watched = new WeakMap();

function groupFor(threshold, rootMargin) {
    const key = `${threshold}|${rootMargin}`;
    let group = groups.get(key);
    if (!group) {
        group = { key, count: 0, observer: null };
        group.observer = new IntersectionObserver(entries => {
            for (const entry of entries) {
                changed(entry.target, entry.isIntersecting);
            }
        }, { threshold, rootMargin });
        groups.set(key, group);
    }
    return group;
}

function changed(element, inView) {
    const state = watched.get(element);
    if (!state) return;

    state.inView = inView;
    clearTimeout(state.timer);
    if (state.debounce > 0) {
        // A crossing counts only once it has held for the debounce, so content a scroll carries past says nothing.
        state.timer = setTimeout(() => settle(element, state), state.debounce);
    } else {
        settle(element, state);
    }
}

function settle(element, state) {
    state.timer = 0;
    if (watched.get(element) !== state || state.inView === state.reported) return;

    state.reported = state.inView;
    if (state.inView || state.leaves) {
        state.dotNetRef.invokeMethodAsync('OnVisibilityChanged', state.inView);
    }
    if (state.inView && state.once) {
        unobserve(element);
    }
}

/**
 * Watches an element. Watching it again with other options carries over whether it was last reported in view.
 * @param {HTMLElement} element - The element to watch.
 * @param {object} dotNetRef - The component, called with OnVisibilityChanged(bool).
 * @param {{threshold: number, rootMargin: string, once: boolean, debounce: number, leaves: boolean}} options
 *   once stops watching after the first time in view, debounce is in milliseconds, and leaves says whether .NET hears
 *   a leave or only the next time in view.
 */
export function observe(element, dotNetRef, options) {
    if (!element) return;

    const previous = watched.get(element);
    unobserve(element);

    const group = groupFor(options.threshold, options.rootMargin);
    watched.set(element, {
        group,
        dotNetRef,
        once: options.once,
        debounce: options.debounce,
        leaves: options.leaves,
        reported: previous?.reported ?? false,
        inView: previous?.inView ?? false,
        timer: 0
    });
    group.count++;
    group.observer.observe(element);
}

/**
 * Stops watching an element.
 * @param {HTMLElement} element - The element to stop watching.
 */
export function unobserve(element) {
    if (!element) return;

    const state = watched.get(element);
    if (!state) return;

    clearTimeout(state.timer);
    watched.delete(element);
    state.group.observer.unobserve(element);
    if (--state.group.count === 0) {
        state.group.observer.disconnect();
        groups.delete(state.group.key);
    }
}
