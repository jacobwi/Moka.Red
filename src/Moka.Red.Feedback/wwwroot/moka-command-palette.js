// Browsers cache an ES module per URL, so every MokaCommandPalette on the page shares
// this file. Shortcut state is therefore keyed per registration: a second palette must
// not clobber the first one's listener, and disposing one must not unhook the others.
const _shortcuts = new Map();
let _nextHandle = 0;

const _isMac = /mac|iphone|ipad|ipod/i.test(navigator.userAgentData?.platform ?? navigator.platform ?? '');

const MODIFIERS = new Set(['mod', 'ctrl', 'control', 'meta', 'cmd', 'command', 'alt', 'option', 'shift']);

// "Mod+K", "Ctrl+Shift+P", "Alt+Space": modifiers, then one key. Mod is Ctrl, or Cmd on a Mac.
// Null or empty turns the shortcut off, and so does a modifier this does not know: ignoring it
// would turn "Win+K" into a bare K.
function parseShortcut(text) {
    if (typeof text !== 'string') return null;

    const parts = text.split('+').map(p => p.trim().toLowerCase()).filter(Boolean);
    if (parts.length === 0) return null;

    const key = parts.pop();
    const unknown = parts.find(p => !MODIFIERS.has(p));
    if (unknown !== undefined) {
        console.warn(`[MokaCommandPalette] Unknown modifier "${unknown}" in shortcut "${text}"; the shortcut is off.`);
        return null;
    }

    const mods = new Set(parts);
    const mod = mods.has('mod');
    const spec = {
        key: key === 'space' ? ' ' : key,
        ctrl: mods.has('ctrl') || mods.has('control') || (mod && !_isMac),
        meta: mods.has('meta') || mods.has('cmd') || mods.has('command') || (mod && _isMac),
        alt: mods.has('alt') || mods.has('option'),
        shift: mods.has('shift')
    };

    // Without Ctrl, Cmd or Alt a one-character key is typing ("/", "Shift+K").
    spec.types = !spec.ctrl && !spec.meta && !spec.alt && spec.key.length === 1;
    return spec;
}

// Fields that take typing. A shortcut that types a character is left to them.
function isEditable(target) {
    if (!(target instanceof Element)) return false;
    if (target.isContentEditable) return true;

    const tag = target.tagName;
    if (tag === 'TEXTAREA' || tag === 'SELECT') return true;
    return tag === 'INPUT'
        && !/^(button|checkbox|radio|range|color|file|image|reset|submit)$/i.test(target.type);
}

// Every modifier has to match exactly: Ctrl+Shift+K is not Ctrl+K. Letters and digits are also
// compared by code, because Alt and Shift change what e.key reports for them.
function matches(e, spec) {
    if (e.ctrlKey !== spec.ctrl || e.metaKey !== spec.meta || e.altKey !== spec.alt || e.shiftKey !== spec.shift) {
        return false;
    }

    const key = typeof e.key === 'string' ? e.key.toLowerCase() : '';
    const code = typeof e.code === 'string' ? e.code.toLowerCase() : '';
    return key === spec.key || code === 'key' + spec.key || code === 'digit' + spec.key;
}

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
