/**
 * Iyzico checkoutform HTML'i genelde <script src="..."> icerir.
 * innerHTML ile enjekte edilen script'ler guvenlik nedeniyle calismaz; odeme formu gorunmez.
 * Script'leri DOM'a yeniden ekleyerek calistirir.
 */
function injectIyzicoCheckoutHtml(container, html) {
    if (!container) return;
    container.innerHTML = html;
    var scripts = Array.prototype.slice.call(container.querySelectorAll('script'));
    for (var i = 0; i < scripts.length; i++) {
        var old = scripts[i];
        var s = document.createElement('script');
        for (var j = 0; j < old.attributes.length; j++) {
            var a = old.attributes[j];
            s.setAttribute(a.name, a.value);
        }
        if (!old.src && old.textContent) s.textContent = old.textContent;
        if (old.parentNode) old.parentNode.replaceChild(s, old);
    }
}

/** KO: Iyzico HTML (script tag) — html binding script calistirmaz; ozel baglama */
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

function ModulesViewModel() {
    var self = this;
    self.activeModules = ko.observableArray([]);
    self.availableModules = ko.observableArray([]);
    self.requests = ko.observableArray([]);

    self.defaultModules = ko.computed(function () {
        return self.activeModules().filter(function (m) { return m.isDefault; });
    });

    // Paket sabit fiyatları — SalonModuleGroups.cs ile senkron
    var PACKAGE_PRICES = { 1: 400, 3: 1500, 5: 1500, 6: 200 };
    var PACKAGE_NAMES = { 1: 'Stok Tedarik / Finans', 3: 'Müşteri Sadakati / Pazarlama', 5: 'Profesyonel', 6: 'Kurumsal' };

    self.activeGroups = ko.computed(function () {
        var nonDefault = self.activeModules().filter(function (m) { return !m.isDefault && m.isActive; });
        var grouped = {};
        nonDefault.forEach(function (m) {
            var gId = m.groupId || 0;
            var gName = PACKAGE_NAMES[gId] || m.groupName || 'Diger';
            if (!grouped[gId]) grouped[gId] = { groupId: gId, groupName: gName, packagePrice: PACKAGE_PRICES[gId] || 0, modules: [] };
            grouped[gId].modules.push(m);
        });
        return Object.values(grouped).sort(function (a, b) { return a.groupId - b.groupId; });
    });

    self.availableGroups = ko.computed(function () {
        var all = self.availableModules();
        var grouped = {};
        all.forEach(function (m) {
            var gId = m.groupId || 0;
            var gName = PACKAGE_NAMES[gId] || m.groupName || 'Diger';
            if (!grouped[gId]) grouped[gId] = { groupId: gId, groupName: gName, packagePrice: PACKAGE_PRICES[gId] || 0, modules: [] };
            grouped[gId].modules.push(m);
        });
        return Object.values(grouped).sort(function (a, b) { return a.groupId - b.groupId; });
    });

    self.branchCount = ko.observable(1);

    // Aylik toplam = (Temel Paket 1700 + aktif grup paket fiyatları) × (1 + 0.9*(N-1)) (sube indirimi)
    self.baseMonthly = ko.computed(function () {
        var total = 1700; // Temel Paket zorunlu
        var activeGroupIds = {};
        self.activeModules().forEach(function (m) {
            if (!m.isDefault && m.isActive && m.groupId) activeGroupIds[m.groupId] = true;
        });
        Object.keys(activeGroupIds).forEach(function (gId) {
            total += PACKAGE_PRICES[gId] || 0;
        });
        return total;
    });
    self.branchMultiplier = ko.computed(function () {
        var n = self.branchCount() || 1;
        return 1 + 0.9 * (n - 1);
    });
    self.monthlyTotal = ko.computed(function () {
        return Math.round(self.baseMonthly() * self.branchMultiplier() * 100) / 100;
    });

    self.load = function () {
        $.get('/proxy/sln-module-requests', function (data) { self.requests(data || []); });
        $.get('/proxy/sln-module-requests/available', function (data) { self.availableModules(data || []); });
        $.get('/proxy/sln-module-requests/active', function (data) { self.activeModules(data || []); });
        $.get('/proxy/sln-branches?_nb=1', function (data) {
            var branches = Array.isArray(data) ? data : (data.items || []);
            var active = branches.filter(function (b) { return b.isActive !== false; }).length;
            self.branchCount(active > 0 ? active : 1);
        });
    };

    self.requestDeactivation = function (mod) {
        var name = mod.description || mod.systemName;
        confirmModal('Modul Iptali', name + ' modulunu iptal etmek istediginize emin misiniz?\nIptal talebi admin onayina gonderilecektir.', function () {
            confirmModal('Iptal Sebebi', 'Iptal sebebini girebilirsiniz (zorunlu degil):', function (notes) {
                $.ajax({
                    url: '/proxy/sln-module-requests',
                    method: 'POST',
                    contentType: 'application/json',
                    data: JSON.stringify({ moduleId: mod.id, requestTypeId: 2, notes: notes || null }),
                    success: function () { toastr.success('Iptal talebi olusturuldu.'); self.load(); },
                    error: function (xhr) { toastr.error(xhr.responseJSON?.message || 'Talep olusturulamadi.'); }
                });
            }, { input: true, inputLabel: 'Iptal sebebi' });
        }, { confirmClass: 'btn-danger', confirmText: 'Iptal Talep Et' });
    };

    // Eski talep sistemi yerine satin alma akisi
    self.requestModule = self.purchaseModule;

    self.cancelPackage = function (pkg) {
        var name = pkg.groupName;
        var count = pkg.modules.length;
        confirmModal('Paket Iptali', name + ' paketini iptal etmek ister misiniz?\n\nIçindeki ' + count + ' modül için toplu iptal talebi oluşturulacak.', function () {
            var done = 0, errors = 0;
            pkg.modules.forEach(function (m) {
                $.ajax({
                    url: '/proxy/sln-module-requests',
                    method: 'POST',
                    contentType: 'application/json',
                    data: JSON.stringify({ moduleId: m.id, requestTypeId: 2, notes: 'Paket iptali: ' + name })
                }).always(function (res, status) {
                    done++;
                    if (status === 'error') errors++;
                    if (done === pkg.modules.length) {
                        if (errors === 0) toastr.success('Paket iptal talebi olusturuldu (' + done + ' modül).');
                        else toastr.warning(done - errors + '/' + done + ' modul icin iptal talebi olusturuldu.');
                        self.load();
                    }
                });
            });
        }, { confirmClass: 'btn-danger', confirmText: 'Paketi İptal Et' });
    };

    // === SATIN ALMA AKISI ===
    self.purchaseStep = ko.observable('preview'); // preview -> checkout -> result
    self.purchaseLoading = ko.observable(false);
    self.purchasePreview = ko.observable(null);
    self.purchaseResult = ko.observable(null);
    self.purchaseGroupId = ko.observable(null);
    /** Iyzico odeme formu (API'den gelen HTML); view'da iyzicoCheckoutHtml ile bagli */
    self.checkoutFormHtml = ko.observable('');

    self.purchaseStep.subscribe(function (step) {
        if (step !== 'checkout') self.checkoutFormHtml('');
    });

    self.purchasePackage = function (pkg) {
        self.purchaseGroupId(pkg.groupId);
        self.purchaseStep('preview');
        self.purchasePreview(null);
        self.purchaseResult(null);
        self.purchaseLoading(true);

        var modal = new bootstrap.Modal(document.getElementById('purchaseModal'));
        modal.show();

        $.ajax({
            url: '/proxy/payments/package-preview',
            method: 'POST',
            contentType: 'application/json',
            data: JSON.stringify({ packageGroupId: pkg.groupId }),
            success: function (data) {
                self.purchasePreview(data);
                self.purchaseLoading(false);
            },
            error: function (xhr) {
                toastr.error(xhr.responseJSON?.message || 'Fiyat bilgisi alinamadi.');
                self.purchaseLoading(false);
                bootstrap.Modal.getInstance(document.getElementById('purchaseModal')).hide();
            }
        });
    };

    self.purchaseModule = function (mod) {
        // Tek modul icin de ayni akis, groupId yerine moduleId gonder
        self.purchaseGroupId(mod.groupId || mod.moduleGroupId);
        self.purchaseStep('preview');
        self.purchasePreview(null);
        self.purchaseResult(null);
        self.purchaseLoading(true);

        var modal = new bootstrap.Modal(document.getElementById('purchaseModal'));
        modal.show();

        $.ajax({
            url: '/proxy/payments/package-preview',
            method: 'POST',
            contentType: 'application/json',
            data: JSON.stringify({ packageGroupId: mod.groupId || mod.moduleGroupId }),
            success: function (data) {
                self.purchasePreview(data);
                self.purchaseLoading(false);
            },
            error: function (xhr) {
                toastr.error(xhr.responseJSON?.message || 'Fiyat bilgisi alinamadi.');
                self.purchaseLoading(false);
                bootstrap.Modal.getInstance(document.getElementById('purchaseModal')).hide();
            }
        });
    };

    self.startCheckout = function () {
        self.checkoutFormHtml('');
        self.purchaseStep('checkout');
        self.purchaseLoading(true);

        $.ajax({
            url: '/proxy/payments/package-checkout',
            method: 'POST',
            contentType: 'application/json',
            data: JSON.stringify({ packageGroupId: self.purchaseGroupId() }),
            success: function (data) {
                self.purchaseLoading(false);
                var raw = data.htmlContent || data.checkoutFormHtml || data.HtmlContent || data.CheckoutFormHtml;
                if (data.success && raw) {
                    self.checkoutFormHtml(raw);
                } else {
                    toastr.error(data.error || 'Odeme formu olusturulamadi.');
                    self.purchaseStep('preview');
                }
            },
            error: function (xhr) {
                toastr.error(xhr.responseJSON?.message || 'Odeme baslatilamadi.');
                self.purchaseLoading(false);
                self.purchaseStep('preview');
            }
        });
    };

    // Iyzico callback sonrasi sonuc kontrolu
    self.checkPaymentResult = function (token) {
        $.ajax({
            url: '/proxy/payments/package-result',
            method: 'POST',
            contentType: 'application/json',
            data: JSON.stringify({ token: token }),
            success: function (data) {
                self.purchaseResult(data);
                self.purchaseStep('result');
            },
            error: function () {
                self.purchaseResult({ success: false, error: 'Odeme sonucu alinamadi.' });
                self.purchaseStep('result');
            }
        });
    };

    // Iyzico postMessage listener (callback sonrasi)
    window.addEventListener('message', function (e) {
        if (e.data === 'payment-success' || (e.data && e.data.type === 'payment-success')) {
            self.purchaseResult({ success: true });
            self.purchaseStep('result');
        } else if (e.data === 'payment-failed' || (e.data && e.data.type === 'payment-failed')) {
            self.purchaseResult({ success: false, error: e.data.error || 'Odeme basarisiz oldu.' });
            self.purchaseStep('result');
        }
    });

    // Eski talep sistemi (requestPackage) yerine purchasePackage kullaniliyor
    self.requestPackage = self.purchasePackage;

    self.cancelRequest = function (req) {
        confirmModal('Talep Iptali', 'Bu talebi iptal etmek istiyor musunuz?', function () {
            $.ajax({
                url: '/proxy/sln-module-requests/' + req.id,
                method: 'DELETE',
                success: function () { toastr.success('Talep iptal edildi.'); self.load(); },
                error: function (xhr) { toastr.error(xhr.responseJSON?.message || 'Iptal edilemedi.'); }
            });
        }, { confirmClass: 'btn-danger', confirmText: 'Iptal Et' });
    };

    self.load();
}

ko.applyBindings(new ModulesViewModel(), document.getElementById('modules-vm'));
