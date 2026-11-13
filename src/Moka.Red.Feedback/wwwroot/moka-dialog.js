// The body scroll lock lives in Core so the media gallery's lightbox can take it too (Primitives
// cannot reference Feedback). Re-exported rather than copied: the browser keeps one module per URL,
// so dialogs, bottom sheets and the lightbox share one lock count.
export { lockBodyScroll, unlockBodyScroll } from '../Moka.Red.Core/moka-scroll-lock.js';

// Everything that can be in the tab order. A negative tabindex takes any of them out (a roving tab
// stop, a disabled MokaButton link), which tabbableWithin checks.
const FOCUSABLE_SELECTOR = [
    'a[href]',
    'button:not([disabled])',
    'input:not([disabled]):not([type="hidden"])',
    'select:not([disabled])',
    'textarea:not([disabled])',
    'iframe',
    'audio[controls]',
    'video[controls]',
    'summary',
    '[contenteditable]:not([contenteditable="false"])',
    '[tabindex]'
].join(', ');

// Keyed per trap rather than held in module state: the module is cached per URL, so
// every dialog on the page shares this file and a single shared slot would let one
// dialog's teardown break another's. Dialogs, drawers and cheatsheets all trap through
// here, so an element that holds a trap is what counts as a dialog below, whatever its markup.
const _focusTraps = new Map();
let _nextTrapHandle = 0;

// Released dialogs, mapped to where their focus came from. Closing several dialogs at once (the
// service's CloseAll) releases their traps before Blazor removes their elements, so a dialog must
// not hand focus back into one below it that is closing in the same step.
const _releasedDialogs = new WeakMap();

// Recent focus targets, newest last. A trap attached after focus already moved into its dialog (a
// field that focused itself while the script loaded) returns focus to the newest one outside it.
// This used to be one slot that skipped only MokaDialog's wrapper, by class name. It pointed into a
// drawer or cheatsheet that had taken focus early, and out of a dialog that opened another one.
const FOCUS_HISTORY_SIZE = 16;
const _focusHistory = [];
document.addEventListener('focusin', (e) => {
    const target = e.target;
    if (!(target instanceof Element)) return;

    // Removed elements can never take focus back, and keeping them would hold on to their subtree.
    for (let i = _focusHistory.length - 1; i >= 0; i--) {
        if (_focusHistory[i] === target || !_focusHistory[i].isConnected) _focusHistory.splice(i, 1);
    }
    _focusHistory.push(target);
    if (_focusHistory.length > FOCUS_HISTORY_SIZE) _focusHistory.shift();
}, true);

function isVisible(el) {
    return !!(el.offsetWidth || el.offsetHeight || el.getClientRects().length);
}

function tabbableWithin(element) {
    return Array.from(element.querySelectorAll(FOCUSABLE_SELECTOR))
        .filter(el => (!el.hasAttribute('tabindex') || el.tabIndex >= 0)
            && el.getAttribute('aria-hidden') !== 'true'
            && isVisible(el));
}

// Radios that share a name are one tab stop, as the browser treats them.
function radioGroupOf(el, element) {
    if (!(el instanceof HTMLInputElement) || el.type !== 'radio' || !el.name) return null;
    return Array.from(element.querySelectorAll('input[type="radio"]'))
        .filter(radio => radio.name === el.name && radio.form === el.form);
}

// Tab into a radio group lands on its checked radio, when it has one.
function landingFor(target, tabbable, element) {
    const checked = radioGroupOf(target, element)?.find(radio => radio.checked && tabbable.includes(radio));
    return checked || target;
}

// Tab and Shift+Tab go to the next or previous element in the tab order, counted in document order
// from the focused element, and wrap at the ends. The browser's own move was only stopped at the
// first and last element, so from the dialog itself or a tabindex="-1" element in it, Tab walked
// out of the dialog.
function moveFocus(element, backwards) {
    const tabbable = tabbableWithin(element);
    if (tabbable.length === 0) {
        element.focus();
        return;
    }

    // Leave a radio group from its far end, so Tab skips the rest of the group.
    let from = document.activeElement || element;
    const group = element.contains(from) ? radioGroupOf(from, element) : null;
    if (group) from = backwards ? group[0] : group[group.length - 1];

    const count = tabbable.length;
    const side = backwards ? Node.DOCUMENT_POSITION_PRECEDING : Node.DOCUMENT_POSITION_FOLLOWING;
    let start = backwards ? count - 1 : 0;
    for (let step = 0; step < count; step++) {
        const i = backwards ? count - 1 - step : step;
        if (from.compareDocumentPosition(tabbable[i]) & side) {
            start = i;
            break;
        }
    }

    // An element that turns focus down (visibility: hidden, inert, a disabled fieldset) is skipped.
    for (let step = 0; step < count; step++) {
        const target = landingFor(tabbable[(start + (backwards ? count - step : step)) % count], tabbable, element);
        target.focus();
        if (document.activeElement === target) return;
    }
}

// Where focus was before it went into `element`: the newest earlier focus target that is still on
// the page and outside it. An ancestor does not count. For a MokaDialog that is its full-screen
// wrapper, which a click beside the dialog focuses and which goes away with it.
function focusBefore(element) {
    for (let i = _focusHistory.length - 1; i >= 0; i--) {
        const candidate = _focusHistory[i];
        if (candidate.isConnected && !element.contains(candidate) && !candidate.contains(element)) {
            return candidate;
        }
    }
    return null;
}

function holdsTrap(element) {
    for (const trap of _focusTraps.values()) {
        if (trap.element === element) return true;
    }
    return false;
}

// The dialog `node` sits in: the nearest element, itself included, that holds a trap now or held one
// until it was released. A role="dialog" that never trapped focus here (the gallery's lightbox) is
// not one, so it cannot hide the released dialog around it.
function trapOwner(node) {
    for (let el = node; el; el = el.parentElement) {
        if (_releasedDialogs.has(el) || holdsTrap(el)) return el;
    }
    return null;
}

// Marks a control that never takes the first focus, such as a header's close button: a tooltip that shows on keyboard
// focus would cover the dialog's first line the moment it opens. Tab still reaches it.
const FOCUS_LAST = 'data-moka-focus-last';

function canTakeFocus(element) {
    return typeof element.focus === 'function' && element.hasAttribute('tabindex');
}

// Where focus starts. [autofocus] and [data-autofocus] always win. Otherwise, following the WAI-ARIA dialog pattern,
// the first control in each part options.order names, in that order, then the first control anywhere, skipping
// controls marked FOCUS_LAST, then the element itself, so a dialog with nothing but a close button is announced by its
// name. options.initial 'first' is the first control in the tab order whatever it is, 'self' the element itself.
function initialFocus(element, options) {
    const requested = element.querySelector('[autofocus], [data-autofocus]');
    if (requested && isVisible(requested)) return requested;

    const initial = options?.initial;
    if (initial === 'self') return canTakeFocus(element) ? element : null;

    const tabbable = tabbableWithin(element);
    if (initial === 'first') return tabbable.length > 0 ? landingFor(tabbable[0], tabbable, element) : null;

    const willing = tabbable.filter(el => !el.closest(`[${FOCUS_LAST}]`));
    for (const selector of options?.order || []) {
        const part = element.querySelector(selector);
        const first = part ? willing.find(el => part.contains(el)) : null;
        if (first) return landingFor(first, tabbable, element);
    }

    if (willing.length > 0) return landingFor(willing[0], tabbable, element);
    if (canTakeFocus(element)) return element;
    return tabbable.length > 0 ? landingFor(tabbable[0], tabbable, element) : null;
}

// Gives focus back to a control that lost it by turning disabled, once it is enabled again, unless focus has moved
// on from the trapped element meanwhile.
function returnWhenEnabled(trap, control) {
    trap.returning?.disconnect();
    const observer = new MutationObserver(() => {
        if (control.matches(':disabled')) return;

        observer.disconnect();
        trap.returning = null;
        if (control.isConnected && document.activeElement === trap.element) control.focus({ preventScroll: true });
    });
    observer.observe(control, { attributes: true, attributeFilter: ['disabled'] });
    trap.returning = observer;
}

/**
 * Keeps Tab inside an element and moves focus into it, unless focus is already inside.
 * @param {HTMLElement} element - The dialog, sheet or drawer.
 * @param {{initial: ?string, order: ?string[]}} [options] - Where focus starts: initial 'first' for the first control
 *   in the tab order, 'self' for the element itself, else the first control in the parts order names (selectors
 *   inside the element, tried in turn) and never one marked data-moka-focus-last unless nothing else can take it.
 * @returns {number} A handle for releaseFocus.
 */
export function trapFocus(element, options) {
    if (!element) return 0;

    const handle = ++_nextTrapHandle;
    _releasedDialogs.delete(element);
    const focusAlreadyInside = element.contains(document.activeElement);
    const previouslyFocused = focusAlreadyInside ? focusBefore(element) : document.activeElement;

    const onKeyDown = (e) => {
        if (e.key !== 'Tab' || e.ctrlKey || e.altKey || e.metaKey) return;

        // Already used by something inside, such as a dialog open within this one that moved focus.
        if (e.defaultPrevented) return;

        e.preventDefault();
        moveFocus(element, e.shiftKey);
    };

    // A control that turns disabled while it has focus, such as a button showing its loading state, drops focus to the
    // page behind the dialog, and Tab then walks the page. Focus goes to the element instead, and back to the control
    // once it is enabled again, so a failed attempt leaves the person on the button they pressed. The check waits a
    // turn: by then the browser has moved focus, and a window that only lost focus still has it on the control.
    const onFocusOut = (e) => {
        if (e.relatedTarget) return;

        const lost = e.target;
        setTimeout(() => {
            const trap = _focusTraps.get(handle);
            if (!trap || !element.isConnected) return;

            const active = document.activeElement;
            if (active && active !== document.body) return;

            element.focus({ preventScroll: true });
            if (lost instanceof Element && lost.isConnected && lost.matches(':disabled')) returnWhenEnabled(trap, lost);
        }, 0);
    };

    element.addEventListener('keydown', onKeyDown);
    element.addEventListener('focusout', onFocusOut);
    _focusTraps.set(handle, { element, onKeyDown, onFocusOut, previouslyFocused, returning: null });

    // Leave focus alone if it is already inside.
    if (!focusAlreadyInside) {
        const target = initialFocus(element, options);
        if (target) {
            target.focus();
        } else if (typeof element.focus === 'function') {
            element.focus();
        }
    }

    return handle;
}

export function releaseFocus(handle) {
    const trap = _focusTraps.get(handle);
    if (!trap) return;

    trap.element.removeEventListener('keydown', trap.onKeyDown);
    trap.element.removeEventListener('focusout', trap.onFocusOut);
    trap.returning?.disconnect();
    _focusTraps.delete(handle);
    _releasedDialogs.set(trap.element, trap.previouslyFocused);

    // Only hand focus back when it is still in this dialog, or nowhere because the dialog is gone.
    // With dialogs stacked, a lower one closing must not pull focus out of the one on top.
    const active = document.activeElement;
    const focusHere = !active || active === document.body || trap.element.contains(active);
    if (!focusHere) return;

    let previous = trap.previouslyFocused;
    for (let hops = 0; previous && hops < 32; hops++) {
        const owner = trapOwner(previous);
        if (!owner || !_releasedDialogs.has(owner) || owner === trap.element) break;
        previous = _releasedDialogs.get(owner);
    }

    if (previous && typeof previous.focus === 'function' && document.contains(previous)) {
        previous.focus();
    }
}
