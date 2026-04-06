/**
 * MokaReveal JS module — IntersectionObserver interop for scroll-triggered animations.
 */

const observers = new WeakMap();

/**
 * Observes an element for viewport intersection.
 * @param {HTMLElement} element - The element to observe.
 * @param {number} threshold - Visibility threshold (0-1).
 * @param {object} dotNetRef - .NET object reference for callbacks.
 * @param {boolean} once - If true, unobserves after first intersection.
 */
export function observe(element, threshold, dotNetRef, once) {
    if (!element) return;

    // Clean up any existing observer
    unobserve(element);

    const observer = new IntersectionObserver(
        (entries) => {
            for (const entry of entries) {
                const isVisible = entry.isIntersecting;
                dotNetRef.invokeMethodAsync("OnVisibilityChanged", isVisible);

                if (isVisible && once) {
                    observer.unobserve(element);
                    observers.delete(element);
                }
            }
        },
        { threshold: threshold }
    );

    observer.observe(element);
    observers.set(element, observer);
}

/**
 * Stops observing an element.
 * @param {HTMLElement} element - The element to stop observing.
 */
export function unobserve(element) {
    if (!element) return;

    const observer = observers.get(element);
    if (observer) {
        observer.unobserve(element);
        observer.disconnect();
        observers.delete(element);
    }
}
