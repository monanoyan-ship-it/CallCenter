function ServiceManagementViewModel() {
    var self = this;
    self.periods = ko.observableArray([]);
    self.selectedPeriod = ko.observable(null);
    self.periodForm = { name: ko.observable(''), startDate: ko.observable(''), endDate: ko.observable('') };
    self.bulkForm = { productTypeId: ko.observable(''), adjustType: ko.observable('amount'), value: ko.observable(0) };

    self.load = function () {
        $.get('/proxy/service-pricing/periods', function (d) { self.periods(d || []); });
    };

    self.openNewPeriod = function () {
        self.periodForm.name(''); self.periodForm.startDate(''); self.periodForm.endDate('');
        new bootstrap.Modal('#newPeriodModal').show();
    };

    self.savePeriod = function () {
        var d = { name: self.periodForm.name(), startDate: self.periodForm.startDate(), endDate: self.periodForm.endDate() };
        if (!d.name || !d.startDate || !d.endDate) { toastr.warning('Tum alanlar zorunludur.'); return; }
        $.ajax({ url: '/proxy/service-pricing/periods', method: 'POST', contentType: 'application/json', data: JSON.stringify(d) })
            .done(function () { bootstrap.Modal.getInstance(document.getElementById('newPeriodModal')).hide(); self.load(); toastr.success('Donem olusturuldu.'); })
            .fail(function (x) { toastr.error(x.responseJSON?.message || 'Hata'); });
    };

    self.openPeriodDetail = function (p) {
        $.get('/proxy/service-pricing/periods/' + p.id, function (d) { self.selectedPeriod(d); });
    };

    self.updatePrice = function (itemId, newPrice) {
        $.ajax({ url: '/proxy/service-pricing/items/' + itemId + '/price', method: 'PUT', contentType: 'application/json', data: JSON.stringify({ monthlyPrice: parseFloat(newPrice) || 0 }) })
            .done(function () { toastr.success('Fiyat guncellendi.'); })
            .fail(function () { toastr.error('Guncelleme hatasi.'); });
    };

    self.openBulkAdjust = function () {
        self.bulkForm.productTypeId(''); self.bulkForm.adjustType('amount'); self.bulkForm.value(0);
        new bootstrap.Modal('#bulkAdjustModal').show();
    };

    self.executeBulkAdjust = function () {
        var pid = self.selectedPeriod()?.id;
        if (!pid) return;
        var d = { productTypeId: self.bulkForm.productTypeId() ? parseInt(self.bulkForm.productTypeId()) : null, adjustType: self.bulkForm.adjustType(), value: parseFloat(self.bulkForm.value()) };
        $.ajax({ url: '/proxy/service-pricing/periods/' + pid + '/bulk-adjust', method: 'POST', contentType: 'application/json', data: JSON.stringify(d) })
            .done(function (r) { bootstrap.Modal.getInstance(document.getElementById('bulkAdjustModal')).hide(); toastr.success((r.updated || 0) + ' kalem guncellendi.'); self.openPeriodDetail({ id: pid }); })
            .fail(function () { toastr.error('Hata'); });
    };

    self.activatePeriod = function (p) {
        confirmModal('Donemi Aktif Et', p.name + ' donemini aktif etmek istiyor musunuz? Fiyatlar tum sisteme yansiyacak.', function () {
            $.ajax({ url: '/proxy/service-pricing/periods/' + p.id + '/activate', method: 'PUT' })
                .done(function () { self.load(); toastr.success('Donem aktif edildi.'); })
                .fail(function (x) { toastr.error(x.responseJSON?.message || 'Hata'); });
        }, { confirmText: 'Aktif Et', confirmClass: 'btn-success' });
    };

    self.deletePeriod = function (p) {
        confirmModal('Donemi Sil', p.name + ' donemini silmek istiyor musunuz?', function () {
            $.ajax({ url: '/proxy/service-pricing/periods/' + p.id, method: 'DELETE' })
                .done(function () { self.load(); self.selectedPeriod(null); toastr.success('Donem silindi.'); })
                .fail(function (x) { toastr.error(x.responseJSON?.message || 'Silinemedi'); });
        }, { confirmText: 'Sil', confirmClass: 'btn-danger' });
    };

    self.load();
}
ko.applyBindings(new ServiceManagementViewModel(), document.getElementById('servicemanagement-vm'));
