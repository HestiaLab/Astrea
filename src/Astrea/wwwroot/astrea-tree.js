/**
 * Astrea Tree 拖拽模块（ES module）。
 * initialize(containerId, treeId, dotNetRef) 返回 { destroy, updateConfig }。
 * 拖拽协议：dragstart 时 JS 同步收集源节点 id 写入 dataTransfer（供跨树检测），
 * 随后调用 C# BeginDrag 获得归一化源与 revision；drop 时组装完整协议消息交给 C# HandleDrop 校验。
 */

function createInsertLine() {
    const line = document.createElement('div');
    line.style.position = 'absolute';
    line.style.height = '2px';
    line.style.background = '#1890ff';
    line.style.pointerEvents = 'none';
    line.style.zIndex = '99';
    line.style.display = 'none';
    return line;
}

function generateSessionId() {
    if (typeof crypto !== 'undefined' && typeof crypto.randomUUID === 'function') {
        return crypto.randomUUID();
    }
    return `${Date.now()}-${Math.random().toString(36).slice(2)}`;
}

export function initialize(containerId, treeId, dotNetObjectRef, options = {}) {
    const treeElement = document.getElementById(containerId);
    if (!treeElement) {
        console.error(`[Astrea.Tree] Tree element with ID "${containerId}" not found.`);
        return null;
    }

    const config = Object.assign(
        {
            nodeSelector: '.tree-node[draggable="true"]',
            textSelector: '.tree-node-content__main',
            selectedClass: 'selected',
            dragSourceClass: 'dragging',
            dragOverClass: 'drag-over-center',
            dragZoneRatio: 1 / 3,
        },
        options,
    );

    let dropPos = null;
    let lastDragOverElement = null;
    let rafId = 0;
    let isDestroyed = false;
    // 活动拖拽会话（同树 drop 时携带 C# 返回的 revision 与归一化源）
    let activeDrag = null;

    const insertLine = createInsertLine();
    treeElement.appendChild(insertLine);

    const getNodeId = (el) => (el ? el.getAttribute('data-node-id') : null);

    const findNodeEl = (id) => {
        if (!id) {
            return null;
        }
        return treeElement.querySelector(`[data-node-id="${CSS.escape(id)}"]`);
    };

    const getTargetNode = (event) => {
        const target = event.target;
        if (!(target instanceof HTMLElement) || typeof target.closest !== 'function') {
            return null;
        }
        return target.closest(config.nodeSelector);
    };

    const resetDragStyles = () => {
        if (lastDragOverElement) {
            lastDragOverElement.classList.remove(config.dragOverClass);
            lastDragOverElement = null;
        }
        if (insertLine) {
            insertLine.style.display = 'none';
        }
    };

    const updatePreviewPosition = (clientX, clientY) => {
        if (rafId) {
            return;
        }
        rafId = requestAnimationFrame(() => {
            const rect = treeElement.getBoundingClientRect();
            dotNetObjectRef.invokeMethodAsync('UpdatePreviewPos', clientX - rect.left, clientY - rect.top);
            rafId = 0;
        });
    };

    const clearSourceHighlight = () => {
        treeElement
            .querySelectorAll(`.${config.dragSourceClass}`)
            .forEach((el) => el.classList.remove(config.dragSourceClass));
    };

    const onDragEnd = () => {
        resetDragStyles();
        clearSourceHighlight();
        dotNetObjectRef.invokeMethodAsync('ClearDrag');
        dropPos = null;
        activeDrag = null;
    };

    const onDragStart = async (e) => {
        const target = getTargetNode(e);
        if (!target) {
            return;
        }
        const img = new Image();
        img.src = 'data:image/svg+xml,<svg xmlns="http://www.w3.org/2000/svg" width="1" height="1"></svg>';
        if (e.dataTransfer) {
            e.dataTransfer.setDragImage(img, 0, 0);
        }

        // 同步收集源节点 id（选中节点，否则被拖拽节点自身）
        let sourceNodeIds = Array.from(
            treeElement.querySelectorAll(`${config.nodeSelector}.${config.selectedClass}`),
        )
            .map(getNodeId)
            .filter(Boolean);
        if (sourceNodeIds.length === 0 && getNodeId(target)) {
            sourceNodeIds = [getNodeId(target)];
            target.classList.add(config.selectedClass);
        }

        const sessionId = generateSessionId();

        // 同步写入 dataTransfer（供跨树 drop 侧检测源；完整消息由 C# 补齐）
        const syncMessage = {
            protocolVersion: 1,
            sessionId,
            sourceTreeId: treeId,
            sourceNodeIds,
        };
        if (e.dataTransfer) {
            e.dataTransfer.setData('application/json', JSON.stringify(syncMessage));
            e.dataTransfer.effectAllowed = 'move';
        }

        activeDrag = { ...syncMessage, sourceRevision: null };

        // 高亮源节点
        sourceNodeIds.forEach((id) => {
            const el = findNodeEl(id);
            const textEl = el ? el.querySelector(config.textSelector) : null;
            if (textEl) {
                textEl.classList.add(config.dragSourceClass);
            }
        });

        // 异步：C# 归一化源 + 记录 revision
        try {
            const beginResult = await dotNetObjectRef.invokeMethodAsync('BeginDrag', sessionId, JSON.stringify(sourceNodeIds));
            if (beginResult && typeof beginResult === 'string') {
                const parsed = JSON.parse(beginResult);
                activeDrag.sourceRevision = parsed.sourceRevision;
                activeDrag.sourceNodeIds = parsed.sourceNodeIds || sourceNodeIds;
                const rect = treeElement.getBoundingClientRect();
                dotNetObjectRef.invokeMethodAsync(
                    'ShowPreview',
                    (parsed.sourceNodeIds || sourceNodeIds).length,
                    e.clientX - rect.left,
                    e.clientY - rect.top,
                );
            }
        } catch (error) {
            // BeginDrag 失败：拖拽会话无效，后续 drop 将被 C# 拒绝
        }
    };

    const onDragOver = (e) => {
        const targetNode = getTargetNode(e);
        const textEl = targetNode ? targetNode.querySelector(config.textSelector) : null;
        if (!targetNode || !textEl || textEl.classList.contains(config.dragSourceClass)) {
            resetDragStyles();
            if (e.dataTransfer) {
                e.dataTransfer.dropEffect = 'none';
            }
            return;
        }

        e.preventDefault();
        if (e.dataTransfer) {
            e.dataTransfer.dropEffect = 'move';
        }
        updatePreviewPosition(e.clientX, e.clientY);

        const nodeRect = targetNode.getBoundingClientRect();
        const dragOffsetY = e.clientY - nodeRect.top;
        const topThreshold = nodeRect.height * config.dragZoneRatio;
        const bottomThreshold = nodeRect.height * (1 - config.dragZoneRatio);

        if (textEl !== lastDragOverElement) {
            resetDragStyles();
            lastDragOverElement = textEl;
        }

        const treeRect = treeElement.getBoundingClientRect();
        const textRect = textEl.getBoundingClientRect();
        const lineTop = (pos) => `${(pos === 'before' ? nodeRect.top : nodeRect.bottom) - treeRect.top}px`;
        const lineLeft = `${textRect.left - treeRect.left}px`;
        const lineWidth = `${textRect.width}px`;

        if (dragOffsetY < topThreshold) {
            insertLine.style.top = lineTop('before');
            insertLine.style.left = lineLeft;
            insertLine.style.width = lineWidth;
            insertLine.style.display = 'block';
            if (lastDragOverElement) {
                lastDragOverElement.classList.remove(config.dragOverClass);
            }
            dropPos = 'before';
        } else if (dragOffsetY > bottomThreshold) {
            insertLine.style.top = lineTop('after');
            insertLine.style.left = lineLeft;
            insertLine.style.width = lineWidth;
            insertLine.style.display = 'block';
            if (lastDragOverElement) {
                lastDragOverElement.classList.remove(config.dragOverClass);
            }
            dropPos = 'after';
        } else {
            insertLine.style.display = 'none';
            if (lastDragOverElement) {
                lastDragOverElement.classList.add(config.dragOverClass);
            }
            dropPos = 'inside';
        }
    };

    const onDragLeave = (e) => {
        if (!e.relatedTarget || !treeElement.contains(e.relatedTarget)) {
            resetDragStyles();
        }
    };

    const onDrop = async (e) => {
        e.preventDefault();
        const targetNode = getTargetNode(e);
        const targetId = getNodeId(targetNode);
        resetDragStyles();

        let source = null;
        try {
            source = JSON.parse(e.dataTransfer.getData('application/json'));
        } catch (error) {
            source = null;
        }

        if (!targetId || !source || !Array.isArray(source.sourceNodeIds) || source.sourceNodeIds.length === 0 || !dropPos) {
            onDragEnd();
            return;
        }

        // 完整协议消息：同树用 activeDrag（含 C# 返回的 revision），跨树用 dataTransfer 的最小消息
        const message = {
            protocolVersion: 1,
            sessionId: source.sessionId,
            sourceTreeId: source.sourceTreeId,
            sourceNodeIds: source.sourceNodeIds,
            sourceRevision: activeDrag && activeDrag.sessionId === source.sessionId ? activeDrag.sourceRevision : null,
            targetTreeId: treeId,
            targetNodeId: targetId,
            relation: dropPos,
        };

        await dotNetObjectRef.invokeMethodAsync('HandleDrop', JSON.stringify(message));
        onDragEnd();
    };

    treeElement.addEventListener('dragstart', onDragStart);
    treeElement.addEventListener('dragend', onDragEnd);
    treeElement.addEventListener('dragover', onDragOver);
    treeElement.addEventListener('dragleave', onDragLeave);
    treeElement.addEventListener('drop', onDrop);

    const destroy = () => {
        if (isDestroyed) {
            return;
        }
        treeElement.removeEventListener('dragstart', onDragStart);
        treeElement.removeEventListener('dragend', onDragEnd);
        treeElement.removeEventListener('dragover', onDragOver);
        treeElement.removeEventListener('dragleave', onDragLeave);
        treeElement.removeEventListener('drop', onDrop);
        insertLine.remove();
        if (rafId) {
            cancelAnimationFrame(rafId);
        }
        clearSourceHighlight();
        isDestroyed = true;
    };

    const updateConfig = (newOptions) => {
        if (isDestroyed) {
            return;
        }
        Object.assign(config, newOptions);
    };

    return { destroy, updateConfig };
}

/**
 * 虚拟化：确保目标节点已渲染；未渲染则滚动容器使其进入视口。
 * 返回 true 表示目标 DOM 已存在（可立即更新 aria-activedescendant），
 * false 表示已滚动（Virtualize 将重渲染，需等待下一轮 OnAfterRender）。
 */
export function ensureNodeRendered(containerId, nodeDomId, index, itemSize) {
    if (document.getElementById(nodeDomId)) {
        return true;
    }

    const container = document.getElementById(containerId);
    if (!container) {
        // 容器不存在（未渲染/已销毁）：返回 true 避免死循环等待
        return true;
    }

    const top = index * itemSize;
    const viewTop = container.scrollTop;
    const viewBottom = viewTop + container.clientHeight;

    let target = viewTop;
    if (top < viewTop) {
        target = top;
    } else if (top + itemSize > viewBottom) {
        target = top + itemSize - container.clientHeight;
    }

    if (Math.abs(target - viewTop) < 1) {
        // 已在视口位置但 DOM 未渲染（罕见）：微移 1px 触发 scroll 事件与重测
        target = viewTop + (viewTop > 0 ? -1 : 1);
    }

    container.scrollTop = target;
    return false;
}

/** 纯检查：目标节点 DOM 是否已渲染（不滚动）。 */
export function isNodeRendered(nodeDomId) {
    return document.getElementById(nodeDomId) !== null;
}
