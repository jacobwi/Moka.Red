/**
 * Moka.Red theme helpers: the OS colour scheme for MokaThemeProvider.AutoDetectColorScheme.
 * A module rather than eval, which a content security policy without 'unsafe-eval' blocks.
 */

const _watchers = new Map();
let _nextHandle = 0;

const darkQuery = () => window.matchMedia('(prefers-color-scheme: dark)');

/** Whether the OS asks for a dark colour scheme right now. */
export function prefersDarkColorScheme() {
	return darkQuery().matches;
}

/**
 * Calls OnColorSchemeChanged(bool) on the .NET object whenever the OS scheme changes.
 * @param {object} dotNetRef - The provider's DotNetObjectReference.
 * @returns {number} Handle to pass to unwatchColorScheme.
 */
export function watchColorScheme(dotNetRef) {
	const query = darkQuery();
	const listener = e => dotNetRef.invokeMethodAsync('OnColorSchemeChanged', e.matches);
	query.addEventListener('change', listener);

	const handle = ++_nextHandle;
	_watchers.set(handle, { query, listener });
	return handle;
}

/** Stops the watch that watchColorScheme returned the handle for. */
export function unwatchColorScheme(handle) {
	const watcher = _watchers.get(handle);
	if (!watcher) return;

	watcher.query.removeEventListener('change', watcher.listener);
	_watchers.delete(handle);
}
