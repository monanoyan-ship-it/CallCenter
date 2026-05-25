// Salon Dashboard — istatistik kartlari + bugun randevulari + dogum gunu hatirlatmalari
(function () {
    var DASH_LOCALE = document.documentElement.lang || undefined;
    function dashT(key, fallback) {
        return (window.salonT || function (k, f) { return f || k; })(key, fallback);
    }
    function fmt(n) { return (n || 0).toLocaleString(DASH_LOCALE); }
    function fmtMoney(n) {
        return (Number(n) || 0).toLocaleString(DASH_LOCALE, { maximumFractionDigits: 2 });
    }
    function tmpl(text, values) {
        return String(text || '').replace(/\{(\w+)\}/g, function (_, key) {
            return values && values[key] != null ? values[key] : '';
        });
    }
    function escapeHtml(value) {
        return String(value || '')
            .replace(/&/g, '&amp;')
            .replace(/</g, '&lt;')
            .replace(/>/g, '&gt;')
            .replace(/"/g, '&quot;')
            .replace(/'/g, '&#39;');
    }

    function priorityLabel(priority) {
        return ({
            high: dashT('salon.dashboard.action.priority.high', 'Acil'),
            medium: dashT('salon.dashboard.action.priority.medium', 'Bugün'),
            low: dashT('salon.dashboard.action.priority.low', 'Kontrol')
        })[priority] || dashT('salon.dashboard.action.priority.medium', 'Bugün');
    }

    function priorityCss(priority) {
        return ({
            high: 'bg-danger-subtle text-danger',
            medium: 'bg-warning-subtle text-warning-emphasis',
            low: 'bg-info-subtle text-info-emphasis'
        })[priority] || 'bg-secondary-subtle text-secondary-emphasis';
    }

    function actionWeight(priority) {
        return ({ high: 0, medium: 1, low: 2 })[priority] ?? 3;
    }

    function buildActionItems(d) {
        var appointments = d.todayAppointments || [];
        var pendingAppointments = appointments.filter(function (a) { return a.statusId === 1; }).length;
        var firstAppointment = appointments.length ? appointments[0] : null;
        var lowStockAlerts = d.lowStockAlerts || [];
        var reminders = d.reminders || [];
        var items = [];

        if ((d.activeStaff || 0) === 0) {
            items.push({
                priority: 'high',
                icon: 'bi-person-plus',
                title: dashT('salon.dashboard.action.staff_title', 'Aktif personel yok'),
                desc: dashT('salon.dashboard.action.staff_desc', 'Takvim ve hizmet eşleştirmeleri için en az bir personel aktif olmalı.'),
                href: '/Staff'
            });
        }

        if ((d.totalClients || 0) === 0) {
            items.push({
                priority: 'high',
                icon: 'bi-person-heart',
                title: dashT('salon.dashboard.action.first_client_title', 'İlk müşteriyi ekle'),
                desc: dashT('salon.dashboard.action.first_client_desc', 'Müşteri listesi boşsa randevu ve satış akışları zayıf kalır.'),
                href: '/Clients'
            });
        }

        if (pendingAppointments > 0) {
            items.push({
                priority: 'high',
                icon: 'bi-calendar2-check',
                title: tmpl(dashT('salon.dashboard.action.confirm_appointments_title', '{count} randevu onay bekliyor'), { count: fmt(pendingAppointments) }),
                desc: dashT('salon.dashboard.action.confirm_appointments_desc', 'Saat sırasına göre randevuları kontrol et.'),
                href: '/Appointments'
            });
        } else if (firstAppointment) {
            var time = firstAppointment.startTime ? firstAppointment.startTime.substring(11, 16) : '-';
            items.push({
                priority: 'medium',
                icon: 'bi-clock-history',
                title: dashT('salon.dashboard.action.next_appointment_title', 'Sıradaki randevuyu hazırla'),
                desc: tmpl(dashT('salon.dashboard.action.next_appointment_desc', '{time} · {client} · {personnel}'), {
                    time: time,
                    client: firstAppointment.clientName || '-',
                    personnel: firstAppointment.personnelName || '-'
                }),
                href: '/Appointments'
            });
        } else {
            items.push({
                priority: 'medium',
                icon: 'bi-calendar-plus',
                title: dashT('salon.dashboard.action.empty_calendar_title', 'Bugün takvim boş'),
                desc: dashT('salon.dashboard.action.empty_calendar_desc', 'Bekleme listesi ve pasif müşterilerden gün içine doluluk yarat.'),
                href: '/Waitlist'
            });
        }

        if ((d.lowStockCount || 0) > 0) {
            var firstStock = lowStockAlerts.length ? lowStockAlerts[0].productName : '-';
            items.push({
                priority: 'high',
                icon: 'bi-box-seam',
                title: tmpl(dashT('salon.dashboard.action.critical_stock_title', '{count} ürün kritik stokta'), { count: fmt(d.lowStockCount) }),
                desc: tmpl(dashT('salon.dashboard.action.critical_stock_desc', 'En düşük stok: {product}. Sipariş miktarını kontrol et.'), { product: firstStock }),
                href: '/Products'
            });
        }

        if (reminders.length > 0) {
            items.push({
                priority: 'medium',
                icon: 'bi-balloon-heart',
                title: tmpl(dashT('salon.dashboard.action.birthday_title', '{count} doğum günü fırsatı'), { count: fmt(reminders.length) }),
                desc: tmpl(dashT('salon.dashboard.action.birthday_desc', '{client} ile başlayarak kişisel kampanya gönder.'), { client: reminders[0].fullName || '-' }),
                href: '/Marketing?tab=campaigns'
            });
        }

        if ((d.todayRevenue || 0) > 0) {
            items.push({
                priority: 'low',
                icon: 'bi-cash-coin',
                title: dashT('salon.dashboard.action.cash_title', 'Gün sonu kasa kontrolünü kaçırma'),
                desc: tmpl(dashT('salon.dashboard.action.cash_desc', 'Bugünkü ciro {amount} ₺. Kasa kapanışını hazırla.'), { amount: fmtMoney(d.todayRevenue) }),
                href: '/Cash'
            });
        }

        return items.sort(function (a, b) { return actionWeight(a.priority) - actionWeight(b.priority); }).slice(0, 5);
    }

    function renderActionCenter(d) {
        var list = document.getElementById('actionCenterList');
        if (!list) return;

        var items = buildActionItems(d);
        if (!items.length) {
            list.innerHTML = '<div class="py-2">' +
                '<div class="small fw-semibold">' + escapeHtml(dashT('salon.dashboard.action.all_clear_title', 'Bugün kritik aksiyon yok')) + '</div>' +
                '<div class="dashboard-action-desc">' + escapeHtml(dashT('salon.dashboard.action.all_clear_desc', 'Takvim, stok ve hatırlatmalar şu an sakin.')) + '</div>' +
                '</div>';
            return;
        }

        list.innerHTML = items.map(function (item) {
            return '<a class="dashboard-action" href="' + escapeHtml(item.href) + '">' +
                '<div class="d-flex align-items-start gap-3">' +
                '<span class="dashboard-action-icon"><i class="bi ' + escapeHtml(item.icon) + '"></i></span>' +
                '<span><span class="dashboard-action-title d-block">' + escapeHtml(item.title) + '</span>' +
                '<span class="dashboard-action-desc d-block">' + escapeHtml(item.desc) + '</span></span>' +
                '</div>' +
                '<span class="d-inline-flex align-items-center gap-2 flex-shrink-0">' +
                '<span class="badge ' + priorityCss(item.priority) + '">' + escapeHtml(priorityLabel(item.priority)) + '</span>' +
                '<i class="bi bi-chevron-right text-muted"></i>' +
                '</span>' +
                '</a>';
        }).join('');
    }

    function loadDashboard() {
        $.get('/proxy/sln-dashboard', function (d) {
            document.getElementById('totalClients').textContent = fmt(d.totalClients);
            document.getElementById('todayAppointments').textContent = fmt(d.todayAppointmentsCount);
            document.getElementById('todayRevenue').textContent = fmt(d.todayRevenue) + ' ₺';
            document.getElementById('activeStaff').textContent = fmt(d.activeStaff);
            renderActionCenter(d);

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
