function OrganizationsViewModel() {
    var self = this;
    self.customers = ko.observableArray([]);
    self.customerId = ko.observable('');
    self.units = ko.observableArray([]);
    self.isLoading = ko.observable(false);
    self.isSaving = ko.observable(false);
    self.selectedUnit = ko.observable(null);
    self.personnel = ko.observableArray([]);
    self.personnelLoading = ko.observable(false);
    self.formTitle = ko.observable('Yeni Birim');

    self.form = {
        id: ko.observable(null), name: ko.observable(''), code: ko.observable(''),
        unitType: ko.observable('Departman'), parentId: ko.observable(''),
        phone: ko.observable(''), email: ko.observable(''), address: ko.observable(''),
        isActive: ko.observable(true)
    };

    self.loadCustomers = function() {
        $.get('/proxy/supervisor/customers', function(data) {
            self.customers(Array.isArray(data) ? data : (data.items || data.data || []));
        });
    };

    self.customerId.subscribe(function(val) {
        self.selectedUnit(null);
        if (val) self.loadUnits();
    });

    self.loadUnits = function() {
        self.isLoading(true);
        $.get('/proxy/organizations', { customerId: self.customerId() }, function(data) {
            var items = Array.isArray(data) ? data : (data.items || data.data || []);
            // Flatten tree with levels
            var flat = [];
            function flatten(list, level) {
                list.forEach(function(u) {
                    u.level = level;
                    flat.push(u);
                    if (u.children && u.children.length > 0) flatten(u.children, level + 1);
                });
            }
            flatten(items, 0);
            self.units(flat);
        }).always(function() { self.isLoading(false); });
    };

    self.selectUnit = function(unit) {
        self.selectedUnit(unit);
        self.loadPersonnel(unit.id);
    };

    self.loadPersonnel = function(unitId) {
        self.personnelLoading(true);
        $.get('/proxy/organizations/' + unitId + '/personnel', function(data) {
            self.personnel(Array.isArray(data) ? data : (data.items || data.data || []));
        }).always(function() { self.personnelLoading(false); });
    };

    self.removePersonnel = function(p) {
        var unitId = self.selectedUnit().id;
        var pid = p.id || p.personnelId;
        $.ajax({
            url: '/proxy/organizations/' + unitId + '/personnel/' + pid, method: 'DELETE',
            success: function() { toastr.success('Personel cikarildi.'); self.loadPersonnel(unitId); },
            error: function() { toastr.error('Cikarma hatasi.'); }
        });
    };

    self.resetForm = function() {
        self.form.id(null); self.form.name(''); self.form.code('');
        self.form.unitType('Departman'); self.form.parentId('');
        self.form.phone(''); self.form.email(''); self.form.address('');
        self.form.isActive(true);
    };

    self.openCreate = function() { self.resetForm(); self.formTitle('Yeni Birim'); new bootstrap.Modal('#unitModal').show(); };

    self.openEditUnit = function() {
        var u = self.selectedUnit();
        if (!u) return;
        self.form.id(u.id); self.form.name(u.name || ''); self.form.code(u.code || '');
        self.form.unitType(u.unitType || u.unitTypeName || 'Departman'); self.form.parentId(u.parentId || '');
        self.form.phone(u.phone || ''); self.form.email(u.email || ''); self.form.address(u.address || '');
        self.form.isActive(u.isActive !== false);
        self.formTitle('Birim Duzenle');
        new bootstrap.Modal('#unitModal').show();
    };

    self.save = function() {
        if (!self.form.name()) { toastr.warning('Birim adi zorunludur.'); return; }
        self.isSaving(true);
        var payload = {
            customerId: parseInt(self.customerId()), name: self.form.name(), code: self.form.code(),
            unitType: self.form.unitType(), parentId: self.form.parentId() || null,
            phone: self.form.phone(), email: self.form.email(), address: self.form.address(),
            isActive: self.form.isActive()
        };
        var method = self.form.id() ? 'PUT' : 'POST';
        var url = self.form.id() ? '/proxy/organizations/' + self.form.id() : '/proxy/organizations';
        $.ajax({
            url: url, method: method, contentType: 'application/json', data: JSON.stringify(payload),
            success: function() {
                toastr.success('Kaydedildi.'); bootstrap.Modal.getInstance(document.getElementById('unitModal')).hide();
                self.loadUnits();
            },
            error: function() { toastr.error('Kaydetme hatasi.'); }
        }).always(function() { self.isSaving(false); });
    };

    self.deleteUnit = function() {
        var u = self.selectedUnit();
        if (!u) return;
        confirmModal('Birimi Sil', 'Bu birimi silmek istediginize emin misiniz?', function () {
            $.ajax({
                url: '/proxy/organizations/' + u.id, method: 'DELETE',
                success: function() { toastr.success('Silindi.'); self.selectedUnit(null); self.loadUnits(); },
                error: function() { toastr.error('Silme hatasi.'); }
            });
        }, { confirmText: 'Sil', confirmClass: 'btn-danger' });
    };

    self.loadCustomers();
}

ko.applyBindings(new OrganizationsViewModel(), document.getElementById('organizations-vm'));
