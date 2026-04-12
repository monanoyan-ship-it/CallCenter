function CampaignsViewModel() {
    var self = this;

    // Kampanyalar
    self.campaigns = ko.observableArray([]);
    self.isEditingCampaign = ko.observable(false);
    self.editingCampaignId = ko.observable(null);
    self.isSaving = ko.observable(false);
    self.segmentPreviewCount = ko.observable(0);

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
            minSpent: ko.observable('')
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
    var statusTexts = { 1: 'Taslak', 2: 'Zamanlanmis', 3: 'Gonderiliyor', 4: 'Tamamlandi' };
    var statusBadges = { 1: 'bg-secondary', 2: 'bg-info', 3: 'bg-warning', 4: 'bg-success' };

    // ═══ Kampanya ═══

    self.loadCampaigns = function () {
        $.ajax({ url: '/proxy/sln-marketing/campaigns', method: 'GET' }).done(function (data) {
            (data || []).forEach(function (c) {
                c.statusText = statusTexts[c.statusId] || 'Bilinmiyor';
                c.statusBadge = statusBadges[c.statusId] || 'bg-secondary';
            });
            self.campaigns(data || []);
        }).fail(function () {
            toastr.error('Kampanyalar yuklenemedi');
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
        var ms = self.campaignForm.filter.minSpent();
        if (ms) f.minSpent = parseFloat(ms);
        return Object.keys(f).length > 0 ? JSON.stringify(f) : null;
    };

    self.resetCampaignForm = function () {
        self.campaignForm.name('');
        self.campaignForm.messageTemplate('');
        self.campaignForm.scheduledAt('');
        self.campaignForm.filter.genderId('');
        self.campaignForm.filter.minAge('');
        self.campaignForm.filter.maxAge('');
        self.campaignForm.filter.city('');
        self.campaignForm.filter.lastVisitDays('');
        self.campaignForm.filter.minSpent('');
        self.isEditingCampaign(false);
        self.editingCampaignId(null);
        self.segmentPreviewCount(0);
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
        self.segmentPreviewCount(campaign.totalRecipients);

        if (campaign.segmentFilter) {
            try {
                var f = JSON.parse(campaign.segmentFilter);
                self.campaignForm.filter.genderId(f.genderId ? f.genderId.toString() : '');
                self.campaignForm.filter.minAge(f.minAge || '');
                self.campaignForm.filter.maxAge(f.maxAge || '');
                self.campaignForm.filter.city(f.city || '');
                self.campaignForm.filter.lastVisitDays(f.lastVisitDays || '');
                self.campaignForm.filter.minSpent(f.minSpent || '');
            } catch (e) { }
        }
        campaignModal.show();
    };

    self.previewSegment = function () {
        var filter = self.buildSegmentFilter();
        $.ajax({
            url: '/proxy/sln-marketing/campaigns/segment-preview',
            method: 'POST',
            contentType: 'application/json',
            data: JSON.stringify(filter)
        }).done(function (data) {
            self.segmentPreviewCount(data.matchingClients || 0);
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
            toastr.warning('Kampanya adi ve mesaj sablonu zorunludur');
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
                toastr.success(self.isEditingCampaign() ? 'Kampanya guncellendi' : 'Kampanya olusturuldu');
                self.isSaving(false);
            }).fail(function (xhr) {
                toastr.error(xhr.responseJSON || 'Bir hata olustu');
                self.isSaving(false);
            });
    };

    self.sendCampaign = function (campaign) {
        confirmModal('Onay', campaign.name + ' kampanyasini gondermek istediginize emin misiniz?', function() {
            $.ajax({
                url: '/proxy/sln-marketing/campaigns/' + campaign.id + '/send',
                method: 'POST'
            }).done(function () {
                self.loadCampaigns();
                toastr.success('Kampanya gonderildi');
            }).fail(function (xhr) {
                toastr.error(xhr.responseJSON || 'Gonderilemedi');
            });
        });
    };

    self.removeCampaign = function (campaign) {
        confirmModal('Onay', 'Bu kampanyayi silmek istediginize emin misiniz?', function() {
            $.ajax({ url: '/proxy/sln-marketing/campaigns/' + campaign.id, method: 'DELETE' })
                .done(function () {
                    self.loadCampaigns();
                    toastr.success('Kampanya silindi');
                }).fail(function () { toastr.error('Silinemedi'); });
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
            toastr.error('Hatirlatmalar yuklenemedi');
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
            toastr.warning('Mesaj sablonu zorunludur');
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
                toastr.success(self.isEditingReminder() ? 'Hatirlatma guncellendi' : 'Hatirlatma olusturuldu');
                self.isSaving(false);
            }).fail(function (xhr) {
                toastr.error(xhr.responseJSON || 'Bir hata olustu');
                self.isSaving(false);
            });
    };

    self.toggleReminder = function (reminder) {
        $.ajax({
            url: '/proxy/sln-marketing/reminders/' + reminder.id + '/toggle',
            method: 'POST'
        }).done(function () {
            toastr.success('Hatirlatma durumu degistirildi');
        }).fail(function () {
            reminder.isActive(!ko.unwrap(reminder.isActive));
            toastr.error('Durum degistirilemedi');
        });
        return true; // checkbox binding icin
    };

    self.removeReminder = function (reminder) {
        confirmModal('Onay', 'Bu hatirlatmayi silmek istediginize emin misiniz?', function() {
            $.ajax({ url: '/proxy/sln-marketing/reminders/' + reminder.id, method: 'DELETE' })
                .done(function () {
                    self.loadReminders();
                    toastr.success('Hatirlatma silindi');
                }).fail(function () { toastr.error('Silinemedi'); });
        });
    };

    // Init
    $(document).ready(function () {
        campaignModal = new bootstrap.Modal(document.getElementById('campaignModal'));
        reminderModal = new bootstrap.Modal(document.getElementById('reminderModal'));
        self.loadCampaigns();
        self.loadReminders();
    });
}

ko.applyBindings(new CampaignsViewModel(), document.getElementById('campaigns-vm'));
