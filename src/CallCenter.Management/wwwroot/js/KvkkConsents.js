function ConsentsViewModel() {
    var self = this;
    self.items = ko.observableArray([]);
    self.privacyNotices = ko.observableArray([]);
    self.isLoading = ko.observable(false);
    self.isSaving = ko.observable(false);
    self.currentPage = ko.observable(1);
    self.totalCount = ko.observable(0);
    self.pageSize = 20;

    self.form = { personName: ko.observable(''), privacyNoticeUid: ko.observable('') };

    self.pageNumbers = ko.computed(function() {
        var total = Math.ceil(self.totalCount() / self.pageSize);
        var pages = [];
        for (var i = 1; i <= total; i++) pages.push(i);
        return pages;
    });

    self.goToPage = function(page) { self.currentPage(page); self.loadData(); };

    self.loadData = function() {
        self.isLoading(true);
        $.get('/proxy/kvkk/consents', { page: self.currentPage(), pageSize: self.pageSize }, function(data) {
            var items = Array.isArray(data) ? data : (data.items || data.data || []);
            self.items(items);
            self.totalCount(data.totalCount || data.total || items.length);
        }).always(function() { self.isLoading(false); });
    };

    self.loadNotices = function() {
        $.get('/proxy/kvkk/privacy-notices', function(data) {
            self.privacyNotices(Array.isArray(data) ? data : (data.items || data.data || []));
        });
    };

    self.openCreate = function() {
        self.form.personName(''); self.form.privacyNoticeUid('');
        new bootstrap.Modal('#consentModal').show();
    };

    self.save = function() {
        if (!self.form.personName() || !self.form.privacyNoticeUid()) { toastr.warning('Tum alanlar zorunludur.'); return; }
        self.isSaving(true);
        $.ajax({
            url: '/proxy/kvkk/consents', method: 'POST', contentType: 'application/json',
            data: JSON.stringify({ personName: self.form.personName(), privacyNoticeUid: self.form.privacyNoticeUid() }),
            success: function() {
                toastr.success('Onay kaydedildi.'); bootstrap.Modal.getInstance(document.getElementById('consentModal')).hide();
                self.loadData();
            },
            error: function() { toastr.error('Kaydetme hatasi.'); }
        }).always(function() { self.isSaving(false); });
    };

    self.revoke = function(item) {
        confirmModal('Onay Iptali', 'Bu onayi iptal etmek istediginize emin misiniz?', function () {
            $.ajax({
                url: '/proxy/kvkk/consents/' + (item.uid || item.id) + '/revoke', method: 'POST',
                success: function() { toastr.success('Onay iptal edildi.'); self.loadData(); },
                error: function() { toastr.error('Iptal hatasi.'); }
            });
        }, { confirmText: 'Iptal Et', confirmClass: 'btn-danger' });
    };

    self.loadNotices();
    self.loadData();
}

ko.applyBindings(new ConsentsViewModel(), document.getElementById('consents-vm'));
