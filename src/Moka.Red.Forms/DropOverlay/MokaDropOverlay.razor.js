const overlays = new WeakMap();

// A drag that ends outside the page (Escape, or a drag out of the window in some browsers) can leave the
// enter and leave counts uneven. dragover keeps arriving while files are over the page, even with the
// pointer still, so a quiet second means the drag is over.
const quietMs = 1000;

const hasFiles = e => !!e.dataTransfer && Array.from(e.dataTransfer.types || []).includes('Files');

// A file input takes a drop itself, so only drops that miss one are cancelled, or the browser would open
// the file in the tab.
const isFileInput = target => target instanceof HTMLInputElement && target.type === 'file';

export function bindDropOverlay(overlay) {
	if (!overlay || overlays.has(overlay)) {
		return;
	}

	let depth = 0;
	let quiet = 0;

	const hide = () => {
		depth = 0;
		clearTimeout(quiet);
		overlay.removeAttribute('data-moka-active');
		overlay.removeAttribute('data-moka-count');
	};

	// The looks that show a count carry its words. The browser lists a dragged file's kind while it is over the page,
	// though not its name or size, and some browsers list nothing until the drop; then the plain title stays.
	const count = e => {
		const many = overlay.getAttribute('data-moka-count-many');
		const slot = many === null ? null : overlay.querySelector('[data-moka-drop-count]');
		if (!slot) {
			return;
		}

		const files = Array.from(e.dataTransfer.items || []).filter(item => item.kind === 'file').length;
		if (files === 0) {
			overlay.removeAttribute('data-moka-count');
			return;
		}

		const text = files === 1
			? overlay.getAttribute('data-moka-count-one') || ''
			: many.replace('{0}', String(files));
		if (slot.textContent !== text) {
			slot.textContent = text;
		}

		overlay.setAttribute('data-moka-count', String(files));
	};

	const keepAlive = () => {
		clearTimeout(quiet);
		quiet = setTimeout(hide, quietMs);
	};

	const onEnter = e => {
		if (!hasFiles(e)) {
			return;
		}

		depth++;
		overlay.setAttribute('data-moka-active', '');
		count(e);
		keepAlive();
	};

	const onLeave = e => {
		if (!hasFiles(e)) {
			return;
		}

		depth = Math.max(0, depth - 1);
		if (depth === 0) {
			hide();
		}
	};

	const onOver = e => {
		if (!hasFiles(e)) {
			return;
		}

		keepAlive();
		if (!isFileInput(e.target)) {
			e.preventDefault();
		}
	};

	const onDrop = e => {
		if (hasFiles(e) && !isFileInput(e.target)) {
			e.preventDefault();
		}

		setTimeout(hide, 0);
	};

	window.addEventListener('dragenter', onEnter);
	window.addEventListener('dragleave', onLeave);
	window.addEventListener('dragover', onOver);
	window.addEventListener('drop', onDrop);
	overlays.set(overlay, { onEnter, onLeave, onOver, onDrop, hide });
}

export function unbindDropOverlay(overlay) {
	const handlers = overlay ? overlays.get(overlay) : null;
	if (!handlers) {
		return;
	}

	window.removeEventListener('dragenter', handlers.onEnter);
	window.removeEventListener('dragleave', handlers.onLeave);
	window.removeEventListener('dragover', handlers.onOver);
	window.removeEventListener('drop', handlers.onDrop);
	handlers.hide();
	overlays.delete(overlay);
}
