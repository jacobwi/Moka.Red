/**
 * Moka.Red.Navigation - Tabs JavaScript interop module.
 * Provides browser storage, keyboard support for the tab strip and its context menu, and context
 * menu positioning.
 */

const boundStrips = new WeakSet();
const boundMenus = new WeakSet();

const TAB = '[role="tab"]';

function tabsOf(strip) {
    return Array.from(strip.querySelectorAll(TAB));
}

// Exactly one tab is in the tab order at a time (a roving tabindex).
function setTabStop(strip, tab) {
    for (const other of tabsOf(strip)) {
        other.tabIndex = other === tab ? 0 : -1;
    }
}

async function closeFocusedTab(strip, tab, dotNetRef) {
    const index = tabsOf(strip).indexOf(tab);

    try {
        await dotNetRef.invokeMethodAsync("CloseTabFromKeyboard", tab.getAttribute("data-tab-id"));
    } catch {
        // The circuit went away.
        return;
    }

    // The tab stays when it could not be closed. When it went, focus went with it, so it moves to
    // the selected tab, or else to the tab that took the closed one's place.
    const active = document.activeElement;
    if (tab.isConnected || (active && active !== document.body)) {
        return;
    }

    const tabs = tabsOf(strip);
    const next = strip.querySelector(`${TAB}[aria-selected="true"]`) ?? tabs[Math.min(index, tabs.length - 1)];
    next?.focus();
}

export const MokaTabs = {

    /**
     * Keyboard support for a tab strip, following the WAI-ARIA tabs pattern with manual activation.
     * The arrow keys, Home and End move focus between tabs, Enter or Space activate the focused tab,
     * and Delete closes it. Keys pressed on content nested inside a tab are left alone. A middle
     * click closes a tab, so its mousedown is kept from starting the browser's autoscroll.
     * Safe to call again for the same strip.
     * @param {HTMLElement} strip - The element with role="tablist"
     * @param {object} dotNetRef - The strip's DotNetObjectReference, called for Delete
     */
    bindTabStrip: function (strip, dotNetRef) {
        if (!strip || boundStrips.has(strip)) {
            return;
        }

        boundStrips.add(strip);

        // A click focuses the tab (it has tabindex -1), and that tab becomes the tab stop.
        strip.addEventListener("focusin", e => {
            if (e.target instanceof HTMLElement && e.target.matches(TAB)) {
                setTabStop(strip, e.target);
            }
        });

        // Leaving the strip puts the tab stop back on the selected tab, so Tab returns to it.
        strip.addEventListener("focusout", e => {
            if (e.relatedTarget instanceof Node && strip.contains(e.relatedTarget)) {
                return;
            }

            const selected = strip.querySelector(`${TAB}[aria-selected="true"]`);
            if (selected) {
                setTabStop(strip, selected);
            }
        });

        strip.addEventListener("keydown", e => {
            const tab = e.target;
            if (!(tab instanceof HTMLElement) || !tab.matches(TAB) || e.altKey || e.ctrlKey || e.metaKey) {
                return;
            }

            const tabs = tabsOf(strip);
            const index = tabs.indexOf(tab);
            let next = null;

            switch (e.key) {
                case "ArrowRight":
                    next = tabs[(index + 1) % tabs.length];
                    break;
                case "ArrowLeft":
                    next = tabs[(index - 1 + tabs.length) % tabs.length];
                    break;
                case "Home":
                    next = tabs[0];
                    break;
                case "End":
                    next = tabs[tabs.length - 1];
                    break;
                case "Enter":
                case " ":
                    // Space would otherwise scroll the page.
                    e.preventDefault();
                    if (!e.repeat) {
                        tab.click();
                    }
                    return;
                case "Delete":
                    e.preventDefault();
                    if (!e.repeat && dotNetRef) {
                        closeFocusedTab(strip, tab, dotNetRef);
                    }
                    return;
                default:
                    return;
            }

            e.preventDefault();
            if (next) {
                setTabStop(strip, next);
                next.focus();
            }
        });

        strip.addEventListener("mousedown", e => {
            if (e.button === 1 && e.target instanceof Element && e.target.closest(TAB)) {
                e.preventDefault();
            }
        });
    },

    /**
     * Puts focus back on a tab after the built-in context menu closed, unless focus already moved
     * somewhere else (a dialog the menu item opened, say). Falls back to the selected tab when the
     * menu's own tab is gone.
     * @param {HTMLElement} strip - The element with role="tablist"
     * @param {string} tabId - The tab the menu was opened for
     */
    restoreTabFocus: function (strip, tabId) {
        const active = document.activeElement;
        if (!strip || (active && active !== document.body)) {
            return;
        }

        const tab = strip.querySelector(`${TAB}[data-tab-id="${CSS.escape(tabId)}"]`)
            ?? strip.querySelector(`${TAB}[aria-selected="true"]`);
        tab?.focus();
    },

    /**
     * Focuses the first enabled item of the built-in tab context menu, and moves between its items
     * with the arrow keys, Home and End.
     * @param {HTMLElement} menu - The element with role="menu"
     */
    bindMenu: function (menu) {
        if (!menu || boundMenus.has(menu)) {
            return;
        }

        boundMenus.add(menu);
        const itemsOf = () => Array.from(menu.querySelectorAll('[role="menuitem"]:not(:disabled)'));

        menu.addEventListener("keydown", e => {
            const items = itemsOf();
            if (items.length === 0) {
                return;
            }

            const index = items.indexOf(document.activeElement);
            let next;

            switch (e.key) {
                case "ArrowDown":
                    next = items[(index + 1) % items.length];
                    break;
                case "ArrowUp":
                    next = items[index <= 0 ? items.length - 1 : index - 1];
                    break;
                case "Home":
                    next = items[0];
                    break;
                case "End":
                    next = items[items.length - 1];
                    break;
                default:
                    return;
            }

            e.preventDefault();
            next.focus();
        });

        itemsOf()[0]?.focus();
    },

    /**
     * Saves a value to the specified storage (sessionStorage or localStorage).
     * @param {string} storageType - "session" or "local"
     * @param {string} key - Storage key
     * @param {string} value - JSON string to store
     */
    saveState: function (storageType, key, value) {
        try {
            const storage = storageType === "local" ? localStorage : sessionStorage;
            storage.setItem(key, value);
        } catch (e) {
            console.warn("[MokaTabs] Failed to save state:", e);
        }
    },

    /**
     * Loads a value from the specified storage.
     * @param {string} storageType - "session" or "local"
     * @param {string} key - Storage key
     * @returns {string|null} The stored JSON string, or null
     */
    loadState: function (storageType, key) {
        try {
            const storage = storageType === "local" ? localStorage : sessionStorage;
            return storage.getItem(key);
        } catch (e) {
            console.warn("[MokaTabs] Failed to load state:", e);
            return null;
        }
    },

    /**
     * Removes a value from the specified storage.
     * @param {string} storageType - "session" or "local"
     * @param {string} key - Storage key
     */
    removeState: function (storageType, key) {
        try {
            const storage = storageType === "local" ? localStorage : sessionStorage;
            storage.removeItem(key);
        } catch (e) {
            console.warn("[MokaTabs] Failed to remove state:", e);
        }
    },

    /**
     * Measures a context menu element and returns a position that keeps it inside the viewport.
     * The element must already be in the DOM (rendered at its requested position).
     * @param {HTMLElement} element - The context menu element
     * @param {number} x - Desired X coordinate
     * @param {number} y - Desired Y coordinate
     * @param {number} [margin] - Gap kept between the menu and the viewport edge (default 8)
     * @returns {{ x: number, y: number }} Adjusted coordinates
     */
    constrainContextMenu: function (element, x, y, margin) {
        if (!element) {
            return { x: x, y: y };
        }

        const gap = typeof margin === "number" ? margin : 8;
        const rect = element.getBoundingClientRect();
        const maxX = Math.max(gap, window.innerWidth - rect.width - gap);
        const maxY = Math.max(gap, window.innerHeight - rect.height - gap);
        return {
            x: Math.min(Math.max(x, gap), maxX),
            y: Math.min(Math.max(y, gap), maxY)
        };
    },

    /**
     * Scrolls the tab strip so that a specific tab header is visible.
     * @param {string} tabId - The data-tab-id attribute value
     * @param {HTMLElement} [strip] - The strip to look in. Without it the first matching header in
     *     the document is used, which is the wrong one when two strips share tab ids.
     */
    scrollTabIntoView: function (tabId, strip) {
        const el = (strip ?? document).querySelector(`.moka-tab-header[data-tab-id="${CSS.escape(tabId)}"]`);
        if (el) {
            el.scrollIntoView({ behavior: "smooth", block: "nearest", inline: "nearest" });
        }
    }
};
