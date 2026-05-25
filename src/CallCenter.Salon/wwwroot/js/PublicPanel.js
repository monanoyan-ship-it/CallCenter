(function () {
        var panelData = document.getElementById('panel-data');
        var PANEL_LANG = panelData ? panelData.getAttribute('data-lang') : 'tr';
        try { window.salonTranslations = JSON.parse((panelData && panelData.getAttribute('data-translations')) || '{}'); } catch (error) { window.salonTranslations = {}; }
        window.salonT = function(key, fallback) {
            return (window.salonTranslations && window.salonTranslations[key]) || fallback || key;
        };

        function getStoredPlatformToken() {
            var token = localStorage.getItem('platformToken');
            if (!token || token === 'null' || token === 'undefined') {
                localStorage.removeItem('platformToken');
                localStorage.removeItem('platformUser');
                return '';
            }
            return token;
        }

        var TOKEN = getStoredPlatformToken();
        if (!TOKEN) window.location.href = '/user/login';

        var user = JSON.parse(localStorage.getItem('platformUser') || '{}');
        document.getElementById('userName').textContent = user.fullName || '';
        var salonByCustomerId = {};
        var myReviewsByCustomerId = {};
        var myReviewsByKey = {};
        var showingPastAppointments = false;

        function escapeHtml(value) {
            return String(value == null ? '' : value).replace(/[&<>"']/g, function(ch) {
                return ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' })[ch];
            });
        }

        function escapeAttr(value) {
            return escapeHtml(value).replace(/`/g, '&#96;');
        }

        function reviewLookupKey(customerId, salonSlug) {
            return String(customerId || 0) + '|' + String(salonSlug || '').trim().toLowerCase();
        }

        function findExistingReview(customerId, salonSlug) {
            if (salonSlug) {
                return myReviewsByKey[reviewLookupKey(customerId, salonSlug)] || null;
            }
            return myReviewsByCustomerId[customerId] || null;
        }

        function parseJsonSafe(response) {
            return response.text().then(function (text) {
                var body = text == null ? '' : String(text).trim();
                if (!body) return null;
                try {
                    return JSON.parse(body);
                } catch (error) {
                    return null;
                }
            });
        }

        function api(path, opts) {
            opts = opts || {};
            opts.headers = Object.assign({ 'Authorization': 'Bearer ' + TOKEN, 'Content-Type': 'application/json' }, opts.headers || {});
            return fetch('/public-proxy/' + path, opts).then(function(r) {
                if (r.status === 401) { logout(); return; }
                return parseJsonSafe(r);
            });
        }

        function logout() {
            localStorage.removeItem('platformToken');
            localStorage.removeItem('platformUser');
            window.location.href = '/user/login';
        }

        // --- Salonlarım ---
        function loadSalons() {
            api('platform/salons').then(function(data) {
                var el = document.getElementById('salonList');
                salonByCustomerId = {};
                if (!data || data.length === 0) { el.innerHTML = ''; document.getElementById('noSalons').style.display = ''; return; }
                data.forEach(function(s) {
                    if (s && s.customerId) salonByCustomerId[s.customerId] = s;
                });
                document.getElementById('noSalons').style.display = 'none';
                el.innerHTML = data.map(function(s) {
                    var slug = s.slug || s.customerId;
                    return '<div class="col-md-4"><div class="card section-card h-100"><div class="card-body text-center">' +
                        (s.logoUrl ? '<img src="' + s.logoUrl + '" style="max-height:60px;" class="mb-2" />' : '<i class="bi bi-shop fs-1 text-muted mb-2 d-block"></i>') +
                        '<h6>' + s.salonName + '</h6>' +
                        '<small class="text-muted">' + (s.city || '') + ' ' + (s.district || '') + '</small>' +
                        '<div class="mt-2"><button class="btn btn-sm ' + (s.isFavorite ? 'btn-warning' : 'btn-outline-warning') + '" data-panel-action="toggle-fav" data-customer-id="' + s.customerId + '"><i class="bi bi-star' + (s.isFavorite ? '-fill' : '') + '"></i></button>' +
                        ' <a href="/salon/' + slug + '/book" class="btn btn-sm btn-outline-primary"><i class="bi bi-calendar-plus me-1"></i>' + salonT('salon.panel.action.appointment', 'Randevu') + '</a>' +
                        ' <button class="btn btn-sm btn-outline-danger" data-panel-action="open-health" data-customer-id="' + s.customerId + '"><i class="bi bi-heart-pulse me-1"></i>' + salonT('salon.panel.health.button', 'Sağlık') + '</button></div>' +
                        '</div></div></div>';
                }).join('');
            });
        }

        function toggleFav(customerId) {
            api('platform/salons/' + customerId + '/favorite', { method: 'POST' }).then(loadSalons);
        }

        var _healthModal;
        var activeHealthCustomerId = null;

        function openHealth(customerId) {
            activeHealthCustomerId = customerId;
            api('platform/salons/' + customerId + '/health').then(function(data) {
                if (!data) {
                    showToast(salonT('salon.panel.health.load_failed', 'Sağlık bilgileri yüklenemedi.'), false);
                    return;
                }
                document.getElementById('healthSalonName').textContent = data.salonName || '';
                document.getElementById('healthSkinType').value = data.skinType || '';
                document.getElementById('healthSkinSensitivity').value = data.skinSensitivity || '';
                document.getElementById('healthAllergies').value = data.allergies || '';
                document.getElementById('healthContraindications').value = data.contraindications || '';
                document.getElementById('healthMedicalNotes').value = data.medicalNotes || '';
                if (!_healthModal) _healthModal = new bootstrap.Modal(document.getElementById('healthModal'));
                _healthModal.show();
            });
        }

        function saveHealth() {
            if (!activeHealthCustomerId) return;
            api('platform/salons/' + activeHealthCustomerId + '/health', {
                method: 'PUT',
                body: JSON.stringify({
                    skinType: document.getElementById('healthSkinType').value || null,
                    skinSensitivity: document.getElementById('healthSkinSensitivity').value || null,
                    allergies: document.getElementById('healthAllergies').value || null,
                    contraindications: document.getElementById('healthContraindications').value || null,
                    medicalNotes: document.getElementById('healthMedicalNotes').value || null
                })
            }).then(function(data) {
                if (_healthModal) _healthModal.hide();
                showToast((data && data.message) || salonT('salon.panel.health.saved', 'Sağlık bilgileriniz salona iletildi.'), true);
            });
        }

        // --- Randevularım ---
        function apptStatusBadge(s) {
            if (s === 1) return '<span class="badge bg-secondary ms-1">' + salonT('salon.panel.status.pending', 'Bekliyor') + '</span>';
            if (s === 2) return '<span class="badge bg-success ms-1">' + salonT('salon.panel.status.approved', 'Onaylandı') + '</span>';
            if (s === 3) return '<span class="badge bg-info text-dark ms-1">' + salonT('salon.panel.status.completed', 'Tamamlandı') + '</span>';
            if (s === 4) return '<span class="badge bg-danger ms-1">' + salonT('salon.panel.status.cancelled', 'İptal') + '</span>';
            if (s === 5) return '<span class="badge bg-warning text-dark ms-1">' + salonT('salon.panel.status.no_show', 'Gelmedi') + '</span>';
            if (s === 6) return '<span class="badge bg-warning text-dark ms-1">' + salonT('salon.panel.status.payment_pending', 'Ödeme Bekleniyor') + '</span>';
            return '';
        }

        function loadAppointments(past) {
            showingPastAppointments = !!past;
            api('platform/appointments?past=' + (past || false)).then(function(data) {
                var el = document.getElementById('appointmentList');
                if (!data || data.length === 0) { el.innerHTML = ''; document.getElementById('noAppts').style.display = ''; return; }
                document.getElementById('noAppts').style.display = 'none';
                el.innerHTML = data.map(function(a) {
                    var dateOnly = a.appointmentDate ? a.appointmentDate.substring(0, 10) : '';
                    var date = dateOnly ? new Date(dateOnly).toLocaleDateString(PANEL_LANG || undefined) : '';
                    var time = a.startTime ? a.startTime.substring(0, 5) : '';
                    var canCancel = a.statusId === 1 || a.statusId === 2;
                    var canPay = !!a.canPay;
                    var customerId = a.customerId || 0;
                    var salonSlug = a.salonSlug || (salonByCustomerId[customerId] && salonByCustomerId[customerId].slug) || '';
                    var existingReview = customerId ? findExistingReview(customerId, salonSlug) : null;
                    var reviewLabel = existingReview
                        ? salonT('salon.panel.review.update_button', 'Yorumu Güncelle')
                        : salonT('salon.panel.review.write_button', 'Yorum Yaz');
                    var payLabel = a.statusId === 6
                        ? salonT('salon.panel.appointments.resume_payment', 'Ödemeye Devam Et')
                        : salonT('salon.panel.appointments.pay_now', 'Öde');
                    var apptIso = dateOnly ? new Date(dateOnly + 'T' + (a.startTime || '00:00:00')).toISOString() : '';
                    var cancelBtn = canCancel && apptIso
                        ? '<button class="btn btn-sm btn-outline-danger" data-panel-action="cancel-appt" data-id="' + a.id + '" data-is-prepaid="' + (a.isPrepaid ? 'true' : 'false') + '" data-prepaid-amount="' + (a.prepaidAmount || 0) + '" data-start-time="' + apptIso + '"><i class="bi bi-x-lg"></i></button>'
                        : '';
                    var payBtn = canPay
                        ? '<button class="btn btn-sm btn-primary" data-panel-action="appointment-payment" data-id="' + a.id + '"><i class="bi bi-credit-card me-1"></i>' + payLabel + (a.remainingAmount > 0 ? ' (' + formatMoney(a.remainingAmount) + ')' : '') + '</button>'
                        : '';
                    var reviewBtn = a.statusId === 3 && salonSlug
                        ? '<button class="btn btn-sm btn-outline-warning" data-panel-action="open-review" data-customer-id="' + customerId + '" data-slug="' + escapeAttr(salonSlug) + '" data-salon-name="' + escapeAttr(a.salonName || '') + '"><i class="bi bi-star me-1"></i>' + reviewLabel + '</button>'
                        : '';
                    return '<div class="card section-card mb-2"><div class="card-body d-flex justify-content-between align-items-center py-2">' +
                        '<div><strong>' + a.salonName + '</strong>' + apptStatusBadge(a.statusId) +
                        '<div class="small text-muted">' + date + ' ' + time + '</div>' +
                        '<div class="small">' + (a.serviceNames || []).join(', ') + '</div></div>' +
                        '<div class="d-flex gap-2 flex-wrap justify-content-end">' + payBtn + reviewBtn + cancelBtn + '</div>' +
                        '</div></div>';
                }).join('');
            });
        }

        var _cancelModal, _cancelToast;
        var _reviewModal;
        var pendingCancelAppointmentId = null;
        var pendingReview = null;
        var appointmentPaymentBusy = false;

        function formatMoney(amount) {
            var value = Number(amount || 0);
            return value.toLocaleString(PANEL_LANG || undefined, { minimumFractionDigits: value % 1 === 0 ? 0 : 2, maximumFractionDigits: 2 }) + ' TL';
        }

        function activateAppointmentsTab() {
            var trigger = document.querySelector('a[href="#tabAppointments"]');
            if (!trigger) return;
            bootstrap.Tab.getOrCreateInstance(trigger).show();
        }

        function handleAppointmentPaymentResult(success, error) {
            activateAppointmentsTab();
            if (success) {
                showToast(salonT('salon.panel.payment.completed', 'Ödeme tamamlandı.'), true);
            } else {
                showToast(error || salonT('salon.panel.payment.failed', 'Ödeme tamamlanamadı. Lütfen tekrar deneyin.'), false);
            }
            loadAppointments(false);
            loadPaymentHistory();
        }

        function openAppointmentPayment(id) {
            if (appointmentPaymentBusy) return;
            appointmentPaymentBusy = true;

            fetch('/public-proxy/platform/appointments/' + id + '/pay-checkout', {
                method: 'POST',
                headers: { 'Authorization': 'Bearer ' + TOKEN, 'Content-Type': 'application/json' },
                body: '{}'
            })
            .then(function(r) {
                if (r.status === 401) { logout(); return Promise.reject(); }
                return r.json().then(function(data) {
                    return { ok: r.ok, data: data || {} };
                }, function() {
                    return { ok: r.ok, data: {} };
                });
            })
            .then(function(resp) {
                appointmentPaymentBusy = false;
                if (!resp.ok) {
                    showToast(resp.data.error || resp.data.message || salonT('salon.panel.payment.failed', 'Ödeme tamamlanamadı. Lütfen tekrar deneyin.'), false);
                    return;
                }

                if (!resp.data.htmlContent) {
                    showToast(salonT('salon.panel.payment.failed', 'Ödeme tamamlanamadı. Lütfen tekrar deneyin.'), false);
                    return;
                }

                var paymentWindow = window.open('', 'corplynkAppointmentPayment', 'width=520,height=860');
                if (!paymentWindow) {
                    showToast(salonT('salon.panel.payment.popup_blocked', 'Ödeme penceresi açılamadı. Tarayıcı açılır pencere izni verin.'), false);
                    return;
                }

                paymentWindow.document.open();
                paymentWindow.document.write(resp.data.htmlContent);
                paymentWindow.document.close();
                paymentWindow.focus();
            })
            .catch(function() {
                appointmentPaymentBusy = false;
                showToast(salonT('salon.panel.error.request_failed', 'İstek başarısız.'), false);
            });
        }

        function showToast(msg, success) {
            var el = document.getElementById('appToast');
            el.className = 'toast align-items-center border-0 text-bg-' + (success === false ? 'danger' : 'success');
            document.getElementById('appToastMsg').textContent = msg;
            if (!_cancelToast) _cancelToast = new bootstrap.Toast(el, { delay: 4000 });
            _cancelToast.show();
        }

        function cancelAppt(id, isPrepaid, prepaidAmount, startTime) {
            var msg = salonT('salon.panel.cancel.question', 'Randevunuzu iptal etmek istediğinize emin misiniz?');
            if (isPrepaid && prepaidAmount > 0) {
                var hoursLeft = (new Date(startTime) - new Date()) / 3600000;
                if (hoursLeft >= 24) {
                    msg += '<br><br><span class="text-success fw-semibold">' + salonT('salon.panel.cancel.refund_yes', 'Ücretsiz iptal süresi içindesiniz; depozitonuz iade edilir.') + '</span>';
                } else {
                    msg += '<br><br><span class="text-danger fw-semibold">' + salonT('salon.panel.cancel.refund_maybe_no', 'Ücretsiz iptal süresi geçmiş olabilir; depozito iadesi yapılamayabilir.') + '</span>';
                }
            }
            document.getElementById('cancelModalMsg').innerHTML = msg;
            if (!_cancelModal) _cancelModal = new bootstrap.Modal(document.getElementById('cancelModal'));
            pendingCancelAppointmentId = id;
            _cancelModal.show();
        }

        function loadMyReviews() {
            return api('platform/reviews/me').then(function(data) {
                myReviewsByCustomerId = {};
                myReviewsByKey = {};
                (data || []).forEach(function(review) {
                    if (review && review.customerId) {
                        if (!myReviewsByCustomerId[review.customerId]) {
                            myReviewsByCustomerId[review.customerId] = review;
                        }
                        if (review.salonSlug) {
                            myReviewsByKey[reviewLookupKey(review.customerId, review.salonSlug)] = review;
                        }
                    }
                });
                return data || [];
            });
        }

        function openReview(customerId, salonSlug, salonName) {
            if (!customerId || !salonSlug) {
                showToast(salonT('salon.panel.review.slug_missing', 'Salon bağlantısı bulunamadı.'), false);
                return;
            }

            pendingReview = {
                customerId: customerId,
                salonSlug: salonSlug
            };

            var existingReview = findExistingReview(customerId, salonSlug);
            document.getElementById('reviewSalonName').textContent = salonName || '';
            document.getElementById('reviewRating').value = existingReview && existingReview.rating ? existingReview.rating : '5';
            document.getElementById('reviewComment').value = existingReview && existingReview.comment ? existingReview.comment : '';

            if (!_reviewModal) _reviewModal = new bootstrap.Modal(document.getElementById('reviewModal'));
            _reviewModal.show();
        }

        function submitReview() {
            if (!pendingReview) return;

            var rating = parseInt(document.getElementById('reviewRating').value, 10);
            if (rating < 1 || rating > 5) {
                showToast(salonT('salon.panel.review.rating_required', 'Puan seçin.'), false);
                return;
            }

            api('platform/reviews', {
                method: 'POST',
                body: JSON.stringify({
                    salonSlug: pendingReview.salonSlug,
                    rating: rating,
                    comment: document.getElementById('reviewComment').value || null,
                    displayName: user.fullName || null
                })
            }).then(function(data) {
                if (!data || data.message || data.error) {
                    showToast((data && (data.message || data.error)) || salonT('salon.panel.review.failed', 'Yorum kaydedilemedi.'), false);
                    return;
                }

                if (_reviewModal) _reviewModal.hide();
                pendingReview = null;
                showToast(salonT('salon.panel.review.saved', 'Yorumunuz onaya gönderildi.'), true);
                loadMyReviews().then(function() {
                    loadAppointments(showingPastAppointments);
                });
            });
        }

        // --- Sadakat ---
        function loadLoyalty() {
            api('platform/loyalty').then(function(data) {
                var el = document.getElementById('loyaltyList');
                if (!data || data.length === 0) { el.innerHTML = ''; document.getElementById('noLoyalty').style.display = ''; return; }
                document.getElementById('noLoyalty').style.display = 'none';
                el.innerHTML = data.map(function(l) {
                    var cards = (l.giftCards || []).map(function(g) {
                        return '<span class="badge bg-warning text-dark me-1">' + g.code + ': ' + g.remainingBalance + ' TL</span>';
                    }).join('');
                    return '<div class="card section-card mb-2"><div class="card-body">' +
                        '<h6>' + l.salonName + '</h6>' +
                        '<div class="row g-2">' +
                        '<div class="col-4"><div class="small text-muted">' + salonT('salon.panel.loyalty.points', 'Puan') + '</div><strong>' + l.currentPoints + '</strong></div>' +
                        '<div class="col-4"><div class="small text-muted">' + salonT('salon.panel.loyalty.membership', 'Üyelik') + '</div><strong>' + (l.membershipPlanName || '-') + '</strong></div>' +
                        '<div class="col-4"><div class="small text-muted">' + salonT('salon.panel.loyalty.discount', 'İndirim') + '</div><strong>' + (l.membershipDiscount ? '%' + l.membershipDiscount : '-') + '</strong></div>' +
                        '</div>' +
                        (cards ? '<div class="mt-2"><small class="text-muted">' + salonT('salon.panel.loyalty.gift_cards', 'Hediye Kartları') + ':</small> ' + cards + '</div>' : '') +
                        '</div></div>';
                }).join('');
            });
        }

        // --- Profil ---
        function loadProfile() {
            api('platform/me').then(function(data) {
                if (!data) return;
                document.getElementById('profName').value = data.fullName || '';
                document.getElementById('profEmail').value = data.email || '';
            });
        }

        function saveProfile() {
            api('platform/me', {
                method: 'PUT',
                body: JSON.stringify({
                    fullName: document.getElementById('profName').value,
                    email: document.getElementById('profEmail').value || null
                })
            }).then(function() { showToast(salonT('salon.panel.profile.saved', 'Profil güncellendi.'), true); });
        }

        function changePassword() {
            var cur = document.getElementById('pwdCurrent').value;
            var nw = document.getElementById('pwdNew').value;
            var nw2 = document.getElementById('pwdNew2').value;
            if (!cur || !nw || !nw2) { showToast(salonT('salon.panel.password.required', 'Tüm şifre alanlarını doldurun.'), false); return; }
            if (nw !== nw2) { showToast(salonT('salon.panel.password.mismatch', 'Yeni şifreler eşleşmiyor.'), false); return; }
            if (nw.length < 6) { showToast(salonT('salon.panel.password.min_length', 'Yeni şifre en az 6 karakter olmalıdır.'), false); return; }
            fetch('/public-proxy/platform/me/password', {
                method: 'PUT',
                headers: { 'Authorization': 'Bearer ' + TOKEN, 'Content-Type': 'application/json' },
                body: JSON.stringify({ currentPassword: cur, newPassword: nw })
            }).then(function(r) {
                return r.json().then(function(j) { return { ok: r.ok, status: r.status, j: j }; }, function() { return { ok: r.ok, status: r.status, j: null }; });
            }).then(function(x) {
                if (x.status === 401) { logout(); return; }
                if (x.ok) {
                    showToast((x.j && x.j.message) ? x.j.message : salonT('salon.panel.password.saved', 'Şifreniz güncellendi.'), true);
                    document.getElementById('pwdCurrent').value = '';
                    document.getElementById('pwdNew').value = '';
                    document.getElementById('pwdNew2').value = '';
                } else {
                    showToast((x.j && x.j.message) ? x.j.message : salonT('salon.panel.password.failed', 'Şifre değiştirilemedi.'), false);
                }
            }).catch(function() { showToast(salonT('salon.panel.error.request_failed', 'İstek başarısız.'), false); });
        }

        // Init
        loadSalons();
        loadMyReviews().then(function() {
            loadAppointments(false);
        }, function() {
            loadAppointments(false);
        });
        loadLoyalty();
        loadProfile();
        loadBilling();
        loadPaymentHistory();

        window.addEventListener('message', function(e) {
            if (e.data === 'payment-success' || (e.data && e.data.type === 'payment-success')) {
                handleAppointmentPaymentResult(true);
            } else if (e.data === 'payment-failed' || (e.data && e.data.type === 'payment-failed')) {
                handleAppointmentPaymentResult(false, e.data && e.data.error);
            }
        });

        (function handleIyzicoReturn() {
            var p = new URLSearchParams(window.location.search);
            var token = p.get('iyzicoToken');
            if (!token) return;

            var paid = p.get('paid') === 'true';
            var payerr = p.get('payerr');

            var u = new URL(window.location.href);
            u.searchParams.delete('iyzicoToken');
            u.searchParams.delete('paid');
            u.searchParams.delete('payerr');
            var q = u.searchParams.toString();
            window.history.replaceState({}, '', u.pathname + (q ? '?' + q : '') + u.hash);

            handleAppointmentPaymentResult(paid, payerr ? decodeURIComponent(payerr) : salonT('salon.panel.payment.not_completed', 'Ödeme tamamlanamadı. Lütfen tekrar deneyin.'));
        })();

        // --- Fatura Bilgileri ---
        function toggleBillingType() {
            var type = document.getElementById('billingType').value;
            document.getElementById('corporateFields').style.display = type === '2' ? '' : 'none';
        }

        function loadBilling() {
            api('platform/me').then(function(data) {
                if (!data) return;
                document.getElementById('billingType').value = data.billingType || 1;
                document.getElementById('billingFullName').value = data.billingFullName || data.fullName || '';
                document.getElementById('billingCompanyName').value = data.billingCompanyName || '';
                document.getElementById('billingTaxOffice').value = data.billingTaxOffice || '';
                document.getElementById('billingTaxNumber').value = data.billingTaxNumber || '';
                document.getElementById('billingAddress').value = data.billingAddress || '';
                document.getElementById('billingCity').value = data.billingCity || '';
                document.getElementById('billingDistrict').value = data.billingDistrict || '';
                document.getElementById('billingPostalCode').value = data.billingPostalCode || '';
                toggleBillingType();
            });
        }

        function saveBilling() {
            api('platform/billing-info', {
                method: 'PUT',
                body: JSON.stringify({
                    billingType: parseInt(document.getElementById('billingType').value),
                    billingFullName: document.getElementById('billingFullName').value || null,
                    billingCompanyName: document.getElementById('billingCompanyName').value || null,
                    billingTaxOffice: document.getElementById('billingTaxOffice').value || null,
                    billingTaxNumber: document.getElementById('billingTaxNumber').value || null,
                    billingAddress: document.getElementById('billingAddress').value || null,
                    billingCity: document.getElementById('billingCity').value || null,
                    billingDistrict: document.getElementById('billingDistrict').value || null,
                    billingPostalCode: document.getElementById('billingPostalCode').value || null
                })
            }).then(function() { showToast(salonT('salon.panel.billing.saved', 'Fatura bilgileri güncellendi.'), true); });
        }

        function loadPaymentHistory() {
            api('payments/history?page=1').then(function(data) {
                var el = document.getElementById('paymentHistoryList');
                var empty = document.getElementById('noPayments');
                if (!data || !data.length) {
                    if (el) el.innerHTML = '';
                    if (empty) empty.style.display = '';
                    return;
                }
                if (empty) empty.style.display = 'none';
                if (!el) return;
                el.innerHTML = '<div class="table-responsive"><table class="table table-sm table-hover align-middle mb-0"><thead class="table-light"><tr><th>' + salonT('salon.panel.payments.date', 'Tarih') + '</th><th>' + salonT('salon.panel.payments.type', 'Tür') + '</th><th>' + salonT('salon.panel.payments.business', 'İşletme') + '</th><th class="text-end">' + salonT('salon.panel.payments.amount', 'Tutar') + '</th><th>' + salonT('salon.panel.payments.status', 'Durum') + '</th><th></th></tr></thead><tbody>' +
                    data.map(function(p) {
                        var dt = p.completedAt || p.createdAt;
                        var dateStr = dt ? new Date(dt).toLocaleString(PANEL_LANG || undefined) : '-';
                        var canDl = p.statusId === 2 || p.statusId === 4;
                        var uid = p.uid || '';
                        var dl = canDl && uid
                            ? '<button type="button" class="btn btn-outline-secondary btn-sm py-0 px-2" data-panel-action="download-receipt" data-uid="' + uid + '"><i class="bi bi-download me-1"></i>' + salonT('salon.panel.payments.download', 'İndir') + '</button>'
                            : '<span class="text-muted small">-</span>';
                        var biz = p.customerName || '-';
                        var amt = p.amount != null ? Number(p.amount).toFixed(2) + ' ' + (p.currency || 'TRY') : '-';
                        return '<tr><td class="text-nowrap">' + dateStr + '</td><td>' + (p.paymentType || '') + '</td><td>' + biz + '</td><td class="text-end">' + amt + '</td><td><span class="small">' + (p.status || '') + '</span></td><td class="text-end">' + dl + '</td></tr>';
                    }).join('') + '</tbody></table></div>';
            });
        }

        function downloadReceipt(uid) {
            fetch('/public-proxy/payments/my-receipt/' + uid, { headers: { 'Authorization': 'Bearer ' + TOKEN } })
                .then(function(r) {
                    if (r.status === 401) { logout(); return Promise.reject(); }
                    if (!r.ok) {
                        return r.json().then(function(j) {
                            showToast((j && j.message) ? j.message : salonT('salon.panel.payments.receipt_failed', 'Dekont alınamadı.'), false);
                        }, function() { showToast(salonT('salon.panel.payments.receipt_failed', 'Dekont alınamadı.'), false); });
                    }
                    var fname = 'corpLynk-dekont.html';
                    var disp = r.headers.get('Content-Disposition');
                    if (disp) {
                        var utf8name = /filename\*=UTF-8''([^;\n]+)/i.exec(disp);
                        var quoted = /filename="([^"]+)"/i.exec(disp);
                        var simple = /filename=([^;\n]+)/i.exec(disp);
                        if (utf8name && utf8name[1]) fname = decodeURIComponent(utf8name[1].trim());
                        else if (quoted && quoted[1]) fname = quoted[1].trim();
                        else if (simple && simple[1]) fname = simple[1].replace(/"/g, '').trim();
                    }
                    return r.blob().then(function(blob) {
                        var url = URL.createObjectURL(blob);
                        var a = document.createElement('a');
                        a.href = url;
                        a.download = fname;
                        document.body.appendChild(a);
                        a.click();
                        a.remove();
                        URL.revokeObjectURL(url);
                        showToast(salonT('salon.panel.payments.receipt_downloaded', 'Dekont indirildi.'), true);
                    });
                })
                .catch(function() { });
        }

        var cancelConfirmButton = document.getElementById('cancelModalConfirmBtn');
        if (cancelConfirmButton) {
            cancelConfirmButton.addEventListener('click', function() {
                if (!pendingCancelAppointmentId) return;
                var id = pendingCancelAppointmentId;
                pendingCancelAppointmentId = null;
                if (_cancelModal) _cancelModal.hide();
                api('platform/appointments/' + id, { method: 'DELETE' })
                    .then(function(data) {
                        showToast(data && data.message ? data.message : salonT('salon.panel.cancel.success', 'Randevunuz iptal edildi.'), true);
                        loadAppointments(false);
                    })
                    .catch(function() { loadAppointments(false); });
            });
        }

        document.addEventListener('click', function(event) {
            var actionEl = event.target.closest('[data-panel-action]');
            if (!actionEl) return;
            var action = actionEl.getAttribute('data-panel-action');
            if (action === 'logout') {
                event.preventDefault();
                logout();
            } else if (action === 'load-appointments') {
                event.preventDefault();
                loadAppointments(actionEl.getAttribute('data-past') === 'true');
            } else if (action === 'save-profile') {
                event.preventDefault();
                saveProfile();
            } else if (action === 'change-password') {
                event.preventDefault();
                changePassword();
            } else if (action === 'save-billing') {
                event.preventDefault();
                saveBilling();
            } else if (action === 'toggle-fav') {
                event.preventDefault();
                toggleFav(parseInt(actionEl.getAttribute('data-customer-id'), 10));
            } else if (action === 'open-health') {
                event.preventDefault();
                openHealth(parseInt(actionEl.getAttribute('data-customer-id'), 10));
            } else if (action === 'save-health') {
                event.preventDefault();
                saveHealth();
            } else if (action === 'appointment-payment') {
                event.preventDefault();
                openAppointmentPayment(parseInt(actionEl.getAttribute('data-id'), 10));
            } else if (action === 'open-review') {
                event.preventDefault();
                openReview(
                    parseInt(actionEl.getAttribute('data-customer-id'), 10),
                    actionEl.getAttribute('data-slug') || '',
                    actionEl.getAttribute('data-salon-name') || ''
                );
            } else if (action === 'submit-review') {
                event.preventDefault();
                submitReview();
            } else if (action === 'cancel-appt') {
                event.preventDefault();
                cancelAppt(
                    parseInt(actionEl.getAttribute('data-id'), 10),
                    actionEl.getAttribute('data-is-prepaid') === 'true',
                    Number(actionEl.getAttribute('data-prepaid-amount') || 0),
                    actionEl.getAttribute('data-start-time') || ''
                );
            } else if (action === 'download-receipt') {
                event.preventDefault();
                downloadReceipt(actionEl.getAttribute('data-uid'));
            }
        });

        document.addEventListener('change', function(event) {
            var actionEl = event.target.closest('[data-panel-action="toggle-billing-type"]');
            if (!actionEl) return;
            toggleBillingType();
        });
})();


