// Runs moka-table.js's sorting of keys typed on a cell while its editor opens, and its typing of the held keys into the
// editor's text, and prints, as JSON, what each gave. Run by MokaTableReleaseFixTests: node table-held-keys.mjs <path>
// The table script imports two Core scripts by a path that only resolves in the browser, so this loads a copy without
// those imports, with stand-ins for what they export.

import { readFileSync, writeFileSync, mkdtempSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { join } from 'node:path';
import { pathToFileURL } from 'node:url';

const source = readFileSync(process.argv[2], 'utf8').replace(/^import .*$/gm, '');
const copy = join(mkdtempSync(join(tmpdir(), 'moka-table-')), 'moka-table.mjs');
writeFileSync(copy, `const popupOpen = () => {}, popupClose = () => {}, copyText = () => {};\n${source}`);
const { heldKeyKind, typeHeldKeys } = await import(pathToFileURL(copy).href);

const altGraph = { getModifierState: key => key === 'AltGraph' };

console.log(JSON.stringify({
	letter: heldKeyKind({ key: 'a' }),
	space: heldKeyKind({ key: ' ' }),
	backspace: heldKeyKind({ key: 'Backspace' }),
	emoji: heldKeyKind({ key: '\u{1F600}' }),
	altGraphLetter: heldKeyKind({ key: '@', ctrlKey: true, altKey: true, ...altGraph }),
	enter: heldKeyKind({ key: 'Enter' }),
	escape: heldKeyKind({ key: 'Escape' }),
	tab: heldKeyKind({ key: 'Tab', shiftKey: true }),
	heldEnter: heldKeyKind({ key: 'Enter', repeat: true }),
	shiftEnter: heldKeyKind({ key: 'Enter', shiftKey: true }),
	arrow: heldKeyKind({ key: 'ArrowDown' }),
	f2: heldKeyKind({ key: 'F2' }),
	copy: heldKeyKind({ key: 'c', ctrlKey: true }),
	delete: heldKeyKind({ key: 'Delete' }),
	composing: heldKeyKind({ key: 'a', isComposing: true }),
	overSelection: typeHeldKeys('12.5', 0, 4, ['4', '2']),
	withBackspace: typeHeldKeys('12.5', 0, 4, ['4', '3', 'Backspace', '2']),
	backspaceFirst: typeHeldKeys('Lamp', 0, 4, ['Backspace', 'D']),
	atCaret: typeHeldKeys('Lamp', 4, 4, ['s']),
	backspacePastStart: typeHeldKeys('', 0, 0, ['Backspace', 'x'])
}));
