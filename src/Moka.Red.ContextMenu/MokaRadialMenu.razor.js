/**
 * MokaRadialMenu JS module - the pointer's side of the ring.
 *
 * The highlight follows the pointer's angle around the hub, the list of a button with choices shows while it is
 * pointed at, and a held button let go over a button picks it (a flick). None of that goes to .NET: the ring is told
 * where the pointer was just before a key (OnPointed), and what a flick picked (OnFlicked).
 *
 * The ring element marks the pointer's state with data attributes Blazor never renders, so a render never undoes
 * them: data-moka-pointer on the ring while the pointer leads, data-moka-pointed on the slot and choice it points at.
 */

// Shared with other packages; the relative import resolves against this module's own URL.
export { preventKeys } from '../Moka.Red.Core/moka-keys.js';
export { captureFocus, restoreFocus } from './MokaContextMenu.razor.js';

const VIEWPORT_MARGIN = 8;
const FLICK_DISTANCE = 14;

const rings = new Map();

/**
 * The centre of the focused element, for a ring the keyboard opened. Called before the ring takes focus.
 * @returns {{x:number, y:number}|null} Null when nothing with a box has focus.
 */
export function measureFocusCenter() {
    const active = document.activeElement;
    if (!active || active === document.body || active === document.documentElement) return null;

    const rect = active.getBoundingClientRect();
    if (rect.width === 0 && rect.height === 0) return null;

    return { x: rect.left + rect.width / 2, y: rect.top + rect.height / 2 };
}

/**
 * Starts following the pointer for an open ring. Binding the same ring again replaces its state.
 * @param {HTMLElement} ring - The ring element (role="menu").
 * @param {object} dotNet - Receives OnPointed(index, choice) and OnFlicked(index, choice).
 * @param {boolean} flick - Whether a button was still held when the ring opened.
 */
export function bind(ring, dotNet, flick) {
    if (!ring) return;
    unbind(ring);

    const state = { dotNet, flick: !!flick, moved: !flick, leads: false, drawn: false, slot: -1, choice: -1 };

    state.move = (e) => {
        if (!ring.isConnected) { unbind(ring); return; }
        follow(ring, state, e.clientX, e.clientY);
    };

    state.up = () => {
        if (!ring.isConnected) { unbind(ring); return; }
        if (state.flick && state.moved && state.slot >= 0) {
            dotNet.invokeMethodAsync('OnFlicked', state.slot, state.choice).catch(() => { });
        }
        state.flick = false;
    };

    // A finger that opened the ring with a long press would otherwise pan the page as it moves toward a button,
    // and the browser would take the pointer away.
    state.touch = (e) => {
        if (state.flick) e.preventDefault();
    };

    state.cancel = () => {
        state.flick = false;
    };

    // Capture, so .NET hears the pointer's position before the key itself reaches it.
    state.key = () => {
        if (!state.leads) return;
        dotNet.invokeMethodAsync('OnPointed', state.slot, state.choice).catch(() => { });
        state.leads = false;
        point(ring, state, -1, -1);
        state.drawn = false;
        ring.removeAttribute('data-moka-pointer');
    };

    document.addEventListener('pointermove', state.move, { capture: true, passive: true });
    document.addEventListener('pointerup', state.up, true);
    document.addEventListener('pointercancel', state.cancel, true);
    document.addEventListener('touchmove', state.touch, { capture: true, passive: false });
    ring.addEventListener('keydown', state.key, true);
    rings.set(ring, state);
}

/**
 * Stops following the pointer. The ring also does this itself once it has left the page.
 * @param {HTMLElement} ring - The ring element.
 */
export function unbind(ring) {
    const state = rings.get(ring);
    if (!state) return;

    document.removeEventListener('pointermove', state.move, { capture: true });
    document.removeEventListener('pointerup', state.up, true);
    document.removeEventListener('pointercancel', state.cancel, true);
    document.removeEventListener('touchmove', state.touch, { capture: true });
    ring.removeEventListener('keydown', state.key, true);
    rings.delete(ring);
}

/**
 * Keeps the choices of a button inside the viewport, for a list the keyboard opened.
 * @param {HTMLElement} ring - The ring element.
 * @param {number} index - The button's position.
 */
export function fitChoices(ring, index) {
    const slot = ring ? slotsOf(ring)[index] : null;
    fit(slot?.querySelector(':scope > .moka-radial-menu__choices'));
}

function slotsOf(ring) {
    return [...ring.querySelectorAll(':scope > .moka-radial-menu__slot')];
}

function choicesOf(slot) {
    return slot ? [...slot.querySelectorAll(':scope > .moka-radial-menu__choices > .moka-radial-menu__choice')] : [];
}

function follow(ring, state, x, y) {
    const centre = ring.getBoundingClientRect();
    const dx = x - centre.left;
    const dy = y - centre.top;
    const distance = Math.hypot(dx, dy);
    if (distance > FLICK_DISTANCE) state.moved = true;

    const slots = slotsOf(ring);
    if (slots.length === 0) return;

    // Over a list that shows, the choice under the pointer, whether the pointer or the keyboard opened it. Bounds
    // rather than the event's target, since a held button keeps the pointer's events on what it was pressed on.
    for (let i = 0; i < slots.length; i++) {
        const hit = choicesOf(slots[i]).findIndex((choice) => within(choice.getBoundingClientRect(), x, y));
        if (hit >= 0) {
            lead(ring, state);
            point(ring, state, i, hit);
            return;
        }
    }

    const hub = ring.querySelector(':scope > .moka-radial-menu__hub');
    const track = ring.querySelector(':scope > .moka-radial-menu__track');
    const button = slots[0].querySelector(':scope > .moka-radial-menu__button');
    const inner = (hub?.offsetWidth ?? 0) * 0.44;
    const outer = (track?.offsetWidth ?? 0) / 2 + (button?.offsetWidth ?? 0) * 0.8;

    if (distance < inner) {
        lead(ring, state);
        point(ring, state, -1, -1);
        return;
    }

    // Past the buttons the highlight stays where it was, so a flick can overshoot.
    if (distance > outer) return;

    let degrees = (Math.atan2(dy, dx) * 180) / Math.PI + 90;
    if (degrees < 0) degrees += 360;
    lead(ring, state);
    point(ring, state, Math.round(degrees / (360 / slots.length)) % slots.length, -1);
}

function lead(ring, state) {
    if (state.leads) return;
    state.leads = true;
    ring.setAttribute('data-moka-pointer', '');
}

function point(ring, state, slotIndex, choiceIndex) {
    if (state.drawn && state.slot === slotIndex && state.choice === choiceIndex) return;

    const slots = slotsOf(ring);
    const slotChanged = !state.drawn || state.slot !== slotIndex;
    state.drawn = true;
    state.slot = slotIndex;
    state.choice = choiceIndex;

    slots.forEach((slot, i) => {
        slot.toggleAttribute('data-moka-pointed', i === slotIndex);
        choicesOf(slot).forEach((choice, j) => choice.toggleAttribute('data-moka-pointed', i === slotIndex && j === choiceIndex));
    });

    const word = ring.querySelector(':scope > .moka-radial-menu__hub > .moka-radial-menu__pointer-word');
    const slot = slots[slotIndex];
    if (word) {
        const choice = choicesOf(slot)[choiceIndex];
        const button = slot?.querySelector(':scope > .moka-radial-menu__button');
        const hub = ring.querySelector(':scope > .moka-radial-menu__hub');
        word.textContent = choice
            ? choice.querySelector('.moka-radial-menu__choice-text')?.textContent ?? ''
            : button?.getAttribute('aria-label') ?? hub?.dataset.mokaSub ?? '';
    }

    if (slotChanged && slot) fit(slot.querySelector(':scope > .moka-radial-menu__choices'));
}

// A hidden list has no box, so it never counts.
function within(rect, x, y) {
    return rect.width > 0 && x >= rect.left && x <= rect.right && y >= rect.top && y <= rect.bottom;
}

// Moves a shown list back inside the viewport through two custom properties its CSS adds to its offset.
function fit(list) {
    if (!list) return;

    list.style.removeProperty('--moka-radial-fit-x');
    list.style.removeProperty('--moka-radial-fit-y');
    const rect = list.getBoundingClientRect();
    if (rect.width === 0 && rect.height === 0) return;

    const shift = (start, end, size) => {
        if (end > size - VIEWPORT_MARGIN) return Math.max(size - VIEWPORT_MARGIN - end, VIEWPORT_MARGIN - start);
        if (start < VIEWPORT_MARGIN) return VIEWPORT_MARGIN - start;
        return 0;
    };

    list.style.setProperty('--moka-radial-fit-x', `${shift(rect.left, rect.right, window.innerWidth)}px`);
    list.style.setProperty('--moka-radial-fit-y', `${shift(rect.top, rect.bottom, window.innerHeight)}px`);
}
