/**
 * MokaContextMenuTrigger JS module.
 *
 * A left click from the keyboard carries no pointer position, so the trigger opens its menu
 * under its content instead, and needs to know where that content is.
 */

/**
 * Measures the trigger's content in viewport coordinates.
 * @param {HTMLElement} wrapper - The trigger's wrapper element.
 * @returns {{left:number, top:number, bottom:number}|null} The first box inside the wrapper, or null when nothing in it has one.
 */
export function measureContent(wrapper) {
    const rect = wrapper ? firstBox(wrapper) : null;
    return rect ? { left: rect.left, top: rect.top, bottom: rect.bottom } : null;
}

// The wrapper is display: contents and has no box of its own, so the first element in it that
// has one stands in for it.
function firstBox(parent) {
    for (const child of parent.children) {
        const rect = child.getBoundingClientRect();
        if (rect.width > 0 || rect.height > 0) return rect;

        // Another display: contents element has no box either, but its children can.
        const inner = firstBox(child);
        if (inner) return inner;
    }

    return null;
}
