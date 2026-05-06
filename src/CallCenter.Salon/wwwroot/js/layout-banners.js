(function () {
    var container = document.getElementById('slnBillingBanners');
    if (!container) return;

    function escapeHtml(text) {
        if (text == null || text === '') return '';
        var d = document.createElement('div');
        d.textContent = text;
        return d.innerHTML;
    }

    function bannerT(key, fallback) {
        return (window.salonT || function (k, f) { return f || k; })(key, fallback);
    }

    $.ajax({ url: '/proxy/subscriptions/banner', dataType: 'text' })
        .done(function (text) {
            if (text == null || !String(text).trim()) return;
            var data;
            try {
                data = JSON.parse(String(text).trim());
            } catch (e) {
                return;
            }
            if (!data) return;
            var parts = [];

            if (data.overdue && data.overdue.message) {
                parts.push(
                    '<div class="alert alert-danger sln-banner-row mb-0 rounded-0 border-0 border-bottom py-2 px-3 d-flex flex-wrap align-items-center justify-content-between gap-2" role="alert">' +
                    '<span class="d-flex align-items-center"><i class="bi bi-exclamation-octagon-fill me-2"></i>' +
                    escapeHtml(data.overdue.message) + '</span>' +
                    '<a class="btn btn-sm btn-danger flex-shrink-0" href="/Modules"><i class="bi bi-credit-card me-1"></i>' + escapeHtml(bannerT('salon.layout.payment_modules', 'Ödeme / Modüller')) + '</a></div>'
                );
            }

            if (data.info && data.info.message) {
                parts.push(
                    '<div class="alert alert-light border sln-banner-row mb-0 rounded-0 border-0 border-bottom py-2 px-3" role="alert">' +
                    '<span class="d-flex align-items-center"><i class="bi bi-info-circle text-primary me-2"></i>' +
                    escapeHtml(data.info.message) + '</span></div>'
                );
            }

            if (parts.length) container.innerHTML = parts.join('');
        })
        .fail(function () { /* sessiz */ });
})();
