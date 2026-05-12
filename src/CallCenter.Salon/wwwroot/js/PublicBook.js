(function () {
    toastr.options = { closeButton: true, progressBar: true, positionClass: 'toast-top-right', timeOut: 3500 };

var bookData = document.getElementById('book-data');
        var bookSlug = bookData ? bookData.getAttribute('data-slug') : '';
        var bookDocumentTitlePrefix = bookData ? bookData.getAttribute('data-title-prefix') : 'Randevu';
        var bookTexts = {};
        document.querySelectorAll('#book-i18n [data-key]').forEach(function (el) {
            bookTexts[el.dataset.key] = (el.textContent || '').trim();
        });
        function bookT(key, fallback) {
            return bookTexts[key] || fallback || key;
        }
        function getStoredPlatformToken() {
            var token = localStorage.getItem('platformToken');
            if (!token || token === 'null' || token === 'undefined') {
                localStorage.removeItem('platformToken');
                localStorage.removeItem('platformUser');
                return '';
            }
            return token;
        }

        // Ahmet: randevu icin login zorunlu (kimlik gerek). Bekleme listesi profil sayfasindan anonim alinir.
        if (!getStoredPlatformToken()) {
            window.location.href = '/user/login?returnUrl=' + encodeURIComponent(window.location.pathname);
        }

        function BookViewModel() {
            var self = this;
            self.salonName = ko.observable('');
            self.branchName = ko.observable('');
            self.isHeadquarter = ko.observable(true);
            self.logoUrl = ko.observable('');
            self.coverImageUrl = ko.observable('');
            self.displaySalonName = ko.computed(function () {
                if (self.branchName() && self.branchName() !== self.salonName() && !self.isHeadquarter()) {
                    return self.branchName() + ' - ' + self.salonName();
                }
                return self.salonName();
            });
            self.customerId = ko.observable(null);
            self.categories = ko.observableArray([]);
            self.serviceCombos = ko.observableArray([]);
            self.expandedCategoryId = ko.observable(null);
            self.selectedServiceId = ko.observable(null);
            self.selectedServiceIds = ko.observableArray([]);
            self.selectedComboId = ko.observable(null);

            // Staff
            self.availableStaff = ko.observableArray([]);
            self.staffLoading = ko.observable(false);
            self.selectedStaffId = ko.observable(null); // null = fark etmez

            // Date & Time
            function localDateInputValue(date) {
                var d = date || new Date();
                var y = d.getFullYear();
                var m = String(d.getMonth() + 1).padStart(2, '0');
                var day = String(d.getDate()).padStart(2, '0');
                return y + '-' + m + '-' + day;
            }

            self.selectedDate = ko.observable(localDateInputValue());
            self.slots = ko.observableArray([]);
            self.slotsLoaded = ko.observable(false);
            self.selectedSlot = ko.observable(null);
            self.isDayClosed = ko.observable(false);
            // Slot seçildiğinde hangi personel atandı (fark etmez modunda)
            self.autoAssignedStaffId = ko.observable(null);
            self.autoAssignedStaffName = ko.observable(null);

            // Step
            self.currentStep = ko.observable(1);
            self.isSaving = ko.observable(false);
            self.bookingDone = ko.observable(false);

            // Policy
            self.bookingPolicy = ko.observable(null);

            self.form = {
                fullName: ko.observable(''),
                phone: ko.observable(''),
                email: ko.observable(''),
                notes: ko.observable('')
            };

            // ═══ Bekleme Listesi ═══
            self.waitlistOpen = ko.observable(false);
            self.waitlistSaving = ko.observable(false);
            self.waitlist = {
                fullName: ko.observable(''),
                phone: ko.observable(''),
                email: ko.observable(''),
                timeSlot: ko.observable('Farketmez'),
                notes: ko.observable('')
            };

            self.openWaitlist = function () {
                // form.fullName/phone doluysa onceden doldur
                self.waitlist.fullName(self.form.fullName() || '');
                self.waitlist.phone(self.form.phone() || '');
                self.waitlist.email(self.form.email() || '');
                self.waitlist.timeSlot('Farketmez');
                self.waitlist.notes('');
                self.waitlistOpen(true);
            };

            self.closeWaitlist = function () { self.waitlistOpen(false); };

            self.submitWaitlist = function () {
                if (!self.waitlist.fullName() || !self.waitlist.phone()) {
                    toastr.warning(bookT('salon.book.waitlist.required', 'Ad ve telefon zorunlu'));
                    return;
                }
                if (self.selectedServiceIds().length === 0) {
                    toastr.warning(bookT('salon.book.waitlist.service_required', 'Önce hizmet seçiniz'));
                    return;
                }
                self.waitlistSaving(true);
                var payload = {
                    fullName: self.waitlist.fullName(),
                    phone: self.waitlist.phone(),
                    email: self.waitlist.email() || null,
                    serviceId: self.selectedServiceIds()[0],
                    serviceIds: self.selectedServiceIds(),
                    personnelId: self.selectedStaffId() || null,
                    preferredDate: self.selectedDate() + 'T00:00:00Z',
                    preferredTimeSlot: self.waitlist.timeSlot(),
                    notes: self.waitlist.notes() || null
                };
                fetch('/proxy/salon/' + bookSlug + '/waitlist', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify(payload)
                }).then(function (r) { return r.json().then(function (j) { return { ok: r.ok, body: j }; }); })
                  .then(function (res) {
                       self.waitlistSaving(false);
                       if (res.ok) {
                           toastr.success(res.body.message || bookT('salon.book.waitlist.success', 'Bekleme listesine eklendiniz.'));
                           self.closeWaitlist();
                       } else {
                           toastr.error(res.body.message || bookT('salon.book.waitlist.save_failed', 'Kayıt başarısız'));
                       }
                   })
                   .catch(function () {
                       self.waitlistSaving(false);
                       toastr.error(bookT('salon.book.error.connection', 'Bağlantı hatası'));
                   });
            };

            // Checkout aktif mi (Iyzico form gösteriliyor)
            self.checkoutActive = ko.observable(false);

            // Computed
            self.selectedCombo = ko.computed(function () {
                var id = self.selectedComboId();
                if (!id) return null;
                return self.serviceCombos().find(function (c) { return c.id === id; }) || null;
            });

            self.comboSummary = function (combo) {
                var names = (combo.items || []).map(function (i) { return i.serviceName; }).filter(Boolean);
                return names.length ? names.join(' + ') : '';
            };

            self.allServices = ko.computed(function () {
                var list = [];
                self.categories().forEach(function (c) {
                    (c.services || []).forEach(function (s) { list.push(s); });
                });
                return list;
            });

            self.selectedServices = ko.computed(function () {
                var ids = self.selectedServiceIds();
                return ids.map(function (id) {
                    return self.allServices().find(function (s) { return s.id === id; });
                }).filter(Boolean);
            });

            self.selectedServiceName = ko.computed(function () {
                var combo = self.selectedCombo();
                if (combo) return combo.name || '';
                return self.selectedServices().map(function (s) { return s.name; }).join(' + ');
            });

            self.selectedServicePrice = ko.computed(function () {
                var combo = self.selectedCombo();
                if (combo) {
                    var comboPrice = combo.price || 0;
                    return comboPrice > 0 ? comboPrice.toLocaleString(document.documentElement.lang || undefined) + ' TL' : '';
                }
                var price = self.selectedServices().reduce(function (total, s) { return total + (Number(s.price) || 0); }, 0);
                return price > 0 ? price.toLocaleString(document.documentElement.lang || undefined) + ' TL' : '';
            });

            self.selectedStaffName = ko.computed(function () {
                var id = self.selectedStaffId();
                if (id === null) return bookT('salon.book.staff.any', 'Fark etmez');
                var staff = self.availableStaff().find(function (s) { return s.id === id; });
                return staff ? staff.name : '';
            });

            // Onay sayfasında gösterilecek personel: "fark etmez" modunda slot'tan otomatik atananı göster
            self.confirmedStaffName = ko.computed(function () {
                if (self.selectedStaffId() !== null) return self.selectedStaffName();
                return self.autoAssignedStaffName()
                    ? self.autoAssignedStaffName() + ' (' + bookT('salon.book.staff.auto_suffix', 'otomatik') + ')'
                    : bookT('salon.book.staff.any', 'Fark etmez');
            });

            self.securePaymentDescription = ko.computed(function () {
                var policy = self.bookingPolicy();
                if (!policy || !policy.depositAmount) return '';
                var amount = policy.depositAmount.toLocaleString(document.documentElement.lang || undefined) + ' TL';
                return bookT('salon.book.payment.deposit_3ds_desc', '{amount} tutarındaki depozito için 3D Secure güvenli ödeme sayfasına yönlendirileceksiniz.')
                    .replace('{amount}', amount);
            });

            self.formattedDate = ko.computed(function () {
                var d = self.selectedDate();
                if (!d) return '';
                var dt = new Date(d + 'T00:00:00');
                return dt.toLocaleDateString(document.documentElement.lang || undefined, { day: 'numeric', month: 'long', year: 'numeric', weekday: 'long' });
            });

            self.selectedSlotLabel = ko.computed(function () {
                var st = self.selectedSlot();
                if (!st) return '';
                var s = self.slots().find(function (x) { return x.startTime === st; });
                return s ? s.timeText : '';
            });

            self.canProceed = ko.computed(function () {
                var step = self.currentStep();
                if (step === 1) return self.selectedServiceIds().length > 0 || !!self.selectedComboId();
                if (step === 2) return true; // null = fark etmez, her zaman gecerli
                if (step === 3) return !!self.selectedSlot();
                if (step === 4) return !!self.form.fullName() && !!self.form.phone();
                return true;
            });

            // Giris yapan kullanicinin bilgilerini doldur
            var platformUser = null;
            try { platformUser = JSON.parse(localStorage.getItem('platformUser') || 'null'); } catch (e) {}
            if (platformUser) {
                self.form.fullName(platformUser.fullName || '');
                self.form.phone(platformUser.phone || '');
                self.form.email(platformUser.email || '');
            }

            self.ensureSalonLink = function () {
                var token = getStoredPlatformToken();
                var cid = self.customerId();
                if (!token) return;
                if (!cid) {
                    fetch('/proxy/salon/' + bookSlug)
                        .then(function (r) { return r.ok ? r.json() : null; })
                        .then(function (data) {
                            if (!data) return;
                            self.customerId(data.customerId || data.CustomerId || null);
                            if (self.customerId()) self.ensureSalonLink();
                        })
                        .catch(function () {});
                    return;
                }
                fetch('/public-proxy/platform/salons/join', {
                    method: 'POST',
                    headers: { 'Authorization': 'Bearer ' + token, 'Content-Type': 'application/json' },
                    body: JSON.stringify({ customerId: cid })
                }).catch(function () { /* Panel randevuyu telefonla da bulur; uyelik linki en iyi caba. */ });
            };

            // Load salon
            fetch('/proxy/salon/' + bookSlug)
                .then(function (r) { return r.json(); })
                .then(function (data) {
                    self.salonName(data.salonName);
                    self.branchName(data.branchName || '');
                    self.isHeadquarter(data.isHeadquarter !== false);
                    self.logoUrl(data.logoUrl || '');
                    self.coverImageUrl(data.coverImageUrl || '');
                    self.customerId(data.customerId || data.CustomerId || null);
                    self.categories(data.serviceCategories || []);
                    self.serviceCombos(data.serviceCombos || []);
                document.title = bookDocumentTitlePrefix + ' - ' + data.salonName;
                    // Ilk kategoriyi ac
                    if (data.serviceCategories && data.serviceCategories.length > 0) {
                        self.expandedCategoryId(data.serviceCategories[0].id);
                    }
                });

            // Load booking policy
            fetch('/proxy/salon/' + bookSlug + '/booking-policy')
                .then(function (r) { return r.ok ? r.json() : null; })
                .then(function (data) { if (data) self.bookingPolicy(data); });

            // Category accordion
            self.toggleCategory = function (category) {
                self.expandedCategoryId(self.expandedCategoryId() === category.id ? null : category.id);
            };

            self.selectService = function (svc) {
                self.selectedComboId(null);
                var ids = self.selectedServiceIds().slice();
                var index = ids.indexOf(svc.id);
                if (index >= 0) ids.splice(index, 1);
                else ids.push(svc.id);
                self.selectedServiceIds(ids);
                self.selectedServiceId(ids.length ? ids[0] : null);
                self.availableStaff([]);
                self.slots([]);
                self.selectedStaffId(null);
                self.selectedSlot(null);
            };

            self.selectCombo = function (combo) {
                var ids = (combo.items || []).map(function (item) { return item.serviceId; }).filter(Boolean);
                self.selectedComboId(combo.id);
                self.selectedServiceIds(ids);
                self.selectedServiceId(ids.length ? ids[0] : null);
            };

            self.selectStaff = function (staff) { self.selectedStaffId(staff.id); };

            // Load staff for selected service
            self.loadStaff = function () {
                var ids = self.selectedServiceIds();
                if (!ids.length && !self.selectedComboId()) return;
                self.staffLoading(true);
                self.availableStaff([]);
                self.selectedStaffId(null);
                var url = '/proxy/salon/' + bookSlug + '/available-staff?serviceIds=' + ids.join(',');
                if (self.selectedComboId()) url += '&comboId=' + self.selectedComboId();
                fetch(url)
                    .then(function (r) { return r.json(); })
                    .then(function (data) {
                        self.availableStaff(data || []);
                        self.staffLoading(false);
                    })
                    .catch(function () { self.staffLoading(false); });
            };

            self.selectedDate.subscribe(function () { self.loadSlots(); });

            self.loadSlots = function () {
                var ids = self.selectedServiceIds();
                var date = self.selectedDate();
                if ((!ids.length && !self.selectedComboId()) || !date) return;
                self.slotsLoaded(false);
                self.selectedSlot(null);
                self.isDayClosed(false);
                self.autoAssignedStaffId(null);
                self.autoAssignedStaffName(null);

                var url = '/proxy/salon/' + bookSlug + '/available-slots?serviceIds=' + ids.join(',') + '&date=' + date;
                if (self.selectedComboId()) {
                    url += '&comboId=' + self.selectedComboId();
                }
                if (self.selectedStaffId() !== null) {
                    url += '&personnelId=' + self.selectedStaffId();
                }

                fetch(url)
                    .then(function (r) { return r.json(); })
                    .then(function (data) {
                        // API yeni format: { isClosed, slots }
                        if (data && typeof data.isClosed !== 'undefined') {
                            self.isDayClosed(data.isClosed);
                            self.slots(data.slots || []);
                        } else {
                            // Eski format (dizi) fallback — availableStaff yoksa bos dizi ekle
                            self.isDayClosed(false);
                            var items = Array.isArray(data) ? data : [];
                            items.forEach(function(s) { if (!s.availableStaff) s.availableStaff = []; });
                            self.slots(items);
                        }
                        self.slotsLoaded(true);
                    })
                    .catch(function () { self.slotsLoaded(true); });
            };

            self.selectSlot = function (slot) {
                self.selectedSlot(slot.startTime);
                // Fark etmez modunda: slottaki ilk müsait personeli otomatik ata (sadece onay ekranı için)
                if (self.selectedStaffId() === null && slot.availableStaff && slot.availableStaff.length > 0) {
                    self.autoAssignedStaffId(slot.availableStaff[0].id);
                    self.autoAssignedStaffName(slot.availableStaff[0].name);
                } else {
                    self.autoAssignedStaffId(null);
                    self.autoAssignedStaffName(null);
                }
            };

            self.nextStep = function () {
                if (!self.canProceed()) return;
                var step = self.currentStep();
                if (step === 1) { self.loadStaff(); }
                if (step === 2) { self.loadSlots(); }
                self.currentStep(step + 1);
            };

            self.prevStep = function () { self.currentStep(self.currentStep() - 1); };

            self.confirmBooking = function () {
                self.isSaving(true);
                var payload = {
                    fullName: self.form.fullName(),
                    phone: self.form.phone(),
                    email: self.form.email(),
                    serviceId: self.selectedServiceIds()[0],
                    serviceIds: self.selectedServiceIds(),
                    comboId: self.selectedComboId(),
                    startTime: self.selectedSlot(),
                    notes: self.form.notes()
                };
                if (self.selectedStaffId() !== null) {
                    payload.personnelId = self.selectedStaffId();
                } else if (self.autoAssignedStaffId()) {
                    payload.personnelId = self.autoAssignedStaffId();
                }

                fetch('/proxy/salon/' + bookSlug + '/book-checkout', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify(payload)
                })
                .then(function (r) {
                    return r.json().then(function (d) { return { ok: r.ok, data: d }; });
                })
                .then(function (res) {
                    self.isSaving(false);
                    if (res.ok && res.data && res.data.success) {
                        if (res.data.requireDeposit && res.data.htmlContent) {
                            // 3DS ödeme formu göster
                            self.checkoutActive(true);
                            var container = document.getElementById('iyzico-checkout-container');
                            container.style.display = '';
                            $(container).html(res.data.htmlContent);
                        } else {
                            self.ensureSalonLink();
                            self.bookingDone(true);
                        }
                    } else {
                        var msg = (res.data && (res.data.message || res.data)) || bookT('salon.book.error.create_failed', 'Randevu oluşturulamadı.');
                        toastr.error(typeof msg === 'string' ? msg : bookT('salon.book.error.create_failed', 'Randevu oluşturulamadı.'));
                    }
                })
                .catch(function () {
                    toastr.error(bookT('salon.book.error.retry', 'Bir hata oluştu. Lütfen tekrar deneyin.'));
                    self.isSaving(false);
                });
            };

            // Iyzico postMessage listener (iframe callback sonrasi)
            window.addEventListener('message', function (e) {
                if (e.data === 'payment-success' || (e.data && e.data.type === 'payment-success')) {
                    self.checkoutActive(false);
                    document.getElementById('iyzico-checkout-container').style.display = 'none';
                    self.ensureSalonLink();
                    self.bookingDone(true);
                } else if (e.data === 'payment-failed' || (e.data && e.data.type === 'payment-failed')) {
                    self.checkoutActive(false);
                    document.getElementById('iyzico-checkout-container').style.display = 'none';
                    toastr.error((e.data && e.data.error) || bookT('salon.book.payment.failed', 'Ödeme başarısız oldu. Lütfen tekrar deneyin.'));
                }
            });
        }

        var bookVm = new BookViewModel();
        var bookRoot = document.getElementById('book-vm');
        ko.applyBindings(bookVm, bookRoot);


        // Iyzico 3DS tam-sayfa yönlendirme dönüşü: /salon/{slug}/book?iyzicoToken=...&paid=true/false
        (function handleIyzicoReturn() {
            var p = new URLSearchParams(window.location.search);
            var token = p.get('iyzicoToken');
            if (!token) return;

            var paid = p.get('paid') === 'true';
            var payerr = p.get('payerr');

            // URL'i temizle
            var u = new URL(window.location.href);
            u.searchParams.delete('iyzicoToken');
            u.searchParams.delete('paid');
            u.searchParams.delete('payerr');
            var q = u.searchParams.toString();
            window.history.replaceState({}, '', u.pathname + (q ? '?' + q : '') + u.hash);

            if (paid) {
                bookVm.currentStep(5);
                bookVm.ensureSalonLink();
                bookVm.bookingDone(true);
            } else {
                toastr.error(payerr ? decodeURIComponent(payerr) : bookT('salon.book.payment.not_completed', 'Ödeme tamamlanamadı. Lütfen tekrar deneyin.'));
            }
        })();
})();
