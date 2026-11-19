// Drags cards on a board through MokaBentoGrid.razor.js over a small stand-in for the DOM, and prints as JSON where the
// bar showed and what the drop told .NET. Run by MokaBentoBoardScriptTests: node bento-board.mjs <path to the script>
//
// Each card has a box, as a browser would have laid it out, and in rows that flow a flex-grow, which the stylesheet
// makes the card's share of the columns.

import { pathToFileURL } from 'node:url';

const BAR = 4;

function parse(selector) {
	return selector.split(',').map(alternative => {
		const parts = [];
		let combinator = ' ';
		for (const token of alternative.trim().replace(/\s*>\s*/g, ' > ').split(/\s+/)) {
			if (token === '>') {
				combinator = '>';
				continue;
			}
			parts.push({ combinator, simple: compound(token) });
			combinator = ' ';
		}
		return parts;
	});
}

function compound(text) {
	const simple = [];
	const pattern = /:scope|\.[\w-]+|\[([\w-]+)(?:(\*?=)"([^"]*)")?\]|[\w-]+/g;
	for (let match = pattern.exec(text); match; match = pattern.exec(text)) {
		const token = match[0];
		if (token === ':scope') simple.push({ scope: true });
		else if (token[0] === '.') simple.push({ cls: token.slice(1) });
		else if (token[0] === '[') simple.push({ attr: match[1], op: match[2], value: match[3] });
		else simple.push({ tag: token });
	}
	return simple;
}

function matchesSimple(element, s, scope) {
	if (s.scope) return element === scope;
	if (s.cls) return element.classList.contains(s.cls);
	if (s.attr) {
		if (!element.attributes.has(s.attr)) return false;
		const value = element.attributes.get(s.attr);
		return s.op === undefined || (s.op === '=' ? value === s.value : value.includes(s.value));
	}
	return element.tag === s.tag;
}

function matchesParts(element, parts, index, scope) {
	if (!parts[index].simple.every(s => matchesSimple(element, s, scope))) return false;
	if (index === 0) return true;
	if (parts[index].combinator === '>') return !!element.parent && matchesParts(element.parent, parts, index - 1, scope);
	for (let up = element.parent; up; up = up.parent) {
		if (matchesParts(up, parts, index - 1, scope)) return true;
	}
	return false;
}

class FakeElement {
	constructor(tag, attributes = {}) {
		this.tag = tag;
		this.attributes = new Map(Object.entries(attributes));
		this.children = [];
		this.parent = null;
		this.listeners = new Map();
		this.style = { removeProperty: name => delete this.style[name] };
		this.box = { left: 0, top: 0, width: 0, height: 0 };
		this.computed = {};
		this.inert = false;
	}

	get classList() {
		const classes = () => (this.attributes.get('class') ?? '').split(/\s+/).filter(Boolean);
		return { contains: name => classes().includes(name) };
	}

	get parentElement() {
		return this.parent;
	}

	get isConnected() {
		let element = this;
		while (element.parent) element = element.parent;
		return element === document.documentElement;
	}

	get dataset() {
		const out = {};
		for (const [name, value] of this.attributes) {
			if (name.startsWith('data-')) out[name.slice(5).replace(/-([a-z])/g, (_, c) => c.toUpperCase())] = value;
		}
		return out;
	}

	get clientWidth() {
		return this.box.width;
	}

	append(...children) {
		for (const child of children) {
			child.parent = this;
			this.children.push(child);
		}
	}

	remove() {
		if (!this.parent) return;
		this.parent.children = this.parent.children.filter(child => child !== this);
		this.parent = null;
	}

	setAttribute(name, value) {
		this.attributes.set(name, String(value));
	}

	removeAttribute(name) {
		this.attributes.delete(name);
	}

	hasAttribute(name) {
		return this.attributes.has(name);
	}

	matches(selector, scope = null) {
		return parse(selector).some(parts => matchesParts(this, parts, parts.length - 1, scope));
	}

	closest(selector) {
		for (let element = this; element; element = element.parent) {
			if (element.matches(selector)) return element;
		}
		return null;
	}

	* descendants() {
		for (const child of this.children) {
			yield child;
			yield* child.descendants();
		}
	}

	querySelectorAll(selector) {
		return [...this.descendants()].filter(element => element.matches(selector, this));
	}

	querySelector(selector) {
		return this.querySelectorAll(selector)[0] ?? null;
	}

	cloneNode() {
		const copy = new FakeElement(this.tag, Object.fromEntries(this.attributes));
		copy.box = { ...this.box };
		copy.computed = { ...this.computed };
		for (const child of this.children) copy.append(child.cloneNode(true));
		return copy;
	}

	addEventListener(type, listener) {
		if (!this.listeners.has(type)) this.listeners.set(type, []);
		this.listeners.get(type).push(listener);
	}

	removeEventListener(type, listener) {
		this.listeners.set(type, (this.listeners.get(type) ?? []).filter(l => l !== listener));
	}

	dispatch(type, event) {
		for (const listener of [...(this.listeners.get(type) ?? [])]) listener(event);
	}

	setPointerCapture() {
	}

	releasePointerCapture() {
	}

	getBoundingClientRect() {
		const { left, top, width, height } = this.box;
		return { left, top, width, height, right: left + width, bottom: top + height };
	}
}

const page = new FakeElement('html');
const body = new FakeElement('body');
page.append(body);
const windowListeners = new FakeElement('window');

globalThis.document = {
	documentElement: page,
	body,
	scrollingElement: page,
	createElement: tag => new FakeElement(tag),
	addEventListener: (type, listener) => windowListeners.addEventListener(`document:${type}`, listener),
	removeEventListener: (type, listener) => windowListeners.removeEventListener(`document:${type}`, listener),
	// The board only: the copy that follows the pointer lets it through.
	elementFromPoint(x, y) {
		const hits = [...body.children[0].descendants()].filter(element => {
			const r = element.getBoundingClientRect();
			return r.width > 0 && x >= r.left && x < r.right && y >= r.top && y < r.bottom;
		});
		return hits.at(-1) ?? null;
	},
};
globalThis.window = {
	innerHeight: 2000,
	addEventListener: (type, listener) => windowListeners.addEventListener(type, listener),
	removeEventListener: (type, listener) => windowListeners.removeEventListener(type, listener),
};
globalThis.getComputedStyle = element => ({
	columnGap: '20px',
	rowGap: '20px',
	overflowY: 'visible',
	flexGrow: '0',
	...element.computed,
	getPropertyValue: name => element.computed[name] ?? '',
});
globalThis.requestAnimationFrame = () => 1;
globalThis.cancelAnimationFrame = () => { };

const { bindBoard } = await import(pathToFileURL(process.argv[2]).href);

// A board of cards, each [id, left, top, width, height, share], in the board's order.
function board(rows, cards) {
	const root = new FakeElement('div', { class: `moka-bento-grid moka-bento-grid--board${rows ? ' moka-bento-grid--rows' : ''}` });
	const list = new FakeElement('div', { class: 'moka-bento-grid__cards' });
	list.box = { left: 0, top: 0, width: 1000, height: 1000 };
	list.computed = { '--_board-columns': '4' };
	root.append(list);
	for (const [id, left, top, width, height, share] of cards) {
		const slot = new FakeElement('div', { class: 'moka-bento-grid__slot' });
		const card = new FakeElement('div', { class: 'moka-bento-item', 'data-card': id });
		card.box = { left, top, width, height };
		if (rows) card.computed = { flexGrow: String(share) };
		const grip = new FakeElement('button', { 'data-tool': 'grip' });
		grip.box = { left: left + 4, top: top + 4, width: 20, height: 20 };
		card.append(grip);
		slot.append(card);
		list.append(slot);
	}
	for (const child of [...body.children]) child.remove();
	body.append(root);
	const calls = [];
	bindBoard(root, { invokeMethodAsync: (...args) => { calls.push(args); return Promise.resolve(); } });
	return { root, calls };
}

// Picks the card up by its grip, moves over (fx, fy) of the target's box, reads the bar, then drops or presses Escape.
function drag({ root, calls }, id, target, fx, fy, drop = true) {
	const card = root.querySelector(`[data-card="${id}"]`);
	const grip = card.querySelector('[data-tool="grip"]');
	const g = grip.getBoundingClientRect();
	root.dispatch('pointerdown', { target: grip, button: 0, pointerId: 1, clientX: g.left + 2, clientY: g.top + 2, preventDefault() { } });
	const t = root.querySelector(`[data-card="${target}"]`).getBoundingClientRect();
	const x = t.left + t.width * fx;
	const y = t.top + t.height * fy;
	windowListeners.dispatch('pointermove', { pointerId: 1, clientX: x, clientY: y });
	const marker = body.children.at(-1).style;
	const bar = marker.display === 'none' ? { bar: 'none' }
		: parseFloat(marker.width) === BAR
			? { bar: 'vertical', x: parseFloat(marker.left) + BAR / 2, top: parseFloat(marker.top), height: parseFloat(marker.height) }
			: { bar: 'horizontal', y: parseFloat(marker.top) + BAR / 2, left: parseFloat(marker.left), width: parseFloat(marker.width) };
	const before = calls.length;
	if (drop) windowListeners.dispatch('pointerup', { pointerId: 1, clientX: x, clientY: y });
	else windowListeners.dispatch('document:keydown', { key: 'Escape', preventDefault() { }, stopPropagation() { } });
	return { ...bar, dropped: calls.slice(before), leftOver: body.children.length - 1 };
}

// A grid of four 235 px columns with 20 px gaps: two small cards, a full one, and the small one being dragged.
const grid = () => board(false, [
	['a', 0, 0, 235, 100],
	['b', 255, 0, 235, 100],
	['f', 0, 120, 1000, 100],
	['x', 0, 240, 235, 100],
]);

// Rows that flow, as the browser lays them out: [a wide, b small], [f full], [l small, alone since g is full], [g full],
// [x small]. a and b grew into the row's last column in proportion to their shares.
const rows = () => board(true, [
	['a', 0, 0, 653, 100, 2],
	['b', 673, 0, 327, 100, 1],
	['f', 0, 120, 1000, 100, 4],
	['l', 0, 240, 1000, 100, 1],
	['g', 0, 360, 1000, 100, 4],
	['x', 0, 480, 1000, 100, 1],
]);

// A wide card that doesn't fit beside [a wide, b small].
const wideRows = () => board(true, [
	['a', 0, 0, 653, 100, 2],
	['b', 673, 0, 327, 100, 1],
	['w', 0, 120, 1000, 100, 2],
]);

console.log(JSON.stringify({
	grid: {
		besideSmall: drag(grid(), 'x', 'b', 0.25, 0.5),
		aboveFull: drag(grid(), 'x', 'f', 0.75, 0.25),
	},
	rows: {
		afterGrownWide: drag(rows(), 'x', 'a', 0.8, 0.5),
		beforeSmall: drag(rows(), 'x', 'b', 0.25, 0.9),
		afterAlone: drag(rows(), 'x', 'l', 0.2, 0.8),
		beforeAlone: drag(rows(), 'x', 'l', 0.9, 0.2),
		afterFull: drag(rows(), 'x', 'f', 0.5, 0.8),
		wideAfterFullRow: drag(wideRows(), 'w', 'b', 0.75, 0.5),
		cancelled: drag(rows(), 'x', 'a', 0.8, 0.5, false),
	},
}));
