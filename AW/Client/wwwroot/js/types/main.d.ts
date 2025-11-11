interface MainInterface {
    initialize(): void;
    cleanup(): void;
    forcecleanup(): void;
}
declare global {
    interface Window {
        Main: MainInterface;
        initSidebarDrag?: () => void;
        initThemeToggle?: () => void;
    }
}
export declare const Main: MainInterface;
export default Main;
