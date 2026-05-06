function slnJsT(key, fallback) {
    return (window.salonT || function (k, f) { return f || k; })(key, fallback);
}

function PackagesViewModel() {
    var self = this;
    self.definitions = ko.observableArray([]);
    self.clientPackages = ko.observableArray([]);
    self.serviceList = ko.observableArray([]);
    self.clientList = ko.observableArray([]);
    self.isEditingDef = ko.observable(false);
    self.editingDefId = ko.observable(null);
    self.sellingDef = ko.observable(null);
    self.sellClientId = ko.observable(null);
    self.sellPaymentMethodId = ko.observable('1');
    self.isSaving = ko.observable(false);

    self.defForm = {
        name: ko.observable(''),
        description: ko.observable(''),
        serviceId: ko.observable(null),
        totalSessions: ko.observable(10),
        price: ko.observable(0),
        validDays: ko.observable(365)
    };

    self.clientAutocomplete = createAutocomplete(self.clientList, 'fullName', self.sellClientId);

    var defModal, sellModal;
    function readError(xhr) {
        if (typeof xhr.responseJSON === 'string') return xhr.responseJSON;
        return xhr.responseJSON?.error || xhr.responseJSON?.message || xhr.responseText || slnJsT('salon.common.error.generic', 'Hata');
    }

    self.loadData = function () {
        $.ajax({ url: '/proxy/sln-packages/definitions', method: 'GET' }).done(function (data) {
            self.definitions(data.items || data);
        });
        $.ajax({ url: '/proxy/sln-packages/client-packages', method: 'GET' }).done(function (data) {
            self.clientPackages(data.items || data);
        });
    };

    self.loadLookups = function () {
        $.ajax({ url: '/proxy/sln-services', method: 'GET' }).done(function (data) {
            self.serviceList(data.items || data);
        });
        $.ajax({ url: '/proxy/sln-clients?pageSize=1000', method: 'GET' }).done(function (data) {
            self.clientList(data.items || data);
        });
    };

    // Paket Tanimi CRUD
    self.openNewDef = function () {
        self.isEditingDef(false);
        self.editingDefId(null);
        self.defForm.name('');
        self.defForm.description('');
        self.defForm.serviceId(null);
        self.defForm.totalSessions(10);
        self.defForm.price(0);
        self.defForm.validDays(365);
        defModal.show();
    };

    self.openEditDef = function (def) {
        self.isEditingDef(true);
        self.editingDefId(def.id);
        self.defForm.name(def.name);
        self.defForm.description(def.description || '');
        self.defForm.serviceId(def.serviceId);
        self.defForm.totalSessions(def.totalSessions);
        self.defForm.price(def.price);
        self.defForm.validDays(def.validDays);
        defModal.show();
    };

    self.saveDef = function () {
        var data = {
            name: self.defForm.name(),
            description: self.defForm.description(),
            serviceId: parseInt(self.defForm.serviceId()) || 0,
            totalSessions: parseInt(self.defForm.totalSessions()) || 1,
            price: parseFloat(self.defForm.price()) || 0,
            validDays: parseInt(self.defForm.validDays()) || 365,
            isActive: true
        };
        if (!data.name || !data.serviceId) { toastr.warning(slnJsT('salon.packages.js.paket_adi_ve_hizmet_zorunludur', 'Paket adı ve hizmet zorunludur')); return; }

        self.isSaving(true);
        var url = '/proxy/sln-packages/definitions';
        var method = 'POST';
        if (self.isEditingDef()) { url += '/' + self.editingDefId(); method = 'PUT'; }

        $.ajax({ url: url, method: method, contentType: 'application/json', data: JSON.stringify(data) }).done(function () {
            defModal.hide();
            self.loadData();
            toastr.success(slnJsT('salon.packages.js.paket_tanimi_kaydedildi', 'Paket tanımı kaydedildi'));
            self.isSaving(false);
        }).fail(function (xhr) {
            toastr.error(readError(xhr));
            self.isSaving(false);
        });
    };

    self.removeDef = function (def) {
        confirmModal(slnJsT('salon.common.btn.confirm', 'Onayla'), "'" + def.name + "' paketini silmek istediginize emin misiniz?", function() {
            $.ajax({ url: '/proxy/sln-packages/definitions/' + def.id, method: 'DELETE' }).done(function () {
                self.loadData();
                toastr.success(slnJsT('salon.packages.js.paket_tanimi_silindi', 'Paket tanımı silindi'));
            });
        });
    };

    // Paket Satisi
    self.openSell = function (def) {
        self.sellingDef(def);
        self.sellClientId(null);
        self.sellPaymentMethodId('1');
        self.clientAutocomplete.clear();
        sellModal.show();
    };

    self.confirmSell = function () {
        if (!self.sellClientId()) {
            toastr.warning(slnJsT('salon.packages.js.paket_satisi_icin_musteri_secilmelidir', 'Paket satışı için müşteri seçilmelidir'));
            return;
        }

        var data = {
            packageDefinitionId: self.sellingDef().id,
            slnClientId: parseInt(self.sellClientId()),
            paymentMethodId: parseInt(self.sellPaymentMethodId()) || 1
        };

        self.isSaving(true);
        $.ajax({
            url: '/proxy/sln-packages/sell', method: 'POST',
            contentType: 'application/json', data: JSON.stringify(data)
        }).done(function () {
            sellModal.hide();
            self.loadData();
            toastr.success(slnJsT('salon.packages.js.paket_satildi_ve_tahsilat_kaydedildi', 'Paket satildi ve tahsilat kaydedildi'));
            self.isSaving(false);
        }).fail(function (xhr) {
            toastr.error(readError(xhr));
            self.isSaving(false);
        });
    };

    // Seans Kullan
    self.useSession = function (pkg) {
        confirmModal(slnJsT('salon.common.btn.confirm', 'Onayla'), '1 seans kullanilacak. Emin misiniz?', function() {
            $.ajax({
                url: '/proxy/sln-packages/use', method: 'POST',
                contentType: 'application/json',
                data: JSON.stringify({ clientPackageId: pkg.id, notes: 'Manuel paket kullanim' })
            }).done(function () {
                self.loadData();
                toastr.success('1 seans kullanildi');
            }).fail(function (xhr) {
                toastr.error(readError(xhr));
            });
        });
    };

    $(document).ready(function () {
        defModal = new bootstrap.Modal(document.getElementById('defModal'));
        sellModal = new bootstrap.Modal(document.getElementById('sellModal'));
        self.loadLookups();
        self.loadData();
    });
}

ko.applyBindings(new PackagesViewModel(), document.getElementById('packages-vm'));
