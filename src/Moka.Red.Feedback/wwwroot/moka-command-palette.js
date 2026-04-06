let _listener = null;
let _dotNetRef = null;

export function registerShortcut(dotNetRef) {
    _dotNetRef = dotNetRef;
    _listener = (e) => {
        if ((e.ctrlKey || e.metaKey) && e.key === 'k') {
            e.preventDefault();
            _dotNetRef.invokeMethodAsync('ToggleFromJs');
        }
    };
    document.addEventListener('keydown', _listener);
}

export function focusInput(inputElement) {
    if (inputElement) {
        setTimeout(() => inputElement.focus(), 50);
    }
}

export function dispose() {
    if (_listener) {
        document.removeEventListener('keydown', _listener);
        _listener = null;
    }
    _dotNetRef = null;
}
