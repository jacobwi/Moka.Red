/**
 * MokaSplitPane - one-shot measurement of the first pane.
 *
 * The pane is reached through the root ElementReference rather than an id lookup:
 * the Id parameter is caller-controlled, and two panes sharing an Id would
 * otherwise measure each other.
 */

export function measureFirstPane(root, horizontal) {
	if (!root) return 0;

	const first = root.querySelector('.moka-split-pane-first');
	if (!first) return 0;

	const rect = first.getBoundingClientRect();
	return horizontal ? rect.width : rect.height;
}
