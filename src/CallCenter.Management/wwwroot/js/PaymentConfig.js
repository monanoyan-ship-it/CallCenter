function PaymentConfigViewModel() {
    var self = this;
    self.items = ko.observableArray([]);
    self.activeConfig = ko.observable(null);
    self.pendingHavale = ko.observableArray([]);
    self.isLoading = ko.observable(false);
    self.isSaving = ko.observable(false);
    self.formTitle = ko.observable('Yeni Saglayici');
    self.deleteId = null;

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
                success: function() { toastr.success('Havale onaylandi.'); self.loadPendingHavale(); },
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
                success: function() { toastr.success('Havale reddedildi.'); self.loadPendingHavale(); },
                error: function() { toastr.error('Reddetme hatasi.'); }
            });
        }, { input: true, inputLabel: 'Red nedeni (opsiyonel)', confirmText: 'Reddet', confirmClass: 'btn-danger' });
    };

    // Baslangic
    self.loadData();
    self.loadPendingHavale();
}

ko.applyBindings(new PaymentConfigViewModel(), document.getElementById('paymentconfig-vm'));
