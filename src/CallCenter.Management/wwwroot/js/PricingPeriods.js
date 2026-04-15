function PricingPeriodsViewModel() {
    var self = this;
    self.periods = ko.observableArray([]);
    self.selectedPeriodId = ko.observable(null);
    self.selectedPeriod = ko.observable(null);
    self.isSaving = ko.observable(false);

    var today = new Date();
    var endOfYear = new Date(today.getFullYear(), 11, 31);
    self.newPeriod = {
        name: ko.observable(today.getFullYear() + ' Dönemi'),
        startDate: ko.observable(today.toISOString().substring(0, 10)),
        endDate: ko.observable(endOfYear.toISOString().substring(0, 10))
    };

    var newPeriodModal;

    self.loadPeriods = function () {
        $.get('/proxy/service-pricing/periods', function (data) {
            self.periods(data || []);
        });
    };

    self.selectPeriod = function (p) {
        self.selectedPeriodId(p.id);
        $.get('/proxy/service-pricing/periods/' + p.id, function (data) {
            self.selectedPeriod(data);
        });
    };

    self.openNewPeriod = function () {
        if (!newPeriodModal) newPeriodModal = new bootstrap.Modal(document.getElementById('newPeriodModal'));
        newPeriodModal.show();
    };

    self.createPeriod = function () {
        var payload = {
            name: self.newPeriod.name(),
            startDate: self.newPeriod.startDate() + 'T00:00:00Z',
            endDate: self.newPeriod.endDate() + 'T00:00:00Z'
        };
        if (!payload.name || !self.newPeriod.startDate() || !self.newPeriod.endDate()) {
            toastr.warning('Tüm alanlar zorunlu');
            return;
        }
        self.isSaving(true);
        $.ajax({
            url: '/proxy/service-pricing/periods',
            method: 'POST',
            contentType: 'application/json',
            data: JSON.stringify(payload)
        }).done(function (res) {
            toastr.success('Dönem oluşturuldu (' + (res.itemCount || 0) + ' kalem)');
            newPeriodModal.hide();
            self.loadPeriods();
        }).fail(function (xhr) {
            toastr.error(xhr.responseJSON?.message || 'Oluşturulamadı');
        }).always(function () { self.isSaving(false); });
    };

    var saveTimer;
    self.saveItemPrice = function (itemId, price) {
        clearTimeout(saveTimer);
        saveTimer = setTimeout(function () {
            $.ajax({
                url: '/proxy/service-pricing/items/' + itemId + '/price',
                method: 'PUT',
                contentType: 'application/json',
                data: JSON.stringify({ monthlyPrice: parseFloat(price) || 0 })
            }).done(function () {
                toastr.success('Fiyat güncellendi', '', { timeOut: 1200 });
            }).fail(function () {
                toastr.error('Fiyat kaydedilemedi');
            });
        }, 400);
    };

    self.activateSelected = function () {
        var p = self.selectedPeriod();
        if (!p) return;
        confirmModal('Dönem Aktivasyonu', '"' + p.name + '" dönemini aktif etmek istiyor musunuz?\n\nMevcut aktif dönem "Geçmiş" olarak işaretlenecek.', function () {
            $.ajax({
                url: '/proxy/service-pricing/periods/' + p.id + '/activate',
                method: 'PUT'
            }).done(function () {
                toastr.success('Dönem aktif edildi');
                self.loadPeriods();
                self.selectPeriod(p);
            }).fail(function (xhr) {
                toastr.error(xhr.responseJSON?.message || 'Aktif edilemedi');
            });
        });
    };

    self.deleteSelected = function () {
        var p = self.selectedPeriod();
        if (!p) return;
        confirmModal('Dönem Silme', '"' + p.name + '" dönemini silmek istiyor musunuz?', function () {
            $.ajax({
                url: '/proxy/service-pricing/periods/' + p.id,
                method: 'DELETE'
            }).done(function () {
                toastr.success('Dönem silindi');
                self.selectedPeriod(null);
                self.selectedPeriodId(null);
                self.loadPeriods();
            }).fail(function (xhr) {
                toastr.error(xhr.responseJSON?.message || 'Silinemedi');
            });
        }, { confirmClass: 'btn-danger', confirmText: 'Sil' });
    };

    self.loadPeriods();
}

ko.applyBindings(new PricingPeriodsViewModel(), document.getElementById('periods-vm'));
