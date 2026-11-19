// The field that takes the place of a secret's button, or the first one Save found a problem with, takes focus. Another
// component draws the field, so .NET holds no reference to its input, only its id.
export function focusField(id) {
	document.getElementById(id)?.focus();
}
