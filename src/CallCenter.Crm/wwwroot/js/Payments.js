// Iyzico checkout form'unu DOM'a basmak icin Salon'da kullanilan ayni helper:
// /js/iyzico-checkout.js -> window.renderIyzicoCheckoutHtml(container, html)
// KO binding kullanmiyoruz cunku KO 'visible' / observable update sirasi inline
// <script> tag'in execute timing'iyle yarisiyor; Salon public profile akisi
// dogrudan getElementById + render cagrisi yapip stabil calisiyor (bkz.
// PublicProfile.js:509). Burada da ayni patterni kullaniyoruz.

function CrmPaymentsViewModel() {
    var self = this;
    var locale = document.documentElement.lang || undefined;

    self.loading = ko.observable(false);
    self.preview = ko.observable(null);
    self.step = ko.observable('confirm');
    self.result = ko.observable(null);

    self.hasPayableLines = ko.computed(function () {
        var p = self.preview();
        return !!(p && Array.isArray(p.lines) && p.lines.length > 0);
    });

    self.amountText = function (amount) {
        var p = self.preview();
        var n = Number(amount);
        if (isNaN(n)) n = 0;
        return n.toLocaleString(locale, { minimumFractionDigits: 2, maximumFractionDigits: 2 }) + ' ' + ((p && p.currency) || 'TRY');
    };

    self.totalText = ko.computed(function () {
        var p = self.preview();
        return self.amountText(p ? p.totalAmount : 0);
    });

    self.supportHref = ko.computed(function () {
        var p = self.preview();
        var email = (p && p.supportEmail) || 'info@corplynk.com';
        return 'mailto:' + encodeURIComponent(email)
            + '?subject=' + encodeURIComponent('Ödeme detayları hakkında')
            + '&body=' + encodeURIComponent('Merhaba, ödeme öncesi hizmetlerimi kontrol etmek istiyorum.');
    });

    self.resultTitle = ko.computed(function () {
        var r = self.result();
        return r && r.success ? 'Ödeme başarılı' : 'Ödeme başarısız';
    });

    self.resultMessage = ko.computed(function () {
        var r = self.result();
        if (!r) return '';
        return r.success
            ? (r.message || 'Ödemeniz başarılı. Hizmetleriniz güncellendi.')
            : (r.error || 'Ödeme başarısız oldu.');
    });

    function parseAjaxBody(text, xhr) {
        if (xhr && (xhr.status === 204 || xhr.status === 205)) return null;
        if (text == null) return null;
        var t = String(text).trim();
        if (!t) return null;
        try { return JSON.parse(t); } catch (e) { return null; }
    }

    function ajaxErrorMessage(xhr, fallback) {
        if (xhr && xhr.responseJSON) return xhr.responseJSON.error || xhr.responseJSON.message || fallback;
        var body = parseAjaxBody(xhr && xhr.responseText, xhr);
        return (body && (body.error || body.message)) || fallback;
    }

    function showPaymentModal() {
        bootstrap.Modal.getOrCreateInstance(document.getElementById('paymentConfirmModal'), { focus: false }).show();
    }

    self.load = function () {
        self.loading(true);
        $.ajax({
            url: '/proxy/payments/checkout-preview?paymentContext=all&materializeSalonDebt=true',
            method: 'GET',
            dataType: 'text',
            cache: false
        }).done(function (text, st, xhr) {
            var data = parseAjaxBody(text, xhr);
            self.preview(data && data.success ? data : null);
        }).fail(function (xhr) {
            self.preview(null);
            var status = xhr && xhr.status;
            if (status !== 400) toastr.error(ajaxErrorMessage(xhr, 'Ödemeler alınamadı.'));
        }).always(function () {
            self.loading(false);
        });
    };

    self.openConfirm = function () {
        if (!self.hasPayableLines()) {
            toastr.info('Bekleyen ödeme bulunmuyor.');
            return;
        }
        self.step('confirm');
        self.result(null);
        var container = document.getElementById('crm-iyzico-checkout');
        if (container) container.innerHTML = '';
        showPaymentModal();
    };

    self.startCheckout = function () {
        self.step('checkout');
        self.loading(true);
        var container = document.getElementById('crm-iyzico-checkout');
        if (container) container.innerHTML = '';
        var preview = self.preview();
        var billingPeriodIds = preview && Array.isArray(preview.lines)
            ? preview.lines.map(function (line) { return line.billingPeriodId; }).filter(function (id) { return id > 0; })
            : [];
        $.ajax({
            url: '/proxy/payments/checkout-session',
            method: 'POST',
            contentType: 'application/json',
            dataType: 'text',
            data: JSON.stringify({ paymentContext: 'all', returnApp: 'crm', billingPeriodIds: billingPeriodIds })
        }).done(function (text, st, xhr) {
            var data = parseAjaxBody(text, xhr);
            var raw = data && (data.htmlContent || data.checkoutFormHtml || data.HtmlContent || data.CheckoutFormHtml);
            if (data && data.success && raw) {
                // Salon ile ayni pattern: direkt DOM'a getElementById + helper.
                var target = document.getElementById('crm-iyzico-checkout');
                if (target && typeof window.renderIyzicoCheckoutHtml === 'function') {
                    window.renderIyzicoCheckoutHtml(target, raw);
                } else if (target) {
                    target.innerHTML = raw;
                }
            } else {
                toastr.error((data && data.error) || 'Ödeme formu oluşturulamadı.');
                self.step('confirm');
            }
        }).fail(function (xhr) {
            toastr.error(ajaxErrorMessage(xhr, 'Ödeme başlatılamadı.'));
            self.step('confirm');
        }).always(function () {
            self.loading(false);
        });
    };

    self.checkPaymentResult = function (token) {
        self.loading(true);
        $.ajax({
            url: '/proxy/payments/package-result',
            method: 'POST',
            contentType: 'application/json',
            dataType: 'text',
            data: JSON.stringify({ token: token })
        }).done(function (text, st, xhr) {
            var data = parseAjaxBody(text, xhr);
            self.result(data && typeof data === 'object' ? data : { success: false, error: 'Geçersiz yanıt' });
            self.step('result');
            showPaymentModal();
            if (data && data.success) self.load();
        }).fail(function (xhr) {
            self.result({ success: false, error: ajaxErrorMessage(xhr, 'Ödeme sonucu alınamadı.') });
            self.step('result');
            showPaymentModal();
        }).always(function () {
            self.loading(false);
        });
    };

    window.addEventListener('message', function (e) {
        if (e.data === 'payment-success' || (e.data && e.data.type === 'payment-success')) {
            self.result({ success: true });
            self.step('result');
            self.load();
        } else if (e.data === 'payment-failed' || (e.data && e.data.type === 'payment-failed')) {
            self.result({ success: false, error: e.data.error || 'Ödeme başarısız oldu.' });
            self.step('result');
        }
    });

    try {
        var token = new URLSearchParams(window.location.search).get('iyzicoToken');
        if (token) self.checkPaymentResult(token);
    } catch (e) {
        // ignore
    }

    self.load();
}

$(function () {
    var el = document.getElementById('crmPaymentsPage');
    if (el) ko.applyBindings(new CrmPaymentsViewModel(), el);
});
