function CashViewModel() {
    var self = this;
    self.cashRegisters = ko.observableArray([]);
    self.transactions = ko.observableArray([]);
    self.selectedRegister = ko.observable(null);
    self.isSaving = ko.observable(false);

    self.registerForm = {
        name: ko.observable(''),
        branchId: ko.observable(null)
    };
    self.branches = ko.observableArray([]);

    // Subeleri yukle (admin ise hepsi; sube muduru ise kendi subesi)
    self.loadBranches = function () {
        $.get('/proxy/sln-branches', function (data) {
            self.branches(data || []);
            // Tek sube varsa otomatik sec
            if (data && data.length === 1) self.registerForm.branchId(data[0].id);
        }).fail(function () { /* subeler modulu yok olabilir */ });
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
        // Varsayilan: tek sube varsa secili, coklu subede kullanici secer
        if (self.branches().length === 1) {
            self.registerForm.branchId(self.branches()[0].id);
        } else {
            self.registerForm.branchId(null);
        }
        registerModal.show();
    };

    self.saveRegister = function () {
        var data = {
            name: self.registerForm.name(),
            branchId: self.registerForm.branchId() ? parseInt(self.registerForm.branchId()) : null
        };
        if (!data.name) { toastr.warning('Kasa adi zorunludur'); return; }
        if (self.branches().length > 1 && !data.branchId) {
            toastr.warning('Sube seciniz'); return;
        }

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

    // ═══ Kasa Ac (Gun Basi) ═══
    self.openRegisterForm = {
        balanceType: ko.observable('auto'),
        openingBalance: ko.observable(0),
        lastClosingBalance: ko.observable(null)
    };

    var openRegisterModal;
    var openRegisterId = null;

    self.openCashRegister = function (register) {
        openRegisterId = register.id;
        self.openRegisterForm.balanceType('auto');
        self.openRegisterForm.openingBalance(0);
        self.openRegisterForm.lastClosingBalance(null);

        // Son kapanistan bakiye bilgisi al
        $.ajax({ url: '/proxy/sln-finance/cash-registers/' + register.id + '/daily-summary', method: 'GET' }).done(function (data) {
            if (data && data.closingBalance !== undefined) {
                self.openRegisterForm.lastClosingBalance(data.closingBalance);
            } else if (data && data.net !== undefined) {
                self.openRegisterForm.lastClosingBalance(data.net);
            }
        });
        openRegisterModal.show();
    };

    self.saveOpenRegister = function () {
        var balanceType = self.openRegisterForm.balanceType();
        var openingBalance = balanceType === 'manual'
            ? (parseFloat(self.openRegisterForm.openingBalance()) || 0)
            : null;

        var payload = { openingBalance: openingBalance };

        self.isSaving(true);
        $.ajax({
            url: '/proxy/sln-finance/cash-registers/' + openRegisterId + '/open',
            method: 'POST',
            contentType: 'application/json',
            data: JSON.stringify(payload)
        }).done(function () {
            openRegisterModal.hide();
            self.loadRegisters();
            toastr.success('Kasa acildi');
        }).fail(function (xhr) {
            toastr.error(xhr.responseJSON?.error || 'Kasa acilamadi');
        }).always(function () {
            self.isSaving(false);
        });
    };

    // ═══ Z Raporu ═══
    self.zReport = ko.observable(null);
    self.zReportLoading = ko.observable(false);

    var zReportModal;

    self.openZReport = function (register) {
        self.zReport(null);
        self.zReportLoading(true);
        zReportModal.show();

        $.ajax({ url: '/proxy/sln-finance/z-report/' + register.id, method: 'GET' }).done(function (data) {
            self.zReport(data);
        }).fail(function (xhr) {
            toastr.error(xhr.responseJSON?.error || 'Z Raporu yuklenemedi');
        }).always(function () {
            self.zReportLoading(false);
        });
    };

    // ═══ Gun Sonu Kasa Kapama ═══
    self.closingForm = {
        countedTotal: ko.observable(0),
        notes: ko.observable('')
    };
    self.dailySummary = ko.observable(null);
    self.closings = ko.observableArray([]);

    var closingModal;
    var closingRegisterId = null;

    self.openClosing = function (register) {
        closingRegisterId = register.id;
        self.closingForm.countedTotal(0);
        self.closingForm.notes('');
        self.dailySummary(null);

        $.ajax({ url: '/proxy/sln-finance/cash-registers/' + register.id + '/daily-summary', method: 'GET' }).done(function (data) {
            self.dailySummary(data);
        });
        closingModal.show();
    };

    self.saveClosing = function () {
        var data = {
            registerId: closingRegisterId,
            countedTotal: parseFloat(self.closingForm.countedTotal()) || 0,
            notes: self.closingForm.notes()
        };

        self.isSaving(true);
        $.ajax({
            url: '/proxy/sln-finance/cash-closings',
            method: 'POST',
            contentType: 'application/json',
            data: JSON.stringify(data)
        }).done(function () {
            closingModal.hide();
            self.loadRegisters();
            toastr.success('Gun sonu kapama yapildi');
            self.isSaving(false);
        }).fail(function (xhr) {
            toastr.error(xhr.responseJSON?.error || 'Kapama yapilamadi');
            self.isSaving(false);
        });
    };

    self.loadClosings = function () {
        $.ajax({ url: '/proxy/sln-finance/cash-closings', method: 'GET' }).done(function (data) {
            self.closings(data.items || data);
        });
    };

    $(document).ready(function () {
        registerModal = new bootstrap.Modal(document.getElementById('registerModal'));
        transactionModal = new bootstrap.Modal(document.getElementById('transactionModal'));
        closingModal = new bootstrap.Modal(document.getElementById('closingModal'));
        openRegisterModal = new bootstrap.Modal(document.getElementById('openRegisterModal'));
        zReportModal = new bootstrap.Modal(document.getElementById('zReportModal'));
        self.loadRegisters();
        self.loadClosings();
        self.loadBranches();
    });
}

ko.applyBindings(new CashViewModel(), document.getElementById('cash-vm'));
