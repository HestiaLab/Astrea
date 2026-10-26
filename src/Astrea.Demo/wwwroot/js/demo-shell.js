window.demoShell = {
    focusPageHeading() {
        window.scrollTo(0, 0);
        const heading = document.querySelector('[data-demo-page-heading]');
        if (!heading) {
            return;
        }

        heading.setAttribute('tabindex', '-1');
        heading.focus({ preventScroll: true });
    },
};
