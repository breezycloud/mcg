// Device Detection and PWA Install Functions
window.DeviceHelper = {
    // Device detection functions
    getDeviceType: function() {
        const userAgent = navigator.userAgent || navigator.vendor || window.opera;
        
        // iOS detection
        if (/iPad|iPhone|iPod/.test(userAgent) && !window.MSStream) {
            return 'ios';
        }
        
        // Android detection
        if (/android/i.test(userAgent)) {
            return 'android';
        }
        
        // Desktop detection
        return 'desktop';
    },
    
    // Enhanced tablet detection
    isTablet: function() {
        const userAgent = navigator.userAgent || navigator.vendor || window.opera;
        
        // iPad detection
        if (/iPad/.test(userAgent) && !window.MSStream) {
            return true;
        }
        
        // Android tablet detection (larger screens + Android)
        if (/android/i.test(userAgent)) {
            // Check screen size for Android tablets
            const screenWidth = Math.max(window.screen.width, window.screen.height);
            const screenDensity = window.devicePixelRatio || 1;
            const actualWidth = screenWidth / screenDensity;
            
            // Android tablets typically have larger screen widths
            // Also check for specific tablet patterns
            return actualWidth >= 768 || /tablet/i.test(userAgent);
        }
        
        // Windows tablets
        if (/Windows/.test(userAgent) && /Touch/.test(userAgent)) {
            return true;
        }
        
        return false;
    },
    
    // Check if device is mobile phone (not tablet)
    isMobilePhone: function() {
        const userAgent = navigator.userAgent || navigator.vendor || window.opera;
        
        // iPhone detection (excluding iPad)
        if (/iPhone|iPod/.test(userAgent) && !window.MSStream && !/iPad/.test(userAgent)) {
            return true;
        }
        
        // Android phone detection (Android but not tablet)
        if (/android/i.test(userAgent) && !this.isTablet()) {
            return true;
        }
        
        // Other mobile patterns
        if (/BlackBerry|IEMobile|Opera Mini/i.test(userAgent)) {
            return true;
        }
        
        return false;
    },
    
    // Check if device is mobile size (phone or tablet)
    isMobileSize: function() {
        // Check viewport width for mobile-sized screens
        return window.matchMedia('(max-width: 1024px)').matches || 
               /Android|iPhone|iPad|iPod|BlackBerry|IEMobile|Opera Mini|Tablet/i.test(navigator.userAgent) ||
               (navigator.maxTouchPoints && navigator.maxTouchPoints > 2) ||
               window.matchMedia('(pointer: coarse)').matches;
    },
    
    isIOS: function() {
        return this.getDeviceType() === 'ios';
    },
    
    isAndroid: function() {
        return this.getDeviceType() === 'android';
    },
    
    isDesktop: function() {
        return this.getDeviceType() === 'desktop';
    },
    
    // PWA detection functions
    isStandalone: function() {
        try {
            return Boolean(
                window.matchMedia('(display-mode: standalone)').matches ||
                window.navigator.standalone ||
                document.referrer.includes('android-app://')
            );
        } catch (e) {
            console.error("Error checking PWA mode:", e);
            return false;
        }
    },
    
    canInstallPWA: function() {
        // Check if browser supports PWA installation
        return 'serviceWorker' in navigator && 'BeforeInstallPromptEvent' in window;
    },
    
    // iOS specific checks
    isIOSSafari: function() {
        const userAgent = navigator.userAgent;
        return this.isIOS() && /Safari/.test(userAgent) && !/CriOS|FxiOS|EdgiOS/.test(userAgent);
    },
    
    // Android specific PWA install capability
    hasAndroidInstallPrompt: function() {
        return this.isAndroid() && this.canInstallPWA();
    },
    
    // Desktop specific PWA install capability
    hasDesktopInstallPrompt: function() {
        return this.isDesktop() && this.canInstallPWA();
    },
    
    // Browser detection for iOS instructions
    getIOSBrowser: function() {
        if (!this.isIOS()) return null;
        
        const userAgent = navigator.userAgent;
        if (/CriOS/.test(userAgent)) return 'chrome';
        if (/FxiOS/.test(userAgent)) return 'firefox';
        if (/EdgiOS/.test(userAgent)) return 'edge';
        if (/Safari/.test(userAgent)) return 'safari';
        return 'unknown';
    },
    
    // Get device info for display
    getDeviceInfo: function() {
        const deviceType = this.getDeviceType();
        const isStandalone = this.isStandalone();
        const canInstall = this.canInstallPWA();
        
        return {
            type: deviceType,
            isTablet: this.isTablet(),
            isMobilePhone: this.isMobilePhone(),
            isMobileSize: this.isMobileSize(),
            isStandalone: isStandalone,
            canInstall: canInstall,
            browser: deviceType === 'ios' ? this.getIOSBrowser() : null,
            hasInstallPrompt: deviceType === 'android' ? this.hasAndroidInstallPrompt() : 
                            deviceType === 'desktop' ? this.hasDesktopInstallPrompt() : false
        };
    }
};

// Enhanced PWA install function for Android and Desktop
window.installPWA = function() {
    const deviceInfo = window.DeviceHelper.getDeviceInfo();
    
    if ((deviceInfo.type === 'android' || deviceInfo.type === 'desktop') && deviceInfo.hasInstallPrompt) {
        // Use existing triggerPwaInstall function
        if (window.triggerPwaInstall) {
            window.triggerPwaInstall();
        } else {
            console.warn('PWA install prompt not available');
        }
    } else {
        console.log('PWA installation not supported on this device/browser');
    }
};