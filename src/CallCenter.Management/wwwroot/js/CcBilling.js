function CcBillingViewModel() {
    var self = this;
    self.items = ko.observableArray([]);
    self.isLoading = ko.observable(false);
    self.yearFilter = ko.observable(new Date().getFullYear());
    self.monthFilter = ko.observable('');
    self.statusFilter = ko.observable('');
    self.selectedIds = ko.observableArray([]);
    self.selectAll = ko.observable(false);
    self.summary = ko.observable({});

    self.tahakkukDetailLoading = ko.observable(false);
    self.tahakkukDetailError = ko.observable(null);
    self.tahakkukDetail = ko.observable(null);

    self.selectAll.subscribe(function(val) {
        self.selectedIds(val ? self.items().map(function(i) { return i.periodId; }) : []);
    });

    self.computeSummary = function() {
        var all = self.items();
        var totalAmount = 0;
        var draftCount = 0, invoicedCount = 0, overdueCount = 0;
        all.forEach(function(i) {
            totalAmount += (i.amount || 0) + (i.serviceAmount || 0);
            if (i.statusId === 1) draftCount++;
            else if (i.statusId === 2) invoicedCount++;
            else if (i.statusId === 4) overdueCount++;
        });
        self.summary({
            totalCount: all.length,
            draftCount: draftCount,
            invoicedCount: invoicedCount,
            overdueCount: overdueCount,
            totalAmount: totalAmount.toFixed(2)
        });
    };

    self.loadData = function() {
        self.isLoading(true);
        self.selectedIds([]);
        var params = { productTypeId: 1 };
        if (self.yearFilter()) params.year = self.yearFilter();
        if (self.monthFilter()) params.month = self.monthFilter();
        if (self.statusFilter()) params.statusId = self.statusFilter();
        $.get('/proxy/billing/report', params, function(data) {
            var list = Array.isArray(data) ? data : (data.items || data.data || []);
            self.items(list);
            self.computeSummary();
        }).always(function() { self.isLoading(false); });
    };

    self.showTahakkukDetail = function(periodId) {
        self.tahakkukDetailLoading(true);
        self.tahakkukDetailError(null);
        self.tahakkukDetail(null);
        $.get('/proxy/billing/periods/' + periodId + '/detail')
            .done(function(d) {
                self.tahakkukDetail(d);
                new bootstrap.Modal(document.getElementById('ccTahakkukDetailModal')).show();
            })
            .fail(function(xhr) {
                var msg = 'Detay yuklenemedi.';
                try {
                    var j = xhr.responseJSON || (xhr.responseText && JSON.parse(xhr.responseText));
                    if (j && j.message) msg = j.message;
                } catch (err) { /* ignore */ }
                self.tahakkukDetailError(msg);
                new bootstrap.Modal(document.getElementById('ccTahakkukDetailModal')).show();
            })
            .always(function() { self.tahakkukDetailLoading(false); });
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

        var title = 'Tahakkuk Silme';
        var message = (item.customerName || 'Firma') + ' icin ' + (item.month || '-') + '/' + (item.year || '-') +
            ' donemi tahakkuku silinsin mi?';

        confirmModal(title, message, function() {
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

    // Tekil Fatura Kes
    self.singleInvoice = function(periodId) {
        self.updatePeriod(periodId, { statusId: 2, isPaid: false }).done(function() {
            toastr.success('Fatura kesildi.');
            self.loadData();
        }).fail(function() { toastr.error('Islem hatasi.'); });
    };

    // Toplu Fatura Kes
    self.bulkInvoice = function() {
        var ids = self.selectedIds().slice();
        if (!ids.length) return;
        // Sadece Tahakkuk (1) olanlari faturala
        var drafts = self.items().filter(function(i) { return ids.indexOf(i.periodId) >= 0 && i.statusId === 1; });
        if (!drafts.length) { toastr.warning('Secililer arasinda fatura kesilecek kayit yok.'); return; }
        var promises = drafts.map(function(d) { return self.updatePeriod(d.periodId, { statusId: 2, isPaid: false }); });
        $.when.apply($, promises).done(function() {
            toastr.success(drafts.length + ' fatura kesildi.');
            self.loadData();
        }).fail(function() { toastr.error('Islem hatasi.'); });
    };

    // Odeme Modali
    self._paymentMode = null;
    self._paymentPeriodId = null;

    self.showPaymentModal = function(mode, periodId) {
        self._paymentMode = mode;
        self._paymentPeriodId = periodId || null;
        $('#paymentMethodSelect').val('1');
        $('#paymentNotes').val('');
        new bootstrap.Modal(document.getElementById('paymentModal')).show();
    };

    $('#confirmPaymentBtn').on('click', function() {
        var methodId = parseInt($('#paymentMethodSelect').val());
        var notes = $('#paymentNotes').val() || null;
        var payload = { statusId: 3, isPaid: true, paymentMethodId: methodId, notes: notes };

        if (self._paymentMode === 'single') {
            self.updatePeriod(self._paymentPeriodId, payload).done(function() {
                toastr.success('Odeme kaydedildi.');
                bootstrap.Modal.getInstance(document.getElementById('paymentModal')).hide();
                self.loadData();
            }).fail(function() { toastr.error('Islem hatasi.'); });
        } else {
            // Toplu odeme: faturalanmis + geciken olanlari al
            var ids = self.selectedIds().slice();
            var targets = self.items().filter(function(i) {
                return ids.indexOf(i.periodId) >= 0 && (i.statusId === 2 || i.statusId === 4);
            });
            if (!targets.length) { toastr.warning('Secililer arasinda odeme alinacak kayit yok.'); return; }
            var promises = targets.map(function(t) { return self.updatePeriod(t.periodId, payload); });
            $.when.apply($, promises).done(function() {
                toastr.success(targets.length + ' odeme kaydedildi.');
                bootstrap.Modal.getInstance(document.getElementById('paymentModal')).hide();
                self.loadData();
            }).fail(function() { toastr.error('Islem hatasi.'); });
        }
    });

    self.loadData();
}
ko.applyBindings(new CcBillingViewModel(), document.getElementById('cc-billing-vm'));
