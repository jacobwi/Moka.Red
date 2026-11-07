// The camera, the frames and the decoding all stay in the browser. .NET hears only a decoded text, and starts,
// resumes and stops the scan.

const sessions = new WeakMap();

// A code that stays in view is reported once. It counts as gone after this many frames without it, since a
// blurred frame or two doesn't mean it left.
const framesToForget = 3;

const workerUrl = new URL('../moka-qr-worker.js', import.meta.url);
const jsqrUrl = new URL('../jsqr.js', import.meta.url);

let decoderPromise = null;

function getDecoder() {
	decoderPromise ??= createDecoder();
	return decoderPromise;
}

// The browser's own detector where it reads QR codes, else jsQR in a worker, else jsQR on this thread. The
// jsQR file is fetched only here, the first time something is scanned.
async function createDecoder() {
	return (await nativeDecoder()) ?? (await workerDecoder()) ?? (await inlineDecoder());
}

async function nativeDecoder() {
	const Detector = globalThis.BarcodeDetector;
	if (!Detector) {
		return null;
	}

	try {
		const formats = await Detector.getSupportedFormats();
		if (!formats.includes('qr_code')) {
			return null;
		}

		const detector = new Detector({ formats: ['qr_code'] });
		return {
			async decode(frame) {
				const codes = await detector.detect(frame.canvas);
				return codes.length > 0 && codes[0].rawValue ? codes[0].rawValue : null;
			}
		};
	} catch {
		return null;
	}
}

function workerDecoder() {
	return new Promise(resolve => {
		let worker;
		try {
			worker = new Worker(workerUrl);
		} catch {
			resolve(null);
			return;
		}

		const pending = new Map();
		let next = 1;
		let ready = false;
		let dead = false;

		const fail = () => {
			for (const answer of pending.values()) {
				answer(null);
			}

			pending.clear();
		};

		const decoder = {
			decode(frame) {
				if (dead) {
					return Promise.resolve(null);
				}

				const image = frame.context.getImageData(0, 0, frame.width, frame.height);
				return new Promise(answer => {
					const id = next++;
					pending.set(id, answer);
					worker.postMessage({ id, pixels: image.data.buffer, width: image.width, height: image.height },
						[image.data.buffer]);
				});
			}
		};

		const giveUp = setTimeout(() => {
			worker.terminate();
			resolve(null);
		}, 5000);

		worker.onerror = e => {
			e.preventDefault();
			dead = true;
			fail();
			if (!ready) {
				clearTimeout(giveUp);
				worker.terminate();
				resolve(null);
			}
		};

		worker.onmessage = e => {
			if ('ready' in e.data) {
				clearTimeout(giveUp);
				ready = e.data.ready;
				if (ready) {
					resolve(decoder);
				} else {
					worker.terminate();
					resolve(null);
				}

				return;
			}

			const answer = pending.get(e.data.id);
			if (answer) {
				pending.delete(e.data.id);
				answer(e.data.text);
			}
		};
	});
}

// Where a worker can't start (a content security policy without worker-src, an app scheme that refuses it).
async function inlineDecoder() {
	try {
		await import(jsqrUrl.href);
	} catch {
		return null;
	}

	const jsQR = typeof globalThis.jsQR === 'function' ? globalThis.jsQR : globalThis.jsQR?.default;
	if (typeof jsQR !== 'function') {
		return null;
	}

	return {
		async decode(frame) {
			try {
				const image = frame.context.getImageData(0, 0, frame.width, frame.height);
				const code = jsQR(image.data, image.width, image.height, { inversionAttempts: 'attemptBoth' });
				return code && code.data ? code.data : null;
			} catch {
				return null;
			}
		}
	};
}

// Draws a picture, scaled down to maxSide, on white: a transparent image reads as black to jsQR.
function draw(source, width, height, maxSide, canvas) {
	const scale = Math.min(1, maxSide / Math.max(width, height));
	const w = Math.max(1, Math.round(width * scale));
	const h = Math.max(1, Math.round(height * scale));
	if (canvas.width !== w || canvas.height !== h) {
		canvas.width = w;
		canvas.height = h;
	}

	const context = canvas.getContext('2d', { willReadFrequently: true });
	context.fillStyle = '#fff';
	context.fillRect(0, 0, w, h);
	context.drawImage(source, 0, 0, w, h);
	return { canvas, context, width: w, height: h };
}

function problemFor(error) {
	switch (error && error.name) {
		case 'NotAllowedError':
		case 'PermissionDeniedError':
		case 'SecurityError':
			return 'denied';
		case 'NotFoundError':
		case 'DevicesNotFoundError':
		case 'OverconstrainedError':
			return 'missing';
		case 'NotReadableError':
		case 'TrackStartError':
		case 'AbortError':
			return 'busy';
		default:
			return 'failed';
	}
}

function endSession(session) {
	session.stopped = true;
	clearTimeout(session.timer);
	if (session.stream) {
		session.stream.getTracks().forEach(track => track.stop());
		session.stream = null;
	}

	if (session.video.srcObject) {
		session.video.srcObject = null;
	}
}

function schedule(session) {
	if (!session.stopped && !session.paused) {
		session.timer = setTimeout(() => scan(session), session.interval);
	}
}

async function scan(session) {
	const video = session.video;
	let text = null;
	if (video.videoWidth && video.videoHeight && !document.hidden) {
		try {
			text = await session.decoder.decode(draw(video, video.videoWidth, video.videoHeight, session.maxSide, session.canvas));
		} catch {
			text = null;
		}
	}

	if (session.stopped) {
		return;
	}

	if (text && text === session.last) {
		session.misses = 0;
	} else if (!text) {
		if (session.last && ++session.misses >= framesToForget) {
			session.last = null;
		}
	} else {
		session.last = text;
		session.misses = 0;
		session.paused = true;
		try {
			await session.dotNetRef.invokeMethodAsync('OnCodeFound', text);
		} catch {
			endSession(session);
		}

		return;
	}

	schedule(session);
}

/**
 * Starts the camera into the video element and scans its frames. Resolves to 'started', or to why it couldn't:
 * 'denied', 'missing', 'busy', 'insecure', 'unsupported', 'failed', or 'stopped' when stopScanner came first.
 */
export async function startScanner(video, dotNetRef, options) {
	const previous = sessions.get(video);
	if (previous) {
		endSession(previous);
	}

	const session = {
		video,
		dotNetRef,
		stream: null,
		decoder: null,
		timer: 0,
		stopped: false,
		paused: false,
		last: null,
		misses: 0,
		maxSide: (options && options.maxSide) || 720,
		interval: (options && options.interval) || 200,
		canvas: document.createElement('canvas')
	};
	sessions.set(video, session);

	if (!navigator.mediaDevices || !navigator.mediaDevices.getUserMedia) {
		sessions.delete(video);
		return window.isSecureContext === false ? 'insecure' : 'unsupported';
	}

	const decoder = await getDecoder();
	if (session.stopped) {
		return 'stopped';
	}

	if (!decoder) {
		sessions.delete(video);
		return 'unsupported';
	}

	let stream;
	try {
		stream = await navigator.mediaDevices.getUserMedia({
			video: { facingMode: { ideal: 'environment' }, width: { ideal: 1280 }, height: { ideal: 720 } },
			audio: false
		});
	} catch (error) {
		if (sessions.get(video) === session) {
			sessions.delete(video);
		}

		return session.stopped ? 'stopped' : problemFor(error);
	}

	if (session.stopped) {
		stream.getTracks().forEach(track => track.stop());
		return 'stopped';
	}

	session.stream = stream;
	session.decoder = decoder;
	video.muted = true;
	video.setAttribute('playsinline', '');
	video.srcObject = stream;
	try {
		await video.play();
	} catch {
		// Autoplay rules can refuse play() on a muted stream in some embedded views; the frames still arrive.
	}

	if (session.stopped) {
		return 'stopped';
	}

	schedule(session);
	return 'started';
}

/** Goes on scanning after .NET has looked at a reported code. That code isn't reported again while it stays in view. */
export function resumeScanner(video) {
	const session = sessions.get(video);
	if (session && !session.stopped && session.paused) {
		session.paused = false;
		schedule(session);
	}
}

/** Stops scanning and turns the camera off. */
export function stopScanner(video) {
	const session = sessions.get(video);
	if (session) {
		endSession(session);
		sessions.delete(video);
	}
}

async function loadImage(blob) {
	try {
		return await createImageBitmap(blob);
	} catch {
		// Chrome's createImageBitmap refuses SVG, which an image element draws.
	}

	const url = URL.createObjectURL(blob);
	try {
		const image = new Image();
		image.src = url;
		await image.decode();
		return image;
	} catch {
		return null;
	} finally {
		URL.revokeObjectURL(url);
	}
}

/** Reads a QR code from an image (a File or a Blob). Resolves to its text, or null when there is none or no decoder. */
export async function decodeImage(blob) {
	const decoder = await getDecoder();
	const image = decoder && blob ? await loadImage(blob) : null;
	if (!image) {
		return null;
	}

	try {
		const width = image.naturalWidth || image.width;
		const height = image.naturalHeight || image.height;
		return await decoder.decode(draw(image, width, height, 1600, document.createElement('canvas')));
	} catch {
		return null;
	} finally {
		if (typeof image.close === 'function') {
			image.close();
		}
	}
}

/** Reads the picked file of a file input, then clears it so the same file can be picked again. Resolves to [name, text]. */
export async function decodePickedImage(input) {
	const file = input && input.files && input.files[0];
	if (!file) {
		return null;
	}

	try {
		return [file.name, await decodeImage(file)];
	} finally {
		input.value = '';
	}
}
