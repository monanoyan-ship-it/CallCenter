function CustomersViewModel() {
    var self = this;

    // Liste
    self.customers = ko.observableArray([]);
    self.isLoading = ko.observable(false);
    self.searchText = ko.observable('');
    self.currentPage = ko.observable(1);
    self.totalCount = ko.observable(0);
    self.totalPages = ko.observable(1);
    self.isSaving = ko.observable(false);

    // Form
    self.form = {
        id: ko.observable(null),
        name: ko.observable(''),
        phone: ko.observable(''),
        email: ko.observable(''),
        taxNumber: ko.observable(''),
        address: ko.observable(''),
        maxUsers: ko.observable(5),
        products: ko.observableArray([]),
        adminUsername: ko.observable(''),
        adminPassword: ko.observable('')
    };
    self.formTitle = ko.observable('Yeni Müşteri');
    self.productTypes = ko.observableArray([]);

    // Ürün checkbox/fiyat yönetimi
    self.isProductActive = function (productTypeId) {
        var prod = self.form.products().find(function(p) { return p.productTypeId === productTypeId; });
        return prod ? prod.active : ko.observable(false);
    };
    self.getProductPrice = function (productTypeId) {
        var prod = self.form.products().find(function(p) { return p.productTypeId === productTypeId; });
        return prod ? prod.monthlyPrice : ko.observable(0);
    };

    // Silme
    self.deleteTarget = ko.observable(null);
    self.deleteMessage = ko.computed(function () {
        var t = self.deleteTarget();
        return t ? "'" + t.name + "' müşterisini silmek istediğinize emin misiniz?" : '';
    });

    // Faturalama
    self.billingYear = ko.observable(new Date().getFullYear());
    self.billingMonth = ko.observable(new Date().getMonth() + 1);
    self.isGeneratingBilling = ko.observable(false);

    // Sayfalama
    self.pageNumbers = ko.computed(function () {
        var pages = [];
        for (var i = 1; i <= self.totalPages(); i++) pages.push(i);
        return pages;
    });

    self.initFormProducts = function (apiProducts) {
        var types = self.productTypes();
        apiProducts = apiProducts || [];
        self.form.products(types.map(function(t) {
            var existing = apiProducts.find(function(p) { return p.productTypeId === t.id; });
            return {
                productTypeId: t.id,
                active: ko.observable(!!existing),
                monthlyPrice: ko.observable(existing ? existing.monthlyPrice : 0)
            };
        }));
    };

    self.loadData = function () {
        self.isLoading(true);
        var url = '/proxy/customers?page=' + self.currentPage() + '&pageSize=20';
        if (self.searchText()) url += '&search=' + encodeURIComponent(self.searchText());

        $.get(url, function (data) {
            self.customers(data.items || []);
            self.totalCount(data.totalCount || 0);
            self.totalPages(data.totalPages || 1);
        }).fail(function () {
            toastr.error('Veri yüklenemedi.');
        }).always(function () {
            self.isLoading(false);
        });
    };

    self.onSearchKeyUp = function (data, event) {
        if (event.key === 'Enter') {
            self.currentPage(1);
            self.loadData();
        }
        return true;
    };

    self.goToPage = function (page) {
        self.currentPage(page);
        self.loadData();
    };

    self.resetForm = function () {
        self.form.id(null);
        self.form.name('');
        self.form.phone('');
        self.form.email('');
        self.form.taxNumber('');
        self.form.address('');
        self.form.maxUsers(5);
        self.initFormProducts([]);
        self.form.adminUsername('');
        self.form.adminPassword('');
    };

    self.openCreate = function () {
        self.resetForm();
        self.formTitle('Yeni Müşteri');
        new bootstrap.Modal('#customerModal').show();
    };

    self.openEdit = function (customer) {
        self.form.id(customer.id);
        self.form.name(customer.name || '');
        self.form.phone(customer.phone || '');
        self.form.email(customer.email || '');
        self.form.taxNumber(customer.taxNumber || '');
        self.form.address(customer.address || '');
        self.form.maxUsers(customer.maxUsers || 5);
        self.initFormProducts(customer.products || []);
        self.formTitle('Müşteri Düzenle');
        new bootstrap.Modal('#customerModal').show();
    };

    self.saveCustomer = function () {
        if (!self.form.name()) { toastr.warning('Firma adı zorunlu.'); return; }
        self.isSaving(true);

        var activeProducts = self.form.products().filter(function(p) { return p.active(); });
        var payload = {
            name: self.form.name(),
            phone: self.form.phone(),
            email: self.form.email(),
            taxNumber: self.form.taxNumber(),
            address: self.form.address(),
            maxUsers: parseInt(self.form.maxUsers()) || 5,
            products: activeProducts.map(function(p) {
                return { productTypeId: p.productTypeId, monthlyPrice: parseFloat(p.monthlyPrice()) || 0 };
            })
        };

        if (!self.form.id()) {
            payload.adminUsername = self.form.adminUsername();
            payload.adminPassword = self.form.adminPassword();
        }

        var method = self.form.id() ? 'PUT' : 'POST';
        var url = self.form.id() ? '/proxy/customers/' + self.form.id() : '/proxy/customers';

        $.ajax({
            url: url, method: method,
            contentType: 'application/json',
            data: JSON.stringify(payload),
            success: function () {
                toastr.success('Müşteri kaydedildi.');
                bootstrap.Modal.getInstance(document.getElementById('customerModal')).hide();
                self.loadData();
            },
            error: function (xhr) {
                var msg = xhr.responseJSON?.message || 'Kaydetme hatası.';
                toastr.error(msg);
            },
            complete: function () { self.isSaving(false); }
        });
    };

    self.confirmDelete = function (customer) {
        self.deleteTarget(customer);
        new bootstrap.Modal('#deleteModal').show();
    };

    self.executeDelete = function () {
        var t = self.deleteTarget();
        if (!t) return;
        $.ajax({
            url: '/proxy/customers/' + t.id, method: 'DELETE',
            success: function () {
                toastr.success('Müşteri silindi.');
                bootstrap.Modal.getInstance(document.getElementById('deleteModal')).hide();
                self.loadData();
            },
            error: function () { toastr.error('Silme hatası.'); }
        });
    };

    self.generateBilling = function () {
        self.isGeneratingBilling(true);
        $.ajax({
            url: '/proxy/customers/billing/generate',
            method: 'POST',
            contentType: 'application/json',
            data: JSON.stringify({ year: parseInt(self.billingYear()), month: parseInt(self.billingMonth()) }),
            success: function (data) {
                var pel = data.platformEligibleWithoutSubscription != null ? data.platformEligibleWithoutSubscription : null;
                var msg = 'Salon platform (abonelik kaydı olmayan): ' + (data.platformTahakkukCreated || 0) + ' oluşturuldu, ' + (data.platformTahakkukSkipped || 0) + ' atlandı';
                if (pel !== null) msg += ' (aday müşteri: ' + pel + ')';
                msg += '. CC: ' + (data.created || 0) + ' dönem, ' + (data.skipped || 0) + ' atlandı (zaten vardı).';
                if (pel === 0) {
                    toastr.info(msg + ' Aboneliksiz salon müşterisi yok; platform tahakkuku Hizmet Yönetimi -> Abonelikler (aktif abonelikler) ile ayrı kesilir.');
                } else {
                    toastr.success(msg);
                }
                bootstrap.Modal.getInstance(document.getElementById('billingModal')).hide();
            },
            error: function () { toastr.error('Faturalama hatası.'); },
            complete: function () { self.isGeneratingBilling(false); }
        });
    };

    // Lookup: Ürün Tipleri
    $.get('/proxy/management/product-types', function (d) {
        self.productTypes(Array.isArray(d) ? d : []);
        self.initFormProducts([]);
    });

    self.loadData();
}

ko.applyBindings(new CustomersViewModel(), document.getElementById('customers-vm'));
