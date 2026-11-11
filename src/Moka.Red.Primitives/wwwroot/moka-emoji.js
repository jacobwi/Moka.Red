// Emoji data shared by MokaEmojiPanel and MokaEmojiTypeahead: the data file, fetched once per page and only when a
// picker first needs it, search, skin tones, and which emoji versions this system can draw.
//
// The data file is moka-emoji.en.json beside this module (format 1, written by tools/Moka.Red.EmojiGen), or an app's
// own file in the same format. Each row is [glyph, group, name, shortcodes, keywords, version, tone].

const TONES = ['', '\u{1F3FB}', '\u{1F3FC}', '\u{1F3FD}', '\u{1F3FE}', '\u{1F3FF}'];
const VARIATION = '\uFE0F';
const TONE_MARKS = /[\u{1F3FB}-\u{1F3FF}]/gu;
const VARIATIONS = /\uFE0F/g;

// The stack the panel draws with. MokaFlag's font covers only the flag code points, so it is fetched only once a
// flag shows, and it is what draws flags on Windows, which has none of its own.
export const EMOJI_FONTS = "'Twemoji Country Flags', 'Apple Color Emoji', 'Segoe UI Emoji', 'Noto Color Emoji', sans-serif";

// Versions every system still in use draws. Only newer ones are probed.
const ALWAYS_DRAWN = 11;

const datasets = new Map();
let drawable;

/** The address of a data file: the built-in English one, or an app's own, read against the page's base. */
export function dataUrl(source) {
	return source
		? new URL(source, document.baseURI).href
		: new URL('./moka-emoji.en.json', import.meta.url).href;
}

/**
 * Loads a data file once per page. A failed load is forgotten, so the next picker that opens tries again.
 * @param {string|null} source - An app's own data file, or null for the built-in one.
 */
export function load(source) {
	const url = dataUrl(source);
	let pending = datasets.get(url);
	if (!pending) {
		pending = fetch(url)
			.then(response => {
				if (!response.ok) throw new Error(`The emoji data at ${url} answered ${response.status}.`);
				return response.json();
			})
			.then(prepare);
		pending.catch(() => datasets.delete(url));
		datasets.set(url, pending);
	}

	return pending;
}

function prepare(json) {
	const groups = (json.groups || []).map(group => ({ id: String(group.id), name: String(group.name) }));
	const emoji = (json.emoji || []).map((row, index) => {
		const [glyph, group, name, codes, keywords, version, tone] = row;
		const lower = String(name).toLowerCase();
		return {
			index,
			glyph,
			group,
			name: String(name),
			lower,
			words: lower.split(/[\s:,.'()\u2019-]+/).filter(Boolean),
			codes: codes ? String(codes).split('|') : [],
			keywords: keywords ? String(keywords).split('|') : [],
			version: Number(version) || 0,
			tone: tone === 1 ? [0] : Array.isArray(tone) ? tone : null
		};
	});

	const byGlyph = new Map();
	const byCode = new Map();
	for (const entry of emoji) {
		byGlyph.set(bare(entry.glyph), entry);
		for (const code of entry.codes) {
			if (!byCode.has(code)) byCode.set(code, entry);
		}
	}

	return { groups, emoji, byGlyph, byCode };
}

/** An emoji without its skin tone and variation selectors, the way the data file keys it. */
export function bare(glyph) {
	return String(glyph ?? '').replace(TONE_MARKS, '').replace(VARIATIONS, '');
}

/** The data row for an emoji, with or without a skin tone. */
export function find(data, glyph) {
	return data.byGlyph.get(bare(glyph)) ?? null;
}

/**
 * The emoji with a skin tone, for the ones that take one. The modifier goes after each code point the row names, and
 * an FE0F right after one is dropped, as Unicode writes the toned sequences. So a ZWJ sequence such as 🧑‍💻 keeps
 * its parts, and 🧑‍🤝‍🧑 tones both people.
 * @param {object} entry - A data row.
 * @param {number} tone - 0 for none, 1 to 5 from light to dark.
 */
export function withTone(entry, tone) {
	if (!tone || !entry.tone || !TONES[tone]) return entry.glyph;

	const points = [...entry.glyph];
	let result = '';
	for (let i = 0; i < points.length; i++) {
		if (i > 0 && points[i] === VARIATION && entry.tone.includes(i - 1)) continue;
		result += points[i];
		if (entry.tone.includes(i)) result += TONES[tone];
	}

	return result;
}

function clean(query) {
	return String(query ?? '').trim().toLowerCase().replace(/^:+|:+$/g, '');
}

/**
 * The emoji matching what was typed, best first: an exact shortcode, then shortcodes and names that start with it,
 * then words in the name, then keywords, then anything containing it. Ties keep Unicode's order.
 * @param {object} data - A loaded data file.
 * @param {string} query - What was typed, with or without colons.
 * @param {{keywords?: boolean, visible?: (entry: object) => boolean, limit?: number}} options - keywords false
 *     matches names only.
 */
export function search(data, query, options) {
	const q = clean(query);
	if (!q) return [];

	const keywords = options?.keywords !== false;
	const visible = options?.visible;
	const hits = [];
	for (const entry of data.emoji) {
		if (visible && !visible(entry)) continue;
		const score = keywords ? fullScore(entry, q) : nameScore(entry, q);
		if (score) hits.push([score, entry]);
	}

	hits.sort((a, b) => b[0] - a[0] || a[1].index - b[1].index);
	const limit = options?.limit;
	return (limit ? hits.slice(0, limit) : hits).map(hit => hit[1]);
}

function nameScore(entry, q) {
	if (entry.lower === q) return 75;
	if (entry.lower.startsWith(q)) return 70;
	if (entry.words.some(word => word.startsWith(q))) return 60;
	if (entry.lower.includes(q)) return 30;
	return 0;
}

// Whole matches first (code, then name, then keyword), then beginnings, so "lol" finds 😂 before 🍭 :lollipop:.
function fullScore(entry, q) {
	if (entry.codes[0] === q) return 100;
	if (entry.codes.includes(q)) return 95;
	const byName = nameScore(entry, q);
	if (byName === 75) return 90;
	if (entry.keywords.includes(q)) return 85;
	if (entry.codes.some(code => code.startsWith(q))) return 80;
	if (byName >= 60) return byName;
	if (entry.keywords.some(word => word.startsWith(q))) return 50;
	if (byName || entry.codes.some(code => code.includes(q))) return 30;
	return 0;
}

/**
 * The newest emoji version this system draws, found once per page by drawing one emoji of each version on a canvas:
 * one that comes out as a box has no color, and a ZWJ sequence the font doesn't join comes out as two pictures, wider
 * than one. Infinity when the canvas can't be read (some browsers blur it against fingerprinting), so nothing is hidden
 * by mistake.
 * @param {object} data - A loaded data file.
 */
export function drawableVersion(data) {
	drawable ??= Promise.resolve().then(() => probe(data)).catch(() => Infinity);
	return drawable;
}

function probe(data) {
	if (typeof document === 'undefined') return Infinity;

	const size = 40;
	const canvas = document.createElement('canvas');
	canvas.width = size;
	canvas.height = size;
	const context = canvas.getContext('2d', { willReadFrequently: true });
	if (!context) return Infinity;

	context.font = `30px ${EMOJI_FONTS}`;
	context.textBaseline = 'top';
	context.fillStyle = '#000';

	const colorful = glyph => {
		context.clearRect(0, 0, size, size);
		context.fillText(glyph, 2, 2);
		const pixels = context.getImageData(0, 0, size, size).data;
		let count = 0;
		for (let i = 0; i < pixels.length; i += 4) {
			if (pixels[i + 3] < 128) continue;
			const high = Math.max(pixels[i], pixels[i + 1], pixels[i + 2]);
			const low = Math.min(pixels[i], pixels[i + 1], pixels[i + 2]);
			if (high - low > 40) count++;
		}

		return count > 12;
	};

	const reference = '\u{1F600}';
	if (!colorful(reference)) return Infinity;
	const width = context.measureText(reference).width;

	const flags = data.groups.findIndex(group => group.id === 'flags');
	const samples = new Map();
	for (const entry of data.emoji) {
		if (entry.version <= ALWAYS_DRAWN || entry.group === flags) continue;
		const current = samples.get(entry.version);
		// A face of the version if it has one: faces are colorful, where a grey rock or a white flag would read as a box.
		if (!current || (current.group !== 0 && entry.group === 0)) samples.set(entry.version, entry);
	}

	let newest = ALWAYS_DRAWN;
	for (const version of [...samples.keys()].sort((a, b) => a - b)) {
		const glyph = samples.get(version).glyph;
		if (!colorful(glyph) || context.measureText(glyph).width > width * 1.5) break;
		newest = version;
	}

	return newest;
}

/**
 * Puts text in a field at the caret, over any selection, and tells Blazor through an input event, so @bind hears it
 * as typing. The caret ends after the text.
 * @param {HTMLInputElement|HTMLTextAreaElement} field - The field.
 * @param {string} text - What to insert.
 */
export function insertText(field, text) {
	if (!field || typeof field.setRangeText !== 'function' || !text) return;

	const length = field.value.length;
	const start = field.selectionStart ?? length;
	const end = field.selectionEnd ?? start;
	field.setRangeText(text, start, end, 'end');
	field.dispatchEvent(new Event('input', { bubbles: true }));
}

/**
 * Inserts text into the field that sits in the same box as a button, the way a field's own emoji button does it, and
 * puts focus back in the field.
 * @param {HTMLElement} button - The button that opened the picker.
 * @param {string} box - A selector for the element holding both, such as ".moka-textfield".
 * @param {string} text - What to insert.
 */
export function insertNear(button, box, text) {
	const field = button?.closest(box)?.querySelector('textarea, input:not([type="hidden"])');
	if (!field) return;
	insertText(field, text);
	field.focus({ preventScroll: true });
}
