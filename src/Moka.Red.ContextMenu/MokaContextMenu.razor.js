/**
 * MokaContextMenu JS module - measurement and viewport clamping for context menus.
 *
 * Menus are position: fixed, so every coordinate here is in viewport space.
 */

const DEFAULT_MARGIN = 8;

/** Elements that had focus when a menu opened, keyed by an opaque token. */
const focusStash = new Map();
let focusTokenSeed = 0;

/**
 * Measures a menu element and the viewport it lives in.
 * @param {HTMLElement} element - The menu root element.
 * @returns {{left:number, top:number, width:number, height:number, viewportWidth:number, viewportHeight:number}|null}
 */
export function measureMenu(element) {
    if (!element) return null;

    const rect = element.getBoundingClientRect();
    return {
        left: rect.left,
        top: rect.top,
        width: rect.width,
        height: rect.height,
        viewportWidth: window.innerWidth,
        viewportHeight: window.innerHeight
    };
}

/**
 * Measures the item a sub-menu hangs off, together with its parent menu's edges.
 * @param {HTMLElement} menuElement - The parent menu root element.
 * @param {number} index - The value of the item's data-moka-ctx-index attribute.
 * @returns {{menuLeft:number, menuRight:number, itemTop:number}|null}
 */
export function measureItemAnchor(menuElement, index) {
    if (!menuElement) return null;

    const menuRect = menuElement.getBoundingClientRect();
    // :scope keeps the lookup on this menu's own rows - an open sub-menu is a DOM child
    // and reuses the same index attribute.
    const item = menuElement.querySelector(`:scope > [data-moka-ctx-index="${index}"]`);
    const itemRect = item ? item.getBoundingClientRect() : null;

    return {
        menuLeft: menuRect.left,
        menuRight: menuRect.right,
        itemTop: itemRect ? itemRect.top : menuRect.top
    };
}

/**
 * Clamps a menu box into the viewport.
 * @param {number} x - Desired left coordinate.
 * @param {number} y - Desired top coordinate.
 * @param {number} width - Measured menu width.
 * @param {number} height - Measured menu height.
 * @param {number} [margin] - Gap kept between the menu and the viewport edge.
 * @returns {{x:number, y:number}}
 */
export function constrainToViewport(x, y, width, height, margin) {
    const gap = typeof margin === "number" ? margin : DEFAULT_MARGIN;
    const maxX = Math.max(gap, window.innerWidth - width - gap);
    const maxY = Math.max(gap, window.innerHeight - height - gap);

    return {
        x: Math.min(Math.max(x, gap), maxX),
        y: Math.min(Math.max(y, gap), maxY)
    };
}

/**
 * Remembers the currently focused element so it can be restored when the menu closes.
 * @returns {string|null} A token for restoreFocus, or null when nothing useful had focus.
 */
export function captureFocus() {
    const active = document.activeElement;
    if (!active || active === document.body || typeof active.focus !== "function") {
        return null;
    }

    const token = `moka-ctx-${++focusTokenSeed}`;
    focusStash.set(token, active);
    return token;
}

/**
 * Restores focus to the element captured under the given token and drops the token.
 * @param {string} token - A token returned by captureFocus.
 */
export function restoreFocus(token) {
    if (!token) return;

    const element = focusStash.get(token);
    focusStash.delete(token);
    if (element && element.isConnected) {
        element.focus();
    }
}
