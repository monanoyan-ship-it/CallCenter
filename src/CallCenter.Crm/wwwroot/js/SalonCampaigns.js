function SalonCampaignsViewModel() {
    var self = this;
    var t = window.crmT || function (key, fallback) { return fallback || key; };

    self.campaigns = ko.observableArray([]);
    self.reminders = ko.observableArray([]);
    self.inboxMessages = ko.observableArray([]);
    self.branches = ko.observableArray([]);
    self.segmentPresets = ko.observableArray([]);
    self.segmentPreviewCount = ko.observable(0);
    self.segmentSmsReachableCount = ko.observable(0);
    self.segmentMissingPhoneCount = ko.observable(0);
    self.segmentEstimatedSmsCost = ko.observable(0);
    self.isSaving = ko.observable(false);
    self.isEditingCampaign = ko.observable(false);
    self.editingCampaignId = ko.observable(null);
    self.isEditingReminder = ko.observable(false);
    self.editingReminderId = ko.observable(null);
    var allBranchesValue = '__all__';
    self.branchTargetOptions = ko.computed(function () {
        return [{ id: allBranchesValue, name: t('crm.salon.common.all_branches', 'Tüm Şubeler') }].concat(self.branches() || []);
    });

    self.campaignForm = {
        name: ko.observable(''),
        messageTemplate: ko.observable(''),
        scheduledAt: ko.observable(''),
        branchTarget: ko.observable(allBranchesValue),
        segmentFilter: ko.observable(null)
    };

    self.reminderForm = {
        reminderTypeId: ko.observable('1'),
        messageTemplate: ko.observable(''),
        daysBefore: ko.observable(1),
        inactiveDaysThreshold: ko.observable(30),
        branchTarget: ko.observable(allBranchesValue),
        isActive: ko.observable(true)
    };

    self.replyForm = {
        phone: ko.observable(''),
        message: ko.observable('')
    };

    var campaignModal;
    var reminderModal;
    var replyModal;
    var statusTexts = {
        1: t('crm.common.status.draft', 'Taslak'),
        2: t('crm.common.status.scheduled', 'Planlı'),
        3: t('crm.common.status.sending', 'Gönderiliyor'),
        4: t('crm.common.status.completed', 'Tamamlandı')
    };
    var statusBadges = { 1: 'bg-secondary', 2: 'bg-info', 3: 'bg-warning text-dark', 4: 'bg-success' };

    function readError(xhr, fallback) {
        return xhr.responseJSON?.error || xhr.responseJSON?.message || xhr.responseText || fallback;
    }

    function items(data) {
        return data && Array.isArray(data.items) ? data.items : (Array.isArray(data) ? data : []);
    }

    function resolveBranchTarget(value) {
        if (value === allBranchesValue) return { ok: true, allBranches: true, branchId: null };
        var branchId = parseInt(value, 10) || null;
        if (branchId) return { ok: true, allBranches: false, branchId: branchId };

        toastr.warning(t('crm.salon.common.branch_target_required', 'Şube seçin veya Tüm Şubeler seçeneğini seçin'));
        return { ok: false };
    }

    function appendBranchTarget(url, target) {
        var separator = url.indexOf('?') >= 0 ? '&' : '?';
        if (target.allBranches) return url + separator + 'allBranches=true';
        if (target.branchId) return url + separator + 'branchId=' + encodeURIComponent(target.branchId);
        return url;
    }

    function setSegmentPreview(data) {
        data = data || {};
        self.segmentPreviewCount(data.matchingClients || 0);
        self.segmentSmsReachableCount(data.smsReachableClients || 0);
        self.segmentMissingPhoneCount(data.missingPhoneCount || 0);
        self.segmentEstimatedSmsCost(data.estimatedSmsCost || 0);
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

    self.money = function (value) {
        return (parseFloat(value) || 0).toLocaleString('tr-TR', { minimumFractionDigits: 2, maximumFractionDigits: 2 }) + ' TL';
    };

    self.loadBranches = function () {
        $.get('/proxy/crm/salon/branches')
            .done(function (data) { self.branches(items(data)); })
            .fail(function () { self.branches([]); });
    };

    self.loadCampaigns = function () {
        $.get('/proxy/crm/salon/campaigns')
            .done(function (data) { self.campaigns(items(data)); })
            .fail(function (xhr) { toastr.error(readError(xhr, t('crm.salon.campaigns.load_failed', 'Kampanyalar yüklenemedi'))); });
    };

    self.loadReminders = function () {
        $.get('/proxy/crm/salon/reminders')
            .done(function (data) { self.reminders(items(data)); })
            .fail(function (xhr) { toastr.error(readError(xhr, t('crm.salon.campaigns.reminders_load_failed', 'Hatırlatmalar yüklenemedi'))); });
    };

    self.loadSegmentPresets = function () {
        $.get('/proxy/crm/salon/campaigns/segment-presets')
            .done(function (data) { self.segmentPresets(items(data)); })
            .fail(function () { self.segmentPresets([]); });
    };

    self.loadInbox = function () {
        $.get('/proxy/crm/salon/whatsapp/messages?page=1&pageSize=100')
            .done(function (data) { self.inboxMessages(items(data)); })
            .fail(function (xhr) { toastr.error(readError(xhr, t('crm.salon.campaigns.inbox_load_failed', 'Mesaj kutusu yüklenemedi'))); });
    };

    self.openCampaign = function () {
        self.isEditingCampaign(false);
        self.editingCampaignId(null);
        self.campaignForm.name('');
        self.campaignForm.messageTemplate('');
        self.campaignForm.scheduledAt('');
        self.campaignForm.branchTarget(allBranchesValue);
        self.campaignForm.segmentFilter(null);
        setSegmentPreview({});
        campaignModal.show();
    };

    self.editCampaign = function (campaign) {
        self.isEditingCampaign(true);
        self.editingCampaignId(campaign.id);
        self.campaignForm.name(campaign.name || '');
        self.campaignForm.messageTemplate(campaign.messageTemplate || '');
        self.campaignForm.scheduledAt(campaign.scheduledAt ? campaign.scheduledAt.substring(0, 16) : '');
        self.campaignForm.branchTarget(campaign.branchId ? String(campaign.branchId) : allBranchesValue);
        self.campaignForm.segmentFilter(campaign.segmentFilter || null);
        setSegmentPreview({ matchingClients: campaign.totalRecipients, smsReachableClients: campaign.totalRecipients });
        campaignModal.show();
    };

    self.applySegmentPreset = function (preset) {
        self.campaignForm.segmentFilter(preset.filterJson || null);
        setSegmentPreview(preset);
    };

    self.previewSegment = function () {
        var target = resolveBranchTarget(self.campaignForm.branchTarget());
        if (!target.ok) return;

        $.ajax({
            url: appendBranchTarget('/proxy/crm/salon/campaigns/segment-preview', target),
            method: 'POST',
            contentType: 'application/json',
            data: JSON.stringify(self.campaignForm.segmentFilter())
        }).done(setSegmentPreview)
            .fail(function (xhr) { toastr.error(readError(xhr, t('crm.salon.campaigns.preview_failed', 'Önizleme alınamadı'))); });
    };

    self.saveCampaign = function () {
        var target = resolveBranchTarget(self.campaignForm.branchTarget());
        if (!target.ok) return;

        var payload = {
            name: self.campaignForm.name(),
            messageTemplate: self.campaignForm.messageTemplate(),
            segmentFilter: self.campaignForm.segmentFilter(),
            scheduledAt: self.campaignForm.scheduledAt() ? self.campaignForm.scheduledAt() + ':00Z' : null
        };

        if (!payload.name || !payload.messageTemplate) {
            toastr.warning(t('crm.salon.campaigns.required', 'Ad ve mesaj zorunludur'));
            return;
        }

        var url = appendBranchTarget('/proxy/crm/salon/campaigns', target);
        var method = 'POST';
        if (self.isEditingCampaign()) {
            url = appendBranchTarget('/proxy/crm/salon/campaigns/' + self.editingCampaignId(), target);
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
        confirmModal(t('crm.common.confirm', 'Onayla'), t('crm.salon.campaigns.send_confirm', "'{name}' kampanyası gönderilsin mi?").replace('{name}', campaign.name || ''), function () {
            $.ajax({ url: '/proxy/crm/salon/campaigns/' + campaign.id + '/send', method: 'POST' })
                .done(function () {
                    self.loadCampaigns();
                    toastr.success(t('crm.salon.campaigns.sent_success', 'Kampanya gönderildi'));
                })
                .fail(function (xhr) { toastr.error(readError(xhr, t('crm.salon.campaigns.send_failed', 'Kampanya gönderilemedi'))); });
        });
    };

    self.deleteCampaign = function (campaign) {
        confirmModal(t('crm.common.confirm', 'Onayla'), t('crm.salon.campaigns.delete_confirm', 'Bu kampanyayı silmek istediğinize emin misiniz?'), function () {
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
        self.reminderForm.branchTarget(allBranchesValue);
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
        self.reminderForm.branchTarget(reminder.branchId ? String(reminder.branchId) : allBranchesValue);
        self.reminderForm.isActive(reminder.isActive !== false);
        reminderModal.show();
    };

    self.saveReminder = function () {
        var target = resolveBranchTarget(self.reminderForm.branchTarget());
        if (!target.ok) return;

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

        var url = appendBranchTarget('/proxy/crm/salon/reminders', target);
        var method = 'POST';
        if (self.isEditingReminder()) {
            url = appendBranchTarget('/proxy/crm/salon/reminders/' + self.editingReminderId(), target);
            method = 'PUT';
        }

        self.isSaving(true);
        $.ajax({ url: url, method: method, contentType: 'application/json', data: JSON.stringify(payload) })
            .done(function () {
                reminderModal.hide();
                self.loadReminders();
                toastr.success(t('crm.salon.campaigns.reminder_saved', 'Hatırlatma kaydedildi'));
            })
            .fail(function (xhr) { toastr.error(readError(xhr, t('crm.salon.campaigns.reminder_save_failed', 'Hatırlatma kaydedilemedi'))); })
            .always(function () { self.isSaving(false); });
    };

    self.toggleReminder = function (reminder) {
        $.ajax({ url: '/proxy/crm/salon/reminders/' + reminder.id + '/toggle', method: 'POST' })
            .done(function () {
                self.loadReminders();
                toastr.success(t('crm.common.status_changed', 'Durum değiştirildi'));
            })
            .fail(function (xhr) { toastr.error(readError(xhr, t('crm.common.status_change_failed', 'Durum değiştirilemedi'))); });
    };

    self.deleteReminder = function (reminder) {
        confirmModal(t('crm.common.confirm', 'Onayla'), t('crm.salon.campaigns.reminder_delete_confirm', 'Bu hatırlatmayı silmek istediğinize emin misiniz?'), function () {
            $.ajax({ url: '/proxy/crm/salon/reminders/' + reminder.id, method: 'DELETE' })
                .done(function () {
                    self.loadReminders();
                    toastr.success(t('crm.salon.campaigns.reminder_deleted', 'Hatırlatma silindi'));
                })
                .fail(function (xhr) { toastr.error(readError(xhr, t('crm.common.delete_failed', 'Silinemedi'))); });
        });
    };

    self.openReply = function (message) {
        self.replyForm.phone(message.phoneNumber || '');
        self.replyForm.message('');
        replyModal.show();
    };

    self.sendReply = function () {
        var payload = {
            phone: self.replyForm.phone(),
            message: self.replyForm.message()
        };

        if (!payload.phone || !payload.message) {
            toastr.warning(t('crm.salon.campaigns.reply_required', 'Telefon ve mesaj zorunludur'));
            return;
        }

        self.isSaving(true);
        $.ajax({
            url: '/proxy/crm/salon/whatsapp/send-message',
            method: 'POST',
            contentType: 'application/json',
            data: JSON.stringify(payload)
        }).done(function () {
            replyModal.hide();
            self.loadInbox();
            toastr.success(t('crm.salon.campaigns.reply_sent', 'Mesaj gönderildi'));
        }).fail(function (xhr) {
            toastr.error(readError(xhr, t('crm.salon.campaigns.reply_failed', 'Mesaj gönderilemedi')));
        }).always(function () {
            self.isSaving(false);
        });
    };

    $(document).ready(function () {
        campaignModal = new bootstrap.Modal(document.getElementById('salonCampaignModal'));
        reminderModal = new bootstrap.Modal(document.getElementById('salonReminderModal'));
        replyModal = new bootstrap.Modal(document.getElementById('salonReplyModal'));
        self.loadBranches();
        self.loadCampaigns();
        self.loadSegmentPresets();
        self.loadReminders();
        self.loadInbox();
    });
}

ko.applyBindings(new SalonCampaignsViewModel(), document.getElementById('salon-campaigns-vm'));
