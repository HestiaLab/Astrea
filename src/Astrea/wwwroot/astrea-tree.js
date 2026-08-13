/**
 * Astrea Tree 拖拽模块（ES module）。
 * 提供 initialize(treeId, dotNetRef) / getElementRect(id)。
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

export function initialize(treeId, dotNetObjectRef, options = {}) {
    const treeElement = document.getElementById(treeId);
    if (!treeElement) {
        console.error(`[Astrea.Tree] Tree element with ID "${treeId}" not found.`);
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

    const insertLine = createInsertLine();
    treeElement.appendChild(insertLine);

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

    const onDragEnd = () => {
        resetDragStyles();
        treeElement
            .querySelectorAll(`.${config.dragSourceClass}`)
            .forEach((el) => el.classList.remove(config.dragSourceClass));
        dotNetObjectRef.invokeMethodAsync('HidePreview');
        dropPos = null;
    };

    const onDragStart = (e) => {
        const target = getTargetNode(e);
        if (!target) {
            return;
        }
        const img = new Image();
        img.src = 'data:image/svg+xml,<svg xmlns="http://www.w3.org/2000/svg" width="1" height="1"></svg>';
        if (e.dataTransfer) {
            e.dataTransfer.setDragImage(img, 0, 0);
        }

        let selectedIds = Array.from(
            treeElement.querySelectorAll(`${config.nodeSelector}.${config.selectedClass}`),
        )
            .map((el) => el.id)
            .filter(Boolean);
        if (selectedIds.length === 0 && target.id) {
            selectedIds = [target.id];
            target.classList.add(config.selectedClass);
        }

        selectedIds.forEach((id) => {
            const el = document.getElementById(id);
            const textEl = el ? el.querySelector(config.textSelector) : null;
            if (textEl) {
                textEl.classList.add(config.dragSourceClass);
            }
        });

        const rect = treeElement.getBoundingClientRect();
        dotNetObjectRef.invokeMethodAsync('ShowPreview', selectedIds, e.clientX - rect.left, e.clientY - rect.top);
        if (e.dataTransfer) {
            e.dataTransfer.effectAllowed = 'move';
            e.dataTransfer.setData('application/json', JSON.stringify(selectedIds));
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
        const lineTop = (pos) => `${(pos === 'top' ? nodeRect.top : nodeRect.bottom) - treeRect.top}px`;
        const lineLeft = `${textRect.left - treeRect.left}px`;
        const lineWidth = `${textRect.width}px`;

        if (dragOffsetY < topThreshold) {
            insertLine.style.top = lineTop('top');
            insertLine.style.left = lineLeft;
            insertLine.style.width = lineWidth;
            insertLine.style.display = 'block';
            if (lastDragOverElement) {
                lastDragOverElement.classList.remove(config.dragOverClass);
            }
            dropPos = 'top';
        } else if (dragOffsetY > bottomThreshold) {
            insertLine.style.top = lineTop('bottom');
            insertLine.style.left = lineLeft;
            insertLine.style.width = lineWidth;
            insertLine.style.display = 'block';
            if (lastDragOverElement) {
                lastDragOverElement.classList.remove(config.dragOverClass);
            }
            dropPos = 'bottom';
        } else {
            insertLine.style.display = 'none';
            if (lastDragOverElement) {
                lastDragOverElement.classList.add(config.dragOverClass);
            }
            dropPos = 'center';
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
        resetDragStyles();
        if (e.dataTransfer && targetNode && targetNode.id) {
            try {
                const sourceIds = JSON.parse(e.dataTransfer.getData('application/json'));
                if (Array.isArray(sourceIds) && sourceIds.length > 0) {
                    await dotNetObjectRef.invokeMethodAsync('HandleNodesDrop', sourceIds, targetNode.id, dropPos);
                }
            } catch (error) {
                console.error('Drop operation failed:', error);
            } finally {
                onDragEnd();
            }
        } else {
            onDragEnd();
        }
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

export function getElementRect(id) {
    const el = document.getElementById(id);
    if (!el) {
        return null;
    }
    const rect = el.getBoundingClientRect();
    const scrollY = window.scrollY || document.documentElement.scrollTop;
    return { top: rect.top + scrollY, height: rect.height };
}
