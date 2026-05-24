// Salon Dashboard — istatistik kartlari + bugun randevulari + dogum gunu hatirlatmalari
(function () {
    var DASH_LOCALE = document.documentElement.lang || undefined;
    function dashT(key, fallback) {
        return (window.salonT || function (k, f) { return f || k; })(key, fallback);
    }
    function fmt(n) { return (n || 0).toLocaleString(DASH_LOCALE); }
    function escapeHtml(value) {
        return String(value || '')
            .replace(/&/g, '&amp;')
            .replace(/</g, '&lt;')
            .replace(/>/g, '&gt;')
            .replace(/"/g, '&quot;')
            .replace(/'/g, '&#39;');
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
                    lowStockList.innerHTML = '<p class="text-muted small mb-0">' + escapeHtml(dashT('salon.dashboard.no_critical_stock', 'Kritik stok yok.')) + '</p>';
                } else {
                    lowStockList.innerHTML = d.lowStockAlerts.map(function (p) {
                        var unit = escapeHtml(p.unit || '');
                        return '<a href="/Products" class="d-block text-decoration-none text-dark border-bottom py-2">' +
                            '<div class="d-flex align-items-start justify-content-between gap-2">' +
                            '<div><div class="small fw-semibold">' + escapeHtml(p.productName) + '</div>' +
                            '<div class="text-muted" style="font-size:.75rem;">' + escapeHtml(dashT('salon.dashboard.stock', 'Stok')) + ': ' + fmt(p.stockQuantity) + ' / ' + fmt(p.minStockLevel) + ' ' + unit + '</div></div>' +
                            '<span class="badge bg-danger-subtle text-danger">' + escapeHtml(dashT('salon.dashboard.critical', 'Kritik')) + '</span></div>' +
                            '<div class="small text-success mt-1"><i class="bi bi-cart-plus me-1"></i>' + escapeHtml(dashT('salon.dashboard.reorder_suggestion', 'Sipariş önerisi')) + ': ' + fmt(p.reorderQuantity) + ' ' + unit + '</div>' +
                            '</a>';
                    }).join('');
                }
            }

            // Bugunun randevulari
            var apptList = document.getElementById('todayApptList');
            if (apptList) {
                if (!d.todayAppointments || d.todayAppointments.length === 0) {
                    apptList.innerHTML = '<p class="text-muted small mb-0">' + escapeHtml(dashT('salon.dashboard.no_appointments_today', 'Bugün randevu yok.')) + '</p>';
                } else {
                    apptList.innerHTML = d.todayAppointments.map(function (a) {
                        var time = a.startTime ? a.startTime.substring(11, 16) : '-';
                        var statusText = ({
                            1: dashT('salon.dashboard.status.scheduled', 'Planlanmış'),
                            2: dashT('salon.dashboard.status.confirmed', 'Onaylandı'),
                            3: dashT('salon.dashboard.status.completed', 'Tamamlandı'),
                            4: dashT('salon.dashboard.status.cancelled', 'İptal'),
                            5: dashT('salon.dashboard.status.no_show', 'Gelmedi')
                        })[a.statusId] || '';
                        var statusCss = ({1:'bg-warning text-dark',2:'bg-info',3:'bg-success',4:'bg-danger',5:'bg-secondary'})[a.statusId] || 'bg-secondary';
                        return '<div class="d-flex align-items-center justify-content-between border-bottom py-2">' +
                            '<div><span class="fw-semibold small">' + time + '</span> · <span class="small">' + (a.clientName || '-') + '</span>' +
                            '<div class="text-muted" style="font-size:.75rem;">' + (a.personnelName || '-') + '</div></div>' +
                            '<span class="badge ' + statusCss + '">' + statusText + '</span></div>';
                    }).join('');
                }
            }

            // Hatirlatmalar — dogum gunleri
            var remList = document.getElementById('reminderList');
            if (remList) {
                if (!d.reminders || d.reminders.length === 0) {
                    remList.innerHTML = '<p class="text-muted small mb-0">' + escapeHtml(dashT('salon.dashboard.no_birthdays_this_week', 'Bu hafta doğum günü yok.')) + '</p>';
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
