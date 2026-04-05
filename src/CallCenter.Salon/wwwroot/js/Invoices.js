function InvoicesViewModel() {
    var self = this;
    self.invoices = ko.observableArray([]);
    self.clientList = ko.observableArray([]);
    self.staffList = ko.observableArray([]);
    self.serviceCategories = ko.observableArray([]);
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
        tipAmount: ko.observable(0),
        notes: ko.observable(''),
        items: ko.observableArray([])
    };

    // ═══ Yeni Musteri ═══
    self.newClientVisible = ko.observable(false);
    self.isCreatingClient = ko.observable(false);
    self.newClientForm = { fullName: ko.observable(''), phone: ko.observable('') };

    self.showNewClient = function (q) {
        self.newClientForm.fullName(q || '');
        self.newClientForm.phone('');
        self.newClientVisible(true);
        self.clientAutocomplete.showDropdown(false);
    };
    self.hideNewClient = function () { self.newClientVisible(false); };
    self.saveNewClient = function () {
        var name = self.newClientForm.fullName(), phone = self.newClientForm.phone();
        if (!name || !phone) { toastr.warning('Ad ve telefon zorunludur'); return; }
        self.isCreatingClient(true);
        $.ajax({
            url: '/proxy/sln-clients', method: 'POST',
            contentType: 'application/json',
            data: JSON.stringify({ fullName: name, phone: phone })
        }).done(function (c) {
            var list = self.clientList();
            list.push(c);
            self.clientList(list);
            self.form.slnClientId(c.id);
            self.clientAutocomplete.query(c.fullName);
            self.clientAutocomplete.selectedName(c.fullName);
            self.newClientVisible(false);
            toastr.success('Musteri olusturuldu');
        }).fail(function () { toastr.error('Musteri olusturulamadi'); })
          .always(function () { self.isCreatingClient(false); });
    };

    // ═══ Autocomplete ═══
    self.clientAutocomplete = createAutocomplete(self.clientList, 'fullName', self.form.slnClientId);

    self.summary = ko.observable({});

    var statusNames = { 1: 'Acik', 2: 'Odendi', 3: 'Iptal' };
    var statusCss = { 1: 'bg-warning text-dark', 2: 'bg-success', 3: 'bg-danger' };
    var paymentNames = { 1: 'Nakit', 2: 'Kredi Karti', 3: 'Karma', 4: 'Havale' };

    // ���══ Kalem Yonetimi ═══
    function createItem(type, id, name, price) {
        var item = {
            type: type,
            itemId: id,
            itemName: name,
            unitPrice: ko.observable(price || 0),
            discountAmount: ko.observable(0),
            personnelId: ko.observable(null)
        };
        item.lineTotal = ko.computed(function () {
            var total = (parseFloat(item.unitPrice()) || 0) - (parseFloat(item.discountAmount()) || 0);
            return Math.max(0, total).toLocaleString('tr-TR') + ' ₺';
        });
        return item;
    }

    self.addServiceItem = function (svc) {
        // Toggle: zaten ekliyse kaldir
        var existing = self.form.items().find(function (i) { return i.type === 'Service' && i.itemId === svc.id; });
        if (existing) { self.form.items.remove(existing); return; }
        self.form.items.push(createItem('Service', svc.id, svc.name, svc.price));
    };

    self.addProductItem = function (prod) {
        var existing = self.form.items().find(function (i) { return i.type === 'Product' && i.itemId === prod.id; });
        if (existing) { self.form.items.remove(existing); return; }
        self.form.items.push(createItem('Product', prod.id, prod.name, prod.salePrice || 0));
    };

    self.isItemAdded = function (id, type) {
        return self.form.items().some(function (i) { return i.type === type && i.itemId === id; });
    };

    self.removeItem = function (item) { self.form.items.remove(item); };

    self.grandTotal = ko.computed(function () {
        var total = 0;
        self.form.items().forEach(function (item) {
            total += Math.max(0, (parseFloat(item.unitPrice()) || 0) - (parseFloat(item.discountAmount()) || 0));
        });
        return total - (parseFloat(self.form.discountAmount()) || 0) + (parseFloat(self.form.tipAmount()) || 0);
    });

    // ═══ Filtre ═══
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

    // ═══ Veri Yukleme ═══
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
                inv.statusText = statusNames[inv.statusId] || '?';
                inv.statusCss = statusCss[inv.statusId] || 'bg-secondary';
                inv.paymentMethodText = paymentNames[inv.paymentMethodId] || '-';
                inv.itemsSummary = (inv.items || []).map(function (it) { return it.itemName; }).join(', ') || '-';
            });
            self.invoices(items);
            self.calculateSummary(items);
        });
    };

    self.calculateSummary = function (items) {
        var total = 0, openCount = 0;
        items.forEach(function (inv) {
            total += inv.netAmount || 0;
            if (inv.statusId === 1) openCount++;
        });
        self.summary({
            totalCount: items.length, totalAmount: total, openCount: openCount,
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
            self.serviceCategories(data.items || data);
        });
        $.ajax({ url: '/proxy/sln-products', method: 'GET' }).done(function (data) {
            self.productList(data.items || data);
        });
    };

    // ═══ Form ═══
    self.openNew = function () {
        self.isEditing(false);
        self.form.slnClientId(null);
        self.form.discountAmount(0);
        self.form.paymentMethodId(1);
        self.form.tipAmount(0);
        self.form.notes('');
        self.form.items([]);
        self.clientAutocomplete.clear();
        self.newClientVisible(false);
        formModal.show();
    };

    // Randevudan adisyon olusturma (disaridan cagrilabilir)
    self.openFromAppointment = function (clientId, clientName, serviceNames, serviceIds, personnelId) {
        self.openNew();
        if (clientId) {
            self.form.slnClientId(clientId);
            self.clientAutocomplete.query(clientName || '');
            self.clientAutocomplete.selectedName(clientName || '');
        }
        // Hizmetleri ekle
        if (serviceIds && serviceIds.length) {
            var allServices = [];
            self.serviceCategories().forEach(function (cat) {
                (cat.services || []).forEach(function (s) { allServices.push(s); });
            });
            serviceIds.forEach(function (sid) {
                var svc = allServices.find(function (s) { return s.id === sid; });
                if (svc) {
                    var item = createItem('Service', svc.id, svc.name, svc.price);
                    if (personnelId) item.personnelId(personnelId);
                    self.form.items.push(item);
                }
            });
        }
    };

    self.openDetail = function (invoice) {
        self.isEditing(true);
        self.form.slnClientId(invoice.slnClientId || null);
        self.form.discountAmount(invoice.discountAmount || 0);
        self.form.paymentMethodId(invoice.paymentMethodId || 1);
        self.form.tipAmount(invoice.tipAmount || 0);
        self.form.notes(invoice.notes || '');
        self.form.items([]);
        if (invoice.slnClientId) {
            self.clientAutocomplete.setFromValue(invoice.slnClientId);
        }
        (invoice.items || []).forEach(function (it) {
            var isService = !!it.serviceId;
            var item = createItem(
                isService ? 'Service' : 'Product',
                isService ? it.serviceId : it.productId,
                it.itemName,
                it.unitPrice || 0
            );
            item.discountAmount(it.discountAmount || 0);
            item.personnelId(it.personnelId || null);
            self.form.items.push(item);
        });
        formModal.show();
    };

    self.save = function () {
        var items = [];
        self.form.items().forEach(function (it) {
            var isService = it.type === 'Service';
            items.push({
                serviceId: isService ? it.itemId : null,
                productId: !isService ? it.itemId : null,
                personnelId: it.personnelId() ? parseInt(it.personnelId()) : null,
                quantity: 1,
                unitPrice: parseFloat(it.unitPrice()) || 0,
                discountAmount: parseFloat(it.discountAmount()) || 0
            });
        });

        if (items.length === 0) { toastr.warning('En az bir kalem ekleyiniz'); return; }

        var data = {
            slnClientId: self.form.slnClientId() ? parseInt(self.form.slnClientId()) : null,
            paymentMethodId: parseInt(self.form.paymentMethodId()) || 1,
            discountAmount: parseFloat(self.form.discountAmount()) || 0,
            tipAmount: parseFloat(self.form.tipAmount()) || 0,
            notes: self.form.notes(),
            items: items
        };

        self.isSaving(true);
        $.ajax({
            url: '/proxy/sln-finance/invoices', method: 'POST',
            contentType: 'application/json', data: JSON.stringify(data)
        }).done(function () {
            formModal.hide();
            self.loadData();
            toastr.success('Adisyon olusturuldu');
        }).fail(function (xhr) {
            toastr.error(xhr.responseJSON?.error || xhr.responseJSON?.message || 'Bir hata olustu');
        }).always(function () { self.isSaving(false); });
    };

    self.cancelInvoice = function (invoice) {
        if (!confirm('Bu adisyonu iptal etmek istediginize emin misiniz?')) return;
        $.ajax({ url: '/proxy/sln-finance/invoices/' + invoice.id + '/cancel', method: 'PUT' }).done(function () {
            self.loadData();
            toastr.success('Adisyon iptal edildi');
        });
    };

    self.filterStartDate.subscribe(function () { self.loadData(); });
    self.filterEndDate.subscribe(function () { self.loadData(); });

    $(document).ready(function () {
        formModal = new bootstrap.Modal(document.getElementById('invoiceModal'));
        self.loadLookups();
        self.loadData();

        // Randevudan geliyorsa otomatik ac
        var params = new URLSearchParams(window.location.search);
        if (params.get('fromAppt') === '1') {
            var clientId = parseInt(params.get('clientId')) || null;
            var clientName = params.get('clientName') || '';
            var serviceIdsStr = params.get('serviceIds') || '';
            var personnelId = parseInt(params.get('personnelId')) || null;
            var serviceIds = serviceIdsStr ? serviceIdsStr.split(',').map(Number).filter(Boolean) : [];

            // Lookup'lar yuklendikten sonra ac
            setTimeout(function () {
                self.openFromAppointment(clientId, clientName, null, serviceIds, personnelId);
            }, 800);
            history.replaceState(null, '', window.location.pathname);
        }
    });
}

ko.applyBindings(new InvoicesViewModel(), document.getElementById('invoices-vm'));
