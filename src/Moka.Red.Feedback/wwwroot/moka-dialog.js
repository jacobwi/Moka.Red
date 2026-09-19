// Body scroll locking is reference counted: stacked dialogs (a dialog opening a bottom
// sheet, a queued dialog replacing another) each take a lock, and scrolling is only
// restored once the last of them releases.
let _scrollLockCount = 0;
let _previousOverflow = '';

export function lockBodyScroll() {
    if (_scrollLockCount === 0) {
        _previousOverflow = document.body.style.overflow;
        document.body.style.overflow = 'hidden';
    }
    _scrollLockCount++;
}

export function unlockBodyScroll() {
    if (_scrollLockCount === 0) return;

    _scrollLockCount--;
    if (_scrollLockCount === 0) {
        document.body.style.overflow = _previousOverflow;
        _previousOverflow = '';
    }
}

const FOCUSABLE_SELECTOR = [
    'a[href]',
    'button:not([disabled])',
    'input:not([disabled]):not([type="hidden"])',
    'select:not([disabled])',
    'textarea:not([disabled])',
    '[contenteditable="true"]',
    '[tabindex]:not([tabindex="-1"])'
].join(', ');

// Keyed per trap rather than held in module state: the module is cached per URL, so
// every dialog on the page shares this file and a single shared slot would let one
// dialog's teardown break another's.
const _focusTraps = new Map();
let _nextTrapHandle = 0;

// Released dialogs, mapped to where their focus came from. Closing several dialogs at once (the
// service's CloseAll) releases their traps before Blazor removes their elements, so a dialog must
// not hand focus back into one below it that is closing in the same step.
const _releasedDialogs = new WeakMap();

// The last element that had focus outside every dialog. A trap attached after focus already
// moved into its dialog (a field that focused itself while the script loaded) returns focus here.
let _lastFocusOutsideDialogs = null;
document.addEventListener('focusin', (e) => {
    if (e.target instanceof Element && !e.target.closest('.moka-dialog-wrapper')) {
        _lastFocusOutsideDialogs = e.target;
    }
}, true);

function isVisible(el) {
    return !!(el.offsetWidth || el.offsetHeight || el.getClientRects().length);
}

function focusableWithin(element) {
    return Array.from(element.querySelectorAll(FOCUSABLE_SELECTOR))
        .filter(el => el.getAttribute('aria-hidden') !== 'true' && isVisible(el));
}

export function trapFocus(element) {
    if (!element) return 0;

    const handle = ++_nextTrapHandle;
    _releasedDialogs.delete(element);
    const focusAlreadyInside = element.contains(document.activeElement);
    const previouslyFocused = focusAlreadyInside ? _lastFocusOutsideDialogs : document.activeElement;

    const onKeyDown = (e) => {
        if (e.key !== 'Tab') return;

        const focusable = focusableWithin(element);
        if (focusable.length === 0) {
            e.preventDefault();
            element.focus();
            return;
        }

        const first = focusable[0];
        const last = focusable[focusable.length - 1];
        const active = document.activeElement;
        const outside = !element.contains(active);

        if (e.shiftKey) {
            if (outside || active === first) {
                e.preventDefault();
                last.focus();
            }
        } else if (outside || active === last) {
            e.preventDefault();
            first.focus();
        }
    };

    element.addEventListener('keydown', onKeyDown);
    _focusTraps.set(handle, { element, onKeyDown, previouslyFocused });

    // Leave focus alone if it is already inside. Otherwise start on the field the content asked
    // for, and only then on the first control, which is usually the Close button.
    if (!focusAlreadyInside) {
        const requested = element.querySelector('[autofocus], [data-autofocus]');
        const target = requested && isVisible(requested) ? requested : focusableWithin(element)[0];
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
        const owner = previous.closest?.('[role="dialog"]');
        if (!owner || !_releasedDialogs.has(owner) || owner === trap.element) break;
        previous = _releasedDialogs.get(owner);
    }

    if (previous && typeof previous.focus === 'function' && document.contains(previous)) {
        previous.focus();
    }
}
