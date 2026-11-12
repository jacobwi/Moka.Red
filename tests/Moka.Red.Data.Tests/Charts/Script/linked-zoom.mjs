// Drives moka-chart.js's linked zoom over a small stand-in for the DOM and prints, as JSON, what each chart told .NET.
// Run by LinkedZoomScriptTests: node linked-zoom.mjs <path to moka-chart.js>

import { pathToFileURL } from 'node:url';

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

class FakeElement extends FakeNode {
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

class FakeSvg extends FakeElement {
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

const chart = await import(pathToFileURL(process.argv[2]).href);

// A chart's root, its plot box with the marks and the surface, the surface's overlay and keyboard grid, and a .NET
// reference that records what it is told.
function makeChart(name, link, window, extent, calls) {
	const root = new FakeElement('moka-chart');
	const box = root.appendChild(new FakeElement('moka-chart-plot__box'));
	const marks = box.appendChild(new FakeSvg('moka-linechart__marks'));
	const surface = box.appendChild(new FakeElement('moka-chart-surface', {
		'data-moka-version': '1',
		'data-moka-panel': '0',
		'data-moka-count': '4',
		'data-moka-x0': String(extent[0]),
		'data-moka-x1': String(extent[1]),
		'data-moka-w0': String(window[0]),
		'data-moka-w1': String(window[1])
	}));
	surface.appendChild(new FakeElement('moka-chart-overlay'));
	const grid = surface.appendChild(new FakeElement('moka-chart-surface__grid'));
	calls[name] = [];
	const ref = {
		invokeMethodAsync(method, ...args) {
			calls[name].push([method, ...args]);
			return Promise.resolve(null);
		}
	};
	chart.bind(root, ref, { mode: 'none', keys: true, sync: 'group', link, zoom: 2 });
	return { root, grid, marks };
}

const wait = ms => new Promise(resolve => setTimeout(resolve, ms));
const results = {};

// A key zoom on one chart: it and the chart linked to it each hear the range once; a chart with another kind of x
// axis, and one that doesn't link, hear nothing.
{
	const calls = {};
	const a = makeChart('a', 'time', [1000, 2000], [1000, 2000], calls);
	const b = makeChart('b', 'time', [1000, 2000], [500, 2500], calls);
	const c = makeChart('c', 'category:4:0123', [0, 3], [0, 3], calls);
	const d = makeChart('d', null, [1000, 2000], [1000, 2000], calls);
	a.root.dispatch('keydown', { target: a.grid, key: '+', preventDefault() {}, stopPropagation() {} });
	const stretched = !!b.marks.style.transform && !c.marks.style.transform && !d.marks.style.transform;
	await wait(250);
	results.key = { calls, stretched };
	for (const one of [a, b, c, d]) chart.unbind(one.root);
}

// A zoom chosen on the .NET side: the chart that chose it hears nothing back, the linked one hears it once.
{
	const calls = {};
	const a = makeChart('a', 'number', [0, 100], [0, 100], calls);
	makeChart('b', 'number', [0, 100], [0, 100], calls);
	chart.shareZoom(a.root, 20, 40);
	await wait(50);
	const zoomed = JSON.parse(JSON.stringify(calls));
	chart.shareZoom(a.root, null, null);
	await wait(50);
	results.share = { zoomed, reset: calls };
}

process.stdout.write(JSON.stringify(results));
process.exit(0);
