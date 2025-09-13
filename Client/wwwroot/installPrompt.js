function triggerPwaInstall() {
    let pwaInstall = document.querySelector("pwa-install");
    if (pwaInstall) {
        pwaInstall.showDialog();
    } else {
        console.error("pwa-install element not found!");
    }
}

window.PWAHelper = {
    isStandalone: function () {
        try {
            return Boolean(
                window.matchMedia('(display-mode: standalone)').matches ||
                window.navigator.standalone
            );
        } catch (e) {
            console.error("Error checking PWA mode:", e);
            return false; // Default to false if there's an issue
        }
    }
};