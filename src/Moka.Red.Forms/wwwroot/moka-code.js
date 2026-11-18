/**
 * Code inputs (one-time codes, PINs): one field holds the whole code over a row of boxes.
 *
 * The field works without this script. Once it runs it keeps only the digits as text arrives, so a code pasted
 * with spaces or hyphens fits its boxes before .NET hears it, marks the box at the caret with data-moka-caret
 * (which .NET never renders), and can ask the browser for a code from a text message (WebOTP). Nothing here calls
 * .NET: the field's own input event carries the code.
 */

const fields = new WeakMap();

function boxesOf(input) {
    const row = input.closest('[data-moka-boxes]');
    return row ? row.querySelectorAll(':scope > [data-moka-box]') : [];
}

function lengthOf(input) {
    return Number.parseInt(input.dataset.mokaLength, 10) || 0;
}

// Full width digits, which a Japanese or Chinese keyboard types, count as the digits they show, as in .NET.
function digitsOf(text) {
    return text
        .replace(/[０-９]/g, digit => String.fromCharCode(digit.charCodeAt(0) - 0xFF10 + 48))
        .replace(/[^0-9]+/g, '');
}

// Keeps the digits, up to the code's length, with the caret after the digits it followed.
function tidy(input) {
    const text = input.value;
    const digits = digitsOf(text).slice(0, lengthOf(input));
    if (digits === text) return;

    const caret = input.selectionEnd ?? text.length;
    const before = Math.min(digitsOf(text.slice(0, caret)).length, digits.length);
    input.value = digits;
    input.setSelectionRange(before, before);
}

// The caret's box, or every box a selection covers. The caret after the last digit stays in the last box.
function markCaret(input) {
    const boxes = boxesOf(input);
    const focused = document.activeElement === input;
    const last = boxes.length - 1;
    const start = Math.min(input.selectionStart ?? 0, last);
    const end = Math.max(Math.min((input.selectionEnd ?? 0) - 1, last), start);
    boxes.forEach((box, i) => box.toggleAttribute('data-moka-caret', focused && i >= start && i <= end));
}

/**
 * Starts tidying the field and marking its caret. Safe to call again.
 * @param {HTMLInputElement} input - The field. Its data-moka-length holds the number of digits, and its boxes are
 *   the [data-moka-box] children of the [data-moka-boxes] element around it.
 */
export function bindCode(input) {
    if (!input || fields.has(input)) return;

    const mark = () => markCaret(input);
    const state = {
        sms: null,
        handlers: {
            // Ahead of Blazor's own listener, which waits for the event to bubble to the document.
            input: e => {
                if (!e.isComposing) tidy(input);
                if (state.sms && input.value.length >= lengthOf(input)) state.sms.abort();
                mark();
            },
            compositionend: () => {
                tidy(input);
                mark();
            },
            focus: mark,
            blur: mark,
            select: mark,
            selectionchange: mark,
            keyup: mark,
            pointerup: mark
        }
    };
    fields.set(input, state);
    for (const [name, handler] of Object.entries(state.handlers)) input.addEventListener(name, handler);
    mark();
}

/**
 * Stops everything bindCode and readSmsCode started for the field.
 * @param {HTMLInputElement} input - The field.
 */
export function unbindCode(input) {
    const state = input ? fields.get(input) : null;
    if (!state) return;

    state.sms?.abort();
    for (const [name, handler] of Object.entries(state.handlers)) input.removeEventListener(name, handler);
    boxesOf(input).forEach(box => box.removeAttribute('data-moka-caret'));
    fields.delete(input);
}

/**
 * Asks the browser once for a code from a text message, where the WebOTP API is there (Chrome on Android, over
 * HTTPS), and puts it in the field as if typed. Typing a whole code or turning it off stops the request.
 * @param {HTMLInputElement} input - The field, bound with bindCode.
 * @param {boolean} on - Whether to ask.
 */
export function readSmsCode(input, on) {
    const state = input ? fields.get(input) : null;
    if (!state) return;

    if (!on) {
        state.sms?.abort();
        return;
    }

    if (state.sms || !('OTPCredential' in window)) return;

    const request = new AbortController();
    state.sms = request;
    navigator.credentials.get({ otp: { transport: ['sms'] }, signal: request.signal })
        .then(credential => {
            if (request.signal.aborted || !credential?.code || !input.isConnected) return;
            input.value = credential.code;
            input.dispatchEvent(new Event('input', { bubbles: true }));
        })
        .catch(error => {
            // Stopped by us, or refused by the person: the field still takes a typed code.
            if (error?.name !== 'AbortError' && error?.name !== 'NotAllowedError') throw error;
        })
        .finally(() => {
            if (state.sms === request) state.sms = null;
        });
}
