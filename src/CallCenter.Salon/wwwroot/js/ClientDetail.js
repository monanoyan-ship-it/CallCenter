function ClientDetailViewModel() {
    var self = this;
    var id = clientDetailId;

    self.client = ko.observable({});
    self.formulas = ko.observableArray([]);
    self.isSaving = ko.observable(false);

    self.formulaForm = {
        formulaText: ko.observable(''),
        colorCode: ko.observable(''),
        oxidantRatio: ko.observable(''),
        applicationNotes: ko.observable('')
    };

    var formulaModal;

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
        }).fail(function () {
            toastr.error('Musteri bilgisi yuklenemedi');
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

    $(document).ready(function () {
        formulaModal = new bootstrap.Modal(document.getElementById('formulaModal'));
        self.loadClient();
    });
}

ko.applyBindings(new ClientDetailViewModel(), document.getElementById('client-detail-vm'));
