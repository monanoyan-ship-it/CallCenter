function slnJsT(key, fallback) {
    return (window.salonT || function (k, f) { return f || k; })(key, fallback);
}

function BeforeAfterViewModel() {
    var self = this;
    self.photos = ko.observableArray([]);
    self.clients = ko.observableArray([]);
    self.services = ko.observableArray([]);
    self.searchQuery = ko.observable('');
    self.isSaving = ko.observable(false);

    self.form = {
        slnClientId: ko.observable(''),
        serviceId: ko.observable(''),
        beforePhotoUrl: ko.observable(''),
        afterPhotoUrl: ko.observable(''),
        notes: ko.observable(''),
        isPublic: ko.observable(false)
    };

    self.filteredPhotos = ko.computed(function () {
        var q = (self.searchQuery() || '').toLowerCase();
        if (!q) return self.photos();
        return self.photos().filter(function (p) {
            return (p.clientName || '').toLowerCase().indexOf(q) >= 0
                || (p.serviceName || '').toLowerCase().indexOf(q) >= 0;
        });
    });

    var formModal;

    self.loadData = function () {
        $.ajax({ url: '/proxy/sln-before-after', method: 'GET' }).done(function (data) {
            self.photos(data.items || data);
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
    };

    self.openNew = function () {
        self.form.slnClientId('');
        self.form.serviceId('');
        self.form.beforePhotoUrl('');
        self.form.afterPhotoUrl('');
        self.form.notes('');
        self.form.isPublic(false);
        formModal.show();
    };

    self.save = function () {
        var data = {
            slnClientId: parseInt(self.form.slnClientId()) || 0,
            serviceId: self.form.serviceId() ? parseInt(self.form.serviceId()) : null,
            beforePhotoUrl: self.form.beforePhotoUrl() || null,
            afterPhotoUrl: self.form.afterPhotoUrl() || null,
            notes: self.form.notes() || null,
            isPublic: self.form.isPublic()
        };

        if (!data.slnClientId) {
            toastr.warning(slnJsT('salon.beforeafter.js.musteri_secimi_zorunludur', 'Müşteri seçimi zorunludur'));
            return;
        }

        self.isSaving(true);
        $.ajax({
            url: '/proxy/sln-before-after', method: 'POST',
            contentType: 'application/json', data: JSON.stringify(data)
        }).done(function () {
            formModal.hide();
            self.loadData();
            toastr.success(slnJsT('salon.beforeafter.js.fotograf_eklendi', 'Fotoğraf eklendi'));
            self.isSaving(false);
        }).fail(function (xhr) {
            toastr.error(xhr.responseJSON || slnJsT('salon.common.error.generic', 'Bir hata oluştu'));
            self.isSaving(false);
        });
    };

    self.remove = function (photo) {
        confirmModal(slnJsT('salon.common.btn.confirm', 'Onayla'), slnJsT('salon.beforeafter.js.bu_fotografi_silmek_istediginize_emin_misiniz', 'Bu fotoğrafı silmek istediğinize emin misiniz?'), function() {
            $.ajax({ url: '/proxy/sln-before-after/' + photo.id, method: 'DELETE' })
                .done(function () { self.loadData(); toastr.success(slnJsT('salon.beforeafter.js.fotograf_silindi', 'Fotoğraf silindi')); });
        });
    };

    $(document).ready(function () {
        formModal = new bootstrap.Modal(document.getElementById('beforeAfterModal'));
        self.loadData();
        self.loadLookups();
    });
}

ko.applyBindings(new BeforeAfterViewModel(), document.getElementById('beforeafter-vm'));
