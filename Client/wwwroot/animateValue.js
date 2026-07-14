// Tweens a DOM element's text content between two numbers via requestAnimationFrame.
// Deliberately owns the element's textContent entirely — the Blazor component that calls this
// must never bind text into the same element via Razor, or its next re-render will fight this
// animation for control of the DOM node.
window.animateValue = function (element, from, to, durationMs, decimals, prefix, suffix) {
    if (!element) return;

    prefix = prefix || '';
    suffix = suffix || '';
    decimals = decimals || 0;

    function format(n) {
        return prefix + n.toLocaleString(undefined, { minimumFractionDigits: decimals, maximumFractionDigits: decimals }) + suffix;
    }

    if (!durationMs || durationMs <= 0 || from === to) {
        element.textContent = format(to);
        return;
    }

    var startTime = null;

    function step(timestamp) {
        if (startTime === null) startTime = timestamp;
        var progress = Math.min((timestamp - startTime) / durationMs, 1);
        var eased = 1 - Math.pow(1 - progress, 3); // easeOutCubic
        var current = from + (to - from) * eased;
        element.textContent = format(current);

        if (progress < 1) {
            requestAnimationFrame(step);
        } else {
            element.textContent = format(to);
        }
    }

    requestAnimationFrame(step);
};
