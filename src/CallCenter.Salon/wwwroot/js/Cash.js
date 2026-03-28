function CashViewModel() {
    var self = this;
    self.cashRegisters = ko.observableArray([]);
    self.transactions = ko.observableArray([]);
    self.selectedRegister = ko.observable(null);
    self.isSaving = ko.observable(false);

    self.registerForm = {
        name: ko.observable('')
    };

    self.transactionForm = {
        transactionTypeId: ko.observable(1),
        amount: ko.observable(0),
        description: ko.observable(''),
        paymentMethodId: ko.observable(1)
    };

    var transactionTypeNames = { 1: 'Gelir', 2: 'Gider', 3: 'Transfer' };
    var paymentMethodNames = { 1: 'Nakit', 2: 'Kredi Karti' };

    var registerModal, transactionModal;
    var transactionRegisterId = null;

    self.loadRegisters = function () {
        $.ajax({ url: '/proxy/sln-finance/cash-registers', method: 'GET' }).done(function (data) {
            self.cashRegisters(data.items || data);
        }).fail(function () {
            toastr.error('Kasa listesi yuklenemedi');
        });
    };

    self.loadTransactions = function (registerId) {
        $.ajax({ url: '/proxy/sln-finance/cash-registers/' + registerId + '/transactions', method: 'GET' }).done(function (data) {
            var items = data.items || data;
            items.forEach(function (t) {
                t.typeName = transactionTypeNames[t.transactionTypeId] || 'Bilinmiyor';
                t.paymentMethodName = paymentMethodNames[t.paymentMethodId] || '-';
            });
            self.transactions(items);
        }).fail(function () {
            toastr.error('Kasa hareketleri yuklenemedi');
        });
    };

    self.viewTransactions = function (register) {
        self.selectedRegister(register);
        self.loadTransactions(register.id);
    };

    self.clearSelection = function () {
        self.selectedRegister(null);
        self.transactions([]);
    };

    // Kasa CRUD
    self.openNewRegister = function () {
        self.registerForm.name('');
        registerModal.show();
    };

    self.saveRegister = function () {
        var data = { name: self.registerForm.name() };
        if (!data.name) { toastr.warning('Kasa adi zorunludur'); return; }

        self.isSaving(true);
        $.ajax({
            url: '/proxy/sln-finance/cash-registers',
            method: 'POST',
            contentType: 'application/json',
            data: JSON.stringify(data)
        }).done(function () {
            registerModal.hide();
            self.loadRegisters();
            toastr.success('Kasa olusturuldu');
            self.isSaving(false);
        }).fail(function (xhr) {
            toastr.error(xhr.responseJSON?.error || 'Kasa olusturulamadi');
            self.isSaving(false);
        });
    };

    // Islem CRUD
    self.openNewTransaction = function (register) {
        transactionRegisterId = register.id;
        self.transactionForm.transactionTypeId(1);
        self.transactionForm.amount(0);
        self.transactionForm.description('');
        self.transactionForm.paymentMethodId(1);
        transactionModal.show();
    };

    self.saveTransaction = function () {
        var data = {
            transactionTypeId: parseInt(self.transactionForm.transactionTypeId()) || 1,
            amount: parseFloat(self.transactionForm.amount()) || 0,
            description: self.transactionForm.description(),
            paymentMethodId: parseInt(self.transactionForm.paymentMethodId()) || 1
        };

        if (!data.description || !data.amount) {
            toastr.warning('Aciklama ve tutar zorunludur');
            return;
        }

        self.isSaving(true);
        $.ajax({
            url: '/proxy/sln-finance/cash-registers/' + transactionRegisterId + '/transactions',
            method: 'POST',
            contentType: 'application/json',
            data: JSON.stringify(data)
        }).done(function () {
            transactionModal.hide();
            self.loadRegisters();
            if (self.selectedRegister() && self.selectedRegister().id === transactionRegisterId) {
                self.loadTransactions(transactionRegisterId);
            }
            toastr.success('Islem kaydedildi');
            self.isSaving(false);
        }).fail(function (xhr) {
            toastr.error(xhr.responseJSON?.error || 'Islem kaydedilemedi');
            self.isSaving(false);
        });
    };

    $(document).ready(function () {
        registerModal = new bootstrap.Modal(document.getElementById('registerModal'));
        transactionModal = new bootstrap.Modal(document.getElementById('transactionModal'));
        self.loadRegisters();
    });
}

ko.applyBindings(new CashViewModel(), document.getElementById('cash-vm'));
