function SalesViewModel() {
    var self = this;
    self.categories = ko.observableArray([]);
    self.allServices = ko.observableArray([]);
    self.clientList = ko.observableArray([]);
    self.staffList = ko.observableArray([]);
    self.recipes = ko.observableArray([]);
    self.selectedCategoryId = ko.observable(null);
    self.showRecipes = ko.observable(false);
    self.cartItems = ko.observableArray([]);
    self.clientId = ko.observable(null);
    self.selectedPersonnelId = ko.observable(null);
    self.paymentMethodId = ko.observable('1');
    self.discountAmount = ko.observable(0);
    self.tipAmount = ko.observable(0);
    self.linkedAppointmentId = ko.observable(null);
    self.todayAppointments = ko.observableArray([]);
    self.appointmentsLoading = ko.observable(false);
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
        $.ajax({ url: '/proxy/sln-recipes', method: 'GET' }).done(function (data) {
            self.recipes((data.items || data).filter(function (r) { return r.isActive; }));
        });
    };

    // ═══ Recipe Toggle ═══
    self.toggleRecipes = function () {
        self.showRecipes(!self.showRecipes());
        if (self.showRecipes()) self.selectedCategoryId(null);
    };

    // ═══ Add Recipe to Cart ═══
    self.addRecipeToCart = function (recipe) {
        (recipe.items || []).forEach(function (item) {
            for (var i = 0; i < item.quantity; i++) {
                var existing = self.cartItems().find(function (c) { return c.serviceId === item.serviceId; });
                if (existing) {
                    existing.quantity(existing.quantity() + 1);
                } else {
                    self.cartItems.push({
                        serviceId: item.serviceId,
                        name: item.serviceName,
                        unitPrice: item.servicePrice,
                        quantity: ko.observable(1)
                    });
                }
            }
        });
        toastr.info(recipe.name + ' sepete eklendi');
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
            tipAmount: parseFloat(self.tipAmount()) || 0,
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
            toastr.success('Ödeme alındı');

            // Randevu bağlıysa tamamlandı olarak işaretle
            if (self.linkedAppointmentId()) {
                $.ajax({
                    url: '/proxy/sln-appointments/' + self.linkedAppointmentId() + '/status',
                    method: 'PUT',
                    contentType: 'application/json',
                    data: JSON.stringify({ statusId: 3 }) // Tamamlandı
                });
            }

            self.cartItems([]);
            self.clientId(null);
            self.clientAutocomplete.clear();
            self.discountAmount(0);
            self.tipAmount(0);
            self.linkedAppointmentId(null);
            self.isSaving(false);
        }).fail(function (xhr) {
            toastr.error(xhr.responseJSON?.error || 'Ödeme alınamadı');
            self.isSaving(false);
        });
    };

    // ═══ Randevu Çek ═══
    var appointmentModal;

    self.openAppointments = function () {
        self.appointmentsLoading(true);
        self.todayAppointments([]);
        if (!appointmentModal) appointmentModal = new bootstrap.Modal(document.getElementById('appointmentModal'));
        appointmentModal.show();

        $.get('/proxy/sln-appointments?date=today', function (data) {
            var list = (data.items || data || []).map(function (a) {
                var startTime = new Date(a.startTime);
                a.startTimeText = startTime.getHours().toString().padStart(2, '0') + ':' + startTime.getMinutes().toString().padStart(2, '0');
                a.clientName = a.clientName || '-';
                a.personnelName = a.personnelName || null;
                a.serviceNamesText = (a.serviceNames || []).join(', ') || (a.serviceName || '-');
                return a;
            });
            self.todayAppointments(list);
            self.appointmentsLoading(false);
        }).fail(function () { self.appointmentsLoading(false); });
    };

    self.selectAppointment = function (appt) {
        // Sepeti temizle
        self.cartItems([]);

        // Müşteriyi seç
        if (appt.slnClientId) {
            self.clientId(appt.slnClientId);
            self.clientAutocomplete.query(appt.clientName || '');
            self.clientAutocomplete.selectedName(appt.clientName || '');
        }

        // Personeli seç
        if (appt.personnelId) {
            self.selectedPersonnelId(appt.personnelId.toString());
        }

        // Hizmetleri sepete ekle
        var services = appt.services || [];
        if (services.length > 0) {
            services.forEach(function (s) {
                var svc = self.allServices().find(function (sv) { return sv.id === (s.slnServiceId || s.serviceId); });
                if (svc) {
                    self.cartItems.push({
                        serviceId: svc.id,
                        name: svc.name,
                        unitPrice: svc.price,
                        quantity: ko.observable(1)
                    });
                }
            });
        } else if (appt.serviceNames && appt.serviceNames.length > 0) {
            // serviceNames varsa isimleriyle eşleştir
            appt.serviceNames.forEach(function (svcName) {
                var svc = self.allServices().find(function (sv) { return sv.name === svcName; });
                if (svc) {
                    self.cartItems.push({
                        serviceId: svc.id,
                        name: svc.name,
                        unitPrice: svc.price,
                        quantity: ko.observable(1)
                    });
                }
            });
        }

        // Randevu bağla
        self.linkedAppointmentId(appt.id);

        appointmentModal.hide();
        toastr.info('Randevu sepete alındı. Ek hizmet/ürün ekleyebilirsiniz.');
    };

    self.unlinkAppointment = function () {
        self.linkedAppointmentId(null);
    };

    // ═══ Init ═══
    $(document).ready(function () {
        self.loadData();
    });
}

ko.applyBindings(new SalesViewModel(), document.getElementById('sales-vm'));
