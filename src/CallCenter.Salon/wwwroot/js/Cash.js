function CashViewModel() {
    var self = this;
    self.cashRegisters = ko.observableArray([]);
    self.transactions = ko.observableArray([]);
    self.selectedRegister = ko.observable(null);
    self.isSaving = ko.observable(false);

    self.registerForm = {
        name: ko.observable(''),
        type: ko.observable('Cash'),
        initialBalance: ko.observable(0),
        description: ko.observable('')
    };

    self.transactionForm = {
        type: ko.observable('Income'),
        amount: ko.observable(0),
        description: ko.observable(''),
        notes: ko.observable('')
    };

    var registerModal, transactionModal;
    var transactionRegisterId = null;

    self.loadRegisters = function () {
        $.ajax({ url: '/proxy/sln-finance/cash-registers', method: 'GET' }).done(function (data) {
            var items = data.items || data;
            items.forEach(function (r) {
                r.typeName = { Cash: 'Nakit', Bank: 'Banka', POS: 'POS' }[r.type] || r.type || '-';
            });
            self.cashRegisters(items);
        }).fail(function () {
            toastr.error('Kasa listesi yuklenemedi');
        });
    };

    self.loadTransactions = function (registerId) {
        $.ajax({ url: '/proxy/sln-finance/cash-registers/' + registerId + '/transactions', method: 'GET' }).done(function (data) {
            var items = data.items || data;
            items.forEach(function (t) {
                t.typeName = t.type === 'Income' ? 'Gelir' : 'Gider';
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
        self.registerForm.type('Cash');
        self.registerForm.initialBalance(0);
        self.registerForm.description('');
        registerModal.show();
    };

    self.saveRegister = function () {
        var data = {
            name: self.registerForm.name(),
            type: self.registerForm.type(),
            initialBalance: parseFloat(self.registerForm.initialBalance()) || 0,
            description: self.registerForm.description()
        };

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
        self.transactionForm.type('Income');
        self.transactionForm.amount(0);
        self.transactionForm.description('');
        self.transactionForm.notes('');
        transactionModal.show();
    };

    self.saveTransaction = function () {
        var data = {
            type: self.transactionForm.type(),
            amount: parseFloat(self.transactionForm.amount()) || 0,
            description: self.transactionForm.description(),
            notes: self.transactionForm.notes()
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
