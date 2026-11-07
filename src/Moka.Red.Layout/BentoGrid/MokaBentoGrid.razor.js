// Drags a board's cards by their grips, with a mouse, a pen or a finger. Blazor's elements never move: a copy
// follows the pointer and a bar marks where the card would land, and only the drop reaches .NET, which redraws
// the board in its new order.

const boards = new WeakMap();

const EDGE = 60;
const SCROLL_STEP = 12;
const BAR = 4;

export function bindBoard(root, dotnet) {
	if (!root || boards.has(root)) return;

	let drag = null;

	const cardsOf = () => root.querySelector(':scope > .moka-bento-grid__cards');

	// A card of this board, not of a board inside one of its cards.
	const cardOf = el => {
		const cards = cardsOf();
		for (let card = el?.closest?.('[data-card]'); card; card = card.parentElement?.closest('[data-card]')) {
			if (card.closest('.moka-bento-grid__cards') === cards) return card;
		}
		return null;
	};

	const onDown = e => {
		if (drag || e.button > 0) return;
		const grip = e.target.closest?.('[data-tool="grip"]');
		const item = grip ? cardOf(grip) : null;
		if (!item || grip.closest('[data-card]') !== item) return;
		e.preventDefault();

		const cards = cardsOf();
		const rect = item.getBoundingClientRect();
		const gaps = getComputedStyle(cards);
		const ghost = copyOf(item, rect);
		const marker = document.createElement('div');
		marker.setAttribute('aria-hidden', 'true');
		Object.assign(marker.style, {
			position: 'fixed', zIndex: 'var(--moka-z-popover)', pointerEvents: 'none', display: 'none',
			background: 'var(--moka-color-primary)', borderRadius: 'var(--moka-radius-full)',
		});
		// Inside the theme's root the copy keeps the look's rules and tokens. Body is the fallback.
		(root.closest('.moka-root') ?? document.body).append(ghost, marker);
		item.setAttribute('data-lifted', '');

		drag = {
			grip, item, ghost, marker, cards,
			pointer: e.pointerId,
			columnGap: parseFloat(gaps.columnGap) || 0,
			rowGap: parseFloat(gaps.rowGap) || 0,
			dx: e.clientX - rect.left, dy: e.clientY - rect.top,
			x: e.clientX, y: e.clientY,
			target: null, before: false,
			scroller: scrollerOf(root),
		};

		// The moves are heard on the window, so the drag carries on where capturing the pointer isn't allowed.
		try {
			grip.setPointerCapture(e.pointerId);
			grip.addEventListener('lostpointercapture', onCancel);
		} catch {
		}
		window.addEventListener('pointermove', onMove);
		window.addEventListener('pointerup', onUp);
		window.addEventListener('pointercancel', onCancel);
		document.addEventListener('keydown', onEscape, true);
		drag.frame = requestAnimationFrame(scroll);
	};

	const onMove = e => {
		if (!drag || e.pointerId !== drag.pointer) return;
		drag.x = e.clientX;
		drag.y = e.clientY;
		drag.ghost.style.left = `${e.clientX - drag.dx}px`;
		drag.ghost.style.top = `${e.clientY - drag.dy}px`;
		track();
	};

	const track = () => {
		const over = cardOf(document.elementFromPoint(drag.x, drag.y));
		if (!over) return;
		const marker = drag.marker.style;
		if (over === drag.item) {
			drag.target = null;
			marker.display = 'none';
			return;
		}

		// Cards side by side take the place left or right of the one under the pointer; full rows, above or below.
		const r = over.getBoundingClientRect();
		const sideways = r.width < drag.cards.clientWidth * 0.6;
		const before = sideways ? drag.x < r.left + r.width / 2 : drag.y < r.top + r.height / 2;
		drag.target = over;
		drag.before = before;
		marker.display = 'block';
		if (sideways) {
			const x = before ? r.left - drag.columnGap / 2 : r.right + drag.columnGap / 2;
			Object.assign(marker, { left: `${x - BAR / 2}px`, top: `${r.top}px`, width: `${BAR}px`, height: `${r.height}px` });
		} else {
			const y = before ? r.top - drag.rowGap / 2 : r.bottom + drag.rowGap / 2;
			Object.assign(marker, { left: `${r.left}px`, top: `${y - BAR / 2}px`, width: `${r.width}px`, height: `${BAR}px` });
		}
	};

	// Near the top or bottom of what scrolls, it scrolls, so a card can travel further than the screen shows.
	const scroll = () => {
		if (!drag) return;
		const { scroller } = drag;
		const view = scroller === document.scrollingElement
			? { top: 0, bottom: window.innerHeight }
			: scroller.getBoundingClientRect();
		const by = drag.y < view.top + EDGE ? -SCROLL_STEP : drag.y > view.bottom - EDGE ? SCROLL_STEP : 0;
		if (by) {
			const before = scroller.scrollTop;
			scroller.scrollBy(0, by);
			if (scroller.scrollTop !== before) track();
		}
		drag.frame = requestAnimationFrame(scroll);
	};

	const finish = () => {
		const d = drag;
		drag = null;
		cancelAnimationFrame(d.frame);
		window.removeEventListener('pointermove', onMove);
		window.removeEventListener('pointerup', onUp);
		window.removeEventListener('pointercancel', onCancel);
		d.grip.removeEventListener('lostpointercapture', onCancel);
		document.removeEventListener('keydown', onEscape, true);
		try {
			d.grip.releasePointerCapture(d.pointer);
		} catch {
		}
		d.ghost.remove();
		d.marker.remove();
		d.item.removeAttribute('data-lifted');
		return d;
	};

	const onUp = e => {
		if (!drag || e.pointerId !== drag.pointer) return;
		const d = finish();
		if (d.target && d.target !== d.item && d.target.isConnected) {
			dotnet.invokeMethodAsync('Dropped', d.item.dataset.card, d.target.dataset.card, d.before).catch(() => { });
		}
	};

	const onCancel = e => {
		if (drag && e.pointerId === drag.pointer) finish();
	};

	const onEscape = e => {
		if (e.key !== 'Escape' || !drag) return;
		e.preventDefault();
		e.stopPropagation();
		finish();
	};

	root.addEventListener('pointerdown', onDown);
	boards.set(root, {
		dispose() {
			if (drag) finish();
			root.removeEventListener('pointerdown', onDown);
		},
	});
}

export function unbindBoard(root) {
	boards.get(root)?.dispose();
	boards.delete(root);
}

// After a card moves, focus goes back to where it was: the tool on the moved card (its grip when that tool is now
// disabled), the hidden card's place in the tray, or Done.
export function focusBoard(root, card, part) {
	if (!root) return;
	let target = null;
	if (part === 'done') {
		target = root.querySelector(':scope > .moka-bento-grid__bar .moka-bento-grid__done');
	} else if (part === 'tray') {
		target = root.querySelector(`:scope > .moka-bento-grid__tray [data-tray-card="${CSS.escape(card ?? '')}"]`);
	} else {
		const cards = root.querySelector(':scope > .moka-bento-grid__cards');
		const item = [...(cards?.querySelectorAll(`[data-card="${CSS.escape(card ?? '')}"]`) ?? [])]
			.find(c => c.closest('.moka-bento-grid__cards') === cards);
		const tool = name => item?.querySelector(`:scope > .moka-bento-item__tools [data-tool="${name}"]`);
		const wanted = tool(part);
		target = wanted && !wanted.disabled ? wanted : tool('grip');
	}
	target?.focus();
}

function copyOf(item, rect) {
	const ghost = item.cloneNode(true);
	for (const el of [ghost, ...ghost.querySelectorAll('[id], [data-card]')]) {
		el.removeAttribute('id');
		el.removeAttribute('data-card');
	}
	// A tooltip's popup finds its trigger by anchor name, and the last element with a name wins: the copy's triggers
	// would take the card's popups with them.
	for (const el of ghost.querySelectorAll('[style*="anchor-name"]')) {
		el.style.removeProperty('anchor-name');
	}
	ghost.setAttribute('aria-hidden', 'true');
	ghost.inert = true;
	Object.assign(ghost.style, {
		position: 'fixed', left: `${rect.left}px`, top: `${rect.top}px`, width: `${rect.width}px`, height: `${rect.height}px`,
		margin: '0', boxSizing: 'border-box', pointerEvents: 'none', zIndex: 'var(--moka-z-popover)', opacity: '0.92',
		overflow: 'hidden', outline: 'none', transition: 'none', cursor: 'grabbing',
		boxShadow: '0 0 0 2px var(--moka-color-primary), var(--moka-shadow-popup-lg)',
	});
	return ghost;
}

function scrollerOf(el) {
	for (let node = el.parentElement; node && node !== document.body; node = node.parentElement) {
		const overflow = getComputedStyle(node).overflowY;
		if ((overflow === 'auto' || overflow === 'scroll') && node.scrollHeight > node.clientHeight) return node;
	}
	return document.scrollingElement ?? document.documentElement;
}
