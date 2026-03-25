function ClientDetailViewModel() {
    var self = this;
    var id = clientDetailId;

    self.client = ko.observable({});
    self.appointments = ko.observableArray([]);
    self.invoices = ko.observableArray([]);
    self.formulas = ko.observableArray([]);
    self.photos = ko.observableArray([]);
    self.isSaving = ko.observable(false);

    self.formulaForm = {
        title: ko.observable(''),
        content: ko.observable('')
    };

    self.totalSpent = ko.computed(function () {
        var total = 0;
        self.invoices().forEach(function (inv) { total += inv.totalAmount || 0; });
        return total;
    });

    self.lastVisit = ko.computed(function () {
        var appts = self.appointments();
        if (appts.length === 0) return null;
        var sorted = appts.slice().sort(function (a, b) {
            return new Date(b.appointmentDate) - new Date(a.appointmentDate);
        });
        return new Date(sorted[0].appointmentDate).toLocaleDateString('tr-TR');
    });

    var formulaModal, photoModal;

    self.loadClient = function () {
        $.ajax({ url: '/proxy/sln-clients/' + id, method: 'GET' }).done(function (data) {
            if (data.gender === 'Female') data.genderText = 'Kadin';
            else if (data.gender === 'Male') data.genderText = 'Erkek';
            else data.genderText = '';
            if (data.birthDate) {
                var bd = new Date(data.birthDate);
                var today = new Date();
                data.age = today.getFullYear() - bd.getFullYear();
                if (today.getMonth() < bd.getMonth() || (today.getMonth() === bd.getMonth() && today.getDate() < bd.getDate())) data.age--;
            }
            self.client(data);
        }).fail(function () {
            toastr.error('Musteri bilgisi yuklenemedi');
        });
    };

    self.loadAppointments = function () {
        $.ajax({ url: '/proxy/sln-appointments?clientId=' + id, method: 'GET' }).done(function (data) {
            var items = data.items || data;
            items.forEach(function (a) {
                a.statusText = { Pending: 'Bekliyor', Confirmed: 'Onaylandi', InProgress: 'Devam Ediyor', Completed: 'Tamamlandi', Cancelled: 'Iptal', NoShow: 'Gelmedi' }[a.status] || a.status;
                a.statusCss = { Pending: 'bg-warning text-dark', Confirmed: 'bg-info', InProgress: 'bg-primary', Completed: 'bg-success', Cancelled: 'bg-danger', NoShow: 'bg-secondary' }[a.status] || 'bg-secondary';
            });
            self.appointments(items);
        });
    };

    self.loadInvoices = function () {
        $.ajax({ url: '/proxy/sln-invoices?clientId=' + id, method: 'GET' }).done(function (data) {
            var items = data.items || data;
            items.forEach(function (inv) {
                inv.statusText = { Open: 'Acik', Closed: 'Kapali', Cancelled: 'Iptal' }[inv.status] || inv.status;
                inv.statusCss = { Open: 'bg-warning text-dark', Closed: 'bg-success', Cancelled: 'bg-danger' }[inv.status] || 'bg-secondary';
            });
            self.invoices(items);
        });
    };

    self.loadFormulas = function () {
        $.ajax({ url: '/proxy/sln-clients/' + id + '/formulas', method: 'GET' }).done(function (data) {
            self.formulas(data);
        });
    };

    self.loadPhotos = function () {
        $.ajax({ url: '/proxy/sln-clients/' + id + '/photos', method: 'GET' }).done(function (data) {
            self.photos(data);
        });
    };

    // Formula CRUD
    self.openNewFormula = function () {
        self.formulaForm.title('');
        self.formulaForm.content('');
        formulaModal.show();
    };

    self.saveFormula = function () {
        var data = { title: self.formulaForm.title(), content: self.formulaForm.content() };
        if (!data.title || !data.content) { toastr.warning('Baslik ve icerik zorunludur'); return; }

        self.isSaving(true);
        $.ajax({
            url: '/proxy/sln-clients/' + id + '/formulas',
            method: 'POST',
            contentType: 'application/json',
            data: JSON.stringify(data)
        }).done(function () {
            formulaModal.hide();
            self.loadFormulas();
            toastr.success('Formul kaydedildi');
            self.isSaving(false);
        }).fail(function () {
            toastr.error('Formul kaydedilemedi');
            self.isSaving(false);
        });
    };

    self.removeFormula = function (formula) {
        if (!confirm('Bu formulu silmek istediginize emin misiniz?')) return;
        $.ajax({
            url: '/proxy/sln-clients/' + id + '/formulas/' + formula.id,
            method: 'DELETE'
        }).done(function () {
            self.loadFormulas();
            toastr.success('Formul silindi');
        });
    };

    // Photo
    self.openPhotoUpload = function () {
        document.getElementById('photoFile').value = '';
        photoModal.show();
    };

    self.uploadPhoto = function () {
        var fileInput = document.getElementById('photoFile');
        if (!fileInput.files.length) { toastr.warning('Dosya seciniz'); return; }

        var formData = new FormData();
        formData.append('file', fileInput.files[0]);

        self.isSaving(true);
        $.ajax({
            url: '/proxy/sln-clients/' + id + '/photos',
            method: 'POST',
            data: formData,
            processData: false,
            contentType: false
        }).done(function () {
            photoModal.hide();
            self.loadPhotos();
            toastr.success('Fotograf yuklendi');
            self.isSaving(false);
        }).fail(function () {
            toastr.error('Fotograf yuklenemedi');
            self.isSaving(false);
        });
    };

    self.removePhoto = function (photo) {
        if (!confirm('Bu fotografi silmek istediginize emin misiniz?')) return;
        $.ajax({
            url: '/proxy/sln-clients/' + id + '/photos/' + photo.id,
            method: 'DELETE'
        }).done(function () {
            self.loadPhotos();
            toastr.success('Fotograf silindi');
        });
    };

    $(document).ready(function () {
        formulaModal = new bootstrap.Modal(document.getElementById('formulaModal'));
        photoModal = new bootstrap.Modal(document.getElementById('photoModal'));
        self.loadClient();
        self.loadAppointments();
        self.loadInvoices();
        self.loadFormulas();
        self.loadPhotos();
    });
}

ko.applyBindings(new ClientDetailViewModel(), document.getElementById('client-detail-vm'));
