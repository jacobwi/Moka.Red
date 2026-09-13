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
    const previouslyFocused = document.activeElement;

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

    const focusable = focusableWithin(element);
    if (focusable.length > 0) {
        focusable[0].focus();
    } else if (typeof element.focus === 'function') {
        element.focus();
    }

    return handle;
}

export function releaseFocus(handle) {
    const trap = _focusTraps.get(handle);
    if (!trap) return;

    trap.element.removeEventListener('keydown', trap.onKeyDown);
    _focusTraps.delete(handle);

    const previous = trap.previouslyFocused;
    if (previous && typeof previous.focus === 'function' && document.contains(previous)) {
        previous.focus();
    }
}
