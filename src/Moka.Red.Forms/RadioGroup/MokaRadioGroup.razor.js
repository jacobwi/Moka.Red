// MokaRadioGroup's Browse mode: the cards are a listbox, and the keys and the pointer move its highlight here, as
// moka-hover.js does for lists. A move only sets an attribute. .NET hears a choice, and a new highlight only while the
// page listens for one, one call at a time with the newest waiting (blazor/performance/javascript-interoperability.md,
// "Avoid excessively fine-grained calls").

const groups = new Map();

/**
 * Starts the keys, pointer and focus of a browsed group.
 * @param {string} key - Names the group for reportBrowse and unbindBrowse.
 * @param {HTMLElement} list - The role="listbox" element.
 * @param {object} dotNetRef - Receives ChooseFromBrowse(key) and HighlightFromBrowse(key).
 * @param {boolean} report - Whether the page listens for the highlight.
 */
export function bindBrowse(key, list, dotNetRef, report) {
	if (!list || groups.has(key)) return;

	const abort = new AbortController();
	const on = { signal: abort.signal };
	const state = { lit: null, report: !!report, sent: null, waiting: null, inFlight: false };

	const options = () => [...list.querySelectorAll('[role="option"]')].filter(o => o.closest('[role="listbox"]') === list);
	const enabled = option => option.getAttribute('aria-disabled') !== 'true';
	const chosen = all => all.find(o => o.getAttribute('aria-selected') === 'true' && enabled(o)) ?? null;
	const firstEnabled = all => all.find(enabled) ?? null;
	const optionAt = target => {
		const option = target?.closest?.('[role="option"]');
		return option && option.closest('[role="listbox"]') === list ? option : null;
	};
	const off = () => list.getAttribute('aria-disabled') === 'true';
	const hasFocus = () => document.activeElement === list;
	const hovered = () => options().find(o => o.matches(':hover') && enabled(o)) ?? null;

	// The chosen card is what the page shows already, so a highlight on it reports as no highlight at all.
	const shown = () => state.lit && state.lit.isConnected && state.lit.getAttribute('aria-selected') !== 'true'
		? state.lit.dataset.mokaOption ?? null
		: null;

	const send = () => {
		if (!state.report || state.inFlight || state.waiting === state.sent) return;
		const next = state.waiting;
		state.sent = next;
		state.inFlight = true;
		dotNetRef.invokeMethodAsync('HighlightFromBrowse', next)
			.catch(() => { })
			.finally(() => {
				state.inFlight = false;
				send();
			});
	};

	const light = (option, reveal) => {
		if (state.lit !== option) {
			state.lit?.removeAttribute('data-moka-highlight');
			state.lit = option ?? null;
			if (option) {
				option.setAttribute('data-moka-highlight', '');
				list.setAttribute('aria-activedescendant', option.id);
			} else {
				list.removeAttribute('aria-activedescendant');
			}
		}

		if (option && reveal) option.scrollIntoView({ block: 'nearest', inline: 'nearest' });
		state.waiting = shown();
		send();
	};

	const choose = option => {
		if (!option || !enabled(option) || off()) return;
		light(option, false);
		if (option.getAttribute('aria-selected') === 'true') return;
		// Once .NET has drawn the choice, this card is the chosen one, which reports as no highlight.
		if (state.sent === option.dataset.mokaOption) state.sent = null;
		state.waiting = null;
		dotNetRef.invokeMethodAsync('ChooseFromBrowse', option.dataset.mokaOption).catch(() => { });
	};

	const columns = () => Math.max(1, getComputedStyle(list).gridTemplateColumns.split(' ').filter(Boolean).length);

	// Moves the highlight past disabled cards; returns false when the key isn't one the list takes.
	const move = key => {
		const all = options();
		if (all.length === 0) return false;

		const rtl = getComputedStyle(list).direction === 'rtl';
		const forward = rtl ? 'ArrowLeft' : 'ArrowRight';
		const back = rtl ? 'ArrowRight' : 'ArrowLeft';
		const current = state.lit && state.lit.isConnected ? state.lit : chosen(all) ?? firstEnabled(all);
		let index = all.indexOf(current);
		let step;
		switch (key) {
			case forward: step = 1; break;
			case back: step = -1; break;
			case 'ArrowDown': step = columns(); break;
			case 'ArrowUp': step = -columns(); break;
			case 'Home': light(firstEnabled(all), true); return true;
			case 'End': light(all.findLast(enabled) ?? null, true); return true;
			default: return false;
		}

		for (let next = index + step; next >= 0 && next < all.length; next += step) {
			if (enabled(all[next])) {
				index = next;
				break;
			}
		}

		light(all[index] ?? null, true);
		return true;
	};

	list.addEventListener('keydown', e => {
		if (e.isComposing || e.altKey || e.ctrlKey || e.metaKey || off()) return;

		if (e.key === 'Enter' || e.key === ' ') {
			e.preventDefault();
			choose(state.lit && state.lit.isConnected ? state.lit : null);
			return;
		}

		if (move(e.key)) e.preventDefault();
	}, on);

	list.addEventListener('focus', () => {
		if (off() || (state.lit && state.lit.isConnected)) return;
		const all = options();
		light(chosen(all) ?? firstEnabled(all), true);
	}, on);

	// The highlight goes back to the chosen card once neither the pointer nor focus is on the cards.
	list.addEventListener('blur', () => light(hovered(), false), on);

	list.addEventListener('pointerover', e => {
		if (off()) return;
		const option = optionAt(e.target);
		if (option && enabled(option)) light(option, false);
	}, on);

	list.addEventListener('pointerleave', () => {
		light(hasFocus() ? chosen(options()) : null, false);
	}, on);

	list.addEventListener('click', e => choose(optionAt(e.target)), on);

	// .NET redraws the cards. A highlighted card that went away takes the highlight with it.
	const observer = new MutationObserver(() => {
		if (state.lit && !state.lit.isConnected) {
			state.lit = null;
			const all = options();
			light(hasFocus() ? chosen(all) ?? firstEnabled(all) : null, false);
		}
	});
	observer.observe(list, { childList: true, subtree: true });

	groups.set(key, {
		report: next => {
			state.report = next;
			state.sent = state.waiting = shown();
		},
		stop: () => {
			abort.abort();
			observer.disconnect();
			state.lit?.removeAttribute('data-moka-highlight');
			list.removeAttribute('aria-activedescendant');
		}
	});
}

/**
 * Turns the highlight reports on or off, when the page starts or stops listening.
 * @param {string} key - The group's key.
 * @param {boolean} report - Whether the page listens.
 */
export function reportBrowse(key, report) {
	groups.get(key)?.report(!!report);
}

/**
 * Stops what bindBrowse started.
 * @param {string} key - The group's key.
 */
export function unbindBrowse(key) {
	const group = groups.get(key);
	if (!group) return;
	groups.delete(key);
	group.stop();
}
