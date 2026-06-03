function StorageConfigViewModel() {
    var self = this;
    self.items = ko.observableArray([]);
    self.isLoading = ko.observable(false);
    self.isSaving = ko.observable(false);
    self.formTitle = ko.observable('Yeni Yapılandırma');
    self.deleteId = null;

    self.form = {
        id: ko.observable(null), providerTypeId: ko.observable('4'), basePath: ko.observable(''),
        endpoint: ko.observable(''), accessKey: ko.observable(''), secretKey: ko.observable(''),
        bucketName: ko.observable(''), region: ko.observable(''), isActive: ko.observable(true)
    };

    self.loadData = function() {
        self.isLoading(true);
        $.get('/proxy/CloudStorage/configs', function(data) {
            var items = Array.isArray(data) ? data : (data.items || data.data || []);
            self.items(items);
        }).always(function() { self.isLoading(false); });
    };

    self.resetForm = function() {
        self.form.id(null);
        self.form.providerTypeId('4');
        self.form.basePath('');
        self.form.endpoint('');
        self.form.accessKey('');
        self.form.secretKey('');
        self.form.bucketName('');
        self.form.region('');
        self.form.isActive(true);
    };

    self.openCreate = function() { self.resetForm(); self.formTitle('Yeni Yapılandırma'); new bootstrap.Modal('#storageModal').show(); };

    self.openEdit = function(item) {
        self.form.id(item.id);
        self.form.providerTypeId(item.providerTypeId || '4');
        self.form.basePath(item.basePath || '');
        self.form.endpoint('');
        self.form.accessKey('');
        self.form.secretKey('');
        self.form.bucketName('');
        self.form.region('');
        self.form.isActive(item.isActive !== false);
        self.formTitle('Yapılandırma Düzenle');
        new bootstrap.Modal('#storageModal').show();
    };

    self.save = function() {
        self.isSaving(true);
        var payload = {
            providerTypeId: parseInt(self.form.providerTypeId()),
            basePath: self.form.basePath(),
            endpoint: self.form.endpoint(),
            accessKey: self.form.accessKey(),
            secretKey: self.form.secretKey(),
            bucketName: self.form.bucketName(),
            region: self.form.region(),
            isActive: self.form.isActive()
        };
        var method = self.form.id() ? 'PUT' : 'POST';
        var url = self.form.id() ? '/proxy/CloudStorage/configs/' + self.form.id() : '/proxy/CloudStorage/configs';
        $.ajax({
            url: url, method: method, contentType: 'application/json', data: JSON.stringify(payload),
            success: function() {
                toastr.success('Kaydedildi.'); bootstrap.Modal.getInstance(document.getElementById('storageModal')).hide();
                self.loadData();
            },
            error: function() { toastr.error('Kaydetme hatası.'); }
        }).always(function() { self.isSaving(false); });
    };

    self.testConnection = function(item) {
        toastr.info('Test başlatıldı...');
        $.ajax({
            url: '/proxy/CloudStorage/configs/' + item.id + '/test', method: 'POST',
            success: function(data) {
                if (data && data.success) toastr.success('Bağlantı başarılı!');
                else toastr.warning('Test sonucu: ' + (data.message || 'Bilinmeyen'));
            },
            error: function() { toastr.error('Bağlantı testi başarısız.'); }
        });
    };

    self.confirmDelete = function(item) { self.deleteId = item.id; new bootstrap.Modal('#deleteModal').show(); };

    self.executeDelete = function() {
        $.ajax({
            url: '/proxy/CloudStorage/configs/' + self.deleteId, method: 'DELETE',
            success: function() { toastr.success('Silindi.'); bootstrap.Modal.getInstance(document.getElementById('deleteModal')).hide(); self.loadData(); },
            error: function() { toastr.error('Silme hatası.'); }
        });
    };

    self.loadData();
}

ko.applyBindings(new StorageConfigViewModel(), document.getElementById('storageconfig-vm'));
