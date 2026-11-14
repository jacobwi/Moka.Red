// Drives MokaDockLayout.razor.js over a small stand-in for the DOM and prints, as JSON, what it did and what it told
// .NET. Run by MokaDockScriptTests: node dock-layout.mjs <path to MokaDockLayout.razor.js>
//
// The script imports moka-drag.js from where the app serves it (_content/Moka.Red.Core), which isn't where it sits in
// the source tree, so it is copied into a temporary folder beside a stand-in for that module.

import { copyFileSync, mkdirSync, mkdtempSync, rmSync, writeFileSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { join } from 'node:path';
import { pathToFileURL } from 'node:url';

// A selector engine for what the script asks: compounds of tags, classes and attributes, joined by descendant or
// child combinators, in comma lists, with :scope.
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
	const pattern = /:scope|\.[\w-]+|\[([\w-]+)(?:="([^"]*)")?\]|[\w-]+/g;
	for (let match = pattern.exec(text); match; match = pattern.exec(text)) {
		const token = match[0];
		if (token === ':scope') simple.push({ scope: true });
		else if (token[0] === '.') simple.push({ cls: token.slice(1) });
		else if (token[0] === '[') simple.push({ attr: match[1], value: match[2] });
		else simple.push({ tag: token });
	}
	return simple;
}

function matchesCompound(element, simple, scope) {
	return simple.every(s => s.scope ? element === scope
		: s.cls ? element.classes().includes(s.cls)
			: s.attr ? (s.value === undefined ? element.attributes.has(s.attr) : element.attributes.get(s.attr) === s.value)
				: element.tag === s.tag);
}

function matchesParts(element, parts, index, scope) {
	if (!matchesCompound(element, parts[index].simple, scope)) return false;
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
		this.style = {};
		this.box = { left: 0, top: 0, width: 0, height: 0 };
		this.clientWidth = 0;
		this.clientHeight = 0;
		this.scrollWidth = 0;
		this.scrollLeft = 0;
	}

	classes() {
		return (this.attributes.get('class') ?? '').split(/\s+/).filter(Boolean);
	}

	append(...children) {
		for (const child of children) {
			child.parent = this;
			this.children.push(child);
		}
		return this;
	}

	get dataset() {
		const out = {};
		for (const [name, value] of this.attributes) {
			if (name.startsWith('data-')) out[name.slice(5).replace(/-([a-z0-9])/g, (_, c) => c.toUpperCase())] = value;
		}
		return out;
	}

	getAttribute(name) {
		return this.attributes.has(name) ? this.attributes.get(name) : null;
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

	toggleAttribute(name, on) {
		if (on) this.attributes.set(name, '');
		else this.attributes.delete(name);
		return on;
	}

	matches(selector) {
		return parse(selector).some(parts => matchesParts(this, parts, parts.length - 1, null));
	}

	closest(selector) {
		for (let element = this; element; element = element.parent) {
			if (element.matches(selector)) return element;
		}
		return null;
	}

	contains(other) {
		for (let element = other; element; element = element.parent) {
			if (element === this) return true;
		}
		return false;
	}

	* descendants() {
		for (const child of this.children) {
			yield child;
			yield* child.descendants();
		}
	}

	querySelectorAll(selector) {
		const alternatives = parse(selector);
		return [...this.descendants()].filter(e => alternatives.some(parts => matchesParts(e, parts, parts.length - 1, this)));
	}

	querySelector(selector) {
		return this.querySelectorAll(selector)[0] ?? null;
	}

	getBoundingClientRect() {
		const { left, top, width, height } = this.box;
		return { left, top, width, height, right: left + width, bottom: top + height };
	}

	getClientRects() {
		return this.box.width > 0 ? [this.box] : [];
	}

	addEventListener(type, handler) {
		this.listeners.set(type, [...(this.listeners.get(type) ?? []), handler]);
	}

	removeEventListener(type, handler) {
		this.listeners.set(type, (this.listeners.get(type) ?? []).filter(h => h !== handler));
	}

	dispatch(type, event) {
		for (const handler of this.listeners.get(type) ?? []) handler(event);
	}
}

const observers = { mutation: [], resize: [] };
let frames = 0;

globalThis.CSS = { supports: () => true, escape: text => text };
globalThis.ResizeObserver = class {
	constructor(callback) {
		this.callback = callback;
		observers.resize.push(this);
	}

	observe() {}

	disconnect() {}
};
globalThis.MutationObserver = class {
	constructor(callback) {
		this.callback = callback;
		observers.mutation.push(this);
	}

	observe() {}

	disconnect() {}
};
globalThis.requestAnimationFrame = () => {
	frames++;
	return frames;
};
globalThis.document = {
	visibilityState: 'visible',
	body: { style: {} },
	addEventListener() {},
	removeEventListener() {},
	getElementById: () => null
};
globalThis.window = { open: () => null, matchMedia: () => ({ matches: false }) };

const folder = mkdtempSync(join(tmpdir(), 'moka-dock-'));
const script = join(folder, 'Moka.Red.Layout', 'DockLayout', 'MokaDockLayout.razor.js');
mkdirSync(join(folder, 'Moka.Red.Layout', 'DockLayout'), { recursive: true });
mkdirSync(join(folder, 'Moka.Red.Core'), { recursive: true });
copyFileSync(process.argv[2], script);
writeFileSync(join(folder, 'Moka.Red.Core', 'moka-drag.js'),
	'export function keySplitter() { return null; }\n'
	+ 'export function resolveLength() { return null; }\n'
	+ 'export function startSplitterDrag() {}\n'
	+ 'export function syncSplitterValues() {}\n');
const dock = await import(pathToFileURL(script).href);

function recorder() {
	const calls = [];
	return {
		calls,
		invokeMethodAsync(method, ...args) {
			calls.push([method, ...args]);
			return Promise.resolve(null);
		}
	};
}

function bound() {
	const root = new FakeElement('div', { 'data-moka-dock-layout': '' });
	const ref = recorder();
	dock.bind(root, ref, {});
	ref.calls.length = 0;
	return { root, ref, mutate: () => observers.mutation.at(-1).callback([]) };
}

const results = {};

// A preset in a view transition: the update callback is .NET's apply and nothing waits on a frame. The groups and
// hosts are named only while it runs, and a transition the browser skips (its ready rejects) leaves nothing unhandled.
// Where the page is hidden, motion is reduced or there is no API, it says so and starts nothing.
{
	const log = [];
	let started = 0;
	let finish;
	const { root } = bound();
	document.startViewTransition = update => {
		started++;
		log.push(`named ${root.hasAttribute('data-moka-dock-transition')}`);
		const updateCallbackDone = Promise.resolve().then(update).then(() => {
			log.push('update done');
		});
		const ready = Promise.reject(new Error('Transition was skipped.'));
		const finished = new Promise(resolve => {
			finish = resolve;
		});
		return { updateCallbackDone, ready, finished };
	};
	const ref = {
		invokeMethodAsync: (method, name) => new Promise(resolve => setTimeout(() => {
			log.push(`${method} ${name}`);
			resolve();
		}, 10))
	};

	log.push(`returned ${await dock.transition(root, ref, 'Debug')}`);
	const namedWhileRunning = root.hasAttribute('data-moka-dock-transition');
	finish();
	await new Promise(resolve => setTimeout(resolve, 0));
	const animated = { log: [...log], frames, started, namedWhileRunning, namedAfter: root.hasAttribute('data-moka-dock-transition') };

	// A page that draws no frames never gets its picture taken, so the update would never be called: the transition
	// is skipped, which calls it at once.
	document.startViewTransition = update => {
		started++;
		let call;
		const updateCallbackDone = new Promise(resolve => {
			call = () => resolve(Promise.resolve().then(update));
		});
		return {
			updateCallbackDone,
			ready: Promise.reject(new Error('Transition was skipped.')),
			finished: updateCallbackDone,
			skipTransition() {
				log.push('skipped');
				call();
			}
		};
	};
	log.length = 0;
	const t0 = Date.now();
	log.push(`returned ${await dock.transition(root, ref, 'Write')}`);
	await new Promise(resolve => setTimeout(resolve, 0));
	const stalled = { log: [...log], ms: Date.now() - t0, namedAfter: root.hasAttribute('data-moka-dock-transition') };

	document.visibilityState = 'hidden';
	const hidden = { result: await dock.transition(root, ref, 'Debug'), started };
	document.visibilityState = 'visible';

	window.matchMedia = () => ({ matches: true });
	const reduced = { result: await dock.transition(root, ref, 'Debug'), started };
	window.matchMedia = () => ({ matches: false });

	delete document.startViewTransition;
	const unsupported = await dock.transition(root, ref, 'Debug');
	results.transition = { animated, stalled, hidden, reduced, unsupported };
}

// A tab that becomes the one showing in a strip too narrow for its tabs scrolls into view; a strip the person
// scrolled keeps its place while the showing tab stays the same.
{
	const { root, mutate } = bound();
	const strip = new FakeElement('div', { class: 'moka-dock-tabs', role: 'tablist', 'data-moka-dock-tabs': 'g' });
	Object.assign(strip, { clientWidth: 200, scrollWidth: 600 });
	strip.box = { left: 100, top: 0, width: 200, height: 30 };
	const tab = index => {
		const wrap = new FakeElement('div', { class: 'moka-dock-tab-wrap' });
		wrap.getBoundingClientRect = () => {
			const left = 100 + index * 100 - strip.scrollLeft;
			return { left, top: 0, width: 100, height: 30, right: left + 100, bottom: 30 };
		};
		const button = new FakeElement('button', { role: 'tab', 'aria-selected': index === 0 ? 'true' : 'false' });
		wrap.append(button);
		strip.append(wrap);
		return button;
	};
	const tabs = [0, 1, 2, 3, 4].map(tab);
	root.append(strip);
	mutate();
	const atStart = strip.scrollLeft;

	strip.scrollLeft = 150;
	mutate();
	const scrolledByThePerson = strip.scrollLeft;

	tabs[0].setAttribute('aria-selected', 'false');
	tab(5).setAttribute('aria-selected', 'true');
	mutate();
	results.reveal = { atStart, scrolledByThePerson, afterDrop: strip.scrollLeft };
}

// Escape goes to .NET whatever the page shows, since the page can be a round trip behind; it is claimed only when
// something it undoes is on screen, and a menu keeps it to itself.
{
	const { root, ref } = bound();
	const button = new FakeElement('button');
	const menu = new FakeElement('div', { role: 'menu' }).append(new FakeElement('button'));
	root.append(button, menu);
	const press = target => {
		const event = { key: 'Escape', target, defaultPrevented: false, preventDefault() { this.defaultPrevented = true; } };
		root.dispatch('keydown', event);
		return event.defaultPrevented;
	};

	const nothingShown = { claimed: press(button), calls: ref.calls.splice(0).map(c => c[0]) };
	root.append(new FakeElement('section', { class: 'moka-dock-group moka-dock-group--maximized' }));
	const maximized = { claimed: press(button), calls: ref.calls.splice(0).map(c => c[0]) };
	const inMenu = { claimed: press(menu.children[0]), calls: ref.calls.splice(0).map(c => c[0]) };
	results.escape = { nothingShown, maximized, inMenu };
}

// The area floating windows sit in, for a window opened from code to fit.
{
	const root = new FakeElement('div');
	const empty = dock.measureMain(root);
	const main = new FakeElement('div', { class: 'moka-dock-main' });
	Object.assign(main, { clientWidth: 640, clientHeight: 180 });
	root.append(main);
	results.measure = { empty, main: dock.measureMain(root) };
}

rmSync(folder, { recursive: true, force: true });
process.stdout.write(JSON.stringify(results));
process.exit(0);
