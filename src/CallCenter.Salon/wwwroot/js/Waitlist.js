function WaitlistViewModel() {
    var self = this;
    self.entries = ko.observableArray([]);
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

    var statusTexts = { 1: 'Bekliyor', 2: 'Bildirildi', 3: 'Randevu Alindi', 4: 'Iptal' };
    var statusBadges = { 1: 'bg-warning', 2: 'bg-info', 3: 'bg-success', 4: 'bg-secondary' };

    self.statusText = function (id) { return statusTexts[id] || 'Bilinmiyor'; };
    self.statusBadge = function (id) { return statusBadges[id] || 'bg-secondary'; };

    self.filteredEntries = ko.computed(function () {
        var q = (self.searchQuery() || '').toLowerCase();
        if (!q) return self.entries();
        return self.entries().filter(function (e) {
            return (e.clientName || '').toLowerCase().indexOf(q) >= 0
                || (e.serviceName || '').toLowerCase().indexOf(q) >= 0;
        });
    });

    var formModal;

    self.loadData = function () {
        $.ajax({ url: '/proxy/sln-waitlist', method: 'GET' }).done(function (data) {
            self.entries(data.items || data);
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
        $.ajax({ url: '/proxy/sln-clients/staff', method: 'GET' }).done(function (data) {
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
            self.loadData();
            toastr.success('Bekleme listesine eklendi');
            self.isSaving(false);
        }).fail(function (xhr) {
            toastr.error(xhr.responseJSON || 'Bir hata olustu');
            self.isSaving(false);
        });
    };

    self.notifyEntry = function (entry) {
        $.ajax({ url: '/proxy/sln-waitlist/' + entry.id + '/status/2', method: 'PUT' })
            .done(function () { self.loadData(); toastr.success('Musteri bilgilendirildi'); });
    };

    self.appointmentMade = function (entry) {
        $.ajax({ url: '/proxy/sln-waitlist/' + entry.id + '/status/3', method: 'PUT' })
            .done(function () { self.loadData(); toastr.success('Randevu alindi olarak isaretlendi'); });
    };

    self.removeEntry = function (entry) {
        if (!confirm('Bu kaydi silmek istediginize emin misiniz?')) return;
        $.ajax({ url: '/proxy/sln-waitlist/' + entry.id, method: 'DELETE' })
            .done(function () { self.loadData(); toastr.success('Kayit silindi'); });
    };

    $(document).ready(function () {
        formModal = new bootstrap.Modal(document.getElementById('waitlistModal'));
        self.loadData();
        self.loadLookups();
    });
}

ko.applyBindings(new WaitlistViewModel(), document.getElementById('waitlist-vm'));
