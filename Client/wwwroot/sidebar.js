window.matchMedia('(max-width: 768px)').addEventListener('change', (e) => {
    DotNet.invokeMethodAsync('YourApp', 'HandleScreenResize');
});

window.getScrollMetrics = (element) => {
    return {
        scrollTop: element.scrollTop,
        scrollHeight: element.scrollHeight,
        clientHeight: element.clientHeight
    };
};