// Box selection: a mouse press on empty space, a drag, and every item the rectangle touches is reported to .NET. One
// document listener serves every bound host and exists only while one is bound; everything else lives for one press.

// The values of MokaBoxSelectPhase and MokaBoxSelectMode. A test holds the two together.
const PHASE = { Started: 0, Changed: 1, Finished: 2, Cancelled: 3 };
const MODE = { Replace: 0, Add: 1, Toggle: 2 };

// How far a press moves before it is a box rather than a click, as Windows does.
const THRESHOLD = 4;

// Auto-scroll starts this close to the scroll container's edge and goes faster the closer the pointer is.
const EDGE = 36;
const MAX_STEP = 28;

const DEFAULT_KEY = "data-moka-select-key";
const DEFAULT_INDEX = "data-moka-select-index";
const SELECTING = "data-moka-box-selecting";

// What a press on means the person wants that thing, not a box.
const INTERACTIVE = [
	"a[href]", "area[href]", "button", "input", "select", "textarea", "label", "summary", "iframe", "embed", "object",
	"audio[controls]", "video[controls]", '[contenteditable]:not([contenteditable="false"])',
	"[role=button]", "[role=link]", "[role=checkbox]", "[role=radio]", "[role=switch]", "[role=tab]", "[role=menuitem]",
	"[role=menuitemcheckbox]", "[role=menuitemradio]", "[role=option]", "[role=treeitem]", "[role=slider]",
	"[role=spinbutton]", "[role=textbox]", "[role=searchbox]", "[role=combobox]", "[role=scrollbar]",
	"[data-moka-no-box-select]"
].join(", ");

const hosts = new Map();
let press = null;

/**
 * Lets a host draw boxes, or takes new options and a new reporter if it already can.
 * @param {HTMLElement} host The element the options' selectors start from.
 * @param {object|null} reporter A DotNetObjectReference whose method hears every report.
 * @param {object} options
 * @param {string} [options.area] Selector from the host for where presses count. Null is the host.
 * @param {string} options.box Selector from the host for the element drawn as the box.
 * @param {string} [options.items] Selector for the items. Null is elements carrying the key attribute.
 * @param {string} [options.key] The attribute holding an item's key.
 * @param {string} [options.scroller] Selector for the element that scrolls. Null is the nearest scrolling ancestor.
 * @param {object} [options.geometry] Lines of equal height, to cover items that aren't drawn.
 * @param {string} [options.method] The reporter's method.
 */
export function bindBoxSelect(host, reporter, options) {
	if (!host) {
		return;
	}

	const existing = hosts.get(host);
	if (existing) {
		existing.reporter = reporter ?? null;
		existing.options = options ?? {};
		if (press?.bound === existing) {
			press.dirty = true;
		}

		return;
	}

	hosts.set(host, { host, reporter: reporter ?? null, options: options ?? {}, queue: [], sending: false });
	if (hosts.size === 1) {
		document.addEventListener("pointerdown", onPointerDown, true);
	}
}

/**
 * Stops a host drawing boxes. A box being drawn is cancelled and reported so.
 * @param {HTMLElement} host The host.
 */
export function unbindBoxSelect(host) {
	const bound = host && hosts.get(host);
	if (!bound) {
		return;
	}

	if (press?.bound === bound) {
		cancel(press, false);
	}

	hosts.delete(host);
	if (hosts.size === 0) {
		document.removeEventListener("pointerdown", onPointerDown, true);
	}
}

function keyOf(options) {
	return options.key || DEFAULT_KEY;
}

function itemSelector(options) {
	return options.items || `[${CSS.escape(keyOf(options))}]`;
}

function areaOf(bound) {
	const { host, options } = bound;
	return options.area ? host.querySelector(options.area) : host;
}

function isScrolling(element) {
	const style = getComputedStyle(element);
	return /(auto|scroll|overlay)/.test(style.overflowY + style.overflowX);
}

// Null stands for the window.
function scrollerOf(bound, area) {
	const named = bound.options.scroller;
	if (named) {
		const found = area.closest(named) ?? document.querySelector(named);
		return found && found !== document.documentElement && found !== document.body ? found : null;
	}

	for (let element = area; element && element !== document.body && element !== document.documentElement; element = element.parentElement) {
		if (isScrolling(element)) {
			return element;
		}
	}

	return null;
}

function safeMatches(element, selector) {
	try {
		return element.matches(selector);
	} catch {
		return false;
	}
}

function isEmptySpace(target, area, options) {
	const items = itemSelector(options);
	for (let element = target; element && element !== area; element = element.parentElement) {
		if (safeMatches(element, items) || element.matches(INTERACTIVE) || element.draggable === true || element.isContentEditable) {
			return false;
		}
	}

	return true;
}

// A press on a scrollbar lands on the element that owns it, outside its client box.
function onScrollbar(target, e) {
	if (!target.clientWidth && !target.clientHeight) {
		return false;
	}

	const rect = target.getBoundingClientRect();
	const left = rect.left + target.clientLeft;
	const top = rect.top + target.clientTop;
	return e.clientX < left || e.clientX >= left + target.clientWidth || e.clientY < top || e.clientY >= top + target.clientHeight;
}

function modeOf(e) {
	if (e.shiftKey) {
		return MODE.Add;
	}

	return e.ctrlKey || e.metaKey ? MODE.Toggle : MODE.Replace;
}

function chooseHost(target) {
	let chosen = null;
	for (const bound of hosts.values()) {
		if (!bound.host.isConnected) {
			continue;
		}

		const area = areaOf(bound);
		if (!area) {
			continue;
		}

		const scroller = scrollerOf(bound, area);
		const inArea = area.contains(target);
		const onScroller = !inArea && scroller !== null && target === scroller && scroller.contains(area);
		if (!inArea && !onScroller) {
			continue;
		}

		// The innermost area wins, so a selection area inside another keeps its presses.
		if (!chosen || chosen.area.contains(area)) {
			chosen = { bound, area, scroller, inArea };
		}
	}

	return chosen;
}

function onPointerDown(e) {
	if (e.pointerType !== "mouse" || e.button !== 0 || !e.isPrimary || !(e.target instanceof Element)) {
		return;
	}

	// A mouse has one primary button, so a new press means the last one ended without this hearing it.
	if (press) {
		cancel(press, false);
	}

	const chosen = chooseHost(e.target);
	if (!chosen || (chosen.inArea && !isEmptySpace(e.target, chosen.area, chosen.bound.options)) || onScrollbar(e.target, e)) {
		return;
	}

	startPress(chosen, e);
}

function startPress(chosen, e) {
	const controller = new AbortController();
	const { signal } = controller;
	press = {
		...chosen,
		controller,
		pointerId: e.pointerId,
		mode: modeOf(e),
		startX: e.clientX,
		startY: e.clientY,
		x: e.clientX,
		y: e.clientY,
		dragging: false,
		dirty: false,
		frame: 0,
		box: null,
		drawn: null,
		start: null,
		startLine: null,
		signature: null,
		observer: null
	};

	const current = press;
	window.addEventListener("pointermove", event => onMove(current, event), { capture: true, signal });
	window.addEventListener("pointerup", event => onUp(current, event), { capture: true, signal });
	window.addEventListener("pointercancel", event => onPointerCancel(current, event), { capture: true, signal });
	window.addEventListener("keydown", event => onKey(current, event), { capture: true, signal });
	window.addEventListener("contextmenu", event => onContextMenu(current, event), { capture: true, signal });
	window.addEventListener("blur", () => cancel(current, false), { signal });
	document.addEventListener("selectstart", prevent, { capture: true, signal });
	document.addEventListener("dragstart", prevent, { capture: true, signal });
}

function prevent(e) {
	e.preventDefault();
}

function onMove(current, e) {
	if (current !== press || e.pointerId !== current.pointerId) {
		return;
	}

	current.x = e.clientX;
	current.y = e.clientY;
	if (current.dragging) {
		current.dirty = true;
	} else if (Math.hypot(current.x - current.startX, current.y - current.startY) >= THRESHOLD) {
		begin(current);
	}
}

function onUp(current, e) {
	if (current !== press || e.pointerId !== current.pointerId) {
		return;
	}

	if (!current.dragging) {
		end(current);
		return;
	}

	current.x = e.clientX;
	current.y = e.clientY;
	const { keys, range } = measure(current);
	end(current);
	report(current.bound, PHASE.Finished, current.mode, keys, range);
	swallowNextClick();
}

function onPointerCancel(current, e) {
	if (e.pointerId === current.pointerId) {
		cancel(current, false);
	}
}

function onKey(current, e) {
	if (current === press && current.dragging && e.key === "Escape") {
		e.preventDefault();
		e.stopImmediatePropagation();
		cancel(current, true);
	}
}

function onContextMenu(current, e) {
	if (current !== press) {
		return;
	}

	if (current.dragging) {
		e.preventDefault();
	}

	cancel(current, false);
}

function begin(current) {
	const area = current.area;
	if (!area.isConnected) {
		end(current);
		return;
	}

	current.dragging = true;
	current.start = toContent(current.scroller, current.startX, current.startY);
	current.startLine = lineAt(current, current.startY);

	try {
		area.setPointerCapture(current.pointerId);
	} catch {
		// The pointer is no longer down, which its pointerup will say.
	}

	const { signal } = current.controller;
	area.addEventListener("lostpointercapture", () => {
		if (current === press && current.dragging) {
			cancel(current, false);
		}
	}, { signal });
	(current.scroller ?? window).addEventListener("scroll", () => { current.dirty = true; }, { passive: true, signal });

	// Virtualized rows arrive while the list scrolls under the box, with no scroll of their own to say so.
	current.observer = new MutationObserver(() => { current.dirty = true; });
	current.observer.observe(area, { childList: true, subtree: true });

	current.bound.host.setAttribute(SELECTING, "");
	current.box = current.bound.options.box ? current.bound.host.querySelector(current.bound.options.box) : null;
	if (current.box) {
		current.box.hidden = false;
		current.drawn = { left: 0, top: 0 };
		current.box.style.left = "0px";
		current.box.style.top = "0px";
	}

	const { keys, range } = measure(current);
	current.signature = signatureOf(keys, range);
	report(current.bound, PHASE.Started, current.mode, keys, range);
	current.frame = requestAnimationFrame(() => frame(current));
}

function frame(current) {
	if (current !== press || !current.dragging) {
		return;
	}

	const scrolled = autoScroll(current);
	if (current.dirty || scrolled) {
		current.dirty = false;
		const { keys, range } = measure(current);
		const signature = signatureOf(keys, range);
		if (signature !== current.signature) {
			current.signature = signature;
			report(current.bound, PHASE.Changed, current.mode, keys, range);
		}
	}

	current.frame = requestAnimationFrame(() => frame(current));
}

function cancel(current, waitForRelease) {
	if (current !== press) {
		return;
	}

	const wasDragging = current.dragging;
	end(current);
	if (!wasDragging) {
		return;
	}

	report(current.bound, PHASE.Cancelled, current.mode, [], null);
	if (waitForRelease) {
		// The button is still down. Its release would click whatever is under it, which the cancelled box shouldn't.
		const stop = new AbortController();
		window.addEventListener("pointerup", () => {
			stop.abort();
			swallowNextClick();
		}, { capture: true, signal: stop.signal });
		window.addEventListener("pointerdown", () => stop.abort(), { capture: true, signal: stop.signal });
		window.addEventListener("blur", () => stop.abort(), { signal: stop.signal });
	}
}

function end(current) {
	if (current !== press) {
		return;
	}

	press = null;
	current.controller.abort();
	cancelAnimationFrame(current.frame);
	current.observer?.disconnect();
	if (current.dragging) {
		current.bound.host.removeAttribute(SELECTING);
		if (current.box) {
			current.box.hidden = true;
		}

		try {
			if (current.area.hasPointerCapture(current.pointerId)) {
				current.area.releasePointerCapture(current.pointerId);
			}
		} catch {
			// The area left the page.
		}
	}
}

// The click a released drag ends with would reach a container's own onclick, which usually clears the selection the
// box just made. It arrives in the same task as the pointerup, so a timeout is late enough to stop listening.
function swallowNextClick() {
	const swallow = e => {
		e.preventDefault();
		e.stopImmediatePropagation();
	};

	window.addEventListener("click", swallow, { capture: true, once: true });
	setTimeout(() => window.removeEventListener("click", swallow, true), 0);
}

function viewOf(scroller) {
	const width = document.documentElement.clientWidth;
	const height = document.documentElement.clientHeight;
	if (!scroller) {
		return { left: 0, top: 0, right: width, bottom: height };
	}

	const rect = scroller.getBoundingClientRect();
	const left = rect.left + scroller.clientLeft;
	const top = rect.top + scroller.clientTop;
	return {
		left: Math.max(left, 0),
		top: Math.max(top, 0),
		right: Math.min(left + scroller.clientWidth, width),
		bottom: Math.min(top + scroller.clientHeight, height)
	};
}

function originOf(scroller) {
	if (!scroller) {
		return { x: -window.scrollX, y: -window.scrollY };
	}

	const rect = scroller.getBoundingClientRect();
	return { x: rect.left + scroller.clientLeft - scroller.scrollLeft, y: rect.top + scroller.clientTop - scroller.scrollTop };
}

function toContent(scroller, x, y) {
	const origin = originOf(scroller);
	return { x: x - origin.x, y: y - origin.y };
}

function clamp(value, low, high) {
	return Math.min(Math.max(value, low), high);
}

// The box in window coordinates, its start kept where it is in the content however far that has scrolled.
function boxOf(current, view) {
	const origin = originOf(current.scroller);
	const x = clamp(current.x, view.left, view.right);
	const y = clamp(current.y, view.top, view.bottom);
	const startX = current.start.x + origin.x;
	const startY = current.start.y + origin.y;
	const left = Math.min(startX, x);
	const top = Math.min(startY, y);
	return {
		left,
		top,
		right: Math.max(Math.max(startX, x), left + 1),
		bottom: Math.max(Math.max(startY, y), top + 1),
		pointerY: y
	};
}

function overlaps(rect, box) {
	return rect.left < box.right && rect.right > box.left && rect.top < box.bottom && rect.bottom > box.top;
}

function measure(current) {
	const view = viewOf(current.scroller);
	const box = boxOf(current, view);
	const { area, bound } = current;
	const { options } = bound;
	const key = keyOf(options);

	const keys = [];
	const seen = new Set();
	let items = [];
	try {
		items = area.querySelectorAll(itemSelector(options));
	} catch {
		// A selector the browser can't read finds nothing.
	}

	for (const item of items) {
		const rect = item.getBoundingClientRect();
		if ((rect.width > 0 || rect.height > 0) && overlaps(rect, box)) {
			const value = item.getAttribute(key);
			if (value !== null && !seen.has(value)) {
				seen.add(value);
				keys.push(value);
			}
		}
	}

	const range = options.geometry ? rangeOf(current, box) : null;
	draw(current, box, view);
	return { keys, range };
}

function draw(current, box, view) {
	const element = current.box;
	if (!element || !element.isConnected) {
		return;
	}

	const left = Math.max(box.left, view.left);
	const top = Math.max(box.top, view.top);
	const right = Math.min(box.right, view.right);
	const bottom = Math.min(box.bottom, view.bottom);

	// Where the box's containing block starts, read from where the box is now, so any positioned parent will do.
	const rect = element.getBoundingClientRect();
	const originX = rect.left - current.drawn.left;
	const originY = rect.top - current.drawn.top;
	current.drawn = { left: left - originX, top: top - originY };
	element.style.left = `${current.drawn.left}px`;
	element.style.top = `${current.drawn.top}px`;
	element.style.width = `${Math.max(0, right - left)}px`;
	element.style.height = `${Math.max(0, bottom - top)}px`;
}

function signatureOf(keys, range) {
	const lines = range ? `${range[0]},${range[1]},${range[2]},${range[3]}` : "";
	return `${lines}|${keys.join("\u0000")}`;
}

// Reports go one at a time, each after the last has been handled, so Blazor Server never has more than one on the
// wire. A change waiting behind one in flight is replaced by the next, and the final report replaces it too.
function report(bound, phase, mode, keys, range) {
	const item = { phase, mode, keys, range };
	const queue = bound.queue;
	const last = queue[queue.length - 1];
	if (phase === PHASE.Changed && last?.phase === PHASE.Changed) {
		queue[queue.length - 1] = item;
	} else {
		if (phase === PHASE.Finished || phase === PHASE.Cancelled) {
			while (queue.length > 0 && queue[queue.length - 1].phase === PHASE.Changed) {
				queue.pop();
			}
		}

		queue.push(item);
	}

	pump(bound);
}

function pump(bound) {
	if (bound.sending || bound.queue.length === 0) {
		return;
	}

	const item = bound.queue.shift();
	const reporter = bound.reporter;
	if (!reporter) {
		pump(bound);
		return;
	}

	bound.sending = true;
	Promise.resolve()
		.then(() => reporter.invokeMethodAsync(bound.options.method || "HandleBoxSelect", item.phase, item.mode, item.keys, item.range))
		.catch(() => { })
		.finally(() => {
			bound.sending = false;
			pump(bound);
		});
}

function autoScroll(current) {
	const view = viewOf(current.scroller);
	const step = (position, low, high) => {
		const reach = Math.min(EDGE, (high - low) / 4);
		if (position < low + reach) {
			return -Math.ceil(MAX_STEP * Math.min(1, (low + reach - position) / reach));
		}

		if (position > high - reach) {
			return Math.ceil(MAX_STEP * Math.min(1, (position - (high - reach)) / reach));
		}

		return 0;
	};

	const dx = step(current.x, view.left, view.right);
	const dy = step(current.y, view.top, view.bottom);
	if (dx === 0 && dy === 0) {
		return false;
	}

	const element = current.scroller ?? document.scrollingElement ?? document.documentElement;
	const beforeX = element.scrollLeft;
	const beforeY = element.scrollTop;
	element.scrollLeft += dx;
	element.scrollTop += dy;
	return element.scrollLeft !== beforeX || element.scrollTop !== beforeY;
}

// Geometry: every item sits on a line of equal height, with the same number of columns on each, so where an item is
// follows from its index. Any drawn item that carries its index is enough to place the ones that aren't drawn.
function layoutOf(current) {
	const geometry = current.bound.options.geometry;
	const count = Math.max(0, Math.floor(geometry.count ?? 0));
	if (count === 0) {
		return null;
	}

	const columns = Math.max(1, Math.floor(geometry.columns ?? 1));
	const attribute = geometry.index || DEFAULT_INDEX;
	const base = Math.floor(geometry.base ?? 0);

	let found = [];
	try {
		found = current.area.querySelectorAll(`[${CSS.escape(attribute)}]`);
	} catch {
		return null;
	}

	const drawn = [];
	for (const element of found) {
		const index = Number(element.getAttribute(attribute)) - base;
		if (Number.isInteger(index) && index >= 0 && index < count) {
			drawn.push({ element, line: Math.floor(index / columns), column: index % columns });
		}
	}

	// Only a few rectangles are read: the first drawn item, the furthest line from it, and another column.
	const placed = entry => {
		entry.rect ??= entry.element.getBoundingClientRect();
		return entry.rect.width > 0 || entry.rect.height > 0;
	};

	const at = drawn.findIndex(placed);
	if (at < 0) {
		return null;
	}

	const anchor = drawn[at];
	const farLine = drawn.findLast(entry => entry.line !== anchor.line && placed(entry)) ?? null;
	const otherColumn = columns === 1 ? null : drawn.find(entry => entry.column !== anchor.column && placed(entry)) ?? null;

	const pitchY = geometry.lineHeight > 0
		? geometry.lineHeight
		: farLine ? (farLine.rect.top - anchor.rect.top) / (farLine.line - anchor.line) : anchor.rect.height;
	if (!(pitchY > 0)) {
		return null;
	}

	const pitchX = !otherColumn ? anchor.rect.width : (otherColumn.rect.left - anchor.rect.left) / (otherColumn.column - anchor.column);

	return { count, columns, anchor, pitchY, pitchX };
}

function lineAt(current, y) {
	if (!current.bound.options.geometry) {
		return null;
	}

	const layout = layoutOf(current);
	return layout ? layout.anchor.line + (y - layout.anchor.rect.top) / layout.pitchY : null;
}

// The lines are counted in line units from where the box started, measured while that place was drawn, so a long
// drag through a virtualized list doesn't drift with the list's estimate of the rows it hasn't drawn.
function rangeOf(current, box) {
	const layout = layoutOf(current);
	if (!layout) {
		return null;
	}

	const { count, columns, anchor, pitchY, pitchX } = layout;
	const here = anchor.line + (box.pointerY - anchor.rect.top) / pitchY;
	current.startLine ??= anchor.line + (current.start.y + originOf(current.scroller).y - anchor.rect.top) / pitchY;
	const top = Math.min(current.startLine, here);
	const bottom = Math.max(Math.max(current.startLine, here), top + 1 / pitchY);
	const height = anchor.rect.height / pitchY;

	const lastLine = Math.floor((count - 1) / columns);
	const firstLine = Math.max(0, Math.floor(top - height) + 1);
	const finalLine = Math.min(lastLine, Math.ceil(bottom) - 1);
	if (firstLine > finalLine) {
		return null;
	}

	let firstColumn = -1;
	let lastColumn = -1;
	for (let column = 0; column < columns; column++) {
		const left = anchor.rect.left + (column - anchor.column) * pitchX;
		const rect = { left: Math.min(left, left + anchor.rect.width), right: Math.max(left, left + anchor.rect.width) };
		if (rect.left < box.right && rect.right > box.left) {
			firstColumn = firstColumn < 0 ? column : firstColumn;
			lastColumn = column;
		}
	}

	if (firstColumn < 0 || firstLine * columns + firstColumn >= count) {
		return null;
	}

	return [firstLine, finalLine, firstColumn, lastColumn, columns, count];
}
