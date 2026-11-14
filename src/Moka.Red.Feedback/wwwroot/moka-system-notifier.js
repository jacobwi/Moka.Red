/**
 * MokaSystemNotifier over the browser's Notification API. Nothing here asks for permission unless .NET calls
 * requestPermission, and a host without the API (a MAUI web view, an old browser) answers "unsupported".
 */

function supported() {
	return typeof window !== 'undefined' && 'Notification' in window;
}

export function permission() {
	return supported() ? Notification.permission : 'unsupported';
}

export async function requestPermission() {
	if (!supported()) {
		return 'unsupported';
	}

	if (Notification.permission !== 'default') {
		return Notification.permission;
	}

	return await Notification.requestPermission();
}

export function notify(options, clickRef) {
	if (!supported()) {
		return 'unsupported';
	}

	if (Notification.permission !== 'granted') {
		return 'not-allowed';
	}

	if (options.onlyWhenHidden && document.visibilityState === 'visible') {
		return 'visible';
	}

	const init = {};
	if (options.body) init.body = options.body;
	if (options.icon) init.icon = options.icon;
	if (options.tag) init.tag = options.tag;

	let notification;
	try {
		notification = new Notification(options.title, init);
	} catch (error) {
		// Chrome on Android shows notifications only through a service worker and refuses the constructor.
		if (error instanceof TypeError) {
			return 'unsupported';
		}

		throw error;
	}

	if (clickRef) {
		notification.onclick = () => {
			window.focus();
			clickRef.invokeMethodAsync('Clicked').catch(error => console.error('Moka.Red: a system notification\'s click handler failed.', error));
			notification.close();
		};
		notification.onclose = () => {
			clickRef.invokeMethodAsync('Closed').catch(error => console.warn('Moka.Red: a closed system notification couldn\'t reach the app.', error));
		};
	}

	return 'shown';
}
