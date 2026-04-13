function WaitlistViewModel() {
    var self = this;

    // ═══════════════════════════════════════════
    //  TAB 1: Bugunun Randevulari
    // ═══════════════════════════════════════════
    self.todayAppointments = ko.observableArray([]);
    self.isLoadingAppts = ko.observable(false);

    var apptStatusNames = { 1: 'Planlanmis', 2: 'Onaylandi', 3: 'Tamamlandi', 4: 'Iptal', 5: 'Gelmedi' };
    var apptStatusCss = { 1: 'bg-warning text-dark', 2: 'bg-info', 3: 'bg-success', 4: 'bg-danger', 5: 'bg-secondary' };

    function toDateStr(d) { return d.toISOString().substring(0, 10); }

    var today = new Date();
    self.todayLabel = today.toLocaleDateString('tr-TR', { weekday: 'long', day: 'numeric', month: 'long', year: 'numeric' });

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
                    var st = new Date(a.startTime);
                    a.startTimeFormatted = st.toLocaleTimeString('tr-TR', { hour: '2-digit', minute: '2-digit' });
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

    self.apptComplete = function (a) { updateApptStatus(a.id, 3, 'Randevu tamamlandi'); };
    self.apptNoShow = function (a) { updateApptStatus(a.id, 5, 'Gelmedi olarak isaretlendi'); };
    self.apptCancel = function (a) {
        confirmModal('Onay', 'Bu randevuyu iptal etmek istediginize emin misiniz?', function() {
            updateApptStatus(a.id, 4, 'Randevu iptal edildi');
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

    var wlStatusTexts = { 1: 'Bekliyor', 2: 'Bildirildi', 3: 'Randevu Alindi', 4: 'Iptal', 5: 'Gerceklesti' };
    var wlStatusBadges = { 1: 'bg-warning text-dark', 2: 'bg-info', 3: 'bg-success', 4: 'bg-secondary', 5: 'bg-primary' };

    self.wlStatusText = function (id) { return wlStatusTexts[id] || '?'; };
    self.wlStatusBadge = function (id) { return wlStatusBadges[id] || 'bg-secondary'; };

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
        var today = toDateStr(new Date());
        $.ajax({ url: '/proxy/sln-waitlist?date=' + today, method: 'GET' }).done(function (data) {
            self.waitlistEntries(data.items || data);
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
            toastr.warning('Musteri, hizmet ve tarih zorunludur');
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
            toastr.error(xhr.responseJSON || 'Bir hata olustu');
            self.isSaving(false);
        });
    };

    self.notifyEntry = function (entry) {
        $.ajax({ url: '/proxy/sln-waitlist/' + entry.id + '/status/2', method: 'PUT' })
            .done(function () { self.loadWaitlist(); toastr.success('Musteri bilgilendirildi'); });
    };

    self.appointmentMade = function (entry) {
        $.ajax({ url: '/proxy/sln-waitlist/' + entry.id + '/status/3', method: 'PUT' })
            .done(function () { self.loadWaitlist(); toastr.success('Randevu alindi olarak isaretlendi'); });
    };

    self.markCompleted = function (entry) {
        $.ajax({ url: '/proxy/sln-waitlist/' + entry.id + '/status/5', method: 'PUT' })
            .done(function () { self.loadWaitlist(); toastr.success('Gerceklesti olarak isaretlendi'); });
    };

    self.removeEntry = function (entry) {
        confirmModal('Onay', 'Bu kaydi silmek istediginize emin misiniz?', function() {
            $.ajax({ url: '/proxy/sln-waitlist/' + entry.id, method: 'DELETE' })
                .done(function () { self.loadWaitlist(); toastr.success('Kayit silindi'); });
        });
    };

    // ═══ Init ═══
    $(document).ready(function () {
        formModal = new bootstrap.Modal(document.getElementById('waitlistModal'));
        self.loadTodayAppointments();
        self.loadWaitlist();
        self.loadLookups();
    });
}

ko.applyBindings(new WaitlistViewModel(), document.getElementById('waitlist-vm'));
