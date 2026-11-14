/**
 * Remembered banner dismissals for MokaBrowserBannerStorage, in localStorage. A browser that refuses storage (a
 * private window that blocks it, a full quota) means nothing is remembered; the console says so.
 */

const PREFIX = 'moka-banner:';

export function isDismissed(id) {
	try {
		return localStorage.getItem(PREFIX + id) !== null;
	} catch (error) {
		console.warn('Moka.Red: banner dismissals can\'t be read from local storage.', error);
		return false;
	}
}

export function remember(id) {
	try {
		localStorage.setItem(PREFIX + id, new Date().toISOString());
	} catch (error) {
		console.warn('Moka.Red: a banner dismissal can\'t be kept in local storage.', error);
	}
}

export function forget(id) {
	try {
		localStorage.removeItem(PREFIX + id);
	} catch (error) {
		console.warn('Moka.Red: a banner dismissal can\'t be removed from local storage.', error);
	}
}
