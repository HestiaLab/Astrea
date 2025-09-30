import { initSidebarDrag } from './sidebarDrag.js';
import { initThemeToggle } from './themeToggle.js';

interface MainInterface {
    initialize(): void;
    cleanup(): void;
    forcecleanup(): void;
}

// 声明全局函数类型
declare global {
    interface Window {
        Main: MainInterface;
        initSidebarDrag?: () => void;
        initThemeToggle?: () => void;
    }
}

export const Main: MainInterface = (() => {
    return {
        initialize(): void {
            console.log('Initializing scripts...');

            // 挂载侧边栏拖拽
            initSidebarDrag();
            console.log('side bar drag script is loaded...');

            // 挂载主题切换
            initThemeToggle();
            console.log('theme toggle script is loaded...');

            // 关闭页面前执行销毁任务
            window.addEventListener('beforeunload', () => this.cleanup());
        },

        cleanup(): void {
            // 执行主题切换清理
            document.documentElement.removeAttribute('data-theme');
        },

        forcecleanup(): void {
            /**
             * 强制清除
             */
            this.cleanup();
            localStorage.removeItem('theme');
        }
    };
})();

export default Main;
