/**
 * Segmented inputs (OTP, PIN, IP address, MAC address): moves focus between the boxes in the
 * browser.
 *
 * The component also moves focus from .NET, but that waits for a server round trip. On Blazor
 * Server a fast typist's next key would land in the full box, where maxlength drops it. Moving
 * focus here, on the input event itself, keeps every key.
 */

const boundRoots = new WeakSet();

function boxesOf(root) {
    return Array.from(root.querySelectorAll('input')).filter(box => !box.disabled);
}

function acceptsAll(box) {
    const pattern = box.dataset.mokaAccept;
    if (!pattern) return true;
    try {
        return new RegExp(`^(?:${pattern})+$`).test(box.value);
    } catch {
        return true;
    }
}

function focusNext(root, box) {
    const boxes = boxesOf(root);
    const next = boxes[boxes.indexOf(box) + 1];
    if (!next) return false;
    next.focus();
    next.select();
    return true;
}

/**
 * Starts the box-to-box focus behaviour for one segmented input. Safe to call again.
 * @param {HTMLElement} root - The component's root element. It carries data-moka-separator when
 *   the value has a separator (IP, MAC); each box carries data-moka-accept, a regex for one valid
 *   character.
 */
export function bindSegments(root) {
    if (!root || boundRoots.has(root)) return;
    boundRoots.add(root);

    // A full box hands focus on at once. A box holding a character the component will reject
    // keeps focus, so the user can correct it where they typed it.
    root.addEventListener('input', e => {
        const box = e.target;
        if (!(box instanceof HTMLInputElement) || box.maxLength <= 0) return;
        if (box.value.length < box.maxLength || !acceptsAll(box)) return;
        focusNext(root, box);
    });

    // The separator moves to the next box instead of being typed, so it never lands in the box.
    root.addEventListener('keydown', e => {
        const separator = root.dataset.mokaSeparator;
        const box = e.target;
        if (!separator || e.key !== separator || !(box instanceof HTMLInputElement)) return;
        if (e.isComposing || e.ctrlKey || e.altKey || e.metaKey) return;

        e.preventDefault();
        if (box.value.length > 0) focusNext(root, box);
    });
}
