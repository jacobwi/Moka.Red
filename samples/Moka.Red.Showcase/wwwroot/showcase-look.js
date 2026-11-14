const YEAR = 60 * 60 * 24 * 365;

export function keep(name, value) {
	document.cookie = `${name}=${encodeURIComponent(value)}; path=/; max-age=${YEAR}; samesite=lax`;
}

export function focusFirst(root) {
	if (!root) {
		return;
	}

	const target = root.querySelector('input:checked:not(:disabled)')
		?? root.querySelector('button:not(:disabled), [href], input:not(:disabled), select:not(:disabled), textarea:not(:disabled), [tabindex]:not([tabindex="-1"])');
	target?.focus({ preventScroll: true });
}

export function focusId(id) {
	document.getElementById(id)?.focus({ preventScroll: true });
}
