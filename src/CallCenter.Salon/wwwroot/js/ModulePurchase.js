function PurchaseViewModel(moduleId) {
    var self = this;

    self.isLoading = ko.observable(true);
    self.isProcessing = ko.observable(false);
    self.moduleInfo = ko.observable(null);
    self.bankInfo = ko.observable(null);
    self.paymentMethod = ko.observable('kk');
    self.paymentResult = ko.observable(null);

    self.card = {
        holderName: ko.observable(''), number: ko.observable(''),
        expMonth: ko.observable(''), expYear: ko.observable(''),
        cvc: ko.observable(''), installment: ko.observable('1')
    };

    // Yil secenekleri
    var currentYear = new Date().getFullYear();
    self.yearOptions = [];
    for (var y = currentYear; y <= currentYear + 12; y++) self.yearOptions.push(String(y).slice(-2));

    self.loadModule = function () {
        $.get('/proxy/sln-module-requests/available', function (data) {
            var modules = Array.isArray(data) ? data : (data.items || data.data || []);
            var mod = modules.find(function (m) { return m.moduleId === moduleId; });
            if (mod) {
                self.moduleInfo({ name: mod.moduleName || mod.name, description: mod.description || '', monthlyPrice: mod.monthlyPrice || 0 });
            }
        }).always(function () { self.isLoading(false); });

        // Banka bilgileri
        $.get('/proxy/payment-config/bank-info', function (data) { self.bankInfo(data); }).fail(function () { });
    };

    self.formatCardNumber = function (vm, e) {
        var val = self.card.number().replace(/\D/g, '').substring(0, 16);
        var formatted = val.replace(/(\d{4})(?=\d)/g, '$1 ');
        self.card.number(formatted);
        return true;
    };

    self.payWithCard = function () {
        var num = self.card.number().replace(/\s/g, '');
        if (!self.card.holderName() || num.length < 15 || !self.card.expMonth() || !self.card.expYear() || !self.card.cvc()) {
            toastr.warning('Lutfen tum kart bilgilerini doldurun.'); return;
        }

        self.isProcessing(true);
        $.ajax({
            url: '/proxy/payments/module-purchase', method: 'POST',
            contentType: 'application/json',
            data: JSON.stringify({
                moduleId: moduleId,
                card: {
                    cardHolderName: self.card.holderName(),
                    cardNumber: num,
                    expireMonth: self.card.expMonth(),
                    expireYear: self.card.expYear(),
                    cvc: self.card.cvc(),
                    installment: parseInt(self.card.installment())
                }
            }),
            success: function (data) {
                self.paymentResult({ success: true, transactionId: data.transactionId });
            },
            error: function (xhr) {
                var msg = xhr.responseJSON ? xhr.responseJSON.message : 'Odeme islemi basarisiz oldu.';
                self.paymentResult({ success: false, error: msg });
            }
        }).always(function () { self.isProcessing(false); });
    };

    self.submitHavale = function () {
        confirmModal('Havale Onayi', 'Havaleyi yaptiginizi onayliyor musunuz?', function () {
            self.isProcessing(true);
            $.ajax({
                url: '/proxy/payments/havale-request', method: 'POST',
                contentType: 'application/json',
                data: JSON.stringify({ moduleId: moduleId }),
                success: function (data) {
                    self.paymentResult({ success: true, message: data.message });
                },
                error: function (xhr) {
                    var msg = xhr.responseJSON ? xhr.responseJSON.message : 'Havale talebi olusturulamadi.';
                    self.paymentResult({ success: false, error: msg });
                }
            }).always(function () { self.isProcessing(false); });
        });
    };

    self.copyIban = function () {
        var iban = self.bankInfo() ? (self.bankInfo().iban || self.bankInfo().IBAN) : '';
        navigator.clipboard.writeText(iban).then(function () { toastr.success('IBAN kopyalandi.'); });
    };

    self.loadModule();
}
