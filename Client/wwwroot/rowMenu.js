// Keyboard behavior for the shared RowActionMenu.razor dropdown: focuses the first item on open,
// cycles ArrowUp/ArrowDown between items, reports Escape back to .NET (which closes the menu),
// and restores focus to the trigger button on close.
window.rowMenu = (function () {
    const states = {};
    const focusableSelector = 'a[href], button:not([disabled]), [role="menuitem"]:not([disabled])';

    function getItems(menu) {
        return Array.from(menu.querySelectorAll(focusableSelector))
            .filter(el => el.offsetParent !== null);
    }

    function activate(menuId, triggerId, dotNetRef) {
        const menu = document.getElementById(menuId);
        if (!menu) return;

        const handler = function (e) {
            if (e.key === 'Escape') {
                e.preventDefault();
                dotNetRef.invokeMethodAsync('OnEscapePressed');
                return;
            }
            if (e.key !== 'ArrowDown' && e.key !== 'ArrowUp') return;

            const items = getItems(menu);
            if (items.length === 0) return;
            e.preventDefault();

            const currentIndex = items.indexOf(document.activeElement);
            let nextIndex;
            if (e.key === 'ArrowDown') {
                nextIndex = currentIndex < 0 ? 0 : (currentIndex + 1) % items.length;
            } else {
                nextIndex = currentIndex < 0 ? items.length - 1 : (currentIndex - 1 + items.length) % items.length;
            }
            items[nextIndex].focus();
        };

        document.addEventListener('keydown', handler, true);
        states[menuId] = {
            trigger: document.getElementById(triggerId),
            handler: handler
        };

        const items = getItems(menu);
        (items[0] || menu).focus();
    }

    function deactivate(menuId) {
        const state = states[menuId];
        if (!state) return;

        document.removeEventListener('keydown', state.handler, true);
        if (state.trigger && typeof state.trigger.focus === 'function') {
            state.trigger.focus();
        }
        delete states[menuId];
    }

    return { activate: activate, deactivate: deactivate };
})();
