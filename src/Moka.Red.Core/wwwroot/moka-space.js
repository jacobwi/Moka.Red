// Something stuck to an edge of the window says how much of that edge it covers, as a custom property on the root
// element. moka.css turns the properties into the page's scroll padding, so a link to a part of the page doesn't land
// under them, and other sticky parts can keep clear of them too.
const watched = new Map();

// offsetHeight rather than a bounding box, which an entrance animation would move. A floating element repeats the gap
// under it over it.
function spaceOf(element, floating) {
	if (!element.isConnected) return 0;
	const height = element.offsetHeight;
	if (height === 0) return 0;
	if (!floating) return height;
	const gap = parseFloat(getComputedStyle(element.parentElement).bottom) || 0;
	return height + gap * 2;
}

// The browser jumps to a page's #part as the page loads, before any bar has said how much it covers, so the part can
// end up under a bar at the top. When that bar first reports, a part still under it is brought back into view.
function revealHashTarget(space) {
	let id;
	try {
		id = decodeURIComponent(location.hash.slice(1));
	} catch {
		return;
	}

	const target = id && (document.getElementById(id) ?? document.getElementsByName(id)[0]);
	if (!target) return;

	// Near the top rather than at it: the page can shift a pixel or two between the jump and now.
	const top = target.getBoundingClientRect().top;
	if (Math.abs(top) < space) target.scrollIntoView();
}

function publish(property) {
	let space = 0;
	let atTop = false;
	for (const [element, entry] of watched) {
		if (entry.property !== property) continue;
		space = Math.max(space, spaceOf(element, entry.floating));
		atTop ||= !entry.floating;
	}

	const root = document.documentElement.style;
	const first = !root.getPropertyValue(property);
	if (space > 0) {
		root.setProperty(property, `${Math.ceil(space)}px`);
		if (first && atTop) revealHashTarget(space);
	} else {
		root.removeProperty(property);
	}
}

export function watchSpace(element, property, floating) {
	if (!element || !property) return null;

	const observer = new ResizeObserver(() => publish(property));
	watched.set(element, { property, floating: !!floating });
	observer.observe(element);
	publish(property);

	return {
		dispose: () => {
			observer.disconnect();
			watched.delete(element);
			publish(property);
		}
	};
}
