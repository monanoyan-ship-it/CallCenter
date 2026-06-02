function SalonWinbackViewModel() {
    var self = this;
    var t = window.crmT || function (key, fallback) { return fallback || key; };

    self.rules = ko.observableArray([]);
    self.isSaving = ko.observable(false);
    self.isEditing = ko.observable(false);
    self.editingId = ko.observable(null);

    self.form = {
        name: ko.observable(''),
        inactiveDays: ko.observable(30),
        channelId: ko.observable('1'),
        messageTemplate: ko.observable(''),
        discountPercent: ko.observable(''),
        isActive: ko.observable(true)
    };

    var formModal;
    var channelTexts = {
        1: 'SMS',
        2: 'WhatsApp',
        3: t('crm.common.email', 'E-posta')
    };

    function readError(xhr, fallback) {
        return xhr.responseJSON?.error || xhr.responseJSON?.message || xhr.responseText || fallback;
    }

    function items(data) {
        return data && Array.isArray(data.items) ? data.items : (Array.isArray(data) ? data : []);
    }

    self.channelText = function (channelId) {
        return channelTexts[channelId] || t('crm.common.unknown', 'Bilinmiyor');
    };

    self.loadData = function () {
        $.get('/proxy/crm/salon/winback')
            .done(function (data) { self.rules(items(data)); })
            .fail(function (xhr) { toastr.error(readError(xhr, t('crm.salon.winback.load_failed', 'Geri kazanım planları yüklenemedi'))); });
    };

    self.openNew = function () {
        self.isEditing(false);
        self.editingId(null);
        self.form.name('');
        self.form.inactiveDays(30);
        self.form.channelId('1');
        self.form.messageTemplate('');
        self.form.discountPercent('');
        self.form.isActive(true);
        formModal.show();
    };

    self.edit = function (rule) {
        self.isEditing(true);
        self.editingId(rule.id);
        self.form.name(rule.name || '');
        self.form.inactiveDays(rule.inactiveDays || 30);
        self.form.channelId(String(rule.channelId || 1));
        self.form.messageTemplate(rule.messageTemplate || '');
        self.form.discountPercent(rule.discountPercent || '');
        self.form.isActive(rule.isActive !== false);
        formModal.show();
    };

    self.save = function () {
        var discount = self.form.discountPercent();
        var payload = {
            name: self.form.name(),
            inactiveDays: parseInt(self.form.inactiveDays(), 10) || 30,
            channelId: parseInt(self.form.channelId(), 10) || 1,
            messageTemplate: self.form.messageTemplate(),
            discountPercent: discount ? parseInt(discount, 10) : null,
            isActive: self.form.isActive() === true
        };

        if (!payload.name || !payload.messageTemplate) {
            toastr.warning(t('crm.salon.winback.required', 'Ad ve mesaj zorunludur'));
            return;
        }

        var url = '/proxy/crm/salon/winback';
        var method = 'POST';
        if (self.isEditing()) {
            url = '/proxy/crm/salon/winback/' + self.editingId();
            method = 'PUT';
        }

        self.isSaving(true);
        $.ajax({ url: url, method: method, contentType: 'application/json', data: JSON.stringify(payload) })
            .done(function () {
                formModal.hide();
                self.loadData();
                toastr.success(t('crm.salon.winback.saved', 'Geri kazanım planı kaydedildi'));
            })
            .fail(function (xhr) { toastr.error(readError(xhr, t('crm.salon.winback.save_failed', 'Geri kazanım planı kaydedilemedi'))); })
            .always(function () { self.isSaving(false); });
    };

    self.previewRule = function (rule) {
        $.get('/proxy/crm/salon/winback/' + rule.id + '/preview')
            .done(function (data) {
                var msg = t('crm.salon.winback.preview_text', 'Uygun müşteri: {eligible}, SMS: {sms}, e-posta: {email}')
                    .replace('{eligible}', data.eligibleClients || 0)
                    .replace('{sms}', data.smsReachableClients || 0)
                    .replace('{email}', data.emailReachableClients || 0);
                toastr.info(msg, rule.name || t('crm.salon.winback.preview_title', 'Önizleme'));
            })
            .fail(function (xhr) { toastr.error(readError(xhr, t('crm.salon.winback.preview_failed', 'Önizleme alınamadı'))); });
    };

    self.createCampaign = function (rule) {
        confirmModal(t('crm.common.confirm', 'Onayla'), t('crm.salon.winback.create_campaign_confirm', "'{name}' planından kampanya oluşturulsun mu?").replace('{name}', rule.name || ''), function () {
            $.ajax({ url: '/proxy/crm/salon/winback/' + rule.id + '/create-campaign', method: 'POST' })
                .done(function () { toastr.success(t('crm.salon.winback.campaign_created', 'Geri kazanım kampanyası oluşturuldu')); })
                .fail(function (xhr) { toastr.error(readError(xhr, t('crm.salon.winback.campaign_create_failed', 'Kampanya oluşturulamadı'))); });
        });
    };

    self.toggle = function (rule) {
        $.ajax({ url: '/proxy/crm/salon/winback/' + rule.id + '/toggle', method: 'POST' })
            .done(function () {
                self.loadData();
                toastr.success(t('crm.common.status_changed', 'Durum değiştirildi'));
            })
            .fail(function (xhr) { toastr.error(readError(xhr, t('crm.common.status_change_failed', 'Durum değiştirilemedi'))); });
    };

    self.remove = function (rule) {
        confirmModal(t('crm.common.confirm', 'Onayla'), t('crm.salon.winback.delete_confirm', 'Bu geri kazanım planını silmek istediğinize emin misiniz?'), function () {
            $.ajax({ url: '/proxy/crm/salon/winback/' + rule.id, method: 'DELETE' })
                .done(function () {
                    self.loadData();
                    toastr.success(t('crm.salon.winback.deleted', 'Geri kazanım planı silindi'));
                })
                .fail(function (xhr) { toastr.error(readError(xhr, t('crm.common.delete_failed', 'Silinemedi'))); });
        });
    };

    $(document).ready(function () {
        formModal = new bootstrap.Modal(document.getElementById('salonWinbackModal'));
        self.loadData();
    });
}

ko.applyBindings(new SalonWinbackViewModel(), document.getElementById('salon-winback-vm'));
