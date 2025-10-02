window.apexChartsHelper = {
    charts: {},

    renderChart: function (elementId, options) {
        const element = document.getElementById(elementId);
        if (!element) {
            console.error('Element not found:', elementId);
            return;
        }
        // Destroy existing chart if it exists
        if (this.charts[elementId]) {
            this.charts[elementId].destroy();
        }

        // Create new chart
        const chart = new ApexCharts(element, options);
        chart.render();
        this.charts[elementId] = chart;
    },

    updateChart: function (elementId, options) {
        if (this.charts[elementId]) {
            this.charts[elementId].updateOptions(options);
        }
    },

    destroyChart: function (elementId) {
        if (this.charts[elementId]) {
            this.charts[elementId].destroy();
            delete this.charts[elementId];
        }
    }
};