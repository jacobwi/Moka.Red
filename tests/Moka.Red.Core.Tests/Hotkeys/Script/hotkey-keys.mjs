// Parses shortcuts and matches presses with moka-hotkeys.js on a given platform, and prints which presses match as JSON.
// Run by MokaHotkeyKeysScriptTests: node hotkey-keys.mjs <path to moka-hotkeys.js> <navigator.platform>

import { pathToFileURL } from 'node:url';

// Node has a navigator of its own with only a getter, so it's replaced rather than assigned.
Object.defineProperty(globalThis, 'navigator', { value: { platform: process.argv[3] }, configurable: true });
globalThis.Element = class {};
const listeners = [];
globalThis.document = {
    addEventListener(type, listener) {
        if (type === 'keydown') listeners.push(listener);
    },
    removeEventListener() {},
    querySelectorAll() { return []; }
};

const { parseShortcut, matches, bindHotkeys, setHotkeys } = await import(pathToFileURL(process.argv[2]).href);

const cases = {
    'question-us': ['?', { key: '?', code: 'Slash', shiftKey: true }],
    'question-fr': ['?', { key: '?', code: 'KeyM', shiftKey: true }],
    'question-slash-pressed': ['?', { key: '/', code: 'Slash' }],
    'slash-us': ['/', { key: '/', code: 'Slash' }],
    'slash-de': ['/', { key: '/', code: 'Digit7', shiftKey: true }],
    'shift-question-with-shift': ['Shift+?', { key: '?', code: 'Slash', shiftKey: true }],
    'shift-question-without-shift': ['Shift+?', { key: '?', code: 'Slash' }],
    'ctrl-question': ['Ctrl+?', { key: '?', code: 'Slash', ctrlKey: true, shiftKey: true }],
    'ctrl-question-without-ctrl': ['Ctrl+?', { key: '?', code: 'Slash', shiftKey: true }],
    'letter-with-shift': ['K', { key: 'K', code: 'KeyK', shiftKey: true }],
    'letter': ['K', { key: 'k', code: 'KeyK' }],
    'mod-with-ctrl': ['Mod+K', { key: 'k', code: 'KeyK', ctrlKey: true }],
    'mod-with-meta': ['Mod+K', { key: 'k', code: 'KeyK', metaKey: true }]
};

const result = {};
for (const [id, [shortcut, press]] of Object.entries(cases)) {
    const event = { ctrlKey: false, metaKey: false, altKey: false, shiftKey: false, ...press };
    result[id] = matches(event, parseShortcut(shortcut));
}

// A held shortcut: the first press runs it, and every press, repeats included, is taken from the browser.
const fired = [];
const handle = bindHotkeys({ invokeMethodAsync(_, id) { fired.push(id); return Promise.resolve(); } });
setHotkeys(handle, [{ id: 'palette', keys: 'Ctrl+K' }]);
function press(repeat) {
    let prevented = false;
    const event = {
        key: 'k', code: 'KeyK', ctrlKey: true, metaKey: false, altKey: false, shiftKey: false, repeat,
        target: new Element(), preventDefault() { prevented = true; }
    };
    for (const listener of listeners) listener(event);
    return prevented;
}
result.held = { firstPrevented: press(false), repeatPrevented: press(true), repeatPrevented2: press(true), runs: fired.length };

console.log(JSON.stringify(result));
