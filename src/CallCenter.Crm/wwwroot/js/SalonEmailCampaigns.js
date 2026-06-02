function SalonEmailCampaignsViewModel() {
    var self = this;
    var t = window.crmT || function (key, fallback) { return fallback || key; };

    self.campaigns = ko.observableArray([]);
    self.branches = ko.observableArray([]);
    self.segmentPresets = ko.observableArray([]);
    self.segmentPreviewCount = ko.observable(0);
    self.segmentEmailReachableCount = ko.observable(0);
    self.segmentMissingEmailCount = ko.observable(0);
    self.segmentExcludedCount = ko.observable(0);
    self.isSaving = ko.observable(false);
    self.isEditing = ko.observable(false);
    self.editingId = ko.observable(null);
    var allBranchesValue = '__all__';
    self.branchTargetOptions = ko.computed(function () {
        return [{ id: allBranchesValue, name: t('crm.salon.common.all_branches', 'Tüm Şubeler') }].concat(self.branches() || []);
    });

    self.form = {
        subject: ko.observable(''),
        htmlBody: ko.observable(''),
        segmentFilter: ko.observable(''),
        branchTarget: ko.observable(allBranchesValue),
        scheduledAt: ko.observable('')
    };

    var formModal;
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
        self.segmentEmailReachableCount(data.emailReachableClients || 0);
        self.segmentMissingEmailCount(data.missingEmailCount || 0);
        self.segmentExcludedCount(data.excludedByOptOutCount || 0);
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
            .fail(function (xhr) { toastr.error(readError(xhr, t('crm.salon.emailcampaigns.load_failed', 'E-posta kampanyaları yüklenemedi'))); });
    };

    self.loadBranches = function () {
        $.get('/proxy/crm/salon/branches')
            .done(function (data) { self.branches(items(data)); })
            .fail(function () { self.branches([]); });
    };

    self.loadSegmentPresets = function () {
        $.get('/proxy/crm/salon/email-campaigns/segment-presets')
            .done(function (data) { self.segmentPresets(items(data)); })
            .fail(function () { self.segmentPresets([]); });
    };

    self.openNew = function () {
        self.isEditing(false);
        self.editingId(null);
        self.form.subject('');
        self.form.htmlBody('');
        self.form.segmentFilter('');
        self.form.branchTarget(allBranchesValue);
        self.form.scheduledAt('');
        setSegmentPreview({});
        formModal.show();
    };

    self.edit = function (campaign) {
        self.isEditing(true);
        self.editingId(campaign.id);
        self.form.subject(campaign.subject || '');
        self.form.htmlBody(campaign.htmlBody || '');
        self.form.segmentFilter(campaign.segmentFilter || '');
        self.form.branchTarget(campaign.branchId ? String(campaign.branchId) : allBranchesValue);
        self.form.scheduledAt(campaign.scheduledAt ? campaign.scheduledAt.substring(0, 16) : '');
        setSegmentPreview({ matchingClients: campaign.totalRecipients, emailReachableClients: campaign.totalRecipients });
        formModal.show();
    };

    self.applySegmentPreset = function (preset) {
        self.form.segmentFilter(preset.filterJson || '');
        setSegmentPreview(preset);
    };

    self.previewSegment = function () {
        var target = resolveBranchTarget(self.form.branchTarget());
        if (!target.ok) return;

        $.ajax({
            url: appendBranchTarget('/proxy/crm/salon/email-campaigns/segment-preview', target),
            method: 'POST',
            contentType: 'application/json',
            data: JSON.stringify(self.form.segmentFilter() || null)
        }).done(setSegmentPreview)
            .fail(function (xhr) { toastr.error(readError(xhr, t('crm.salon.emailcampaigns.preview_failed', 'Önizleme alınamadı'))); });
    };

    self.save = function () {
        var target = resolveBranchTarget(self.form.branchTarget());
        if (!target.ok) return;

        var payload = {
            subject: self.form.subject(),
            htmlBody: self.form.htmlBody(),
            segmentFilter: self.form.segmentFilter() || null,
            scheduledAt: self.form.scheduledAt() ? self.form.scheduledAt() + ':00Z' : null
        };

        if (!payload.subject || !payload.htmlBody) {
            toastr.warning(t('crm.salon.emailcampaigns.required', 'Konu ve içerik zorunludur'));
            return;
        }

        var url = appendBranchTarget('/proxy/crm/salon/email-campaigns', target);
        var method = 'POST';
        if (self.isEditing()) {
            url = appendBranchTarget('/proxy/crm/salon/email-campaigns/' + self.editingId(), target);
            method = 'PUT';
        }

        self.isSaving(true);
        $.ajax({ url: url, method: method, contentType: 'application/json', data: JSON.stringify(payload) })
            .done(function () {
                formModal.hide();
                self.loadData();
                toastr.success(t('crm.salon.emailcampaigns.saved', 'E-posta kampanyası kaydedildi'));
            })
            .fail(function (xhr) { toastr.error(readError(xhr, t('crm.salon.emailcampaigns.save_failed', 'E-posta kampanyası kaydedilemedi'))); })
            .always(function () { self.isSaving(false); });
    };

    self.send = function (campaign) {
        confirmModal(t('crm.common.confirm', 'Onayla'), t('crm.salon.emailcampaigns.send_confirm', "'{subject}' kampanyası gönderilsin mi?").replace('{subject}', campaign.subject || ''), function () {
            $.ajax({ url: '/proxy/crm/salon/email-campaigns/' + campaign.id + '/send', method: 'POST' })
                .done(function () {
                    self.loadData();
                    toastr.success(t('crm.salon.emailcampaigns.sent_success', 'E-posta kampanyası gönderildi'));
                })
                .fail(function (xhr) { toastr.error(readError(xhr, t('crm.salon.emailcampaigns.send_failed', 'E-posta kampanyası gönderilemedi'))); });
        });
    };

    self.remove = function (campaign) {
        confirmModal(t('crm.common.confirm', 'Onayla'), t('crm.salon.emailcampaigns.delete_confirm', 'Bu e-posta kampanyasını silmek istediğinize emin misiniz?'), function () {
            $.ajax({ url: '/proxy/crm/salon/email-campaigns/' + campaign.id, method: 'DELETE' })
                .done(function () {
                    self.loadData();
                    toastr.success(t('crm.salon.emailcampaigns.deleted', 'E-posta kampanyası silindi'));
                })
                .fail(function (xhr) { toastr.error(readError(xhr, t('crm.common.delete_failed', 'Silinemedi'))); });
        });
    };

    $(document).ready(function () {
        formModal = new bootstrap.Modal(document.getElementById('salonEmailCampaignModal'));
        self.loadBranches();
        self.loadData();
        self.loadSegmentPresets();
    });
}

ko.applyBindings(new SalonEmailCampaignsViewModel(), document.getElementById('salon-emailcampaigns-vm'));
