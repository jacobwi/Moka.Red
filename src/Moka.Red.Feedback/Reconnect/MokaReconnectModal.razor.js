// Runs with no .NET to call: this is what's on screen when the circuit is gone.
const MODAL_ID = "components-reconnect-modal";
const STATE_CHANGED = "components-reconnect-state-changed";
const BOUND = Symbol.for("moka-reconnect-bound");

// A failed attempt can come back in milliseconds, which would look like the button did nothing.
const TRYING_AT_LEAST_MS = 800;

const ASKS_FOR_A_CHOICE = new Set(["failed", "paused", "resume-failed"]);

export function bind() {
	const modal = document.getElementById(MODAL_ID);

	// A symbol, not a data attribute: enhanced navigation syncs attributes and would remove one.
	if (!modal || modal[BOUND]) {
		return;
	}

	modal[BOUND] = true;

	const retryWhenVisible = async () => {
		if (document.visibilityState === "visible") {
			await retry();
		}
	};

	let trying = false;

	const retry = async () => {
		if (trying) {
			return;
		}

		trying = true;
		modal.classList.add("moka-reconnect--trying");
		document.removeEventListener("visibilitychange", retryWhenVisible);
		const shortest = new Promise(done => setTimeout(done, TRYING_AT_LEAST_MS));

		try {
			// True means back. False means the server answered and no longer has this circuit. An exception
			// means the server didn't answer.
			if (await globalThis.Blazor.reconnect()) {
				return;
			}

			if (await globalThis.Blazor.resumeCircuit()) {
				modal.close();
			} else {
				location.reload();
			}
		} catch {
			await shortest;
			document.addEventListener("visibilitychange", retryWhenVisible);
		} finally {
			trying = false;
			modal.classList.remove("moka-reconnect--trying");
		}
	};

	const resume = async () => {
		try {
			if (!(await globalThis.Blazor.resumeCircuit())) {
				location.reload();
			}
		} catch {
			modal.classList.replace("components-reconnect-paused", "components-reconnect-resume-failed");
			focusFirstAction(modal);
		}
	};

	const stateChanged = event => {
		const { state } = event.detail;

		if (state === "rejected") {
			location.reload();
			return;
		}

		if (state === "hide") {
			document.removeEventListener("visibilitychange", retryWhenVisible);
			modal.close();
			return;
		}

		if (!modal.open) {
			modal.showModal();
		}

		if (state === "failed") {
			document.addEventListener("visibilitychange", retryWhenVisible);
		}

		// showModal focused nothing while no button was on screen, and the state can change with the dialog open.
		if (ASKS_FOR_A_CHOICE.has(state)) {
			focusFirstAction(modal);
		}
	};

	modal.addEventListener(STATE_CHANGED, stateChanged);

	// A person pressing Escape would be left on a dead page with nothing saying why. closedby="none" covers the
	// browsers that know it.
	modal.addEventListener("cancel", event => event.preventDefault());

	modal.querySelector("#components-reconnect-button").addEventListener("click", retry);
	modal.querySelector("#components-resume-button").addEventListener("click", resume);
	modal.querySelector("#moka-reconnect-reload").addEventListener("click", () => location.reload());
}

function focusFirstAction(modal) {
	const visible = [...modal.querySelectorAll("button")].filter(button => button.offsetParent !== null);
	if (visible.length > 0 && !visible.includes(document.activeElement)) {
		visible[0].focus();
	}
}

bind();
