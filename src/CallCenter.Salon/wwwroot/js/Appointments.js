function AppointmentsViewModel() {
    var self = this;
    self.appointments = ko.observableArray([]);
    self.clientList = ko.observableArray([]);
    self.serviceList = ko.observableArray([]);
    self.staffList = ko.observableArray([]);
    self.selectedDate = ko.observable(new Date().toISOString().substring(0, 10));
    self.selectedStaffId = ko.observable(null);
    self.isEditing = ko.observable(false);
    self.editingId = ko.observable(null);
    self.isSaving = ko.observable(false);

    self.form = {
        slnClientId: ko.observable(null),
        startTime: ko.observable(''),
        serviceId: ko.observable(null),
        personnelId: ko.observable(null),
        notes: ko.observable('')
    };

    // ═══ Autocomplete'ler ═══
    self.clientAutocomplete = createAutocomplete(self.clientList, 'fullName', self.form.slnClientId);
    self.serviceAutocomplete = createAutocomplete(self.serviceList, 'name', self.form.serviceId);
    self.staffAutocomplete = createAutocomplete(self.staffList, 'fullName', self.form.personnelId);

    var statusNames = { 1: 'Planlanmis', 2: 'Onaylandi', 3: 'Tamamlandi', 4: 'Iptal', 5: 'Gelmedi' };
    var statusCss = { 1: 'bg-warning text-dark', 2: 'bg-info', 3: 'bg-success', 4: 'bg-danger', 5: 'bg-secondary' };

    self.formattedDate = ko.computed(function () {
        var d = self.selectedDate();
        if (!d) return '';
        var date = new Date(d + 'T00:00:00');
        var days = ['Pazar', 'Pazartesi', 'Sali', 'Carsamba', 'Persembe', 'Cuma', 'Cumartesi'];
        return date.toLocaleDateString('tr-TR', { day: 'numeric', month: 'long', year: 'numeric' }) + ' - ' + days[date.getDay()];
    });

    self.filteredAppointments = ko.computed(function () {
        var staffId = self.selectedStaffId();
        var all = self.appointments();
        if (!staffId) return all;
        return all.filter(function (a) { return a.personnelId == staffId; });
    });

    var formModal;

    self.loadAppointments = function () {
        var d = self.selectedDate();
        var url = '/proxy/sln-appointments?from=' + d + 'T00:00:00Z&to=' + d + 'T23:59:59Z';
        $.ajax({ url: url, method: 'GET' }).done(function (data) {
            var items = data.items || data;
            items.forEach(function (a) {
                a.statusText = statusNames[a.statusId] || 'Bilinmiyor';
                a.statusCss = statusCss[a.statusId] || 'bg-secondary';
                if (a.startTime) {
                    var st = new Date(a.startTime);
                    a.startTimeFormatted = st.toLocaleTimeString('tr-TR', { hour: '2-digit', minute: '2-digit' });
                }
                if (a.endTime) {
                    var et = new Date(a.endTime);
                    a.endTimeFormatted = et.toLocaleTimeString('tr-TR', { hour: '2-digit', minute: '2-digit' });
                }
                if (a.startTime && a.endTime) {
                    a.durationMinutes = Math.round((new Date(a.endTime) - new Date(a.startTime)) / 60000);
                } else {
                    a.durationMinutes = null;
                }
            });
            items.sort(function (a, b) { return (a.startTime || '').localeCompare(b.startTime || ''); });
            self.appointments(items);
        }).fail(function () {
            toastr.error('Randevular yuklenemedi');
        });
    };

    self.loadLookups = function () {
        $.ajax({ url: '/proxy/sln-clients?pageSize=1000', method: 'GET' }).done(function (data) {
            self.clientList(data.items || data);
        });
        $.ajax({ url: '/proxy/sln-services', method: 'GET' }).done(function (data) {
            self.serviceList(data.items || data);
        });
        $.ajax({ url: '/proxy/portal/personnel', method: 'GET' }).done(function (data) {
            self.staffList(data.items || data);
        });
    };

    self.selectedDate.subscribe(function () { self.loadAppointments(); });

    self.prevDay = function () {
        var d = new Date(self.selectedDate() + 'T00:00:00');
        d.setDate(d.getDate() - 1);
        self.selectedDate(d.toISOString().substring(0, 10));
    };

    self.nextDay = function () {
        var d = new Date(self.selectedDate() + 'T00:00:00');
        d.setDate(d.getDate() + 1);
        self.selectedDate(d.toISOString().substring(0, 10));
    };

    self.goToday = function () {
        self.selectedDate(new Date().toISOString().substring(0, 10));
    };

    self.resetForm = function () {
        self.form.slnClientId(null);
        self.form.startTime('');
        self.form.serviceId(null);
        self.form.personnelId(null);
        self.form.notes('');
        self.isEditing(false);
        self.editingId(null);
        self.clientAutocomplete.clear();
        self.serviceAutocomplete.clear();
        self.staffAutocomplete.clear();
    };

    self.openNew = function () {
        self.resetForm();
        formModal.show();
    };

    self.openEdit = function (appt) {
        self.isEditing(true);
        self.editingId(appt.id);
        self.form.slnClientId(appt.slnClientId);
        self.form.startTime(appt.startTime ? appt.startTime.substring(0, 16) : '');
        self.form.serviceId(appt.serviceId);
        self.form.personnelId(appt.personnelId);
        self.form.notes(appt.notes || '');
        self.clientAutocomplete.setFromValue(appt.slnClientId);
        self.serviceAutocomplete.setFromValue(appt.serviceId);
        self.staffAutocomplete.setFromValue(appt.personnelId);
        formModal.show();
    };

    self.save = function () {
        var startTimeVal = self.form.startTime();
        if (!startTimeVal) {
            startTimeVal = self.selectedDate() + 'T09:00:00';
        } else if (startTimeVal.length <= 5) {
            startTimeVal = self.selectedDate() + 'T' + startTimeVal + ':00';
        }

        var data = {
            slnClientId: self.form.slnClientId(),
            personnelId: self.form.personnelId(),
            serviceId: self.form.serviceId(),
            startTime: startTimeVal,
            notes: self.form.notes()
        };

        if (!data.slnClientId || !data.startTime || !data.serviceId || !data.personnelId) {
            toastr.warning('Musteri, saat, hizmet ve personel zorunludur');
            return;
        }

        self.isSaving(true);
        var url = '/proxy/sln-appointments';
        var method = 'POST';
        if (self.isEditing()) {
            url += '/' + self.editingId();
            method = 'PUT';
        }

        $.ajax({
            url: url, method: method,
            contentType: 'application/json',
            data: JSON.stringify(data)
        }).done(function () {
            formModal.hide();
            self.loadAppointments();
            toastr.success(self.isEditing() ? 'Randevu guncellendi' : 'Randevu eklendi');
            self.isSaving(false);
        }).fail(function (xhr) {
            toastr.error(xhr.responseJSON?.error || 'Bir hata olustu');
            self.isSaving(false);
        });
    };

    self.complete = function (appt) {
        $.ajax({
            url: '/proxy/sln-appointments/' + appt.id + '/status',
            method: 'PUT',
            contentType: 'application/json',
            data: JSON.stringify({ statusId: 3 })
        }).done(function () {
            self.loadAppointments();
            toastr.success('Randevu tamamlandi');
        });
    };

    self.cancel = function (appt) {
        if (!confirm('Bu randevuyu iptal etmek istediginize emin misiniz?')) return;
        $.ajax({
            url: '/proxy/sln-appointments/' + appt.id + '/status',
            method: 'PUT',
            contentType: 'application/json',
            data: JSON.stringify({ statusId: 4 })
        }).done(function () {
            self.loadAppointments();
            toastr.success('Randevu iptal edildi');
        });
    };

    $(document).ready(function () {
        formModal = new bootstrap.Modal(document.getElementById('appointmentModal'));
        self.loadLookups();
        self.loadAppointments();
    });
}

ko.applyBindings(new AppointmentsViewModel(), document.getElementById('appointments-vm'));
