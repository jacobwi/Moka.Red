/**
 * MokaCodeBlock JS module — clipboard interop.
 */
export function copyToClipboard(text) {
    return navigator.clipboard.writeText(text);
}
