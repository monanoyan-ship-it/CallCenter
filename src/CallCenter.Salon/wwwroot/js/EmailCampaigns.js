function EmailCampaignsViewModel() {
    var self = this;
    self.campaigns = ko.observableArray([]);
    self.isEditing = ko.observable(false);
    self.editingId = ko.observable(null);
    self.isSaving = ko.observable(false);

    self.form = {
        subject: ko.observable(''),
        htmlBody: ko.observable(''),
        segmentFilter: ko.observable(''),
        scheduledAt: ko.observable('')
    };

    var statusTexts = { 1: 'Taslak', 2: 'Planlanmis', 3: 'Gonderiliyor', 4: 'Tamamlandi' };
    var statusBadges = { 1: 'bg-secondary', 2: 'bg-info', 3: 'bg-warning', 4: 'bg-success' };

    self.statusText = function (id) { return statusTexts[id] || 'Bilinmiyor'; };
    self.statusBadge = function (id) { return statusBadges[id] || 'bg-secondary'; };

    var formModal;

    self.loadData = function () {
        $.ajax({ url: '/proxy/sln-email-campaigns', method: 'GET' }).done(function (data) {
            self.campaigns(data.items || data);
        });
    };

    self.resetForm = function () {
        self.form.subject('');
        self.form.htmlBody('');
        self.form.segmentFilter('');
        self.form.scheduledAt('');
        self.isEditing(false);
        self.editingId(null);
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
        formModal.show();
    };

    self.save = function () {
        var data = {
            subject: self.form.subject(),
            htmlBody: self.form.htmlBody(),
            segmentFilter: self.form.segmentFilter() || null,
            scheduledAt: self.form.scheduledAt() ? self.form.scheduledAt() + ':00Z' : null
        };

        if (!data.subject || !data.htmlBody) {
            toastr.warning('Konu ve icerik zorunludur');
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
                toastr.success(self.isEditing() ? 'Kampanya guncellendi' : 'Kampanya olusturuldu');
                self.isSaving(false);
            }).fail(function (xhr) {
                toastr.error(xhr.responseJSON || 'Bir hata olustu');
                self.isSaving(false);
            });
    };

    self.remove = function (campaign) {
        confirmModal('Onay', 'Bu kampanyayi silmek istediginize emin misiniz?', function() {
            $.ajax({ url: '/proxy/sln-email-campaigns/' + campaign.id, method: 'DELETE' })
                .done(function () { self.loadData(); toastr.success('Kampanya silindi'); })
                .fail(function () { toastr.error('Silinemedi'); });
        });
    };

    $(document).ready(function () {
        formModal = new bootstrap.Modal(document.getElementById('emailCampaignModal'));
        self.loadData();
    });
}

ko.applyBindings(new EmailCampaignsViewModel(), document.getElementById('emailcampaigns-vm'));
