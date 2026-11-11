// MokaCountrySelect: the browser's country for the suggestions, and the flag grid's keys, pointer and columns.
//
// The grid keeps its highlight here, as moka-hover.js does for lists: a key or a mouse move only moves an attribute,
// and .NET hears once, when a tile is picked (blazor/performance/javascript-interoperability.md, "Avoid excessively
// fine-grained calls"). It also reports how many tiles fit in a row, only when that changes, because the grid's rows
// are rendered by .NET as ARIA rows.

const grids = new Map();

/**
 * The country of the browser's language, such as "SE" for sv-SE, or null when it names none.
 * @returns {string|null}
 */
export function browserCountry() {
	try {
		const region = new Intl.Locale(navigator.language).maximize().region;
		return /^[A-Z]{2}$/.test(region ?? '') ? region : null;
	} catch {
		return null;
	}
}

/**
 * Starts the grid's keys, pointer and column count.
 * @param {HTMLElement} root - The panel around the search box and the grid.
 * @param {HTMLInputElement} search - The search box, a combobox controlling the grid.
 * @param {HTMLElement} grid - The role="grid" element.
 * @param {HTMLElement} ruler - An element as wide as a tile's least width.
 * @param {object} dotNetRef - Receives PickFromGrid(code) and SetGridColumns(count).
 */
export function bindGrid(root, search, grid, ruler, dotNetRef) {
	if (!root || !search || !grid || grids.has(grid)) return;

	const abort = new AbortController();
	const on = { signal: abort.signal };
	const state = { lit: null, inGrid: false, columns: 0 };

	const tiles = () => [...grid.querySelectorAll('[data-moka-code]')];
	const rowOf = tile => [...tile.parentElement.querySelectorAll('[data-moka-code]')];
	const columnCount = () => Math.max(1, grid.querySelector('[role=row]')?.querySelectorAll('[data-moka-code]').length ?? 1);

	const setInGrid = inGrid => {
		state.inGrid = inGrid;
		root.toggleAttribute('data-moka-in-grid', inGrid);
		if (inGrid && state.lit) search.setAttribute('aria-activedescendant', state.lit.id);
		else search.removeAttribute('aria-activedescendant');
	};

	const light = (tile, reveal) => {
		if (state.lit && state.lit !== tile) state.lit.removeAttribute('data-moka-lit');
		state.lit = tile ?? null;
		if (!tile) {
			grid.removeAttribute('aria-activedescendant');
			search.removeAttribute('aria-activedescendant');
			return;
		}

		tile.setAttribute('data-moka-lit', '');
		grid.setAttribute('aria-activedescendant', tile.id);
		if (state.inGrid) search.setAttribute('aria-activedescendant', tile.id);
		if (reveal) tile.scrollIntoView({ block: 'nearest' });
	};

	const startTile = () => {
		const all = tiles();
		return all.find(tile => tile.getAttribute('aria-selected') === 'true' && !search.value.trim()) ?? all[0] ?? null;
	};

	const pick = tile => {
		if (!tile) return;
		dotNetRef.invokeMethodAsync('PickFromGrid', tile.dataset.mokaCode).catch(() => { });
	};

	// Moves the highlight; returns false when the key isn't one the grid takes.
	const move = (key, ctrl) => {
		const all = tiles();
		if (all.length === 0) return false;

		const current = state.lit && state.lit.isConnected ? state.lit : startTile();
		const index = all.indexOf(current);
		const columns = columnCount();
		let next;
		switch (key) {
			case 'ArrowRight': next = Math.min(all.length - 1, index + 1); break;
			case 'ArrowLeft': next = Math.max(0, index - 1); break;
			case 'ArrowDown': next = index + columns < all.length ? index + columns : index; break;
			case 'ArrowUp': next = index - columns >= 0 ? index - columns : index; break;
			case 'Home': next = ctrl ? 0 : all.indexOf(rowOf(current)[0]); break;
			case 'End': next = ctrl ? all.length - 1 : all.indexOf(rowOf(current).at(-1)); break;
			case 'PageDown': next = Math.min(all.length - 1, index + columns * 4); break;
			case 'PageUp': next = Math.max(0, index - columns * 4); break;
			default: return false;
		}

		light(all[next], true);
		return true;
	};

	grid.addEventListener('keydown', e => {
		if (e.key === 'Enter' || e.key === ' ') {
			e.preventDefault();
			pick(state.lit ?? startTile());
			return;
		}

		if (move(e.key, e.ctrlKey || e.metaKey)) e.preventDefault();
	}, on);

	grid.addEventListener('focus', () => {
		if (!state.lit || !state.lit.isConnected) light(startTile(), true);
	}, on);

	// From the search box, Down goes into the grid, and once there the arrows move in both directions. Up from the
	// first row hands the keys back to the search box. Anything else is typing.
	search.addEventListener('keydown', e => {
		if (e.isComposing) return;

		if (e.key === 'Enter') {
			if (state.lit) {
				e.preventDefault();
				pick(state.lit);
			}

			return;
		}

		if (!state.inGrid) {
			if (e.key === 'ArrowDown' && tiles().length > 0) {
				e.preventDefault();
				setInGrid(true);
				light(state.lit && state.lit.isConnected ? state.lit : startTile(), true);
			}

			return;
		}

		if (e.key === 'ArrowUp' && state.lit && tiles().indexOf(state.lit) < columnCount()) {
			e.preventDefault();
			setInGrid(false);
			return;
		}

		if (['ArrowDown', 'ArrowUp', 'ArrowLeft', 'ArrowRight'].includes(e.key)) {
			e.preventDefault();
			move(e.key, false);
			return;
		}

		setInGrid(false);
	}, on);

	search.addEventListener('blur', () => setInGrid(false), on);

	grid.addEventListener('pointermove', e => {
		const tile = e.target.closest?.('[data-moka-code]');
		if (tile && tile !== state.lit) light(tile, false);
	}, { ...on, passive: true });

	grid.addEventListener('click', e => {
		const tile = e.target.closest?.('[data-moka-code]');
		if (!tile) return;
		light(tile, false);
		pick(tile);
	}, on);

	// .NET redraws the tiles when the search or the columns change. The highlight stays on the same country if it is
	// still there, else goes to the first tile of a search.
	const observer = new MutationObserver(() => {
		const all = tiles();
		const code = state.lit?.dataset.mokaCode;
		const same = code ? all.find(tile => tile.dataset.mokaCode === code) : null;
		if (same) light(same, false);
		else if (search.value.trim()) light(all[0] ?? null, false);
		else light(null, false);
	});
	observer.observe(grid, { childList: true, subtree: true });

	const measure = () => {
		const tileWidth = ruler?.getBoundingClientRect().width || 58;
		const row = grid.querySelector('[role=row]');
		const gap = row ? parseFloat(getComputedStyle(row).columnGap) || 0 : 0;
		const style = getComputedStyle(grid);
		const width = grid.clientWidth - (parseFloat(style.paddingLeft) || 0) - (parseFloat(style.paddingRight) || 0);
		if (width <= 0) return;
		const columns = Math.max(2, Math.min(12, Math.floor((width + gap) / (tileWidth + gap))));
		if (columns !== state.columns) {
			state.columns = columns;
			dotNetRef.invokeMethodAsync('SetGridColumns', columns).catch(() => { });
		}
	};

	const resizeObserver = new ResizeObserver(measure);
	resizeObserver.observe(grid);
	measure();

	grids.set(grid, () => {
		abort.abort();
		observer.disconnect();
		resizeObserver.disconnect();
		root.removeAttribute('data-moka-in-grid');
	});
}

/**
 * Stops what bindGrid started.
 * @param {HTMLElement} grid - The grid bindGrid was given.
 */
export function unbindGrid(grid) {
	const stop = grids.get(grid);
	if (!stop) return;
	grids.delete(grid);
	stop();
}
