// Keys stay in the browser: Enter opens the first result, the arrows walk the results, Escape goes back to the box.
// The listener sits on the finder, so it survives Blazor redrawing the results inside it.
export function bindFinder(finder) {
	if (!finder || finder.dataset.ovBound) {
		return;
	}

	finder.dataset.ovBound = 'true';
	finder.addEventListener('keydown', event => {
		const input = finder.querySelector('input');
		const links = [...finder.querySelectorAll('.ov-hits a[href]')];
		const at = links.indexOf(document.activeElement);

		if (event.target === input) {
			if (event.key === 'Enter' && links.length > 0) {
				event.preventDefault();
				links[0].click();
			} else if (event.key === 'ArrowDown' && links.length > 0) {
				event.preventDefault();
				links[0].focus();
			}
			return;
		}

		if (at < 0) {
			return;
		}

		if (event.key === 'ArrowDown' && at < links.length - 1) {
			event.preventDefault();
			links[at + 1].focus();
		} else if (event.key === 'ArrowUp') {
			event.preventDefault();
			(at > 0 ? links[at - 1] : input).focus();
		} else if (event.key === 'Escape') {
			event.preventDefault();
			input.focus();
		}
	});
}

export function focusFinder(finder) {
	const input = finder?.querySelector('input');
	if (input) {
		input.focus();
		input.select();
	}
}
