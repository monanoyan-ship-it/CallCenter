(function () {
    var profileData = document.getElementById('profile-data');
    var PROFILE_LANG = profileData ? profileData.getAttribute('data-lang') : 'tr';
    var profileSlug = profileData ? profileData.getAttribute('data-slug') : '';
    try {
        window.salonTranslations = JSON.parse((profileData && profileData.getAttribute('data-translations')) || '{}');
    } catch (error) {
        window.salonTranslations = {};
    }
    window.salonT = window.salonT || function(key, fallback) {
        return (window.salonTranslations && window.salonTranslations[key]) || fallback || key;
    };
    function profileT(key, fallback) {
        return window.salonT(key, fallback);
    }
    toastr.options = { closeButton: true, progressBar: true, positionClass: 'toast-top-right', timeOut: 3500 };

var salonSlug = profileSlug;

        function PublicSalonViewModel() {
            var self = this;
            self.t = profileT;
            self.salon = ko.observable({});
            self.loaded = ko.observable(false);
            self.notFound = ko.observable(false);
            self.workingHours = ko.observableArray([]);
            self.membershipPlans = ko.observableArray([]);
            self.banners = ko.observableArray([]);
            self.galleryImages = ko.observableArray([]);
            self.teamMembers = ko.observableArray([]);
            self.reviews = ko.observableArray([]);
            self.reviewStats = ko.observable({});
            self.selectedPlanId = ko.observable(null);
            self.profileUrl = ko.computed(function () {
                return window.location.origin + '/salon/' + (self.salon().slug || salonSlug);
            });
            self.bookingUrl = ko.computed(function () {
                return self.profileUrl() + '/book';
            });
            self.qrCodeUrl = ko.computed(function () {
                return 'https://api.qrserver.com/v1/create-qr-code/?size=300x300&data=' + encodeURIComponent(self.bookingUrl());
            });
            self.widgetCode = ko.computed(function () {
                return '<iframe src="' + self.bookingUrl() + '?embed=1" width="100%" height="720" style="border:0;border-radius:8px" loading="lazy"></iframe>';
            });
            self.serviceCombos = ko.computed(function () {
                var s = self.salon();
                return s && Array.isArray(s.serviceCombos) ? s.serviceCombos : [];
            });

            // One cikan ilk 3 hizmet (kategorilerden flatten, fiyati > 0)
            self.featuredServices = ko.computed(function () {
                var s = self.salon();
                if (!s || !s.serviceCategories) return [];
                var all = [];
                s.serviceCategories.forEach(function (cat) {
                    (cat.services || []).forEach(function (sv) { if (sv.price > 0) all.push(sv); });
                });
                return all.slice(0, 3);
            });
            self.comboSummary = function (combo) {
                return (combo.items || [])
                    .map(function (item) { return item.serviceName; })
                    .filter(Boolean)
                    .join(' + ');
            };

            self.copyText = function (text, successKey, fallback) {
                if (navigator.clipboard && navigator.clipboard.writeText) {
                    navigator.clipboard.writeText(text).then(function () {
                        toastr.success(profileT(successKey, fallback));
                    }).catch(function () {
                        toastr.error(profileT('salon.profile.distribution.copy_failed', 'Kopyalanamadi'));
                    });
                    return;
                }
                toastr.info(text);
            };

            self.copyBookingLink = function () {
                self.copyText(self.bookingUrl(), 'salon.profile.distribution.booking_copied', 'Randevu linki kopyalandi');
            };

            self.copyWidgetCode = function () {
                self.copyText(self.widgetCode(), 'salon.profile.distribution.widget_copied', 'Widget kodu kopyalandi');
            };

            self.shareProfile = function () {
                var shareData = {
                    title: self.salon().salonName || document.title,
                    text: profileT('salon.profile.distribution.share_text', 'Online randevu linki'),
                    url: self.profileUrl()
                };
                if (navigator.share) {
                    navigator.share(shareData).catch(function () {});
                    return;
                }
                self.copyText(self.profileUrl(), 'salon.profile.distribution.profile_copied', 'Profil linki kopyalandi');
            };

            // Signup
            self.signupPlanId = ko.observable(null);
            self.signupPlanName = ko.observable('');
            self.signupPlanPrice = ko.observable(0);
            self.isSubmitting = ko.observable(false);
            self.membershipCheckoutActive = ko.observable(false);
            self.signupForm = {
                fullName: ko.observable(''),
                phone: ko.observable(''),
                email: ko.observable(''),
                cardName: ko.observable(''),
                cardNumber: ko.observable(''),
                expMonth: ko.observable(''),
                expYear: ko.observable(''),
                cvc: ko.observable('')
            };

            self.getPlanPrice = function (plan) {
                if (!plan) return 0;
                var value = plan.monthlyPrice != null ? plan.monthlyPrice : plan.price;
                return Number(value || 0);
            };

            self.formatCardNumber = function (data, event) {
                var val = self.signupForm.cardNumber().replace(/\D/g, '');
                if (val.length > 16) val = val.substring(0, 16);
                var formatted = val.replace(/(\d{4})(?=\d)/g, '$1 ');
                self.signupForm.cardNumber(formatted);
                return true;
            };

            self.normalizePublicPhone = function (raw) {
                if (typeof normalizePhone === 'function' && typeof formatFullPhone === 'function') {
                    var parsed = normalizePhone(raw || '', '+90');
                    return formatFullPhone(parsed.countryCode, parsed.national);
                }

                var digits = String(raw || '').replace(/\D/g, '');
                if (digits.startsWith('00')) digits = digits.substring(2);
                if (digits.startsWith('90')) return '+' + digits;
                if (digits.startsWith('0')) digits = digits.substring(1);
                return digits ? '+90' + digits : '';
            };

            var signupModal;

            // Bolum siralama
            var defaultOrder = ['banners', 'gallery', 'services', 'memberships', 'team', 'reviews', 'map'];
            self.orderedSections = ko.computed(function () {
                var s = self.salon();
                if (!s || !s.slug) return [];
                var order = defaultOrder.slice();
                if (s.sectionOrderJson) {
                    try {
                        var savedOrder = JSON.parse(s.sectionOrderJson);
                        if (Array.isArray(savedOrder)) {
                            order = savedOrder.filter(function (key) { return defaultOrder.indexOf(key) >= 0; });
                            defaultOrder.forEach(function (key) {
                                if (order.indexOf(key) < 0) order.push(key);
                            });
                        }
                    } catch (e) {}
                }
                return order.map(function (key) { return { key: key }; });
            });

            var dayNames = {
                mon: profileT('salon.profile.weekday.mon', 'Pazartesi'),
                tue: profileT('salon.profile.weekday.tue', 'Salı'),
                wed: profileT('salon.profile.weekday.wed', 'Çarşamba'),
                thu: profileT('salon.profile.weekday.thu', 'Perşembe'),
                fri: profileT('salon.profile.weekday.fri', 'Cuma'),
                sat: profileT('salon.profile.weekday.sat', 'Cumartesi'),
                sun: profileT('salon.profile.weekday.sun', 'Pazar')
            };
            var dayOrder = ['mon', 'tue', 'wed', 'thu', 'fri', 'sat', 'sun'];
            var closedText = profileT('salon.profile.hours.closed', 'Kapalı');
            var todayIdx = (new Date().getDay() + 6) % 7;

            fetch('/proxy/salon/' + salonSlug)
                .then(function (r) { if (!r.ok) throw new Error(); return r.json(); })
                .then(function (data) {
                    self.salon(data);
                    document.title = data.salonName + ' | CorpLynk Salon';

                    if (data.workingHoursJson) {
                        try {
                            var h = JSON.parse(data.workingHoursJson);
                            var list = [];
                            dayOrder.forEach(function (key, idx) {
                                var val = h[key] || 'closed';
                                list.push({ day: dayNames[key], hours: val === 'closed' ? closedText : val, isToday: idx === todayIdx });
                            });
                            self.workingHours(list);
                        } catch (e) {}
                    }

                    // Bannerlari parse et
                    if (data.bannersJson) {
                        try {
                            var banners = JSON.parse(data.bannersJson);
                            self.banners(banners.filter(function (b) { return b.url; }));
                        } catch (e) {}
                    }

                    if (data.galleryImagesJson) {
                        try {
                            var gallery = JSON.parse(data.galleryImagesJson);
                            self.galleryImages((Array.isArray(gallery) ? gallery : []).map(function (item) {
                                return typeof item === 'string' ? item : (item && item.url);
                            }).filter(Boolean));
                        } catch (e) {}
                    }

                    // Uyelik planlarini yukle
                    if (data.showMemberships !== false) {
                        fetch('/proxy/salon/' + salonSlug + '/memberships')
                            .then(function (r) { return r.ok ? r.json() : []; })
                            .then(function (plans) { self.membershipPlans(plans); });
                    }

                    // Ekibi yukle
                    if (data.showTeam !== false) {
                        fetch('/proxy/salon/' + salonSlug + '/team')
                            .then(function (r) { return r.ok ? r.json() : []; })
                            .then(function (team) { self.teamMembers(team); });
                    }

                    // Yorumlari yukle
                    if (data.showReviews !== false) {
                        fetch('/proxy/salon/' + salonSlug + '/reviews')
                            .then(function (r) { return r.ok ? r.json() : { reviews: [], stats: {} }; })
                            .then(function (d) {
                                self.reviews(d.reviews || []);
                                self.reviewStats(d.stats || {});
                            });
                    }

                    // Harita
                    if (data.showMap !== false && data.latitude && data.longitude) {
                        setTimeout(function () {
                            var mapEl = document.getElementById('salonMap');
                            if (mapEl && typeof L !== 'undefined') {
                                var map = L.map('salonMap').setView([data.latitude, data.longitude], 15);
                                L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
                                    attribution: '&copy; OpenStreetMap'
                                }).addTo(map);
                                L.marker([data.latitude, data.longitude])
                                    .addTo(map)
                                    .bindPopup('<b>' + data.salonName + '</b><br>' + (data.address || ''))
                                    .openPopup();
                            }
                        }, 500);
                    }

                    self.loaded(true);
                })
                .catch(function () { self.notFound(true); });

            // ═══ Bekleme Listesi (anonim) ═══
            var waitlistModal;
            self.waitlistSaving = ko.observable(false);
            self.waitlist = {
                fullName: ko.observable(''),
                phone: ko.observable(''),
                serviceId: ko.observable(''),
                date: ko.observable(new Date().toISOString().substring(0, 10)),
                timeSlot: ko.observable('Farketmez'),
                notes: ko.observable('')
            };
            self.allFlatServices = ko.computed(function () {
                var list = [];
                var s = self.salon();
                if (s && s.serviceCategories) {
                    s.serviceCategories.forEach(function (cat) {
                        (cat.services || []).forEach(function (svc) { list.push({ id: svc.id, name: svc.name }); });
                    });
                }
                return list;
            });

            self.openWaitlist = function () {
                self.waitlist.fullName('');
                self.waitlist.phone('');
                self.waitlist.serviceId('');
                self.waitlist.date(new Date().toISOString().substring(0, 10));
                self.waitlist.timeSlot('Farketmez');
                self.waitlist.notes('');
                if (!waitlistModal) waitlistModal = new bootstrap.Modal(document.getElementById('waitlistModal'));
                waitlistModal.show();
            };

            self.submitWaitlist = function () {
                var normalizedWaitlistPhone = self.normalizePublicPhone(self.waitlist.phone());
                if (!self.waitlist.fullName() || !normalizedWaitlistPhone) {
                    toastr.warning(profileT('salon.book.waitlist.required', 'Ad ve telefon zorunlu')); return;
                }
                if (!self.waitlist.serviceId()) {
                    toastr.warning(profileT('salon.profile.waitlist.service_required', 'Hizmet seçiniz')); return;
                }
                self.waitlistSaving(true);
                var payload = {
                    fullName: self.waitlist.fullName(),
                    phone: normalizedWaitlistPhone,
                    serviceId: parseInt(self.waitlist.serviceId()),
                    preferredDate: self.waitlist.date() + 'T00:00:00Z',
                    preferredTimeSlot: self.waitlist.timeSlot(),
                    notes: self.waitlist.notes() || null
                };
                fetch('/proxy/salon/' + self.salon().slug + '/waitlist', {
                    method: 'POST', headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify(payload)
                }).then(function (r) { return r.json().then(function (j) { return { ok: r.ok, body: j }; }); })
                  .then(function (res) {
                      self.waitlistSaving(false);
                      if (res.ok) {
                          toastr.success(res.body.message || profileT('salon.book.waitlist.success', 'Bekleme listesine eklendiniz.'));
                          if (waitlistModal) waitlistModal.hide();
                      } else {
                          toastr.error((res.body && res.body.message) || profileT('salon.book.waitlist.save_failed', 'Kayıt başarısız'));
                      }
                  })
                  .catch(function () {
                      self.waitlistSaving(false);
                      toastr.error(profileT('salon.book.error.connection', 'Bağlantı hatası'));
                  });
            };

            self.openSignup = function (planId, planName) {
                self.signupPlanId(planId);
                self.signupPlanName(planName);
                // Plan fiyatini bul
                var plan = self.membershipPlans().find(function (p) { return p.id === planId; });
                self.signupPlanPrice(self.getPlanPrice(plan));
                self.membershipCheckoutActive(false);
                self.signupForm.fullName('');
                self.signupForm.phone('');
                self.signupForm.email('');
                self.signupForm.cardName('');
                self.signupForm.cardNumber('');
                self.signupForm.expMonth('');
                self.signupForm.expYear('');
                self.signupForm.cvc('');
                var checkoutContainer = document.getElementById('membership-checkout-container');
                if (checkoutContainer) {
                    checkoutContainer.style.display = 'none';
                    checkoutContainer.innerHTML = '';
                }
                if (!signupModal) signupModal = new bootstrap.Modal(document.getElementById('signupModal'));
                signupModal.show();
            };

            self.submitSignup = function () {
                var name = self.signupForm.fullName();
                var phone = self.normalizePublicPhone(self.signupForm.phone());
                if (!name || !phone) { toastr.warning(profileT('salon.profile.membership.required', 'Ad ve telefon zorunludur.')); return; }

                var price = self.signupPlanPrice();
                if (price > 0) {
                    var token = getStoredPlatformToken();
                    if (!token) {
                        toastr.warning(profileT('salon.profile.membership.login_required', 'Ücretli salon üyeliği için müşteri girişi gereklidir.'));
                        var returnUrl = window.location.pathname + window.location.search + '#section-memberships';
                        window.location.href = '/user/login?returnUrl=' + encodeURIComponent(returnUrl);
                        return;
                    }
                }

                self.isSubmitting(true);

                // Adim 1: Uyelik basvurusu
                fetch('/proxy/salon/' + salonSlug + '/membership-signup', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({
                        planId: self.signupPlanId(),
                        fullName: name,
                        phone: phone,
                        email: self.signupForm.email() || null
                    })
                })
                .then(function (r) {
                    return r.text().then(function (text) {
                        var data = {};
                        try { data = text ? JSON.parse(text) : {}; } catch (e) {}
                        return { ok: r.ok, status: r.status, data: data };
                    });
                })
                .then(function (res) {
                    if (!res.ok || !res.data.success) {
                        self.isSubmitting(false);
                        toastr.error(res.data.message || res.data || profileT('salon.common.error.generic', 'Bir hata oluştu'));
                        return;
                    }

                    // Ucretli plan ise guvenli checkout formunu baslat
                    if (price > 0 && res.data.slnClientId) {
                        var token = getStoredPlatformToken();
                        fetch('/public-proxy/payments/membership-checkout', {
                            method: 'POST',
                            headers: { 'Content-Type': 'application/json', 'Authorization': 'Bearer ' + token },
                            body: JSON.stringify({
                                planId: self.signupPlanId(),
                                slnClientId: res.data.slnClientId,
                                slug: salonSlug
                            })
                        })
                        .then(function (r2) { return r2.json().then(function (d2) { return { ok: r2.ok, data: d2 }; }); })
                        .then(function (payRes) {
                            self.isSubmitting(false);
                            if (payRes.ok && payRes.data.success) {
                                var container = document.getElementById('membership-checkout-container');
                                self.membershipCheckoutActive(true);
                                if (container) {
                                    container.style.display = '';
                                    renderIyzicoCheckoutHtml(container, payRes.data.htmlContent || payRes.data.checkoutFormHtml || '');
                                }
                                toastr.info(profileT('salon.profile.membership.checkout_opened', 'Güvenli ödeme formu açıldı. Ödeme tamamlanınca üyeliğiniz aktif edilir.'));
                            } else {
                                var payError = payRes.data ? (payRes.data.error || payRes.data.message) : null;
                                toastr.error(profileT('salon.profile.membership.payment_form_failed', 'Ödeme formu başlatılamadı:') + ' ' + (payError || profileT('salon.profile.error.unknown', 'Bilinmeyen hata')));
                            }
                        })
                        .catch(function () { self.isSubmitting(false); toastr.error(profileT('salon.profile.membership.payment_connection_error', 'Ödeme bağlantı hatası.')); });
                    } else {
                        self.isSubmitting(false);
                        signupModal.hide();
                        toastr.success(res.data.message || profileT('salon.profile.membership.signup_success', 'Üyelik başvurunuz alındı!'));
                    }
                })
                .catch(function () { self.isSubmitting(false); toastr.error(profileT('salon.book.error.connection', 'Bağlantı hatası')); });
            };

            window.addEventListener('message', function (event) {
                if (!event.data || !self.membershipCheckoutActive()) return;
                if (event.data.type === 'payment-success') {
                    self.membershipCheckoutActive(false);
                    self.isSubmitting(false);
                    if (signupModal) signupModal.hide();
                    toastr.success(profileT('salon.profile.membership.payment_success', 'Ödeme başarılı. Salon üyeliğiniz aktif edildi.'));
                }
                if (event.data.type === 'payment-failed') {
                    self.isSubmitting(false);
                    toastr.error(event.data.error || profileT('salon.book.payment.failed', 'Ödeme başarısız oldu. Lütfen tekrar deneyin.'));
                }
            });
        }

        var publicSalonVm = new PublicSalonViewModel();
        var publicSalonRoot = document.getElementById('public-salon-vm');
        ko.applyBindings(publicSalonVm, publicSalonRoot);

        function bindStandaloneModal(id) {
            var el = document.getElementById(id);
            if (!el || !publicSalonRoot || publicSalonRoot.contains(el)) return;
            if (ko.dataFor(el)) return;
            ko.applyBindings(publicSalonVm, el);
        }

        bindStandaloneModal('waitlistModal');
        bindStandaloneModal('signupModal');

        function setJoinAreaDisplay(id, value) {
            var el = document.getElementById(id);
            if (el) el.style.display = value;
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

        (function () {
            var qs = new URLSearchParams(window.location.search);
            if (qs.get('paid') === 'true') toastr.success(profileT('salon.profile.membership.payment_success', 'Ödeme başarılı. Salon üyeliğiniz aktif edildi.'));
            var payerr = qs.get('payerr');
            if (payerr) toastr.error(payerr);
        })();

        // ═══ Platform User: Salon Müşterisi Ol ═══
        (function () {
            var token = getStoredPlatformToken();
            var salonSlug = profileSlug;

            if (!token) {
                var loginJoin = document.getElementById('btnLoginToJoin');
                if (loginJoin) loginJoin.href = '/user/login?returnUrl=' + encodeURIComponent(window.location.pathname + window.location.search);
                setJoinAreaDisplay('btnLoginToJoin', '');
                return;
            }

            // Login olmuş — bu salona üye mi kontrol et
            fetch('/public-proxy/platform/salons', { headers: { 'Authorization': 'Bearer ' + token } })
                .then(function (r) {
                    if (r.status === 401 || r.status === 403) {
                        localStorage.removeItem('platformToken');
                        localStorage.removeItem('platformUser');
                        var loginJoin = document.getElementById('btnLoginToJoin');
                        if (loginJoin) loginJoin.href = '/user/login?returnUrl=' + encodeURIComponent(window.location.pathname + window.location.search);
                        setJoinAreaDisplay('btnLoginToJoin', '');
                        return null;
                    }
                    return r.ok ? r.json() : [];
                })
                .then(function (salons) {
                    if (!Array.isArray(salons)) return;
                    // customerId'yi salon profil verisinden al
                    var vm = ko.dataFor(document.getElementById('public-salon-vm'));
                    var profileData = vm && vm.salon ? vm.salon() : null;
                    var customerId = profileData && profileData.customerId;

                    if (!customerId) {
                        // customerId'yi DOM'dan alamadıysak slug ile proxy'den alalım
                        setJoinAreaDisplay('btnJoinSalon', '');
                        return;
                    }

                    var isMember = salons.some(function (s) { return s.customerId === customerId; });
                    if (isMember) {
                        setJoinAreaDisplay('alreadyMember', '');
                    } else {
                        setJoinAreaDisplay('btnJoinSalon', '');
                    }
                })
                .catch(function () {
                    setJoinAreaDisplay('btnJoinSalon', '');
                });
        })();

        function joinThisSalon() {
            var token = getStoredPlatformToken();
            if (!token) { window.location.href = '/user/login?returnUrl=' + encodeURIComponent(window.location.pathname + window.location.search); return; }

            // customerId çek: önce KO binding'den, fail olursa fetch ile salon endpoint'inden
            var vm = ko.dataFor(document.getElementById('public-salon-vm'));
            var customerId = vm && vm.salon && vm.salon() ? vm.salon().customerId : null;

            var doJoin = function (cid) {
                if (!cid) { toastr.error(profileT('salon.profile.join.salon_missing', 'Salon bilgisi alınamadı.')); return; }
                var btn = document.getElementById('btnJoinSalon');
                if (btn) btn.disabled = true;

                fetch('/public-proxy/platform/salons/join', {
                    method: 'POST',
                    headers: { 'Authorization': 'Bearer ' + token, 'Content-Type': 'application/json' },
                    body: JSON.stringify({ customerId: cid })
                })
                .then(function (r) {
                    return r.text().then(function (text) {
                        var data = {};
                        try { data = text ? JSON.parse(text) : {}; } catch (e) {}
                        return { ok: r.ok, status: r.status, data: data };
                    });
                })
                .then(function (res) {
                    if (res.ok) {
                        toastr.success(profileT('salon.profile.join.success', 'Salona üye oldunuz!'));
                        setJoinAreaDisplay('btnJoinSalon', 'none');
                        setJoinAreaDisplay('alreadyMember', '');
                    } else {
                        if (res.status === 401 || res.status === 403) {
                            localStorage.removeItem('platformToken');
                            localStorage.removeItem('platformUser');
                            window.location.href = '/user/login?returnUrl=' + encodeURIComponent(window.location.pathname + window.location.search);
                            return;
                        }
                        if (btn) btn.disabled = false;
                        toastr.error(res.data && res.data.message ? res.data.message : profileT('salon.profile.join.failed', 'İşlem başarısız.'));
                    }
                })
                .catch(function () {
                    if (btn) btn.disabled = false;
                    toastr.error(profileT('salon.book.error.connection', 'Bağlantı hatası'));
                });
            };

            if (customerId) {
                doJoin(customerId);
            } else {
                // Fallback: slug üzerinden public proxy ile salon profilini çek
                fetch('/public-proxy/salon/' + encodeURIComponent(profileSlug))
                    .then(function (r) { return r.ok ? r.json() : null; })
                    .then(function (data) { doJoin(data && data.customerId); })
                    .catch(function () { doJoin(null); });
            }
        }

    var joinSalonButton = document.getElementById('btnJoinSalon');
    if (joinSalonButton) {
        joinSalonButton.addEventListener('click', function (event) {
            event.preventDefault();
            joinThisSalon();
        });
    }
})();
