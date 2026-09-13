/**
 * Moka.Red.Navigation - Tabs JavaScript interop module.
 * Provides browser storage, drag-and-drop helpers, and context menu positioning.
 */
export const MokaTabs = {

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
     */
    scrollTabIntoView: function (tabId) {
        const el = document.querySelector(`[data-tab-id="${tabId}"]`);
        if (el) {
            el.scrollIntoView({ behavior: "smooth", block: "nearest", inline: "nearest" });
        }
    }
};
