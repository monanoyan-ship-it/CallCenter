function PrivacyNoticesViewModel() {
    var self = this;
    self.items = ko.observableArray([]);
    self.isLoading = ko.observable(false);
    self.isSaving = ko.observable(false);
    self.formTitle = ko.observable('Yeni Aydinlatma Metni');

    self.form = { uid: ko.observable(null), title: ko.observable(''), content: ko.observable(''), version: ko.observable('') };

    self.loadData = function() {
        self.isLoading(true);
        $.get('/proxy/kvkk/privacy-notices', function(data) {
            self.items(Array.isArray(data) ? data : (data.items || data.data || []));
        }).always(function() { self.isLoading(false); });
    };

    self.openCreate = function() {
        self.form.uid(null); self.form.title(''); self.form.content(''); self.form.version('');
        self.formTitle('Yeni Aydinlatma Metni');
        new bootstrap.Modal('#noticeModal').show();
    };

    self.openEdit = function(item) {
        self.form.uid(item.uid || item.id); self.form.title(item.title || '');
        self.form.content(item.content || ''); self.form.version(item.version || '');
        self.formTitle('Aydinlatma Metni Duzenle');
        new bootstrap.Modal('#noticeModal').show();
    };

    self.save = function() {
        if (!self.form.title()) { toastr.warning('Baslik zorunludur.'); return; }
        self.isSaving(true);
        var payload = { title: self.form.title(), content: self.form.content(), version: self.form.version() };
        var method = self.form.uid() ? 'PUT' : 'POST';
        var url = self.form.uid() ? '/proxy/kvkk/privacy-notices/' + self.form.uid() : '/proxy/kvkk/privacy-notices';
        $.ajax({
            url: url, method: method, contentType: 'application/json', data: JSON.stringify(payload),
            success: function() {
                toastr.success('Kaydedildi.'); bootstrap.Modal.getInstance(document.getElementById('noticeModal')).hide();
                self.loadData();
            },
            error: function() { toastr.error('Kaydetme hatasi.'); }
        }).always(function() { self.isSaving(false); });
    };

    self.activate = function(item) {
        $.ajax({
            url: '/proxy/kvkk/privacy-notices/' + (item.uid || item.id) + '/activate', method: 'POST',
            success: function() { toastr.success('Aktif edildi.'); self.loadData(); },
            error: function() { toastr.error('Aktivasyon hatasi.'); }
        });
    };

    self.loadData();
}

ko.applyBindings(new PrivacyNoticesViewModel(), document.getElementById('privacynotices-vm'));
