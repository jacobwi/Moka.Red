// Saves a chart. Imported only when someone picks an export, so a page of charts never loads it.
//
// A picture is rebuilt as one standalone SVG of the chart as it is drawn: each svg in it copied with its computed paint
// written inline, each HTML box with a background or border (bars, grid lines, legend keys) turned into a rect, and
// each run of text into text, all at the boxes the browser measured. The fonts that text uses are embedded from the
// page's own @font-face rules, so the file reads the same elsewhere; a font that can't be fetched falls back to the
// system's. A PNG is that SVG drawn on a canvas.

const SVG = 'http://www.w3.org/2000/svg';

// What an svg's look comes from once its stylesheet is gone.
const PAINT = [
	'fill', 'fill-opacity', 'fill-rule', 'stroke', 'stroke-width', 'stroke-opacity', 'stroke-dasharray', 'stroke-dashoffset',
	'stroke-linecap', 'stroke-linejoin', 'opacity', 'vector-effect', 'stop-color', 'stop-opacity', 'paint-order',
	'shape-rendering', 'font-family', 'font-size', 'font-weight', 'text-anchor', 'dominant-baseline', 'visibility', 'display'
];

const COLORS = new Set(['fill', 'stroke', 'stop-color']);

let probe = null;
const colorCache = new Map();

/**
 * Saves the chart inside root as a picture.
 * @param {HTMLElement} root The chart's frame: its title, legend and plot.
 * @param {string} format "png" or "svg".
 * @param {string} fileName The file's name, with its extension.
 * @param {{scale?: number, skip?: string, pad?: number}} options How much larger a PNG is than the screen, what to
 *   leave out (anything inside it marked data-moka-export="keep" is still drawn), and the margin around the picture.
 */
export async function exportPicture(root, format, fileName, options) {
	if (!root || !root.isConnected) {
		throw new Error('The chart is not on the page.');
	}

	const scale = Math.max(1, Number(options?.scale) || 2);
	const pad = Math.max(0, Number(options?.pad ?? 16) || 0);
	const skip = options?.skip || '';
	const origin = root.getBoundingClientRect();
	const contentWidth = Math.max(1, Math.ceil(origin.width));

	const picture = svgElement('svg');
	const defs = svgElement('defs', null, picture);
	const background = svgElement('rect', { fill: color(backgroundOf(root)) }, picture);
	const content = svgElement('g', { transform: `translate(${pad} ${pad})` }, picture);

	const state = { root, origin, skip, used: new Map(), extent: { bottom: 0 } };
	draw(root, state, content);
	await embedFonts(defs, state.used);

	// What was left out at the bottom (a range strip, a toolbar) leaves no empty band.
	const width = contentWidth + pad * 2;
	const height = Math.max(1, Math.ceil(Math.min(state.extent.bottom, origin.height))) + pad * 2;
	for (const [key, value] of Object.entries({ width, height, viewBox: `0 0 ${width} ${height}` })) {
		picture.setAttribute(key, String(value));
	}

	background.setAttribute('width', String(width));
	background.setAttribute('height', String(height));

	const text = new XMLSerializer().serializeToString(picture);
	if (format === 'svg') {
		save(new Blob([text], { type: 'image/svg+xml' }), fileName);
		return;
	}

	save(await toPng(text, width, height, scale), fileName);
}

/**
 * Saves what .NET streams as a file, such as a chart's values as CSV. The stream is read into memory here, as the
 * Blazor docs' file download does; it never sits in one interop message.
 * @param {object} streamRef A DotNetStreamReference.
 * @param {string} fileName The file's name, with its extension.
 * @param {string} mime Its media type.
 */
export async function download(streamRef, fileName, mime) {
	const data = await streamRef.arrayBuffer();
	save(new Blob([data], { type: mime || 'application/octet-stream' }), fileName);
}

function save(blob, fileName) {
	const url = URL.createObjectURL(blob);
	const anchor = document.createElement('a');
	anchor.href = url;
	anchor.download = fileName || '';
	anchor.style.display = 'none';
	document.body.appendChild(anchor);
	anchor.click();
	anchor.remove();
	URL.revokeObjectURL(url);
}

function toPng(text, width, height, scale) {
	return new Promise((resolve, reject) => {
		const url = URL.createObjectURL(new Blob([text], { type: 'image/svg+xml' }));
		const image = new Image();
		image.onload = () => {
			URL.revokeObjectURL(url);
			const canvas = document.createElement('canvas');
			canvas.width = Math.round(width * scale);
			canvas.height = Math.round(height * scale);
			const context = canvas.getContext('2d');
			context.scale(scale, scale);
			context.drawImage(image, 0, 0, width, height);
			canvas.toBlob(blob => (blob ? resolve(blob) : reject(new Error('The picture could not be made.'))), 'image/png');
		};
		image.onerror = () => {
			URL.revokeObjectURL(url);
			reject(new Error('The picture could not be made.'));
		};
		image.src = url;
	});
}

function draw(element, state, target) {
	for (const child of element.childNodes) {
		if (child.nodeType === Node.TEXT_NODE) {
			drawText(child, state, target);
		} else if (child.nodeType === Node.ELEMENT_NODE) {
			drawElement(child, state, target);
		}
	}
}

function drawElement(element, state, target) {
	if (state.skip && element.matches(state.skip)) {
		// A legend inside a toolbar left out is still part of the picture.
		for (const kept of element.querySelectorAll('[data-moka-export="keep"]')) {
			const outer = kept.parentElement?.closest('[data-moka-export="keep"]');
			if (!outer || !element.contains(outer)) {
				drawElement(kept, state, target);
			}
		}

		return;
	}

	const style = getComputedStyle(element);
	if (style.display === 'none' || element.tagName === 'INPUT' || element.tagName === 'TEXTAREA' || element.tagName === 'SELECT') {
		return;
	}

	// Dimmed content stays dimmed in the picture.
	const opacity = Number(style.opacity);
	if (opacity <= 0) {
		return;
	}

	let layer = target;
	if (opacity < 1) {
		layer = svgElement('g', { opacity: String(opacity) }, target);
	}

	if (element instanceof SVGSVGElement) {
		if (style.visibility === 'visible') {
			copySvg(element, state, layer);
		}

		return;
	}

	if (style.visibility === 'visible') {
		drawBox(element, style, state, layer);
	}

	draw(element, state, layer);
}

function reach(state, box) {
	state.extent.bottom = Math.max(state.extent.bottom, box.bottom - state.origin.top);
}

function drawBox(element, style, state, target) {
	const box = element.getBoundingClientRect();
	if (box.width <= 0 || box.height <= 0) {
		return;
	}

	const x = box.left - state.origin.left;
	const y = box.top - state.origin.top;
	const radius = parseFloat(style.borderTopLeftRadius) || 0;
	const fill = color(style.backgroundColor);
	const sides = ['Top', 'Right', 'Bottom', 'Left'].map(side => ({
		width: style[`border${side}Style`] === 'none' ? 0 : parseFloat(style[`border${side}Width`]) || 0,
		color: color(style[`border${side}Color`])
	}));
	const drawn = sides.filter(side => side.width > 0 && side.color !== 'none');
	const even = drawn.length === 4 && drawn.every(side => side.width === drawn[0].width && side.color === drawn[0].color);
	if (fill !== 'none' || drawn.length > 0) {
		reach(state, box);
	}

	if (fill !== 'none' || even) {
		const inset = even ? drawn[0].width / 2 : 0;
		const attributes = {
			x: round(x + inset), y: round(y + inset),
			width: round(Math.max(0, box.width - inset * 2)), height: round(Math.max(0, box.height - inset * 2)),
			fill
		};
		if (radius > 0) {
			attributes.rx = round(Math.min(radius, box.width / 2, box.height / 2));
		}

		if (even) {
			attributes.stroke = drawn[0].color;
			attributes['stroke-width'] = round(drawn[0].width);
		}

		svgElement('rect', attributes, target);
	}

	ring(style, x, y, box, radius, target);

	if (even) {
		return;
	}

	const [top, right, bottom, left] = sides;
	const edge = (side, ex, ey, ew, eh) => {
		if (side.width > 0 && side.color !== 'none') {
			svgElement('rect', { x: round(ex), y: round(ey), width: round(ew), height: round(eh), fill: side.color }, target);
		}
	};
	edge(top, x, y, box.width, top.width);
	edge(bottom, x, y + box.height - bottom.width, box.width, bottom.width);
	edge(left, x, y, left.width, box.height);
	edge(right, x + box.width - right.width, y, right.width, box.height);
}

// A shadow with no offset or blur is a ring, which is how marks and keys draw their outlines; it becomes a stroke.
function ring(style, x, y, box, radius, target) {
	const shadow = /^((?:rgba?|hsla?|color|oklch|oklab)\([^)]*\)|#[0-9a-f]+|[a-z]+)\s+(-?[\d.]+)px\s+(-?[\d.]+)px\s+(-?[\d.]+)px\s+(-?[\d.]+)px(\s+inset)?/i
		.exec(style.boxShadow || '');
	if (!shadow || Number(shadow[2]) !== 0 || Number(shadow[3]) !== 0 || Number(shadow[4]) !== 0) {
		return;
	}

	const width = Number(shadow[5]);
	const stroke = color(shadow[1]);
	if (!(width > 0) || stroke === 'none') {
		return;
	}

	const offset = shadow[6] ? width / 2 : -width / 2;
	const attributes = {
		x: round(x + offset), y: round(y + offset),
		width: round(Math.max(0, box.width - offset * 2)), height: round(Math.max(0, box.height - offset * 2)),
		fill: 'none', stroke, 'stroke-width': round(width)
	};
	if (radius > 0) {
		attributes.rx = round(Math.max(0, Math.min(radius - offset, box.width / 2, box.height / 2)));
	}

	svgElement('rect', attributes, target);
}

function copySvg(source, state, target) {
	const box = source.getBoundingClientRect();
	if (box.width <= 0 || box.height <= 0) {
		return;
	}

	const copy = source.cloneNode(true);
	const from = [source, ...source.querySelectorAll('*')];
	const to = [copy, ...copy.querySelectorAll('*')];
	for (let i = 0; i < from.length; i++) {
		const style = getComputedStyle(from[i]);
		const declarations = [];
		for (const property of PAINT) {
			let value = style.getPropertyValue(property);
			if (!value) {
				continue;
			}

			if (COLORS.has(property) && !value.startsWith('url(')) {
				value = color(value);
			}

			declarations.push(`${property}:${value}`);
			if (property === 'font-family') {
				noteFont(state.used, value, from[i].textContent ?? '');
			}
		}

		to[i].setAttribute('style', declarations.join(';'));
		to[i].removeAttribute('class');
	}

	copy.querySelectorAll('title').forEach(title => title.remove());
	copy.setAttribute('x', round(box.left - state.origin.left));
	copy.setAttribute('y', round(box.top - state.origin.top));
	copy.setAttribute('width', round(box.width));
	copy.setAttribute('height', round(box.height));
	copy.removeAttribute('aria-hidden');
	copy.removeAttribute('focusable');
	target.appendChild(copy);
	reach(state, box);
}

function drawText(node, state, target) {
	const value = node.nodeValue;
	const parent = node.parentElement;
	if (!value || !value.trim() || !parent) {
		return;
	}

	const style = getComputedStyle(parent);
	if (style.visibility !== 'visible') {
		return;
	}

	const fill = color(style.color);
	if (fill === 'none') {
		return;
	}

	const range = document.createRange();
	range.selectNodeContents(node);
	const whole = range.getBoundingClientRect();
	if (whole.width <= 0 || whole.height <= 0) {
		return;
	}

	noteFont(state.used, style.fontFamily, value);
	reach(state, whole);
	const attributes = {
		'font-family': style.fontFamily,
		'font-size': style.fontSize,
		'font-weight': style.fontWeight,
		'font-style': style.fontStyle,
		fill
	};
	if (style.letterSpacing && style.letterSpacing !== 'normal') {
		attributes['letter-spacing'] = style.letterSpacing;
	}

	const angle = rotationOf(parent, state.root);
	if (angle !== 0) {
		const cx = round(whole.left + whole.width / 2 - state.origin.left);
		const cy = round(whole.top + whole.height / 2 - state.origin.top);
		const text = svgElement('text', {
			...attributes, x: cx, y: cy, 'text-anchor': 'middle', 'dominant-baseline': 'central',
			transform: `rotate(${round(angle)} ${cx} ${cy})`
		}, target);
		text.textContent = cased(value.replace(/\s+/g, ' ').trim(), style.textTransform);
		return;
	}

	const metrics = fontMetrics(style);
	for (const line of linesOf(node, value, range)) {
		const text = svgElement('text', {
			...attributes,
			x: round(line.left - state.origin.left),
			y: round(line.top - state.origin.top + (line.height - metrics.ascent - metrics.descent) / 2 + metrics.ascent)
		}, target);
		text.setAttributeNS('http://www.w3.org/XML/1998/namespace', 'xml:space', 'preserve');
		text.textContent = cased(line.text, style.textTransform);
	}
}

// Wrapped text gives one box per line; the words are grouped by the line they sit on.
function linesOf(node, value, range) {
	const rects = [...range.getClientRects()].filter(rect => rect.width > 0);
	if (rects.length <= 1) {
		const rect = rects[0] ?? range.getBoundingClientRect();
		return [{ text: value.replace(/\s+/g, ' ').trim(), left: rect.left, top: rect.top, height: rect.height }];
	}

	const lines = [];
	const words = /\S+/g;
	let match;
	while ((match = words.exec(value)) !== null) {
		range.setStart(node, match.index);
		range.setEnd(node, match.index + match[0].length);
		const rect = range.getBoundingClientRect();
		if (rect.width <= 0) {
			continue;
		}

		const last = lines[lines.length - 1];
		if (last && Math.abs(last.top - rect.top) < rect.height / 2) {
			last.text += ` ${match[0]}`;
		} else {
			lines.push({ text: match[0], left: rect.left, top: rect.top, height: rect.height });
		}
	}

	return lines;
}

// A turned label (an axis that rotates its labels) is drawn turned the same way about its middle.
function rotationOf(element, root) {
	for (let node = element; node && node !== root; node = node.parentElement) {
		const transform = getComputedStyle(node).transform;
		if (transform && transform !== 'none') {
			const matrix = new DOMMatrixReadOnly(transform);
			const angle = Math.atan2(matrix.b, matrix.a) * 180 / Math.PI;
			if (Math.abs(angle) > 0.5) {
				return angle;
			}
		}
	}

	return 0;
}

function cased(text, transform) {
	switch (transform) {
		case 'uppercase':
			return text.toUpperCase();
		case 'lowercase':
			return text.toLowerCase();
		case 'capitalize':
			return text.replace(/(^|\s)(\S)/g, (_, space, letter) => space + letter.toUpperCase());
		default:
			return text;
	}
}

function fontMetrics(style) {
	const context = canvas2d();
	context.font = `${style.fontStyle} ${style.fontWeight} ${style.fontSize} ${style.fontFamily}`;
	const measured = context.measureText('Hg');
	const size = parseFloat(style.fontSize) || 12;
	return {
		ascent: measured.fontBoundingBoxAscent ?? measured.actualBoundingBoxAscent ?? size * 0.8,
		descent: measured.fontBoundingBoxDescent ?? measured.actualBoundingBoxDescent ?? size * 0.2
	};
}

// Any CSS color, a color-mix or a color space included, as plain rgb that every SVG reader knows. The browser draws
// it on one pixel and the pixel is read back.
function color(value) {
	if (!value || value === 'transparent' || value === 'none') {
		return 'none';
	}

	if (colorCache.has(value)) {
		return colorCache.get(value);
	}

	const context = canvas2d();
	context.clearRect(0, 0, 1, 1);
	context.fillStyle = '#000';
	context.fillStyle = value;
	context.fillRect(0, 0, 1, 1);
	const [r, g, b, a] = context.getImageData(0, 0, 1, 1).data;
	const result = a === 0 ? 'none' : a === 255 ? `rgb(${r},${g},${b})` : `rgba(${r},${g},${b},${round(a / 255)})`;
	colorCache.set(value, result);
	return result;
}

function canvas2d() {
	if (!probe) {
		const canvas = document.createElement('canvas');
		canvas.width = 1;
		canvas.height = 1;
		probe = canvas.getContext('2d', { willReadFrequently: true });
	}

	return probe;
}

function backgroundOf(element) {
	for (let node = element; node && node.nodeType === Node.ELEMENT_NODE; node = node.parentElement) {
		const background = getComputedStyle(node).backgroundColor;
		if (color(background) !== 'none') {
			return background;
		}
	}

	return getComputedStyle(element).getPropertyValue('--moka-color-surface').trim() || '#fff';
}

function noteFont(used, families, text) {
	for (const family of families.split(',')) {
		const name = family.trim().replace(/^["']|["']$/g, '').toLowerCase();
		if (!name) {
			continue;
		}

		let points = used.get(name);
		if (!points) {
			points = new Set();
			used.set(name, points);
		}

		for (const character of text) {
			points.add(character.codePointAt(0));
		}
	}
}

// Embeds the faces of the page's own @font-face rules that the picture's text uses: by family, and by unicode-range
// so a Latin chart doesn't carry the extended set.
async function embedFonts(defs, used) {
	const faces = [];
	for (const sheet of document.styleSheets) {
		let rules;
		try {
			rules = sheet.cssRules;
		} catch {
			continue;
		}

		for (const rule of rules ?? []) {
			if (!(rule instanceof CSSFontFaceRule)) {
				continue;
			}

			const family = rule.style.getPropertyValue('font-family').trim().replace(/^["']|["']$/g, '');
			const points = used.get(family.toLowerCase());
			if (!points || !covers(rule.style.getPropertyValue('unicode-range'), points)) {
				continue;
			}

			const source = /url\(\s*["']?([^"')]+)["']?\s*\)/.exec(rule.style.getPropertyValue('src'));
			if (source) {
				faces.push({ rule, family, url: new URL(source[1], sheet.href ?? document.baseURI).href });
			}
		}
	}

	const embedded = await Promise.all(faces.map(async face => {
		try {
			const response = await fetch(face.url);
			if (!response.ok) {
				return '';
			}

			const data = await asDataUrl(await response.blob());
			const style = face.rule.style;
			const extra = ['font-style', 'font-weight', 'font-stretch', 'unicode-range']
				.map(property => [property, style.getPropertyValue(property)])
				.filter(([, value]) => value)
				.map(([property, value]) => `${property}:${value};`)
				.join('');
			return `@font-face{font-family:"${face.family}";src:url(${data}) format("woff2");${extra}}`;
		} catch {
			return '';
		}
	}));

	const css = embedded.filter(Boolean).join('\n');
	if (css) {
		svgElement('style', null, defs).textContent = css;
	}
}

function covers(unicodeRange, points) {
	if (!unicodeRange) {
		return true;
	}

	const ranges = unicodeRange.split(',').map(part => {
		const [from, to] = part.trim().replace(/^U\+/i, '').split('-');
		if (from.includes('?')) {
			return [parseInt(from.replace(/\?/g, '0'), 16), parseInt(from.replace(/\?/g, 'F'), 16)];
		}

		return [parseInt(from, 16), parseInt(to ?? from, 16)];
	});
	for (const point of points) {
		if (ranges.some(([from, to]) => point >= from && point <= to)) {
			return true;
		}
	}

	return false;
}

function asDataUrl(blob) {
	return new Promise((resolve, reject) => {
		const reader = new FileReader();
		reader.onload = () => resolve(reader.result);
		reader.onerror = () => reject(reader.error);
		reader.readAsDataURL(blob);
	});
}

function svgElement(name, attributes, parent) {
	const element = document.createElementNS(SVG, name);
	for (const [key, value] of Object.entries(attributes ?? {})) {
		if (value !== undefined && value !== null) {
			element.setAttribute(key, String(value));
		}
	}

	parent?.appendChild(element);
	return element;
}

function round(value) {
	return Math.round(value * 100) / 100;
}
