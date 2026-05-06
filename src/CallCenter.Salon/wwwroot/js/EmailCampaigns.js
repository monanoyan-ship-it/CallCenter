function slnJsT(key, fallback) {
    return (window.salonT || function (k, f) { return f || k; })(key, fallback);
}

function EmailCampaignsViewModel() {
    var self = this;
    self.campaigns = ko.observableArray([]);
    self.isEditing = ko.observable(false);
    self.editingId = ko.observable(null);
    self.isSaving = ko.observable(false);
    self.segmentPresets = ko.observableArray([]);
    self.segmentPreviewCount = ko.observable(0);
    self.segmentEmailReachableCount = ko.observable(0);
    self.segmentMissingEmailCount = ko.observable(0);
    self.segmentExcludedCount = ko.observable(0);

    self.form = {
        subject: ko.observable(''),
        htmlBody: ko.observable(''),
        segmentFilter: ko.observable(''),
        scheduledAt: ko.observable('')
    };

    var statusTexts = {
        1: slnJsT('salon.campaigns.status.draft', 'Taslak'),
        2: slnJsT('salon.campaigns.status.scheduled', 'Planlanmış'),
        3: slnJsT('salon.campaigns.status.sending', 'Gönderiliyor'),
        4: slnJsT('salon.campaigns.status.completed', 'Tamamlandı')
    };
    var statusBadges = { 1: 'bg-secondary', 2: 'bg-info', 3: 'bg-warning', 4: 'bg-success' };

    self.statusText = function (id) { return statusTexts[id] || slnJsT('salon.common.unknown', 'Bilinmiyor'); };
    self.statusBadge = function (id) { return statusBadges[id] || 'bg-secondary'; };

    var formModal;

    self.loadData = function () {
        $.ajax({ url: '/proxy/sln-email-campaigns', method: 'GET' }).done(function (data) {
            self.campaigns(data.items || data);
        });
    };

    self.loadSegmentPresets = function () {
        $.ajax({ url: '/proxy/sln-email-campaigns/segment-presets', method: 'GET' }).done(function (data) {
            self.segmentPresets(data || []);
        }).fail(function () {
            toastr.error(slnJsT('salon.campaigns.js.segment_presets_load_failed', 'Hazır segmentler yüklenemedi'));
        });
    };

    self.resetForm = function () {
        self.form.subject('');
        self.form.htmlBody('');
        self.form.segmentFilter('');
        self.form.scheduledAt('');
        self.isEditing(false);
        self.editingId(null);
        setSegmentPreview({});
    };

    self.openNew = function () {
        self.resetForm();
        formModal.show();
    };

    self.openEdit = function (campaign) {
        self.isEditing(true);
        self.editingId(campaign.id);
        self.form.subject(campaign.subject);
        self.form.htmlBody(campaign.htmlBody);
        self.form.segmentFilter(campaign.segmentFilter || '');
        self.form.scheduledAt(campaign.scheduledAt ? campaign.scheduledAt.substring(0, 16) : '');
        setSegmentPreview({ matchingClients: campaign.totalRecipients, emailReachableClients: campaign.totalRecipients });
        formModal.show();
    };

    self.applySegmentPreset = function (preset) {
        self.form.segmentFilter(preset.filterJson || '');
        setSegmentPreview(preset);
    };

    self.previewSegment = function () {
        var filter = self.form.segmentFilter() || null;
        $.ajax({
            url: '/proxy/sln-email-campaigns/segment-preview',
            method: 'POST',
            contentType: 'application/json',
            data: JSON.stringify(filter)
        }).done(function (data) {
            setSegmentPreview(data);
        }).fail(function () {
            toastr.error('Segment onizlemesi alinamadi');
        });
    };

    function setSegmentPreview(data) {
        data = data || {};
        self.segmentPreviewCount(data.matchingClients || 0);
        self.segmentEmailReachableCount(data.emailReachableClients || 0);
        self.segmentMissingEmailCount(data.missingEmailCount || 0);
        self.segmentExcludedCount(data.excludedByOptOutCount || 0);
    }

    self.save = function () {
        var data = {
            subject: self.form.subject(),
            htmlBody: self.form.htmlBody(),
            segmentFilter: self.form.segmentFilter() || null,
            scheduledAt: self.form.scheduledAt() ? self.form.scheduledAt() + ':00Z' : null
        };

        if (!data.subject || !data.htmlBody) {
            toastr.warning(slnJsT('salon.emailcampaigns.js.konu_ve_icerik_zorunludur', 'Konu ve icerik zorunludur'));
            return;
        }

        self.isSaving(true);
        var url = '/proxy/sln-email-campaigns';
        var method = 'POST';
        if (self.isEditing()) {
            url += '/' + self.editingId();
            method = 'PUT';
        }

        $.ajax({ url: url, method: method, contentType: 'application/json', data: JSON.stringify(data) })
            .done(function () {
                formModal.hide();
                self.loadData();
                toastr.success(self.isEditing() ? slnJsT('salon.emailcampaigns.js.kampanya_guncellendi', 'Kampanya güncellendi') : slnJsT('salon.emailcampaigns.js.kampanya_olusturuldu', 'Kampanya oluşturuldu'));
                self.isSaving(false);
            }).fail(function (xhr) {
                toastr.error(xhr.responseJSON || slnJsT('salon.common.error.generic', 'Bir hata oluştu'));
                self.isSaving(false);
            });
    };

    self.remove = function (campaign) {
        confirmModal(slnJsT('salon.common.btn.confirm', 'Onayla'), slnJsT('salon.emailcampaigns.js.bu_kampanyayi_silmek_istediginize_emin_misiniz', 'Bu kampanyayı silmek istediğinize emin misiniz?'), function() {
            $.ajax({ url: '/proxy/sln-email-campaigns/' + campaign.id, method: 'DELETE' })
                .done(function () { self.loadData(); toastr.success(slnJsT('salon.emailcampaigns.js.kampanya_silindi', 'Kampanya silindi')); })
                .fail(function () { toastr.error('Silinemedi'); });
        });
    };

    self.send = function (campaign) {
        confirmModal(slnJsT('salon.common.btn.confirm', 'Onayla'), campaign.subject + slnJsT('salon.emailcampaigns.js.e_posta_kampanyasini_gondermek_istediginize_emin_misiniz', ' e-posta kampanyasini gondermek istediğinize emin misiniz?'), function() {
            $.ajax({ url: '/proxy/sln-email-campaigns/' + campaign.id + '/send', method: 'POST' })
                .done(function () {
                    self.loadData();
                    toastr.success('E-posta kampanyasi gonderildi');
                })
                .fail(function (xhr) {
                    toastr.error(xhr.responseJSON || 'Gonderilemedi');
                });
        });
    };

    $(document).ready(function () {
        formModal = new bootstrap.Modal(document.getElementById('emailCampaignModal'));
        self.loadData();
        self.loadSegmentPresets();
    });
}

ko.applyBindings(new EmailCampaignsViewModel(), document.getElementById('emailcampaigns-vm'));
