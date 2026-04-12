function KvkkSettingsViewModel() {
    var self = this;

    // Retention
    self.retentionPolicies = ko.observableArray([]);
    self.retentionLoading = ko.observable(false);
    self.retentionFormTitle = ko.observable('Yeni Saklama Politikasi');
    self.retentionForm = { id: ko.observable(null), dataCategory: ko.observable(''), retentionPeriod: ko.observable(''), description: ko.observable('') };

    // Destruction
    self.destructionPolicies = ko.observableArray([]);
    self.destructionLoading = ko.observable(false);
    self.destructionFormTitle = ko.observable('Yeni Yikim Politikasi');
    self.destructionForm = { id: ko.observable(null), dataCategory: ko.observable(''), destructionMethod: ko.observable(''), period: ko.observable(''), description: ko.observable('') };

    // Retention CRUD
    self.loadRetention = function() {
        self.retentionLoading(true);
        $.get('/proxy/kvkk/retention', function(data) {
            self.retentionPolicies(Array.isArray(data) ? data : (data.items || data.data || []));
        }).always(function() { self.retentionLoading(false); });
    };

    self.openCreateRetention = function() {
        self.retentionForm.id(null); self.retentionForm.dataCategory('');
        self.retentionForm.retentionPeriod(''); self.retentionForm.description('');
        self.retentionFormTitle('Yeni Saklama Politikasi');
        new bootstrap.Modal('#retentionModal').show();
    };

    self.openEditRetention = function(item) {
        self.retentionForm.id(item.id); self.retentionForm.dataCategory(item.dataCategory || '');
        self.retentionForm.retentionPeriod(item.retentionPeriod || '');
        self.retentionForm.description(item.description || '');
        self.retentionFormTitle('Saklama Politikasi Duzenle');
        new bootstrap.Modal('#retentionModal').show();
    };

    self.saveRetention = function() {
        if (!self.retentionForm.dataCategory()) { toastr.warning('Kategori zorunludur.'); return; }
        var payload = { dataCategory: self.retentionForm.dataCategory(), retentionPeriod: self.retentionForm.retentionPeriod(), description: self.retentionForm.description() };
        var method = self.retentionForm.id() ? 'PUT' : 'POST';
        var url = self.retentionForm.id() ? '/proxy/kvkk/retention/' + self.retentionForm.id() : '/proxy/kvkk/retention';
        $.ajax({ url: url, method: method, contentType: 'application/json', data: JSON.stringify(payload),
            success: function() { toastr.success('Kaydedildi.'); bootstrap.Modal.getInstance(document.getElementById('retentionModal')).hide(); self.loadRetention(); },
            error: function() { toastr.error('Hata.'); }
        });
    };

    // Destruction CRUD
    self.loadDestruction = function() {
        self.destructionLoading(true);
        $.get('/proxy/kvkk/destruction', function(data) {
            self.destructionPolicies(Array.isArray(data) ? data : (data.items || data.data || []));
        }).always(function() { self.destructionLoading(false); });
    };

    self.openCreateDestruction = function() {
        self.destructionForm.id(null); self.destructionForm.dataCategory('');
        self.destructionForm.destructionMethod(''); self.destructionForm.period('');
        self.destructionForm.description('');
        self.destructionFormTitle('Yeni Yikim Politikasi');
        new bootstrap.Modal('#destructionModal').show();
    };

    self.openEditDestruction = function(item) {
        self.destructionForm.id(item.id); self.destructionForm.dataCategory(item.dataCategory || '');
        self.destructionForm.destructionMethod(item.destructionMethod || '');
        self.destructionForm.period(item.period || '');
        self.destructionForm.description(item.description || '');
        self.destructionFormTitle('Yikim Politikasi Duzenle');
        new bootstrap.Modal('#destructionModal').show();
    };

    self.saveDestruction = function() {
        if (!self.destructionForm.dataCategory()) { toastr.warning('Kategori zorunludur.'); return; }
        var payload = { dataCategory: self.destructionForm.dataCategory(), destructionMethod: self.destructionForm.destructionMethod(), period: self.destructionForm.period(), description: self.destructionForm.description() };
        var method = self.destructionForm.id() ? 'PUT' : 'POST';
        var url = self.destructionForm.id() ? '/proxy/kvkk/destruction/' + self.destructionForm.id() : '/proxy/kvkk/destruction';
        $.ajax({ url: url, method: method, contentType: 'application/json', data: JSON.stringify(payload),
            success: function() { toastr.success('Kaydedildi.'); bootstrap.Modal.getInstance(document.getElementById('destructionModal')).hide(); self.loadDestruction(); },
            error: function() { toastr.error('Hata.'); }
        });
    };

    self.loadRetention();
    self.loadDestruction();
}

ko.applyBindings(new KvkkSettingsViewModel(), document.getElementById('kvkksettings-vm'));
