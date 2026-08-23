import assert from 'node:assert/strict';
import { readFile } from 'node:fs/promises';
import test from 'node:test';

const sourceUrl = new URL('../../src/Astrea/wwwroot/astrea-tree.js', import.meta.url);
const source = await readFile(sourceUrl, 'utf8');
const moduleUrl = `data:text/javascript;base64,${Buffer.from(source).toString('base64')}`;
const treeModule = await import(moduleUrl);

class FakeElement {
    constructor(tagName = 'DIV') {
        this.tagName = tagName;
        this.attributes = new Map();
        this.listeners = new Map();
        this.items = [];
        this.isContentEditable = false;
    }

    getAttribute(name) {
        return this.attributes.get(name) ?? null;
    }

    setAttribute(name, value) {
        this.attributes.set(name, value);
    }

    querySelectorAll(selector) {
        return selector === '[role="treeitem"]' ? this.items : [];
    }

    querySelector(selector) {
        return this.items.find((item) => `#${item.getAttribute('id')}` === selector) ?? null;
    }

    addEventListener(name, listener) {
        this.listeners.set(name, listener);
    }

    removeEventListener(name, listener) {
        if (this.listeners.get(name) === listener) {
            this.listeners.delete(name);
        }
    }

    dispatch(name, event) {
        this.listeners.get(name)?.(event);
    }
}

globalThis.HTMLElement = FakeElement;
globalThis.CSS = { escape: (value) => value };

function createKeyboardTree() {
    const tree = new FakeElement();
    const first = new FakeElement();
    first.setAttribute('id', 'first');
    first.setAttribute('role', 'treeitem');
    first.setAttribute('aria-level', '1');
    const second = new FakeElement();
    second.setAttribute('id', 'second');
    second.setAttribute('role', 'treeitem');
    second.setAttribute('aria-level', '1');
    tree.items = [first, second];
    tree.setAttribute('aria-activedescendant', 'first');
    globalThis.document = { getElementById: (id) => (id === 'tree' ? tree : null) };
    return { tree, first, second };
}

function keyEvent(key, target) {
    let prevented = false;
    return {
        key,
        target,
        altKey: false,
        metaKey: false,
        preventDefault() {
            prevented = true;
        },
        get prevented() {
            return prevented;
        },
    };
}

test('keyboard controller prevents only keys with an available Tree action', () => {
    const { tree, first } = createKeyboardTree();
    const controller = treeModule.initializeKeyboard('tree');

    const down = keyEvent('ArrowDown', tree);
    tree.dispatch('keydown', down);
    assert.equal(down.prevented, true);

    const upAtFirst = keyEvent('ArrowUp', tree);
    tree.dispatch('keydown', upAtFirst);
    assert.equal(upAtFirst.prevented, false);

    const tab = keyEvent('Tab', tree);
    tree.dispatch('keydown', tab);
    assert.equal(tab.prevented, false);

    first.setAttribute('aria-disabled', 'true');
    const enterDisabled = keyEvent('Enter', tree);
    tree.dispatch('keydown', enterDisabled);
    assert.equal(enterDisabled.prevented, false);

    const input = new FakeElement('INPUT');
    const inputSpace = keyEvent(' ', input);
    tree.dispatch('keydown', inputSpace);
    assert.equal(inputSpace.prevented, false);

    controller.destroy();
    assert.equal(tree.listeners.has('keydown'), false);
});

test('drag preview movement does not invoke .NET on every animation frame', () => {
    assert.equal(source.includes("invokeMethodAsync('UpdatePreviewPos'"), false);
    assert.equal(source.includes('requestAnimationFrame'), true);
});
