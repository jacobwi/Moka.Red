/**
 * MokaRadialMenu JS module - the pointer's side of the ring.
 *
 * The highlight follows the pointer's angle around the hub, the list of a button with choices shows while it is
 * pointed at, and a held button let go over a button picks it (a flick). None of that goes to .NET: the ring is told
 * where the pointer was just before a key (OnPointed), and what a flick picked (OnFlicked).
 *
 * The ring element marks the pointer's state with data attributes Blazor never renders, so a render never undoes
 * them: data-moka-pointer on the ring while the pointer leads, data-moka-pointed on the slot and choice it points at,
 * and on the hub while the pointer is in it.
 *
 * Every shape places its buttons through --moka-radial-angle on each slot, so the module reads the angles from there
 * and finds the nearest one, which works for a full ring, a half ring and a corner fan alike.
 */

// Shared with other packages; the relative import resolves against this module's own URL.
export { preventKeys } from '../Moka.Red.Core/moka-keys.js';
export { captureFocus, restoreFocus } from './MokaContextMenu.razor.js';

const VIEWPORT_MARGIN = 8;
const FLICK_DISTANCE = 14;
const DEAD_ZONE = 12;
const CLEARANCE = 4;
const MAX_CLEARING = 240;

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
 * The size of the window the ring is placed in, without scrollbars, so a shape can choose which way to open.
 * @returns {{width:number, height:number}}
 */
export function measureViewport() {
    const root = document.documentElement;
    return { width: root.clientWidth || window.innerWidth, height: root.clientHeight || window.innerHeight };
}

/**
 * Starts following the pointer for an open ring. Binding the same ring again replaces its state.
 * @param {HTMLElement} ring - The ring element (role="menu").
 * @param {object} dotNet - Receives OnPointed(index, choice) and OnFlicked(index, choice).
 * @param {boolean} flick - Whether a button was still held when the ring opened.
 * @param {{around?:boolean, reach?:number, inner?:number, turn?:boolean, arc?:boolean}} [options] - The shape:
 *   whether the buttons go all the way round, how far either side of a button's angle still points at it (as a share
 *   of the angle between buttons) when they don't, the hub's share of its size that counts as the hub, whether a pie
 *   wedge turns to the highlight, and whether choices sit on an arc outside their button.
 */
export function bind(ring, dotNet, flick, options) {
    if (!ring) return;
    unbind(ring);

    const o = options || {};
    const state = {
        dotNet, flick: !!flick, moved: !flick, leads: false, drawn: false, slot: -1, choice: -1, target: null, hub: false,
        around: o.around !== false,
        reach: Number.isFinite(o.reach) ? o.reach : 1,
        inner: Number.isFinite(o.inner) ? o.inner : 0.44,
        turn: !!o.turn,
        arc: !!o.arc,
        angle: undefined
    };

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
        letGo(ring, state);
    };

    document.addEventListener('pointermove', state.move, { capture: true, passive: true });
    document.addEventListener('pointerup', state.up, true);
    document.addEventListener('pointercancel', state.cancel, true);
    document.addEventListener('touchmove', state.touch, { capture: true, passive: false });
    ring.addEventListener('keydown', state.key, true);

    // The wedge of a pie swings to the keyboard's highlight too, which only the rendered classes say. Watching them
    // keeps the swing in the browser, with no call per key.
    if (state.turn) {
        state.observer = new MutationObserver(() => {
            if (!state.leads) turnToActive(ring, state);
        });
        state.observer.observe(ring, { subtree: true, childList: true, attributes: true, attributeFilter: ['class'] });
        turnToActive(ring, state);
    }

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
    state.observer?.disconnect();
    rings.delete(ring);
}

/**
 * Forgets what the pointer pointed at, after the ring swapped its buttons for a button's choices or back, so the
 * keyboard's highlight shows until the pointer moves again.
 * @param {HTMLElement} ring - The ring element.
 */
export function reset(ring) {
    const state = rings.get(ring);
    if (!state) return;
    letGo(ring, state);
    if (state.turn) turnToActive(ring, state);
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

function angleOf(element, property = '--moka-radial-angle') {
    const value = parseFloat(element.style.getPropertyValue(property));
    return Number.isFinite(value) ? value : 0;
}

function angleBetween(a, b) {
    return Math.abs((((a - b) % 360) + 540) % 360 - 180);
}

function nearest(elements, degrees, reach, property) {
    let best = -1;
    let gap = Infinity;
    elements.forEach((element, i) => {
        const d = angleBetween(degrees, angleOf(element, property));
        if (d < gap) { gap = d; best = i; }
    });
    return gap <= reach ? best : -1;
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

    const degrees = (Math.atan2(dy, dx) * 180) / Math.PI;

    // On the arc of choices outside a button, the nearest choice by angle, so a flick that carries on outward lands
    // on one without having to hit its pill.
    if (state.arc) {
        const hit = arcChoice(slots, centre, distance, degrees);
        if (hit) {
            lead(ring, state);
            point(ring, state, hit.slot, hit.choice);
            return;
        }
    }

    const hub = ring.querySelector(':scope > .moka-radial-menu__hub');
    const inner = hub ? Math.max(hub.offsetWidth, hub.offsetHeight) * state.inner : DEAD_ZONE;
    if (distance < inner) {
        lead(ring, state);
        point(ring, state, -1, -1, !!hub);
        return;
    }

    // Past the buttons the highlight stays where it was, so a flick can overshoot.
    if (distance > outerRadius(ring, slots, centre)) return;

    let reach = 180;
    if (!state.around) {
        const step = slots.length > 1 ? angleBetween(angleOf(slots[1]), angleOf(slots[0])) : 90;
        reach = step * state.reach;
    }

    const index = nearest(slots, degrees, reach);
    if (index < 0) return;

    lead(ring, state);
    point(ring, state, index, -1);
}

function outerRadius(ring, slots, centre) {
    const track = ring.querySelector(':scope > .moka-radial-menu__track, :scope > .moka-radial-menu__disc');
    const button = slots[0].querySelector(':scope > .moka-radial-menu__button');
    if (track) return track.offsetWidth / 2 + (button?.offsetWidth ?? 0) * 0.8;

    // No band to measure: as far as the farthest button reaches, and a button's width more.
    let far = 0;
    for (const slot of slots) {
        const rect = slot.querySelector(':scope > .moka-radial-menu__button')?.getBoundingClientRect();
        if (!rect) continue;
        const across = Math.max(Math.abs(rect.left - centre.left), Math.abs(rect.right - centre.left));
        const down = Math.max(Math.abs(rect.top - centre.top), Math.abs(rect.bottom - centre.top));
        far = Math.max(far, Math.hypot(across, down) + Math.min(rect.width, rect.height));
    }
    return far;
}

function arcChoice(slots, centre, distance, degrees) {
    for (let i = 0; i < slots.length; i++) {
        if (!slots[i].querySelector(':scope > .moka-radial-menu__choices--arc')) continue;

        const choices = choicesOf(slots[i]);
        const first = choices[0]?.getBoundingClientRect();
        if (!first || first.width === 0) continue;

        const radius = Math.hypot(first.left + first.width / 2 - centre.left, first.top + first.height / 2 - centre.top);
        if (distance < radius - first.height * 0.7 || distance > radius + first.height * 1.6) continue;

        const step = choices.length > 1
            ? angleBetween(angleOf(choices[1], '--moka-radial-choice-angle'), angleOf(choices[0], '--moka-radial-choice-angle'))
            : 24;
        const hit = nearest(choices, degrees, step / 2 + 2, '--moka-radial-choice-angle');
        if (hit >= 0) return { slot: i, choice: hit };
    }

    return null;
}

function lead(ring, state) {
    if (state.leads) return;
    state.leads = true;
    ring.setAttribute('data-moka-pointer', '');
}

function letGo(ring, state) {
    state.leads = false;
    point(ring, state, -1, -1);
    state.drawn = false;
    ring.removeAttribute('data-moka-pointer');
}

function point(ring, state, slotIndex, choiceIndex, onHub = false) {
    const slots = slotsOf(ring);
    const slot = slots[slotIndex] ?? null;
    if (state.drawn && state.slot === slotIndex && state.choice === choiceIndex && state.target === slot && state.hub === onHub) return;

    const slotChanged = !state.drawn || state.slot !== slotIndex || state.target !== slot;
    state.drawn = true;
    state.slot = slotIndex;
    state.choice = choiceIndex;
    state.target = slot;
    state.hub = onHub;

    slots.forEach((each, i) => {
        each.toggleAttribute('data-moka-pointed', i === slotIndex);
        choicesOf(each).forEach((choice, j) => choice.toggleAttribute('data-moka-pointed', i === slotIndex && j === choiceIndex));
    });

    const hub = ring.querySelector(':scope > .moka-radial-menu__hub');
    hub?.toggleAttribute('data-moka-pointed', onHub);

    const word = hub?.querySelector(':scope > .moka-radial-menu__pointer-word');
    if (word) {
        const choice = choicesOf(slot)[choiceIndex];
        const button = slot?.querySelector(':scope > .moka-radial-menu__button');
        word.textContent = choice
            ? choice.querySelector('.moka-radial-menu__choice-text')?.textContent ?? ''
            : button?.getAttribute('aria-label') ?? hub?.dataset.mokaSub ?? '';
    }

    if (slotChanged && slot) fit(slot.querySelector(':scope > .moka-radial-menu__choices'));
    if (state.turn && slot) turnTo(ring, state, angleOf(slot));
}

function turnToActive(ring, state) {
    const active = ring.querySelector(':scope > .moka-radial-menu__slot--active');
    if (active) turnTo(ring, state, angleOf(active));
}

// The lit wedge turns the short way round, so its angle keeps counting past a full turn instead of wrapping.
function turnTo(ring, state, degrees) {
    const lit = ring.querySelector(':scope > .moka-radial-menu__disc > .moka-radial-menu__disc-lit');
    if (!lit) return;

    const target = degrees + 90;
    const first = !Number.isFinite(state.angle);
    state.angle = first ? target : state.angle + ((((target - state.angle) % 360) + 540) % 360) - 180;

    // The first angle is where the wedge starts, not somewhere to swing from.
    if (first) lit.style.transition = 'none';
    lit.style.setProperty('--moka-radial-turn', `${state.angle}deg`);
    if (first) {
        lit.getBoundingClientRect();
        lit.style.removeProperty('transition');
    }
}

// A hidden list has no box, so it never counts.
function within(rect, x, y) {
    return rect.width > 0 && x >= rect.left && x <= rect.right && y >= rect.top && y <= rect.bottom;
}

// Moves a shown list clear of its own button and the word printed beside it, then back inside the viewport,
// through two custom properties its CSS adds to its offset.
function fit(list) {
    if (!list) return;

    list.style.removeProperty('--moka-radial-fit-x');
    list.style.removeProperty('--moka-radial-fit-y');
    const stem = list.parentElement?.querySelector(':scope > .moka-radial-menu__stem');
    stem?.style.removeProperty('--moka-radial-stem-extra');
    const rect = list.getBoundingClientRect();
    if (rect.width === 0 && rect.height === 0) return;

    const [clearX, clearY, along] = clearOfOwnButton(list, rect);
    if (along > 0) stem?.style.setProperty('--moka-radial-stem-extra', `${along}px`);

    const shift = (start, end, size) => {
        if (end > size - VIEWPORT_MARGIN) return Math.max(size - VIEWPORT_MARGIN - end, VIEWPORT_MARGIN - start);
        if (start < VIEWPORT_MARGIN) return VIEWPORT_MARGIN - start;
        return 0;
    };

    const x = clearX + shift(rect.left + clearX, rect.right + clearX, window.innerWidth);
    const y = clearY + shift(rect.top + clearY, rect.bottom + clearY, window.innerHeight);
    list.style.setProperty('--moka-radial-fit-x', `${x}px`);
    list.style.setProperty('--moka-radial-fit-y', `${y}px`);
}

// A pill, a printed word or a wide wedge can reach past where a list starts, so the list moves on in the direction it
// opens until nothing of its own slot is under it.
function clearOfOwnButton(list, rect) {
    const slot = list.parentElement;
    const direction = openingDirection(list, slot);
    if (!slot || !direction) return [0, 0, 0];

    const obstacles = [...slot.querySelectorAll(':scope > .moka-radial-menu__button, :scope > .moka-radial-menu__label')]
        .map((element) => element.getBoundingClientRect())
        .filter((r) => r.width > 0);

    const hits = (dx, dy) => obstacles.some((r) =>
        rect.left + dx < r.right + CLEARANCE && rect.right + dx > r.left - CLEARANCE &&
        rect.top + dy < r.bottom + CLEARANCE && rect.bottom + dy > r.top - CLEARANCE);

    for (let along = 0; along <= MAX_CLEARING; along += 4) {
        const dx = direction[0] * along;
        const dy = direction[1] * along;
        if (!hits(dx, dy)) return [dx, dy, along];
    }

    return [0, 0, 0];
}

function openingDirection(list, slot) {
    const classes = list.classList;
    if (classes.contains('moka-radial-menu__choices--right')) return [1, 0];
    if (classes.contains('moka-radial-menu__choices--left')) return [-1, 0];
    if (classes.contains('moka-radial-menu__choices--top')) return [0, -1];
    if (classes.contains('moka-radial-menu__choices--bottom')) return [0, 1];
    if (classes.contains('moka-radial-menu__choices--flyout') && slot) {
        const radians = (angleOf(slot) * Math.PI) / 180;
        return [Math.cos(radians), Math.sin(radians)];
    }
    return null;
}
