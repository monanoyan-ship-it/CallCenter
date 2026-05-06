/**
 * Iyzico checkoutform HTML'i genelde <script src="..."> icerir.
 * innerHTML ile enjekte edilen script'ler guvenlik nedeniyle calismaz; odeme formu gorunmez.
 * Script'leri DOM'a yeniden ekleyerek calistirir.
 */
function moduleT(key, fallback) {
    return (window.salonT || function (k, f) { return f || k; })(key, fallback);
}

var MODULE_LOCALE = document.documentElement.lang || undefined;

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
        container.innerHTML = '<p class="text-danger small mb-0">' + moduleT('salon.modules.checkout_inject_error', 'Ödeme formu yüklenirken hata oluştu. Sayfayı yenileyip tekrar deneyin.') + '</p>';
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
    var PACKAGE_NAMES = {
        1: moduleT('salon.modules.package.stock_finance', 'Stok Tedarik / Finans'),
        3: moduleT('salon.modules.package.loyalty_marketing', 'Müşteri Sadakati / Pazarlama'),
        5: moduleT('salon.modules.package.professional', 'Profesyonel'),
        6: moduleT('salon.modules.package.corporate', 'Kurumsal')
    };
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
            var gName = PACKAGE_NAMES[gId] || m.groupName || moduleT('salon.modules.package.other', 'Diğer');
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
            var gName = PACKAGE_NAMES[gId] || m.groupName || moduleT('salon.modules.package.other', 'Diğer');
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
            ? moduleT('salon.modules.platform_activation_title', 'Demo / abonelik aktivasyonu')
            : moduleT('salon.modules.platform_unpaid_title', 'Ödenmemiş platform dönemi');
    });
    self.platformBillingHint = ko.computed(function () {
        return self.hasUpcomingPlatformBilling()
            ? moduleT('salon.modules.platform_activation_hint', 'Demo bitişindeki ilk abonelik dönemini şimdiden kartla ödeyebilirsiniz.')
            : moduleT('salon.modules.platform_payment_hint', 'Iyzico güvenli ödeme; modül satın alma ile aynı kart altyapısı.');
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
        return moduleT('salon.modules.branch_summary', '{count} şube · şube eşik indirimi %{discount} · şube satırı {amount} ₺/ay (tüm şubeler × Temel Paket brüt, sonra eşik)')
            .replace('{count}', s.branchCount)
            .replace('{discount}', br)
            .replace('{amount}', net.toLocaleString(MODULE_LOCALE));
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
        return dt ? new Date(dt).toLocaleString(MODULE_LOCALE) : '-';
    };

    self.paymentAmountText = function (payment) {
        if (!payment || payment.amount == null) return '-';
        return Number(payment.amount).toLocaleString(MODULE_LOCALE, { minimumFractionDigits: 2 }) + ' ' + (payment.currency || 'TRY');
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
                        toastr.error((body && body.message) || moduleT('salon.modules.receipt_download_failed', 'Dekont indirilemedi.'));
                    }, function () {
                        toastr.error(moduleT('salon.modules.receipt_download_failed', 'Dekont indirilemedi.'));
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
                    toastr.success(moduleT('salon.panel.payments.receipt_downloaded', 'Dekont indirildi.'));
                });
            })
            .catch(function () {
                toastr.error(moduleT('salon.modules.receipt_download_failed', 'Dekont indirilemedi.'));
            });
    };

    self.downloadHavaleReceipt = function (payment) {
        if (!payment || !payment.uid) return;
        fetch('/proxy/payments/havale-receipt/' + payment.uid, { credentials: 'same-origin' })
            .then(function (response) {
                if (!response.ok) {
                    return response.json().then(function (body) {
                        toastr.error((body && body.message) || moduleT('salon.modules.bank_receipt_download_failed', 'Havale dekontu indirilemedi.'));
                    }, function () {
                        toastr.error(moduleT('salon.modules.bank_receipt_download_failed', 'Havale dekontu indirilemedi.'));
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
                    toastr.success(moduleT('salon.modules.bank_receipt_downloaded', 'Havale dekontu indirildi.'));
                });
            })
            .catch(function () {
                toastr.error(moduleT('salon.modules.bank_receipt_download_failed', 'Havale dekontu indirilemedi.'));
            });
    };

    self.retryPayment = function (payment) {
        if (!payment) return;
        if (payment.paymentTypeId === 2) {
            self.startPlatformAccrualPayment();
            return;
        }
        if (payment.paymentTypeId === 4 && payment.packageGroupId) {
            self.purchasePackage({ groupId: payment.packageGroupId, groupName: payment.paymentType || moduleT('salon.modules.package', 'Paket') });
            return;
        }
        toastr.warning(moduleT('salon.modules.retry_not_available', 'Bu ödeme için tekrar deneme akışı bulunamadı.'));
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
                toastr.warning(moduleT('salon.modules.price_list_fallback', 'Fiyat listesi yüklenemedi; varsayılan fiyatlar kullanılıyor.'));
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
                    toastr.error(moduleT('salon.modules.payment_status_failed', 'Ödeme durumu alınamadı.'));
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
        confirmModal(
            moduleT('salon.modules.cancel_module_title', 'Modül İptali'),
            moduleT('salon.modules.cancel_module_body', '{name} modülünü iptal etmek istediğinize emin misiniz?\\nİptal talebi admin onayına gönderilecektir.').replace('{name}', name),
            function () {
            confirmModal(moduleT('salon.modules.cancel_reason_title', 'İptal Sebebi'), moduleT('salon.modules.cancel_reason_body', 'İptal sebebini girebilirsiniz (zorunlu değil):'), function (notes) {
                $.ajax({
                    url: '/proxy/sln-module-requests',
                    method: 'POST',
                    contentType: 'application/json',
                    data: JSON.stringify({ moduleId: mod.id, requestTypeId: 2, notes: notes || null }),
                    success: function () { toastr.success(moduleT('salon.modules.cancel_request_created', 'İptal talebi oluşturuldu.')); self.load(); },
                    error: function (xhr) { toastr.error((xhr.responseJSON && xhr.responseJSON.message) || moduleT('salon.modules.request_create_failed', 'Talep oluşturulamadı.')); }
                });
            }, { input: true, inputLabel: moduleT('salon.modules.cancel_reason_label', 'İptal sebebi') });
        }, { confirmClass: 'btn-danger', confirmText: moduleT('salon.modules.request_cancel_button', 'İptal Talep Et') });
    };

    // Eski talep sistemi yerine satin alma akisi
    self.requestModule = self.purchaseModule;

    self.cancelPackage = function (pkg) {
        var name = pkg.groupName;
        var count = pkg.modules.length;
        confirmModal(
            moduleT('salon.modules.cancel_package_title', 'Paket İptali'),
            moduleT('salon.modules.cancel_package_body', '{name} paketini iptal etmek ister misiniz?\\n\\nİçindeki {count} modül için toplu iptal talebi oluşturulacak.')
                .replace('{name}', name)
                .replace('{count}', count),
            function () {
            var done = 0, errors = 0;
            pkg.modules.forEach(function (m) {
                $.ajax({
                    url: '/proxy/sln-module-requests',
                    method: 'POST',
                    contentType: 'application/json',
                    data: JSON.stringify({ moduleId: m.id, requestTypeId: 2, notes: moduleT('salon.modules.package_cancel_note', 'Paket iptali:') + ' ' + name })
                }).always(function (res, status) {
                    done++;
                    if (status === 'error') errors++;
                    if (done === pkg.modules.length) {
                        if (errors === 0) toastr.success(moduleT('salon.modules.package_cancel_created', 'Paket iptal talebi oluşturuldu ({count} modül).').replace('{count}', done));
                        else toastr.warning(moduleT('salon.modules.package_cancel_partial', '{ok}/{total} modül için iptal talebi oluşturuldu.').replace('{ok}', done - errors).replace('{total}', done));
                        self.load();
                    }
                });
            });
        }, { confirmClass: 'btn-danger', confirmText: moduleT('salon.modules.cancel_package_button', 'Paketi İptal Et') });
    };

    // === SATIN ALMA AKISI ===
    self.purchaseStep = ko.observable('preview'); // preview -> checkout -> result
    self.purchaseLoading = ko.observable(false);
    self.purchasePreview = ko.observable(null);
    self.purchaseResult = ko.observable(null);
    self.purchaseGroupId = ko.observable(null);
    self.purchaseModalTitle = ko.observable(moduleT('salon.modules.purchase_modal_title', 'Modül Satın Al'));
    /** Iyzico odeme formu (API'den gelen HTML); view'da iyzicoCheckoutHtml ile bagli */
    self.checkoutFormHtml = ko.observable('');

    self.purchaseResultTitle = ko.computed(function () {
        var r = self.purchaseResult();
        return r && r.success
            ? moduleT('salon.modules.payment_success_title', 'Ödeme Başarılı!')
            : moduleT('salon.modules.payment_failed_title', 'Ödeme Başarısız');
    });

    self.purchaseResultMessage = ko.computed(function () {
        var r = self.purchaseResult();
        if (!r) return '';
        if (r.success) return r.message || moduleT('salon.modules.module_activated', 'Modülünüz aktif edildi.');
        return r.error || moduleT('salon.modules.payment_failed_message', 'Ödeme başarısız oldu.');
    });

    self.purchaseResultRequiresSessionRefresh = ko.computed(function () {
        var r = self.purchaseResult();
        return !!(r && r.success && r.requiresSessionRefresh !== false);
    });

    self.purchaseStep.subscribe(function (step) {
        if (step !== 'checkout') self.checkoutFormHtml('');
    });

    self.purchasePackage = function (pkg) {
        self.purchaseModalTitle(moduleT('salon.modules.purchase_modal_title', 'Modül Satın Al'));
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
                toastr.error(moduleT('salon.modules.invalid_price_response', 'Fiyat yanıtı geçersiz.'));
                bootstrap.Modal.getInstance(document.getElementById('purchaseModal')).hide();
            }
        }).fail(function (xhr) {
            var msg = ajaxErrorMessage(xhr, moduleT('salon.modules.price_info_failed', 'Fiyat bilgisi alınamadı.'));
            toastr.error(msg);
            self.purchaseLoading(false);
            bootstrap.Modal.getInstance(document.getElementById('purchaseModal')).hide();
        });
    };

    self.purchaseModule = function (mod) {
        // Tek modul icin de ayni akis, groupId yerine moduleId gonder
        self.purchaseModalTitle(moduleT('salon.modules.purchase_modal_title', 'Modül Satın Al'));
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
                toastr.error(moduleT('salon.modules.invalid_price_response', 'Fiyat yanıtı geçersiz.'));
                bootstrap.Modal.getInstance(document.getElementById('purchaseModal')).hide();
            }
        }).fail(function (xhr) {
            var msg = ajaxErrorMessage(xhr, moduleT('salon.modules.price_info_failed', 'Fiyat bilgisi alınamadı.'));
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
                toastr.error(moduleT('salon.modules.invalid_payment_response', 'Ödeme yanıtı geçersiz.'));
                self.purchaseStep('preview');
                return;
            }
            var raw = data.htmlContent || data.checkoutFormHtml || data.HtmlContent || data.CheckoutFormHtml;
            if (data.success && raw) {
                self.checkoutFormHtml(raw);
            } else {
                toastr.error(data.error || moduleT('salon.modules.payment_form_create_failed', 'Ödeme formu oluşturulamadı.'));
                self.purchaseStep('preview');
            }
        }).fail(function (xhr) {
            var msg = ajaxErrorMessage(xhr, moduleT('salon.modules.payment_start_failed', 'Ödeme başlatılamadı.'));
            toastr.error(msg);
            self.purchaseLoading(false);
            self.purchaseStep('preview');
        });
    };

    /** Açık platform tahakkuku: api/payments/subscription-checkout (ücretli abonelik veya aboneliksiz salon platform borcu) */
    self.startPlatformAccrualPayment = function () {
        self.purchaseModalTitle(moduleT('salon.modules.subscription_payment_title', 'Abonelik Ödemesi'));
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
                toastr.error(moduleT('salon.modules.invalid_payment_response', 'Ödeme yanıtı geçersiz.'));
                self.purchaseStep('preview');
                bootstrap.Modal.getInstance(document.getElementById('purchaseModal')).hide();
                return;
            }
            var raw = data.htmlContent || data.checkoutFormHtml || data.HtmlContent || data.CheckoutFormHtml;
            if (data.success && raw) {
                self.checkoutFormHtml(raw);
            } else {
                toastr.error(data.error || moduleT('salon.modules.payment_form_create_failed', 'Ödeme formu oluşturulamadı.'));
                self.purchaseStep('preview');
                bootstrap.Modal.getInstance(document.getElementById('purchaseModal')).hide();
            }
        }).fail(function (xhr) {
            var msg = ajaxErrorMessage(xhr, moduleT('salon.modules.payment_start_failed', 'Ödeme başlatılamadı.'));
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
            self.purchaseResult(data && typeof data === 'object' ? data : { success: false, error: moduleT('salon.modules.invalid_response', 'Geçersiz yanıt') });
            self.purchaseStep('result');
            if (data && data.success) self.load();
        }).fail(function (xhr) {
            var msg = ajaxErrorMessage(xhr, moduleT('salon.modules.payment_result_failed', 'Ödeme sonucu alınamadı.'));
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
            self.purchaseResult({ success: false, error: e.data.error || moduleT('salon.modules.payment_failed_message', 'Ödeme başarısız oldu.') });
            self.purchaseStep('result');
        }
    });

    // Eski talep sistemi (requestPackage) yerine purchasePackage kullaniliyor
    self.requestPackage = self.purchasePackage;

    self.cancelRequest = function (req) {
        confirmModal(moduleT('salon.modules.cancel_request_title', 'Talep İptali'), moduleT('salon.modules.cancel_request_body', 'Bu talebi iptal etmek istiyor musunuz?'), function () {
            $.ajax({
                url: '/proxy/sln-module-requests/' + req.id,
                method: 'DELETE',
                success: function () { toastr.success(moduleT('salon.modules.request_cancelled', 'Talep iptal edildi.')); self.load(); },
                error: function (xhr) { toastr.error((xhr.responseJSON && xhr.responseJSON.message) || moduleT('salon.modules.cancel_failed', 'İptal edilemedi.')); }
            });
        }, { confirmClass: 'btn-danger', confirmText: moduleT('salon.panel.cancel.confirm', 'İptal Et') });
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
