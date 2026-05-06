function slnJsT(key, fallback) {
    return (window.salonT || function (k, f) { return f || k; })(key, fallback);
}

function SalesViewModel() {
    var self = this;
    self.categories = ko.observableArray([]);
    self.allServices = ko.observableArray([]);
    self.products = ko.observableArray([]);
    self.clientList = ko.observableArray([]);
    self.staffList = ko.observableArray([]);
    self.recipes = ko.observableArray([]);
    self.selectedCategoryId = ko.observable(null);
    self.productSearchQuery = ko.observable('');
    self.showRecipes = ko.observable(false);
    self.cartItems = ko.observableArray([]);
    self.clientId = ko.observable(null);
    self.selectedPersonnelId = ko.observable(null);
    self.paymentMethodId = ko.observable('1');
    self.giftCardCode = ko.observable('');
    self.discountAmount = ko.observable(0);
    self.tipAmount = ko.observable(0);
    self.tipIncludeInTotal = ko.observable(false); // BUG.A2: bahsis toplama dahil mi
    self.linkedAppointmentId = ko.observable(null);
    self.todayAppointments = ko.observableArray([]);
    self.appointmentsLoading = ko.observable(false);
    self.isSaving = ko.observable(false);
    self.isPrepaid = ko.observable(false);
    self.prepaidAmount = ko.observable(0);

    function readError(xhr, fallback) {
        if (typeof xhr.responseJSON === 'string') return xhr.responseJSON;
        return xhr.responseJSON?.error || xhr.responseJSON?.message || xhr.responseText || fallback;
    }

    // ═══ Autocomplete ═══
    self.clientAutocomplete = createAutocomplete(self.clientList, 'fullName', self.clientId);

    self.ensureBenefitFields = function (item) {
        if (typeof item.benefitText !== 'function') item.benefitText = ko.observable(item.benefitText || null);
        if (!('membershipId' in item)) item.membershipId = null;
        if (!('useMembershipBenefit' in item)) item.useMembershipBenefit = false;
        if (!('clientPackageId' in item)) item.clientPackageId = null;
        if (!('usePackageSession' in item)) item.usePackageSession = false;
        if (!('packageRemainingSessions' in item)) item.packageRemainingSessions = null;
        return item;
    };

    self.resetServiceBenefit = function (item) {
        self.ensureBenefitFields(item);
        if (!item.serviceId) return;
        item.membershipId = null;
        item.useMembershipBenefit = false;
        item.clientPackageId = null;
        item.usePackageSession = false;
        item.packageRemainingSessions = null;
        item.benefitText(null);
        item.editPrice(item.unitPrice);
    };

    // Musteri secildiginde uyelik kontrolu
    self.clientId.subscribe(function (newClientId) {
        if (!newClientId || self.cartItems().length === 0) return;
        self.applyClientBenefits();
    });

    self.applyMembershipBenefits = function () {
        self.applyClientBenefits();
    };

    self.applyClientBenefits = function () {
        var clientId = self.clientId();
        if (!clientId) return;

        var serviceItems = self.cartItems().filter(function (i) { return i.serviceId; });
        serviceItems.forEach(self.resetServiceBenefit);

        var serviceIds = serviceItems.map(function (i) { return i.serviceId; })
            .filter(function (value, index, arr) { return arr.indexOf(value) === index; });
        if (serviceIds.length === 0) return;

        $.ajax({
            url: '/proxy/sln-packages/usable',
            method: 'POST',
            contentType: 'application/json',
            data: JSON.stringify({ slnClientId: parseInt(clientId), serviceIds: serviceIds })
        }).done(function (packages) {
            (packages || []).forEach(function (pkg) {
                var item = serviceItems.find(function (i) { return i.serviceId === pkg.serviceId && i.usePackageSession !== true; });
                if (!item || pkg.remainingSessions <= 0) return;

                item.editPrice(0);
                item.clientPackageId = pkg.clientPackageId;
                item.usePackageSession = true;
                item.packageRemainingSessions = pkg.remainingSessions;
                item.benefitText(pkg.packageName + ': paket seansi (kalan ' + pkg.remainingSessions + ')');
            });
        }).always(function () {
            self.applyMembershipOnly();
        });
    };

    self.applyMembershipOnly = function () {
        var clientId = self.clientId();
        if (!clientId) return;
        var serviceIds = self.cartItems().filter(function (i) { return i.serviceId && i.usePackageSession !== true; }).map(function (i) { return i.serviceId; });
        if (serviceIds.length === 0) return;

        self.cartItems().forEach(function (item) {
            if (!item.serviceId || item.usePackageSession === true) return;
            self.ensureBenefitFields(item);
            item.membershipId = null;
            item.useMembershipBenefit = false;
            item.benefitText(null);
            item.editPrice(item.unitPrice);
        });

        $.ajax({
            url: '/proxy/sln-memberships/check-benefits',
            method: 'POST',
            contentType: 'application/json',
            data: JSON.stringify({ slnClientId: parseInt(clientId), serviceIds: serviceIds })
        }).done(function (benefits) {
            if (!benefits || !benefits.length) return;
            self.cartItems().forEach(function (item) {
                if (item.usePackageSession === true) return;
                self.ensureBenefitFields(item);
                var benefit = benefits.find(function (b) { return b.serviceId === item.serviceId; });
                if (!benefit) return;

                if (benefit.hasFreeBenefit && benefit.remainingFree > 0) {
                    // Ucretsiz hakki var
                    item.editPrice(0);
                    item.membershipId = benefit.membershipId;
                    item.useMembershipBenefit = true;
                    item.benefitText(benefit.planName + ': ' + benefit.usedThisPeriod + '/' + benefit.freeCount + ' kullanildi (ucretsiz)');
                } else if (benefit.discountPercent && benefit.discountPercent > 0) {
                    // Indirimli
                    var discounted = item.unitPrice * (1 - benefit.discountPercent / 100);
                    item.editPrice(Math.round(discounted * 100) / 100);
                    item.membershipId = null;
                    item.useMembershipBenefit = false;
                    item.benefitText(benefit.planName + ': ' + slnJsT('salon.sales.membership_discount_suffix', '%{percent} indirim').replace('{percent}', benefit.discountPercent));
                }
            });
            if (benefits.some(function (b) { return b.hasFreeBenefit || b.discountPercent; }))
                toastr.info(slnJsT('salon.sales.js.uyelik_avantajlari_uygulandi', 'Üyelik avantajları uygulandı.'));
        });
    };

    self.filteredServices = ko.computed(function () {
        var catId = self.selectedCategoryId();
        if (!catId) return self.allServices();
        return self.allServices().filter(function (s) { return s.categoryId === catId && s.isActive; });
    });

    self.filteredProducts = ko.computed(function () {
        var q = (self.productSearchQuery() || '').trim().toLowerCase();
        if (!q) return [];
        return self.products().filter(function (p) {
            return (p.isActive !== false)
                && (((p.name || '').toLowerCase().indexOf(q) >= 0)
                    || ((p.barcode || '').toLowerCase().indexOf(q) >= 0));
        }).slice(0, 8);
    });

    self.subtotal = ko.computed(function () {
        var total = 0;
        self.cartItems().forEach(function (item) { total += item.quantity() * (parseFloat(item.editPrice()) || 0); });
        return total;
    });

    self.grandTotal = ko.computed(function () {
        var tip = self.tipIncludeInTotal() ? (parseFloat(self.tipAmount()) || 0) : 0;
        return Math.max(0, self.subtotal() - (parseFloat(self.discountAmount()) || 0) + tip);
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
        self.loadProducts();
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

    self.loadProducts = function () {
        $.ajax({ url: '/proxy/sln-products', method: 'GET' })
            .done(function (data) { self.products(data.items || data); })
            .fail(function () { self.products([]); });
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
                        quantity: ko.observable(1),
                        benefitText: ko.observable(null)
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
            benefitText: ko.observable(null)
        });
        // Uyelik kontrolu
        self.applyMembershipBenefits();
    };

    self.addProductToCart = function (product) {
        var stock = parseFloat(product.stockQuantity) || 0;
        if (stock <= 0) {
            toastr.warning(slnJsT('salon.sales.js.urun_stogu_yok', 'Ürün stoğu yok'));
            return;
        }

        var existing = self.cartItems().find(function (item) { return item.productId === product.id; });
        if (existing) {
            var nextQuantity = existing.quantity() + 1;
            if (nextQuantity > stock) {
                toastr.warning('Yetersiz stok: ' + product.name);
                return;
            }
            existing.quantity(nextQuantity);
            return;
        }

        self.cartItems.push({
            serviceId: null,
            productId: product.id,
            name: product.name,
            unitPrice: product.salePrice || 0,
            editPrice: ko.observable(product.salePrice || 0),
            quantity: ko.observable(1),
            stockQuantity: stock,
            benefitText: ko.observable(null)
        });
    };

    self.addProductBySearch = function () {
        var q = (self.productSearchQuery() || '').trim().toLowerCase();
        if (!q) return;

        var exact = self.products().find(function (p) {
            return (p.barcode || '').toLowerCase() === q;
        });
        var matches = self.filteredProducts();
        var product = exact || (matches.length === 1 ? matches[0] : null);

        if (!product) {
            toastr.warning(slnJsT('salon.sales.js.urun_bulunamadi', 'Ürün bulunamadı'));
            return;
        }

        self.addProductToCart(product);
        self.productSearchQuery('');
    };

    self.onProductSearchKeydown = function (_, event) {
        if (event.key === 'Enter') {
            self.addProductBySearch();
            return false;
        }
        return true;
    };

    self.increaseQty = function (item) {
        if (item.productId && item.quantity() + 1 > item.stockQuantity) {
            toastr.warning('Yetersiz stok: ' + item.name);
            return;
        }
        if (item.usePackageSession === true && item.quantity() + 1 > item.packageRemainingSessions) {
            toastr.warning(slnJsT('salon.sales.js.paket_seansi_yetersiz', 'Paket seansi yetersiz: ') + item.name);
            return;
        }
        item.quantity(item.quantity() + 1);
        if (item.serviceId) self.applyClientBenefits();
    };

    self.decreaseQty = function (item) {
        if (item.quantity() > 1) {
            item.quantity(item.quantity() - 1);
        } else {
            self.cartItems.remove(item);
        }
        if (item.serviceId) self.applyClientBenefits();
    };

    self.removeFromCart = function (item) {
        self.cartItems.remove(item);
        if (item.serviceId) self.applyClientBenefits();
    };

    // ═══ Checkout ═══
    // Asil odeme islemi (personel + musteri kontrolleri gectikten sonra)
    self._executeCheckout = function () {
        var items = self.cartItems().map(function (item) {
            return {
                serviceId: item.serviceId,
                productId: item.productId || null,
                personnelId: self.selectedPersonnelId() ? parseInt(self.selectedPersonnelId()) : null,
                quantity: item.quantity(),
                unitPrice: parseFloat(item.editPrice()) || item.unitPrice,
                discountAmount: 0,
                membershipId: item.useMembershipBenefit === true ? item.membershipId : null,
                useMembershipBenefit: item.useMembershipBenefit === true,
                clientPackageId: item.usePackageSession === true ? item.clientPackageId : null,
                usePackageSession: item.usePackageSession === true
            };
        });

        var data = {
            slnClientId: self.clientId() ? parseInt(self.clientId()) : null,
            paymentMethodId: parseInt(self.paymentMethodId()) || 1,
            giftCardCode: parseInt(self.paymentMethodId()) === 5 ? self.giftCardCode() : null,
            discountAmount: parseFloat(self.discountAmount()) || 0,
            tipAmount: parseFloat(self.tipAmount()) || 0,
            includeTipInTotal: self.tipIncludeInTotal() === true,
            notes: self.isPrepaid() ? slnJsT('salon.sales.note.prepayment_prefix', 'Ön ödeme') + ': ' + self.prepaidAmount() + ' TL (Online)' : null,
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
            toastr.success(slnJsT('salon.sales.js.odeme_alindi', 'Ödeme alındı'));

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
            self.loadProducts();
            self.clientId(null);
            self.clientAutocomplete.clear();
            self.discountAmount(0);
            self.tipAmount(0);
            self.giftCardCode('');
            self.linkedAppointmentId(null);
            self.isSaving(false);
        }).fail(function (xhr) {
            toastr.error(readError(xhr, slnJsT('salon.sales.js.odeme_alinamadi', 'Ödeme alınamadı')));
            self.isSaving(false);
        });
    };

    self.checkout = function () {
        if (self.cartItems().length === 0) {
            toastr.warning('Sepet bos');
            return;
        }

        if (parseInt(self.paymentMethodId()) === 5 && !(self.giftCardCode() || '').trim()) {
            toastr.warning('Hediye karti kodu girilmelidir');
            return;
        }

        // BUG2.2/PAY.4: Musteri secilmediyse ad/soyad sor, hizli musteri olustur
        if (!self.clientId()) {
            confirmModal(slnJsT('salon.sales.js.hizli_musteri', 'Hızlı Müşteri'), slnJsT('salon.sales.js.musteri_secilmedi_tahsilat_icin_ad_soyad_girin', 'Müşteri seçilmedi. Tahsilat için ad-soyad girin:'), function (name) {
                name = (name || '').trim();
                if (!name) { toastr.warning(slnJsT('salon.sales.js.musteri_adi_gerekli', 'Müşteri adı gerekli.')); return; }

                var body = { fullName: name };
                self.isSaving(true);
                $.ajax({
                    url: '/proxy/sln-clients',
                    method: 'POST',
                    contentType: 'application/json; charset=utf-8',
                    dataType: 'json',
                    data: JSON.stringify(body)
                }).done(function (resp) {
                    var newId = (resp && (resp.id || resp.Id)) || null;
                    if (!newId) {
                        console.error('[hizli musteri] gecerli id yok', resp);
                        toastr.error(slnJsT('salon.sales.js.musteri_olusturulamadi_kimlik_donmedi', 'Müşteri oluşturulamadı (kimlik dönmedi).'));
                        self.isSaving(false);
                        return;
                    }
                    self.clientId(newId);
                    // Autocomplete gorunumunu de senkronize et
                    if (self.clientAutocomplete) {
                        self.clientAutocomplete.query(name);
                        if (typeof self.clientAutocomplete.selectedName === 'function') {
                            self.clientAutocomplete.selectedName(name);
                        }
                    }
                    self.isSaving(false);
                    self.checkout(); // recurse — personel kontrolu icin
                }).fail(function (xhr) {
                    console.error('[hizli musteri] POST failed', xhr.status, xhr.responseText);
                    var msg = (xhr.responseJSON && (xhr.responseJSON.message || xhr.responseJSON.error))
                        || (slnJsT('salon.sales.customer_create_failed_http', 'Müşteri oluşturulamadı') + ' (HTTP ' + xhr.status + ').');
                    toastr.error(msg);
                    self.isSaving(false);
                });
            }, { input: true, inputLabel: slnJsT('salon.appointments.full_name_required', 'Ad Soyad *'), confirmText: slnJsT('salon.common.continue', 'Devam'), confirmClass: 'btn-primary' });
            return;
        }

        // BUG2.1: Personel secilmediyse uyari
        if (!self.selectedPersonnelId()) {
            confirmModal(
                slnJsT('salon.sales.staff_not_selected', 'Personel Seçilmedi'),
                slnJsT('salon.sales.staff_not_selected_confirm', 'Bu tahsilat personele atanmadan kaydedilecek. Devam edilsin mi?'),
                function () { self._executeCheckout(); },
                { confirmText: slnJsT('salon.common.continue_action', 'Devam Et'), confirmClass: 'btn-warning' }
            );
            return;
        }

        self._executeCheckout();
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
                // BUG2.17: Naive saat — DB Utc kind ile yazar ama saat LOCAL temsilidir
                a.startTimeText = a.startTime ? a.startTime.substring(11, 16) : '';
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
                        quantity: ko.observable(1),
                        benefitText: ko.observable(null)
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
                        quantity: ko.observable(1),
                        benefitText: ko.observable(null)
                    });
                }
            });
        }

        // Randevu bağla
        self.linkedAppointmentId(appt.id);
        appointmentModal.hide();

        if (!(appt.isPrepaid && appt.prepaidAmount > 0)) {
            if (appt.slnClientId) {
                self.applyClientBenefits();
            }
            toastr.info(slnJsT('salon.sales.js.randevu_sepete_alindi_ek_hizmet_urun_ekleyebilirsiniz', 'Randevu sepete alindi. Ek hizmet/ürün ekleyebilirsiniz.'));
            return;
        }

        // Ön ödemeli ise direkt tamamla mı sor
        if (appt.isPrepaid && appt.prepaidAmount > 0) {
            confirmModal(slnJsT('salon.common.btn.confirm', 'Onayla'), slnJsT('salon.sales.js.bu_randevu_online_odenmis', 'Bu randevu online ödenmiş (') + appt.prepaidAmount + slnJsT('salon.sales.js.tl_ek_islem_yoksa_direkt_tamamlansin_mi', ' TL). Ek işlem yoksa direkt tamamlansın mı?'), function() {
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
                            item.membershipId = b.membershipId;
                            item.useMembershipBenefit = true;
                            self.ensureBenefitFields(item);
                            item.benefitText(b.planName + ': ' + slnJsT('salon.sales.membership_free_usage_suffix', 'ücretsiz ({used}/{total})').replace('{used}', b.usedThisPeriod).replace('{total}', b.freeCount));
                        } else if (b.discountPercent && b.discountPercent > 0) {
                            item.editPrice(Math.round(item.unitPrice * (1 - b.discountPercent / 100) * 100) / 100);
                            item.membershipId = null;
                            item.useMembershipBenefit = false;
                            self.ensureBenefitFields(item);
                            item.benefitText(b.planName + ': ' + slnJsT('salon.sales.membership_discount_suffix', '%{percent} indirim').replace('{percent}', b.discountPercent));
                            allFree = false;
                        } else {
                            allFree = false;
                        }
                    });

                    if (allFree && self.cartItems().length > 0) {
                        confirmModal(slnJsT('salon.common.btn.confirm', 'Onayla'), slnJsT('salon.sales.js.tum_hizmetler_uyelik_kapsaminda_ucretsiz_ek_islem_yoksa_direkt_tamamla', 'Tüm hizmetler üyelik kapsamında ücretsiz. Ek işlem yoksa direkt tamamlansın mı?'), function() {
                            self.completeWithoutPayment(appt.id);
                        });
                        return;
                    }

                    toastr.info(slnJsT('salon.sales.js.uyelik_avantajlari_uygulandi_ek_hizmet_urun_ekleyebilirsiniz', 'Üyelik avantajları uygulandı. Ek hizmet/ürün ekleyebilirsiniz.'));
                });
                return;
            }
        }

        toastr.info(slnJsT('salon.sales.js.randevu_sepete_alindi_ek_hizmet_urun_ekleyebilirsiniz', 'Randevu sepete alındı. Ek hizmet/ürün ekleyebilirsiniz.'));
    };

    self.completeWithoutPayment = function (appointmentId) {
        // Adisyon 0 TL olustur (kayit icin) + randevu tamamla
        var items = self.cartItems().map(function (item) {
            return {
                serviceId: item.serviceId, productId: null,
                personnelId: self.selectedPersonnelId() ? parseInt(self.selectedPersonnelId()) : null,
                quantity: item.quantity(), unitPrice: 0, discountAmount: 0,
                membershipId: item.useMembershipBenefit === true ? item.membershipId : null,
                useMembershipBenefit: item.useMembershipBenefit === true,
                clientPackageId: item.usePackageSession === true ? item.clientPackageId : null,
                usePackageSession: item.usePackageSession === true
            };
        });

        var data = {
            slnClientId: self.clientId() ? parseInt(self.clientId()) : null,
            paymentMethodId: 1,
            discountAmount: 0, tipAmount: 0,
            notes: self.isPrepaid() ? slnJsT('salon.sales.note.completed_with_prepayment', 'Ön ödeme ile tamamlandı') : slnJsT('salon.sales.note.completed_with_membership', 'Üyelik kapsamında tamamlandı'),
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
            toastr.success(slnJsT('salon.sales.js.islem_tamamlandi_odeme_alinmadi', 'İşlem tamamlandı (ödeme alınmadı).'));
            self.cartItems([]);
            self.clientId(null);
            self.clientAutocomplete.clear();
            self.linkedAppointmentId(null);
            self.isPrepaid(false);
            self.prepaidAmount(0);
        }).fail(function (xhr) { toastr.error(readError(xhr, 'Islem kaydedilemedi.')); });
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
