// Moves a board's cards with a mouse, a pen, a finger or the keyboard. Blazor's elements never move: a copy follows the
// pointer, the card's place dims, the column it would land in lights up and a bar marks the spot. Only the drop reaches
// .NET, which moves the item and draws the board again.

const boards = new WeakMap();

const START = 4;
const TOUCH_SLOP = 8;
const HOLD = 250;
const EDGE = 48;
const STEP = 12;
const BAR = 3;

// A press on one of these inside a card is the control's, not a drag.
const CONTROLS = 'a, button, input, select, textarea, label, summary, [contenteditable]:not([contenteditable="false"]), '
	+ '[role="button"], [role="link"], [role="checkbox"], [role="switch"], [role="menuitem"], [data-moka-no-drag]';

export function bindBoard(root, dotnet, options) {
	if (!root || boards.has(root)) return;

	const text = options ?? {};
	let pending = null;
	let drag = null;
	let picked = null;

	const columns = () => [...root.querySelectorAll('[data-column]')].filter(c => c.closest('.moka-kanban') === root);
	const bodyOf = column => column.querySelector(':scope > .moka-kanban__column-body');
	const cardsOf = column => [...(bodyOf(column)?.querySelectorAll(':scope > [data-index]') ?? [])];
	const cardOf = el => {
		const card = el?.closest?.('[data-index]');
		return card && card.closest('.moka-kanban') === root ? card : null;
	};
	const columnIndexOf = card => Number(card.closest('[data-column]').dataset.column);

	const marker = document.createElement('div');
	marker.setAttribute('aria-hidden', 'true');
	marker.className = 'moka-kanban-marker';
	Object.assign(marker.style, {
		position: 'fixed', zIndex: 'var(--moka-z-popover)', pointerEvents: 'none', display: 'none', height: `${BAR}px`,
		background: 'var(--moka-color-primary)', borderRadius: 'var(--moka-radius-full)',
	});

	let lit = null;
	const light = column => {
		if (lit === column) return;
		lit?.removeAttribute('data-moka-drop');
		lit = column;
		lit?.setAttribute('data-moka-drop', '');
	};

	// Where a card would land: the place among the column's other cards, and a bar between them.
	const show = (column, index, card) => {
		light(column);
		const body = bodyOf(column);
		if (!body) return;
		const others = cardsOf(column).filter(c => c !== card);
		const box = body.getBoundingClientRect();
		const style = getComputedStyle(body);
		const gap = parseFloat(style.rowGap) || 0;
		const left = box.left + (parseFloat(style.paddingLeft) || 0);
		const width = body.clientWidth - (parseFloat(style.paddingLeft) || 0) - (parseFloat(style.paddingRight) || 0);
		let y;
		if (others.length === 0) {
			y = box.top + (parseFloat(style.paddingTop) || 0);
		} else if (index < others.length) {
			y = others[index].getBoundingClientRect().top - gap / 2;
		} else {
			y = others[others.length - 1].getBoundingClientRect().bottom + gap / 2;
		}
		if (!marker.isConnected) (root.closest('.moka-root') ?? document.body).append(marker);
		Object.assign(marker.style, { display: 'block', left: `${left}px`, top: `${y - BAR / 2}px`, width: `${width}px` });
	};

	const hide = () => {
		marker.style.display = 'none';
		light(null);
	};

	const announce = message => {
		const status = root.querySelector(':scope > .moka-kanban__status');
		if (!status || !message) return;
		status.textContent = '';
		requestAnimationFrame(() => { status.textContent = message; });
	};

	const words = (template, card, column, index) => {
		if (!template) return '';
		const titles = columns().map(c => c.querySelector('.moka-kanban__column-title')?.textContent?.trim() ?? '');
		const others = cardsOf(columns()[column]).filter(c => c !== card).length;
		return template
			.replaceAll('{item}', nameOf(card))
			.replaceAll('{column}', titles[column] ?? '')
			.replaceAll('{position}', String(index + 1))
			.replaceAll('{count}', String(others + 1));
	};

	const send = (fromColumn, fromIndex, toColumn, toIndex, byKeyboard) =>
		dotnet.invokeMethodAsync('Move', fromColumn, fromIndex, toColumn, toIndex, byKeyboard).catch(() => { });

	// Pointer: a press becomes a drag after a few pixels, or on a finger after a short hold, so a swipe still scrolls.
	const onDown = e => {
		if (drag || pending || picked || root.hasAttribute('data-moka-off')) return;
		if (e.pointerType === 'mouse' && e.button !== 0) return;
		const card = cardOf(e.target);
		if (!card) return;
		const control = e.target.closest?.(CONTROLS);
		if (control && card.contains(control) && control !== card) return;
		if (e.pointerType === 'mouse') {
			e.preventDefault();
			card.focus({ preventScroll: true });
		}
		pending = { card, pointer: e.pointerId, touch: e.pointerType === 'touch', x: e.clientX, y: e.clientY, timer: 0 };
		if (pending.touch) pending.timer = setTimeout(() => pending && lift(pending.x, pending.y), HOLD);
		window.addEventListener('pointermove', onMove);
		window.addEventListener('pointerup', onUp);
		window.addEventListener('pointercancel', onCancel);
	};

	const lift = (x, y) => {
		const { card, pointer, touch } = pending;
		clearTimeout(pending.timer);
		pending = null;
		const rect = card.getBoundingClientRect();
		const ghost = copyOf(card, rect);
		(root.closest('.moka-root') ?? document.body).append(ghost);
		card.setAttribute('data-moka-lifted', '');
		root.setAttribute('data-moka-dragging', '');
		const fromColumn = columnIndexOf(card);
		drag = {
			card, ghost, pointer, touch, fromColumn, fromIndex: Number(card.dataset.index),
			dx: x - rect.left, dy: y - rect.top, x, y, target: null, frame: 0,
		};
		if (touch) window.addEventListener('touchmove', holdStill, { passive: false });
		document.addEventListener('keydown', onEscape, true);
		drag.frame = requestAnimationFrame(scroll);
		announce(words(text.pickedUp, card, fromColumn, drag.fromIndex));
		place();
	};

	const onMove = e => {
		if (pending && e.pointerId === pending.pointer) {
			const moved = Math.hypot(e.clientX - pending.x, e.clientY - pending.y);
			if (pending.touch) {
				if (moved > TOUCH_SLOP) end();
			} else if (moved > START) {
				lift(e.clientX, e.clientY);
			}
			return;
		}
		if (!drag || e.pointerId !== drag.pointer) return;
		drag.x = e.clientX;
		drag.y = e.clientY;
		place();
	};

	const place = () => {
		drag.ghost.style.left = `${drag.x - drag.dx}px`;
		drag.ghost.style.top = `${drag.y - drag.dy}px`;
		const column = document.elementFromPoint(drag.x, drag.y)?.closest?.('[data-column]');
		if (!column || column.closest('.moka-kanban') !== root) {
			drag.target = null;
			hide();
			return;
		}
		const others = cardsOf(column).filter(c => c !== drag.card);
		let index = others.findIndex(c => {
			const r = c.getBoundingClientRect();
			return drag.y < r.top + r.height / 2;
		});
		if (index < 0) index = others.length;
		const toColumn = Number(column.dataset.column);
		if (toColumn === drag.fromColumn && index === drag.fromIndex) {
			drag.target = null;
			marker.style.display = 'none';
			light(column);
			return;
		}
		drag.target = { column: toColumn, index };
		show(column, index, drag.card);
	};

	// Near the board's sides it scrolls sideways, and near a column's top or bottom that column scrolls.
	const scroll = () => {
		if (!drag) return;
		const board = root.getBoundingClientRect();
		const sideways = drag.x < board.left + EDGE ? -STEP : drag.x > board.right - EDGE ? STEP : 0;
		let moved = false;
		if (sideways && root.scrollWidth > root.clientWidth) {
			const before = root.scrollLeft;
			root.scrollLeft += sideways;
			moved = root.scrollLeft !== before;
		}
		const body = document.elementFromPoint(drag.x, drag.y)?.closest?.('.moka-kanban__column-body');
		if (body && body.closest('.moka-kanban') === root && body.scrollHeight > body.clientHeight) {
			const box = body.getBoundingClientRect();
			const down = drag.y < box.top + EDGE ? -STEP : drag.y > box.bottom - EDGE ? STEP : 0;
			if (down) {
				const before = body.scrollTop;
				body.scrollTop += down;
				moved ||= body.scrollTop !== before;
			}
		}
		if (moved) place();
		drag.frame = requestAnimationFrame(scroll);
	};

	const holdStill = e => e.preventDefault();

	const end = () => {
		if (pending) clearTimeout(pending.timer);
		pending = null;
		window.removeEventListener('pointermove', onMove);
		window.removeEventListener('pointerup', onUp);
		window.removeEventListener('pointercancel', onCancel);
		if (!drag) return null;
		const d = drag;
		drag = null;
		cancelAnimationFrame(d.frame);
		window.removeEventListener('touchmove', holdStill);
		document.removeEventListener('keydown', onEscape, true);
		d.ghost.remove();
		d.card.removeAttribute('data-moka-lifted');
		root.removeAttribute('data-moka-dragging');
		hide();
		return d;
	};

	const onUp = e => {
		if (pending && e.pointerId === pending.pointer) {
			end();
			return;
		}
		if (!drag || e.pointerId !== drag.pointer) return;
		const d = end();
		if (d.target && d.card.isConnected) {
			announce(words(text.dropped, d.card, d.target.column, d.target.index));
			send(d.fromColumn, d.fromIndex, d.target.column, d.target.index, false);
		} else {
			announce(words(text.cancelled, d.card, d.fromColumn, d.fromIndex));
		}
	};

	const onCancel = e => {
		if ((pending && e.pointerId === pending.pointer) || (drag && e.pointerId === drag.pointer)) end();
	};

	const onEscape = e => {
		if (e.key !== 'Escape' || !drag) return;
		e.preventDefault();
		e.stopPropagation();
		const d = end();
		announce(words(text.cancelled, d.card, d.fromColumn, d.fromIndex));
	};

	// Keyboard: Space picks the focused card up, the arrows and Home and End move where it would land, Space or Enter
	// drops it and Escape puts it back. Only the drop reaches .NET.
	const onKey = e => {
		if (root.hasAttribute('data-moka-off') || drag) return;
		const card = cardOf(e.target);
		if (!card || e.target !== card || e.altKey || e.ctrlKey || e.metaKey) return;

		if (!picked) {
			if (e.key !== ' ') return;
			e.preventDefault();
			const column = columnIndexOf(card);
			picked = { card, fromColumn: column, fromIndex: Number(card.dataset.index), column, index: Number(card.dataset.index) };
			card.setAttribute('data-moka-picked', '');
			light(columns()[column]);
			announce(words(text.pickedUp, card, column, picked.index));
			return;
		}

		if (card !== picked.card) return;
		const all = columns();
		const room = column => cardsOf(all[column]).filter(c => c !== picked.card).length;
		let { column, index } = picked;
		switch (e.key) {
			case 'ArrowUp': index = Math.max(0, index - 1); break;
			case 'ArrowDown': index = Math.min(room(column), index + 1); break;
			case 'ArrowLeft': column = Math.max(0, column - 1); index = Math.min(index, room(column)); break;
			case 'ArrowRight': column = Math.min(all.length - 1, column + 1); index = Math.min(index, room(column)); break;
			case 'Home': index = 0; break;
			case 'End': index = room(column); break;
			case ' ':
			case 'Enter': {
				e.preventDefault();
				const p = release();
				if (p.column !== p.fromColumn || p.index !== p.fromIndex) {
					announce(words(text.dropped, p.card, p.column, p.index));
					send(p.fromColumn, p.fromIndex, p.column, p.index, true);
				} else {
					announce(words(text.dropped, p.card, p.column, p.index));
				}
				return;
			}
			case 'Escape': {
				e.preventDefault();
				e.stopPropagation();
				const p = release();
				announce(words(text.cancelled, p.card, p.fromColumn, p.fromIndex));
				return;
			}
			case 'Tab': {
				const p = release();
				announce(words(text.cancelled, p.card, p.fromColumn, p.fromIndex));
				return;
			}
			default: return;
		}
		e.preventDefault();
		picked.column = column;
		picked.index = index;
		if (column === picked.fromColumn && index === picked.fromIndex) {
			marker.style.display = 'none';
			light(all[column]);
		} else {
			show(all[column], index, picked.card);
			all[column].scrollIntoView({ block: 'nearest', inline: 'nearest' });
		}
		announce(words(text.moving, picked.card, column, index));
	};

	const release = () => {
		const p = picked;
		picked = null;
		p.card.removeAttribute('data-moka-picked');
		hide();
		return p;
	};

	const onBlur = e => {
		if (picked && e.target === picked.card) {
			const p = release();
			announce(words(text.cancelled, p.card, p.fromColumn, p.fromIndex));
		}
	};

	root.addEventListener('pointerdown', onDown);
	root.addEventListener('keydown', onKey);
	root.addEventListener('focusout', onBlur);
	boards.set(root, {
		dispose() {
			end();
			if (picked) release();
			marker.remove();
			root.removeEventListener('pointerdown', onDown);
			root.removeEventListener('keydown', onKey);
			root.removeEventListener('focusout', onBlur);
		},
	});
}

export function unbindBoard(root) {
	boards.get(root)?.dispose();
	boards.delete(root);
}

// After a card moves from the keyboard, focus follows it to its new place.
export function focusCard(root, column, index) {
	const card = [...(root?.querySelectorAll(`[data-column="${column}"] > .moka-kanban__column-body > [data-index="${index}"]`) ?? [])]
		.find(c => c.closest('.moka-kanban') === root);
	card?.focus();
}

function nameOf(card) {
	const named = card.getAttribute('aria-label');
	if (named) return named;
	const text = (card.textContent ?? '').replace(/\s+/g, ' ').trim();
	return text.length > 80 ? `${text.slice(0, 79)}…` : text;
}

function copyOf(card, rect) {
	const ghost = card.cloneNode(true);
	for (const el of [ghost, ...ghost.querySelectorAll('[id], [data-index]')]) {
		el.removeAttribute('id');
		el.removeAttribute('data-index');
	}
	for (const el of ghost.querySelectorAll('[style*="anchor-name"]')) {
		el.style.removeProperty('anchor-name');
	}
	ghost.removeAttribute('data-moka-lifted');
	ghost.removeAttribute('tabindex');
	ghost.setAttribute('aria-hidden', 'true');
	ghost.setAttribute('data-moka-ghost', '');
	ghost.inert = true;
	Object.assign(ghost.style, {
		position: 'fixed', left: `${rect.left}px`, top: `${rect.top}px`, width: `${rect.width}px`, height: `${rect.height}px`,
		margin: '0', boxSizing: 'border-box', pointerEvents: 'none', zIndex: 'var(--moka-z-popover)',
	});
	return ghost;
}
