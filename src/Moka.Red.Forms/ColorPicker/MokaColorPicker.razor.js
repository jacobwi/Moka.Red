/**
 * MokaColorPicker - the panel's drags and keys, for every layout but the original one.
 *
 * The square, the hue bar or ring and the alpha bar follow the pointer here, through custom properties on the
 * element carrying data-cp-state, and .NET hears only where the pointer was let go, so a drag never waits on the
 * circuit. A key on one of them is a single step and is reported at once. The element's data attributes hold the
 * color as .NET last rendered it, so nothing here has to be told when the parameters change.
 *
 * The same binding finds the eyedropper buttons (data-cp-eye), opens the browser's EyeDropper from the click that
 * pressed one, so the click's user activation is still there, and marks the bound element with
 * data-moka-eyedropper where the browser has one, which is what shows those buttons.
 */

/** Bindings by key, so a panel that left the page can still be let go of. */
const bindings = new Map();

const supportsEyeDropper = typeof window !== 'undefined' && 'EyeDropper' in window;

/**
 * Starts listening on a panel. Binding the same key again replaces the earlier binding.
 * @param {string} key - Unique on the page; unbind() takes the same key.
 * @param {HTMLElement} panel - The element the listeners go on.
 * @param {object} dotNetRef - Reference used to call OnPanelPicked, OnEyeDropped and OnPanelLeft.
 * @param {{reportLeave?: boolean}} options - reportLeave calls OnPanelLeft when focus leaves the panel.
 */
export function bind(key, panel, dotNetRef, options) {
	unbind(key);
	if (!panel) return;

	if (supportsEyeDropper) panel.setAttribute('data-moka-eyedropper', '');

	const state = { panel, dotNetRef, drag: null, reportLeave: options?.reportLeave === true };
	state.listeners = {
		pointerdown: e => begin(state, e),
		keydown: e => onKey(state, e),
		input: e => slide(e),
		click: e => click(state, e),
		focusout: e => leave(state, e)
	};

	for (const [name, listener] of Object.entries(state.listeners)) {
		panel.addEventListener(name, listener);
	}

	bindings.set(key, state);
}

/**
 * Stops listening, and drops a drag in progress without reporting it.
 * @param {string} key - The key bind() was given.
 */
export function unbind(key) {
	const state = bindings.get(key);
	if (!state) return;

	bindings.delete(key);
	state.drag?.stop();
	for (const [name, listener] of Object.entries(state.listeners)) {
		state.panel.removeEventListener(name, listener);
	}
}

/**
 * Moves focus to the first control inside an element: a slider, then a field, then a button.
 * @param {HTMLElement} container - Where to look.
 */
export function focusFirst(container) {
	const target = container?.querySelector('[data-cp-drag]:not([hidden]), input:not([type=hidden]):not([disabled]), button:not([disabled])');
	target?.focus({ preventScroll: true });
}

const clamp = (n, low, high) => Math.min(high, Math.max(low, n));

function read(host) {
	return {
		h: Number(host.dataset.h) || 0,
		s: Number(host.dataset.s) || 0,
		v: Number(host.dataset.v) || 0,
		a: host.dataset.a === undefined ? 1 : Number(host.dataset.a)
	};
}

function hsvToRgb(h, s, v) {
	const f = n => {
		const k = (n + h / 60) % 6;
		return v - v * s * Math.max(0, Math.min(k, 4 - k, 1));
	};
	return [f(5), f(3), f(1)].map(x => Math.round(clamp(x, 0, 1) * 255));
}

function hsvToHsl(h, s, v) {
	const l = v * (1 - s / 2);
	return [h, l <= 0 || l >= 1 ? 0 : (v - l) / Math.min(l, 1 - l), l];
}

function hslToHsv(h, s, l) {
	const v = l + s * Math.min(l, 1 - l);
	return [h, v === 0 ? 0 : 2 * (1 - l / v), v];
}

const hex2 = n => n.toString(16).padStart(2, '0');

// The same custom properties .NET renders, so the panel looks the same before and after .NET answers.
function paint(host, color) {
	const rgb = hsvToRgb(color.h, color.s, color.v);
	const hex = '#' + rgb.map(hex2).join('');
	const [, sl, ll] = hsvToHsl(color.h, color.s, color.v);
	const style = host.style;
	style.setProperty('--_cp-hue', color.h.toFixed(2));
	style.setProperty('--_cp-sat', color.s.toFixed(4));
	style.setProperty('--_cp-val', color.v.toFixed(4));
	style.setProperty('--_cp-alpha', color.a.toFixed(4));
	style.setProperty('--_cp-color', hex);
	style.setProperty('--_cp-color-a', hex + hex2(Math.round(color.a * 255)));
	style.setProperty('--_cp-sl', (sl * 100).toFixed(2) + '%');
	style.setProperty('--_cp-ll', (ll * 100).toFixed(2) + '%');

	for (const slider of host.querySelectorAll('[data-cp-drag]')) {
		const kind = slider.dataset.cpDrag;
		if (kind === 'sv') {
			const s = Math.round(color.s * 100);
			slider.setAttribute('aria-valuenow', String(s));
			slider.setAttribute('aria-valuetext', `Saturation ${s}%, brightness ${Math.round(color.v * 100)}%`);
		} else if (kind === 'hue' || kind === 'ring') {
			slider.setAttribute('aria-valuenow', String(Math.round(color.h)));
			slider.setAttribute('aria-valuetext', `${Math.round(color.h)} degrees`);
		} else if (kind === 'alpha') {
			const a = Math.round(color.a * 100);
			slider.setAttribute('aria-valuenow', String(a));
			slider.setAttribute('aria-valuetext', `${a}% opaque`);
		}
	}
}

function commit(state, host, color) {
	host.dataset.h = String(color.h);
	host.dataset.s = String(color.s);
	host.dataset.v = String(color.v);
	host.dataset.a = String(color.a);
	state.dotNetRef.invokeMethodAsync('OnPanelPicked', color.h, color.s, color.v, color.a).catch(() => {});
}

function place(kind, slider, e, color) {
	const box = slider.getBoundingClientRect();
	if (box.width <= 0 || box.height <= 0) return;

	const x = clamp((e.clientX - box.left) / box.width, 0, 1);
	const y = clamp((e.clientY - box.top) / box.height, 0, 1);
	if (kind === 'sv') {
		color.s = x;
		color.v = 1 - y;
	} else if (kind === 'hue') {
		color.h = x * 360;
	} else if (kind === 'alpha') {
		color.a = x;
	} else if (kind === 'ring') {
		const dx = e.clientX - (box.left + box.width / 2);
		const dy = e.clientY - (box.top + box.height / 2);
		color.h = (Math.atan2(dx, -dy) * 180 / Math.PI + 360) % 360;
	}
}

function begin(state, e) {
	if (e.button !== 0 || state.drag) return;

	const slider = e.target instanceof Element ? e.target.closest('[data-cp-drag]') : null;
	const host = slider?.closest('[data-cp-state]');
	if (!slider || !host || !state.panel.contains(slider) || host.dataset.disabled === 'true') return;

	// No text selection or native drag, and focus goes to the slider as a click would have put it there.
	e.preventDefault();
	slider.focus({ preventScroll: true });

	const kind = slider.dataset.cpDrag;
	const first = read(host);
	const color = { ...first };

	function move(ev) {
		place(kind, slider, ev, color);
		paint(host, color);
	}

	function stop() {
		window.removeEventListener('pointermove', move);
		window.removeEventListener('pointerup', up);
		window.removeEventListener('pointercancel', cancel);
		if (slider.hasPointerCapture?.(e.pointerId)) slider.releasePointerCapture(e.pointerId);
		delete host.dataset.dragging;
		state.drag = null;
	}

	function up() {
		stop();
		if (color.h !== first.h || color.s !== first.s || color.v !== first.v || color.a !== first.a) {
			commit(state, host, color);
		}
	}

	// The browser took the pointer (a scroll, a system gesture): the panel goes back to where it was.
	function cancel() {
		stop();
		paint(host, first);
	}

	state.drag = { stop };
	host.dataset.dragging = kind;
	window.addEventListener('pointermove', move);
	window.addEventListener('pointerup', up);
	window.addEventListener('pointercancel', cancel);
	slider.setPointerCapture?.(e.pointerId);
	move(e);
}

const STEP_KEYS = ['ArrowLeft', 'ArrowRight', 'ArrowUp', 'ArrowDown', 'PageUp', 'PageDown', 'Home', 'End'];

function onKey(state, e) {
	const target = e.target instanceof Element ? e.target : null;
	if (!target) return;

	if (target.matches('[role=tab]')) {
		tabKey(target, e);
		return;
	}

	if (target.matches('input[data-cp-hex]') && e.key === 'Enter') {
		// Enter takes the typed color without submitting a form around the picker.
		e.preventDefault();
		target.dispatchEvent(new Event('change', { bubbles: true }));
		target.select();
		return;
	}

	if (target.matches('input[type=range][data-cp-range]') && e.shiftKey && e.key.startsWith('Arrow')) {
		shiftRange(target, e);
		return;
	}

	if (target.matches('[data-cp-drag]') && STEP_KEYS.includes(e.key) && !e.ctrlKey && !e.metaKey && !e.altKey) {
		stepSlider(state, target, e);
		return;
	}

	if ((e.key === 'i' || e.key === 'I') && !e.ctrlKey && !e.metaKey && !e.altKey && !isTyping(target)) {
		const button = state.panel.querySelector('[data-cp-eye-key]');
		if (button && button.offsetParent !== null) {
			e.preventDefault();
			button.click();
		}
	}
}

function isTyping(element) {
	return element.matches('input, textarea, select, [contenteditable=""], [contenteditable=true]');
}

function stepSlider(state, slider, e) {
	const host = slider.closest('[data-cp-state]');
	if (!host || host.dataset.disabled === 'true') return;

	const kind = slider.dataset.cpDrag;
	const color = read(host);
	const big = e.shiftKey;

	if (kind === 'sv') {
		const step = big ? 0.1 : 0.01;
		switch (e.key) {
			case 'ArrowLeft': color.s -= step; break;
			case 'ArrowRight': color.s += step; break;
			case 'ArrowUp': color.v += step; break;
			case 'ArrowDown': color.v -= step; break;
			case 'PageUp': color.v += 0.1; break;
			case 'PageDown': color.v -= 0.1; break;
			case 'Home': color.s = 0; break;
			case 'End': color.s = 1; break;
		}
		color.s = clamp(color.s, 0, 1);
		color.v = clamp(color.v, 0, 1);
	} else {
		const alpha = kind === 'alpha';
		const max = alpha ? 1 : 360;
		const small = alpha ? 0.01 : 1;
		const large = alpha ? 0.1 : 10;
		let value = alpha ? color.a : color.h;
		switch (e.key) {
			case 'ArrowLeft': case 'ArrowDown': value -= big ? large : small; break;
			case 'ArrowRight': case 'ArrowUp': value += big ? large : small; break;
			case 'PageDown': value -= large; break;
			case 'PageUp': value += large; break;
			case 'Home': value = 0; break;
			case 'End': value = max; break;
		}
		value = clamp(Math.round(value / small) * small, 0, max);
		if (alpha) color.a = value; else color.h = value;
	}

	e.preventDefault();
	paint(host, color);
	commit(state, host, color);
}

function shiftRange(range, e) {
	e.preventDefault();
	const step = Number(range.step) || 1;
	const min = Number(range.min) || 0;
	const max = Number(range.max) || 100;
	const down = e.key === 'ArrowLeft' || e.key === 'ArrowDown';
	range.value = String(clamp(Number(range.value) + (down ? -10 : 10) * step, min, max));
	range.dispatchEvent(new Event('input', { bubbles: true }));
	range.dispatchEvent(new Event('change', { bubbles: true }));
}

// A range slider moves the panel's colors while it is dragged, and .NET hears its change event on release.
function slide(e) {
	const range = e.target instanceof Element ? e.target.closest('input[type=range][data-cp-range]') : null;
	const host = range?.closest('[data-cp-state]');
	if (!range || !host) return;

	const color = read(host);
	const [h, sl, ll] = hsvToHsl(color.h, color.s, color.v);
	const hsl = { h, s: sl, l: ll };
	const which = range.dataset.cpRange;
	hsl[which] = which === 'h' ? Number(range.value) : Number(range.value) / 100;
	const [, s, v] = hslToHsv(hsl.h, hsl.s, hsl.l);
	paint(host, { h: hsl.h, s, v, a: color.a });
}

function tabKey(tab, e) {
	const list = tab.closest('[role=tablist]');
	const tabs = list ? [...list.querySelectorAll('[role=tab]')] : [];
	const i = tabs.indexOf(tab);
	if (i < 0) return;

	let j;
	switch (e.key) {
		case 'ArrowRight': j = (i + 1) % tabs.length; break;
		case 'ArrowLeft': j = (i - 1 + tabs.length) % tabs.length; break;
		case 'Home': j = 0; break;
		case 'End': j = tabs.length - 1; break;
		default: return;
	}

	e.preventDefault();
	tabs[j].focus();
	tabs[j].click();
}

async function click(state, e) {
	const button = e.target instanceof Element ? e.target.closest('[data-cp-eye]') : null;
	if (!button || !state.panel.contains(button) || button.disabled) return;

	if (!supportsEyeDropper) return;

	let picked = null;
	try {
		const result = await new window.EyeDropper().open();
		picked = result?.sRGBHex ?? null;
	} catch {
		// Escape, or a click the browser didn't count as the person's own.
	}

	state.dotNetRef.invokeMethodAsync('OnEyeDropped', picked).catch(() => {});
}

function leave(state, e) {
	if (!state.reportLeave) return;

	const next = e.relatedTarget;
	if (next instanceof Node && state.panel.contains(next)) return;

	state.dotNetRef.invokeMethodAsync('OnPanelLeft').catch(() => {});
}
