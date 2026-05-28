function SalonCampaignsViewModel() {
    var self = this;
    var t = window.crmT || function (key, fallback) { return fallback || key; };

    self.campaigns = ko.observableArray([]);
    self.reminders = ko.observableArray([]);
    self.isSaving = ko.observable(false);
    self.isEditingCampaign = ko.observable(false);
    self.editingCampaignId = ko.observable(null);
    self.isEditingReminder = ko.observable(false);
    self.editingReminderId = ko.observable(null);

    self.campaignForm = {
        name: ko.observable(''),
        messageTemplate: ko.observable(''),
        scheduledAt: ko.observable('')
    };

    self.reminderForm = {
        reminderTypeId: ko.observable('1'),
        messageTemplate: ko.observable(''),
        daysBefore: ko.observable(1),
        inactiveDaysThreshold: ko.observable(30),
        isActive: ko.observable(true)
    };

    var campaignModal;
    var reminderModal;
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

    self.dateText = function (value) {
        if (!value) return '-';
        var d = new Date(value);
        return isNaN(d.getTime()) ? '-' : d.toLocaleString(document.documentElement.lang || undefined);
    };

    self.loadCampaigns = function () {
        $.get('/proxy/crm/salon/campaigns')
            .done(function (data) { self.campaigns(items(data)); })
            .fail(function (xhr) { toastr.error(readError(xhr, t('crm.salon.campaigns.load_failed', 'Kampanyalar yuklenemedi'))); });
    };

    self.loadReminders = function () {
        $.get('/proxy/crm/salon/reminders')
            .done(function (data) { self.reminders(items(data)); })
            .fail(function (xhr) { toastr.error(readError(xhr, t('crm.salon.campaigns.reminders_load_failed', 'Hatirlatmalar yuklenemedi'))); });
    };

    self.openCampaign = function () {
        self.isEditingCampaign(false);
        self.editingCampaignId(null);
        self.campaignForm.name('');
        self.campaignForm.messageTemplate('');
        self.campaignForm.scheduledAt('');
        campaignModal.show();
    };

    self.editCampaign = function (campaign) {
        self.isEditingCampaign(true);
        self.editingCampaignId(campaign.id);
        self.campaignForm.name(campaign.name || '');
        self.campaignForm.messageTemplate(campaign.messageTemplate || '');
        self.campaignForm.scheduledAt(campaign.scheduledAt ? campaign.scheduledAt.substring(0, 16) : '');
        campaignModal.show();
    };

    self.saveCampaign = function () {
        var payload = {
            name: self.campaignForm.name(),
            messageTemplate: self.campaignForm.messageTemplate(),
            segmentFilter: null,
            scheduledAt: self.campaignForm.scheduledAt() ? self.campaignForm.scheduledAt() + ':00Z' : null
        };

        if (!payload.name || !payload.messageTemplate) {
            toastr.warning(t('crm.salon.campaigns.required', 'Ad ve mesaj zorunludur'));
            return;
        }

        var url = '/proxy/crm/salon/campaigns?allBranches=true';
        var method = 'POST';
        if (self.isEditingCampaign()) {
            url = '/proxy/crm/salon/campaigns/' + self.editingCampaignId() + '?allBranches=true';
            method = 'PUT';
        }

        self.isSaving(true);
        $.ajax({ url: url, method: method, contentType: 'application/json', data: JSON.stringify(payload) })
            .done(function () {
                campaignModal.hide();
                self.loadCampaigns();
                toastr.success(t('crm.salon.campaigns.saved', 'Kampanya kaydedildi'));
            })
            .fail(function (xhr) { toastr.error(readError(xhr, t('crm.salon.campaigns.save_failed', 'Kampanya kaydedilemedi'))); })
            .always(function () { self.isSaving(false); });
    };

    self.sendCampaign = function (campaign) {
        confirmModal(t('crm.common.confirm', 'Onayla'), t('crm.salon.campaigns.send_confirm', "'{name}' kampanyasi gonderilsin mi?").replace('{name}', campaign.name || ''), function () {
            $.ajax({ url: '/proxy/crm/salon/campaigns/' + campaign.id + '/send', method: 'POST' })
                .done(function () {
                    self.loadCampaigns();
                    toastr.success(t('crm.salon.campaigns.sent_success', 'Kampanya gonderildi'));
                })
                .fail(function (xhr) { toastr.error(readError(xhr, t('crm.salon.campaigns.send_failed', 'Kampanya gonderilemedi'))); });
        });
    };

    self.deleteCampaign = function (campaign) {
        confirmModal(t('crm.common.confirm', 'Onayla'), t('crm.salon.campaigns.delete_confirm', 'Bu kampanyayi silmek istediginize emin misiniz?'), function () {
            $.ajax({ url: '/proxy/crm/salon/campaigns/' + campaign.id, method: 'DELETE' })
                .done(function () {
                    self.loadCampaigns();
                    toastr.success(t('crm.salon.campaigns.deleted', 'Kampanya silindi'));
                })
                .fail(function (xhr) { toastr.error(readError(xhr, t('crm.common.delete_failed', 'Silinemedi'))); });
        });
    };

    self.openReminder = function () {
        self.isEditingReminder(false);
        self.editingReminderId(null);
        self.reminderForm.reminderTypeId('1');
        self.reminderForm.messageTemplate('');
        self.reminderForm.daysBefore(1);
        self.reminderForm.inactiveDaysThreshold(30);
        self.reminderForm.isActive(true);
        reminderModal.show();
    };

    self.editReminder = function (reminder) {
        self.isEditingReminder(true);
        self.editingReminderId(reminder.id);
        self.reminderForm.reminderTypeId(String(reminder.reminderTypeId || 1));
        self.reminderForm.messageTemplate(reminder.messageTemplate || '');
        self.reminderForm.daysBefore(reminder.daysBefore || 0);
        self.reminderForm.inactiveDaysThreshold(reminder.inactiveDaysThreshold || 0);
        self.reminderForm.isActive(reminder.isActive !== false);
        reminderModal.show();
    };

    self.saveReminder = function () {
        var payload = {
            reminderTypeId: parseInt(self.reminderForm.reminderTypeId(), 10) || 1,
            messageTemplate: self.reminderForm.messageTemplate(),
            daysBefore: parseInt(self.reminderForm.daysBefore(), 10) || 0,
            inactiveDaysThreshold: parseInt(self.reminderForm.inactiveDaysThreshold(), 10) || 0,
            isActive: self.reminderForm.isActive() === true
        };

        if (!payload.messageTemplate) {
            toastr.warning(t('crm.salon.campaigns.message_required', 'Mesaj zorunludur'));
            return;
        }

        var url = '/proxy/crm/salon/reminders?allBranches=true';
        var method = 'POST';
        if (self.isEditingReminder()) {
            url = '/proxy/crm/salon/reminders/' + self.editingReminderId() + '?allBranches=true';
            method = 'PUT';
        }

        self.isSaving(true);
        $.ajax({ url: url, method: method, contentType: 'application/json', data: JSON.stringify(payload) })
            .done(function () {
                reminderModal.hide();
                self.loadReminders();
                toastr.success(t('crm.salon.campaigns.reminder_saved', 'Hatirlatma kaydedildi'));
            })
            .fail(function (xhr) { toastr.error(readError(xhr, t('crm.salon.campaigns.reminder_save_failed', 'Hatirlatma kaydedilemedi'))); })
            .always(function () { self.isSaving(false); });
    };

    self.toggleReminder = function (reminder) {
        $.ajax({ url: '/proxy/crm/salon/reminders/' + reminder.id + '/toggle', method: 'POST' })
            .done(function () {
                self.loadReminders();
                toastr.success(t('crm.common.status_changed', 'Durum degistirildi'));
            })
            .fail(function (xhr) { toastr.error(readError(xhr, t('crm.common.status_change_failed', 'Durum degistirilemedi'))); });
    };

    self.deleteReminder = function (reminder) {
        confirmModal(t('crm.common.confirm', 'Onayla'), t('crm.salon.campaigns.reminder_delete_confirm', 'Bu hatirlatmayi silmek istediginize emin misiniz?'), function () {
            $.ajax({ url: '/proxy/crm/salon/reminders/' + reminder.id, method: 'DELETE' })
                .done(function () {
                    self.loadReminders();
                    toastr.success(t('crm.salon.campaigns.reminder_deleted', 'Hatirlatma silindi'));
                })
                .fail(function (xhr) { toastr.error(readError(xhr, t('crm.common.delete_failed', 'Silinemedi'))); });
        });
    };

    $(document).ready(function () {
        campaignModal = new bootstrap.Modal(document.getElementById('salonCampaignModal'));
        reminderModal = new bootstrap.Modal(document.getElementById('salonReminderModal'));
        self.loadCampaigns();
        self.loadReminders();
    });
}

ko.applyBindings(new SalonCampaignsViewModel(), document.getElementById('salon-campaigns-vm'));
