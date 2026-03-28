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
    self.isSaving = ko.observable(false);

    self.isEditing = ko.observable(false);

    self.form = {
        slnClientId: ko.observable(null),
        discountAmount: ko.observable(0),
        paymentMethodId: ko.observable(1),
        posDeviceId: ko.observable(null),
        tipAmount: ko.observable(0),
        notes: ko.observable(''),
        items: ko.observableArray([])
    };

    // ═══ Autocomplete'ler ═══
    self.clientAutocomplete = createAutocomplete(self.clientList, 'fullName', self.form.slnClientId);

    self.summary = ko.observable({});

    var statusNames = { 1: 'Acik', 2: 'Odendi', 3: 'Iptal' };
    var statusCss = { 1: 'bg-warning text-dark', 2: 'bg-success', 3: 'bg-danger' };
    var paymentNames = { 1: 'Nakit', 2: 'Kredi Karti', 3: 'Karma' };

    self.filteredInvoices = ko.computed(function () {
        var q = (self.searchQuery() || '').toLowerCase();
        var status = self.filterStatus();
        var start = self.filterStartDate();
        var end = self.filterEndDate();

        return self.invoices().filter(function (inv) {
            var matchQ = !q || (inv.invoiceNo || '').toLowerCase().indexOf(q) >= 0
                || (inv.clientName || '').toLowerCase().indexOf(q) >= 0;
            var matchStatus = !status || inv.statusId == status;
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
        return total - (parseFloat(self.form.discountAmount()) || 0);
    });

    var formModal;

    self.loadData = function () {
        var url = '/proxy/sln-finance/invoices';
        var params = [];
        if (self.filterStartDate()) params.push('from=' + self.filterStartDate());
        if (self.filterEndDate()) params.push('to=' + self.filterEndDate());
        if (self.filterStatus()) params.push('statusId=' + self.filterStatus());
        if (params.length) url += '?' + params.join('&');

        $.ajax({ url: url, method: 'GET' }).done(function (data) {
            var items = data.items || data;
            items.forEach(function (inv) {
                inv.statusText = statusNames[inv.statusId] || 'Bilinmiyor';
                inv.statusCss = statusCss[inv.statusId] || 'bg-secondary';
                inv.paymentMethodText = paymentNames[inv.paymentMethodId] || '-';
                inv.itemsSummary = (inv.items || []).map(function (it) { return it.itemName; }).join(', ') || '-';
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
            total += inv.netAmount || 0;
            if (inv.statusId === 1) openCount++;
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
            self.clientList(data.items || data);
        });
        $.ajax({ url: '/proxy/portal/personnel', method: 'GET' }).done(function (data) {
            self.staffList(data.items || data);
        });
        $.ajax({ url: '/proxy/sln-services', method: 'GET' }).done(function (data) {
            self.serviceList(data.items || data);
        });
        $.ajax({ url: '/proxy/sln-products', method: 'GET' }).done(function (data) {
            self.productList(data.items || data);
        });
    };

    self.addItem = function () {
        var item = {
            type: ko.observable('Service'),
            itemId: ko.observable(null),
            personnelId: ko.observable(null),
            quantity: ko.observable(1),
            unitPrice: ko.observable(0),
            discountAmount: ko.observable(0)
        };
        // type degistiginde itemId sifirla
        item.type.subscribe(function () { item.itemId(null); });
        self.form.items.push(item);
    };

    self.removeItem = function (item) {
        self.form.items.remove(item);
    };

    self.openNew = function () {
        self.isEditing(false);
        self.form.slnClientId(null);
        self.form.discountAmount(0);
        self.form.paymentMethodId(1);
        self.form.posDeviceId(null);
        self.form.notes('');
        self.form.items([]);
        self.clientAutocomplete.clear();
        self.addItem();
        formModal.show();
    };

    self.openDetail = function (invoice) {
        self.isEditing(true);
        self.form.slnClientId(null);
        self.form.discountAmount(invoice.discountAmount || 0);
        self.form.paymentMethodId(invoice.paymentMethodId || 1);
        self.form.posDeviceId(null);
        self.form.notes(invoice.notes || '');
        self.form.items([]);
        (invoice.items || []).forEach(function (it) {
            var isService = !!it.serviceId;
            var item = {
                type: ko.observable(isService ? 'Service' : 'Product'),
                itemId: ko.observable(isService ? it.serviceId : it.productId),
                personnelId: ko.observable(it.personnelId || null),
                quantity: ko.observable(it.quantity || 1),
                unitPrice: ko.observable(it.unitPrice || 0),
                discountAmount: ko.observable(it.discountAmount || 0)
            };
            item.type.subscribe(function () { item.itemId(null); });
            self.form.items.push(item);
        });
        formModal.show();
    };

    self.save = function () {
        var items = [];
        self.form.items().forEach(function (it) {
            var id = it.itemId();
            if (!id) return;
            var isService = it.type() === 'Service';
            items.push({
                serviceId: isService ? parseInt(id) : null,
                productId: !isService ? parseInt(id) : null,
                personnelId: it.personnelId() ? parseInt(it.personnelId()) : null,
                quantity: parseInt(it.quantity()) || 1,
                unitPrice: parseFloat(it.unitPrice()) || 0,
                discountAmount: parseFloat(it.discountAmount()) || 0
            });
        });

        var data = {
            slnClientId: self.form.slnClientId() ? parseInt(self.form.slnClientId()) : null,
            paymentMethodId: parseInt(self.form.paymentMethodId()) || 1,
            posDeviceId: self.form.posDeviceId() ? parseInt(self.form.posDeviceId()) : null,
            discountAmount: parseFloat(self.form.discountAmount()) || 0,
            tipAmount: parseFloat(self.form.tipAmount()) || 0,
            notes: self.form.notes(),
            items: items
        };

        if (items.length === 0) { toastr.warning('En az bir kalem ekleyiniz'); return; }

        self.isSaving(true);
        $.ajax({
            url: '/proxy/sln-finance/invoices',
            method: 'POST',
            contentType: 'application/json',
            data: JSON.stringify(data)
        }).done(function () {
            formModal.hide();
            self.loadData();
            toastr.success('Adisyon olusturuldu');
            self.isSaving(false);
        }).fail(function (xhr) {
            toastr.error(xhr.responseJSON?.error || 'Bir hata olustu');
            self.isSaving(false);
        });
    };

    self.cancelInvoice = function (invoice) {
        if (!confirm('Bu adisyonu iptal etmek istediginize emin misiniz?')) return;
        $.ajax({
            url: '/proxy/sln-finance/invoices/' + invoice.id + '/cancel',
            method: 'PUT'
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
