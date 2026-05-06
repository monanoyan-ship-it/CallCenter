function slnJsT(key, fallback) {
    return (window.salonT || function (k, f) { return f || k; })(key, fallback);
}

function ConsentFormsViewModel() {
    var self = this;
    self.forms = ko.observableArray([]);
    self.signedConsents = ko.observableArray([]);
    self.isEditing = ko.observable(false);
    self.editingId = ko.observable(null);
    self.isSaving = ko.observable(false);

    self.form = {
        title: ko.observable(''),
        htmlContent: ko.observable(''),
        requireSignature: ko.observable(false),
        isActive: ko.observable(true)
    };

    var formModal;

    self.loadForms = function () {
        $.ajax({ url: '/proxy/sln-consent-forms', method: 'GET' }).done(function (data) {
            self.forms(data.items || data);
        });
    };

    self.loadConsents = function () {
        $.ajax({ url: '/proxy/sln-consent-forms/consents', method: 'GET' }).done(function (data) {
            self.signedConsents(data.items || data);
        });
    };

    self.resetForm = function () {
        self.form.title('');
        self.form.htmlContent('');
        self.form.requireSignature(false);
        self.form.isActive(true);
        self.isEditing(false);
        self.editingId(null);
    };

    self.openNewForm = function () {
        self.resetForm();
        formModal.show();
    };

    self.openEditForm = function (form) {
        self.isEditing(true);
        self.editingId(form.id);
        self.form.title(form.title);
        self.form.htmlContent(form.htmlContent);
        self.form.requireSignature(form.requireSignature);
        self.form.isActive(form.isActive);
        formModal.show();
    };

    self.saveForm = function () {
        var data = {
            title: self.form.title(),
            htmlContent: self.form.htmlContent(),
            requireSignature: self.form.requireSignature(),
            isActive: self.form.isActive()
        };

        if (!data.title || !data.htmlContent) {
            toastr.warning(slnJsT('salon.consentforms.js.baslik_ve_icerik_zorunludur', 'Baslik ve icerik zorunludur'));
            return;
        }

        self.isSaving(true);
        var url = '/proxy/sln-consent-forms';
        var method = 'POST';
        if (self.isEditing()) {
            url += '/' + self.editingId();
            method = 'PUT';
        }

        $.ajax({ url: url, method: method, contentType: 'application/json', data: JSON.stringify(data) })
            .done(function () {
                formModal.hide();
                self.loadForms();
                toastr.success(self.isEditing() ? slnJsT('salon.consentforms.js.form_guncellendi', 'Form güncellendi') : slnJsT('salon.consentforms.js.form_olusturuldu', 'Form oluşturuldu'));
                self.isSaving(false);
            }).fail(function (xhr) {
                toastr.error(xhr.responseJSON || slnJsT('salon.common.error.generic', 'Bir hata oluştu'));
                self.isSaving(false);
            });
    };

    self.removeForm = function (form) {
        confirmModal(slnJsT('salon.common.btn.confirm', 'Onayla'), slnJsT('salon.consentforms.js.bu_formu_silmek_istediginize_emin_misiniz', 'Bu formu silmek istediğinize emin misiniz?'), function() {
            $.ajax({ url: '/proxy/sln-consent-forms/' + form.id, method: 'DELETE' })
                .done(function () { self.loadForms(); toastr.success(slnJsT('salon.consentforms.js.form_silindi', 'Form silindi')); })
                .fail(function () { toastr.error('Silinemedi'); });
        });
    };

    $(document).ready(function () {
        formModal = new bootstrap.Modal(document.getElementById('consentFormModal'));
        self.loadForms();
        self.loadConsents();
    });
}

ko.applyBindings(new ConsentFormsViewModel(), document.getElementById('consentforms-vm'));
