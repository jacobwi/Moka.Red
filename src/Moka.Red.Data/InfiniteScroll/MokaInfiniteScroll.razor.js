const watches = new Map();

/**
 * Tells .NET once when the sentinel comes into view, then stops until .NET watches again after the load.
 * No root is given on purpose: the viewport root still respects every clipping ancestor, so this works
 * whether the component, a wrapper around it or the page does the scrolling.
 * @param {string} id - Identifies this component across calls.
 * @param {HTMLElement} sentinel - The element to watch.
 * @param {HTMLElement} root - The component's root, whose height shows whether a load added anything.
 * @param {object} dotNetRef - Receives OnSentinelVisible.
 */
export function watchSentinel(id, sentinel, root, dotNetRef) {
	if (!sentinel || !root) return;

	const watch = watches.get(id) ?? { askedAtHeight: -1, stop: null };
	watches.set(id, watch);
	watch.stop?.();

	let visible = false;
	let waitingForScroll = false;

	const ask = () => {
		watch.askedAtHeight = root.scrollHeight;
		watch.stop();
		dotNetRef.invokeMethodAsync('OnSentinelVisible').catch(() => { });
	};

	const askIfVisible = () => {
		if (visible) ask();
	};

	const observer = new IntersectionObserver(entries => {
		visible = entries[entries.length - 1].isIntersecting;
		if (!visible) return;

		if (root.scrollHeight !== watch.askedAtHeight) {
			ask();
			return;
		}

		// The last load added nothing, so the sentinel is still in view. Asking straight away would repeat
		// for as long as the server keeps failing, so the next scroll is what asks again.
		if (!waitingForScroll) {
			waitingForScroll = true;
			document.addEventListener('scroll', askIfVisible, { capture: true, passive: true });
		}
	});

	watch.stop = () => {
		observer.disconnect();
		document.removeEventListener('scroll', askIfVisible, { capture: true });
		watch.stop = null;
	};

	observer.observe(sentinel);
}

/** Stops watching, keeping what the last load added for the next watch. */
export function unwatchSentinel(id) {
	watches.get(id)?.stop?.();
}

/** Stops watching for good. */
export function forgetSentinel(id) {
	unwatchSentinel(id);
	watches.delete(id);
}
