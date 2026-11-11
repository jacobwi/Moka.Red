// A custom property's computed value has its var() references already resolved, so this reads what the theme
// really ends up with, moka.css defaults and dark overrides included.
export function readTokens(element, names) {
	const style = getComputedStyle(element);
	return names.map(name => style.getPropertyValue(name).trim().replace(/\s+/g, ' '));
}
