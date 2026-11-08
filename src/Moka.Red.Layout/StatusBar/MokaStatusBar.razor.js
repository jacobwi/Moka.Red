import { bindFit, unbindFit } from "../../Moka.Red.Core/moka-fit.js";

const layout = {
	sides: [":scope > .moka-statusbar-start", ":scope > .moka-statusbar-end"],
	more: ":scope > .moka-statusbar-end > [data-moka-more]"
};

export function bindStatusBar(bar, reporter) {
	bindFit(bar, reporter, layout);
}

export function unbindStatusBar(bar) {
	unbindFit(bar);
}
