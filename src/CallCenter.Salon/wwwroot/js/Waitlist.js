function slnJsT(key, fallback) {
    return (window.salonT || function (k, f) { return f || k; })(key, fallback);
}

function WaitlistViewModel() {
    var self = this;

    // ═══════════════════════════════════════════
    //  TAB 1: Bugunun Randevulari
    // ═══════════════════════════════════════════
    self.todayAppointments = ko.observableArray([]);
    self.isLoadingAppts = ko.observable(false);

    var apptStatusNames = {
        1: slnJsT('salon.appointments.status.scheduled', 'Planlanmış'),
        2: slnJsT('salon.appointments.status.confirmed', 'Onaylandı'),
        3: slnJsT('salon.appointments.status.completed', 'Tamamlandı'),
        4: slnJsT('salon.appointments.status.cancelled', 'İptal'),
        5: slnJsT('salon.appointments.status.no_show', 'Gelmedi')
    };
    var apptStatusCss = { 1: 'bg-warning text-dark', 2: 'bg-info', 3: 'bg-success', 4: 'bg-danger', 5: 'bg-secondary' };

    function toDateStr(d) { return d.toISOString().substring(0, 10); }

    var today = new Date();
    self.todayLabel = today.toLocaleDateString(document.documentElement.lang || undefined, { weekday: 'long', day: 'numeric', month: 'long', year: 'numeric' });

    self.loadTodayAppointments = function () {
        self.isLoadingAppts(true);
        var d = toDateStr(new Date());
        $.ajax({
            url: '/proxy/sln-appointments?from=' + d + 'T00:00:00Z&to=' + d + 'T23:59:59Z',
            method: 'GET'
        }).done(function (data) {
            var items = data.items || data;
            items.forEach(function (a) {
                a.statusText = apptStatusNames[a.statusId] || '?';
                a.statusCss = apptStatusCss[a.statusId] || 'bg-secondary';
                a.serviceNamesText = (a.serviceNames && a.serviceNames.length > 0)
                    ? a.serviceNames.join(', ')
                    : (a.serviceName || '-');
                if (a.startTime) {
                    // BUG2.17: Naive saat — toLocale yapma, ISO substring ile al
                    a.startTimeFormatted = a.startTime.substring(11, 16);
                }
                if (a.startTime && a.endTime) {
                    a.durationMinutes = Math.round((new Date(a.endTime) - new Date(a.startTime)) / 60000);
                } else {
                    a.durationMinutes = null;
                }
            });
            items.sort(function (a, b) { return (a.startTime || '').localeCompare(b.startTime || ''); });
            self.todayAppointments(items);
        }).always(function () { self.isLoadingAppts(false); });
    };

    function updateApptStatus(id, statusId, msg) {
        $.ajax({
            url: '/proxy/sln-appointments/' + id + '/status',
            method: 'PUT',
            contentType: 'application/json',
            data: JSON.stringify({ statusId: statusId })
        }).done(function () {
            self.loadTodayAppointments();
            toastr.success(msg);
        }).fail(function () { toastr.error('Islem hatasi'); });
    }

    self.apptComplete = function (a) { updateApptStatus(a.id, 3, slnJsT('salon.waitlist.js.randevu_tamamlandi', 'Randevu tamamlandi')); };
    self.apptNoShow = function (a) { updateApptStatus(a.id, 5, 'Gelmedi olarak isaretlendi'); };
    self.apptCancel = function (a) {
        confirmModal(slnJsT('salon.common.btn.confirm', 'Onayla'), slnJsT('salon.waitlist.js.bu_randevuyu_iptal_etmek_istediginize_emin_misiniz', 'Bu randevuyu iptal etmek istediginize emin misiniz?'), function() {
            updateApptStatus(a.id, 4, slnJsT('salon.waitlist.js.randevu_iptal_edildi', 'Randevu iptal edildi'));
        });
    };

    // ═══════════════════════════════════════════
    //  TAB 2: Bekleme Listesi
    // ═══════════════════════════════════════════
    self.waitlistEntries = ko.observableArray([]);
    self.clients = ko.observableArray([]);
    self.services = ko.observableArray([]);
    self.staff = ko.observableArray([]);
    self.searchQuery = ko.observable('');
    self.isSaving = ko.observable(false);

    self.form = {
        slnClientId: ko.observable(''),
        serviceId: ko.observable(''),
        preferredPersonnelId: ko.observable(''),
        preferredDate: ko.observable(''),
        preferredTimeSlot: ko.observable(''),
        notes: ko.observable('')
    };

    var wlStatusTexts = {
        1: slnJsT('salon.waitlist.status.waiting', 'Bekliyor'),
        2: slnJsT('salon.waitlist.status.notified', 'Bildirildi'),
        3: slnJsT('salon.waitlist.js.randevu_alindi', 'Randevu Alındı'),
        4: slnJsT('salon.common.btn.cancel', 'İptal'),
        5: slnJsT('salon.waitlist.status.completed', 'Gerçekleşti')
    };
    var wlStatusBadges = { 1: 'bg-warning text-dark', 2: 'bg-info', 3: 'bg-success', 4: 'bg-secondary', 5: 'bg-primary' };

    self.wlStatusText = function (id) { return wlStatusTexts[id] || '?'; };
    self.wlStatusBadge = function (id) { return wlStatusBadges[id] || 'bg-secondary'; };

    self.formatDate = function (iso) {
        if (!iso) return '-';
        var s = String(iso).substring(0, 10);
        var p = s.split('-');
        return p.length === 3 ? (parseInt(p[2]) + '.' + parseInt(p[1]) + '.' + p[0]) : s;
    };

    self.filteredEntries = ko.computed(function () {
        var q = (self.searchQuery() || '').toLowerCase();
        if (!q) return self.waitlistEntries();
        return self.waitlistEntries().filter(function (e) {
            return (e.clientName || '').toLowerCase().indexOf(q) >= 0
                || (e.serviceName || '').toLowerCase().indexOf(q) >= 0;
        });
    });

    var formModal;

    self.loadWaitlist = function () {
        // Tum kayitlari getir (date filter UI tarafinda yapilir, sadece bekleyen + bildirilenleri goster)
        $.ajax({ url: '/proxy/sln-waitlist', method: 'GET' }).done(function (data) {
            var items = data.items || data || [];
            // Aktif olanlar: 1=Bekliyor, 2=Bildirildi (3=Randevu Alindi, 4=Iptal, 5=Gerceklesti gizli)
            self.waitlistEntries(items.filter(function (e) { return e.statusId === 1 || e.statusId === 2; }));
        });
    };

    self.loadLookups = function () {
        $.ajax({ url: '/proxy/sln-clients', method: 'GET' }).done(function (data) {
            self.clients(data.items || data);
        });
        $.ajax({ url: '/proxy/sln-services', method: 'GET' }).done(function (data) {
            var flat = [];
            (data.items || data || []).forEach(function (cat) {
                (cat.services || []).forEach(function (s) { flat.push(s); });
            });
            self.services(flat.length > 0 ? flat : (data.items || data));
        });
        $.ajax({ url: '/proxy/portal/personnel', method: 'GET' }).done(function (data) {
            self.staff(data.items || data);
        });
    };

    self.openNew = function () {
        self.form.slnClientId('');
        self.form.serviceId('');
        self.form.preferredPersonnelId('');
        self.form.preferredDate('');
        self.form.preferredTimeSlot('');
        self.form.notes('');
        formModal.show();
    };

    self.save = function () {
        var data = {
            slnClientId: parseInt(self.form.slnClientId()) || 0,
            serviceId: parseInt(self.form.serviceId()) || 0,
            preferredPersonnelId: self.form.preferredPersonnelId() ? parseInt(self.form.preferredPersonnelId()) : null,
            preferredDate: self.form.preferredDate() ? self.form.preferredDate() + 'T00:00:00Z' : null,
            preferredTimeSlot: self.form.preferredTimeSlot() || null,
            notes: self.form.notes() || null
        };

        if (!data.slnClientId || !data.serviceId || !data.preferredDate) {
            toastr.warning(slnJsT('salon.waitlist.js.musteri_hizmet_ve_tarih_zorunludur', 'Müşteri, hizmet ve tarih zorunludur'));
            return;
        }

        self.isSaving(true);
        $.ajax({
            url: '/proxy/sln-waitlist', method: 'POST',
            contentType: 'application/json', data: JSON.stringify(data)
        }).done(function () {
            formModal.hide();
            self.loadWaitlist();
            toastr.success('Bekleme listesine eklendi');
            self.isSaving(false);
        }).fail(function (xhr) {
            toastr.error(xhr.responseJSON || slnJsT('salon.common.error.generic', 'Bir hata oluştu'));
            self.isSaving(false);
        });
    };

    self.notifyEntry = function (entry) {
        $.ajax({ url: '/proxy/sln-waitlist/' + entry.id + '/status/2', method: 'PUT' })
            .done(function () { self.loadWaitlist(); toastr.success(slnJsT('salon.waitlist.js.musteri_bilgilendirildi', 'Müşteri bilgilendirildi')); });
    };

    self.appointmentMade = function (entry) {
        $.ajax({ url: '/proxy/sln-waitlist/' + entry.id + '/status/3', method: 'PUT' })
            .done(function () { self.loadWaitlist(); toastr.success(slnJsT('salon.waitlist.js.randevu_alindi_olarak_isaretlendi', 'Randevu alindi olarak isaretlendi')); });
    };

    self.markCompleted = function (entry) {
        $.ajax({ url: '/proxy/sln-waitlist/' + entry.id + '/status/5', method: 'PUT' })
            .done(function () { self.loadWaitlist(); toastr.success(slnJsT('salon.waitlist.js.marked_completed', 'Gerçekleşti olarak işaretlendi')); });
    };

    self.removeEntry = function (entry) {
        confirmModal(slnJsT('salon.common.btn.confirm', 'Onayla'), slnJsT('salon.waitlist.js.delete_confirm', 'Bu kaydı silmek istediğinize emin misiniz?'), function() {
            $.ajax({ url: '/proxy/sln-waitlist/' + entry.id, method: 'DELETE' })
                .done(function () { self.loadWaitlist(); toastr.success(slnJsT('salon.waitlist.js.kayit_silindi', 'Kayit silindi')); });
        });
    };

    self.normalizeBranches = function () {
        confirmModal(slnJsT('salon.common.btn.confirm', 'Onayla'), slnJsT('salon.waitlist.js.sube_bilgisi_olmayan_bekleme_kayitlari_personelin_varsa_veya_merkez_su', 'Şube bilgisi olmayan bekleme kayitlari personelin (varsa) veya merkez şubeye baglanacak. Devam?'), function () {
            $.ajax({ url: '/proxy/sln-waitlist/normalize-branches?_nb=1', method: 'POST' })
                .done(function (res) {
                    if (res && res.error) { toastr.error(res.error); return; }
                    var msg = slnJsT('salon.waitlist.js.records_updated', '{count} kayıt güncellendi').replace('{count}', res.updated || 0);
                    if (res.viaPersonnel > 0) msg += ' (' + slnJsT('salon.appointments.from_staff_suffix', 'personelden') + ': ' + res.viaPersonnel + ')';
                    if (res.viaHq > 0) msg += ' (' + slnJsT('salon.appointments.to_hq_suffix', 'merkeze') + ': ' + res.viaHq + ')';
                    toastr.success(msg);
                    self.loadWaitlist();
                })
                .fail(function () { toastr.error(slnJsT('salon.waitlist.js.normalize_failed', 'Normalize başarısız')); });
        });
    };

    // ═══ Randevuya Donustur ═══
    var convertModal;
    self.isConverting = ko.observable(false);
    self.convertCtx = {
        entryId: ko.observable(0),
        clientName: ko.observable(''),
        serviceName: ko.observable(''),
        preferredPersonnelName: ko.observable('')
    };
    self.convertForm = {
        date: ko.observable(''),
        time: ko.observable(''),
        personnelId: ko.observable(''),
        notes: ko.observable(''),
        slnClientId: ko.observable(0),
        serviceId: ko.observable(0)
    };

    self.openConvert = function (entry) {
        self.convertCtx.entryId(entry.id);
        self.convertCtx.clientName(entry.clientName || '');
        self.convertCtx.serviceName(entry.serviceName || '');
        self.convertCtx.preferredPersonnelName(entry.preferredPersonnelName || '');
        self.convertForm.slnClientId(entry.slnClientId);
        self.convertForm.serviceId(entry.serviceId);
        // Tarih: tercih varsa onu, yoksa bugun
        var d = entry.preferredDate ? entry.preferredDate.substring(0, 10) : toDateStr(new Date());
        self.convertForm.date(d);
        // Saat dilimi varsa default oneri (Sabah=10:00, Ogle=13:00, Aksam=16:00)
        var slotMap = { 'Sabah': '10:00', 'Ogle': '13:00', 'Aksam': '16:00' };
        self.convertForm.time(slotMap[entry.preferredTimeSlot] || '10:00');
        self.convertForm.personnelId(entry.preferredPersonnelId || '');
        self.convertForm.notes(entry.notes || '');
        convertModal.show();
    };

    self.submitConvert = function () {
        if (!self.convertForm.date() || !self.convertForm.time() || !self.convertForm.personnelId()) {
            toastr.warning(slnJsT('salon.waitlist.js.tarih_saat_ve_personel_zorunlu', 'Tarih, saat ve personel zorunlu'));
            return;
        }
        self.isConverting(true);
        var startTime = self.convertForm.date() + 'T' + self.convertForm.time() + ':00Z';
        var apptPayload = {
            slnClientId: self.convertForm.slnClientId(),
            personnelId: parseInt(self.convertForm.personnelId()),
            serviceIds: [self.convertForm.serviceId()],
            startTime: startTime,
            notes: self.convertForm.notes() || null
        };
        $.ajax({
            url: '/proxy/sln-appointments', method: 'POST',
            contentType: 'application/json', data: JSON.stringify(apptPayload)
        }).done(function () {
            // Bekleme kaydini "Randevu Alindi" olarak isaretle
            $.ajax({ url: '/proxy/sln-waitlist/' + self.convertCtx.entryId() + '/status/3', method: 'PUT' })
                .always(function () {
                    self.isConverting(false);
                    convertModal.hide();
                    self.loadWaitlist();
                    self.loadTodayAppointments();
                    toastr.success(slnJsT('salon.waitlist.js.randevu_olusturuldu_bekleme_kaydi_guncellendi', 'Randevu oluşturuldu, bekleme kaydi güncellendi'));
                });
        }).fail(function (xhr) {
            self.isConverting(false);
            var err = (xhr.responseJSON && (xhr.responseJSON.message || xhr.responseJSON.error)) || xhr.responseText || slnJsT('salon.waitlist.js.randevu_olusturulamadi', 'Randevu oluşturulamadı');
            toastr.error(err);
        });
    };

    // ═══ Init ═══
    $(document).ready(function () {
        formModal = new bootstrap.Modal(document.getElementById('waitlistModal'));
        convertModal = new bootstrap.Modal(document.getElementById('convertModal'));
        self.loadTodayAppointments();
        self.loadWaitlist();
        self.loadLookups();
    });
}

ko.applyBindings(new WaitlistViewModel(), document.getElementById('waitlist-vm'));
