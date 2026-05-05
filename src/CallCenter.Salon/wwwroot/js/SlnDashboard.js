// Salon Dashboard — istatistik kartlari + bugun randevulari + dogum gunu hatirlatmalari
(function () {
    function fmt(n) { return (n || 0).toLocaleString('tr-TR'); }
    function escapeHtml(value) {
        return String(value || '')
            .replace(/&/g, '&amp;')
            .replace(/</g, '&lt;')
            .replace(/>/g, '&gt;')
            .replace(/"/g, '&quot;')
            .replace(/'/g, '&#39;');
    }

    function updateSubscriptionPaymentCta(subscription) {
        var btn = document.getElementById('subPaymentBtn');
        var info = document.getElementById('subPaymentInfo');
        if (!btn && !info) return;

        if (btn) {
            btn.style.display = 'none';
            btn.classList.add('disabled');
            btn.setAttribute('aria-disabled', 'true');
            btn.href = '/Modules?pay=subscription';
        }
        if (info) {
            info.style.display = '';
            info.textContent = 'Odeme durumu kontrol ediliyor...';
        }

        $.get('/proxy/subscriptions/my', function (d) {
            var unpaid = d && Array.isArray(d.unpaidBillings) ? d.unpaidBillings : [];
            var hasDebt = unpaid.some(function (b) {
                var total = Number(b && b.total);
                return !isNaN(total) && total > 0;
            });

            if (hasDebt) {
                if (btn) {
                    btn.style.display = '';
                    btn.classList.remove('disabled');
                    btn.removeAttribute('aria-disabled');
                }
                if (info) info.style.display = 'none';
                return;
            }

            if (info) {
                info.style.display = '';
                info.textContent = subscription && subscription.isTrial
                    ? 'Demo surecindesiniz; su anda odenecek tahakkuk yok.'
                    : 'Odenecek tahakkuk yok.';
            }
        }).fail(function () {
            if (info) {
                info.style.display = '';
                info.textContent = 'Odeme durumu alinamadi.';
            }
        });
    }

    function loadDashboard() {
        $.get('/proxy/sln-dashboard', function (d) {
            document.getElementById('totalClients').textContent = fmt(d.totalClients);
            document.getElementById('todayAppointments').textContent = fmt(d.todayAppointmentsCount);
            document.getElementById('todayRevenue').textContent = fmt(d.todayRevenue) + ' ₺';
            document.getElementById('activeStaff').textContent = fmt(d.activeStaff);

            // Kritik stok uyarilari
            var lowStockBadge = document.getElementById('lowStockCount');
            if (lowStockBadge) lowStockBadge.textContent = fmt(d.lowStockCount);

            var lowStockList = document.getElementById('lowStockList');
            if (lowStockList) {
                if (!d.lowStockAlerts || d.lowStockAlerts.length === 0) {
                    lowStockList.innerHTML = '<p class="text-muted small mb-0">Kritik stok yok.</p>';
                } else {
                    lowStockList.innerHTML = d.lowStockAlerts.map(function (p) {
                        var unit = escapeHtml(p.unit || '');
                        return '<a href="/Products" class="d-block text-decoration-none text-dark border-bottom py-2">' +
                            '<div class="d-flex align-items-start justify-content-between gap-2">' +
                            '<div><div class="small fw-semibold">' + escapeHtml(p.productName) + '</div>' +
                            '<div class="text-muted" style="font-size:.75rem;">Stok: ' + fmt(p.stockQuantity) + ' / ' + fmt(p.minStockLevel) + ' ' + unit + '</div></div>' +
                            '<span class="badge bg-danger-subtle text-danger">Kritik</span></div>' +
                            '<div class="small text-success mt-1"><i class="bi bi-cart-plus me-1"></i>Siparis onerisi: ' + fmt(p.reorderQuantity) + ' ' + unit + '</div>' +
                            '</a>';
                    }).join('');
                }
            }

            // Bugunun randevulari
            var apptList = document.getElementById('todayApptList');
            if (apptList) {
                if (!d.todayAppointments || d.todayAppointments.length === 0) {
                    apptList.innerHTML = '<p class="text-muted small mb-0">Bugün randevu yok.</p>';
                } else {
                    apptList.innerHTML = d.todayAppointments.map(function (a) {
                        var time = a.startTime ? a.startTime.substring(11, 16) : '-';
                        var statusText = ({1:'Planlanmış',2:'Onaylandı',3:'Tamamlandı',4:'İptal',5:'Gelmedi'})[a.statusId] || '';
                        var statusCss = ({1:'bg-warning text-dark',2:'bg-info',3:'bg-success',4:'bg-danger',5:'bg-secondary'})[a.statusId] || 'bg-secondary';
                        return '<div class="d-flex align-items-center justify-content-between border-bottom py-2">' +
                            '<div><span class="fw-semibold small">' + time + '</span> · <span class="small">' + (a.clientName || '-') + '</span>' +
                            '<div class="text-muted" style="font-size:.75rem;">' + (a.personnelName || '-') + '</div></div>' +
                            '<span class="badge ' + statusCss + '">' + statusText + '</span></div>';
                    }).join('');
                }
            }

            // Abonelik kartı
            var subCard = document.getElementById('subscriptionCard');
            if (subCard && d.subscription) {
                var s = d.subscription;
                subCard.style.display = '';

                var badge = document.getElementById('subStatusBadge');
                if (s.statusId === 1) { badge.className = 'badge bg-success'; badge.textContent = 'Aktif'; }
                else if (s.statusId === 2) { badge.className = 'badge bg-warning text-dark'; badge.textContent = 'Askıda'; }
                else { badge.className = 'badge bg-secondary'; badge.textContent = 'Pasif'; }

                var pkgWrap = document.getElementById('subPackages');
                pkgWrap.innerHTML = '<span class="badge bg-purple text-white">Temel Paket · ' + fmt(s.basicPackagePrice) + ' ₺</span>';
                (s.activePackages || []).forEach(function (p) {
                    var el = document.createElement('span');
                    el.className = 'badge bg-success-subtle text-success';
                    el.textContent = p.name + ' · ' + fmt(p.monthlyPrice) + ' ₺';
                    pkgWrap.appendChild(el);
                });

                document.getElementById('subMonthlyTotal').textContent = fmt(s.monthlyTotal) + ' ₺/ay';
                var branchInfo = document.getElementById('subBranchInfo');
                if (branchInfo) {
                    if (s.branchCount > 1) {
                        branchInfo.style.display = '';
                        var pct = typeof s.branchDiscountPercent === 'number' ? s.branchDiscountPercent : 0;
                        var gross = typeof s.grossBranchMonthly === 'number' ? s.grossBranchMonthly : 0;
                        var net = typeof s.netBranchMonthly === 'number' ? s.netBranchMonthly : 0;
                        branchInfo.innerHTML = s.branchCount + ' şube · şube eşik indirimi %' + pct +
                            ' · brüt ' + fmt(gross) + ' ₺ → net ' + fmt(net) + ' ₺/ay <span class="text-muted">(paket+temel: ' + fmt(s.baseMonthly) + ' ₺)</span>';
                    } else {
                        branchInfo.style.display = 'none';
                    }
                }
                document.getElementById('subNextBilling').textContent = s.nextBillingDate
                    ? new Date(s.nextBillingDate).toLocaleDateString('tr-TR')
                    : '-';
                updateSubscriptionPaymentCta(s);
            } else if (subCard) {
                subCard.style.display = 'none';
            }

            // Hatirlatmalar — dogum gunleri
            var remList = document.getElementById('reminderList');
            if (remList) {
                if (!d.reminders || d.reminders.length === 0) {
                    remList.innerHTML = '<p class="text-muted small mb-0">Bu hafta doğum günü yok.</p>';
                } else {
                    remList.innerHTML = d.reminders.map(function (r) {
                        return '<div class="d-flex align-items-center justify-content-between border-bottom py-2">' +
                            '<div><i class="bi bi-balloon text-purple me-2"></i><span class="small">' + r.fullName + '</span></div>' +
                            '<span class="badge bg-purple-subtle text-purple">' + r.bdDate + '</span></div>';
                    }).join('');
                }
            }
        }).fail(function () {
            // Sessizce, kart "-" kalir
        });
    }

    // DOMContentLoaded zaten geçtiyse direkt çağır, yoksa olay bekle
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', loadDashboard);
    } else {
        loadDashboard();
    }
    // Ahmet talebi: otomatik yenileme kaldırıldı, kullanıcı sayfayı manuel yenilesin.
})();
