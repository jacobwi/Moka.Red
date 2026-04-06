/**
 * MokaScrollToTop JS module — scroll position detection and smooth scrolling.
 */

let _handler = null;

export function init(dotNetRef, showAfter) {
    dispose();

    _handler = () => {
        const visible = window.scrollY > showAfter;
        dotNetRef.invokeMethodAsync('OnScrollChanged', visible);
    };

    window.addEventListener('scroll', _handler, { passive: true });
    _handler(); // check initial state
}

export function scrollToTop(smooth) {
    window.scrollTo({
        top: 0,
        behavior: smooth ? 'smooth' : 'instant'
    });
}

export function dispose() {
    if (_handler) {
        window.removeEventListener('scroll', _handler);
        _handler = null;
    }
}
