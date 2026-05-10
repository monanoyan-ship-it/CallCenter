function slnJsT(key, fallback) {
    return (window.salonT || function (k, f) { return f || k; })(key, fallback);
}

function WinbackViewModel() {
    var self = this;
    self.rules = ko.observableArray([]);
    self.isEditing = ko.observable(false);
    self.editingId = ko.observable(null);
    self.isSaving = ko.observable(false);
    self.preview = ko.observable(null);
    self.isPreviewLoading = ko.observable(false);

    self.form = {
        name: ko.observable(''),
        inactiveDays: ko.observable(30),
        channelId: ko.observable('1'),
        messageTemplate: ko.observable(''),
        discountPercent: ko.observable(''),
        isActive: ko.observable(true)
    };

    var channelTexts = {
        1: 'SMS',
        2: 'WhatsApp',
        3: slnJsT('salon.winback.channel.email', 'E-posta')
    };
    self.channelText = function (id) { return channelTexts[id] || slnJsT('salon.common.unknown', 'Bilinmiyor'); };

    var formModal;
    var previewModal;
    function readError(xhr) {
        if (typeof xhr.responseJSON === 'string') return xhr.responseJSON;
        return xhr.responseJSON?.error || xhr.responseJSON?.message || xhr.responseText || slnJsT('salon.common.error.generic', 'Bir hata oluştu');
    }

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
            toastr.warning(slnJsT('salon.winback.js.kural_adi_ve_mesaj_sablonu_zorunludur', 'Kural adı ve mesaj sablonu zorunludur'));
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
                toastr.success(self.isEditing() ? slnJsT('salon.winback.js.kural_guncellendi', 'Kural güncellendi') : slnJsT('salon.winback.js.kural_olusturuldu', 'Kural oluşturuldu'));
                self.isSaving(false);
            }).fail(function (xhr) {
                toastr.error(readError(xhr));
                self.isSaving(false);
            });
    };

    self.toggleRule = function (rule) {
        $.ajax({ url: '/proxy/sln-winback/' + rule.id + '/toggle', method: 'POST' })
            .done(function () { toastr.success(slnJsT('salon.winback.js.kural_durumu_degistirildi', 'Kural durumu degistirildi')); })
            .fail(function () {
                rule.isActiveObs(!ko.unwrap(rule.isActiveObs));
                toastr.error(slnJsT('salon.common.status_change_failed', 'Durum değiştirilemedi'));
            });
        return true;
    };

    self.remove = function (rule) {
        confirmModal(slnJsT('salon.common.btn.confirm', 'Onayla'), slnJsT('salon.winback.js.bu_kurali_silmek_istediginize_emin_misiniz', 'Bu kuralı silmek istediğinize emin misiniz?'), function() {
            $.ajax({ url: '/proxy/sln-winback/' + rule.id, method: 'DELETE' })
                .done(function () { self.loadData(); toastr.success(slnJsT('salon.winback.js.kural_silindi', 'Kural silindi')); })
                .fail(function () { toastr.error(slnJsT('salon.common.delete_failed', 'Silinemedi')); });
        });
    };

    self.openPreview = function (rule) {
        self.preview(null);
        self.isPreviewLoading(true);
        previewModal.show();
        $.ajax({ url: '/proxy/sln-winback/' + rule.id + '/preview', method: 'GET' })
            .done(function (data) { self.preview(data); })
            .fail(function (xhr) { toastr.error(readError(xhr)); })
            .always(function () { self.isPreviewLoading(false); });
    };

    self.createCampaign = function (rule) {
        confirmModal(slnJsT('salon.common.btn.confirm', 'Onayla'), slnJsT('salon.winback.js.create_campaign_confirm', "'{name}' kuralından kampanya oluşturulsun mu?").replace('{name}', rule.name || ''), function () {
            $.ajax({ url: '/proxy/sln-winback/' + rule.id + '/create-campaign', method: 'POST' })
                .done(function () { toastr.success(slnJsT('salon.winback.js.campaign_created', 'Winback kampanyası oluşturuldu')); })
                .fail(function (xhr) { toastr.error(readError(xhr)); });
        });
    };

    $(document).ready(function () {
        formModal = new bootstrap.Modal(document.getElementById('winbackModal'));
        previewModal = new bootstrap.Modal(document.getElementById('winbackPreviewModal'));
        self.loadData();
    });
}

ko.applyBindings(new WinbackViewModel(), document.getElementById('winback-vm'));
