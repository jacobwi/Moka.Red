const isTyping = target =>
	target instanceof HTMLElement && (target.isContentEditable || /^(INPUT|TEXTAREA|SELECT)$/.test(target.tagName));

// "/" puts the cursor in the layout's search box, unless the person is typing or a modal is over the page.
export function bindSlashKey(root) {
	const onKey = e => {
		if (e.key !== '/' || e.ctrlKey || e.metaKey || e.altKey || e.defaultPrevented || isTyping(e.target)) return;
		if (document.querySelector('[aria-modal="true"]')) return;

		const box = root.querySelector('input[type="search"]');
		if (!box) return;

		e.preventDefault();
		box.focus();
	};

	document.addEventListener('keydown', onKey);
	return { dispose: () => document.removeEventListener('keydown', onKey) };
}
