// Chart.js Blazor JS Interop
window.chartInterop = {
    _charts: {},

    createChart: function (canvasId, configJson) {
        const canvas = document.getElementById(canvasId);
        if (!canvas) return;

        // Onceki chart varsa yok et
        if (this._charts[canvasId]) {
            this._charts[canvasId].destroy();
            delete this._charts[canvasId];
        }

        const config = JSON.parse(configJson);
        this._charts[canvasId] = new Chart(canvas, config);
    },

    updateChart: function (canvasId, dataJson) {
        const chart = this._charts[canvasId];
        if (!chart) return;

        const data = JSON.parse(dataJson);
        chart.data = data;
        chart.update();
    },

    destroyChart: function (canvasId) {
        if (this._charts[canvasId]) {
            this._charts[canvasId].destroy();
            delete this._charts[canvasId];
        }
    }
};
