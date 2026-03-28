function ClientDetailViewModel() {
    var self = this;
    var id = clientDetailId;

    self.client = ko.observable({});
    self.formulas = ko.observableArray([]);
    self.photos = ko.observableArray([]);
    self.appointments = ko.observableArray([]);
    self.invoices = ko.observableArray([]);
    self.totalSpent = ko.observable(0);
    self.lastVisit = ko.observable(null);
    self.isSaving = ko.observable(false);

    self.formulaForm = {
        formulaText: ko.observable(''),
        colorCode: ko.observable(''),
        oxidantRatio: ko.observable(''),
        applicationNotes: ko.observable('')
    };

    var formulaModal, photoModal;

    var appointmentStatusNames = { 1: 'Planlanmis', 2: 'Onaylandi', 3: 'Tamamlandi', 4: 'Iptal', 5: 'Gelmedi' };
    var appointmentStatusCss = { 1: 'bg-warning text-dark', 2: 'bg-info', 3: 'bg-success', 4: 'bg-danger', 5: 'bg-secondary' };
    var invoiceStatusNames = { 1: 'Acik', 2: 'Odendi', 3: 'Iptal' };
    var invoiceStatusCss = { 1: 'bg-warning text-dark', 2: 'bg-success', 3: 'bg-danger' };

    self.loadClient = function () {
        $.ajax({ url: '/proxy/sln-clients/' + id, method: 'GET' }).done(function (data) {
            data.genderText = data.genderId === 1 ? 'Erkek' : data.genderId === 2 ? 'Kadin' : '';
            if (data.birthDate) {
                var bd = new Date(data.birthDate);
                var today = new Date();
                data.age = today.getFullYear() - bd.getFullYear();
                if (today.getMonth() < bd.getMonth() || (today.getMonth() === bd.getMonth() && today.getDate() < bd.getDate())) data.age--;
            }
            self.client(data);
            self.formulas(data.formulas || []);
            self.photos(data.photos || []);
            self.totalSpent(data.totalSpent || 0);
            self.lastVisit(data.lastVisit ? new Date(data.lastVisit).toLocaleDateString('tr-TR') : null);
        }).fail(function () {
            toastr.error('Musteri bilgisi yuklenemedi');
        });
    };

    self.loadAppointments = function () {
        $.ajax({ url: '/proxy/sln-appointments?slnClientId=' + id, method: 'GET' }).done(function (data) {
            var items = data.items || data;
            items.forEach(function (a) {
                a.statusText = appointmentStatusNames[a.statusId] || 'Bilinmiyor';
                a.statusCss = appointmentStatusCss[a.statusId] || 'bg-secondary';
            });
            self.appointments(items);
        });
    };

    self.loadInvoices = function () {
        $.ajax({ url: '/proxy/sln-finance/invoices?slnClientId=' + id, method: 'GET' }).done(function (data) {
            var items = data.items || data;
            items.forEach(function (inv) {
                inv.statusText = invoiceStatusNames[inv.statusId] || 'Bilinmiyor';
                inv.statusCss = invoiceStatusCss[inv.statusId] || 'bg-secondary';
                inv.servicesSummary = (inv.items || []).map(function (it) { return it.itemName; }).join(', ') || '-';
            });
            self.invoices(items);
        });
    };

    // Formula CRUD
    self.openNewFormula = function () {
        self.formulaForm.formulaText('');
        self.formulaForm.colorCode('');
        self.formulaForm.oxidantRatio('');
        self.formulaForm.applicationNotes('');
        formulaModal.show();
    };

    self.saveFormula = function () {
        var data = {
            slnClientId: id,
            formulaText: self.formulaForm.formulaText(),
            colorCode: self.formulaForm.colorCode(),
            oxidantRatio: self.formulaForm.oxidantRatio(),
            applicationNotes: self.formulaForm.applicationNotes()
        };
        if (!data.formulaText) { toastr.warning('Formul metni zorunludur'); return; }

        self.isSaving(true);
        $.ajax({
            url: '/proxy/sln-clients/formulas',
            method: 'POST',
            contentType: 'application/json',
            data: JSON.stringify(data)
        }).done(function () {
            formulaModal.hide();
            self.loadClient();
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
            url: '/proxy/sln-clients/formulas/' + formula.id,
            method: 'DELETE'
        }).done(function () {
            self.loadClient();
            toastr.success('Formul silindi');
        });
    };

    // Photo CRUD
    self.openPhotoUpload = function () {
        document.getElementById('photoFile').value = '';
        photoModal.show();
    };

    self.uploadPhoto = function () {
        var fileInput = document.getElementById('photoFile');
        if (!fileInput.files || !fileInput.files[0]) {
            toastr.warning('Lutfen bir fotograf seciniz');
            return;
        }

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
            self.loadClient();
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
            self.loadClient();
            toastr.success('Fotograf silindi');
        });
    };

    $(document).ready(function () {
        formulaModal = new bootstrap.Modal(document.getElementById('formulaModal'));
        photoModal = new bootstrap.Modal(document.getElementById('photoModal'));
        self.loadClient();
        self.loadAppointments();
        self.loadInvoices();
    });
}

ko.applyBindings(new ClientDetailViewModel(), document.getElementById('client-detail-vm'));
