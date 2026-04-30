function PricingPeriodsViewModel() {
    var self = this;
    self.periods = ko.observableArray([]);
    self.selectedPeriodId = ko.observable(null);
    self.selectedPeriod = ko.observable(null);
    self.isSaving = ko.observable(false);
    self.isSavingTiers = ko.observable(false);
    self.branchTierRows = ko.observableArray([]);

    function tierRow(t) {
        t = t || {};
        return {
            minBranches: ko.observable(t.minBranches != null ? t.minBranches : 2),
            maxBranches: ko.observable(t.maxBranches != null ? t.maxBranches : 10),
            discountPercent: ko.observable(t.discountPercent != null ? t.discountPercent : 10),
            sortOrder: ko.observable(t.sortOrder != null ? t.sortOrder : 1)
        };
    }

    self.syncBranchTiersFromDetail = function (data) {
        var tiers = (data && data.branchDiscountTiers) ? data.branchDiscountTiers : [];
        if (!tiers.length) {
            self.branchTierRows([
                tierRow({ minBranches: 2, maxBranches: 10, discountPercent: 10, sortOrder: 1 }),
                tierRow({ minBranches: 11, maxBranches: 20, discountPercent: 15, sortOrder: 2 }),
                tierRow({ minBranches: 21, maxBranches: 999, discountPercent: 20, sortOrder: 3 })
            ]);
            return;
        }
        self.branchTierRows(tiers.map(function (x) {
            return tierRow({
                minBranches: x.minBranches,
                maxBranches: x.maxBranches,
                discountPercent: x.discountPercent,
                sortOrder: x.sortOrder
            });
        }));
    };

    self.addBranchTierRow = function () {
        var next = self.branchTierRows().length + 1;
        self.branchTierRows.push(tierRow({ minBranches: 2, maxBranches: 999, discountPercent: 0, sortOrder: next }));
    };

    self.removeBranchTierRow = function (row) {
        self.branchTierRows.remove(row);
    };

    self.saveBranchTiers = function () {
        var p = self.selectedPeriod();
        if (!p) return;
        var tiers = self.branchTierRows().map(function (r, i) {
            return {
                minBranches: parseInt(r.minBranches(), 10) || 0,
                maxBranches: parseInt(r.maxBranches(), 10) || 0,
                discountPercent: parseFloat(r.discountPercent()) || 0,
                sortOrder: parseInt(r.sortOrder(), 10) || (i + 1)
            };
        });
        self.isSavingTiers(true);
        $.ajax({
            url: '/proxy/service-pricing/periods/' + p.id + '/branch-discount-tiers',
            method: 'PUT',
            contentType: 'application/json',
            data: JSON.stringify({ tiers: tiers })
        }).done(function () {
            toastr.success('Şube eşikleri kaydedildi');
            self.selectPeriod(p);
        }).fail(function (xhr) {
            toastr.error(xhr.responseJSON?.message || 'Kaydedilemedi');
        }).always(function () { self.isSavingTiers(false); });
    };

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
            self.syncBranchTiersFromDetail(data);
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
                self.branchTierRows([]);
                self.loadPeriods();
            }).fail(function (xhr) {
                toastr.error(xhr.responseJSON?.message || 'Silinemedi');
            });
        }, { confirmClass: 'btn-danger', confirmText: 'Sil' });
    };

    self.loadPeriods();
}

ko.applyBindings(new PricingPeriodsViewModel(), document.getElementById('periods-vm'));
