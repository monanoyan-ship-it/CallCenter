(function () {
    var container = document.getElementById('slnBillingBanners');
    if (!container) return;

    var dismissTtlMs = 24 * 60 * 60 * 1000;
    var storagePrefix = 'slnBillingBannerDismissed:';

    function escapeHtml(text) {
        if (text == null || text === '') return '';
        var d = document.createElement('div');
        d.textContent = text;
        return d.innerHTML;
    }

    function bannerT(key, fallback) {
        return (window.salonT || function (k, f) { return f || k; })(key, fallback);
    }

    function hashKey(value) {
        var text = String(value || '');
        var hash = 0;
        for (var i = 0; i < text.length; i++) {
            hash = ((hash << 5) - hash) + text.charCodeAt(i);
            hash |= 0;
        }
        return Math.abs(hash).toString(36);
    }

    function storageKey(item, type) {
        return storagePrefix + hashKey((item && item.dismissKey) || (type + ':' + ((item && item.message) || '')));
    }

    function isDismissed(item, type) {
        var key = storageKey(item, type);
        var expiresAt = parseInt(localStorage.getItem(key) || '0', 10);
        if (!expiresAt) return false;
        if (expiresAt <= Date.now()) {
            localStorage.removeItem(key);
            return false;
        }
        return true;
    }

    function closeButton(key) {
        return '<button type="button" class="btn-close btn-sm ms-2 flex-shrink-0" aria-label="Kapat" data-billing-banner-key="' + escapeHtml(key) + '"></button>';
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

            if (data.overdue && data.overdue.message && !isDismissed(data.overdue, 'overdue')) {
                var overdueKey = storageKey(data.overdue, 'overdue');
                parts.push(
                    '<div class="alert alert-danger sln-banner-row mb-0 rounded-0 border-0 border-bottom py-2 px-3 d-flex flex-wrap align-items-center justify-content-between gap-2" role="alert">' +
                    '<span class="d-flex align-items-center"><i class="bi bi-exclamation-octagon-fill me-2"></i>' +
                    escapeHtml(data.overdue.message) + '</span>' +
                    '<span class="d-flex align-items-center gap-2"><a class="btn btn-sm btn-danger flex-shrink-0" href="/Modules"><i class="bi bi-credit-card me-1"></i>' + escapeHtml(bannerT('salon.layout.payment_modules', 'Odeme / Hizmetler')) + '</a>' +
                    closeButton(overdueKey) + '</span></div>'
                );
            }

            if (data.info && data.info.message && !isDismissed(data.info, 'info')) {
                var infoKey = storageKey(data.info, 'info');
                parts.push(
                    '<div class="alert alert-light border sln-banner-row mb-0 rounded-0 border-0 border-bottom py-2 px-3 d-flex align-items-center justify-content-between gap-2" role="alert">' +
                    '<span class="d-flex align-items-center"><i class="bi bi-info-circle text-primary me-2"></i>' +
                    escapeHtml(data.info.message) + '</span>' +
                    closeButton(infoKey) + '</div>'
                );
            }

            if (parts.length) container.innerHTML = parts.join('');
        })
        .fail(function () { /* sessiz */ });

    container.addEventListener('click', function (event) {
        var btn = event.target.closest('[data-billing-banner-key]');
        if (!btn || !btn.classList.contains('btn-close')) return;
        var key = btn.getAttribute('data-billing-banner-key');
        if (!key) return;
        localStorage.setItem(key, String(Date.now() + dismissTtlMs));
        var row = btn.closest('.sln-banner-row');
        if (row) row.remove();
    });
})();
