const editors = new WeakMap();

// The colored copy and the problem marks scroll with the textarea in both directions; the line numbers only up and
// down. data-moka-live tells the stylesheet a copy is there to show, so the textarea's own text can go transparent.
export function bindEditor(root, textarea, gutter, layers) {
	if (!textarea || editors.has(textarea)) {
		return;
	}

	const sync = () => {
		for (const layer of layers) {
			if (layer) {
				layer.scrollTop = textarea.scrollTop;
				layer.scrollLeft = textarea.scrollLeft;
			}
		}

		if (gutter) {
			gutter.scrollTop = textarea.scrollTop;
		}
	};

	textarea.addEventListener('scroll', sync, { passive: true });
	editors.set(textarea, sync);
	root?.setAttribute('data-moka-live', '');
	sync();
}

export function unbindEditor(textarea) {
	const sync = textarea ? editors.get(textarea) : null;
	if (sync) {
		textarea.removeEventListener('scroll', sync);
		editors.delete(textarea);
	}
}

// Selects a stretch of text and scrolls it into view a third of the way down, as editors do for a jump to a problem.
export function selectRange(textarea, start, end) {
	if (!textarea) {
		return;
	}

	textarea.focus();
	textarea.setSelectionRange(start, end);
	const line = textarea.value.slice(0, start).split('\n').length - 1;
	const lineHeight = parseFloat(getComputedStyle(textarea).lineHeight) || 20;
	const top = line * lineHeight;
	if (top < textarea.scrollTop || top > textarea.scrollTop + textarea.clientHeight - lineHeight * 2) {
		textarea.scrollTop = Math.max(0, top - textarea.clientHeight / 3);
	}

	editors.get(textarea)?.();
}
