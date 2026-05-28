function SalonEmailCampaignsViewModel() {
    var self = this;
    var t = window.crmT || function (key, fallback) { return fallback || key; };

    self.campaigns = ko.observableArray([]);
    self.isSaving = ko.observable(false);
    self.isEditing = ko.observable(false);
    self.editingId = ko.observable(null);

    self.form = {
        subject: ko.observable(''),
        htmlBody: ko.observable(''),
        scheduledAt: ko.observable('')
    };

    var formModal;
    var statusTexts = {
        1: t('crm.common.status.draft', 'Taslak'),
        2: t('crm.common.status.scheduled', 'Planli'),
        3: t('crm.common.status.sending', 'Gonderiliyor'),
        4: t('crm.common.status.completed', 'Tamamlandi')
    };
    var statusBadges = { 1: 'bg-secondary', 2: 'bg-info', 3: 'bg-warning text-dark', 4: 'bg-success' };

    function readError(xhr, fallback) {
        return xhr.responseJSON?.error || xhr.responseJSON?.message || xhr.responseText || fallback;
    }

    function items(data) {
        return data && Array.isArray(data.items) ? data.items : (Array.isArray(data) ? data : []);
    }

    self.statusText = function (statusId) {
        return statusTexts[statusId] || t('crm.common.unknown', 'Bilinmiyor');
    };

    self.statusClass = function (statusId) {
        return statusBadges[statusId] || 'bg-secondary';
    };

    self.loadData = function () {
        $.get('/proxy/crm/salon/email-campaigns')
            .done(function (data) { self.campaigns(items(data)); })
            .fail(function (xhr) { toastr.error(readError(xhr, t('crm.salon.emailcampaigns.load_failed', 'E-posta kampanyalari yuklenemedi'))); });
    };

    self.openNew = function () {
        self.isEditing(false);
        self.editingId(null);
        self.form.subject('');
        self.form.htmlBody('');
        self.form.scheduledAt('');
        formModal.show();
    };

    self.edit = function (campaign) {
        self.isEditing(true);
        self.editingId(campaign.id);
        self.form.subject(campaign.subject || '');
        self.form.htmlBody(campaign.htmlBody || '');
        self.form.scheduledAt(campaign.scheduledAt ? campaign.scheduledAt.substring(0, 16) : '');
        formModal.show();
    };

    self.save = function () {
        var payload = {
            subject: self.form.subject(),
            htmlBody: self.form.htmlBody(),
            segmentFilter: null,
            scheduledAt: self.form.scheduledAt() ? self.form.scheduledAt() + ':00Z' : null
        };

        if (!payload.subject || !payload.htmlBody) {
            toastr.warning(t('crm.salon.emailcampaigns.required', 'Konu ve icerik zorunludur'));
            return;
        }

        var url = '/proxy/crm/salon/email-campaigns?allBranches=true';
        var method = 'POST';
        if (self.isEditing()) {
            url = '/proxy/crm/salon/email-campaigns/' + self.editingId() + '?allBranches=true';
            method = 'PUT';
        }

        self.isSaving(true);
        $.ajax({ url: url, method: method, contentType: 'application/json', data: JSON.stringify(payload) })
            .done(function () {
                formModal.hide();
                self.loadData();
                toastr.success(t('crm.salon.emailcampaigns.saved', 'E-posta kampanyasi kaydedildi'));
            })
            .fail(function (xhr) { toastr.error(readError(xhr, t('crm.salon.emailcampaigns.save_failed', 'E-posta kampanyasi kaydedilemedi'))); })
            .always(function () { self.isSaving(false); });
    };

    self.send = function (campaign) {
        confirmModal(t('crm.common.confirm', 'Onayla'), t('crm.salon.emailcampaigns.send_confirm', "'{subject}' kampanyasi gonderilsin mi?").replace('{subject}', campaign.subject || ''), function () {
            $.ajax({ url: '/proxy/crm/salon/email-campaigns/' + campaign.id + '/send', method: 'POST' })
                .done(function () {
                    self.loadData();
                    toastr.success(t('crm.salon.emailcampaigns.sent_success', 'E-posta kampanyasi gonderildi'));
                })
                .fail(function (xhr) { toastr.error(readError(xhr, t('crm.salon.emailcampaigns.send_failed', 'E-posta kampanyasi gonderilemedi'))); });
        });
    };

    self.remove = function (campaign) {
        confirmModal(t('crm.common.confirm', 'Onayla'), t('crm.salon.emailcampaigns.delete_confirm', 'Bu e-posta kampanyasini silmek istediginize emin misiniz?'), function () {
            $.ajax({ url: '/proxy/crm/salon/email-campaigns/' + campaign.id, method: 'DELETE' })
                .done(function () {
                    self.loadData();
                    toastr.success(t('crm.salon.emailcampaigns.deleted', 'E-posta kampanyasi silindi'));
                })
                .fail(function (xhr) { toastr.error(readError(xhr, t('crm.common.delete_failed', 'Silinemedi'))); });
        });
    };

    $(document).ready(function () {
        formModal = new bootstrap.Modal(document.getElementById('salonEmailCampaignModal'));
        self.loadData();
    });
}

ko.applyBindings(new SalonEmailCampaignsViewModel(), document.getElementById('salon-emailcampaigns-vm'));
