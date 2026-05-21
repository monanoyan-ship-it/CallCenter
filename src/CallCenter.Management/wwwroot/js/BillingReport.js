function BillingReportViewModel() {
    var self = this;
    self.items = ko.observableArray([]);
    self.isLoading = ko.observable(false);
    self.yearFilter = ko.observable(new Date().getFullYear());
    self.monthFilter = ko.observable('');
    self.statusFilter = ko.observable('');
    self.currentPage = ko.observable(1);
    self.totalCount = ko.observable(0);
    self.pageSize = 25;
    self.selectedIds = ko.observableArray([]);
    self.selectAll = ko.observable(false);

    self.pageNumbers = ko.computed(function() {
        var total = Math.ceil(self.totalCount() / self.pageSize);
        var pages = [];
        for (var i = 1; i <= total; i++) pages.push(i);
        return pages;
    });

    self.goToPage = function(page) { self.currentPage(page); self.loadData(); };

    self.selectAll.subscribe(function(val) {
        if (val) {
            self.selectedIds(self.items().map(function(i) { return i.periodId; }));
        } else {
            self.selectedIds([]);
        }
    });

    self.loadData = function() {
        self.isLoading(true);
        self.selectedIds([]);
        var params = { page: self.currentPage(), pageSize: self.pageSize };
        if (self.yearFilter()) params.year = self.yearFilter();
        if (self.monthFilter()) params.month = self.monthFilter();
        if (self.statusFilter()) params.statusId = self.statusFilter();
        $.get('/proxy/billing/report', params, function(data) {
            var items = Array.isArray(data) ? data : (data.items || data.data || []);
            self.items(items);
            self.totalCount(data.totalCount || data.total || items.length);
        }).always(function() { self.isLoading(false); });
    };

    self.bulkUpdate = function(statusId, isPaid) {
        var ids = self.selectedIds();
        if (ids.length === 0) return;
        var promises = ids.map(function(periodId) {
            return $.ajax({
                url: '/proxy/customers/billing/' + periodId, method: 'PUT',
                contentType: 'application/json',
                data: JSON.stringify({ statusId: statusId, isPaid: isPaid })
            });
        });
        $.when.apply($, promises).done(function() {
            toastr.success(ids.length + ' kayit guncellendi.');
            self.loadData();
        }).fail(function() { toastr.error('Guncelleme hatasi.'); });
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

        var message = (item.customerName || 'Firma') + ' icin ' + (item.month || '-') + '/' + (item.year || '-') +
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

    self.bulkInvoice = function() { self.bulkUpdate(2, false); };
    self.bulkPaid = function() { self.bulkUpdate(3, true); };

    self.loadData();
}

ko.applyBindings(new BillingReportViewModel(), document.getElementById('billingreport-vm'));
