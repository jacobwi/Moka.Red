// Drives moka-hotkeys.js's one keydown listener with a stand-in document and prints, as JSON, which hotkeys fire for a
// press in a text field and outside one. Run by MokaHotkeyFieldsScriptTests: node hotkey-fields.mjs <path to moka-hotkeys.js>

import { pathToFileURL } from 'node:url';

// Node has a navigator of its own with only a getter, so it's replaced rather than assigned.
Object.defineProperty(globalThis, 'navigator', { value: { platform: 'Win32' }, configurable: true });
globalThis.Element = class {
    constructor(tagName, type) {
        this.tagName = tagName;
        this.type = type;
        this.isContentEditable = false;
    }
};

const listeners = [];
globalThis.document = {
    addEventListener(type, listener) {
        if (type === 'keydown') listeners.push(listener);
    },
    removeEventListener() {},
    querySelectorAll() {
        return [];
    }
};

const hotkeys = await import(pathToFileURL(process.argv[2]).href);

const fired = [];
const handle = hotkeys.bindHotkeys({
    invokeMethodAsync(_, id) {
        fired.push(id);
        return Promise.resolve();
    }
});

// One key per setting, since the listener stops at the first hotkey that matches a press.
hotkeys.setHotkeys(handle, [
    { id: 'modifier-unless-typing', keys: 'Ctrl+Z', inFields: 'unless-typing' },
    { id: 'modifier-never', keys: 'Ctrl+Y', inFields: 'never' },
    { id: 'modifier-always', keys: 'Ctrl+B', inFields: 'always' },
    { id: 'modifier-unset', keys: 'Ctrl+M' },
    { id: 'typing-unless-typing', keys: '/', inFields: 'unless-typing' },
    { id: 'typing-never', keys: 'K', inFields: 'never' },
    { id: 'typing-always', keys: 'J', inFields: 'always' }
]);

const presses = {
    'modifier-unless-typing': { key: 'z', code: 'KeyZ', ctrlKey: true },
    'modifier-never': { key: 'y', code: 'KeyY', ctrlKey: true },
    'modifier-always': { key: 'b', code: 'KeyB', ctrlKey: true },
    'modifier-unset': { key: 'm', code: 'KeyM', ctrlKey: true },
    'typing-unless-typing': { key: '/', code: 'Slash' },
    'typing-never': { key: 'k', code: 'KeyK' },
    'typing-always': { key: 'j', code: 'KeyJ' }
};

const field = new Element('INPUT', 'text');
const area = new Element('TEXTAREA');
const button = new Element('BUTTON', 'submit');

function firesFrom(target, press) {
    fired.length = 0;
    const event = {
        ctrlKey: false,
        metaKey: false,
        altKey: false,
        shiftKey: false,
        repeat: false,
        ...press,
        target,
        preventDefault() {}
    };
    for (const listener of listeners) listener(event);
    return fired.length > 0;
}

const result = {};
for (const [id, press] of Object.entries(presses)) {
    result[id] = {
        inField: firesFrom(field, press),
        inTextArea: firesFrom(area, press),
        onButton: firesFrom(button, press)
    };
}

console.log(JSON.stringify(result));
