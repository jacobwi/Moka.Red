// Fits a row of things into the width it has. What doesn't fit drops out whole, through data-moka-dropped, which the
// row's stylesheet turns into display: none, so it leaves the tab order and the accessibility tree. It runs inside
// observer callbacks, which come before paint, so nothing flashes.
const dropped = "data-moka-dropped";
const fitted = "data-moka-fitted";
const hasMore = "data-moka-has-more";
const expanded = "data-moka-expanded";
const priority = "data-moka-priority";
const moreButton = "[data-moka-more]";
const separator = '[role="separator"]';

// How much free room an item needs before it comes back. Without it, text that changes length near the limit
// (live rates, a clock) would make the item next to it blink in and out.
const roomToReturn = 16;

const hosts = new WeakMap();

/**
 * Starts fitting a row, or measures it again if it is already fitted.
 * @param {HTMLElement} host The row. It gets data-moka-fitted once measured and data-moka-has-more while something dropped.
 * @param {object|null} reporter A DotNetObjectReference whose OnDroppedChanged hears when what dropped changes, or null.
 * @param {object} layout What differs between rows.
 * @param {string[]} layout.sides Selectors from the host for the elements whose direct children can drop.
 * @param {string} [layout.more] Selector from the host for the button that shows while something dropped.
 * @param {string} [layout.keep] Children matching this never drop.
 * @param {string} [layout.filler] Children matching this only take up room: never dropped, never counted.
 * @param {string[]} [layout.watch] Attributes on the children whose change means measuring again.
 * @param {boolean} [layout.positions] Report where the dropped children are instead of how many there are.
 * @param {boolean} [layout.flexible] Children can grow or shrink to fill the row, so free room can't be read from where the last one ends.
 */
export function bindFit(host, reporter, layout) {
	if (!host) {
		return;
	}

	const existing = hosts.get(host);
	if (existing) {
		existing.reporter = reporter ?? null;
		existing.reported = nothingReported(existing.layout);
		fit(host, existing);
		return;
	}

	const bound = { layout, reporter: reporter ?? null, reported: nothingReported(layout), width: null, kept: null };
	const refit = () => fit(host, bound);

	// Only the width decides what fits, and a row can change height often when it holds a panel.
	bound.resize = new ResizeObserver(entries => {
		const { width } = entries[entries.length - 1].contentRect;
		if (width !== bound.width) {
			bound.width = width;
			refit();
		}
	});
	bound.resize.observe(host);

	// A new text size or density changes what fits without touching the row or its text.
	const themed = host.closest(".moka-root");
	if (themed) {
		bound.theme = new MutationObserver(refit);
		bound.theme.observe(themed, { attributes: true, attributeFilter: ["style", "class", "data-moka-theme"] });
	}

	bound.changes = new MutationObserver(refit);
	bound.changes.observe(host, { attributes: true, attributeFilter: [expanded] });
	const contents = {
		childList: true,
		subtree: true,
		characterData: true,
		attributes: true,
		attributeFilter: [priority, ...(layout.watch ?? [])]
	};
	sidesOf(host, layout).forEach(side => side && bound.changes.observe(side, contents));
	const button = layout.more && host.querySelector(layout.more);
	if (button) {
		bound.changes.observe(button, contents);
	}

	hosts.set(host, bound);

	document.fonts?.ready.then(() => {
		if (hosts.has(host)) {
			refit();
		}
	});
}

/**
 * Stops fitting a row and shows everything again.
 * @param {HTMLElement} host The row.
 */
export function unbindFit(host) {
	const bound = host && hosts.get(host);
	if (!bound) {
		return;
	}

	bound.resize.disconnect();
	bound.changes.disconnect();
	bound.theme?.disconnect();
	hosts.delete(host);

	droppedIn(sidesOf(host, bound.layout).filter(Boolean)).forEach(element => element.removeAttribute(dropped));
	host.removeAttribute(fitted);
	host.removeAttribute(hasMore);
}

function fit(host, bound) {
	const { layout } = bound;
	const sides = sidesOf(host, layout);
	if (sides.includes(null)) {
		return;
	}

	const wasDropped = droppedIn(sides);
	wasDropped.forEach(element => element.removeAttribute(dropped));
	host.removeAttribute(hasMore);

	// Measured on one row: only then does an item that may shrink give way before anything is dropped.
	host.setAttribute(fitted, "");

	// When a different item is the one that has to stay, what dropped last time says nothing about what has room now.
	const kept = layout.keep ? sides[0].querySelector(`:scope > :is(${layout.keep})`) : null;
	const returning = kept === bound.kept ? wasDropped.filter(element => !element.matches(separator)) : [];
	bound.kept = kept;

	const button = layout.more ? host.querySelector(layout.more) : null;
	let queue = null;
	while (overflows(sides, returning, layout)) {
		// The button takes room of its own, so it is showing before the first item is chosen.
		if (button && !host.hasAttribute(hasMore)) {
			host.setAttribute(hasMore, "");
			continue;
		}

		queue ??= dropOrder(sides, layout);
		const next = queue.shift();
		if (!next) {
			// Nothing left to drop, such as bare text too long for the row. The stylesheet's fallback applies.
			host.removeAttribute(fitted);
			break;
		}

		next.setAttribute(dropped, "");
		tidySeparators(next.parentElement, layout);
	}

	const count = droppedIn(sides).filter(element => !element.matches(separator)).length;
	if (count === 0) {
		host.removeAttribute(hasMore);
	}

	report(bound, sides, count);

	// Opened, everything shows and the row grows. It is still measured first, so the count stays true and the button
	// can go once the window is wide enough not to need it.
	if (count > 0 && host.hasAttribute(expanded)) {
		droppedIn(sides).forEach(element => element.removeAttribute(dropped));
		host.removeAttribute(fitted);
	}
}

function sidesOf(host, layout) {
	return layout.sides.map(selector => host.querySelector(selector));
}

function droppedIn(sides) {
	return sides.flatMap(side => [...side.children].filter(element => element.hasAttribute(dropped)));
}

function nothingReported(layout) {
	return layout.positions ? "" : "0";
}

function report(bound, sides, count) {
	if (!bound.reporter) {
		return;
	}

	const value = bound.layout.positions ? positionsOf(sides) : count;
	const key = String(value);
	if (bound.reported === key) {
		return;
	}

	bound.reported = key;
	bound.reporter.invokeMethodAsync("OnDroppedChanged", value).catch(() => { });
}

function positionsOf(sides) {
	return sides
		.flatMap(side => [...side.children])
		.flatMap((element, position) => element.hasAttribute(dropped) && !element.matches(separator) ? [position] : []);
}

function spills(side) {
	return side.scrollWidth > side.clientWidth + 1;
}

function overflows(sides, returning, layout) {
	if (sides.some(spills)) {
		return true;
	}

	if (!returning.some(element => !element.hasAttribute(dropped))) {
		return false;
	}

	return layout.flexible ? spillsWithLessRoom(sides[0]) : freeRoom(sides[0]) < roomToReturn;
}

// A spacer or a stretched search box fills the row whatever is in it, so there is no gap after the last item to
// measure. The room is taken away for a moment instead, to see whether everything still fits.
function spillsWithLessRoom(side) {
	side.style.marginInlineEnd = `${roomToReturn}px`;
	const spilled = spills(side);
	side.style.marginInlineEnd = "";
	return spilled;
}

function freeRoom(side) {
	const shown = [...side.children].filter(element => !element.hasAttribute(dropped));
	const edge = shown.length > 0 ? shown[shown.length - 1].getBoundingClientRect().right : side.getBoundingClientRect().left;
	return side.getBoundingClientRect().right - edge;
}

function matches(element, selector) {
	return !!selector && element.matches(selector);
}

function canDrop(element, layout) {
	return !element.matches(separator)
		&& !element.matches(moreButton)
		&& !matches(element, layout.keep)
		&& !matches(element, layout.filler);
}

function dropOrder(sides, layout) {
	const focused = document.activeElement;
	return sides
		.flatMap((side, sideIndex) => [...side.children]
			.map((element, position) => ({
				element,
				sideIndex,
				position,
				holdsFocus: element.contains(focused),
				rank: rankOf(element)
			}))
			.filter(entry => canDrop(entry.element, layout)))
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
function tidySeparators(side, layout) {
	let afterItem = false;
	let trailing = null;
	for (const element of side.children) {
		if (element.matches(separator)) {
			element.toggleAttribute(dropped, !afterItem);
			trailing = afterItem ? element : trailing;
			afterItem = false;
		} else if (!element.matches(moreButton) && !matches(element, layout.filler) && !element.hasAttribute(dropped)) {
			afterItem = true;
			trailing = null;
		}
	}

	trailing?.setAttribute(dropped, "");
}
