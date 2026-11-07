// Tells .NET when the tab has been out of sight for a while, and when it's back. Only those two: switching tabs says
// nothing to .NET until the wait is over.
export function watch(component, waitMs) {
	let waiting = null;
	let away = false;

	const tell = method => component.invokeMethodAsync(method).catch(() => { });

	const changed = () => {
		if (document.visibilityState === 'hidden') {
			waiting ??= setTimeout(() => {
				waiting = null;
				away = true;
				tell('OnTabHidden');
			}, waitMs);
			return;
		}

		clearTimeout(waiting);
		waiting = null;
		if (away) {
			away = false;
			tell('OnTabVisible');
		}
	};

	document.addEventListener('visibilitychange', changed);
	changed();

	return {
		dispose() {
			clearTimeout(waiting);
			document.removeEventListener('visibilitychange', changed);
		}
	};
}
