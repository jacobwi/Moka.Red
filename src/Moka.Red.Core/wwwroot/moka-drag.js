/**
 * Moka.Red — Centralized drag/resize/drop utilities.
 * All pointer-based interactions share this module to avoid code duplication.
 *
 * Usage from Blazor:
 *   var module = await JSRuntime.InvokeAsync<IJSObjectReference>("import",
 *     "./_content/Moka.Red.Core/moka-drag.js");
 *   await module.InvokeVoidAsync("makeDraggable", dotNetRef, element, handle, options);
 */

// ─── DRAGGABLE ──────────────────────────────────────────────
// Makes an element movable by dragging a handle.
// The element should have position: fixed or absolute.

export function makeDraggable(dotNetRef, element, handle, options) {
	if (!element || !handle) return;
	if (handle._mokaDrag) return; // prevent double-attach

	const opts = options || {};
	const callbackMethod = opts.callbackMethod || 'OnDragMoved';
	const bounds = opts.bounds !== false; // default true — constrain to viewport

	function onPointerDown(e) {
		// Don't initiate drag on interactive children (buttons, inputs, links)
		if (e.target.closest('button, input, select, textarea, a, [role="button"]')) return;

		e.preventDefault();

		const rect = element.getBoundingClientRect();
		const offsetX = e.clientX - rect.left;
		const offsetY = e.clientY - rect.top;

		document.body.style.userSelect = 'none';
		handle.style.cursor = 'grabbing';

		function onPointerMove(e) {
			let x = e.clientX - offsetX;
			let y = e.clientY - offsetY;

			if (bounds) {
				// Keep at least 40px visible on each edge
				x = Math.max(-rect.width + 40, Math.min(window.innerWidth - 40, x));
				y = Math.max(0, Math.min(window.innerHeight - 40, y));
			}

			element.style.left = x + 'px';
			element.style.top = y + 'px';
		}

		function onPointerUp(e) {
			document.removeEventListener('pointermove', onPointerMove);
			document.removeEventListener('pointerup', onPointerUp);
			document.body.style.userSelect = '';
			handle.style.cursor = '';

			if (dotNetRef) {
				const finalRect = element.getBoundingClientRect();
				dotNetRef.invokeMethodAsync(callbackMethod, finalRect.left, finalRect.top);
			}
		}

		document.addEventListener('pointermove', onPointerMove);
		document.addEventListener('pointerup', onPointerUp);
	}

	handle.addEventListener('pointerdown', onPointerDown);
	handle._mokaDrag = {
		destroy: () => {
			handle.removeEventListener('pointerdown', onPointerDown);
			delete handle._mokaDrag;
		}
	};
}

export function removeDraggable(handle) {
	handle?._mokaDrag?.destroy();
}

// ─── RESIZABLE ──────────────────────────────────────────────
// Makes an element resizable by dragging a splitter/handle.
// Used for dock panel splitters, resizable panels, etc.

export function makeResizable(dotNetRef, element, splitter, options) {
	if (!element || !splitter) return;
	if (splitter._mokaResize) return;

	const opts = options || {};
	const direction = opts.direction || 'horizontal'; // 'horizontal' or 'vertical'
	const reverse = opts.reverse || false; // true for right/bottom panels
	const minPx = parsePx(opts.min) ?? 50;
	const maxPx = parsePx(opts.max) ?? Infinity;
	const callbackMethod = opts.callbackMethod || 'OnResized';

	const isHorizontal = direction === 'horizontal';

	// Find the parent grid container to update grid-template during drag
	function findGridParent(el) {
		let p = el.parentElement;
		while (p) {
			if (getComputedStyle(p).display === 'grid') return p;
			p = p.parentElement;
		}
		return null;
	}

	function onPointerDown(e) {
		e.preventDefault();
		e.stopPropagation();

		const startPos = isHorizontal ? e.clientX : e.clientY;
		const startSize = isHorizontal ? element.offsetWidth : element.offsetHeight;
		const gridParent = findGridParent(element);

		document.body.style.userSelect = 'none';
		document.body.style.cursor = isHorizontal ? 'col-resize' : 'row-resize';

		// Disable iframe pointer events during resize
		const iframes = document.querySelectorAll('iframe');
		iframes.forEach(f => f.style.pointerEvents = 'none');

		function calcNewSize(e) {
			const currentPos = isHorizontal ? e.clientX : e.clientY;
			const delta = reverse ? startPos - currentPos : currentPos - startPos;
			return Math.min(maxPx, Math.max(minPx, startSize + delta));
		}

		// Pre-compute the grid track index once at drag start
		let trackIndex = -1;
		if (gridParent) {
			const cs = getComputedStyle(element);
			const raw = isHorizontal ? cs.gridColumnStart : cs.gridRowStart;
			// raw can be a number ("1") or a named area ("bottom")
			const parsed = parseInt(raw, 10);
			if (!isNaN(parsed)) {
				trackIndex = parsed - 1;
			} else {
				// Named area: resolve by matching grid-template-areas
				const areaRows = getComputedStyle(gridParent).gridTemplateAreas
					.split('"').filter(s => s.trim()).map(r => r.trim().split(/\s+/));
				if (isHorizontal) {
					// For columns, find the area name in the first row
					trackIndex = areaRows[0]?.indexOf(raw) ?? -1;
				} else {
					// For rows, find which row contains this area name uniquely
					// Each row has the same columns; find the row where our area appears
					for (let i = 0; i < areaRows.length; i++) {
						if (areaRows[i].includes(raw)) {
							trackIndex = i;
							break;
						}
					}
				}
			}
		}

		// Snapshot the original grid template (with 1fr preserved) at drag start
		let origTemplate = null;
		if (gridParent && trackIndex >= 0) {
			const prop = isHorizontal ? 'gridTemplateColumns' : 'gridTemplateRows';
			// Read from the Blazor-set inline style which preserves "1fr"
			origTemplate = gridParent.style[prop]
				? gridParent.style[prop].split(/\s+/)
				: null;
			// If no inline style yet, build from computed but restore 1fr for content tracks
			if (!origTemplate) {
				const computed = getComputedStyle(gridParent)[prop].split(' ');
				origTemplate = computed.map((v, i) => i === trackIndex ? v : v);
				// The Blazor layout uses "1fr" for the content track — find it
				// Content track is the one that isn't a panel (not our trackIndex, not other panels)
				// We identify it as the track that would be "1fr" in the original template
				// Simple heuristic: the content area is always named "content" in areas
				const areaRows = getComputedStyle(gridParent).gridTemplateAreas
					.split('"').filter(s => s.trim()).map(r => r.trim().split(/\s+/));
				if (isHorizontal) {
					const contentIdx = areaRows[0]?.indexOf('content') ?? -1;
					if (contentIdx >= 0) origTemplate[contentIdx] = '1fr';
				} else {
					for (let i = 0; i < areaRows.length; i++) {
						if (areaRows[i].includes('content')) {
							origTemplate[i] = '1fr';
							break;
						}
					}
				}
			}
		}

		function onPointerMove(e) {
			const newSize = calcNewSize(e);

			// Update the parent grid template directly for correct visual feedback
			if (gridParent && trackIndex >= 0 && origTemplate) {
				const prop = isHorizontal ? 'gridTemplateColumns' : 'gridTemplateRows';
				const updated = [...origTemplate];
				updated[trackIndex] = newSize + 'px';
				gridParent.style[prop] = updated.join(' ');
			} else {
				// Fallback: set size on element directly
				if (isHorizontal) {
					element.style.width = newSize + 'px';
				} else {
					element.style.height = newSize + 'px';
				}
			}
		}

		function onPointerUp(e) {
			document.removeEventListener('pointermove', onPointerMove);
			document.removeEventListener('pointerup', onPointerUp);

			document.body.style.userSelect = '';
			document.body.style.cursor = '';
			iframes.forEach(f => f.style.pointerEvents = '');

			const finalSize = calcNewSize(e);

			// Keep the grid template as-is — Blazor re-render will overwrite it.
			// Clearing it causes a layout flash because Blazor re-renders asynchronously.
			if (!gridParent) {
				// Only clear inline size when not using grid (fallback path)
				if (isHorizontal) {
					element.style.width = '';
				} else {
					element.style.height = '';
				}
			}

			if (dotNetRef) {
				dotNetRef.invokeMethodAsync(callbackMethod, finalSize);
			}
		}

		document.addEventListener('pointermove', onPointerMove);
		document.addEventListener('pointerup', onPointerUp);
	}

	splitter.addEventListener('pointerdown', onPointerDown);
	splitter._mokaResize = {
		destroy: () => {
			splitter.removeEventListener('pointerdown', onPointerDown);
			delete splitter._mokaResize;
		}
	};
}

export function removeResizable(splitter) {
	splitter?._mokaResize?.destroy();
}

// ─── SORTABLE ──────────────────────────────────────────────
// Drag-to-reorder items within a container (or between grouped containers).

export function initSortable(dotNetRef, container, options) {
	if (!container || container._mokaSortable) return;

	const opts = options || {};
	const horizontal = opts.horizontal || false;
	const useDragHandle = opts.dragHandle || false;
	const group = opts.group || null;
	const itemSelector = opts.itemSelector || ':scope > .moka-sortable-item';
	const callbackMethod = opts.callbackMethod || 'OnSortEnd';

	if (group) {
		window._mokaSortableGroups = window._mokaSortableGroups || {};
		window._mokaSortableGroups[group] = window._mokaSortableGroups[group] || [];
		window._mokaSortableGroups[group].push({ container, dotNetRef });
	}

	function getItems() {
		return Array.from(container.querySelectorAll(itemSelector))
			.filter(el => !el.classList.contains('moka-sortable-placeholder'));
	}

	function getInsertIndex(e) {
		const items = getItems();
		for (let i = 0; i < items.length; i++) {
			const rect = items[i].getBoundingClientRect();
			const mid = horizontal ? rect.left + rect.width / 2 : rect.top + rect.height / 2;
			const pos = horizontal ? e.clientX : e.clientY;
			if (pos < mid) return i;
		}
		return items.length;
	}

	function onPointerDown(e) {
		const item = e.target.closest(itemSelector.replace(':scope > ', ''));
		if (!item || item.dataset.disabled === 'true' || !container.contains(item)) return;
		if (useDragHandle && !e.target.closest('.moka-sortable-handle')) return;

		e.preventDefault();

		const items = getItems();
		const startIndex = items.indexOf(item);
		if (startIndex < 0) return;

		const rect = item.getBoundingClientRect();
		const offsetX = e.clientX - rect.left;
		const offsetY = e.clientY - rect.top;

		// Create ghost
		const ghost = item.cloneNode(true);
		ghost.className = 'moka-sortable-ghost';
		ghost.style.cssText = `position:fixed;left:${rect.left}px;top:${rect.top}px;width:${rect.width}px;height:${rect.height}px;z-index:99999;pointer-events:none;margin:0;`;
		document.body.appendChild(ghost);

		// Create placeholder
		const placeholder = document.createElement('div');
		placeholder.className = 'moka-sortable-placeholder';
		placeholder.style.cssText = `width:${rect.width}px;height:${rect.height}px;`;
		item.after(placeholder);
		item.style.display = 'none';

		document.body.style.userSelect = 'none';

		function onPointerMove(e) {
			ghost.style.left = (e.clientX - offsetX) + 'px';
			ghost.style.top = (e.clientY - offsetY) + 'px';

			const newIndex = getInsertIndex(e);
			const currentItems = getItems();
			if (newIndex >= currentItems.length) {
				container.appendChild(placeholder);
			} else {
				container.insertBefore(placeholder, currentItems[newIndex]);
			}
		}

		function onPointerUp(e) {
			document.removeEventListener('pointermove', onPointerMove);
			document.removeEventListener('pointerup', onPointerUp);
			document.body.style.userSelect = '';

			// Restore item
			item.style.display = '';
			placeholder.replaceWith(item);
			ghost.remove();

			const finalItems = getItems();
			const finalIndex = finalItems.indexOf(item);

			if (finalIndex !== startIndex && finalIndex >= 0) {
				dotNetRef.invokeMethodAsync(callbackMethod, startIndex, finalIndex);
			}
		}

		document.addEventListener('pointermove', onPointerMove);
		document.addEventListener('pointerup', onPointerUp);
	}

	container.addEventListener('pointerdown', onPointerDown);
	container._mokaSortable = {
		destroy: () => {
			container.removeEventListener('pointerdown', onPointerDown);
			if (group && window._mokaSortableGroups?.[group]) {
				window._mokaSortableGroups[group] =
					window._mokaSortableGroups[group].filter(e => e.container !== container);
			}
			delete container._mokaSortable;
		}
	};
}

export function removeSortable(container) {
	container?._mokaSortable?.destroy();
}

// ─── CANVAS DRAW ────────────────────────────────────────────
// Pointer-based drawing on a canvas element.
// Used for signature pads, annotation tools, etc.

export function initCanvasDraw(dotNetRef, canvas, options) {
	if (!canvas) return;
	if (canvas._mokaCanvasDraw) return;

	const ctx = canvas.getContext('2d');
	const opts = options || {};

	let drawing = false;
	let paths = [];
	let currentPath = [];

	ctx.strokeStyle = opts.strokeColor || '#000000';
	ctx.lineWidth = opts.strokeWidth || 2;
	ctx.lineCap = 'round';
	ctx.lineJoin = 'round';

	// Fill background
	if (opts.backgroundColor) {
		ctx.fillStyle = opts.backgroundColor;
		ctx.fillRect(0, 0, canvas.width, canvas.height);
	}

	function getCanvasCoords(e) {
		const rect = canvas.getBoundingClientRect();
		const scaleX = canvas.width / rect.width;
		const scaleY = canvas.height / rect.height;
		return {
			x: (e.clientX - rect.left) * scaleX,
			y: (e.clientY - rect.top) * scaleY
		};
	}

	function onPointerDown(e) {
		e.preventDefault();
		drawing = true;
		currentPath = [];
		const { x, y } = getCanvasCoords(e);
		ctx.beginPath();
		ctx.moveTo(x, y);
		currentPath.push({ x, y });
	}

	function onPointerMove(e) {
		if (!drawing) return;
		e.preventDefault();
		const { x, y } = getCanvasCoords(e);
		ctx.lineTo(x, y);
		ctx.stroke();
		currentPath.push({ x, y });
	}

	function onPointerUp() {
		if (!drawing) return;
		drawing = false;
		if (currentPath.length > 0) {
			paths.push([...currentPath]);
			currentPath = [];
			const dataUrl = canvas.toDataURL('image/png');
			const callbackMethod = opts.callbackMethod || 'OnSignatureChanged';
			dotNetRef.invokeMethodAsync(callbackMethod, dataUrl);
		}
	}

	canvas.addEventListener('pointerdown', onPointerDown);
	canvas.addEventListener('pointermove', onPointerMove);
	canvas.addEventListener('pointerup', onPointerUp);
	canvas.addEventListener('pointerleave', onPointerUp);

	canvas._mokaCanvasDraw = {
		paths,
		clear: (bgColor) => {
			ctx.fillStyle = bgColor || opts.backgroundColor || '#ffffff';
			ctx.fillRect(0, 0, canvas.width, canvas.height);
			paths.length = 0;
			currentPath = [];
		},
		undo: (strokeColor, strokeWidth, bgColor) => {
			if (paths.length === 0) return;
			paths.pop();
			ctx.fillStyle = bgColor || opts.backgroundColor || '#ffffff';
			ctx.fillRect(0, 0, canvas.width, canvas.height);
			ctx.strokeStyle = strokeColor || opts.strokeColor || '#000000';
			ctx.lineWidth = strokeWidth || opts.strokeWidth || 2;
			ctx.lineCap = 'round';
			ctx.lineJoin = 'round';
			for (const path of paths) {
				ctx.beginPath();
				ctx.moveTo(path[0].x, path[0].y);
				for (let i = 1; i < path.length; i++) {
					ctx.lineTo(path[i].x, path[i].y);
				}
				ctx.stroke();
			}
			const dataUrl = paths.length > 0 ? canvas.toDataURL('image/png') : null;
			const callbackMethod = opts.callbackMethod || 'OnSignatureChanged';
			dotNetRef.invokeMethodAsync(callbackMethod, dataUrl);
		},
		isEmpty: () => paths.length === 0,
		getDataUrl: () => canvas.toDataURL('image/png'),
		destroy: () => {
			canvas.removeEventListener('pointerdown', onPointerDown);
			canvas.removeEventListener('pointermove', onPointerMove);
			canvas.removeEventListener('pointerup', onPointerUp);
			canvas.removeEventListener('pointerleave', onPointerUp);
			delete canvas._mokaCanvasDraw;
		}
	};
}

export function clearCanvas(canvas, bgColor) {
	canvas?._mokaCanvasDraw?.clear(bgColor);
}

export function undoCanvas(canvas, strokeColor, strokeWidth, bgColor) {
	canvas?._mokaCanvasDraw?.undo(strokeColor, strokeWidth, bgColor);
}

export function getCanvasDataUrl(canvas) {
	return canvas?._mokaCanvasDraw?.getDataUrl() ?? null;
}

export function isCanvasEmpty(canvas) {
	return canvas?._mokaCanvasDraw?.isEmpty() ?? true;
}

export function removeCanvasDraw(canvas) {
	canvas?._mokaCanvasDraw?.destroy();
}

// ─── SCROLL ─────────────────────────────────────────────────
// Utility scroll functions used by multiple components.

export function scrollToBottom(element) {
	if (element) element.scrollTop = element.scrollHeight;
}

export function scrollIntoView(element) {
	element?.scrollIntoView({ behavior: 'smooth', block: 'nearest', inline: 'nearest' });
}

// ─── COLOR ─────────────────────────────────────────────────
// Utilities for color picker: eyedropper, gradient area drag, slider drag.

export function isEyeDropperSupported() {
	return 'EyeDropper' in window;
}

export async function pickColor() {
	if (!window.EyeDropper) return null;
	try {
		const dropper = new EyeDropper();
		const result = await dropper.open();
		return result.sRGBHex;
	} catch { return null; }
}

export function copyToClipboard(text) {
	if (navigator.clipboard) {
		navigator.clipboard.writeText(text);
	}
}

export function initColorArea(dotNetRef, element, callbackMethod) {
	if (!element) return;
	if (element._mokaColorArea) return;

	function getPosition(e) {
		const rect = element.getBoundingClientRect();
		const x = Math.max(0, Math.min(1, (e.clientX - rect.left) / rect.width));
		const y = Math.max(0, Math.min(1, (e.clientY - rect.top) / rect.height));
		return { x, y };
	}

	function onPointerDown(e) {
		e.preventDefault();
		element.setPointerCapture(e.pointerId);
		const { x, y } = getPosition(e);
		dotNetRef.invokeMethodAsync(callbackMethod || 'OnColorAreaChanged', x, y);
	}

	function onPointerMove(e) {
		if (!element.hasPointerCapture(e.pointerId)) return;
		const { x, y } = getPosition(e);
		dotNetRef.invokeMethodAsync(callbackMethod || 'OnColorAreaChanged', x, y);
	}

	function onPointerUp(e) {
		if (element.hasPointerCapture(e.pointerId)) {
			element.releasePointerCapture(e.pointerId);
		}
	}

	element.addEventListener('pointerdown', onPointerDown);
	element.addEventListener('pointermove', onPointerMove);
	element.addEventListener('pointerup', onPointerUp);

	element._mokaColorArea = {
		destroy: () => {
			element.removeEventListener('pointerdown', onPointerDown);
			element.removeEventListener('pointermove', onPointerMove);
			element.removeEventListener('pointerup', onPointerUp);
			delete element._mokaColorArea;
		}
	};
}

export function removeColorArea(element) {
	element?._mokaColorArea?.destroy();
}

export function initColorSlider(dotNetRef, element, callbackMethod) {
	if (!element) return;
	if (element._mokaColorSlider) return;

	function getPosition(e) {
		const rect = element.getBoundingClientRect();
		return Math.max(0, Math.min(1, (e.clientX - rect.left) / rect.width));
	}

	function onPointerDown(e) {
		e.preventDefault();
		element.setPointerCapture(e.pointerId);
		const x = getPosition(e);
		dotNetRef.invokeMethodAsync(callbackMethod || 'OnHueSliderChanged', x);
	}

	function onPointerMove(e) {
		if (!element.hasPointerCapture(e.pointerId)) return;
		const x = getPosition(e);
		dotNetRef.invokeMethodAsync(callbackMethod || 'OnHueSliderChanged', x);
	}

	function onPointerUp(e) {
		if (element.hasPointerCapture(e.pointerId)) {
			element.releasePointerCapture(e.pointerId);
		}
	}

	element.addEventListener('pointerdown', onPointerDown);
	element.addEventListener('pointermove', onPointerMove);
	element.addEventListener('pointerup', onPointerUp);

	element._mokaColorSlider = {
		destroy: () => {
			element.removeEventListener('pointerdown', onPointerDown);
			element.removeEventListener('pointermove', onPointerMove);
			element.removeEventListener('pointerup', onPointerUp);
			delete element._mokaColorSlider;
		}
	};
}

export function removeColorSlider(element) {
	element?._mokaColorSlider?.destroy();
}

// ─── HELPERS ────────────────────────────────────────────────

function parsePx(value) {
	if (!value) return null;
	if (typeof value === 'number') return value;
	const match = String(value).match(/^(\d+(?:\.\d+)?)\s*px$/i);
	return match ? parseFloat(match[1]) : null;
}
