/**
 * MokaToastHost. The toasts' timing lives here and in the stylesheet, not in .NET: each toast counts down as the
 * animation of its timer bar, the stylesheet pauses it while the toasts are pointed at or focused, and this module
 * pauses it while the tab is hidden and tells .NET when one ends. A sideways touch swipes a toast away. .NET hears only
 * that a toast ended or was swiped.
 */

const states = new WeakMap();
const hosts = new Set();
let watchingVisibility = false;

// A hidden tab draws no frames, so its animations would all have finished by the time it shows again and every toast
// would go unseen. The pause has to land while the page is still drawing, so style is read back at once.
function syncHidden() {
	const hidden = document.visibilityState === 'hidden';
	for (const host of hosts) {
		if (!host.isConnected) {
			hosts.delete(host);
			continue;
		}

		applyHidden(host, hidden);
	}
}

function applyHidden(host, hidden) {
	host.toggleAttribute('data-moka-hidden', hidden);
	for (const timer of host.querySelectorAll('.moka-toast-timer')) {
		void getComputedStyle(timer).animationPlayState;
	}
}

export function bindHost(host, dotNetRef) {
	if (!host || states.has(host)) {
		return;
	}

	const state = { dotNetRef, swipe: null };

	state.onEnd = event => {
		const timer = event.target;
		if (!(timer instanceof Element) || !timer.classList.contains('moka-toast-timer') || !host.contains(timer)) {
			return;
		}

		dotNetRef.invokeMethodAsync('OnCountdownEnded', timer.dataset.mokaToast, Number(timer.dataset.mokaRun));
	};

	state.onDown = event => startSwipe(host, state, event);

	host.addEventListener('animationend', state.onEnd);
	host.addEventListener('pointerdown', state.onDown);
	states.set(host, state);
	hosts.add(host);

	if (!watchingVisibility) {
		document.addEventListener('visibilitychange', syncHidden);
		watchingVisibility = true;
	}

	applyHidden(host, document.visibilityState === 'hidden');
}

export function unbindHost(host) {
	const state = host ? states.get(host) : null;
	if (!state) {
		return;
	}

	endSwipe(state, false);
	host.removeEventListener('animationend', state.onEnd);
	host.removeEventListener('pointerdown', state.onDown);
	states.delete(host);
	hosts.delete(host);
}

const SWIPE_START = 8;
const SWIPE_SHARE = 0.4;
const SWIPE_SPEED = 0.5;

// The move and release listeners exist only while a finger is down on a toast.
function startSwipe(host, state, event) {
	if (event.pointerType !== 'touch' || state.swipe || !(event.target instanceof Element)) {
		return;
	}

	const toast = event.target.closest('.moka-toast');
	if (!toast || !host.contains(toast) || event.target.closest('button, a, input, select, textarea')) {
		return;
	}

	const swipe = {
		toast,
		id: toast.dataset.mokaToast,
		pointerId: event.pointerId,
		startX: event.clientX,
		startY: event.clientY,
		startTime: event.timeStamp,
		dx: 0,
		sideways: null
	};

	swipe.onMove = moveEvent => moveSwipe(state, moveEvent);
	swipe.onUp = upEvent => releaseSwipe(state, upEvent);
	swipe.onCancel = cancelEvent => {
		if (cancelEvent.pointerId === swipe.pointerId) {
			endSwipe(state, false);
		}
	};

	toast.addEventListener('pointermove', swipe.onMove);
	toast.addEventListener('pointerup', swipe.onUp);
	toast.addEventListener('pointercancel', swipe.onCancel);
	state.swipe = swipe;
}

function moveSwipe(state, event) {
	const swipe = state.swipe;
	if (!swipe || event.pointerId !== swipe.pointerId) {
		return;
	}

	const dx = event.clientX - swipe.startX;
	const dy = event.clientY - swipe.startY;
	if (swipe.sideways === null) {
		if (Math.abs(dx) < SWIPE_START && Math.abs(dy) < SWIPE_START) {
			return;
		}

		swipe.sideways = Math.abs(dx) > Math.abs(dy);
		if (!swipe.sideways) {
			endSwipe(state, false);
			return;
		}

		swipe.toast.setPointerCapture(swipe.pointerId);
		swipe.toast.setAttribute('data-moka-swiping', '');
	}

	swipe.dx = dx;
	const width = swipe.toast.offsetWidth || 1;
	swipe.toast.style.transform = `translateX(${dx}px)`;
	swipe.toast.style.opacity = String(Math.max(0, 1 - Math.abs(dx) / width));
}

function releaseSwipe(state, event) {
	const swipe = state.swipe;
	if (!swipe || event.pointerId !== swipe.pointerId) {
		return;
	}

	const width = swipe.toast.offsetWidth || 1;
	const speed = Math.abs(swipe.dx) / Math.max(1, event.timeStamp - swipe.startTime);
	const away = swipe.sideways && (Math.abs(swipe.dx) > width * SWIPE_SHARE || (speed > SWIPE_SPEED && Math.abs(swipe.dx) > SWIPE_START * 3));
	endSwipe(state, away);
}

// A toast swiped far or fast enough flies off and .NET removes it; any other goes back where it was.
function endSwipe(state, away) {
	const swipe = state.swipe;
	if (!swipe) {
		return;
	}

	state.swipe = null;
	const { toast } = swipe;
	toast.removeEventListener('pointermove', swipe.onMove);
	toast.removeEventListener('pointerup', swipe.onUp);
	toast.removeEventListener('pointercancel', swipe.onCancel);
	if (toast.hasPointerCapture(swipe.pointerId)) {
		toast.releasePointerCapture(swipe.pointerId);
	}

	toast.style.transition = 'transform var(--moka-transition-fast), opacity var(--moka-transition-fast)';
	if (away) {
		toast.style.transform = `translateX(${Math.sign(swipe.dx) * toast.offsetWidth}px)`;
		toast.style.opacity = '0';
		state.dotNetRef.invokeMethodAsync('OnSwiped', swipe.id);
		return;
	}

	toast.removeAttribute('data-moka-swiping');
	toast.style.transform = '';
	toast.style.opacity = '';
	toast.addEventListener('transitionend', () => {
		toast.style.transition = '';
	}, { once: true });
}
