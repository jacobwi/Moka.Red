// A small stand-in for the DOM, enough for moka-chart.js to bind charts and run their handlers in Node.js. Importing it
// installs the globals the script reads.

class FakeNode {
	constructor() {
		this.parentElement = null;
		this.children = [];
	}

	get isConnected() {
		return true;
	}

	contains(other) {
		for (let node = other; node; node = node.parentElement) if (node === this) return true;
		return false;
	}
}

class FakeStyle {
	removeProperty(name) {
		delete this[name.replace(/-([a-z])/g, (_, c) => c.toUpperCase())];
	}
}

export class FakeElement extends FakeNode {
	constructor(classes = '', attributes = {}) {
		super();
		this.attributes = new Map(Object.entries(attributes));
		if (classes) this.attributes.set('class', classes);
		this.style = new FakeStyle();
		this.listeners = new Map();
	}

	get className() {
		return this.attributes.get('class') ?? '';
	}

	set className(value) {
		this.attributes.set('class', value);
	}

	get classList() {
		const element = this;
		return { contains: name => element.className.split(/\s+/).includes(name) };
	}

	get dataset() {
		const out = {};
		for (const [name, value] of this.attributes) {
			if (!name.startsWith('data-')) continue;
			out[name.slice(5).replace(/-([a-z0-9])/g, (_, c) => c.toUpperCase())] = value;
		}
		return out;
	}

	getAttribute(name) {
		return this.attributes.has(name) ? this.attributes.get(name) : null;
	}

	hasAttribute(name) {
		return this.attributes.has(name);
	}

	setAttribute(name, value) {
		this.attributes.set(name, String(value));
	}

	removeAttribute(name) {
		this.attributes.delete(name);
	}

	toggleAttribute(name, force) {
		const on = force ?? !this.attributes.has(name);
		if (on) this.attributes.set(name, '');
		else this.attributes.delete(name);
		return on;
	}

	appendChild(child) {
		child.parentElement = this;
		this.children.push(child);
		return child;
	}

	addEventListener(name, handler) {
		if (!this.listeners.has(name)) this.listeners.set(name, []);
		this.listeners.get(name).push(handler);
	}

	removeEventListener(name, handler) {
		this.listeners.set(name, (this.listeners.get(name) || []).filter(h => h !== handler));
	}

	dispatch(name, event) {
		for (let node = event.target; node; node = node.parentElement) {
			for (const handler of node.listeners?.get(name) || []) handler(event);
		}
	}

	matches(selector) {
		return selector.split(',').some(one => {
			const part = one.trim();
			if (part.startsWith('.')) return this.classList.contains(part.slice(1));
			const attribute = /^\[([a-z-]+)\]$/.exec(part);
			return attribute ? this.hasAttribute(attribute[1]) : false;
		});
	}

	closest(selector) {
		for (let node = this; node; node = node.parentElement) if (node.matches?.(selector)) return node;
		return null;
	}

	querySelectorAll(selector) {
		const found = [];
		const walk = node => {
			for (const child of node.children) {
				if (child.matches(selector)) found.push(child);
				walk(child);
			}
		};
		walk(this);
		return found;
	}

	querySelector(selector) {
		const scoped = /^:scope > (.+)$/.exec(selector);
		if (scoped) return this.children.find(child => child.matches(scoped[1])) ?? null;
		return this.querySelectorAll(selector)[0] ?? null;
	}

	getBoundingClientRect() {
		return { left: 0, top: 0, right: 600, bottom: 200, width: 600, height: 200 };
	}

	focus() {
	}
}

export class FakeSvg extends FakeElement {
}

globalThis.Node = FakeNode;
globalThis.Element = FakeElement;
globalThis.HTMLElement = FakeElement;
globalThis.SVGElement = FakeSvg;
globalThis.MutationObserver = class {
	observe() {
	}

	disconnect() {
	}
};
globalThis.ResizeObserver = class {
	observe() {
	}

	unobserve() {
	}
};
globalThis.requestAnimationFrame = callback => setTimeout(callback, 0);
globalThis.getComputedStyle = () => ({ getPropertyValue: () => '', overflowY: 'visible', position: 'static' });
globalThis.window = new FakeElement();
globalThis.document = new FakeElement();
document.documentElement = { clientWidth: 1280, clientHeight: 800 };
