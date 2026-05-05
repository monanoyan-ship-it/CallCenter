/**
 * Iyzico checkoutform HTML'i genelde <script src="..."> icerir.
 * innerHTML ile enjekte edilen script'ler guvenlik nedeniyle calismaz; odeme formu gorunmez.
 * Script'leri DOM'a yeniden ekleyerek calistirir.
 */
function injectIyzicoCheckoutHtml(container, html) {
    if (!container) return;
    try {
        container.innerHTML = html || '';
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
    } catch (e) {
        console.error('iyzico form inject', e);
        container.innerHTML = '<p class="text-danger small mb-0">Ödeme formu yüklenirken hata oluştu. Sayfayı yenileyip tekrar deneyin.</p>';
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

    // Baslik (grup adlari) — API yokken yedek; fiyat: aktif dönem (package-prices)
    var PACKAGE_NAMES = { 1: 'Stok Tedarik / Finans', 3: 'Müşteri Sadakati / Pazarlama', 5: 'Profesyonel', 6: 'Kurumsal' };
    /** API / hata: GetActiveSalonPackagePricesAsync ile ayni varsayimlar (SalonModuleGroups) */
    var PACKAGE_PRICE_FALLBACK = { 0: 1700, 1: 400, 3: 1500, 5: 1500, 6: 200 };
    self.packagePrices = ko.observable({});

    self.priceForGroup = function (gId) {
        self.packagePrices();
        var m = self.packagePrices() || {};
        var n = gId;
        if (gId == null || gId === '') n = 0;
        n = parseInt(n, 10);
        if (isNaN(n)) n = 0;
        var v = m[n];
        if (v == null) v = m[String(n)];
        if (v != null) return typeof v === 'number' ? v : parseFloat(String(v), 10);
        return PACKAGE_PRICE_FALLBACK[n] != null ? PACKAGE_PRICE_FALLBACK[n] : 0;
    };

    self.activeGroups = ko.computed(function () {
        self.packagePrices();
        var nonDefault = self.activeModules().filter(function (m) { return !m.isDefault && m.isActive; });
        var grouped = {};
        nonDefault.forEach(function (m) {
            var gId = m.groupId || 0;
            var gName = PACKAGE_NAMES[gId] || m.groupName || 'Diger';
            if (!grouped[gId]) grouped[gId] = { groupId: gId, groupName: gName, packagePrice: self.priceForGroup(gId), modules: [] };
            grouped[gId].modules.push(m);
        });
        return Object.values(grouped).sort(function (a, b) { return a.groupId - b.groupId; });
    });

    self.availableGroups = ko.computed(function () {
        self.packagePrices();
        var all = self.availableModules();
        var grouped = {};
        all.forEach(function (m) {
            var gId = m.groupId || 0;
            var gName = PACKAGE_NAMES[gId] || m.groupName || 'Diger';
            if (!grouped[gId]) grouped[gId] = { groupId: gId, groupName: gName, packagePrice: self.priceForGroup(gId), modules: [] };
            grouped[gId].modules.push(m);
        });
        return Object.values(grouped).sort(function (a, b) { return a.groupId - b.groupId; });
    });

    self.branchCount = ko.observable(1);
    self.dashboardSub = ko.observable(null);
    /** api/subscriptions/my → unpaidBillings (bilgi amaçlı) */
    self.platformUnpaid = ko.observableArray([]);
    /** Platform tahakkuku KK (subscription-checkout) */
    self.platformPayLoading = ko.observable(false);
    self.paymentHistory = ko.observableArray([]);
    self.paymentHistoryLoading = ko.observable(false);
    self.hasUpcomingPlatformBilling = ko.computed(function () {
        return self.platformUnpaid().some(function (x) { return x && x.isUpcoming; });
    });
    self.platformBillingTitle = ko.computed(function () {
        return self.hasUpcomingPlatformBilling()
            ? 'Demo / abonelik aktivasyonu'
            : 'Odenmemis platform donemi';
    });
    self.platformBillingHint = ko.computed(function () {
        return self.hasUpcomingPlatformBilling()
            ? 'Demo bitisindeki ilk abonelik donemini simdiden kartla odeyebilirsiniz.'
            : 'Iyzico guvenli odeme; modul satin alma ile ayni kart altyapisi.';
    });

    // Paket + temel (çoklu şubede Temel Paket yalnız şube satırında) — şube tutarı API'de
    self.baseMonthly = ko.computed(function () {
        self.packagePrices();
        var multi = self.branchCount() > 1;
        var total = multi ? 0 : self.priceForGroup(0);
        var activeGroupIds = {};
        self.activeModules().forEach(function (m) {
            if (!m.isDefault && m.isActive && m.groupId) activeGroupIds[m.groupId] = true;
        });
        Object.keys(activeGroupIds).forEach(function (gId) {
            total += self.priceForGroup(gId);
        });
        return total;
    });

    // Dashboard ile aynı tahmin (plan şube fiyatı + dönem eşikleri)
    self.displayMonthlyTotal = ko.computed(function () {
        var s = self.dashboardSub();
        if (s && typeof s.monthlyTotal === 'number') return s.monthlyTotal;
        return self.baseMonthly();
    });

    self.branchSummaryText = ko.computed(function () {
        var s = self.dashboardSub();
        if (!s || !s.branchCount || s.branchCount <= 1) return '';
        var br = typeof s.branchDiscountPercent === 'number' ? s.branchDiscountPercent : 0;
        var net = typeof s.netBranchMonthly === 'number' ? s.netBranchMonthly : 0;
        return s.branchCount + ' şube · şube eşik indirimi %' + br + ' · şube satırı ' + net.toLocaleString('tr-TR') + ' ₺/ay (tüm şubeler × Temel Paket brüt, sonra eşik)';
    });

    /**
     * dataType: 'json' kullanma: boş gövde / HTML hata sayfası jQuery'de JSON.parse → SyntaxError üretir.
     * Metin alıp burada güvenli parse et.
     */
    function parseAjaxBody(text, xhr) {
        if (xhr && (xhr.status === 204 || xhr.status === 205)) return null;
        if (text == null) return null;
        var t = String(text).trim();
        if (!t) return null;
        try {
            return JSON.parse(t);
        } catch (e) {
            return null;
        }
    }

    function ajaxErrorMessage(xhr, fallback) {
        if (xhr && xhr.responseJSON) {
            return xhr.responseJSON.error || xhr.responseJSON.message || fallback;
        }
        var body = parseAjaxBody(xhr && xhr.responseText, xhr);
        return (body && (body.error || body.message)) || fallback;
    }

    function fileNameFromDisposition(disposition, fallback) {
        if (!disposition) return fallback;
        var utf8name = /filename\*=UTF-8''([^;\n]+)/i.exec(disposition);
        var quoted = /filename="([^"]+)"/i.exec(disposition);
        var simple = /filename=([^;\n]+)/i.exec(disposition);
        if (utf8name && utf8name[1]) return decodeURIComponent(utf8name[1].trim());
        if (quoted && quoted[1]) return quoted[1].trim();
        if (simple && simple[1]) return simple[1].replace(/"/g, '').trim();
        return fallback;
    }

    self.paymentStatusClass = function (payment) {
        if (!payment) return 'text-muted';
        if (payment.statusId === 2) return 'text-success';
        if (payment.statusId === 3) return 'text-danger';
        if (payment.statusId === 4) return 'text-warning';
        if (payment.statusId === 5) return 'text-muted';
        return 'text-muted';
    };

    self.paymentDateText = function (payment) {
        var dt = payment && (payment.completedAt || payment.createdAt);
        return dt ? new Date(dt).toLocaleString('tr-TR') : '-';
    };

    self.paymentAmountText = function (payment) {
        if (!payment || payment.amount == null) return '-';
        return Number(payment.amount).toLocaleString('tr-TR', { minimumFractionDigits: 2 }) + ' ' + (payment.currency || 'TRY');
    };

    self.loadPaymentHistory = function () {
        self.paymentHistoryLoading(true);
        $.ajax({ url: '/proxy/payments/history?page=1', dataType: 'text', cache: false })
            .done(function (text, st, xhr) {
                var d = parseAjaxBody(text, xhr);
                self.paymentHistory(Array.isArray(d) ? d : []);
            })
            .fail(function () {
                self.paymentHistory([]);
            })
            .always(function () {
                self.paymentHistoryLoading(false);
            });
    };

    self.downloadReceipt = function (payment) {
        if (!payment || !payment.uid) return;
        fetch('/proxy/payments/receipt/' + payment.uid, { credentials: 'same-origin' })
            .then(function (response) {
                if (!response.ok) {
                    return response.json().then(function (body) {
                        toastr.error((body && body.message) || 'Dekont indirilemedi.');
                    }, function () {
                        toastr.error('Dekont indirilemedi.');
                    });
                }

                var fileName = fileNameFromDisposition(response.headers.get('Content-Disposition'), 'corplynk-dekont.html');
                return response.blob().then(function (blob) {
                    var url = URL.createObjectURL(blob);
                    var a = document.createElement('a');
                    a.href = url;
                    a.download = fileName;
                    document.body.appendChild(a);
                    a.click();
                    a.remove();
                    URL.revokeObjectURL(url);
                    toastr.success('Dekont indirildi.');
                });
            })
            .catch(function () {
                toastr.error('Dekont indirilemedi.');
            });
    };

    self.downloadHavaleReceipt = function (payment) {
        if (!payment || !payment.uid) return;
        fetch('/proxy/payments/havale-receipt/' + payment.uid, { credentials: 'same-origin' })
            .then(function (response) {
                if (!response.ok) {
                    return response.json().then(function (body) {
                        toastr.error((body && body.message) || 'Havale dekontu indirilemedi.');
                    }, function () {
                        toastr.error('Havale dekontu indirilemedi.');
                    });
                }

                var fileName = fileNameFromDisposition(response.headers.get('Content-Disposition'), 'havale-dekont');
                return response.blob().then(function (blob) {
                    var url = URL.createObjectURL(blob);
                    var a = document.createElement('a');
                    a.href = url;
                    a.download = fileName;
                    document.body.appendChild(a);
                    a.click();
                    a.remove();
                    URL.revokeObjectURL(url);
                    toastr.success('Havale dekontu indirildi.');
                });
            })
            .catch(function () {
                toastr.error('Havale dekontu indirilemedi.');
            });
    };

    self.retryPayment = function (payment) {
        if (!payment) return;
        if (payment.paymentTypeId === 2) {
            self.startPlatformAccrualPayment();
            return;
        }
        if (payment.paymentTypeId === 4 && payment.packageGroupId) {
            self.purchasePackage({ groupId: payment.packageGroupId, groupName: payment.paymentType || 'Paket' });
            return;
        }
        toastr.warning('Bu odeme icin tekrar deneme akisi bulunamadi.');
    };

    var initialPlatformPayRequested = false;
    try {
        initialPlatformPayRequested = new URLSearchParams(window.location.search).get('pay') === 'subscription';
    } catch (e) {
        initialPlatformPayRequested = false;
    }
    var initialPlatformPayHandled = false;

    function maybeOpenInitialPlatformPayment() {
        if (!initialPlatformPayRequested || initialPlatformPayHandled) return;
        initialPlatformPayHandled = true;
        if (self.platformUnpaid().length > 0) {
            self.startPlatformAccrualPayment();
        } else {
            self.startPlatformAccrualPayment();
        }
    }

    self.load = function () {
        $.ajax({ url: '/proxy/sln-module-requests/package-prices', dataType: 'text', cache: false })
            .done(function (text, st, xhr) {
                var d = parseAjaxBody(text, xhr);
                self.packagePrices(d && typeof d === 'object' && !Array.isArray(d) ? d : {});
            })
            .fail(function () {
                toastr.warning('Fiyat listesi yüklenemedi; varsayilan fiyatlar kullaniliyor.');
                self.packagePrices({});
            });

        $.ajax({ url: '/proxy/sln-module-requests', dataType: 'text', cache: false })
            .done(function (text, st, xhr) {
                var d = parseAjaxBody(text, xhr);
                self.requests(Array.isArray(d) ? d : []);
            })
            .fail(function () { self.requests([]); });

        $.ajax({ url: '/proxy/sln-module-requests/available', dataType: 'text', cache: false })
            .done(function (text, st, xhr) {
                var d = parseAjaxBody(text, xhr);
                self.availableModules(Array.isArray(d) ? d : []);
            })
            .fail(function () { self.availableModules([]); });

        $.ajax({ url: '/proxy/sln-module-requests/active', dataType: 'text', cache: false })
            .done(function (text, st, xhr) {
                var d = parseAjaxBody(text, xhr);
                self.activeModules(Array.isArray(d) ? d : []);
            })
            .fail(function () { self.activeModules([]); });

        $.ajax({ url: '/proxy/sln-dashboard', dataType: 'text', cache: false })
            .done(function (text, st, xhr) {
                var d = parseAjaxBody(text, xhr);
                self.dashboardSub(d && d.subscription ? d.subscription : null);
            })
            .fail(function () { self.dashboardSub(null); });

        $.ajax({ url: '/proxy/subscriptions/my', dataType: 'text', cache: false })
            .done(function (text, st, xhr) {
                var d = parseAjaxBody(text, xhr);
                var u = d && Array.isArray(d.unpaidBillings) ? d.unpaidBillings : [];
                self.platformUnpaid(u.filter(function (x) {
                    var t = Number(x && x.total);
                    return !isNaN(t) && t > 0;
                }));
                maybeOpenInitialPlatformPayment();
            })
            .fail(function () {
                self.platformUnpaid([]);
                if (initialPlatformPayRequested && !initialPlatformPayHandled) {
                    initialPlatformPayHandled = true;
                    toastr.error('Odeme durumu alinamadi.');
                }
            });

        self.loadPaymentHistory();

        $.ajax({ url: '/proxy/sln-branches?_nb=1', dataType: 'text', cache: false })
            .done(function (text, st, xhr) {
                var d = parseAjaxBody(text, xhr);
                var branches = Array.isArray(d) ? d : (d && Array.isArray(d.items) ? d.items : []);
                var active = branches.filter(function (b) { return b.isActive !== false; }).length;
                self.branchCount(active > 0 ? active : 1);
            })
            .fail(function () { self.branchCount(1); });
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
                    error: function (xhr) { toastr.error((xhr.responseJSON && xhr.responseJSON.message) || 'Talep olusturulamadi.'); }
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
    self.purchaseModalTitle = ko.observable('Modul Satin Al');
    /** Iyzico odeme formu (API'den gelen HTML); view'da iyzicoCheckoutHtml ile bagli */
    self.checkoutFormHtml = ko.observable('');

    self.purchaseResultTitle = ko.computed(function () {
        var r = self.purchaseResult();
        return r && r.success ? 'Odeme Basarili!' : 'Odeme Basarisiz';
    });

    self.purchaseResultMessage = ko.computed(function () {
        var r = self.purchaseResult();
        if (!r) return '';
        if (r.success) return r.message || 'Modulunuz aktif edildi.';
        return r.error || 'Odeme basarisiz oldu.';
    });

    self.purchaseResultRequiresSessionRefresh = ko.computed(function () {
        var r = self.purchaseResult();
        return !!(r && r.success && r.requiresSessionRefresh !== false);
    });

    self.purchaseStep.subscribe(function (step) {
        if (step !== 'checkout') self.checkoutFormHtml('');
    });

    self.purchasePackage = function (pkg) {
        self.purchaseModalTitle('Modul Satin Al');
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
            dataType: 'text',
            data: JSON.stringify({ packageGroupId: pkg.groupId })
        }).done(function (text, st, xhr) {
            var data = parseAjaxBody(text, xhr);
            self.purchasePreview(data && typeof data === 'object' ? data : null);
            self.purchaseLoading(false);
            if (data && data.error) {
                toastr.error(data.error);
                bootstrap.Modal.getInstance(document.getElementById('purchaseModal')).hide();
                return;
            }
            if (!data || typeof data !== 'object') {
                toastr.error('Fiyat yaniti gecersiz.');
                bootstrap.Modal.getInstance(document.getElementById('purchaseModal')).hide();
            }
        }).fail(function (xhr) {
            var msg = ajaxErrorMessage(xhr, 'Fiyat bilgisi alinamadi.');
            toastr.error(msg);
            self.purchaseLoading(false);
            bootstrap.Modal.getInstance(document.getElementById('purchaseModal')).hide();
        });
    };

    self.purchaseModule = function (mod) {
        // Tek modul icin de ayni akis, groupId yerine moduleId gonder
        self.purchaseModalTitle('Modul Satin Al');
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
            dataType: 'text',
            data: JSON.stringify({ packageGroupId: mod.groupId || mod.moduleGroupId })
        }).done(function (text, st, xhr) {
            var data = parseAjaxBody(text, xhr);
            self.purchasePreview(data && typeof data === 'object' ? data : null);
            self.purchaseLoading(false);
            if (data && data.error) {
                toastr.error(data.error);
                bootstrap.Modal.getInstance(document.getElementById('purchaseModal')).hide();
                return;
            }
            if (!data || typeof data !== 'object') {
                toastr.error('Fiyat yaniti gecersiz.');
                bootstrap.Modal.getInstance(document.getElementById('purchaseModal')).hide();
            }
        }).fail(function (xhr) {
            var msg = ajaxErrorMessage(xhr, 'Fiyat bilgisi alinamadi.');
            toastr.error(msg);
            self.purchaseLoading(false);
            bootstrap.Modal.getInstance(document.getElementById('purchaseModal')).hide();
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
            dataType: 'text',
            data: JSON.stringify({ packageGroupId: self.purchaseGroupId() })
        }).done(function (text, st, xhr) {
            self.purchaseLoading(false);
            var data = parseAjaxBody(text, xhr);
            if (!data || typeof data !== 'object') {
                toastr.error('Odeme yaniti gecersiz.');
                self.purchaseStep('preview');
                return;
            }
            var raw = data.htmlContent || data.checkoutFormHtml || data.HtmlContent || data.CheckoutFormHtml;
            if (data.success && raw) {
                self.checkoutFormHtml(raw);
            } else {
                toastr.error(data.error || 'Odeme formu olusturulamadi.');
                self.purchaseStep('preview');
            }
        }).fail(function (xhr) {
            var msg = ajaxErrorMessage(xhr, 'Odeme baslatilamadi.');
            toastr.error(msg);
            self.purchaseLoading(false);
            self.purchaseStep('preview');
        });
    };

    /** Açık platform tahakkuku: api/payments/subscription-checkout (ücretli abonelik veya aboneliksiz salon platform borcu) */
    self.startPlatformAccrualPayment = function () {
        self.purchaseModalTitle('Abonelik Odemesi');
        self.purchasePreview(null);
        self.purchaseResult(null);
        self.purchaseGroupId(null);
        self.checkoutFormHtml('');
        self.purchaseStep('checkout');
        self.purchaseLoading(true);
        self.platformPayLoading(true);

        var modal = new bootstrap.Modal(document.getElementById('purchaseModal'));
        modal.show();

        $.ajax({
            url: '/proxy/payments/subscription-checkout',
            method: 'POST',
            contentType: 'application/json',
            dataType: 'text',
            data: '{}'
        }).done(function (text, st, xhr) {
            self.purchaseLoading(false);
            self.platformPayLoading(false);
            var data = parseAjaxBody(text, xhr);
            if (!data || typeof data !== 'object') {
                toastr.error('Odeme yaniti gecersiz.');
                self.purchaseStep('preview');
                bootstrap.Modal.getInstance(document.getElementById('purchaseModal')).hide();
                return;
            }
            var raw = data.htmlContent || data.checkoutFormHtml || data.HtmlContent || data.CheckoutFormHtml;
            if (data.success && raw) {
                self.checkoutFormHtml(raw);
            } else {
                toastr.error(data.error || 'Odeme formu olusturulamadi.');
                self.purchaseStep('preview');
                bootstrap.Modal.getInstance(document.getElementById('purchaseModal')).hide();
            }
        }).fail(function (xhr) {
            var msg = ajaxErrorMessage(xhr, 'Odeme baslatilamadi.');
            toastr.error(msg);
            self.purchaseLoading(false);
            self.platformPayLoading(false);
            self.purchaseStep('preview');
            bootstrap.Modal.getInstance(document.getElementById('purchaseModal')).hide();
        });
    };

    // Iyzico callback sonrasi (proxy API /package-result; token ile sunucu durumu)
    self.checkPaymentResult = function (token, onComplete) {
        self.purchaseLoading(true);
        $.ajax({
            url: '/proxy/payments/package-result',
            method: 'POST',
            contentType: 'application/json',
            dataType: 'text',
            data: JSON.stringify({ token: token })
        }).done(function (text, st, xhr) {
            var data = parseAjaxBody(text, xhr);
            self.purchaseResult(data && typeof data === 'object' ? data : { success: false, error: 'Gecersiz yanit' });
            self.purchaseStep('result');
            if (data && data.success) self.load();
        }).fail(function (xhr) {
            var msg = ajaxErrorMessage(xhr, 'Odeme sonucu alinamadi.');
            self.purchaseResult({ success: false, error: msg });
            self.purchaseStep('result');
        }).always(function () {
            self.purchaseLoading(false);
            if (typeof onComplete === 'function') onComplete();
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
                error: function (xhr) { toastr.error((xhr.responseJSON && xhr.responseJSON.message) || 'Iptal edilemedi.'); }
            });
        }, { confirmClass: 'btn-danger', confirmText: 'Iptal Et' });
    };

    self.load();
}

var modulesVm = new ModulesViewModel();
ko.applyBindings(modulesVm, document.getElementById('modules-vm'));

/** Iyzico tam sayfa / yonlendirme donusu: API callback -> Salon /Modules?iyzicoToken=... */
(function iyzicoBrowserReturnFromUrl() {
    var p = new URLSearchParams(window.location.search);
    var token = p.get('iyzicoToken');
    if (!token) return;

    var modalEl = document.getElementById('purchaseModal');
    if (!modalEl) return;

    modulesVm.checkoutFormHtml('');
    modulesVm.purchasePreview(null);
    modulesVm.purchaseResult(null);
    modulesVm.purchaseStep('result');
    modulesVm.purchaseLoading(true);

    var modal = bootstrap.Modal.getOrCreateInstance(modalEl);
    modal.show();

    modulesVm.checkPaymentResult(token, function () {
        var u = new URL(window.location.href);
        u.searchParams.delete('iyzicoToken');
        u.searchParams.delete('iyzicoError');
        var q = u.searchParams.toString();
        window.history.replaceState({}, '', u.pathname + (q ? '?' + q : '') + u.hash);
    });
})();
