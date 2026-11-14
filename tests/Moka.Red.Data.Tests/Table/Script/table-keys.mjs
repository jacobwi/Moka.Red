// Runs moka-table.js's sideways steps over a column window and its aim at rows not drawn, and prints, as JSON, where each
// landed. Run by TableScriptTests: node table-keys.mjs <path to moka-table.js>
// The table script imports two Core scripts by a path that only resolves in the browser, so this loads a copy without
// those imports, with stand-ins for what they export.

import { readFileSync, writeFileSync, mkdtempSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { join } from 'node:path';
import { pathToFileURL } from 'node:url';

const source = readFileSync(process.argv[2], 'utf8').replace(/^import .*$/gm, '');
const copy = join(mkdtempSync(join(tmpdir(), 'moka-table-')), 'moka-table.mjs');
writeFileSync(copy, `const popupOpen = () => {}, popupClose = () => {}, copyText = () => {};\n${source}`);
const { stepAcrossColumns, scrollTowardRow } = await import(pathToFileURL(copy).href);

// Columns 0 and 1 pinned, 2 to 9 drawn, 10 to 58 out of view, 59 pinned at the end, and 4 drawn but hidden on a phone.
const shownAt = i => i === 4 ? false : i <= 9 || i === 59 ? true : undefined;
const step = (from, key) => stepAcrossColumns(shownAt, from, key, 59);

// A window of rows 900 to 919 drawn 33,300 px down, at 37 px a row, under a 36 px header in a 420 px box.
const aim = rowIndex => scrollTowardRow({ nearTop: 33300 + (rowIndex > 919 ? 19 * 37 : 0), nearIndex: rowIndex > 919 ? 919 : 900, rowHeight: 37, rowIndex, view: 384 });

console.log(JSON.stringify({
	rightInside: step(2, 'ArrowRight'),
	overHidden: step(3, 'ArrowRight'),
	rightOutOfView: step(9, 'ArrowRight'),
	leftOutOfView: step(59, 'ArrowLeft'),
	leftInside: step(5, 'ArrowLeft'),
	home: step(30, 'Home'),
	end: step(3, 'End'),
	pastTheEnd: step(59, 'ArrowRight'),
	aimBelow: aim(1000),
	aimAbove: aim(100),
	aimTop: aim(0)
}));
