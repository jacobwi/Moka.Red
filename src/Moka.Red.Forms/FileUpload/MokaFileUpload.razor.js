// Loaded only when an upload asks for something only the browser can do: pictures of picked files, a check of the
// files while they are dragged over, or fitting the field look's chips on one line.

// A picture of a picked file, drawn by the browser from the file it already holds. Nothing is read or sent to .NET.
// The URL lives until the file leaves the list, when .NET hands it back to revokePreviews.
export function createPreviews(input, indexes) {
	const files = input && input.files ? input.files : [];
	return (indexes || []).map(index => {
		const file = files[index];
		return file && file.type.startsWith('image/') ? URL.createObjectURL(file) : null;
	});
}

export function revokePreviews(urls) {
	(urls || []).forEach(url => {
		if (typeof url === 'string' && url.startsWith('blob:')) {
			URL.revokeObjectURL(url);
		}
	});
}

const checks = new WeakMap();

const hasFiles = e => !!e.dataTransfer && Array.from(e.dataTransfer.types || []).includes('Files');

// While a file is only dragged, the browser gives each item's type and nothing else, not even its name, so an
// extension in accept is matched through the types it stands for. A type it can't judge counts as fitting: the
// check never turns away what the drop would take.
const extensionTypes = {
	pdf: ['application/pdf'],
	png: ['image/png'],
	jpg: ['image/jpeg'],
	jpeg: ['image/jpeg'],
	gif: ['image/gif'],
	webp: ['image/webp'],
	avif: ['image/avif'],
	svg: ['image/svg+xml'],
	bmp: ['image/bmp'],
	txt: ['text/plain'],
	csv: ['text/csv', 'application/vnd.ms-excel'],
	md: ['text/markdown'],
	json: ['application/json'],
	xml: ['application/xml', 'text/xml'],
	html: ['text/html'],
	zip: ['application/zip', 'application/x-zip-compressed'],
	doc: ['application/msword'],
	docx: ['application/vnd.openxmlformats-officedocument.wordprocessingml.document'],
	xls: ['application/vnd.ms-excel'],
	xlsx: ['application/vnd.openxmlformats-officedocument.spreadsheetml.sheet'],
	ppt: ['application/vnd.ms-powerpoint'],
	pptx: ['application/vnd.openxmlformats-officedocument.presentationml.presentation'],
	mp4: ['video/mp4'],
	mov: ['video/quicktime'],
	webm: ['video/webm'],
	mp3: ['audio/mpeg'],
	wav: ['audio/wav', 'audio/x-wav']
};

const labels = {
	'application/pdf': 'PDF',
	'image/jpeg': 'JPG',
	'image/svg+xml': 'SVG',
	'application/zip': 'ZIP',
	'application/x-zip-compressed': 'ZIP',
	'application/msword': 'DOC',
	'application/vnd.openxmlformats-officedocument.wordprocessingml.document': 'DOCX',
	'application/vnd.ms-excel': 'XLS',
	'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet': 'XLSX',
	'application/vnd.ms-powerpoint': 'PPT',
	'application/vnd.openxmlformats-officedocument.presentationml.presentation': 'PPTX',
	'text/plain': 'TXT',
	'audio/mpeg': 'MP3',
	'video/quicktime': 'MOV'
};

const labelOf = type => {
	if (labels[type]) {
		return labels[type];
	}

	const subtype = (type.split('/')[1] || '').split('+')[0].replace(/^x-/, '');
	return subtype.length > 0 && subtype.length <= 8 ? subtype.toUpperCase() : type;
};

// true, false, or null when it can't be told before the drop.
const fits = (type, tokens) => {
	if (tokens.length === 0) {
		return true;
	}

	let unknown = !type;
	for (const token of tokens) {
		if (token === '*/*') {
			return true;
		}

		if (token.startsWith('.')) {
			const types = extensionTypes[token.slice(1)];
			if (!types) {
				unknown = true;
			} else if (types.includes(type)) {
				return true;
			}
		} else if (token.endsWith('/*')) {
			if (type.startsWith(token.slice(0, -1))) {
				return true;
			}
		} else if (token === type) {
			return true;
		}
	}

	return unknown ? null : false;
};

const tokensOf = accept => (accept || '')
	.split(',')
	.map(token => token.trim().toLowerCase())
	.filter(token => token.startsWith('.') || token.includes('/'));

function judge(target, transfer) {
	const input = target.querySelector(':scope > input[type=file]');
	const files = Array.from(transfer.items || []).filter(item => item.kind === 'file');
	const tokens = tokensOf(input ? input.getAttribute('accept') : '');
	const refused = files.find(item => fits((item.type || '').toLowerCase(), tokens) === false);
	const multiple = !!(input && input.multiple);
	const room = Number(target.getAttribute('data-moka-room') ?? Number.POSITIVE_INFINITY);

	let state = 'ok';
	if (refused) {
		state = 'type';
	} else if (multiple && room <= 0) {
		state = 'full';
	} else if (multiple ? files.length > room : files.length > 1) {
		state = 'many';
	}

	const count = target.querySelector('[data-moka-check-count]');
	if (count) {
		count.textContent = files.length === 0
			? ''
			: files.length === 1
				? count.getAttribute('data-one') || ''
				: (count.getAttribute('data-many') || '').replace('{0}', String(files.length));
	}

	const type = target.querySelector('[data-moka-check-type]');
	if (type) {
		type.textContent = refused ? labelOf((refused.type || '').toLowerCase()) : '';
	}

	const types = target.querySelector('[data-moka-check-types]');
	if (types) {
		const found = [...new Set(files.map(item => item.type).filter(Boolean).map(t => labelOf(t.toLowerCase())))];
		types.textContent = found.join(', ');
	}

	target.setAttribute('data-moka-drag-check', state);
}

export function bindDragCheck(input) {
	const target = input ? input.parentElement : null;
	if (!target || checks.has(target)) {
		return;
	}

	let depth = 0;
	const clear = () => {
		depth = 0;
		target.removeAttribute('data-moka-drag-check');
	};

	const onEnter = e => {
		if (!hasFiles(e)) {
			return;
		}

		if (depth++ === 0) {
			judge(target, e.dataTransfer);
		}
	};

	const onLeave = e => {
		if (!hasFiles(e)) {
			return;
		}

		depth = Math.max(0, depth - 1);
		if (depth === 0) {
			clear();
		}
	};

	target.addEventListener('dragenter', onEnter);
	target.addEventListener('dragleave', onLeave);
	target.addEventListener('drop', clear);
	checks.set(target, { onEnter, onLeave, clear });
}

export function unbindDragCheck(input) {
	const target = input ? input.parentElement : null;
	const bound = target ? checks.get(target) : null;
	if (!bound) {
		return;
	}

	target.removeEventListener('dragenter', bound.onEnter);
	target.removeEventListener('dragleave', bound.onLeave);
	target.removeEventListener('drop', bound.clear);
	bound.clear();
	checks.delete(target);
}

const chipRows = new WeakMap();

// The field look keeps its chips on one line. The ones that wrap out of sight are taken out of Tab and the reading
// order with inert, and counted in the +N beside them.
function fitChips(row) {
	const more = row.parentElement ? row.parentElement.querySelector(':scope > .moka-fileupload-more') : null;
	const chips = Array.from(row.children);
	chips.forEach(chip => {
		chip.inert = false;
	});

	const top = chips.length > 0 ? chips[0].offsetTop : 0;
	let hidden = 0;
	chips.forEach(chip => {
		const out = chip.offsetTop !== top;
		chip.inert = out;
		if (out) {
			hidden++;
		}
	});

	if (more) {
		more.textContent = hidden > 0 ? `+${hidden}` : '';
		more.hidden = hidden === 0;
	}
}

export function bindChips(input) {
	const field = input ? input.parentElement : null;
	const row = field ? field.querySelector(':scope > .moka-fileupload-chiprow') : null;
	if (!row || chipRows.has(row)) {
		return;
	}

	const refit = () => {
		if (!row.isConnected) {
			const bound = chipRows.get(row);
			if (bound) {
				bound.resize.disconnect();
				bound.changes.disconnect();
				chipRows.delete(row);
			}

			return;
		}

		fitChips(row);
	};

	const resize = new ResizeObserver(refit);
	const changes = new MutationObserver(refit);
	resize.observe(row);
	changes.observe(row, { childList: true });
	chipRows.set(row, { resize, changes });
	fitChips(row);
}
