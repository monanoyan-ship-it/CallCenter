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
    self.isPrepaid = ko.observable(false);
    self.prepaidAmount = ko.observable(0);

    // ═══ Autocomplete ═══
    self.clientAutocomplete = createAutocomplete(self.clientList, 'fullName', self.clientId);

    // Musteri secildiginde uyelik kontrolu
    self.clientId.subscribe(function (newClientId) {
        if (!newClientId || self.cartItems().length === 0) return;
        self.applyMembershipBenefits();
    });

    self.applyMembershipBenefits = function () {
        var clientId = self.clientId();
        if (!clientId) return;
        var serviceIds = self.cartItems().filter(function (i) { return i.serviceId; }).map(function (i) { return i.serviceId; });
        if (serviceIds.length === 0) return;

        $.ajax({
            url: '/proxy/sln-memberships/check-benefits',
            method: 'POST',
            contentType: 'application/json',
            data: JSON.stringify({ slnClientId: parseInt(clientId), serviceIds: serviceIds })
        }).done(function (benefits) {
            if (!benefits || !benefits.length) return;
            self.cartItems().forEach(function (item) {
                var benefit = benefits.find(function (b) { return b.serviceId === item.serviceId; });
                if (!benefit) return;

                if (benefit.hasFreeBenefit && benefit.remainingFree > 0) {
                    // Ucretsiz hakki var
                    item.editPrice(0);
                    item.benefitText = benefit.planName + ': ' + benefit.usedThisMonth + '/' + benefit.freeCountPerMonth + ' kullanildi (ucretsiz)';
                } else if (benefit.discountPercent && benefit.discountPercent > 0) {
                    // Indirimli
                    var discounted = item.unitPrice * (1 - benefit.discountPercent / 100);
                    item.editPrice(Math.round(discounted * 100) / 100);
                    item.benefitText = benefit.planName + ': %' + benefit.discountPercent + ' indirim';
                }
            });
            if (benefits.some(function (b) { return b.hasFreeBenefit || b.discountPercent; }))
                toastr.info('Üyelik avantajları uygulandı.');
        });
    };

    self.filteredServices = ko.computed(function () {
        var catId = self.selectedCategoryId();
        if (!catId) return self.allServices();
        return self.allServices().filter(function (s) { return s.categoryId === catId && s.isActive; });
    });

    self.subtotal = ko.computed(function () {
        var total = 0;
        self.cartItems().forEach(function (item) { total += item.quantity() * (parseFloat(item.editPrice()) || 0); });
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
                        editPrice: ko.observable(item.servicePrice),
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
            editPrice: ko.observable(service.price),
            quantity: ko.observable(1),
            benefitText: null
        });
        // Uyelik kontrolu
        self.applyMembershipBenefits();
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

        // BUG2.1: Personel secilmediyse uyari — onayla devam edebilir
        if (!self.selectedPersonnelId()) {
            if (!confirm('Personel seçmediniz. Bu tahsilat personele atanmadan kaydedilecek. Devam edilsin mi?')) {
                return;
            }
        }

        // BUG2.2: Musteri secilmediyse ad/soyad sor, hizli musteri olustur
        if (!self.clientId()) {
            var quickName = prompt('Müşteri seçilmedi. Kayıt için ad soyad girin (iptal = işlem iptal):', '');
            if (quickName === null) return;
            quickName = quickName.trim();
            if (!quickName) {
                toastr.warning('Müşteri adı gerekli.');
                return;
            }
            self.isSaving(true);
            $.ajax({
                url: '/proxy/sln-clients',
                method: 'POST',
                contentType: 'application/json',
                data: JSON.stringify({ fullName: quickName })
            }).done(function (resp) {
                self.clientId(resp.id || resp.Id);
                self.isSaving(false);
                self.checkout();
            }).fail(function () {
                toastr.error('Müşteri oluşturulamadı.');
                self.isSaving(false);
            });
            return;
        }

        var items = self.cartItems().map(function (item) {
            return {
                serviceId: item.serviceId,
                productId: null,
                personnelId: self.selectedPersonnelId() ? parseInt(self.selectedPersonnelId()) : null,
                quantity: item.quantity(),
                unitPrice: parseFloat(item.editPrice()) || item.unitPrice,
                discountAmount: 0
            };
        });

        var data = {
            slnClientId: self.clientId() ? parseInt(self.clientId()) : null,
            paymentMethodId: parseInt(self.paymentMethodId()) || 1,
            discountAmount: parseFloat(self.discountAmount()) || 0,
            tipAmount: parseFloat(self.tipAmount()) || 0,
            notes: self.isPrepaid() ? 'Ön ödeme: ' + self.prepaidAmount() + ' TL (Online)' : null,
            prepaidAmount: self.prepaidAmount(),
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

        var today = new Date();
        var todayStr = today.getFullYear() + '-' + String(today.getMonth() + 1).padStart(2, '0') + '-' + String(today.getDate()).padStart(2, '0');
        var tomorrowDate = new Date(today); tomorrowDate.setDate(tomorrowDate.getDate() + 1);
        var tomorrowStr = tomorrowDate.getFullYear() + '-' + String(tomorrowDate.getMonth() + 1).padStart(2, '0') + '-' + String(tomorrowDate.getDate()).padStart(2, '0');

        $.get('/proxy/sln-appointments?from=' + todayStr + '&to=' + tomorrowStr, function (data) {
            var list = (data.items || data || []).filter(function (a) {
                // Sadece planlanan(1) ve onaylanan(2) randevulari goster
                return a.statusId === 1 || a.statusId === 2;
            }).map(function (a) {
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

    self.remainingAmount = ko.computed(function () {
        return Math.max(0, self.grandTotal() - self.prepaidAmount());
    });

    self.selectAppointment = function (appt) {
        // Sepeti temizle
        self.cartItems([]);

        // Ön ödeme kontrolü
        self.isPrepaid(appt.isPrepaid || false);
        self.prepaidAmount(appt.prepaidAmount || 0);

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
                        editPrice: ko.observable(svc.price),
                        quantity: ko.observable(1)
                    });
                }
            });
        } else if (appt.serviceNames && appt.serviceNames.length > 0) {
            appt.serviceNames.forEach(function (svcName) {
                var svc = self.allServices().find(function (sv) { return sv.name === svcName; });
                if (svc) {
                    self.cartItems.push({
                        serviceId: svc.id,
                        name: svc.name,
                        unitPrice: svc.price,
                        editPrice: ko.observable(svc.price),
                        quantity: ko.observable(1)
                    });
                }
            });
        }

        // Randevu bağla
        self.linkedAppointmentId(appt.id);
        appointmentModal.hide();

        // Ön ödemeli ise direkt tamamla mı sor
        if (appt.isPrepaid && appt.prepaidAmount > 0) {
            confirmModal('Onay', 'Bu randevu online ödenmiş (' + appt.prepaidAmount + ' TL). Ek işlem yoksa direkt tamamlansın mı?', function() {
                self.completeWithoutPayment(appt.id);
            });
            return;
        }

        // Üyelik avantajı kontrolü
        if (appt.slnClientId) {
            var serviceIds = self.cartItems().filter(function (i) { return i.serviceId; }).map(function (i) { return i.serviceId; });
            if (serviceIds.length > 0) {
                $.ajax({
                    url: '/proxy/sln-memberships/check-benefits',
                    method: 'POST',
                    contentType: 'application/json',
                    data: JSON.stringify({ slnClientId: parseInt(appt.slnClientId), serviceIds: serviceIds })
                }).done(function (benefits) {
                    if (!benefits || !benefits.length) return;

                    var allFree = true;
                    self.cartItems().forEach(function (item) {
                        var b = benefits.find(function (x) { return x.serviceId === item.serviceId; });
                        if (!b) { allFree = false; return; }

                        if (b.hasFreeBenefit && b.remainingFree > 0) {
                            item.editPrice(0);
                            item.benefitText = b.planName + ': ücretsiz (' + b.usedThisMonth + '/' + b.freeCountPerMonth + ')';
                        } else if (b.discountPercent && b.discountPercent > 0) {
                            item.editPrice(Math.round(item.unitPrice * (1 - b.discountPercent / 100) * 100) / 100);
                            item.benefitText = b.planName + ': %' + b.discountPercent + ' indirim';
                            allFree = false;
                        } else {
                            allFree = false;
                        }
                    });

                    if (allFree && self.cartItems().length > 0) {
                        confirmModal('Onay', 'Tüm hizmetler üyelik kapsamında ücretsiz. Ek işlem yoksa direkt tamamlansın mı?', function() {
                            self.completeWithoutPayment(appt.id);
                        });
                        return;
                    }

                    toastr.info('Üyelik avantajları uygulandı. Ek hizmet/ürün ekleyebilirsiniz.');
                });
                return;
            }
        }

        toastr.info('Randevu sepete alındı. Ek hizmet/ürün ekleyebilirsiniz.');
    };

    self.completeWithoutPayment = function (appointmentId) {
        // Adisyon 0 TL olustur (kayit icin) + randevu tamamla
        var items = self.cartItems().map(function (item) {
            return {
                serviceId: item.serviceId, productId: null,
                personnelId: self.selectedPersonnelId() ? parseInt(self.selectedPersonnelId()) : null,
                quantity: item.quantity(), unitPrice: 0, discountAmount: 0
            };
        });

        var data = {
            slnClientId: self.clientId() ? parseInt(self.clientId()) : null,
            paymentMethodId: 1,
            discountAmount: 0, tipAmount: 0,
            notes: self.isPrepaid() ? 'Ön ödeme ile tamamlandı' : 'Üyelik kapsamında tamamlandı',
            prepaidAmount: self.prepaidAmount(),
            items: items
        };

        $.ajax({
            url: '/proxy/sln-finance/invoices', method: 'POST',
            contentType: 'application/json', data: JSON.stringify(data)
        }).done(function () {
            // Randevu tamamla
            $.ajax({
                url: '/proxy/sln-appointments/' + appointmentId + '/status',
                method: 'PUT', contentType: 'application/json',
                data: JSON.stringify({ statusId: 3 })
            });
            toastr.success('İşlem tamamlandı (ödeme alınmadı).');
            self.cartItems([]);
            self.clientId(null);
            self.clientAutocomplete.clear();
            self.linkedAppointmentId(null);
            self.isPrepaid(false);
            self.prepaidAmount(0);
        }).fail(function () { toastr.error('İşlem kaydedilemedi.'); });
    };

    self.unlinkAppointment = function () {
        self.linkedAppointmentId(null);
        self.isPrepaid(false);
        self.prepaidAmount(0);
    };

    // ═══ Init ═══
    $(document).ready(function () {
        self.loadData();
    });
}

ko.applyBindings(new SalesViewModel(), document.getElementById('sales-vm'));
