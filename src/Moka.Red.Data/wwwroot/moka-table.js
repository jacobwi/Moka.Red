export function downloadCsv(csvBase64, filename) {
	const csv = atob(csvBase64);
	const blob = new Blob([csv], { type: 'text/csv;charset=utf-8;' });
	const url = URL.createObjectURL(blob);
	const a = document.createElement('a');
	a.href = url;
	a.download = filename;
	document.body.appendChild(a);
	a.click();
	document.body.removeChild(a);
	URL.revokeObjectURL(url);
}

/**
 * Initializes column resize on a table header cell.
 * @param {DotNetObjectReference} dotNetRef - .NET callback
 * @param {HTMLElement} thElement - The <th> element
 * @param {number} colIndex - Column index for the callback
 */
/**
 * Initializes column resize on ALL resizable columns in a table.
 * @param {DotNetObjectReference} dotNetRef - .NET callback
 * @param {string} tableId - The table wrapper element ID
 */
export function initAllColumnResize(dotNetRef, tableId) {
	const wrapper = tableId ? document.getElementById(tableId) : null;
	if (!wrapper) return;

	const headers = wrapper.querySelectorAll('th');
	headers.forEach((th, index) => {
		initColumnResize(dotNetRef, th, index);
	});
}

function initColumnResize(dotNetRef, thElement, colIndex) {
	const handle = thElement.querySelector('.moka-table-resize-handle');
	if (!handle || handle._mokaResize) return;

	handle.addEventListener('pointerdown', (e) => {
		e.preventDefault();
		e.stopPropagation();

		const startX = e.clientX;
		const startWidth = thElement.offsetWidth;

		document.body.style.userSelect = 'none';
		document.body.style.cursor = 'col-resize';

		const iframes = document.querySelectorAll('iframe');
		iframes.forEach(f => f.style.pointerEvents = 'none');

		const onMove = (e) => {
			const newWidth = Math.max(50, startWidth + e.clientX - startX);
			thElement.style.width = newWidth + 'px';
			thElement.style.minWidth = newWidth + 'px';
		};

		const onUp = (e) => {
			document.removeEventListener('pointermove', onMove);
			document.removeEventListener('pointerup', onUp);
			document.body.style.userSelect = '';
			document.body.style.cursor = '';
			iframes.forEach(f => f.style.pointerEvents = '');

			const finalWidth = Math.max(50, startWidth + e.clientX - startX);
			thElement.style.width = '';
			thElement.style.minWidth = '';
			dotNetRef.invokeMethodAsync('OnColumnResized', colIndex, finalWidth);
		};

		document.addEventListener('pointermove', onMove);
		document.addEventListener('pointerup', onUp);
	});

	handle._mokaResize = true;
}

export function dispose() { }
