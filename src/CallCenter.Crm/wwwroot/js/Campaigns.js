var statusMap = {
    1: { name: 'Taslak', css: 'bg-secondary' },
    2: { name: 'Aktif', css: 'bg-success' },
    3: { name: 'Duraklatilmis', css: 'bg-warning text-dark' },
    4: { name: 'Tamamlandi', css: 'bg-primary' },
    5: { name: 'Arsivlendi', css: 'bg-dark' }
};

function formatDate(dateStr) {
    if (!dateStr) return '-';
    var d = new Date(dateStr);
    var day = ('0' + d.getDate()).slice(-2);
    var month = ('0' + (d.getMonth() + 1)).slice(-2);
    return day + '.' + month + '.' + d.getFullYear();
}

function CampaignsViewModel() {
    var self = this;
    self.campaigns = ko.observableArray([]);
    self.searchQuery = ko.observable('');
    self.isLoading = ko.observable(true);
    self.isSaving = ko.observable(false);

    self.form = {
        name: ko.observable(''),
        description: ko.observable(''),
        scheduledDate: ko.observable('')
    };

    self.filteredCampaigns = ko.computed(function () {
        var q = (self.searchQuery() || '').toLowerCase();
        if (!q) return self.campaigns();
        return self.campaigns().filter(function (c) {
            return (c.name || '').toLowerCase().indexOf(q) >= 0
                || (c.description || '').toLowerCase().indexOf(q) >= 0;
        });
    });

    var formModal;

    self.loadData = function () {
        self.isLoading(true);
        $.ajax({
            url: '/proxy/campaigns',
            method: 'GET'
        }).done(function (data) {
            var mapped = (data || []).map(function (c) {
                var s = statusMap[c.statusId] || { name: '', css: 'bg-secondary' };
                c.statusName = s.name;
                c.statusCss = s.css;
                c.scheduledDateFormatted = formatDate(c.scheduledDate);
                return c;
            });
            self.campaigns(mapped);
        }).fail(function () {
            toastr.error('Kampanyalar yuklenemedi');
        }).always(function () {
            self.isLoading(false);
        });
    };

    self.openNew = function () {
        self.form.name('');
        self.form.description('');
        self.form.scheduledDate(new Date().toISOString().slice(0, 10));
        formModal.show();
    };

    self.create = function () {
        if (!self.form.name()) return;
        self.isSaving(true);

        var data = {
            name: self.form.name(),
            description: self.form.description() || null,
            scheduledDate: self.form.scheduledDate() || null
        };

        $.ajax({
            url: '/proxy/campaigns',
            method: 'POST',
            contentType: 'application/json',
            data: JSON.stringify(data)
        }).done(function (result) {
            formModal.hide();
            toastr.success('Kampanya olusturuldu');
            if (result && result.uid) {
                window.location.href = '/Campaigns/Detail/' + result.uid;
            } else {
                self.loadData();
            }
        }).fail(function (xhr) {
            toastr.error(xhr.responseJSON?.error || 'Kampanya olusturulamadi');
        }).always(function () {
            self.isSaving(false);
        });
    };

    self.remove = function (campaign) {
        confirmModal('Kampanyayi Sil', campaign.name + ' kampanyasini silmek istediginize emin misiniz?', function () {
            $.ajax({
                url: '/proxy/campaigns/' + campaign.uid,
                method: 'DELETE'
            }).done(function () {
                self.loadData();
                toastr.success('Kampanya silindi');
            }).fail(function () {
                toastr.error('Silinemedi');
            });
        });
    };

    $(document).ready(function () {
        formModal = new bootstrap.Modal(document.getElementById('campaignModal'));
        self.loadData();
    });
}

ko.applyBindings(new CampaignsViewModel(), document.getElementById('campaigns-vm'));
