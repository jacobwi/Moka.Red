// Drives moka-chart.js's linked zoom over a small stand-in for the DOM and prints, as JSON, what each chart told .NET.
// Run by LinkedZoomScriptTests: node linked-zoom.mjs <path to moka-chart.js>

import { pathToFileURL } from 'node:url';
import { FakeElement, FakeSvg } from './fake-dom.mjs';

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
