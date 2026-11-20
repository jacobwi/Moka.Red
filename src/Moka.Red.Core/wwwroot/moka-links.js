// A link row picked with Enter, in a list whose highlight .NET keeps (the command palette, a context menu). .NET
// decides which row Enter meant, since the browser can be a render behind a run of arrow presses, and calls this right
// away: the browser still counts the key press, which lets a page open a tab for five seconds afterwards in Chromium,
// Firefox and WebKit. A click on the row is the link's own.

/**
 * Opens an address in another tab or window, with no opener and no referrer, as a link with
 * rel="noopener noreferrer" would.
 * @param {string} href - The address.
 * @param {string} target - _blank or a window's name.
 */
export function openTab(href, target) {
	window.open(href, target, 'noopener,noreferrer');
}
