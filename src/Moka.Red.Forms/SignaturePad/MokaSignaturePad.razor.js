/**
 * MokaSignaturePad - shows a signature the parent passes as Value.
 *
 * Drawing with the pointer, sizing the buffer to the screen's pixels and exporting live in Core's
 * moka-drag.js and are re-exported here, so the component only ever imports this one module. The
 * relative import resolves against this module's own URL, so it keeps working when the host app is
 * served from a sub-path.
 */

import { fitCanvas, initCanvasDraw, clearCanvas, setCanvasImage, undoCanvas, removeCanvasDraw } from '../../Moka.Red.Core/moka-drag.js';

export { fitCanvas, initCanvasDraw, removeCanvasDraw, undoCanvas as undoSignature };

// canvas -> the latest showSignature call. An image that loads after a newer call must not paint.
const requests = new WeakMap();

/**
 * Replaces the drawing with a signature: the data URL is loaded into an image and drawn scaled to
 * fit the canvas, centred, over the background. An empty value clears the canvas. The strokes drawn
 * so far are dropped either way. Works on a read-only canvas too, where drawing was never attached.
 * The image is painted from its own pixels at the screen's density, again on every resize.
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

			setCanvasImage(canvas, image, backgroundColor);
			resolve(true);
		};
		image.onerror = () => {
			// A value that is not an image shows as an empty pad rather than as the old drawing.
			if (requests.get(canvas) === request) clearSignature(canvas, backgroundColor);
			resolve(false);
		};

		// An image from another origin without CORS headers would taint the canvas, and the next
		// stroke's export would throw. This way it fails to load instead. Data URLs are unaffected.
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
	clearCanvas(canvas, backgroundColor);
}
