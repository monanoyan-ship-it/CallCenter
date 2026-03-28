function SalesViewModel() {
    var self = this;
    self.categories = ko.observableArray([]);
    self.allServices = ko.observableArray([]);
    self.clientList = ko.observableArray([]);
    self.staffList = ko.observableArray([]);
    self.selectedCategoryId = ko.observable(null);
    self.cartItems = ko.observableArray([]);
    self.clientId = ko.observable(null);
    self.selectedPersonnelId = ko.observable(null);
    self.paymentMethodId = ko.observable('1');
    self.discountAmount = ko.observable(0);
    self.isSaving = ko.observable(false);

    // ═══ Autocomplete ═══
    self.clientAutocomplete = createAutocomplete(self.clientList, 'fullName', self.clientId);

    self.filteredServices = ko.computed(function () {
        var catId = self.selectedCategoryId();
        if (!catId) return self.allServices();
        return self.allServices().filter(function (s) { return s.categoryId === catId && s.isActive; });
    });

    self.subtotal = ko.computed(function () {
        var total = 0;
        self.cartItems().forEach(function (item) { total += item.quantity() * item.unitPrice; });
        return total;
    });

    self.grandTotal = ko.computed(function () {
        return Math.max(0, self.subtotal() - (parseFloat(self.discountAmount()) || 0));
    });

    // ═══ Data Loading ═══
    self.loadData = function () {
        $.ajax({ url: '/proxy/sln-services/categories', method: 'GET' }).done(function (data) {
            self.categories(data);
            // Tum hizmetleri flat listeye cevir
            var services = [];
            data.forEach(function (cat) {
                (cat.services || []).forEach(function (svc) {
                    svc.categoryId = cat.id;
                    svc.categoryColor = cat.color;
                    services.push(svc);
                });
            });
            self.allServices(services);
            // Ilk kategoriyi sec
            if (data.length > 0) self.selectedCategoryId(data[0].id);
        });
        $.ajax({ url: '/proxy/sln-clients?pageSize=1000', method: 'GET' }).done(function (data) {
            self.clientList(data.items || data);
        });
        $.ajax({ url: '/proxy/portal/personnel', method: 'GET' }).done(function (data) {
            self.staffList(data.items || data);
        });
    };

    // ═══ Category Selection ═══
    self.selectCategory = function (cat) {
        self.selectedCategoryId(cat.id);
    };

    // ═══ Cart Operations ═══
    self.addToCart = function (service) {
        // Ayni hizmet varsa adet arttir
        var existing = self.cartItems().find(function (item) { return item.serviceId === service.id; });
        if (existing) {
            existing.quantity(existing.quantity() + 1);
            return;
        }
        self.cartItems.push({
            serviceId: service.id,
            name: service.name,
            unitPrice: service.price,
            quantity: ko.observable(1)
        });
    };

    self.increaseQty = function (item) {
        item.quantity(item.quantity() + 1);
    };

    self.decreaseQty = function (item) {
        if (item.quantity() > 1) {
            item.quantity(item.quantity() - 1);
        } else {
            self.cartItems.remove(item);
        }
    };

    self.removeFromCart = function (item) {
        self.cartItems.remove(item);
    };

    // ═══ Checkout ═══
    self.checkout = function () {
        if (self.cartItems().length === 0) {
            toastr.warning('Sepet bos');
            return;
        }

        var items = self.cartItems().map(function (item) {
            return {
                serviceId: item.serviceId,
                productId: null,
                personnelId: self.selectedPersonnelId() ? parseInt(self.selectedPersonnelId()) : null,
                quantity: item.quantity(),
                unitPrice: item.unitPrice,
                discountAmount: 0
            };
        });

        var data = {
            slnClientId: self.clientId() ? parseInt(self.clientId()) : null,
            paymentMethodId: parseInt(self.paymentMethodId()) || 1,
            discountAmount: parseFloat(self.discountAmount()) || 0,
            notes: null,
            items: items
        };

        self.isSaving(true);
        $.ajax({
            url: '/proxy/sln-finance/invoices',
            method: 'POST',
            contentType: 'application/json',
            data: JSON.stringify(data)
        }).done(function () {
            toastr.success('Odeme alindi');
            self.cartItems([]);
            self.clientId(null);
            self.clientAutocomplete.clear();
            self.discountAmount(0);
            self.isSaving(false);
        }).fail(function (xhr) {
            toastr.error(xhr.responseJSON?.error || 'Odeme alinamadi');
            self.isSaving(false);
        });
    };

    // ═══ Init ═══
    $(document).ready(function () {
        self.loadData();
    });
}

ko.applyBindings(new SalesViewModel(), document.getElementById('sales-vm'));
