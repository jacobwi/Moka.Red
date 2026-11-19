// Drives MokaLogViewer.razor.js's watching over a stand-in for a scrolling list and prints, as JSON, where the list
// sits after each step. Run by MokaLogViewerScriptTests: node log-follow.mjs <path to MokaLogViewer.razor.js>

import { pathToFileURL } from 'node:url';

const observers = [];

globalThis.MutationObserver = class {
    constructor(callback) {
        this.callback = callback;
        this.target = null;
        observers.push(this);
    }

    observe(target) {
        this.target = target;
    }

    disconnect() {
        this.target = null;
    }
};

// A scroller as the browser keeps one: scrollTop is clamped to the content, and the scroll event that says it moved
// arrives later, when scrolled() delivers it. Rows are what querySelectorAll finds, each with a box.
class FakeList {
    constructor(clientHeight, scrollHeight) {
        this.clientHeight = clientHeight;
        this.scrollHeight = scrollHeight;
        this.top = 0;
        this.listeners = new Map();
        this.attributes = new Map();
        this.rows = [];
    }

    get scrollTop() {
        return this.top;
    }

    set scrollTop(value) {
        this.top = Math.max(0, Math.min(value, this.scrollHeight - this.clientHeight));
    }

    getAttribute(name) {
        return this.attributes.has(name) ? this.attributes.get(name) : null;
    }

    querySelectorAll() {
        return this.rows;
    }

    addEventListener(type, listener) {
        this.listeners.set(type, listener);
    }

    removeEventListener(type, listener) {
        if (this.listeners.get(type) === listener) this.listeners.delete(type);
    }

    scrolled() {
        this.listeners.get('scroll')?.();
    }

    // Rows arrive: the content grows and whatever watches this list hears of it. With above, the viewer says how many
    // of them it put above its first row, as it does through data-moka-above.
    grow(by, above) {
        this.scrollHeight += by;
        if (above !== undefined) this.attributes.set('data-moka-above', above);
        for (const observer of observers) {
            if (observer.target === this) observer.callback([]);
        }
    }
}

const follow = { follow: true, newestFirst: false, rowHeight: 0 };
const log = await import(pathToFileURL(process.argv[2]).href);
const results = {};

const list = new FakeList(100, 1000);
log.watch(list, follow);
results.start = list.scrollTop;

list.grow(50);
results.afterRows = list.scrollTop;
list.scrolled();

// The reader turns the wheel up, and a row arrives before the scroll event does.
list.scrollTop = 500;
list.grow(50);
results.wheelBeforeRow = list.scrollTop;
list.scrolled();

list.grow(50);
results.scrolledUp = list.scrollTop;

list.scrollTop = list.scrollHeight - list.clientHeight;
list.scrolled();
list.grow(50);
results.backAtBottom = list.scrollTop;
results.backAtBottomHeight = list.scrollHeight;
list.scrolled();

log.watch(list, follow);
results.watchers = observers.filter(observer => observer.target === list).length;

log.unwatch(list);
list.grow(50);
results.afterUnfollow = list.scrollTop;
results.listening = list.listeners.has('scroll');
results.watchersAfterUnfollow = observers.filter(observer => observer.target === list).length;

// A zoomed page leaves a fraction of a pixel at the bottom.
const zoomed = new FakeList(100, 1000);
log.watch(zoomed, follow);
zoomed.top = 899.5;
zoomed.scrolled();
zoomed.grow(10);
results.zoomed = zoomed.scrollTop;

// Newest first under Virtualize: rows 24 px tall arrive at the top.
const newest = new FakeList(100, 1000);
newest.top = 300;
log.watch(newest, { follow: true, newestFirst: true, rowHeight: 24 });
results.newestStart = newest.scrollTop;
newest.grow(48, '1:2');
results.newestAtTop = newest.scrollTop;

// Scrolled down, the reader stays on their row: the list moves down by the rows that arrived above.
newest.scrollTop = 300;
newest.scrolled();
newest.grow(48, '2:2');
results.newestKeptPlace = newest.scrollTop;

// A later change with the same count, such as Virtualize drawing other rows, doesn't move the list again.
newest.grow(0);
results.newestCountReadOnce = newest.scrollTop;

// Back at the top, it follows again.
newest.scrollTop = 0;
newest.scrolled();
newest.grow(24, '3:1');
results.newestBackAtTop = newest.scrollTop;

// Oldest first with rows measured: older lines paged in above the first while the reader is at the top. The rows the
// viewer drew in place of the old ones are 30 and 40 px tall.
const paged = new FakeList(100, 1000);
log.watch(paged, { follow: true, newestFirst: false, rowHeight: 0 });
paged.scrollTop = 0;
paged.scrolled();
paged.rows = [40, 70, 110, 140].map(top => ({ getBoundingClientRect: () => ({ top }) }));
paged.grow(70, '1:2');
results.pagedKeptPlace = paged.scrollTop;

// Without following, the script still keeps the reader's place, and doesn't jump to an end on its own.
const still = new FakeList(100, 1000);
still.top = 200;
log.watch(still, { follow: false, newestFirst: true, rowHeight: 24 });
results.stillStart = still.scrollTop;
still.grow(72, '1:3');
results.stillKeptPlace = still.scrollTop;

// Turning following on later goes to the newest end.
log.watch(still, { follow: true, newestFirst: true, rowHeight: 24 });
results.stillFollowing = still.scrollTop;
results.stillWatchers = observers.filter(observer => observer.target === still).length;

// A browser snaps scrollTop to device pixels, here half a pixel. Ten rows 23.3 px tall arrive above a reader 300 px
// down, who should end up 233 px further, not a fraction of a pixel off for every row.
class SnappingList extends FakeList {
    get scrollTop() {
        return super.scrollTop;
    }

    set scrollTop(value) {
        super.scrollTop = Math.round(value * 2) / 2;
    }
}

const snapping = new SnappingList(100, 5000);
snapping.top = 300;
log.watch(snapping, { follow: false, newestFirst: true, rowHeight: 23.3 });
for (let i = 1; i <= 10; i++) snapping.grow(23.3, `${i}:1`);
results.snapped = snapping.scrollTop;

process.stdout.write(JSON.stringify(results));
