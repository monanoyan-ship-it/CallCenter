// Salon Dashboard — istatistik kartlari + bugun randevulari + dogum gunu hatirlatmalari
(function () {
    function fmt(n) { return (n || 0).toLocaleString('tr-TR'); }
    function loadDashboard() {
        $.get('/proxy/sln-dashboard', function (d) {
            document.getElementById('totalClients').textContent = fmt(d.totalClients);
            document.getElementById('todayAppointments').textContent = fmt(d.todayAppointmentsCount);
            document.getElementById('todayRevenue').textContent = fmt(d.todayRevenue) + ' ₺';
            document.getElementById('activeStaff').textContent = fmt(d.activeStaff);

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

                var trialAlert = document.getElementById('subTrialAlert');
                if (s.isTrial && s.trialDaysRemaining !== null) {
                    trialAlert.style.display = '';
                    document.getElementById('subTrialText').textContent =
                        s.trialDaysRemaining > 0
                            ? 'Deneme süreniz ' + s.trialDaysRemaining + ' gün içinde bitiyor.'
                            : 'Deneme süreniz bitti.';
                } else {
                    trialAlert.style.display = 'none';
                }

                var pkgWrap = document.getElementById('subPackages');
                pkgWrap.innerHTML = '<span class="badge bg-purple text-white">Temel Paket · ' + fmt(s.basicPackagePrice) + ' ₺</span>';
                (s.activePackages || []).forEach(function (p) {
                    var el = document.createElement('span');
                    el.className = 'badge bg-success-subtle text-success';
                    el.textContent = p.name + ' · ' + fmt(p.monthlyPrice) + ' ₺';
                    pkgWrap.appendChild(el);
                });

                document.getElementById('subMonthlyTotal').textContent = fmt(s.monthlyTotal) + ' ₺/ay';
                document.getElementById('subNextBilling').textContent = s.nextBillingDate
                    ? new Date(s.nextBillingDate).toLocaleDateString('tr-TR')
                    : '-';
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
