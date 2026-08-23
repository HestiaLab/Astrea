/**
 * Astrea ResourceTable 列宽拖拽模块（ES module）。
 * 提供 initializeResizeTable(tableId, dotNetRef, options) / disposeResizableColumns(tableId)。
 *
 * 设计要点：
 * 1. Pointer Events + setPointerCapture：指针捕获后 move/up 事件定向到手柄元素，
 *    不注册 document 级全局监听器，组件销毁后无全局事件泄漏。
 * 2. 每个表格用独立 AbortController 管理全部监听器，dispose 时 abort 即可彻底清理。
 * 3. rAF 节流由状态持有，dispose 时 cancelAnimationFrame 兜底。
 * 4. 拖拽结束通过 dotNetRef 回调 C#（OnColumnResized），支持列宽持久化。
 */

/** @typedef {{header: HTMLTableCellElement, adjacent: (HTMLTableCellElement|null), startX: number, startWidth: number, adjacentStartWidth: number, minWidth: number, columnIndex: number}} ResizeSession */
/** @typedef {{session: (ResizeSession|null), rafId: (number|null), controller: AbortController, dotNetRef: any}} TableState */

/** @type {Map<string, TableState>} */
const tableStates = new Map();

/**
 * 初始化表格列宽拖拽。
 * @param {string} tableId 表格元素 id
 * @param {any} [dotNetRef] 可选，用于拖拽结束回调（C# 侧 [JSInvokable("OnColumnResized")]）
 * @param {{minWidths?: (number|null)[]}} [options] 每列最小宽度（px），缺省按列取 50
 */
export function initializeResizeTable(tableId, dotNetRef, options) {
    const table = document.getElementById(tableId);
    if (!table) {
        return;
    }

    // 幂等：先清理旧状态（AbortController.abort 移除全部旧监听器）
    disposeResizableColumns(tableId);

    const tableRect = table.getBoundingClientRect();
    table.style.width = `${tableRect.width}px`;
    table.style.tableLayout = 'fixed';

    const headers = Array.from(table.querySelectorAll('th.ast-rt-th'));

    headers.forEach((h) => {
        const width = h.getBoundingClientRect().width;
        if (width > 0) {
            h.style.width = `${width}px`;
            h.style.boxSizing = 'border-box';
        }
    });

    const controller = new AbortController();
    /** @type {TableState} */
    const state = {
        session: null,
        rafId: null,
        controller,
        dotNetRef: dotNetRef ?? null,
    };
    tableStates.set(tableId, state);
    const { signal } = controller;

    headers.forEach((header, index) => {
        const handle = header.querySelector('.ast-rt-resize-handle');
        if (!handle) {
            return;
        }

        // 手柄只渲染在非最后一列（与 C# 侧一致），相邻列固定为下一列
        const adjacent = index < headers.length - 1 ? headers[index + 1] : null;
        if (!adjacent) {
            return;
        }

        const minWidth = options?.minWidths?.[index] ?? 50;

        handle.addEventListener('pointerdown', (e) => {
            e.preventDefault();
            e.stopPropagation();

            handle.setPointerCapture(e.pointerId);

            state.session = {
                header,
                adjacent,
                startX: e.clientX,
                startWidth: header.getBoundingClientRect().width,
                adjacentStartWidth: adjacent.getBoundingClientRect().width,
                minWidth,
                adjacentMinWidth: options?.minWidths?.[index + 1] ?? 50,
                columnIndex: index,
            };

            document.body.style.cursor = 'col-resize';
            document.body.style.userSelect = 'none';
            header.classList.add('ast-rt-resizing');
            adjacent.classList.add('ast-rt-resizing');
        }, { signal });

        handle.addEventListener('pointermove', (e) => {
            if (!state.session || state.rafId !== null) {
                return;
            }
            state.rafId = requestAnimationFrame(() => {
                state.rafId = null;
                doDrag(state, e);
            });
        }, { signal });

        handle.addEventListener('pointerup', () => {
            stopDrag(state);
        }, { signal });

        handle.addEventListener('pointercancel', () => {
            stopDrag(state);
        }, { signal });

        // 阻止手柄上的点击冒泡，防止触发排序等其他 th 事件
        handle.addEventListener('click', (e) => e.stopPropagation(), { signal });
    });
}

/**
 * @param {TableState} state
 * @param {PointerEvent} e
 */
function doDrag(state, e) {
    const s = state.session;
    if (!s) {
        return;
    }

    const dx = e.clientX - s.startX;

    let newWidth = s.startWidth + dx;
    let adjacentNewWidth = s.adjacentStartWidth - dx;

    // 边界检查：当前列与相邻列分别受各自最小宽度约束，总宽度守恒
    if (newWidth < s.minWidth) {
        newWidth = s.minWidth;
        adjacentNewWidth = s.startWidth + s.adjacentStartWidth - newWidth;
    } else if (adjacentNewWidth < s.adjacentMinWidth) {
        adjacentNewWidth = s.adjacentMinWidth;
        newWidth = s.startWidth + s.adjacentStartWidth - adjacentNewWidth;
    }

    s.header.style.width = `${newWidth}px`;
    s.adjacent.style.width = `${adjacentNewWidth}px`;
}

/**
 * @param {TableState} state
 */
function stopDrag(state) {
    const s = state.session;
    if (!s) {
        return;
    }

    document.body.style.cursor = '';
    document.body.style.userSelect = '';
    s.header.classList.remove('ast-rt-resizing');
    s.adjacent?.classList.remove('ast-rt-resizing');

    const { columnIndex } = s;
    const finalWidth = s.header.getBoundingClientRect().width;

    state.session = null;
    if (state.rafId !== null) {
        cancelAnimationFrame(state.rafId);
        state.rafId = null;
    }

    // 通知 C# 侧列宽变化（失败静默：组件可能已卸载）
    if (state.dotNetRef) {
        state.dotNetRef
            .invokeMethodAsync('OnColumnResized', columnIndex, finalWidth)
            .catch(() => {});
    }
}

/**
 * 清理指定表格的列宽拖拽：abort 全部监听器、取消未执行的 rAF、恢复拖拽中的视觉状态。
 * C# 侧在组件 Dispose 时调用。
 * @param {string} tableId
 */
export function disposeResizableColumns(tableId) {
    const state = tableStates.get(tableId);
    if (!state) {
        return;
    }

    if (state.rafId !== null) {
        cancelAnimationFrame(state.rafId);
        state.rafId = null;
    }

    state.controller.abort();
    tableStates.delete(tableId);

    // 恢复可能残留的拖拽视觉状态
    const table = document.getElementById(tableId);
    if (table) {
        table.querySelectorAll('.ast-rt-resizing').forEach((el) => el.classList.remove('ast-rt-resizing'));
    }
    document.body.style.cursor = '';
    document.body.style.userSelect = '';
}
