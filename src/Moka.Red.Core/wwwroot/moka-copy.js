/**
 * Moka.Red clipboard writes, made inside the click that asks for them.
 *
 * Safari, iOS and Firefox let a page write to the clipboard only while the click that asked for it is still live
 * (transient activation), and on Blazor Server a .NET click handler runs a round trip later, so its copy was refused.
 * Here one capturing click listener on the document, installed by this package's JavaScript initializer, copies for
 * any element that says what to copy:
 *
 * - data-moka-copy: the text itself.
 * - data-moka-copy-from: a selector. The nearest ancestor holding a match supplies the text: the value of a field, or
 *   the text of every match, one per line (MokaCodeBlock's lines without their numbers).
 * - data-moka-copy-ask: the text comes from .NET, for text too long to keep in the page. The element raises
 *   mokacopyask and .NET answers through answerCopy(). The write still starts in the click, as a ClipboardItem whose
 *   text is the answer still on its way (Chromium 98+, Firefox 127+, Safari 13.1+ take a promise there).
 *
 * When the write settles the element raises mokacopied with detail.copied, which .NET hears as @onmokacopied.
 * Nothing is bound per element, so there is nothing to dispose. State lives on the elements, not in this module, so
 * a second instance of it (another URL for the same file) still answers the first one's questions.
 */

const COPY_SELECTOR = '[data-moka-copy], [data-moka-copy-from], [data-moka-copy-ask]';
const LISTENING = '_mokaCopyListening';
const WAITING = '_mokaCopyWaiting';
const ANSWER_TIMEOUT_MS = 10000;

/** Installs the document's click listener. Calling it again does nothing. */
export function listenForCopies() {
	if (typeof document === 'undefined' || document[LISTENING]) return;
	document[LISTENING] = true;

	// Capturing, so a page script that stops the click on its way up can't stop the copy.
	document.addEventListener('click', onClick, true);
}

/**
 * Copies text, preferring the async Clipboard API and falling back to the copy command where it is missing
 * (insecure origins) or refuses. Call it from inside a click: once the click is over, Safari and Firefox refuse both.
 * @param {string} text - The text to copy.
 * @param {Element} [near] - The element that asked, so the fallback stays inside an open modal dialog with it.
 * @returns {Promise<boolean>} True when the text reached the clipboard.
 */
export function copyText(text, near) {
	if (canWriteText()) {
		return navigator.clipboard.writeText(text).then(() => true, () => copyWithCommand(text, near));
	}

	return Promise.resolve(copyWithCommand(text, near));
}

/**
 * Hands an element the text it asked .NET for with mokacopyask.
 * @param {Element} element - The element that raised mokacopyask.
 * @param {string} text - The text to copy.
 */
export function answerCopy(element, text) {
	const waiting = element?.[WAITING];
	if (!waiting) return;

	delete element[WAITING];
	for (const wait of waiting) wait.settle(typeof text === 'string' ? text : '');
}

function onClick(e) {
	const element = e.target instanceof Element ? e.target.closest(COPY_SELECTOR) : null;
	if (!element || element.matches(':disabled') || element.getAttribute('aria-disabled') === 'true') return;

	const copied = element.hasAttribute('data-moka-copy-ask') ? copyAsked(element) : copyOwn(element);
	copied.then(ok => report(element, ok), () => report(element, false));
}

function copyOwn(element) {
	const text = readText(element);
	return text === null ? Promise.resolve(false) : copyText(text, element);
}

function readText(element) {
	const own = element.getAttribute('data-moka-copy');
	if (own !== null) return own;

	const selector = element.getAttribute('data-moka-copy-from');
	if (!selector) return null;

	for (let scope = element.parentElement; scope; scope = scope.parentElement) {
		let matches;
		try {
			matches = scope.querySelectorAll(selector);
		} catch {
			return null;
		}

		if (matches.length > 0) return Array.from(matches, textOf).join('\n');
	}

	return null;
}

function textOf(element) {
	if (element instanceof HTMLInputElement || element instanceof HTMLTextAreaElement) return element.value;
	return element.textContent ?? '';
}

function copyAsked(element) {
	const wait = waitForAnswer(element);
	element.dispatchEvent(new CustomEvent('mokacopyask', { bubbles: true }));

	// On WebAssembly .NET can answer before dispatchEvent returns, and then the plain write works everywhere.
	if (wait.text !== undefined) return copyText(wait.text, element);

	if (canWriteItems()) {
		try {
			const blob = wait.answer.then(text => new Blob([text], { type: 'text/plain' }));
			return navigator.clipboard.write([new ClipboardItem({ 'text/plain': blob })])
				.then(() => true, () => wait.answer.then(text => copyText(text, element), () => false));
		} catch {
			// Chromium before 98 takes no promise in a ClipboardItem.
		}
	}

	// No ClipboardItem: copy once the answer is in. Chromium still allows it for a few seconds after the click,
	// Safari and Firefox usually don't, and the result says which.
	return wait.answer.then(text => copyText(text, element), () => false);
}

function waitForAnswer(element) {
	const wait = { text: undefined, answer: null, settle: null };
	wait.answer = new Promise((resolve, reject) => {
		const timer = setTimeout(() => {
			const waiting = element[WAITING];
			if (waiting) element[WAITING] = waiting.filter(w => w !== wait);
			reject(new Error('No text arrived to copy.'));
		}, ANSWER_TIMEOUT_MS);

		wait.settle = text => {
			clearTimeout(timer);
			wait.text = text;
			resolve(text);
		};
	});

	(element[WAITING] ??= []).push(wait);
	return wait;
}

function report(element, copied) {
	element.dispatchEvent(new CustomEvent('mokacopied', { bubbles: true, detail: { copied } }));
}

function canWriteText() {
	return window.isSecureContext && typeof navigator.clipboard?.writeText === 'function';
}

function canWriteItems() {
	return window.isSecureContext && typeof ClipboardItem === 'function' &&
		typeof navigator.clipboard?.write === 'function';
}

// The copy command copies the selection, so the text goes into a span that is selected without taking focus (a
// focused field would pull focus out of the button and out of a popup that closes when focus leaves it), and the
// copy event writes exactly the text, with no markup.
function copyWithCommand(text, near) {
	const host = near?.closest?.('dialog[open]') ?? document.body;
	const span = document.createElement('span');
	span.textContent = text;
	span.setAttribute('aria-hidden', 'true');
	span.style.cssText = 'position:fixed;top:0;left:0;clip-path:inset(50%);white-space:pre;' +
		'user-select:text;-webkit-user-select:text;pointer-events:none;';

	const selection = document.getSelection();
	const ranges = [];
	for (let i = 0; i < (selection?.rangeCount ?? 0); i++) ranges.push(selection.getRangeAt(i));

	const onCopy = e => {
		e.clipboardData?.setData('text/plain', text);
		e.preventDefault();
		e.stopImmediatePropagation();
	};

	host.appendChild(span);
	document.addEventListener('copy', onCopy, true);
	try {
		const range = document.createRange();
		range.selectNodeContents(span);
		selection?.removeAllRanges();
		selection?.addRange(range);
		return document.execCommand('copy');
	} catch {
		return false;
	} finally {
		document.removeEventListener('copy', onCopy, true);
		span.remove();
		selection?.removeAllRanges();
		for (const range of ranges) selection?.addRange(range);
	}
}
