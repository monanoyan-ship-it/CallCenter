function injectIyzicoCheckoutHtml(container, html) {
    if (!container) return;
    try {
        container.innerHTML = '';
        if (!html) return;

        // innerHTML ile basilan <script> tag'leri tarayicida execute edilmez ve
        // replaceChild ile swap edilseler bile bazi Chromium surumlerinde
        // 'already-started' flag'i nedeniyle calismadigi gozlemlendi.
        // Cozum: HTML'i ayri bir node'da parse et, child'lari container'a tasi,
        // script tag'lerini DEGISTIRMEK YERINE document.createElement ile yeniden
        // olusturup appendChild yap. Boylece script ilk kez insert edilmis sayilir
        // ve execute olur (iyziInit objesi global'e dusturup bundle'i head'e ekler).
        var temp = document.createElement('div');
        temp.innerHTML = html;
        var children = Array.prototype.slice.call(temp.childNodes);
        children.forEach(function (node) {
            if (node.nodeType === 1 && node.tagName === 'SCRIPT') {
                var s = document.createElement('script');
                for (var i = 0; i < node.attributes.length; i++) {
                    var a = node.attributes[i];
                    s.setAttribute(a.name, a.value);
                }
                if (!node.src && node.textContent) s.text = node.textContent;
                container.appendChild(s);
            } else {
                container.appendChild(node);
            }
        });
    } catch (e) {
        console.error('iyzico form inject', e);
        container.innerHTML = '<p class="text-danger small mb-0">Odeme formu yuklenirken hata olustu. Sayfayi yenileyip tekrar deneyin.</p>';
    }
}

ko.bindingHandlers.iyzicoCheckoutHtml = {
    update: function (element, valueAccessor) {
        var html = ko.unwrap(valueAccessor());
        if (!html) {
            element.innerHTML = '';
            return;
        }
        injectIyzicoCheckoutHtml(element, html);
    }
};

function CrmPaymentsViewModel() {
    var self = this;
    var locale = document.documentElement.lang || undefined;

    self.loading = ko.observable(false);
    self.preview = ko.observable(null);
    self.step = ko.observable('confirm');
    self.checkoutHtml = ko.observable('');
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
            + '?subject=' + encodeURIComponent('Odeme kalemleri hakkinda')
            + '&body=' + encodeURIComponent('Merhaba, odeme oncesi tahakkuk kalemlerimi kontrol etmek istiyorum.');
    });

    self.resultTitle = ko.computed(function () {
        var r = self.result();
        return r && r.success ? 'Odeme basarili' : 'Odeme basarisiz';
    });

    self.resultMessage = ko.computed(function () {
        var r = self.result();
        if (!r) return '';
        return r.success
            ? (r.message || 'Tahakkuk odemeniz basarili. Kalemler kapatildi.')
            : (r.error || 'Odeme basarisiz oldu.');
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
        new bootstrap.Modal(document.getElementById('paymentConfirmModal')).show();
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
            if (status !== 400) toastr.error(ajaxErrorMessage(xhr, 'Tahakkuklar alinamadi.'));
        }).always(function () {
            self.loading(false);
        });
    };

    self.openConfirm = function () {
        if (!self.hasPayableLines()) {
            toastr.info('Odenecek acik tahakkuk bulunmuyor.');
            return;
        }
        self.step('confirm');
        self.result(null);
        self.checkoutHtml('');
        showPaymentModal();
    };

    self.startCheckout = function () {
        self.step('checkout');
        self.loading(true);
        self.checkoutHtml('');
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
                self.checkoutHtml(raw);
            } else {
                toastr.error((data && data.error) || 'Odeme formu olusturulamadi.');
                self.step('confirm');
            }
        }).fail(function (xhr) {
            toastr.error(ajaxErrorMessage(xhr, 'Odeme baslatilamadi.'));
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
            self.result(data && typeof data === 'object' ? data : { success: false, error: 'Gecersiz yanit' });
            self.step('result');
            showPaymentModal();
            if (data && data.success) self.load();
        }).fail(function (xhr) {
            self.result({ success: false, error: ajaxErrorMessage(xhr, 'Odeme sonucu alinamadi.') });
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
            self.result({ success: false, error: e.data.error || 'Odeme basarisiz oldu.' });
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
