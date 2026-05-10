function slnJsT(key, fallback) {
    return (window.salonT || function (k, f) { return f || k; })(key, fallback);
}

function CampaignsViewModel() {
    var self = this;

    // Kampanyalar
    self.campaigns = ko.observableArray([]);
    self.isEditingCampaign = ko.observable(false);
    self.editingCampaignId = ko.observable(null);
    self.isSaving = ko.observable(false);
    self.segmentPreviewCount = ko.observable(0);
    self.segmentSmsReachableCount = ko.observable(0);
    self.segmentEmailReachableCount = ko.observable(0);
    self.segmentExcludedCount = ko.observable(0);
    self.segmentMissingPhoneCount = ko.observable(0);
    self.segmentEstimatedSmsCost = ko.observable(0);
    self.segmentPresets = ko.observableArray([]);

    self.campaignForm = {
        name: ko.observable(''),
        messageTemplate: ko.observable(''),
        scheduledAt: ko.observable(''),
        filter: {
            genderId: ko.observable(''),
            minAge: ko.observable(''),
            maxAge: ko.observable(''),
            city: ko.observable(''),
            lastVisitDays: ko.observable(''),
            inactiveDays: ko.observable(''),
            birthdayInDays: ko.observable(''),
            minSpent: ko.observable(''),
            hasActiveMembership: ko.observable(''),
            hasActivePackage: ko.observable('')
        }
    };

    // Hatirlatmalar
    self.reminders = ko.observableArray([]);
    self.isEditingReminder = ko.observable(false);
    self.editingReminderId = ko.observable(null);

    self.reminderForm = {
        reminderTypeId: ko.observable('1'),
        messageTemplate: ko.observable(''),
        daysBefore: ko.observable(0),
        inactiveDaysThreshold: ko.observable(0),
        isActive: ko.observable(true)
    };

    var campaignModal, reminderModal;
    var statusTexts = {
        1: slnJsT('salon.campaigns.status.draft', 'Taslak'),
        2: slnJsT('salon.campaigns.status.scheduled', 'Zamanlanmış'),
        3: slnJsT('salon.campaigns.status.sending', 'Gönderiliyor'),
        4: slnJsT('salon.campaigns.status.completed', 'Tamamlandı')
    };
    var statusBadges = { 1: 'bg-secondary', 2: 'bg-info', 3: 'bg-warning', 4: 'bg-success' };

    // ═══ Kampanya ═══

    self.loadCampaigns = function () {
        $.ajax({ url: '/proxy/sln-marketing/campaigns', method: 'GET' }).done(function (data) {
            (data || []).forEach(function (c) {
                c.statusText = statusTexts[c.statusId] || slnJsT('salon.common.unknown', 'Bilinmiyor');
                c.statusBadge = statusBadges[c.statusId] || 'bg-secondary';
            });
            self.campaigns(data || []);
        }).fail(function () {
            toastr.error(slnJsT('salon.campaigns.js.campaigns_load_failed', 'Kampanyalar yüklenemedi'));
        });
    };

    self.loadSegmentPresets = function () {
        $.ajax({ url: '/proxy/sln-marketing/campaigns/segment-presets', method: 'GET' }).done(function (data) {
            self.segmentPresets(data || []);
        }).fail(function () {
            toastr.error(slnJsT('salon.campaigns.js.segment_presets_load_failed', 'Hazır segmentler yüklenemedi'));
        });
    };

    self.buildSegmentFilter = function () {
        var f = {};
        var gid = self.campaignForm.filter.genderId();
        if (gid) f.genderId = parseInt(gid);
        var minAge = self.campaignForm.filter.minAge();
        if (minAge) f.minAge = parseInt(minAge);
        var maxAge = self.campaignForm.filter.maxAge();
        if (maxAge) f.maxAge = parseInt(maxAge);
        var city = self.campaignForm.filter.city();
        if (city) f.city = city;
        var lvd = self.campaignForm.filter.lastVisitDays();
        if (lvd) f.lastVisitDays = parseInt(lvd);
        var inactiveDays = self.campaignForm.filter.inactiveDays();
        if (inactiveDays) f.inactiveDays = parseInt(inactiveDays);
        var birthdayInDays = self.campaignForm.filter.birthdayInDays();
        if (birthdayInDays) f.birthdayInDays = parseInt(birthdayInDays);
        var ms = self.campaignForm.filter.minSpent();
        if (ms) f.minSpent = parseFloat(ms);
        var hasMembership = parseNullableBool(self.campaignForm.filter.hasActiveMembership());
        if (hasMembership !== null) f.hasActiveMembership = hasMembership;
        var hasPackage = parseNullableBool(self.campaignForm.filter.hasActivePackage());
        if (hasPackage !== null) f.hasActivePackage = hasPackage;
        return Object.keys(f).length > 0 ? JSON.stringify(f) : null;
    };

    function parseNullableBool(value) {
        if (value === true || value === 'true') return true;
        if (value === false || value === 'false') return false;
        return null;
    }

    self.resetCampaignForm = function () {
        self.campaignForm.name('');
        self.campaignForm.messageTemplate('');
        self.campaignForm.scheduledAt('');
        self.campaignForm.filter.genderId('');
        self.campaignForm.filter.minAge('');
        self.campaignForm.filter.maxAge('');
        self.campaignForm.filter.city('');
        self.campaignForm.filter.lastVisitDays('');
        self.campaignForm.filter.inactiveDays('');
        self.campaignForm.filter.birthdayInDays('');
        self.campaignForm.filter.minSpent('');
        self.campaignForm.filter.hasActiveMembership('');
        self.campaignForm.filter.hasActivePackage('');
        self.isEditingCampaign(false);
        self.editingCampaignId(null);
        setSegmentPreview({});
    };

    self.openNewCampaign = function () {
        self.resetCampaignForm();
        campaignModal.show();
    };

    self.openEditCampaign = function (campaign) {
        self.isEditingCampaign(true);
        self.editingCampaignId(campaign.id);
        self.campaignForm.name(campaign.name);
        self.campaignForm.messageTemplate(campaign.messageTemplate);
        self.campaignForm.scheduledAt(campaign.scheduledAt ? campaign.scheduledAt.substring(0, 16) : '');
        setSegmentPreview({ matchingClients: campaign.totalRecipients, smsReachableClients: campaign.totalRecipients });

        if (campaign.segmentFilter) {
            try {
                var f = JSON.parse(campaign.segmentFilter);
                self.campaignForm.filter.genderId(f.genderId ? f.genderId.toString() : '');
                self.campaignForm.filter.minAge(f.minAge || '');
                self.campaignForm.filter.maxAge(f.maxAge || '');
                self.campaignForm.filter.city(f.city || '');
                self.campaignForm.filter.lastVisitDays(f.lastVisitDays || '');
                self.campaignForm.filter.inactiveDays(f.inactiveDays || '');
                self.campaignForm.filter.birthdayInDays(f.birthdayInDays || '');
                self.campaignForm.filter.minSpent(f.minSpent || '');
                self.campaignForm.filter.hasActiveMembership(f.hasActiveMembership === true ? 'true' : (f.hasActiveMembership === false ? 'false' : ''));
                self.campaignForm.filter.hasActivePackage(f.hasActivePackage === true ? 'true' : (f.hasActivePackage === false ? 'false' : ''));
            } catch (e) { }
        }
        campaignModal.show();
    };

    self.applySegmentPreset = function (preset) {
        self.campaignForm.filter.genderId('');
        self.campaignForm.filter.minAge('');
        self.campaignForm.filter.maxAge('');
        self.campaignForm.filter.city('');
        self.campaignForm.filter.lastVisitDays('');
        self.campaignForm.filter.inactiveDays('');
        self.campaignForm.filter.birthdayInDays('');
        self.campaignForm.filter.minSpent('');
        self.campaignForm.filter.hasActiveMembership('');
        self.campaignForm.filter.hasActivePackage('');

        if (preset.filterJson) {
            try {
                var f = JSON.parse(preset.filterJson);
                self.campaignForm.filter.genderId(f.genderId ? f.genderId.toString() : '');
                self.campaignForm.filter.minAge(f.minAge || '');
                self.campaignForm.filter.maxAge(f.maxAge || '');
                self.campaignForm.filter.city(f.city || '');
                self.campaignForm.filter.lastVisitDays(f.lastVisitDays || '');
                self.campaignForm.filter.inactiveDays(f.inactiveDays || '');
                self.campaignForm.filter.birthdayInDays(f.birthdayInDays || '');
                self.campaignForm.filter.minSpent(f.minSpent || '');
                self.campaignForm.filter.hasActiveMembership(f.hasActiveMembership === true ? 'true' : (f.hasActiveMembership === false ? 'false' : ''));
                self.campaignForm.filter.hasActivePackage(f.hasActivePackage === true ? 'true' : (f.hasActivePackage === false ? 'false' : ''));
            } catch (e) {
                toastr.error(slnJsT('salon.campaigns.js.segment_filter_read_failed', 'Segment filtresi okunamadı'));
            }
        }

        setSegmentPreview(preset);
    };

    function setSegmentPreview(data) {
        data = data || {};
        self.segmentPreviewCount(data.matchingClients || 0);
        self.segmentSmsReachableCount(data.smsReachableClients || 0);
        self.segmentEmailReachableCount(data.emailReachableClients || 0);
        self.segmentExcludedCount(data.excludedByOptOutCount || 0);
        self.segmentMissingPhoneCount(data.missingPhoneCount || 0);
        self.segmentEstimatedSmsCost(data.estimatedSmsCost || 0);
    }

    self.formatMoney = function (value) {
        return (parseFloat(value) || 0).toLocaleString(document.documentElement.lang || undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 }) + ' TL';
    };

    self.previewSegment = function () {
        var filter = self.buildSegmentFilter();
        $.ajax({
            url: '/proxy/sln-marketing/campaigns/segment-preview',
            method: 'POST',
            contentType: 'application/json',
            data: JSON.stringify(filter)
        }).done(function (data) {
            setSegmentPreview(data);
        });
    };

    self.saveCampaign = function () {
        var data = {
            name: self.campaignForm.name(),
            messageTemplate: self.campaignForm.messageTemplate(),
            segmentFilter: self.buildSegmentFilter(),
            scheduledAt: self.campaignForm.scheduledAt() ? self.campaignForm.scheduledAt() + ':00Z' : null
        };

        if (!data.name || !data.messageTemplate) {
            toastr.warning(slnJsT('salon.campaigns.js.kampanya_adi_ve_mesaj_sablonu_zorunludur', 'Kampanya adi ve mesaj sablonu zorunludur'));
            return;
        }

        self.isSaving(true);
        var url = '/proxy/sln-marketing/campaigns';
        var method = 'POST';
        if (self.isEditingCampaign()) {
            url += '/' + self.editingCampaignId();
            method = 'PUT';
        }

        $.ajax({ url: url, method: method, contentType: 'application/json', data: JSON.stringify(data) })
            .done(function () {
                campaignModal.hide();
                self.loadCampaigns();
                toastr.success(self.isEditingCampaign() ? slnJsT('salon.campaigns.js.kampanya_guncellendi', 'Kampanya güncellendi') : slnJsT('salon.campaigns.js.kampanya_olusturuldu', 'Kampanya oluşturuldu'));
                self.isSaving(false);
            }).fail(function (xhr) {
                toastr.error(xhr.responseJSON || slnJsT('salon.common.error.generic', 'Bir hata oluştu'));
                self.isSaving(false);
            });
    };

    self.sendCampaign = function (campaign) {
        confirmModal(slnJsT('salon.common.btn.confirm', 'Onayla'), slnJsT('salon.campaigns.js.send_confirm', '{name} kampanyasını göndermek istediğinize emin misiniz?').replace('{name}', campaign.name || ''), function() {
            $.ajax({
                url: '/proxy/sln-marketing/campaigns/' + campaign.id + '/send',
                method: 'POST'
            }).done(function () {
                self.loadCampaigns();
                toastr.success(slnJsT('salon.campaigns.js.kampanya_gonderildi', 'Kampanya gonderildi'));
            }).fail(function (xhr) {
                toastr.error(xhr.responseJSON || slnJsT('salon.campaigns.js.send_failed', 'Gönderilemedi'));
            });
        });
    };

    self.removeCampaign = function (campaign) {
        confirmModal(slnJsT('salon.common.btn.confirm', 'Onayla'), slnJsT('salon.campaigns.js.delete_confirm', 'Bu kampanyayı silmek istediğinize emin misiniz?'), function() {
            $.ajax({ url: '/proxy/sln-marketing/campaigns/' + campaign.id, method: 'DELETE' })
                .done(function () {
                    self.loadCampaigns();
                    toastr.success(slnJsT('salon.campaigns.js.kampanya_silindi', 'Kampanya silindi'));
                }).fail(function () { toastr.error(slnJsT('salon.common.delete_failed', 'Silinemedi')); });
        });
    };

    // ═══ Hatirlatma ═══

    self.loadReminders = function () {
        $.ajax({ url: '/proxy/sln-marketing/reminders', method: 'GET' }).done(function (data) {
            (data || []).forEach(function (r) {
                r.isActive = ko.observable(r.isActive);
            });
            self.reminders(data || []);
        }).fail(function () {
            toastr.error(slnJsT('salon.campaigns.js.reminders_load_failed', 'Hatırlatmalar yüklenemedi'));
        });
    };

    self.resetReminderForm = function () {
        self.reminderForm.reminderTypeId('1');
        self.reminderForm.messageTemplate('');
        self.reminderForm.daysBefore(0);
        self.reminderForm.inactiveDaysThreshold(0);
        self.reminderForm.isActive(true);
        self.isEditingReminder(false);
        self.editingReminderId(null);
    };

    self.openNewReminder = function () {
        self.resetReminderForm();
        reminderModal.show();
    };

    self.openEditReminder = function (reminder) {
        self.isEditingReminder(true);
        self.editingReminderId(reminder.id);
        self.reminderForm.reminderTypeId(reminder.reminderTypeId.toString());
        self.reminderForm.messageTemplate(reminder.messageTemplate);
        self.reminderForm.daysBefore(reminder.daysBefore);
        self.reminderForm.inactiveDaysThreshold(reminder.inactiveDaysThreshold);
        self.reminderForm.isActive(ko.unwrap(reminder.isActive));
        reminderModal.show();
    };

    self.saveReminder = function () {
        var data = {
            reminderTypeId: parseInt(self.reminderForm.reminderTypeId()),
            messageTemplate: self.reminderForm.messageTemplate(),
            daysBefore: parseInt(self.reminderForm.daysBefore()) || 0,
            inactiveDaysThreshold: parseInt(self.reminderForm.inactiveDaysThreshold()) || 0,
            isActive: self.reminderForm.isActive()
        };

        if (!data.messageTemplate) {
            toastr.warning(slnJsT('salon.campaigns.js.message_template_required', 'Mesaj şablonu zorunludur'));
            return;
        }

        self.isSaving(true);
        var url = '/proxy/sln-marketing/reminders';
        var method = 'POST';
        if (self.isEditingReminder()) {
            url += '/' + self.editingReminderId();
            method = 'PUT';
        }

        $.ajax({ url: url, method: method, contentType: 'application/json', data: JSON.stringify(data) })
            .done(function () {
                reminderModal.hide();
                self.loadReminders();
                toastr.success(self.isEditingReminder() ? slnJsT('salon.campaigns.js.hatirlatma_guncellendi', 'Hatirlatma güncellendi') : slnJsT('salon.campaigns.js.hatirlatma_olusturuldu', 'Hatirlatma oluşturuldu'));
                self.isSaving(false);
            }).fail(function (xhr) {
                toastr.error(xhr.responseJSON || slnJsT('salon.common.error.generic', 'Bir hata oluştu'));
                self.isSaving(false);
            });
    };

    self.toggleReminder = function (reminder) {
        $.ajax({
            url: '/proxy/sln-marketing/reminders/' + reminder.id + '/toggle',
            method: 'POST'
        }).done(function () {
            toastr.success(slnJsT('salon.campaigns.js.hatirlatma_durumu_degistirildi', 'Hatirlatma durumu degistirildi'));
        }).fail(function () {
            reminder.isActive(!ko.unwrap(reminder.isActive));
            toastr.error(slnJsT('salon.common.status_change_failed', 'Durum değiştirilemedi'));
        });
        return true; // checkbox binding icin
    };

    self.removeReminder = function (reminder) {
        confirmModal(slnJsT('salon.common.btn.confirm', 'Onayla'), slnJsT('salon.campaigns.js.reminder_delete_confirm', 'Bu hatırlatmayı silmek istediğinize emin misiniz?'), function() {
            $.ajax({ url: '/proxy/sln-marketing/reminders/' + reminder.id, method: 'DELETE' })
                .done(function () {
                    self.loadReminders();
                    toastr.success(slnJsT('salon.campaigns.js.hatirlatma_silindi', 'Hatirlatma silindi'));
                }).fail(function () { toastr.error(slnJsT('salon.common.delete_failed', 'Silinemedi')); });
        });
    };

    // Init
    $(document).ready(function () {
        campaignModal = new bootstrap.Modal(document.getElementById('campaignModal'));
        reminderModal = new bootstrap.Modal(document.getElementById('reminderModal'));
        self.loadCampaigns();
        self.loadSegmentPresets();
        self.loadReminders();
    });
}

ko.applyBindings(new CampaignsViewModel(), document.getElementById('campaigns-vm'));
