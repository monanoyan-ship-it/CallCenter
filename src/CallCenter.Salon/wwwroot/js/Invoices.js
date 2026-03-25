function InvoicesViewModel() {
    var self = this;
    self.invoices = ko.observableArray([]);
    self.clientList = ko.observableArray([]);
    self.staffList = ko.observableArray([]);
    self.serviceList = ko.observableArray([]);
    self.productList = ko.observableArray([]);
    self.searchQuery = ko.observable('');
    self.filterStatus = ko.observable('');
    self.filterStartDate = ko.observable('');
    self.filterEndDate = ko.observable('');
    self.isEditing = ko.observable(false);
    self.editingId = ko.observable(null);
    self.isSaving = ko.observable(false);

    self.form = {
        clientId: ko.observable(null),
        staffId: ko.observable(null),
        discount: ko.observable(0),
        paymentMethod: ko.observable('Cash'),
        notes: ko.observable(''),
        items: ko.observableArray([])
    };

    self.summary = ko.observable({});

    self.filteredInvoices = ko.computed(function () {
        var q = (self.searchQuery() || '').toLowerCase();
        var status = self.filterStatus();
        var start = self.filterStartDate();
        var end = self.filterEndDate();

        return self.invoices().filter(function (inv) {
            var matchQ = !q || (inv.invoiceNo || '').toLowerCase().indexOf(q) >= 0
                || (inv.clientName || '').toLowerCase().indexOf(q) >= 0;
            var matchStatus = !status || inv.status === status;
            var matchStart = !start || inv.invoiceDate >= start;
            var matchEnd = !end || inv.invoiceDate <= end + 'T23:59:59';
            return matchQ && matchStatus && matchStart && matchEnd;
        });
    });

    self.grandTotal = ko.computed(function () {
        var total = 0;
        self.form.items().forEach(function (item) {
            total += (item.quantity() * item.unitPrice()) || 0;
        });
        return total - (parseFloat(self.form.discount()) || 0);
    });

    var formModal;

    self.loadData = function () {
        var url = '/proxy/sln-invoices';
        var params = [];
        if (self.filterStartDate()) params.push('startDate=' + self.filterStartDate());
        if (self.filterEndDate()) params.push('endDate=' + self.filterEndDate());
        if (params.length) url += '?' + params.join('&');

        $.ajax({ url: url, method: 'GET' }).done(function (data) {
            var items = data.items || data;
            items.forEach(function (inv) {
                inv.statusText = { Open: 'Acik', Closed: 'Kapali', Cancelled: 'Iptal' }[inv.status] || inv.status;
                inv.statusCss = { Open: 'bg-warning text-dark', Closed: 'bg-success', Cancelled: 'bg-danger' }[inv.status] || 'bg-secondary';
                inv.paymentMethodText = { Cash: 'Nakit', CreditCard: 'Kredi Karti', BankTransfer: 'Havale/EFT', Mixed: 'Karma' }[inv.paymentMethod] || inv.paymentMethod || '-';
            });
            self.invoices(items);
            self.calculateSummary(items);
        }).fail(function () {
            toastr.error('Adisyonlar yuklenemedi');
        });
    };

    self.calculateSummary = function (items) {
        var total = 0, openCount = 0;
        items.forEach(function (inv) {
            total += inv.totalAmount || 0;
            if (inv.status === 'Open') openCount++;
        });
        self.summary({
            totalCount: items.length,
            totalAmount: total,
            openCount: openCount,
            averageAmount: items.length > 0 ? Math.round(total / items.length) : 0
        });
    };

    self.loadLookups = function () {
        $.ajax({ url: '/proxy/sln-clients?pageSize=1000', method: 'GET' }).done(function (data) {
            var items = data.items || data;
            items.forEach(function (c) { c.fullName = (c.firstName || '') + ' ' + (c.lastName || ''); });
            self.clientList(items);
        });
        $.ajax({ url: '/proxy/sln-staff', method: 'GET' }).done(function (data) {
            var items = data.items || data;
            items.forEach(function (s) { s.fullName = (s.firstName || '') + ' ' + (s.lastName || ''); });
            self.staffList(items);
        });
        $.ajax({ url: '/proxy/sln-services', method: 'GET' }).done(function (data) {
            self.serviceList(data.items || data);
        });
        $.ajax({ url: '/proxy/sln-products', method: 'GET' }).done(function (data) {
            self.productList(data.items || data);
        });
    };

    self.addItem = function () {
        self.form.items.push({
            type: ko.observable('Service'),
            itemId: ko.observable(null),
            quantity: ko.observable(1),
            unitPrice: ko.observable(0)
        });
    };

    self.removeItem = function (item) {
        self.form.items.remove(item);
    };

    self.openNew = function () {
        self.isEditing(false);
        self.editingId(null);
        self.form.clientId(null);
        self.form.staffId(null);
        self.form.discount(0);
        self.form.paymentMethod('Cash');
        self.form.notes('');
        self.form.items([]);
        self.addItem();
        formModal.show();
    };

    self.openEdit = function (invoice) {
        self.isEditing(true);
        self.editingId(invoice.id);
        self.form.clientId(invoice.clientId);
        self.form.staffId(invoice.staffId);
        self.form.discount(invoice.discount || 0);
        self.form.paymentMethod(invoice.paymentMethod || 'Cash');
        self.form.notes(invoice.notes || '');

        var items = [];
        (invoice.items || []).forEach(function (it) {
            items.push({
                type: ko.observable(it.type || 'Service'),
                itemId: ko.observable(it.itemId),
                quantity: ko.observable(it.quantity || 1),
                unitPrice: ko.observable(it.unitPrice || 0)
            });
        });
        self.form.items(items.length > 0 ? items : []);
        if (items.length === 0) self.addItem();
        formModal.show();
    };

    self.save = function () {
        var items = [];
        self.form.items().forEach(function (it) {
            if (it.itemId()) {
                items.push({
                    type: it.type(),
                    itemId: it.itemId(),
                    quantity: parseInt(it.quantity()) || 1,
                    unitPrice: parseFloat(it.unitPrice()) || 0
                });
            }
        });

        var data = {
            clientId: self.form.clientId(),
            staffId: self.form.staffId(),
            discount: parseFloat(self.form.discount()) || 0,
            paymentMethod: self.form.paymentMethod(),
            notes: self.form.notes(),
            items: items
        };

        if (!data.clientId) { toastr.warning('Musteri zorunludur'); return; }
        if (items.length === 0) { toastr.warning('En az bir kalem ekleyiniz'); return; }

        self.isSaving(true);
        var url = '/proxy/sln-invoices';
        var method = 'POST';
        if (self.isEditing()) {
            url += '/' + self.editingId();
            method = 'PUT';
        }

        $.ajax({
            url: url, method: method,
            contentType: 'application/json',
            data: JSON.stringify(data)
        }).done(function () {
            formModal.hide();
            self.loadData();
            toastr.success(self.isEditing() ? 'Adisyon guncellendi' : 'Adisyon olusturuldu');
            self.isSaving(false);
        }).fail(function (xhr) {
            toastr.error(xhr.responseJSON?.error || 'Bir hata olustu');
            self.isSaving(false);
        });
    };

    self.cancelInvoice = function (invoice) {
        if (!confirm('Bu adisyonu iptal etmek istediginize emin misiniz?')) return;
        $.ajax({
            url: '/proxy/sln-invoices/' + invoice.id,
            method: 'PUT',
            contentType: 'application/json',
            data: JSON.stringify({ status: 'Cancelled' })
        }).done(function () {
            self.loadData();
            toastr.success('Adisyon iptal edildi');
        });
    };

    // Tarih filtreleri degistiginde reload
    self.filterStartDate.subscribe(function () { self.loadData(); });
    self.filterEndDate.subscribe(function () { self.loadData(); });

    $(document).ready(function () {
        formModal = new bootstrap.Modal(document.getElementById('invoiceModal'));
        self.loadLookups();
        self.loadData();
    });
}

ko.applyBindings(new InvoicesViewModel(), document.getElementById('invoices-vm'));
