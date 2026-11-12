// Walks moka-chart.js's arrow keys over parts in rows and prints, as JSON, where each key landed.
// Run by PartStepScriptTests: node part-steps.mjs <path to moka-chart.js> <a listed layout's bytes as base64>

import { pathToFileURL } from 'node:url';

const { stepParts, decodeGeometry } = await import(pathToFileURL(process.argv[2]).href);

function walk(layout, start, keys, turned = false, page = null) {
	const landed = [];
	let at = start;
	for (const pressed of keys) {
		const [key, ctrl] = Array.isArray(pressed) ? pressed : [pressed, false];
		at = stepParts(layout, at, key, ctrl, turned, page);
		landed.push(at);
	}
	return landed;
}

const grid = { kind: 'rows', rows: 3, cols: 4, count: 12, flow: false };
const calendar = { kind: 'columns', rows: 7, cols: 3, count: 17, flow: true };
const listed = decodeGeometry(process.argv[3]);

console.log(JSON.stringify({
	grid: walk(grid, 5, ['ArrowRight', 'ArrowRight', 'ArrowRight', 'ArrowDown', 'ArrowDown', 'Home', 'End', 'ArrowUp',
		'ArrowLeft', ['Home', true], ['End', true], 'PageUp']),
	turned: walk(grid, 5, ['ArrowRight', 'ArrowDown', 'Home', 'End', 'ArrowLeft'], true),
	fresh: [stepParts(grid, -1, 'ArrowDown', false, false, null), stepParts(grid, -1, 'End', true, false, null),
		stepParts(grid, 3, 'Enter', false, false, null)],
	paged: walk(grid, 0, ['PageDown'], false, 3),
	calendar: walk(calendar, 6, ['ArrowDown', 'ArrowUp', 'ArrowRight', 'ArrowRight', 'ArrowDown', 'ArrowRight', 'ArrowLeft',
		'Home', 'End', ['End', true], 'ArrowUp']),
	listed: walk(listed, 1, ['ArrowDown', 'ArrowDown', 'ArrowUp', 'ArrowRight', 'ArrowUp', 'ArrowLeft', 'Home', 'End',
		['End', true], ['Home', true]]),
	decoded: {
		kind: listed.kind,
		rows: listed.rows,
		cols: listed.cols,
		count: listed.count,
		version: listed.version,
		rowOf: [...listed.rowOf],
		orders: listed.orders
	}
}));
