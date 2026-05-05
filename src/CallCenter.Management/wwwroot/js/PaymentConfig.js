function PaymentConfigViewModel() {
    var self = this;
    self.items = ko.observableArray([]);
    self.activeConfig = ko.observable(null);
    self.pendingHavale = ko.observableArray([]);
    self.paymentHistory = ko.observableArray([]);
    self.paymentHistoryLoading = ko.observable(false);
    self.timeline = ko.observable(null);
    self.reconciliation = ko.observable(null);
    self.reconciliationLoading = ko.observable(false);
    self.isLoading = ko.observable(false);
    self.isSaving = ko.observable(false);
    self.formTitle = ko.observable('Yeni Saglayici');
    self.deleteId = null;

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

    function downloadFile(url, fallbackName, errorMessage) {
        fetch(url, { credentials: 'same-origin' })
            .then(function(response) {
                if (!response.ok) {
                    return response.json().then(function(body) {
                        toastr.error((body && body.message) || errorMessage);
                    }, function() {
                        toastr.error(errorMessage);
                    });
                }
                var fileName = fileNameFromDisposition(response.headers.get('Content-Disposition'), fallbackName);
                return response.blob().then(function(blob) {
                    var objectUrl = URL.createObjectURL(blob);
                    var a = document.createElement('a');
                    a.href = objectUrl;
                    a.download = fileName;
                    document.body.appendChild(a);
                    a.click();
                    a.remove();
                    URL.revokeObjectURL(objectUrl);
                });
            })
            .catch(function() { toastr.error(errorMessage); });
    }

    self.formatDate = function(value) {
        return value ? new Date(value).toLocaleString('tr-TR') : '-';
    };

    self.formatAmount = function(item) {
        if (!item || item.amount == null) return '-';
        return Number(item.amount).toLocaleString('tr-TR', { minimumFractionDigits: 2 }) + ' ' + (item.currency || 'TRY');
    };

    self.statusClass = function(item) {
        if (!item) return 'text-muted';
        if (item.statusId === 2) return 'text-success';
        if (item.statusId === 3) return 'text-danger';
        if (item.statusId === 4) return 'text-warning';
        return 'text-muted';
    };

    // Banka formu
    self.bankForm = {
        bankName: ko.observable(''), iban: ko.observable(''),
        accountHolder: ko.observable(''), description: ko.observable('')
    };

    // Provider formu
    self.form = {
        id: ko.observable(null),
        providerTypeId: ko.observable('1'),
        isSandbox: ko.observable(true),
        isActive: ko.observable(true),
        // Iyzico
        iyzicoApiKey: ko.observable(''), iyzicoSecretKey: ko.observable(''),
        // PayTR
        payTrMerchantId: ko.observable(''), payTrMerchantKey: ko.observable(''), payTrMerchantSalt: ko.observable(''),
        // Param
        paramClientCode: ko.observable(''), paramClientUsername: ko.observable(''),
        paramClientPassword: ko.observable(''), paramGuid: ko.observable('')
    };

    self.loadData = function() {
        self.isLoading(true);
        $.get('/proxy/payment-config', function(data) {
            var items = Array.isArray(data) ? data : (data.items || data.data || []);
            self.items(items);
            var active = items.find(function(i) { return i.isActive; });
            self.activeConfig(active || null);
        }).always(function() { self.isLoading(false); });

        // Banka bilgileri
        $.get('/proxy/payment-config/bank-info', function(data) {
            if (data) {
                self.bankForm.bankName(data.bankName || '');
                self.bankForm.iban(data.iban || data.IBAN || '');
                self.bankForm.accountHolder(data.accountHolder || '');
                self.bankForm.description(data.description || '');
            }
        });
    };

    self.loadPendingHavale = function() {
        $.get('/proxy/payments/pending-havale', function(data) {
            self.pendingHavale(Array.isArray(data) ? data : []);
        });
    };

    self.loadPaymentHistory = function() {
        self.paymentHistoryLoading(true);
        $.get('/proxy/payments/history?page=1', function(data) {
            self.paymentHistory(Array.isArray(data) ? data : []);
        }).fail(function() {
            self.paymentHistory([]);
        }).always(function() {
            self.paymentHistoryLoading(false);
        });
    };

    self.loadReconciliation = function() {
        self.reconciliationLoading(true);
        $.get('/proxy/payments/reconciliation', function(data) {
            self.reconciliation(data || null);
        }).fail(function() {
            self.reconciliation(null);
            toastr.error('Mutabakat raporu alinamadi.');
        }).always(function() {
            self.reconciliationLoading(false);
        });
    };

    self.resetForm = function() {
        self.form.id(null); self.form.providerTypeId('1');
        self.form.isSandbox(true); self.form.isActive(true);
        self.form.iyzicoApiKey(''); self.form.iyzicoSecretKey('');
        self.form.payTrMerchantId(''); self.form.payTrMerchantKey(''); self.form.payTrMerchantSalt('');
        self.form.paramClientCode(''); self.form.paramClientUsername('');
        self.form.paramClientPassword(''); self.form.paramGuid('');
    };

    self.openCreate = function() {
        self.resetForm(); self.formTitle('Yeni Saglayici');
        new bootstrap.Modal('#paymentModal').show();
    };

    self.openEdit = function(item) {
        self.form.id(item.id);
        self.form.providerTypeId(String(item.providerTypeId));
        self.form.isSandbox(item.isSandbox);
        self.form.isActive(item.isActive);
        // Credential'lar maskelenmis, bos birak (kullanici yeniden girer)
        self.form.iyzicoApiKey(''); self.form.iyzicoSecretKey('');
        self.form.payTrMerchantId(''); self.form.payTrMerchantKey(''); self.form.payTrMerchantSalt('');
        self.form.paramClientCode(''); self.form.paramClientUsername('');
        self.form.paramClientPassword(''); self.form.paramGuid('');
        self.formTitle('Saglayici Duzenle');
        new bootstrap.Modal('#paymentModal').show();
    };

    self.save = function() {
        self.isSaving(true);
        var payload = {
            providerTypeId: parseInt(self.form.providerTypeId()),
            isSandbox: self.form.isSandbox(),
            isActive: self.form.isActive(),
            iyzicoApiKey: self.form.iyzicoApiKey(),
            iyzicoSecretKey: self.form.iyzicoSecretKey(),
            payTrMerchantId: self.form.payTrMerchantId(),
            payTrMerchantKey: self.form.payTrMerchantKey(),
            payTrMerchantSalt: self.form.payTrMerchantSalt(),
            paramClientCode: self.form.paramClientCode(),
            paramClientUsername: self.form.paramClientUsername(),
            paramClientPassword: self.form.paramClientPassword(),
            paramGuid: self.form.paramGuid(),
            bankName: self.bankForm.bankName(),
            bankIBAN: self.bankForm.iban(),
            bankAccountHolder: self.bankForm.accountHolder(),
            bankDescription: self.bankForm.description()
        };

        var method = self.form.id() ? 'PUT' : 'POST';
        var url = self.form.id() ? '/proxy/payment-config/' + self.form.id() : '/proxy/payment-config';

        $.ajax({
            url: url, method: method, contentType: 'application/json', data: JSON.stringify(payload),
            success: function() {
                toastr.success('Kaydedildi.');
                bootstrap.Modal.getInstance(document.getElementById('paymentModal')).hide();
                self.loadData();
            },
            error: function(xhr) {
                var msg = xhr.responseJSON ? xhr.responseJSON.message : 'Kaydetme hatasi.';
                toastr.error(msg);
            }
        }).always(function() { self.isSaving(false); });
    };

    self.activateConfig = function(item) {
        confirmModal('Aktif Yap', '"' + item.providerName + ' (' + (item.isSandbox ? 'Sandbox' : 'Production') + ')" yapilandirmasini aktif etmek istiyor musunuz? Diger tum yapilandirmalar pasife alinacak.', function () {
            $.ajax({
                url: '/proxy/payment-config/' + item.id + '/activate', method: 'PUT',
                success: function() {
                    toastr.success(item.providerName + ' ' + (item.isSandbox ? '(Sandbox)' : '(Production)') + ' aktif edildi.');
                    self.loadData();
                },
                error: function(xhr) {
                    var msg = xhr.responseJSON ? xhr.responseJSON.message : 'Aktivasyon hatasi.';
                    toastr.error(msg);
                }
            });
        }, { confirmText: 'Aktif Et', confirmClass: 'btn-success' });
    };

    self.testConnection = function(item) {
        toastr.info('Baglanti testi baslatildi...');
        $.ajax({
            url: '/proxy/payment-config/' + item.id + '/test', method: 'POST',
            success: function(data) {
                if (data && data.success) toastr.success('Baglanti basarili! Credential\'lar gecerli.');
                else toastr.error('Test basarisiz: ' + (data.error || 'Bilinmeyen hata'));
                self.loadData();
            },
            error: function() { toastr.error('Baglanti testi yapilamadi.'); }
        });
    };

    self.confirmDelete = function(item) {
        self.deleteId = item.id;
        new bootstrap.Modal('#deleteModal').show();
    };

    self.executeDelete = function() {
        $.ajax({
            url: '/proxy/payment-config/' + self.deleteId, method: 'DELETE',
            success: function() {
                toastr.success('Silindi.');
                bootstrap.Modal.getInstance(document.getElementById('deleteModal')).hide();
                self.loadData();
            },
            error: function() { toastr.error('Silme hatasi.'); }
        });
    };

    self.confirmHavale = function(item) {
        confirmModal('Havale Onayi', 'Bu havaleyi onaylamak istediginize emin misiniz?', function () {
            $.ajax({
                url: '/proxy/payments/havale-confirm/' + item.uid, method: 'POST',
                success: function() { toastr.success('Havale onaylandi.'); self.loadPendingHavale(); self.loadPaymentHistory(); self.loadReconciliation(); },
                error: function(xhr) { toastr.error(xhr.responseJSON ? xhr.responseJSON.message : 'Onaylama hatasi.'); }
            });
        }, { confirmText: 'Onayla', confirmClass: 'btn-success' });
    };

    self.rejectHavale = function(item) {
        confirmModal('Havale Reddi', 'Red nedenini girin (opsiyonel):', function (reason) {
            if (reason === true) reason = null;
            $.ajax({
                url: '/proxy/payments/havale-reject/' + item.uid, method: 'POST',
                contentType: 'application/json', data: JSON.stringify({ reason: reason }),
                success: function() { toastr.success('Havale reddedildi.'); self.loadPendingHavale(); self.loadPaymentHistory(); self.loadReconciliation(); },
                error: function() { toastr.error('Reddetme hatasi.'); }
            });
        }, { input: true, inputLabel: 'Red nedeni (opsiyonel)', confirmText: 'Reddet', confirmClass: 'btn-danger' });
    };

    self.downloadHavaleReceipt = function(item) {
        if (!item || !item.uid) return;
        downloadFile('/proxy/payments/havale-receipt/' + item.uid, 'havale-dekont', 'Havale dekontu indirilemedi.');
    };

    self.downloadReceipt = function(item) {
        if (!item || !item.uid) return;
        downloadFile('/proxy/payments/receipt/' + item.uid, 'corplynk-dekont.html', 'Dekont indirilemedi.');
    };

    self.openTimeline = function(item) {
        if (!item || !item.uid) return;
        $.get('/proxy/payments/' + item.uid + '/timeline', function(data) {
            self.timeline(data || null);
            bootstrap.Modal.getOrCreateInstance(document.getElementById('timelineModal')).show();
        }).fail(function(xhr) {
            toastr.error(xhr.responseJSON ? xhr.responseJSON.message : 'Zaman cizelgesi alinamadi.');
        });
    };

    self.refundPayment = function(item) {
        if (!item || !item.uid) return;
        var maxAmount = Number(item.amount || 0);
        confirmModal('Iade', 'Iade tutarini girin. Bos birakirsaniz tam tutar iade edilir.', function(amountText) {
            var amount = parseFloat(String(amountText || '').replace(',', '.'));
            var payload = { amount: isNaN(amount) || amount <= 0 ? null : amount };
            $.ajax({
                url: '/proxy/payments/' + item.uid + '/refund',
                method: 'POST',
                contentType: 'application/json',
                data: JSON.stringify(payload),
                success: function() {
                    toastr.success('Iade kaydi olusturuldu.');
                    self.loadPaymentHistory();
                    self.loadReconciliation();
                },
                error: function(xhr) {
                    toastr.error(xhr.responseJSON ? xhr.responseJSON.message : 'Iade yapilamadi.');
                }
            });
        }, {
            input: true,
            inputLabel: 'Iade tutari (maks. ' + maxAmount.toLocaleString('tr-TR', { minimumFractionDigits: 2 }) + ')',
            confirmText: 'Iade Et',
            confirmClass: 'btn-warning'
        });
    };

    self.cancelPayment = function(item) {
        if (!item || !item.uid) return;
        confirmModal('Odeme Iptali', 'Iptal nedenini girin (opsiyonel):', function(reason) {
            if (reason === true) reason = null;
            $.ajax({
                url: '/proxy/payments/' + item.uid + '/cancel',
                method: 'POST',
                contentType: 'application/json',
                data: JSON.stringify({ reason: reason }),
                success: function() {
                    toastr.success('Odeme iptal edildi.');
                    self.loadPendingHavale();
                    self.loadPaymentHistory();
                    self.loadReconciliation();
                },
                error: function(xhr) {
                    toastr.error(xhr.responseJSON ? xhr.responseJSON.message : 'Odeme iptal edilemedi.');
                }
            });
        }, { input: true, inputLabel: 'Iptal nedeni', confirmText: 'Iptal Et', confirmClass: 'btn-danger' });
    };

    // Baslangic
    self.loadData();
    self.loadPendingHavale();
    self.loadPaymentHistory();
    self.loadReconciliation();
}

ko.applyBindings(new PaymentConfigViewModel(), document.getElementById('paymentconfig-vm'));
