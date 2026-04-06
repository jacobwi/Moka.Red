/**
 * Moka.Red.Navigation — Tabs JavaScript interop module.
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
     * Constrains a context menu position to remain within the viewport.
     * @param {number} x - Desired X coordinate
     * @param {number} y - Desired Y coordinate
     * @param {number} menuWidth - Width of the context menu element
     * @param {number} menuHeight - Height of the context menu element
     * @returns {{ x: number, y: number }} Adjusted coordinates
     */
    constrainContextMenu: function (x, y, menuWidth, menuHeight) {
        const vw = window.innerWidth;
        const vh = window.innerHeight;
        return {
            x: Math.min(x, vw - menuWidth - 8),
            y: Math.min(y, vh - menuHeight - 8)
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
