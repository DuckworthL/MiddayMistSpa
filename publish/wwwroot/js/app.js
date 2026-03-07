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

// =============================================================================
// Google reCAPTCHA v2 Helpers
// =============================================================================

window.recaptchaWidgetId = null;
window.recaptchaReady = false;
window._captchaToken = '';

// Called by Blazor to render the reCAPTCHA widget
window.renderRecaptcha = function (siteKey) {
    window._captchaToken = '';
    // Load the reCAPTCHA script if not already loaded
    if (!document.getElementById('recaptcha-script')) {
        var script = document.createElement('script');
        script.id = 'recaptcha-script';
        script.src = 'https://www.google.com/recaptcha/api.js?onload=onRecaptchaLoaded&render=explicit';
        script.async = true;
        script.defer = true;
        window._recaptchaSiteKey = siteKey;
        document.head.appendChild(script);
    } else if (window.recaptchaReady) {
        // Script already loaded, just render
        window._renderWidget(siteKey);
    }
};

// Called when the reCAPTCHA API script is loaded
window.onRecaptchaLoaded = function () {
    window.recaptchaReady = true;
    if (window._recaptchaSiteKey) {
        window._renderWidget(window._recaptchaSiteKey);
    }
};

window._renderWidget = function (siteKey) {
    var container = document.getElementById('recaptcha-container');
    if (!container) return;
    // Clear previous widget if any
    container.innerHTML = '';
    window._captchaToken = '';
    try {
        window.recaptchaWidgetId = grecaptcha.render('recaptcha-container', {
            sitekey: siteKey,
            theme: 'light',
            callback: function (token) {
                // Store the token immediately when user completes the CAPTCHA
                window._captchaToken = token;
            },
            'expired-callback': function () {
                // Clear the token when it expires (after ~2 minutes)
                window._captchaToken = '';
            },
            'error-callback': function () {
                window._captchaToken = '';
            }
        });
    } catch (e) {
        console.warn('reCAPTCHA render error:', e);
    }
};

// Get the reCAPTCHA response token (uses stored callback token for reliability)
window.getRecaptchaResponse = function () {
    // First try the stored callback token (most reliable)
    if (window._captchaToken) {
        return window._captchaToken;
    }
    // Fallback: try reading directly from the widget
    if (window.recaptchaWidgetId !== null && typeof grecaptcha !== 'undefined') {
        try {
            return grecaptcha.getResponse(window.recaptchaWidgetId);
        } catch (e) {
            console.warn('getResponse error:', e);
        }
    }
    return '';
};

// Reset the reCAPTCHA widget (e.g., after failed login)
window.resetRecaptcha = function () {
    window._captchaToken = '';
    if (window.recaptchaWidgetId !== null && typeof grecaptcha !== 'undefined') {
        try {
            grecaptcha.reset(window.recaptchaWidgetId);
        } catch (e) {
            console.warn('reCAPTCHA reset error:', e);
        }
    }
};
