// Drives moka-chart.js's outside legend over a small stand-in for the DOM and prints, as JSON, which series each chart
// of the group highlights. Run by ChartLegendGroupTests: node legend-group.mjs <path to moka-chart.js>

import { pathToFileURL } from 'node:url';
import { FakeElement } from './fake-dom.mjs';

const chart = await import(pathToFileURL(process.argv[2]).href);

const silentRef = { invokeMethodAsync: () => Promise.resolve(null) };

// A bound chart with one mark per series, in a sync group, highlighting from its legend or not.
function makeChart(sync, highlight) {
	const root = new FakeElement('moka-chart');
	root.appendChild(new FakeElement('mark', { 'data-moka-key': 'lan' }));
	root.appendChild(new FakeElement('mark', { 'data-moka-key': 'wifi' }));
	chart.bind(root, silentRef, { mode: 'none', sync, highlight });
	return root;
}

function makeLegend() {
	const legend = new FakeElement('moka-chart-legend');
	const lan = legend.appendChild(new FakeElement('moka-chart-legend__item', { 'data-moka-legend-key': 'lan' }));
	const name = lan.appendChild(new FakeElement('moka-chart-legend__name'));
	const wifi = legend.appendChild(new FakeElement('moka-chart-legend__item', { 'data-moka-legend-key': 'wifi' }));
	return { legend, lan, name, wifi };
}

const a = makeChart('net', true);
const b = makeChart('net', true);
const quiet = makeChart('net', false);
const elsewhere = makeChart('other', true);
const { legend, lan, name, wifi } = makeLegend();
chart.bindLegend(legend, 'net');

const lit = () => ({
	a: a.getAttribute('data-moka-hl'),
	b: b.getAttribute('data-moka-hl'),
	quiet: quiet.getAttribute('data-moka-hl'),
	elsewhere: elsewhere.getAttribute('data-moka-hl'),
	dimmed: wifi.hasAttribute('data-moka-dim') && !lan.hasAttribute('data-moka-dim')
});

const results = {};

legend.dispatch('pointerover', { target: name });
results.over = lit();

// Moving within an entry keeps it; leaving the legend clears it.
legend.dispatch('pointerout', { target: name, relatedTarget: lan });
legend.dispatch('pointerout', { target: lan, relatedTarget: null });
results.out = lit();

legend.dispatch('focusin', { target: wifi });
results.focus = lit();

chart.unbindLegend(legend);
const afterUnbind = lit();
legend.dispatch('pointerover', { target: name });
results.unbound = { a: afterUnbind.a, silent: a.getAttribute('data-moka-hl') === null };

process.stdout.write(JSON.stringify(results));
process.exit(0);
