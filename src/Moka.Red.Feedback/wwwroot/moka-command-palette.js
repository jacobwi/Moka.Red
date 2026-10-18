// Parsing and matching live in Core, where everything that binds a key shares them: the palette and the
// hotkey registry used to carry the same forty lines each, and a fix to one would have missed the other.
import { parseShortcut as parse, isEditable, matches } from '../Moka.Red.Core/moka-hotkeys.js';

function parseShortcut(text) {
    return parse(text, 'MokaCommandPalette');
}

// Browsers cache an ES module per URL, so every MokaCommandPalette on the page shares
// this file. Shortcut state is therefore keyed per registration: a second palette must
// not clobber the first one's listener, and disposing one must not unhook the others.
const _shortcuts = new Map();
let _nextHandle = 0;

export function registerShortcut(dotNetRef, shortcut) {
    const handle = ++_nextHandle;
    const registration = { spec: parseShortcut(shortcut) };

    registration.listener = (e) => {
        const spec = registration.spec;
        if (!spec || !matches(e, spec) || (spec.types && isEditable(e.target))) return;

        // Only a matching press is taken from the page, and holding it down toggles once.
        e.preventDefault();
        if (!e.repeat) {
            dotNetRef.invokeMethodAsync('ToggleFromJs');
        }
    };

    _shortcuts.set(handle, registration);
    document.addEventListener('keydown', registration.listener);

    return handle;
}

export function updateShortcut(handle, shortcut) {
    const registration = _shortcuts.get(handle);
    if (registration) {
        registration.spec = parseShortcut(shortcut);
    }
}

export function focusInput(inputElement) {
    if (inputElement) {
        setTimeout(() => inputElement.focus(), 50);
    }
}

export function dispose(handle) {
    const registration = _shortcuts.get(handle);
    if (!registration) return;

    document.removeEventListener('keydown', registration.listener);
    _shortcuts.delete(handle);
}
