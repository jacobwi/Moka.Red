// Browsers cache an ES module per URL, so every MokaCommandPalette on the page shares
// this file. Shortcut state is therefore keyed per registration: a second palette must
// not clobber the first one's listener, and disposing one must not unhook the others.
const _shortcuts = new Map();
let _nextHandle = 0;

export function registerShortcut(dotNetRef) {
    const handle = ++_nextHandle;

    const listener = (e) => {
        if (!(e.ctrlKey || e.metaKey)) return;
        if (typeof e.key !== 'string' || e.key.toLowerCase() !== 'k') return;

        e.preventDefault();
        dotNetRef.invokeMethodAsync('ToggleFromJs');
    };

    _shortcuts.set(handle, listener);
    document.addEventListener('keydown', listener);

    return handle;
}

export function focusInput(inputElement) {
    if (inputElement) {
        setTimeout(() => inputElement.focus(), 50);
    }
}

export function dispose(handle) {
    const listener = _shortcuts.get(handle);
    if (!listener) return;

    document.removeEventListener('keydown', listener);
    _shortcuts.delete(handle);
}
