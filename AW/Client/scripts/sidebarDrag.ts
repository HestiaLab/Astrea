interface DragSelectors {
    main: string;
    sidebar: string;
    resizeHandle: string;
}

interface DragDimensions {
    minWidth: number;
    maxWidth: number;
}

export function initSidebarDrag(): () => void {
    const SELECTORS: DragSelectors = {
        main: 'main',
        sidebar: '#draggable-sidebar',
        resizeHandle: '.resize-handle'
    };

    const DIMENSIONS: DragDimensions = {
        minWidth: 210,
        maxWidth: 400
    };

    const main: HTMLElement | null = document.querySelector(SELECTORS.main);
    const sidebar: HTMLElement | null = document.querySelector(SELECTORS.sidebar);
    const resizeHandle: HTMLElement | null = sidebar?.querySelector(SELECTORS.resizeHandle) || null;

    if (!sidebar || !resizeHandle || !main) {
        console.error('Drag elements not found', { main, resizeHandle, sidebar });
        return () => { };
    }

    let isDragging: boolean = false;
    let startX: number = 0;
    let startWidth: number = 0;
    let newWidth: number = 0;

    const handleMouseDown = (e: MouseEvent): void => {
        isDragging = true;
        startX = e.clientX;
        const computedStyle: CSSStyleDeclaration = window.getComputedStyle(sidebar);
        startWidth = parseInt(computedStyle.width, 10);

        document.body.style.cursor = 'col-resize';
        document.body.style.userSelect = 'none';
        e.preventDefault();
    };

    const handleMouseMove = (e: MouseEvent): void => {
        if (!isDragging) return;

        const deltaX = e.clientX - startX;
        const calculatedWidth = startWidth + deltaX;
        newWidth = Math.max(
            DIMENSIONS.minWidth,
            Math.min(DIMENSIONS.maxWidth, calculatedWidth)
        );

        // 只在实际宽度变化时移动 handle
        const actualDeltaX = newWidth - startWidth;
        resizeHandle.style.transform = `translateX(${actualDeltaX}px)`;
    };

    const handleMouseUp = (e: MouseEvent): void => {
        if (!isDragging) return;
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
    return (): void => {
        if (resizeHandle) {
            resizeHandle.removeEventListener('mousedown', handleMouseDown);
        }
        document.removeEventListener('mousemove', handleMouseMove);
        document.removeEventListener('mouseup', handleMouseUp);
    };
}
