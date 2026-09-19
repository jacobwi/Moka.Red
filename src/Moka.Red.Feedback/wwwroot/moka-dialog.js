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

export function trapFocus(element) {
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

    element.addEventListener('keydown', onKeyDown);
    _focusTraps.set(handle, { element, onKeyDown, previouslyFocused });

    // Leave focus alone if it is already inside. Otherwise start on the field the content asked
    // for, and only then on the first control, which is usually the Close button.
    if (!focusAlreadyInside) {
        const requested = element.querySelector('[autofocus], [data-autofocus]');
        const tabbable = tabbableWithin(element);
        const target = requested && isVisible(requested)
            ? requested
            : tabbable.length > 0 ? landingFor(tabbable[0], tabbable, element) : null;
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
