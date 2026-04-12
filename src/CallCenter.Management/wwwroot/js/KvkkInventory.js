function InventoryViewModel() {
    var self = this;
    self.items = ko.observableArray([]);
    self.isLoading = ko.observable(false);
    self.isSaving = ko.observable(false);
    self.formTitle = ko.observable('Yeni Envanter Kaydi');

    self.form = {
        id: ko.observable(null), dataCategory: ko.observable(''), dataType: ko.observable(''),
        retentionPeriod: ko.observable(''), location: ko.observable(''), securityMeasure: ko.observable('')
    };

    self.loadData = function() {
        self.isLoading(true);
        $.get('/proxy/kvkk/inventory', function(data) {
            self.items(Array.isArray(data) ? data : (data.items || data.data || []));
        }).always(function() { self.isLoading(false); });
    };

    self.openCreate = function() {
        self.form.id(null); self.form.dataCategory(''); self.form.dataType('');
        self.form.retentionPeriod(''); self.form.location(''); self.form.securityMeasure('');
        self.formTitle('Yeni Envanter Kaydi');
        new bootstrap.Modal('#inventoryModal').show();
    };

    self.openEdit = function(item) {
        self.form.id(item.id); self.form.dataCategory(item.dataCategory || '');
        self.form.dataType(item.dataType || '');
        self.form.retentionPeriod(item.retentionPeriod || '');
        self.form.location(item.location || '');
        self.form.securityMeasure(item.securityMeasure || '');
        self.formTitle('Envanter Duzenle');
        new bootstrap.Modal('#inventoryModal').show();
    };

    self.save = function() {
        if (!self.form.dataCategory() || !self.form.dataType()) { toastr.warning('Kategori ve tur zorunludur.'); return; }
        self.isSaving(true);
        var payload = {
            dataCategory: self.form.dataCategory(), dataType: self.form.dataType(),
            retentionPeriod: self.form.retentionPeriod(), location: self.form.location(),
            securityMeasure: self.form.securityMeasure()
        };
        var method = self.form.id() ? 'PUT' : 'POST';
        var url = self.form.id() ? '/proxy/kvkk/inventory/' + self.form.id() : '/proxy/kvkk/inventory';
        $.ajax({
            url: url, method: method, contentType: 'application/json', data: JSON.stringify(payload),
            success: function() {
                toastr.success('Kaydedildi.'); bootstrap.Modal.getInstance(document.getElementById('inventoryModal')).hide();
                self.loadData();
            },
            error: function() { toastr.error('Kaydetme hatasi.'); }
        }).always(function() { self.isSaving(false); });
    };

    self.loadData();
}

ko.applyBindings(new InventoryViewModel(), document.getElementById('inventory-vm'));
