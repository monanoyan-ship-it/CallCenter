function SalonBillingViewModel() {
    var self = this;
    self.items = ko.observableArray([]);
    self.isLoading = ko.observable(false);
    self.yearFilter = ko.observable(new Date().getFullYear());
    self.monthFilter = ko.observable('');
    self.statusFilter = ko.observable('');
    self.selectedIds = ko.observableArray([]);
    self.selectAll = ko.observable(false);
    self.summary = ko.observable({});
    self._invoicedPeriods = ko.observableArray([]); // Faturalanmis olanlari takip et
    self.tahakkukDetailLoading = ko.observable(false);
    self.tahakkukDetailError = ko.observable(null);
    self.tahakkukDetail = ko.observable(null);

    self.selectAll.subscribe(function(val) {
        self.selectedIds(val ? self.items().map(function(i) { return i.periodId; }) : []);
    });

    self.isInvoiced = function(periodId) {
        return self._invoicedPeriods.indexOf(periodId) >= 0;
    };

    self.computeSummary = function() {
        var all = self.items();
        var totalAmount = 0, unpaidCount = 0, paidCount = 0, overdueCount = 0;
        all.forEach(function(i) {
            totalAmount += (i.amount || 0) + (i.serviceAmount || 0);
            if (i.statusId === 3) paidCount++;
            else if (i.statusId === 4) overdueCount++;
            else unpaidCount++;
        });
        self.summary({
            totalCount: all.length,
            unpaidCount: unpaidCount,
            paidCount: paidCount,
            overdueCount: overdueCount,
            totalAmount: totalAmount.toFixed(2)
        });
    };

    self.loadData = function() {
        self.isLoading(true);
        self.selectedIds([]);
        self._invoicedPeriods([]);
        var params = { productTypeId: 2 };
        if (self.yearFilter()) params.year = self.yearFilter();
        if (self.monthFilter()) params.month = self.monthFilter();
        if (self.statusFilter()) params.statusId = self.statusFilter();
        $.get('/proxy/billing/report', params, function(data) {
            var list = Array.isArray(data) ? data : (data.items || data.data || []);
            // Faturalanmis olanlari kaydet (statusId 2 veya 3 olup isPaid true olanlar onceden fatura kesilmis)
            var invoiced = list.filter(function(i) { return i.statusId === 2; }).map(function(i) { return i.periodId; });
            self._invoicedPeriods(invoiced);
            self.items(list);
            self.computeSummary();
        }).always(function() { self.isLoading(false); });
    };

    self.updatePeriod = function(periodId, payload) {
        return $.ajax({
            url: '/proxy/customers/billing/' + periodId,
            method: 'PUT',
            contentType: 'application/json',
            data: JSON.stringify(payload)
        });
    };

    self.canDeletePeriod = function(item) {
        return item
            && item.statusId === 1
            && !item.isPaid
            && !item.paidAt
            && !item.paymentMethodId
            && !item.paymentMethodName;
    };

    self.deletePeriod = function(item) {
        if (!self.canDeletePeriod(item)) {
            toastr.warning('Sadece islem gormemis tahakkuk silinebilir.');
            return;
        }

        var message = (item.customerName || 'Salon') + ' icin ' + (item.month || '-') + '/' + (item.year || '-') +
            ' donemi tahakkuku silinsin mi?';
        confirmModal('Tahakkuk Silme', message, function() {
            $.ajax({
                url: '/proxy/customers/billing/' + item.periodId,
                method: 'DELETE'
            }).done(function() {
                toastr.success('Tahakkuk silindi.');
                self.loadData();
            }).fail(function(xhr) {
                toastr.error(xhr.responseJSON?.message || 'Tahakkuk silinemedi.');
            });
        }, { confirmClass: 'btn-danger', confirmText: 'Sil' });
    };

    self.showTahakkukDetail = function(periodId) {
        self.tahakkukDetailLoading(true);
        self.tahakkukDetailError(null);
        self.tahakkukDetail(null);
        $.get('/proxy/billing/periods/' + periodId + '/detail')
            .done(function(data) {
                self.tahakkukDetail(data);
                new bootstrap.Modal(document.getElementById('salonTahakkukDetailModal')).show();
            })
            .fail(function(xhr) {
                var msg = 'Detay yuklenemedi.';
                try {
                    var json = xhr.responseJSON || (xhr.responseText && JSON.parse(xhr.responseText));
                    if (json && json.message) msg = json.message;
                } catch (err) { /* ignore */ }
                self.tahakkukDetailError(msg);
                new bootstrap.Modal(document.getElementById('salonTahakkukDetailModal')).show();
            })
            .always(function() { self.tahakkukDetailLoading(false); });
    };

    // Fatura Kes (odenmis kayitlara)
    self.markInvoiced = function(periodId) {
        // Odenmis durumu koruyarak fatura kesildi notu ekle
        self.updatePeriod(periodId, { statusId: 3, isPaid: true, notes: 'Fatura kesildi' }).done(function() {
            toastr.success('Fatura kesildi.');
            self._invoicedPeriods.push(periodId);
        }).fail(function() { toastr.error('Islem hatasi.'); });
    };

    // Toplu Fatura Kes (odenmis olanlara)
    self.bulkInvoice = function() {
        var ids = self.selectedIds().slice();
        var targets = self.items().filter(function(i) {
            return ids.indexOf(i.periodId) >= 0 && i.statusId === 3 && self._invoicedPeriods.indexOf(i.periodId) < 0;
        });
        if (!targets.length) { toastr.warning('Secililer arasinda fatura kesilecek kayit yok.'); return; }
        var promises = targets.map(function(t) {
            return self.updatePeriod(t.periodId, { statusId: 3, isPaid: true, notes: 'Fatura kesildi' });
        });
        $.when.apply($, promises).done(function() {
            toastr.success(targets.length + ' fatura kesildi.');
            targets.forEach(function(t) { self._invoicedPeriods.push(t.periodId); });
        }).fail(function() { toastr.error('Islem hatasi.'); });
    };

    // Odeme Modali
    self._paymentMode = null;
    self._paymentPeriodId = null;

    self.showPaymentModal = function(mode, periodId) {
        self._paymentMode = mode;
        self._paymentPeriodId = periodId || null;
        $('#salonPaymentMethodSelect').val('3'); // KK default
        $('#salonPaymentNotes').val('');
        new bootstrap.Modal(document.getElementById('salonPaymentModal')).show();
    };

    $('#salonConfirmPaymentBtn').on('click', function() {
        var methodId = parseInt($('#salonPaymentMethodSelect').val());
        var notes = $('#salonPaymentNotes').val() || null;
        var payload = { statusId: 3, isPaid: true, paymentMethodId: methodId, notes: notes };

        if (self._paymentMode === 'single') {
            self.updatePeriod(self._paymentPeriodId, payload).done(function() {
                toastr.success('Odeme kaydedildi.');
                bootstrap.Modal.getInstance(document.getElementById('salonPaymentModal')).hide();
                self.loadData();
            }).fail(function() { toastr.error('Islem hatasi.'); });
        } else {
            // Toplu odeme: tahakkuk, faturalanmis, geciken olanlari al
            var ids = self.selectedIds().slice();
            var targets = self.items().filter(function(i) {
                return ids.indexOf(i.periodId) >= 0 && (i.statusId === 1 || i.statusId === 2 || i.statusId === 4);
            });
            if (!targets.length) { toastr.warning('Secililer arasinda odeme alinacak kayit yok.'); return; }
            var promises = targets.map(function(t) { return self.updatePeriod(t.periodId, payload); });
            $.when.apply($, promises).done(function() {
                toastr.success(targets.length + ' odeme kaydedildi.');
                bootstrap.Modal.getInstance(document.getElementById('salonPaymentModal')).hide();
                self.loadData();
            }).fail(function() { toastr.error('Islem hatasi.'); });
        }
    });

    self.loadData();
}
ko.applyBindings(new SalonBillingViewModel(), document.getElementById('salon-billing-vm'));
