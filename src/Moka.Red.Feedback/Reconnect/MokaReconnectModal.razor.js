// Runs with no .NET to call: this is what's on screen when the circuit is gone. It watches the state classes rather
// than components-reconnect-state-changed, because that event is .NET 10 only and the classes are on both targets.
const MODAL_ID = "components-reconnect-modal";
const BOUND = Symbol.for("moka-reconnect-bound");

// A failed attempt can come back in milliseconds, which would look like the button did nothing.
const TRYING_AT_LEAST_MS = 800;

const SHOWN = [
	"components-reconnect-show",
	"components-reconnect-retrying",
	"components-reconnect-failed",
	"components-reconnect-paused",
	"components-reconnect-resume-failed"
];

const ASKS_FOR_A_CHOICE = [
	"components-reconnect-failed",
	"components-reconnect-paused",
	"components-reconnect-resume-failed"
];

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

			if (await globalThis.Blazor.resumeCircuit?.()) {
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
		}
	};

	const sync = () => {
		const classes = modal.classList;

		if (classes.contains("components-reconnect-rejected")) {
			location.reload();
			return;
		}

		if (!SHOWN.some(name => classes.contains(name))) {
			document.removeEventListener("visibilitychange", retryWhenVisible);
			if (modal.open) {
				modal.close();
			}

			return;
		}

		if (!modal.open) {
			modal.showModal();
		}

		if (classes.contains("components-reconnect-failed")) {
			document.addEventListener("visibilitychange", retryWhenVisible);
		}

		// showModal focused nothing while no button was on screen, and the state can change with the dialog open.
		if (ASKS_FOR_A_CHOICE.some(name => classes.contains(name))) {
			focusFirstAction(modal);
		}
	};

	new MutationObserver(sync).observe(modal, { attributes: true, attributeFilter: ["class"] });

	// A person pressing Escape would be left on a dead page with nothing saying why. closedby="none" covers the
	// browsers that know it.
	modal.addEventListener("cancel", event => event.preventDefault());

	modal.querySelector("#components-reconnect-button").addEventListener("click", retry);
	modal.querySelector("#components-resume-button").addEventListener("click", resume);
	modal.querySelector("#moka-reconnect-reload").addEventListener("click", () => location.reload());

	sync();
}

function focusFirstAction(modal) {
	const visible = [...modal.querySelectorAll("button")].filter(button => button.offsetParent !== null);
	if (visible.length > 0 && !visible.includes(document.activeElement)) {
		visible[0].focus();
	}
}

bind();
