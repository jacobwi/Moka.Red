const dropped = "data-moka-dropped";
const fitted = "data-moka-fitted";
const hasMore = "data-moka-has-more";
const expanded = "data-moka-expanded";
const priority = "data-moka-priority";
const more = "[data-moka-more]";
const separator = '[role="separator"]';

// How much free room an item needs before it comes back. Without it, text that changes length near the limit
// (live rates, a clock) would make the item next to it blink in and out.
const roomToReturn = 16;

const bars = new WeakMap();

export function bindStatusBar(bar, reporter) {
	if (!bar) {
		return;
	}

	const existing = bars.get(bar);
	if (existing) {
		existing.reporter = reporter ?? null;
		existing.reported = 0;
		fitBar(bar);
		return;
	}

	const fit = () => fitBar(bar);
	const resize = new ResizeObserver(fit);
	const changes = new MutationObserver(fit);
	resize.observe(bar);
	changes.observe(bar, {
		childList: true,
		subtree: true,
		characterData: true,
		attributes: true,
		attributeFilter: [priority, expanded]
	});
	bars.set(bar, { resize, changes, reporter: reporter ?? null, reported: 0 });

	document.fonts?.ready.then(() => {
		if (bars.has(bar)) {
			fit();
		}
	});
}

export function unbindStatusBar(bar) {
	const bound = bar && bars.get(bar);
	if (!bound) {
		return;
	}

	bound.resize.disconnect();
	bound.changes.disconnect();
	bars.delete(bar);
}

function fitBar(bar) {
	const start = bar.querySelector(":scope > .moka-statusbar-start");
	const end = bar.querySelector(":scope > .moka-statusbar-end");
	if (!start || !end) {
		return;
	}

	const wasDropped = droppedIn(bar);
	wasDropped.forEach(element => element.removeAttribute(dropped));
	bar.removeAttribute(hasMore);

	// Measured on one row: only then does an item that may shrink give way before anything is dropped.
	bar.setAttribute(fitted, "");

	const button = end.querySelector(`:scope > ${more}`);
	const returning = wasDropped.filter(element => !element.matches(separator));
	let queue = null;
	while (overflows(start, end, returning)) {
		// The button takes room of its own, so it is showing before the first item is chosen.
		if (button && !bar.hasAttribute(hasMore)) {
			bar.setAttribute(hasMore, "");
			continue;
		}

		queue ??= dropOrder(start, end);
		const next = queue.shift();
		if (!next) {
			// Nothing left to drop, such as bare text too long for the bar. The stylesheet's clip keeps the sides apart.
			bar.removeAttribute(fitted);
			break;
		}

		next.setAttribute(dropped, "");
		tidySeparators(next.parentElement);
	}

	const count = droppedIn(bar).filter(element => !element.matches(separator)).length;
	if (count === 0) {
		bar.removeAttribute(hasMore);
	}

	report(bar, count);

	// Opened, everything shows and the bar grows. It is still measured first, so the count stays true and the button
	// can go once the window is wide enough not to need it.
	if (count > 0 && bar.hasAttribute(expanded)) {
		droppedIn(bar).forEach(element => element.removeAttribute(dropped));
		bar.removeAttribute(fitted);
	}
}

function droppedIn(bar) {
	return [...bar.querySelectorAll(`:scope > * > [${dropped}]`)];
}

function report(bar, count) {
	const bound = bars.get(bar);
	if (!bound?.reporter || bound.reported === count) {
		return;
	}

	bound.reported = count;
	bound.reporter.invokeMethodAsync("OnDroppedChanged", count).catch(() => { });
}

function overflows(start, end, returning) {
	if (start.scrollWidth > start.clientWidth + 1 || end.scrollWidth > end.clientWidth + 1) {
		return true;
	}

	return returning.some(element => !element.hasAttribute(dropped)) && freeRoom(start) < roomToReturn;
}

function freeRoom(start) {
	const shown = [...start.children].filter(element => !element.hasAttribute(dropped));
	const edge = shown.length > 0 ? shown[shown.length - 1].getBoundingClientRect().right : start.getBoundingClientRect().left;
	return start.getBoundingClientRect().right - edge;
}

function dropOrder(start, end) {
	const focused = document.activeElement;
	return [start, end]
		.flatMap((side, sideIndex) => [...side.children]
			.filter(element => !element.matches(separator) && !element.matches(more))
			.map((element, position) => ({
				element,
				sideIndex,
				position,
				holdsFocus: element.contains(focused),
				rank: rankOf(element)
			})))
		.sort((a, b) =>
			a.holdsFocus - b.holdsFocus
			|| a.rank - b.rank
			|| a.sideIndex - b.sideIndex
			|| b.position - a.position)
		.map(entry => entry.element);
}

// An item with a tooltip is the tooltip's element here, with the priority on the item inside it.
function rankOf(element) {
	const holder = element.matches(`[${priority}]`) ? element : element.querySelector(`[${priority}]`);
	return Number(holder?.getAttribute(priority)) || 0;
}

// A separator only stands between two items that are still showing.
function tidySeparators(side) {
	let afterItem = false;
	let trailing = null;
	for (const element of side.children) {
		if (element.matches(more)) {
			continue;
		}

		if (element.matches(separator)) {
			element.toggleAttribute(dropped, !afterItem);
			trailing = afterItem ? element : trailing;
			afterItem = false;
		} else if (!element.hasAttribute(dropped)) {
			afterItem = true;
			trailing = null;
		}
	}

	trailing?.setAttribute(dropped, "");
}
