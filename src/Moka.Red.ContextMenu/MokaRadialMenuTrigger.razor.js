/**
 * MokaRadialMenuTrigger JS module - a long press on a touch screen.
 *
 * iOS sends no contextmenu event for a long press, so this sends one: on the element pressed, at the finger, with a
 * button held, so the ring opens there and letting go over a button picks it. Android sends its own as well, a
 * little later; while ours is fresh it is swallowed, and when Android's comes first ours never fires.
 */

const PRESS_MS = 480;
const SLOP = 10;
const SWALLOW_MS = 1000;

const bound = new WeakSet();

/**
 * Listens for long presses inside the trigger.
 * @param {HTMLElement} wrapper - The trigger's wrapper element.
 */
export function bindLongPress(wrapper) {
    if (!wrapper || bound.has(wrapper)) return;
    bound.add(wrapper);

    let timer = 0;
    let press = null;
    let firedAt = -Infinity;

    const cancel = () => {
        clearTimeout(timer);
        timer = 0;
        press = null;
    };

    wrapper.addEventListener('pointerdown', (e) => {
        cancel();
        if (e.pointerType !== 'touch' || !e.isPrimary) return;

        press = { x: e.clientX, y: e.clientY, target: e.target };
        timer = setTimeout(() => {
            const { x, y, target } = press ?? {};
            timer = 0;
            press = null;
            if (!target?.isConnected) return;

            firedAt = performance.now();
            target.dispatchEvent(new MouseEvent('contextmenu', {
                bubbles: true, cancelable: true, composed: true,
                clientX: x, clientY: y, button: 2, buttons: 1, detail: 1
            }));
        }, PRESS_MS);
    });

    wrapper.addEventListener('pointermove', (e) => {
        if (press && Math.hypot(e.clientX - press.x, e.clientY - press.y) > SLOP) cancel();
    }, { passive: true });

    wrapper.addEventListener('pointerup', cancel);
    wrapper.addEventListener('pointercancel', cancel);

    wrapper.addEventListener('contextmenu', (e) => {
        if (!e.isTrusted) return;

        if (performance.now() - firedAt < SWALLOW_MS) {
            e.preventDefault();
            e.stopPropagation();
            return;
        }

        cancel();
    }, true);
}
