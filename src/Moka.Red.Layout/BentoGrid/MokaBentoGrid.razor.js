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
			flow: root.classList.contains('moka-bento-grid--rows') ? flowOf(cards) : null,
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
		let sideways = r.width < drag.cards.clientWidth * 0.6;
		let before = sideways ? drag.x < r.left + r.width / 2 : drag.y < r.top + r.height / 2;
		// In rows that flow every card grows to fill its row, so its width says nothing. A card with others in its row
		// takes the place left or right of it, one alone in its row above or below, and the bar then shows the row the
		// dragged card will land in: beside the card when it joins that row, above or below it when it won't.
		if (drag.flow?.share.has(over)) {
			const { items, share, columns, rows } = drag.flow;
			const alone = items.every(i => i === over || rows.get(i) !== rows.get(over));
			before = alone ? drag.y < r.top + r.height / 2 : drag.x < r.left + r.width / 2;
			const order = items.filter(i => i !== drag.item);
			order.splice(order.indexOf(over) + (before ? 0 : 1), 0, drag.item);
			const landed = rowsOf(order, share, columns);
			sideways = landed.get(drag.item) === landed.get(over);
		}
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

// Rows that flow, read once when a drag starts. A slot has no box, so its children are the rows' items, in the board's
// order. The stylesheet gives each item its share of the columns as its flex-grow, and items whose shares add up to
// the columns fill a row exactly, so the rows break where those shares pass the column count.
function flowOf(cards) {
	const items = [...cards.children].flatMap(slot => [...slot.children]);
	const share = new Map(items.map(item => [item, parseFloat(getComputedStyle(item).flexGrow) || 1]));
	const columns = parseFloat(getComputedStyle(cards).getPropertyValue('--_board-columns')) || 1;
	return { items, share, columns, rows: rowsOf(items, share, columns) };
}

// The row each item falls in, counted from 0.
function rowsOf(items, share, columns) {
	const rows = new Map();
	let row = 0;
	let used = 0;
	for (const item of items) {
		const takes = share.get(item);
		if (used > 0 && used + takes > columns) {
			row++;
			used = 0;
		}
		used += takes;
		rows.set(item, row);
	}
	return rows;
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
