/**
 * MokaRangeBar - dragging a handle, or with MoveRange the whole range.
 *
 * The handle, its flag and the range follow the pointer here, through the custom properties on the bar that the
 * stylesheet places them from, and .NET hears only where the handle was let go, so a drag never waits on the
 * circuit. A flag's text comes from the app's formatter in .NET: each value is asked for once per drag and kept,
 * and the flag moves on with its old text until the answer arrives.
 *
 * The bar's data attributes hold the scale and the range as .NET last rendered them, so nothing here has to be
 * updated when the parameters change.
 */

/** Listeners and the drag in progress, keyed by the bar. */
const bars = new WeakMap();

/**
 * Starts listening for drags on a bar. Binding the same bar again replaces the earlier binding.
 * @param {HTMLElement} bar - The bar element, holding the track and both handles.
 * @param {object} dotNetRef - Reference used to call FormatValue and OnDragged.
 */
export function bind(bar, dotNetRef) {
	if (!bar) return;

	unbind(bar);
	const state = { dotNetRef, drag: null };
	state.onDown = e => begin(bar, state, e);
	bar.addEventListener('pointerdown', state.onDown);
	bars.set(bar, state);
}

/**
 * Stops listening, and drops a drag in progress without reporting it.
 * @param {HTMLElement} bar - The bar element.
 */
export function unbind(bar) {
	const state = bar && bars.get(bar);
	if (!state) return;

	bar.removeEventListener('pointerdown', state.onDown);
	state.drag?.stop();
	bars.delete(bar);
}

function begin(bar, state, e) {
	if (e.button !== 0 || bar.dataset.disabled === 'true' || state.drag) return;

	const grabbed = e.target instanceof Element ? e.target.closest('[data-moka-handle]') : null;
	if (!grabbed || !bar.contains(grabbed)) return;

	// No text selection or native drag, and focus goes to the handle as a click would have put it there.
	e.preventDefault();
	grabbed.focus();

	const low = Number(bar.dataset.min);
	const high = Number(bar.dataset.max);
	const step = Number(bar.dataset.step);
	const decimals = Math.min(15, Math.max(0, Number(bar.dataset.decimals) || 0));
	const first = { start: Number(bar.dataset.start), end: Number(bar.dataset.end) };
	const current = { start: first.start, end: first.end };

	// What each handle's flag says now. The formatter may have changed since the last drag, so nothing older is kept.
	const texts = new Map();
	const asked = new Set();
	const firstTexts = { start: flagText(bar, 'start'), end: flagText(bar, 'end') };
	texts.set(first.start, firstTexts.start);
	texts.set(first.end, firstTexts.end);

	// The range itself moves both ends together, keeping its width, from where the pointer first took it.
	const moving = grabbed.dataset.mokaHandle === 'range';
	const width = first.end - first.start;
	const anchor = moving ? valueAt(e.clientX) : 0;

	// Two handles on one value: the first move decides which one is held, or the one underneath could never leave.
	let which = moving ? 'range' : grabbed.dataset.mokaHandle;
	let undecided = !moving && first.start === first.end;

	const fraction = value => (high > low ? Math.min(1, Math.max(0, (value - low) / (high - low))) : 0);

	function valueAt(clientX) {
		const box = bar.getBoundingClientRect();
		const at = box.width > 0 ? Math.min(1, Math.max(0, (clientX - box.left) / box.width)) : 0;
		let value = low + at * (high - low);
		if (step > 0) {
			value = Math.round((value - low) / step) * step + low;
		}

		return Number(Math.min(high, Math.max(low, value)).toFixed(decimals));
	}

	function writeText(name, text) {
		const flag = bar.querySelector(`[data-moka-flag="${name}"]`);
		// The text node Blazor rendered is kept and changed, so its next render still reaches it.
		const node = flag?.firstChild;
		if (node && node.nodeType === Node.TEXT_NODE) {
			node.nodeValue = text;
		}

		bar.style.setProperty(`--_range-${name}-chars`, String(text.length));
		bar.querySelector(`[data-moka-handle="${name}"]`)?.setAttribute('aria-valuetext', text);
	}

	function show(name) {
		const value = current[name];
		bar.style.setProperty(`--_range-${name}`, String(fraction(value)));
		bar.querySelector(`[data-moka-handle="${name}"]`)?.setAttribute('aria-valuenow', String(value));

		if (texts.has(value)) {
			writeText(name, texts.get(value));
			return;
		}

		if (asked.has(value)) return;
		asked.add(value);
		state.dotNetRef.invokeMethodAsync('FormatValue', value).then(text => {
			texts.set(value, text);
			// An answer can come after the handle is let go. It still belongs on the flag while the bar shows the
			// value this drag left, which the data attributes say until .NET renders something else.
			for (const handle of ['start', 'end']) {
				const stillShown = state.drag === drag || (!state.drag && Number(bar.dataset[handle]) === value);
				if (current[handle] === value && stillShown) writeText(handle, text);
			}
		}, () => asked.delete(value));
	}

	function move(ev) {
		const value = valueAt(ev.clientX);
		if (moving) {
			const start = Math.min(high - width, Math.max(low, first.start + (value - anchor)));
			const rounded = Number(start.toFixed(decimals));
			if (rounded === current.start) return;

			current.start = rounded;
			current.end = Number((rounded + width).toFixed(decimals));
			show('start');
			show('end');
			return;
		}

		if (undecided) {
			if (value === current.start) return;
			which = value < current.start ? 'start' : 'end';
			undecided = false;
			bar.querySelector(`[data-moka-handle="${which}"]`)?.focus();
		}

		const next = which === 'start' ? Math.min(value, current.end) : Math.max(value, current.start);
		if (next === current[which]) return;

		current[which] = next;
		show(which);
	}

	function stop() {
		window.removeEventListener('pointermove', move);
		window.removeEventListener('pointerup', up);
		window.removeEventListener('pointercancel', cancel);
		if (grabbed.hasPointerCapture?.(e.pointerId)) {
			grabbed.releasePointerCapture(e.pointerId);
		}

		delete bar.dataset.dragging;
		state.drag = null;
	}

	function up() {
		stop();
		if (current.start === first.start && current.end === first.end) return;

		// Kept here too, so a second drag before .NET renders starts from where this one ended.
		bar.dataset.start = String(current.start);
		bar.dataset.end = String(current.end);
		state.dotNetRef.invokeMethodAsync('OnDragged', current.start, current.end).catch(() => {});
	}

	// The browser took the pointer (a scroll, a system gesture): everything goes back to where it was.
	function cancel() {
		stop();
		for (const name of ['start', 'end']) {
			current[name] = first[name];
			bar.style.setProperty(`--_range-${name}`, String(fraction(first[name])));
			bar.querySelector(`[data-moka-handle="${name}"]`)?.setAttribute('aria-valuenow', String(first[name]));
			writeText(name, firstTexts[name]);
		}
	}

	const drag = { stop };
	state.drag = drag;
	bar.dataset.dragging = which;

	// On the window, not the handle: a fast drag leaves the handle behind. Capture keeps the events coming when the
	// pointer leaves the window.
	window.addEventListener('pointermove', move);
	window.addEventListener('pointerup', up);
	window.addEventListener('pointercancel', cancel);
	grabbed.setPointerCapture?.(e.pointerId);
}

function flagText(bar, name) {
	return bar.querySelector(`[data-moka-flag="${name}"]`)?.textContent ?? '';
}
