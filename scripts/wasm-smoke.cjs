// Drives the WASM sample's /smoke page on a published, trimmed build, because trimming can break what the
// analyzer cannot see, and a Debug run does not trim at all. Exits non-zero if a step fails or the page logs
// an error.
//
//   dotnet publish samples/Moka.Red.WasmApp -c Release -o artifacts/wasm
//   npm install --prefix <folder> --no-save playwright-core
//   NODE_PATH=<folder>/node_modules node scripts/wasm-smoke.cjs artifacts/wasm
//
// Uses a browser already on the machine: BROWSER_CHANNEL names it, chrome by default, msedge on Windows.

const fs = require('node:fs');
const http = require('node:http');
const path = require('node:path');
const { chromium } = require('playwright-core');

const root = path.resolve(process.argv[2] ?? 'artifacts/wasm', 'wwwroot');
const types = {
	'.html': 'text/html', '.js': 'text/javascript', '.mjs': 'text/javascript', '.css': 'text/css', '.json': 'application/json',
	'.wasm': 'application/wasm', '.dat': 'application/octet-stream', '.woff2': 'font/woff2', '.png': 'image/png', '.txt': 'text/plain'
};

const server = http.createServer((req, res) => {
	const url = decodeURIComponent(req.url.split('?')[0]);
	let file = path.join(root, url);
	if (!file.startsWith(root) || !fs.existsSync(file) || fs.statSync(file).isDirectory()) {
		// A page address with no file behind it, the way a host serves a single page app.
		file = path.extname(url) ? null : path.join(root, 'index.html');
	}

	if (!file) {
		res.writeHead(404).end();
		return;
	}

	res.writeHead(200, { 'content-type': types[path.extname(file)] ?? 'application/octet-stream' });
	fs.createReadStream(file).pipe(res);
});

const failures = [];
const step = async (name, run) => {
	try {
		await run();
		console.log(`  ok    ${name}`);
	} catch (error) {
		failures.push(name);
		console.log(`  FAIL  ${name}\n        ${String(error.message ?? error).split('\n')[0]}`);
	}
};

const assert = (condition, message) => {
	if (!condition) throw new Error(message);
};

const drag = async (page, from, by) => {
	await from.scrollIntoViewIfNeeded();
	const box = await from.boundingBox();
	const start = { x: box.x + box.width / 2, y: box.y + box.height / 2 };
	await page.mouse.move(start.x, start.y);
	await page.mouse.down();
	await page.mouse.move(start.x + by.x / 2, start.y + by.y / 2, { steps: 5 });
	await page.mouse.move(start.x + by.x, start.y + by.y, { steps: 5 });
	await page.mouse.up();
};

const activeCell = page => page.evaluate(() => {
	const cell = document.activeElement?.closest?.('td[data-col-index]');
	return cell ? `${cell.closest('tr').dataset.rowIndex},${cell.dataset.colIndex}` : null;
});

(async () => {
	await new Promise(resolve => server.listen(0, '127.0.0.1', resolve));
	const url = `http://127.0.0.1:${server.address().port}/smoke`;

	const browser = await chromium.launch({ channel: process.env.BROWSER_CHANNEL ?? 'chrome' });
	const page = await browser.newPage({ viewport: { width: 1280, height: 900 } });
	page.setDefaultTimeout(10000);

	const problems = [];
	page.on('pageerror', error => problems.push(`page error: ${error.message}`));
	page.on('console', message => {
		if (message.type() === 'error') problems.push(`console: ${message.text()}`);
	});
	page.on('response', response => {
		if (response.status() >= 400) problems.push(`${response.status()}: ${response.url()}`);
	});

	await step('boots', async () => {
		await page.goto(url);
		await page.waitForSelector('#table tbody tr[data-row-index]');
	});

	await step('table: arrows move focus without .NET', async () => {
		await page.locator('#table tbody tr[data-row-index="0"] td[data-col-index="0"]').click();
		await page.keyboard.press('ArrowDown');
		assert(await activeCell(page) === '1,0', `focus is on ${await activeCell(page)}`);
		await page.keyboard.press('End');
		assert(await activeCell(page) === '1,1', `focus is on ${await activeCell(page)}`);
	});

	await step('table: Enter edits through the JS callback', async () => {
		await page.keyboard.press('Enter');
		await page.waitForSelector('#table .moka-table-edit-input');
		await page.keyboard.press('Escape');
		await page.waitForSelector('#table .moka-table-edit-input', { state: 'detached' });
	});

	await step('table: Space selects through the JS callback', async () => {
		await page.keyboard.press(' ');
		await page.waitForFunction(() => document.querySelectorAll('#table tbody input[type=checkbox]:checked').length === 1);
	});

	await step('table: search filters', async () => {
		await page.locator('#table .moka-table-search input').fill('Ada');
		await page.waitForFunction(() => document.querySelectorAll('#table tbody tr[data-row-index]').length === 1);
		await page.locator('#table .moka-table-search input').fill('');
		await page.waitForFunction(() => document.querySelectorAll('#table tbody tr[data-row-index]').length === 3);
	});

	await step('virtualized table: arrows walk past the rendered rows, with one tab stop', async () => {
		await page.locator('#virtual tbody tr[data-row-index="0"] td[data-col-index="0"]').click();
		for (let i = 0; i < 40; i++) {
			await page.keyboard.press('ArrowDown');
			await page.waitForTimeout(20);
		}
		assert(await activeCell(page) === '40,0', `focus is on ${await activeCell(page)}`);
		const stops = await page.locator('#virtual tbody td[data-col-index][tabindex="0"]').count();
		assert(stops === 1, `${stops} tab stops`);
		const first = await page.locator('#virtual tbody tr[data-row-index="0"]').count();
		assert(first === 0, 'row 0 is still rendered, so the table did not virtualize');
	});

	await step('context menu: opens where it was asked, from JS results read into types', async () => {
		const row = page.locator('#table tbody tr[data-row-index="1"] td[data-col-index="0"]');
		const box = await row.boundingBox();
		await row.click({ button: 'right' });
		const item = page.getByRole('menuitem', { name: 'Open Ada' });
		await item.waitFor();
		const menu = await page.locator('[role=menu]').first().boundingBox();
		assert(Math.abs(menu.y - box.y) < 200, `the menu is at y ${menu.y}, the row at ${box.y}`);
		await item.click();
		await page.waitForFunction(() => document.querySelector('#menu-result').textContent === 'Ada');
	});

	await step('dialog: a component opened from its type gets its parameters', async () => {
		await page.locator('#open-dialog').click();
		await page.waitForFunction(() => document.querySelector('#dialog-message')?.textContent === 'Set from a parameter');
		await page.locator('#dialog-done').click();
		await page.waitForFunction(() => document.querySelector('#dialog-result').textContent === 'Set from a parameter');
	});

	await step('dialog: dragging reaches .NET by the callback name', async () => {
		await page.locator('#open-drag').click();
		const box = page.getByRole('dialog', { name: 'Drag me' });
		await box.waitFor();
		const before = await box.boundingBox();
		await drag(page, box.locator('h2'), { x: 80, y: 60 });
		await page.waitForTimeout(200);
		const after = await box.boundingBox();
		assert(Math.abs(after.x - before.x) > 40 && Math.abs(after.y - before.y) > 30, `moved ${after.x - before.x}, ${after.y - before.y}`);
		await page.keyboard.press('Escape');
		await box.waitFor({ state: 'detached' });
	});

	await step('select: opens and picks', async () => {
		await page.locator('#select [role=combobox]').click();
		await page.locator('#select [role=option]', { hasText: 'Research' }).click();
		await page.waitForFunction(() => document.querySelector('#select-result').textContent === 'Research');
	});

	await step('select: a key after hovering reaches .NET by the callback name', async () => {
		await page.locator('#select [role=combobox]').click();
		await page.locator('#select [role=option]', { hasText: 'Engineering' }).hover();
		await page.keyboard.press('Enter');
		await page.waitForFunction(() => document.querySelector('#select-result').textContent === 'Engineering');
	});

	await step('infinite scroll: an observer in the browser asks for more', async () => {
		const lines = () => page.locator('#scroll .smoke-line').count();
		await page.locator('#scroll').scrollIntoViewIfNeeded();
		await page.waitForFunction(() => document.querySelectorAll('#scroll .smoke-line').length >= 10);
		const before = await lines();
		await page.locator('#scroll').evaluate(element => { element.scrollTop = element.scrollHeight; });
		await page.waitForFunction(count => document.querySelectorAll('#scroll .smoke-line').length > count, before);
	});

	await step('signature: a stroke reaches .NET as a stream', async () => {
		await page.locator('#signature canvas').scrollIntoViewIfNeeded();
		await drag(page, page.locator('#signature canvas'), { x: 60, y: 20 });
		await page.waitForFunction(() => Number(document.querySelector('#signature-length').textContent) > 100);
	});

	await step('theme: the base class finds it through its private cascading parameter', async () => {
		assert(await page.locator('#theme-read').textContent() === 'found', 'Theme was null');
	});

	await step('sortable: a drag reports the move', async () => {
		const first = page.locator('#sortable .moka-sortable-item').first();
		const second = await page.locator('#sortable .moka-sortable-item').nth(1).boundingBox();
		await drag(page, first, { x: 0, y: second.height * 1.5 });
		await page.waitForFunction(() => document.querySelector('#sortable-result').textContent.startsWith('Two,One,Three (0 to 1)'));
	});

	await step('color picker: dragging the area reports a value', async () => {
		await page.locator('#color .moka-colorpicker-swatch-button').click();
		const area = page.locator('#color .moka-colorpicker-gradient-area');
		await area.waitFor();
		await drag(page, area, { x: 40, y: 20 });
		await page.waitForFunction(() => document.querySelector('#color-result').textContent !== '#336699');
		await page.keyboard.press('Escape');
	});

	await step('dock: a floating panel moves by the callback name', async () => {
		const panel = page.locator('#dock .moka-dock-panel-header').first();
		const before = await panel.boundingBox();
		await drag(page, panel, { x: 70, y: 40 });
		await page.waitForTimeout(200);
		const after = await panel.boundingBox();
		assert(Math.abs(after.x - before.x) > 30, `moved ${after.x - before.x}`);
	});

	await step('form export: source-generated JSON reads back', async () => {
		const json = await page.locator('#form-json').textContent();
		assert(json.includes('"type": "textField"'), `the JSON is ${json}`);
		assert(await page.locator('#form-count').textContent() === '1', 'it did not read back');
	});

	await step('tabs: content opened from a type gets its parameters', async () => {
		await page.locator('#add-tab').click();
		await page.waitForFunction(() => document.querySelector('#tab-content')?.textContent === 'Set from a parameter');
	});

	await step('tabs: a reload finds the type again by its saved name', async () => {
		await page.reload();
		await page.waitForFunction(() => document.querySelector('#tab-content')?.textContent === 'Restored from its name');
	});

	await step('nothing logged an error', async () => {
		assert(problems.length === 0, problems.join(' | '));
	});

	await browser.close();
	server.close();

	console.log(failures.length === 0 ? '\nAll steps passed.' : `\n${failures.length} failed: ${failures.join(', ')}`);
	process.exit(failures.length === 0 ? 0 : 1);
})().catch(error => {
	console.error(error);
	process.exit(1);
});
