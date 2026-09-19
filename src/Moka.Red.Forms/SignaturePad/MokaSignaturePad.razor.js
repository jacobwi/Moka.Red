/**
 * MokaSignaturePad - shows a signature the parent passes as Value.
 *
 * Drawing with the pointer lives in Core's moka-drag.js and is re-exported here, so the component
 * only ever imports this one module (MokaComponentBase caches a single module per component).
 * The relative import resolves against this module's own URL, so it keeps working when the host
 * app is served from a sub-path.
 */

import { initCanvasDraw, clearCanvas, undoCanvas, removeCanvasDraw } from '../../Moka.Red.Core/moka-drag.js';

export { initCanvasDraw, removeCanvasDraw };

/**
 * Gives the drawing buffer the shape the canvas is displayed at: the width stays (600, so a saved
 * signature keeps its size) and the height follows the displayed aspect ratio. Without it a Height
 * other than a third of the width would stretch every stroke.
 * @param {HTMLCanvasElement} canvas
 */
export function fitCanvas(canvas) {
	if (!canvas) return;

	const rect = canvas.getBoundingClientRect();
	if (rect.width < 1 || rect.height < 1) return;

	const height = Math.max(1, Math.round(canvas.width * rect.height / rect.width));
	if (canvas.height !== height) canvas.height = height;
}

// canvas -> the background plus the signature from Value, painted at the canvas size.
const layers = new WeakMap();

// canvas -> the latest showSignature call. An image that loads after a newer call must not paint.
const requests = new WeakMap();

/**
 * Replaces the drawing with a signature: the data URL is loaded into an image and drawn scaled to
 * fit the canvas, centred, over the background. An empty value clears the canvas. The strokes drawn
 * so far are dropped either way. Works on a read-only canvas too, where drawing was never attached.
 * @param {HTMLCanvasElement} canvas
 * @param {?string} dataUrl - The signature, usually a PNG data URL the pad produced before.
 * @param {string} backgroundColor
 * @returns {Promise<boolean>} Whether a signature is shown. False for an empty value or an image
 *   that did not load, and for a call a newer one replaced.
 */
export function showSignature(canvas, dataUrl, backgroundColor) {
	if (!canvas) return Promise.resolve(false);

	const request = {};
	requests.set(canvas, request);

	if (!dataUrl) {
		clearSignature(canvas, backgroundColor);
		return Promise.resolve(false);
	}

	return new Promise(resolve => {
		const image = new Image();
		image.onload = () => {
			if (requests.get(canvas) !== request) {
				resolve(false);
				return;
			}

			const layer = paintLayer(canvas, image, backgroundColor);
			clearSignature(canvas, backgroundColor);
			canvas.getContext('2d').drawImage(layer, 0, 0);
			layers.set(canvas, layer);
			resolve(true);
		};
		image.onerror = () => {
			// A value that is not an image shows as an empty pad rather than as the old drawing.
			if (requests.get(canvas) === request) clearSignature(canvas, backgroundColor);
			resolve(false);
		};

		// An image from another origin without CORS headers would taint the canvas, and the next
		// stroke's toDataURL would throw. This way it fails to load instead. Data URLs are unaffected.
		image.crossOrigin = 'anonymous';
		image.src = dataUrl;
	});
}

/**
 * Clears the canvas to the background and forgets both the strokes and a signature from Value.
 * An image still loading for an earlier showSignature call is not drawn when it arrives.
 * @param {HTMLCanvasElement} canvas
 * @param {string} backgroundColor
 */
export function clearSignature(canvas, backgroundColor) {
	if (!canvas) return;

	requests.delete(canvas);
	layers.delete(canvas);

	// Forgets the strokes too, when drawing is attached. A read-only canvas has none.
	clearCanvas(canvas, backgroundColor);
	fill(canvas, backgroundColor);
}

/**
 * Removes the last stroke. moka-drag.js repaints the background and then the strokes that are
 * left, which would wipe a signature from Value, so that signature is handed over as the
 * background: a pattern of the painted layer fills the canvas exactly.
 * @param {HTMLCanvasElement} canvas
 * @param {string} strokeColor
 * @param {number} strokeWidth
 * @param {string} backgroundColor
 */
export function undoSignature(canvas, strokeColor, strokeWidth, backgroundColor) {
	if (!canvas) return;

	const layer = layers.get(canvas);
	const background = layer ? canvas.getContext('2d').createPattern(layer, 'no-repeat') : backgroundColor;
	undoCanvas(canvas, strokeColor, strokeWidth, background);
}

function paintLayer(canvas, image, backgroundColor) {
	const layer = document.createElement('canvas');
	layer.width = canvas.width;
	layer.height = canvas.height;
	fill(layer, backgroundColor);

	// Fit inside the canvas and keep the aspect ratio, so a signature from a pad of another
	// shape is not stretched. An SVG without a size of its own reports 0 and fills the canvas.
	const imageWidth = image.naturalWidth || layer.width;
	const imageHeight = image.naturalHeight || layer.height;
	const scale = Math.min(layer.width / imageWidth, layer.height / imageHeight);
	const width = imageWidth * scale;
	const height = imageHeight * scale;
	layer.getContext('2d').drawImage(image, (layer.width - width) / 2, (layer.height - height) / 2, width, height);
	return layer;
}

function fill(canvas, backgroundColor) {
	const ctx = canvas.getContext('2d');
	ctx.save();
	ctx.fillStyle = backgroundColor || '#ffffff';
	ctx.fillRect(0, 0, canvas.width, canvas.height);
	ctx.restore();
}
