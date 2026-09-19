/**
 * MokaPopover JS module.
 *
 * The trigger is the consumer's own markup, so the state a screen reader needs (a popup, open or
 * closed, and which element it is) is written onto its first focusable element here.
 */

const FOCUSABLE = 'button, [href], input, select, textarea, [tabindex]:not([tabindex="-1"])';

/**
 * Writes aria-haspopup, aria-expanded and aria-controls onto the trigger. A component that manages
 * its trigger itself (MokaDropdown, a menu button) marks it with data-moka-owns-trigger and is left
 * alone, so the two never overwrite each other.
 * @param {HTMLElement} area - The popover's trigger wrapper.
 * @param {boolean} open - Whether the popup is open.
 * @param {string} popupId - The popup's id.
 */
export function syncTrigger(area, open, popupId) {
    if (!area || area.querySelector('[data-moka-owns-trigger]')) return;

    const trigger = area.querySelector(FOCUSABLE);
    if (!trigger) return;

    trigger.setAttribute('aria-haspopup', 'dialog');
    trigger.setAttribute('aria-expanded', open ? 'true' : 'false');
    if (open) {
        trigger.setAttribute('aria-controls', popupId);
    } else {
        trigger.removeAttribute('aria-controls');
    }
}
