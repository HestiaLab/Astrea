import assert from 'node:assert/strict';
import { readFile } from 'node:fs/promises';
import test from 'node:test';

const sourceUrl = new URL('../../src/Astrea/wwwroot/astrea-resource-table.js', import.meta.url);
const source = await readFile(sourceUrl, 'utf8');
const moduleUrl = `data:text/javascript;base64,${Buffer.from(source).toString('base64')}`;
const tableModule = await import(moduleUrl);

class FakeStyle {
    constructor() {
        this.width = '';
        this.tableLayout = '';
        this.cursor = '';
        this.userSelect = '';
        this.boxSizing = '';
    }
}

class FakeClassList {
    constructor() {
        this.classes = new Set();
    }

    add(name) {
        this.classes.add(name);
    }

    remove(name) {
        this.classes.delete(name);
    }

    contains(name) {
        return this.classes.has(name);
    }
}

class FakeElement {
    constructor(tagName = 'DIV') {
        this.tagName = tagName;
        this.style = new FakeStyle();
        this.classList = new FakeClassList();
        this.attributes = new Map();
        this.listeners = [];
        this.headers = null;
        this.handle = null;
        this.parentElement = null;
        this.rectWidth = 0;
    }

    getBoundingClientRect() {
        const width = this.style.width ? Number.parseFloat(this.style.width) : this.rectWidth;
        return { width, height: 28, top: 0, left: 0, right: 0, bottom: 28 };
    }

    setAttribute(name, value) {
        this.attributes.set(name, String(value));
    }

    getAttribute(name) {
        return this.attributes.get(name) ?? null;
    }

    querySelector(selector) {
        return selector === '.ast-rt-resize-handle' ? this.handle : null;
    }

    querySelectorAll(selector) {
        return selector === 'th.ast-rt-th' ? this.headers ?? [] : [];
    }

    addEventListener(name, listener, options = {}) {
        this.listeners.push({ name, listener, signal: options.signal });
    }

    setPointerCapture() {}

    dispatch(name, event = {}) {
        for (const entry of this.listeners) {
            if (entry.name === name && !entry.signal?.aborted) {
                entry.listener(event);
            }
        }
    }
}

let animationFrames = [];
globalThis.requestAnimationFrame = (callback) => {
    animationFrames.push(callback);
    return animationFrames.length;
};
globalThis.cancelAnimationFrame = () => {};

class FakeResizeObserver {
    static instances = [];

    constructor(callback) {
        this.callback = callback;
        this.element = null;
        this.disconnected = false;
        FakeResizeObserver.instances.push(this);
    }

    observe(element) {
        this.element = element;
    }

    disconnect() {
        this.disconnected = true;
    }
}

globalThis.ResizeObserver = FakeResizeObserver;

function flushAnimationFrames() {
    const pending = animationFrames;
    animationFrames = [];
    pending.forEach((callback) => callback());
}

function createTable(widths) {
    const table = new FakeElement('TABLE');
    table.rectWidth = widths.reduce((total, width) => total + width, 0);
    table.headers = widths.map((width, index) => {
        const header = new FakeElement('TH');
        header.rectWidth = width;
        if (index < widths.length - 1) {
            header.handle = new FakeElement('DIV');
        }
        return header;
    });
    return table;
}

function setUp(tableId, table) {
    globalThis.document = {
        getElementById: (id) => (id === tableId ? table : null),
        body: new FakeElement('BODY'),
    };
}

function createNotifier() {
    const calls = [];
    return {
        calls,
        invokeMethodAsync: (name, ...args) => {
            calls.push([name, ...args]);
            return Promise.resolve();
        },
    };
}

function pointerEvent(clientX) {
    return {
        clientX,
        pointerId: 1,
        preventDefault() {},
        stopPropagation() {},
    };
}

function keyEvent(key) {
    let prevented = false;
    let stopped = false;
    return {
        key,
        preventDefault() {
            prevented = true;
        },
        stopPropagation() {
            stopped = true;
        },
        get prevented() {
            return prevented;
        },
        get stopped() {
            return stopped;
        },
    };
}

test('pointer drag conserves total width, clamps at min width, and notifies once', () => {
    const table = createTable([60, 160, 100]);
    setUp('rt-pointer', table);
    const notifier = createNotifier();
    tableModule.initializeResizeTable('rt-pointer', notifier, { minWidths: [40, 60, 100] });
    const handle = table.headers[0].handle;

    handle.dispatch('pointerdown', pointerEvent(100));
    handle.dispatch('pointermove', pointerEvent(120));
    flushAnimationFrames();
    assert.equal(table.headers[0].style.width, '80px');
    assert.equal(table.headers[1].style.width, '140px');

    handle.dispatch('pointerup');
    assert.equal(notifier.calls.length, 1);
    assert.deepEqual(notifier.calls[0], ['OnColumnResized', 0, 80]);

    handle.dispatch('pointerdown', pointerEvent(100));
    handle.dispatch('pointermove', pointerEvent(-100));
    flushAnimationFrames();
    assert.equal(table.headers[0].style.width, '40px');
    assert.equal(table.headers[1].style.width, '180px');
    handle.dispatch('pointerup');

    const total = table.headers.slice(0, 2).reduce((sum, header) => sum + Number.parseFloat(header.style.width), 0);
    assert.equal(total, 220);
});

test('keyboard arrows resize columns, clamp at min width, and do not stop propagation', () => {
    const table = createTable([60, 160, 100]);
    setUp('rt-keyboard', table);
    const notifier = createNotifier();
    tableModule.initializeResizeTable('rt-keyboard', notifier, { minWidths: [40, 60, 100] });
    const handle = table.headers[0].handle;

    const right = keyEvent('ArrowRight');
    handle.dispatch('keydown', right);
    assert.equal(right.prevented, true);
    assert.equal(right.stopped, false);
    assert.equal(table.headers[0].style.width, '70px');
    assert.equal(table.headers[1].style.width, '150px');
    assert.deepEqual(notifier.calls.at(-1), ['OnColumnResized', 0, 70]);
    assert.equal(handle.getAttribute('aria-valuenow'), '70');

    const left = keyEvent('ArrowLeft');
    handle.dispatch('keydown', left);
    assert.equal(left.prevented, true);
    assert.equal(table.headers[0].style.width, '60px');
    assert.equal(table.headers[1].style.width, '160px');
    assert.equal(notifier.calls.length, 2);

    for (let index = 0; index < 5; index++) {
        handle.dispatch('keydown', keyEvent('ArrowLeft'));
    }

    assert.equal(table.headers[0].style.width, '40px');
    assert.equal(table.headers[1].style.width, '180px');

    const ignored = keyEvent('Enter');
    handle.dispatch('keydown', ignored);
    assert.equal(ignored.prevented, false);
});

test('dispose aborts keyboard and pointer listeners and restores body styles', () => {
    const table = createTable([60, 160, 100]);
    setUp('rt-dispose', table);
    const notifier = createNotifier();
    tableModule.initializeResizeTable('rt-dispose', notifier, { minWidths: [40, 60, 100] });
    const handle = table.headers[0].handle;

    handle.dispatch('pointerdown', pointerEvent(100));
    handle.dispatch('pointermove', pointerEvent(110));
    flushAnimationFrames();
    assert.equal(document.body.style.cursor, 'col-resize');

    tableModule.disposeResizableColumns('rt-dispose');
    assert.equal(document.body.style.cursor, '');
    assert.equal(document.body.style.userSelect, '');

    handle.dispatch('pointermove', pointerEvent(140));
    flushAnimationFrames();
    handle.dispatch('keydown', keyEvent('ArrowRight'));
    assert.equal(table.headers[0].style.width, '70px');
    assert.equal(notifier.calls.length, 0);
});

test('container ResizeObserver keeps the fixed table width in sync and disconnects on dispose', () => {
    const table = createTable([60, 160, 100]);
    const container = new FakeElement('DIV');
    container.rectWidth = 400;
    table.parentElement = container;
    setUp('rt-resize-observer', table);
    const notifier = createNotifier();
    FakeResizeObserver.instances = [];

    tableModule.initializeResizeTable('rt-resize-observer', notifier, { minWidths: [40, 60, 100] });
    assert.equal(FakeResizeObserver.instances.length, 1);
    container.rectWidth = 500;
    FakeResizeObserver.instances[0].callback();
    assert.equal(table.style.width, '500px');

    tableModule.disposeResizableColumns('rt-resize-observer');
    assert.equal(FakeResizeObserver.instances[0].disconnected, true);
});

test('reinitializing with new min widths applies the new constraint', () => {
    const table = createTable([60, 160, 100]);
    setUp('rt-reinit', table);
    const notifier = createNotifier();
    tableModule.initializeResizeTable('rt-reinit', notifier, { minWidths: [40, 60, 100] });
    tableModule.initializeResizeTable('rt-reinit', notifier, { minWidths: [100, 60, 100] });
    const handle = table.headers[0].handle;

    handle.dispatch('keydown', keyEvent('ArrowLeft'));

    assert.equal(table.headers[0].style.width, '100px');
    assert.equal(table.headers[1].style.width, '120px');
    assert.deepEqual(notifier.calls.at(-1), ['OnColumnResized', 0, 100]);
});
