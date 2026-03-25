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
        clientId: ko.observable(null),
        appointmentDate: ko.observable(''),
        startTime: ko.observable(''),
        serviceId: ko.observable(null),
        staffId: ko.observable(null),
        duration: ko.observable(30),
        notes: ko.observable('')
    };

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
        return all.filter(function (a) { return a.staffId == staffId; });
    });

    var formModal;

    self.loadAppointments = function () {
        var url = '/proxy/sln-appointments?date=' + self.selectedDate();
        $.ajax({ url: url, method: 'GET' }).done(function (data) {
            var items = data.items || data;
            items.forEach(function (a) {
                a.statusText = { Pending: 'Bekliyor', Confirmed: 'Onaylandi', InProgress: 'Devam Ediyor', Completed: 'Tamamlandi', Cancelled: 'Iptal', NoShow: 'Gelmedi' }[a.status] || a.status;
                a.statusCss = { Pending: 'bg-warning text-dark', Confirmed: 'bg-info', InProgress: 'bg-primary', Completed: 'bg-success', Cancelled: 'bg-danger', NoShow: 'bg-secondary' }[a.status] || 'bg-secondary';
            });
            items.sort(function (a, b) { return (a.startTime || '').localeCompare(b.startTime || ''); });
            self.appointments(items);
        }).fail(function () {
            toastr.error('Randevular yuklenemedi');
        });
    };

    self.loadLookups = function () {
        $.ajax({ url: '/proxy/sln-clients?pageSize=1000', method: 'GET' }).done(function (data) {
            var items = data.items || data;
            items.forEach(function (c) { c.fullName = (c.firstName || '') + ' ' + (c.lastName || ''); });
            self.clientList(items);
        });
        $.ajax({ url: '/proxy/sln-services', method: 'GET' }).done(function (data) {
            self.serviceList(data.items || data);
        });
        $.ajax({ url: '/proxy/sln-staff', method: 'GET' }).done(function (data) {
            var items = data.items || data;
            items.forEach(function (s) { s.fullName = (s.firstName || '') + ' ' + (s.lastName || ''); });
            self.staffList(items);
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
        self.form.clientId(null);
        self.form.appointmentDate(self.selectedDate());
        self.form.startTime('');
        self.form.serviceId(null);
        self.form.staffId(null);
        self.form.duration(30);
        self.form.notes('');
        self.isEditing(false);
        self.editingId(null);
    };

    self.openNew = function () {
        self.resetForm();
        formModal.show();
    };

    self.openEdit = function (appt) {
        self.isEditing(true);
        self.editingId(appt.id);
        self.form.clientId(appt.clientId);
        self.form.appointmentDate(appt.appointmentDate ? appt.appointmentDate.substring(0, 10) : '');
        self.form.startTime(appt.startTime || '');
        self.form.serviceId(appt.serviceId);
        self.form.staffId(appt.staffId);
        self.form.duration(appt.duration || 30);
        self.form.notes(appt.notes || '');
        formModal.show();
    };

    self.save = function () {
        var data = {
            clientId: self.form.clientId(),
            appointmentDate: self.form.appointmentDate(),
            startTime: self.form.startTime(),
            serviceId: self.form.serviceId(),
            staffId: self.form.staffId(),
            duration: parseInt(self.form.duration()) || 30,
            notes: self.form.notes()
        };

        if (!data.clientId || !data.appointmentDate || !data.startTime || !data.serviceId || !data.staffId) {
            toastr.warning('Musteri, tarih, saat, hizmet ve personel zorunludur');
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
            url: '/proxy/sln-appointments/' + appt.id,
            method: 'PUT',
            contentType: 'application/json',
            data: JSON.stringify({ status: 'Completed' })
        }).done(function () {
            self.loadAppointments();
            toastr.success('Randevu tamamlandi');
        });
    };

    self.cancel = function (appt) {
        if (!confirm('Bu randevuyu iptal etmek istediginize emin misiniz?')) return;
        $.ajax({
            url: '/proxy/sln-appointments/' + appt.id,
            method: 'PUT',
            contentType: 'application/json',
            data: JSON.stringify({ status: 'Cancelled' })
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
