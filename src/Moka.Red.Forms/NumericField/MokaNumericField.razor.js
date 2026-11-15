// Arrow Up and Arrow Down step a number field that has a step. Every other key stays in the browser, so typing sends .NET
// nothing it doesn't already get from the input event.
const bound = new WeakMap();

export function bindSteps(input, field) {
	if (!input || bound.has(input)) return;

	const onKeyDown = e => {
		if (e.key !== 'ArrowUp' && e.key !== 'ArrowDown') return;
		if (e.isComposing || e.shiftKey || e.altKey || e.ctrlKey || e.metaKey) return;
		if (input.readOnly || input.disabled) return;

		e.preventDefault();
		field.invokeMethodAsync('StepBy', e.key === 'ArrowUp' ? 1 : -1);
	};

	input.addEventListener('keydown', onKeyDown);
	bound.set(input, onKeyDown);
}

export function unbindSteps(input) {
	const onKeyDown = input && bound.get(input);
	if (!onKeyDown) return;

	input.removeEventListener('keydown', onKeyDown);
	bound.delete(input);
}
