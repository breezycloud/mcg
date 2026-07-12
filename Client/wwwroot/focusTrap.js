// Minimal focus trap for modal dialogs. Moves focus into the dialog on activate, cycles Tab/
// Shift+Tab between its focusable elements while active, reports Escape back to the .NET side,
// and restores focus to whatever triggered the dialog on deactivate.
window.focusTrap = (function () {
    const states = {};
    const focusableSelector = 'a[href], button:not([disabled]), textarea:not([disabled]), input:not([disabled]), select:not([disabled]), [tabindex]:not([tabindex="-1"])';

    function getFocusable(dialog) {
        return Array.from(dialog.querySelectorAll(focusableSelector))
            .filter(el => el.offsetParent !== null);
    }

    function activate(dialogId, dotNetRef) {
        const dialog = document.getElementById(dialogId);
        if (!dialog) return;

        const handler = function (e) {
            if (e.key === 'Escape') {
                e.preventDefault();
                dotNetRef.invokeMethodAsync('OnEscapePressed');
                return;
            }
            if (e.key !== 'Tab') return;

            const focusable = getFocusable(dialog);
            if (focusable.length === 0) return;
            const first = focusable[0];
            const last = focusable[focusable.length - 1];

            if (e.shiftKey && document.activeElement === first) {
                e.preventDefault();
                last.focus();
            } else if (!e.shiftKey && document.activeElement === last) {
                e.preventDefault();
                first.focus();
            }
        };

        document.addEventListener('keydown', handler, true);
        states[dialogId] = {
            previouslyFocused: document.activeElement,
            handler: handler
        };

        const focusable = getFocusable(dialog);
        (focusable[0] || dialog).focus();
    }

    function deactivate(dialogId) {
        const state = states[dialogId];
        if (!state) return;

        document.removeEventListener('keydown', state.handler, true);
        if (state.previouslyFocused && typeof state.previouslyFocused.focus === 'function') {
            state.previouslyFocused.focus();
        }
        delete states[dialogId];
    }

    return { activate: activate, deactivate: deactivate };
})();
