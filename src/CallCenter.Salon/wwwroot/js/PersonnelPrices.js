function PersonnelPricesViewModel() {
    var self = this;
    self.prices = ko.observableArray([]);
    self.revenueShares = ko.observableArray([]);
    self.staff = ko.observableArray([]);
    self.services = ko.observableArray([]);
    self.isSaving = ko.observable(false);

    self.priceForm = {
        personnelId: ko.observable(''),
        serviceId: ko.observable(''),
        price: ko.observable(0)
    };

    self.revenueForm = {
        personnelId: ko.observable(''),
        modelTypeId: ko.observable('1'),
        personnelSharePercent: ko.observable(60),
        monthlyRent: ko.observable(0),
        minimumGuarantee: ko.observable(0),
        isActive: ko.observable(true)
    };

    var modelTypeTexts = { 1: 'Yuzde', 2: 'Sabit Kira', 3: 'Hibrit' };
    self.modelTypeText = function (id) { return modelTypeTexts[id] || 'Bilinmiyor'; };

    var priceModal, revenueModal;

    self.loadPrices = function () {
        $.ajax({ url: '/proxy/sln-personnel-prices', method: 'GET' }).done(function (data) {
            self.prices(data.items || data);
        });
    };

    self.loadRevenueShares = function () {
        $.ajax({ url: '/proxy/sln-personnel-prices/revenue-shares', method: 'GET' }).done(function (data) {
            self.revenueShares(data.items || data);
        });
    };

    self.loadLookups = function () {
        $.ajax({ url: '/proxy/portal/personnel', method: 'GET' }).done(function (data) {
            self.staff(data.items || data);
        });
        $.ajax({ url: '/proxy/sln-services', method: 'GET' }).done(function (data) {
            var flat = [];
            (data.items || data || []).forEach(function (cat) {
                (cat.services || []).forEach(function (s) { flat.push(s); });
            });
            self.services(flat.length > 0 ? flat : (data.items || data));
        });
    };

    // ═══ Fiyat ═══

    self.openNewPrice = function () {
        self.priceForm.personnelId('');
        self.priceForm.serviceId('');
        self.priceForm.price(0);
        priceModal.show();
    };

    self.savePrice = function () {
        var data = {
            personnelId: parseInt(self.priceForm.personnelId()) || 0,
            serviceId: parseInt(self.priceForm.serviceId()) || 0,
            price: parseFloat(self.priceForm.price()) || 0
        };

        if (!data.personnelId || !data.serviceId) {
            toastr.warning('Personel ve hizmet secimi zorunludur');
            return;
        }

        self.isSaving(true);
        $.ajax({
            url: '/proxy/sln-personnel-prices', method: 'POST',
            contentType: 'application/json', data: JSON.stringify(data)
        }).done(function () {
            priceModal.hide();
            self.loadPrices();
            toastr.success('Fiyat kaydedildi');
            self.isSaving(false);
        }).fail(function (xhr) {
            toastr.error(xhr.responseJSON || 'Bir hata olustu');
            self.isSaving(false);
        });
    };

    self.removePrice = function (price) {
        confirmModal('Onay', 'Bu fiyati silmek istediginize emin misiniz?', function() {
            $.ajax({ url: '/proxy/sln-personnel-prices/' + price.id, method: 'DELETE' })
                .done(function () { self.loadPrices(); toastr.success('Fiyat silindi'); });
        });
    };

    // ═══ Hasilat ═══

    self.openNewRevenue = function () {
        self.revenueForm.personnelId('');
        self.revenueForm.modelTypeId('1');
        self.revenueForm.personnelSharePercent(60);
        self.revenueForm.monthlyRent(0);
        self.revenueForm.minimumGuarantee(0);
        self.revenueForm.isActive(true);
        revenueModal.show();
    };

    self.saveRevenue = function () {
        var data = {
            personnelId: parseInt(self.revenueForm.personnelId()) || 0,
            modelTypeId: parseInt(self.revenueForm.modelTypeId()) || 1,
            personnelSharePercent: parseFloat(self.revenueForm.personnelSharePercent()) || 60,
            monthlyRent: parseFloat(self.revenueForm.monthlyRent()) || 0,
            minimumGuarantee: parseFloat(self.revenueForm.minimumGuarantee()) || 0,
            isActive: self.revenueForm.isActive()
        };

        if (!data.personnelId) {
            toastr.warning('Personel secimi zorunludur');
            return;
        }

        self.isSaving(true);
        $.ajax({
            url: '/proxy/sln-personnel-prices/revenue-shares', method: 'POST',
            contentType: 'application/json', data: JSON.stringify(data)
        }).done(function () {
            revenueModal.hide();
            self.loadRevenueShares();
            toastr.success('Hasilat paylasimi kaydedildi');
            self.isSaving(false);
        }).fail(function (xhr) {
            toastr.error(xhr.responseJSON || 'Bir hata olustu');
            self.isSaving(false);
        });
    };

    self.removeRevenue = function (share) {
        confirmModal('Onay', 'Bu hasilat paylasimini silmek istediginize emin misiniz?', function() {
            $.ajax({ url: '/proxy/sln-personnel-prices/revenue-shares/' + share.id, method: 'DELETE' })
                .done(function () { self.loadRevenueShares(); toastr.success('Hasilat paylasimi silindi'); });
        });
    };

    $(document).ready(function () {
        priceModal = new bootstrap.Modal(document.getElementById('priceModal'));
        revenueModal = new bootstrap.Modal(document.getElementById('revenueModal'));
        self.loadPrices();
        self.loadRevenueShares();
        self.loadLookups();
    });
}

ko.applyBindings(new PersonnelPricesViewModel(), document.getElementById('personnelprices-vm'));
