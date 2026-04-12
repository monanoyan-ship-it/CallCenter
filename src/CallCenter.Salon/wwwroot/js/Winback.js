function WinbackViewModel() {
    var self = this;
    self.rules = ko.observableArray([]);
    self.isEditing = ko.observable(false);
    self.editingId = ko.observable(null);
    self.isSaving = ko.observable(false);

    self.form = {
        name: ko.observable(''),
        inactiveDays: ko.observable(30),
        channelId: ko.observable('1'),
        messageTemplate: ko.observable(''),
        discountPercent: ko.observable(''),
        isActive: ko.observable(true)
    };

    var channelTexts = { 1: 'SMS', 2: 'WhatsApp', 3: 'E-posta' };
    self.channelText = function (id) { return channelTexts[id] || 'Bilinmiyor'; };

    var formModal;

    self.loadData = function () {
        $.ajax({ url: '/proxy/sln-winback', method: 'GET' }).done(function (data) {
            (data || []).forEach(function (r) {
                r.isActiveObs = ko.observable(r.isActive);
            });
            self.rules(data || []);
        });
    };

    self.resetForm = function () {
        self.form.name('');
        self.form.inactiveDays(30);
        self.form.channelId('1');
        self.form.messageTemplate('');
        self.form.discountPercent('');
        self.form.isActive(true);
        self.isEditing(false);
        self.editingId(null);
    };

    self.openNew = function () {
        self.resetForm();
        formModal.show();
    };

    self.openEdit = function (rule) {
        self.isEditing(true);
        self.editingId(rule.id);
        self.form.name(rule.name);
        self.form.inactiveDays(rule.inactiveDays);
        self.form.channelId(rule.channelId.toString());
        self.form.messageTemplate(rule.messageTemplate);
        self.form.discountPercent(rule.discountPercent || '');
        self.form.isActive(ko.unwrap(rule.isActiveObs));
        formModal.show();
    };

    self.save = function () {
        var data = {
            name: self.form.name(),
            inactiveDays: parseInt(self.form.inactiveDays()) || 30,
            channelId: parseInt(self.form.channelId()) || 1,
            messageTemplate: self.form.messageTemplate(),
            discountPercent: self.form.discountPercent() ? parseInt(self.form.discountPercent()) : null,
            isActive: self.form.isActive()
        };

        if (!data.name || !data.messageTemplate) {
            toastr.warning('Kural adi ve mesaj sablonu zorunludur');
            return;
        }

        self.isSaving(true);
        var url = '/proxy/sln-winback';
        var method = 'POST';
        if (self.isEditing()) {
            url += '/' + self.editingId();
            method = 'PUT';
        }

        $.ajax({ url: url, method: method, contentType: 'application/json', data: JSON.stringify(data) })
            .done(function () {
                formModal.hide();
                self.loadData();
                toastr.success(self.isEditing() ? 'Kural guncellendi' : 'Kural olusturuldu');
                self.isSaving(false);
            }).fail(function (xhr) {
                toastr.error(xhr.responseJSON || 'Bir hata olustu');
                self.isSaving(false);
            });
    };

    self.toggleRule = function (rule) {
        $.ajax({ url: '/proxy/sln-winback/' + rule.id + '/toggle', method: 'POST' })
            .done(function () { toastr.success('Kural durumu degistirildi'); })
            .fail(function () {
                rule.isActiveObs(!ko.unwrap(rule.isActiveObs));
                toastr.error('Durum degistirilemedi');
            });
        return true;
    };

    self.remove = function (rule) {
        confirmModal('Onay', 'Bu kurali silmek istediginize emin misiniz?', function() {
            $.ajax({ url: '/proxy/sln-winback/' + rule.id, method: 'DELETE' })
                .done(function () { self.loadData(); toastr.success('Kural silindi'); })
                .fail(function () { toastr.error('Silinemedi'); });
        });
    };

    $(document).ready(function () {
        formModal = new bootstrap.Modal(document.getElementById('winbackModal'));
        self.loadData();
    });
}

ko.applyBindings(new WinbackViewModel(), document.getElementById('winback-vm'));
