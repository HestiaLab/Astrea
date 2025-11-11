export function initSidebarDrag() {
    const SELECTORS = {
        main: 'main',
        sidebar: '#draggable-sidebar',
        resizeHandle: '.resize-handle'
    };
    const DIMENSIONS = {
        minWidth: 210,
        maxWidth: 400
    };
    const main = document.querySelector(SELECTORS.main);
    const sidebar = document.querySelector(SELECTORS.sidebar);
    const resizeHandle = sidebar?.querySelector(SELECTORS.resizeHandle) || null;
    if (!sidebar || !resizeHandle || !main) {
        console.error('Drag elements not found', { main, resizeHandle, sidebar });
        return () => { };
    }
    let isDragging = false;
    let startX = 0;
    let startWidth = 0;
    let newWidth = 0;
    const handleMouseDown = (e) => {
        isDragging = true;
        startX = e.clientX;
        const computedStyle = window.getComputedStyle(sidebar);
        startWidth = parseInt(computedStyle.width, 10);
        document.body.style.cursor = 'col-resize';
        document.body.style.userSelect = 'none';
        e.preventDefault();
    };
    const handleMouseMove = (e) => {
        if (!isDragging)
            return;
        const deltaX = e.clientX - startX;
        const calculatedWidth = startWidth + deltaX;
        newWidth = Math.max(DIMENSIONS.minWidth, Math.min(DIMENSIONS.maxWidth, calculatedWidth));
        // 只在实际宽度变化时移动 handle
        const actualDeltaX = newWidth - startWidth;
        resizeHandle.style.transform = `translateX(${actualDeltaX}px)`;
    };
    const handleMouseUp = (e) => {
        if (!isDragging)
            return;
        isDragging = false;
        requestAnimationFrame(() => {
            sidebar.style.width = `${newWidth}px`;
            main.style.marginLeft = `${newWidth}px`;
        });
        resizeHandle.style.transform = '';
        document.body.style.cursor = '';
        document.body.style.userSelect = '';
    };
    resizeHandle.addEventListener('mousedown', handleMouseDown);
    document.addEventListener('mousemove', handleMouseMove, { passive: true });
    document.addEventListener('mouseup', handleMouseUp);
    // 返回清理函数
    return () => {
        if (resizeHandle) {
            resizeHandle.removeEventListener('mousedown', handleMouseDown);
        }
        document.removeEventListener('mousemove', handleMouseMove);
        document.removeEventListener('mouseup', handleMouseUp);
    };
}
//# sourceMappingURL=sidebarDrag.js.map