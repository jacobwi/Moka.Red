// Decodes QR codes with jsQR off the page's thread, for MokaQRScanner. A classic worker, so jsQR's UMD wrapper
// sees no AMD loader the page may have (Monaco brings one) and always sets self.jsQR.
importScripts('jsqr.js');

const jsQR = typeof self.jsQR === 'function' ? self.jsQR : self.jsQR && self.jsQR.default;

self.onmessage = e => {
	const { id, pixels, width, height } = e.data;
	let text = null;
	try {
		// jsQR keeps the options of the last call, so every call names them.
		const code = jsQR(new Uint8ClampedArray(pixels), width, height, { inversionAttempts: 'attemptBoth' });
		text = code && code.data ? code.data : null;
	} catch {
		text = null;
	}

	self.postMessage({ id, text });
};

self.postMessage({ ready: typeof jsQR === 'function' });
