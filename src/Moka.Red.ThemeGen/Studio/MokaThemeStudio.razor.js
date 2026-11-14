// What changed in the studio's previews. The studio calls snapshot just before it applies a change and compare once the
// change has drawn; nothing listens or runs in between. compare marks the elements whose look changed with
// data-moka-changed, which the studio's stylesheet turns into a short outline, and takes the marks off after MARK_MS.

// The properties a theme sets. Sizes that follow from them, such as a box's width, are left out, or one change of
// padding would mark everything around it.
const PROPERTIES = [
	'color', 'background-color', 'background-image',
	'border-top-color', 'border-right-color', 'border-bottom-color', 'border-left-color',
	'border-top-width', 'border-right-width', 'border-bottom-width', 'border-left-width',
	'border-top-left-radius', 'border-top-right-radius', 'border-bottom-right-radius', 'border-bottom-left-radius',
	'box-shadow', 'padding-top', 'padding-right', 'padding-bottom', 'padding-left', 'min-height', 'row-gap', 'column-gap',
	'font-family', 'font-size', 'font-weight', 'letter-spacing', 'text-transform', 'fill', 'stroke', 'opacity', 'display'
];

const COLOR = PROPERTIES.indexOf('color');
const DISPLAY = PROPERTIES.indexOf('display');

// An inherited value that only came down from the parent marks the parent, not every element under it.
const INHERITED = PROPERTIES.map(name =>
	['color', 'font-family', 'font-size', 'font-weight', 'letter-spacing', 'text-transform', 'fill', 'stroke'].includes(name));

// Borders, fills and strokes left at currentColor follow the element's own color, so they change only when it does.
const FOLLOWS_COLOR = PROPERTIES.map(name => name.endsWith('-color') && name !== 'color' && name !== 'background-color'
	|| name === 'fill' || name === 'stroke');

// A border's color can't be seen while the border has no width.
const WIDTH_OF = PROPERTIES.map(name => name.startsWith('border-') && name.endsWith('-color')
	? PROPERTIES.indexOf(name.replace('-color', '-width'))
	: -1);

const MARK_MS = 1600;

const held = new WeakMap();
const marks = new WeakMap();

export function snapshot(root) {
	settle(root);
	held.set(root, read(root));
}

// The number of elements marked since the snapshot, or -1 when there was none to compare with. A part drawn anew, such
// as a chart that takes another path for a look, is marked once at its top. One that went out of sight marks the
// nearest element still drawn.
export function compare(root) {
	const before = held.get(root);
	if (!before) {
		return -1;
	}

	held.delete(root);
	settle(root);
	const after = read(root);
	const changed = new Set();
	for (const [element, now] of after) {
		const then = before.get(element);
		if (!then) {
			if (before.has(element.parentElement) && shown(element)) {
				changed.add(element);
			}

			continue;
		}

		if (!looksDifferent(element, then, now, before, after)) {
			continue;
		}

		if (shown(element)) {
			changed.add(element);
		} else if (then[DISPLAY] !== now[DISPLAY]) {
			const drawn = nearestShown(element.parentElement, root);
			if (drawn) {
				changed.add(drawn);
			}
		}
	}

	const marked = [...changed];
	mark(root, marked);
	return marked.length;
}

function nearestShown(element, root) {
	for (let current = element; current && current !== root; current = current.parentElement) {
		if (shown(current)) {
			return current;
		}
	}

	return null;
}

// A theme change starts transitions, and a transition reads as its starting value until it has run. Finishing them
// reads where every element ends up. getAnimations brings styles up to date first, so the transitions the change just
// started are among them.
function settle(root) {
	if (typeof root.getAnimations !== 'function' || typeof CSSTransition === 'undefined') {
		return;
	}

	for (const animation of root.getAnimations({ subtree: true })) {
		if (animation instanceof CSSTransition) {
			animation.finish();
		}
	}
}

function read(root) {
	const values = new Map();
	for (const element of root.querySelectorAll('*')) {
		const style = getComputedStyle(element);
		const row = new Array(PROPERTIES.length);
		for (let i = 0; i < PROPERTIES.length; i++) {
			row[i] = style.getPropertyValue(PROPERTIES[i]);
		}

		values.set(element, row);
	}

	return values;
}

function looksDifferent(element, then, now, before, after) {
	const parent = element.parentElement;
	const parentThen = parent ? before.get(parent) : undefined;
	const parentNow = parent ? after.get(parent) : undefined;
	for (let i = 0; i < PROPERTIES.length; i++) {
		if (then[i] === now[i]) {
			continue;
		}

		if (INHERITED[i] && parentThen && parentNow && then[i] === parentThen[i] && now[i] === parentNow[i]) {
			continue;
		}

		if (FOLLOWS_COLOR[i] && then[i] === then[COLOR] && now[i] === now[COLOR]) {
			continue;
		}

		const width = WIDTH_OF[i];
		if (width >= 0 && then[width] === '0px' && now[width] === '0px') {
			continue;
		}

		return true;
	}

	return false;
}

// Elements not drawn, such as a closed tooltip, change with the rest but aren't counted.
function shown(element) {
	return typeof element.checkVisibility !== 'function' || element.checkVisibility();
}

// The marks alternate between two values, so an element marked again by the next change starts its outline over.
function mark(root, elements) {
	const state = marks.get(root) ?? { round: 0, timer: 0, elements: [] };
	clearTimeout(state.timer);
	for (const element of state.elements) {
		element.removeAttribute('data-moka-changed');
	}

	state.round++;
	const value = state.round % 2 === 1 ? 'a' : 'b';
	root.style.setProperty('--_mark-time', `${MARK_MS}ms`);
	for (const element of elements) {
		element.setAttribute('data-moka-changed', value);
	}

	state.elements = elements;
	state.timer = elements.length === 0 ? 0 : setTimeout(() => {
		for (const element of elements) {
			element.removeAttribute('data-moka-changed');
		}

		state.elements = [];
	}, MARK_MS);
	marks.set(root, state);
}
