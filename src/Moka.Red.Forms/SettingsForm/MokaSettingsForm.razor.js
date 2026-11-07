const FOUND_CLASS = 'moka-settings-found';
const FOUND_FOR_MS = 2200;
const FIELDS = 'input:not([type=hidden]), select, textarea, button, [role=radio], [role=combobox], [role=switch], [tabindex="0"]';

const marks = new WeakMap();

// Scrolls a form's element into view, marks it for a moment and puts the cursor in its field. False when this form
// doesn't hold it, or doesn't yet.
export function reveal(root, id) {
	const element = id ? document.getElementById(id) : null;
	if (!element || !root?.contains(element)) return false;

	element.scrollIntoView({ block: 'center' });

	clearTimeout(marks.get(element));
	element.classList.add(FOUND_CLASS);
	marks.set(element, setTimeout(() => element.classList.remove(FOUND_CLASS), FOUND_FOR_MS));

	// Not on a touch screen: the keyboard would pop up over what was just scrolled into view.
	if (matchMedia('(pointer: fine)').matches) {
		const field = element.matches(FIELDS) ? element : element.querySelector(FIELDS);
		field?.focus({ preventScroll: true });
	}

	return true;
}

// Blazor handles a link to another part of this page itself and doesn't tell .NET: no LocationChanged, and
// NavigationManager.Uri stays as it was. So the browser reports it. A link clicked twice counts twice, so it marks the
// setting again. The timeout lets Blazor's own click handling change the address first, and folds several events into one.
export function watchFragment(dotNet) {
	let pending = false;
	const report = () => {
		if (pending) return;
		pending = true;
		setTimeout(() => {
			pending = false;
			dotNet.invokeMethodAsync('OnFragmentFollowed', location.hash).catch(() => {});
		});
	};

	const onClick = e => {
		if (e.ctrlKey || e.metaKey || e.shiftKey || e.button !== 0 || !(e.target instanceof Element)) return;
		const link = e.target.closest('a[href*="#"]');
		if (link && link.hash && link.pathname === location.pathname) report();
	};

	document.addEventListener('click', onClick);
	window.addEventListener('hashchange', report);
	window.addEventListener('popstate', report);
	return {
		dispose: () => {
			document.removeEventListener('click', onClick);
			window.removeEventListener('hashchange', report);
			window.removeEventListener('popstate', report);
		}
	};
}

// The footer sticks only while the form is in view, so a link clicked elsewhere on the page could ask where nobody sees.
export function showPrompt(footer) {
	footer?.scrollIntoView({ block: 'nearest' });
	footer?.querySelector('button')?.focus({ preventScroll: true });
}
