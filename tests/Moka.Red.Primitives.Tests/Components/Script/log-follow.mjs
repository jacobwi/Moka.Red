// Drives MokaLogViewer.razor.js's following over a stand-in for a scrolling list and prints, as JSON, where the list
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
// arrives later, when scrolled() delivers it.
class FakeList {
    constructor(clientHeight, scrollHeight) {
        this.clientHeight = clientHeight;
        this.scrollHeight = scrollHeight;
        this.top = 0;
        this.listeners = new Map();
    }

    get scrollTop() {
        return this.top;
    }

    set scrollTop(value) {
        this.top = Math.max(0, Math.min(value, this.scrollHeight - this.clientHeight));
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

    // Rows arrive: the content grows and whatever watches this list hears of it.
    grow(by) {
        this.scrollHeight += by;
        for (const observer of observers) {
            if (observer.target === this) observer.callback([]);
        }
    }
}

const log = await import(pathToFileURL(process.argv[2]).href);
const results = {};

const list = new FakeList(100, 1000);
log.follow(list);
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

log.follow(list);
results.watchers = observers.filter(observer => observer.target === list).length;

log.unfollow(list);
list.grow(50);
results.afterUnfollow = list.scrollTop;
results.listening = list.listeners.has('scroll');
results.watchersAfterUnfollow = observers.filter(observer => observer.target === list).length;

// A zoomed page leaves a fraction of a pixel at the bottom.
const zoomed = new FakeList(100, 1000);
log.follow(zoomed);
zoomed.top = 899.5;
zoomed.scrolled();
zoomed.grow(10);
results.zoomed = zoomed.scrollTop;

process.stdout.write(JSON.stringify(results));
