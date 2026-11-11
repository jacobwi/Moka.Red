// The spotlight follows the pointer through two custom properties on the box. Nothing reaches .NET, and a
// frame writes at most once however fast the pointer moves. The script is handed the fade wrapper, which
// only the spotlight's markup holds, and listens on the box around it.
const bound = new WeakMap();

export function bindSpotlight(fade) {
	const box = fade?.parentElement;
	if (!box || bound.has(fade)) {
		return;
	}

	let frame = 0;
	let x = 0;
	let y = 0;

	const write = () => {
		frame = 0;
		const rect = box.getBoundingClientRect();
		box.style.setProperty("--_gb-x", `${Math.round(x - rect.left)}px`);
		box.style.setProperty("--_gb-y", `${Math.round(y - rect.top)}px`);
	};

	// A render that drops the wrapper can't be answered with an unbind that finds it, so the listeners
	// leave on their own once it's gone.
	const detach = () => {
		box.removeEventListener("pointermove", move);
		box.removeEventListener("pointerleave", rest);
		if (frame) {
			cancelAnimationFrame(frame);
			frame = 0;
		}

		bound.delete(fade);
	};

	function move(event) {
		if (!fade.isConnected) {
			detach();
			return;
		}

		x = event.clientX;
		y = event.clientY;
		if (!frame) {
			frame = requestAnimationFrame(write);
		}
	}

	function rest() {
		if (!fade.isConnected) {
			detach();
			return;
		}

		if (frame) {
			cancelAnimationFrame(frame);
			frame = 0;
		}

		box.style.removeProperty("--_gb-x");
		box.style.removeProperty("--_gb-y");
	}

	box.addEventListener("pointermove", move, { passive: true });
	box.addEventListener("pointerleave", rest);
	bound.set(fade, { rest, detach });
}

export function unbindSpotlight(fade) {
	const binding = fade && bound.get(fade);
	if (!binding) {
		return;
	}

	binding.rest();
	binding.detach();
}
