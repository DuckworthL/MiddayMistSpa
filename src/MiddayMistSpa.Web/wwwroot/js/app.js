// MiddayMist Spa - Application JavaScript Functions

// Print receipt in a new window
window.printReceipt = function(receiptHtml) {
    var printWindow = window.open('', '_blank', 'width=400,height=600');
    printWindow.document.write(receiptHtml);
    printWindow.document.close();
    printWindow.focus();
    printWindow.print();
    printWindow.close();
};

// Download a file from base64-encoded content
window.downloadFile = function (fileName, contentType, base64Content) {
    var byteCharacters = atob(base64Content);
    var byteNumbers = new Array(byteCharacters.length);
    for (var i = 0; i < byteCharacters.length; i++) {
        byteNumbers[i] = byteCharacters.charCodeAt(i);
    }
    var byteArray = new Uint8Array(byteNumbers);
    var blob = new Blob([byteArray], { type: contentType });
    var url = URL.createObjectURL(blob);
    var a = document.createElement('a');
    a.href = url;
    a.download = fileName;
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
    URL.revokeObjectURL(url);
};

// Chart.js helper - render or update a chart
window.chartInstances = {};

window.renderChart = function (canvasId, chartType, labels, datasets, options) {
    var canvas = document.getElementById(canvasId);
    if (!canvas) return;

    // Destroy existing chart if present
    if (window.chartInstances[canvasId]) {
        window.chartInstances[canvasId].destroy();
    }

    var config = {
        type: chartType,
        data: {
            labels: labels,
            datasets: datasets
        },
        options: Object.assign({
            responsive: true,
            maintainAspectRatio: false,
            plugins: {
                legend: { display: datasets.length > 1, position: 'bottom' }
            }
        }, options || {})
    };

    window.chartInstances[canvasId] = new Chart(canvas.getContext('2d'), config);
};

window.destroyChart = function (canvasId) {
    if (window.chartInstances[canvasId]) {
        window.chartInstances[canvasId].destroy();
        delete window.chartInstances[canvasId];
    }
};
