function PersonnelViewModel() {
    var self = this;
    self.customers = ko.observableArray([]);
    self.customerId = ko.observable('');
    self.items = ko.observableArray([]);
    self.roles = ko.observableArray([]);
    self.orgUnits = ko.observableArray([]);
    self.isLoading = ko.observable(false);
    self.isSaving = ko.observable(false);
    self.currentPage = ko.observable(1);
    self.totalCount = ko.observable(0);
    self.pageSize = 20;
    self.formTitle = ko.observable('Yeni Personel');
    self.deleteId = null;

    self.form = {
        id: ko.observable(null), fullName: ko.observable(''), userName: ko.observable(''),
        email: ko.observable(''), password: ko.observable(''), title: ko.observable(''),
        customerRoleId: ko.observable(''), organizationUnitId: ko.observable(''),
        reportsToPersonnelId: ko.observable(''), isActive: ko.observable(true)
    };

    self.pageNumbers = ko.computed(function() {
        var total = Math.ceil(self.totalCount() / self.pageSize);
        var pages = [];
        for (var i = 1; i <= total; i++) pages.push(i);
        return pages;
    });

    self.goToPage = function(page) { self.currentPage(page); self.loadData(); };

    self.loadCustomers = function() {
        $.get('/proxy/supervisor/customers', function(data) {
            self.customers(Array.isArray(data) ? data : (data.items || data.data || []));
        });
    };

    self.customerId.subscribe(function(val) {
        self.currentPage(1);
        if (val) { self.loadData(); self.loadOrgUnits(); }
    });

    self.loadData = function() {
        if (!self.customerId()) return;
        self.isLoading(true);
        $.get('/proxy/portal/personnel', { customerId: self.customerId(), page: self.currentPage(), pageSize: self.pageSize }, function(data) {
            var items = Array.isArray(data) ? data : (data.items || data.data || []);
            self.items(items);
            self.totalCount(data.totalCount || data.total || items.length);
        }).always(function() { self.isLoading(false); });
    };

    self.loadOrgUnits = function() {
        $.get('/proxy/organizations', { customerId: self.customerId() }, function(data) {
            var items = Array.isArray(data) ? data : (data.items || data.data || []);
            self.orgUnits(items);
        });
    };

    self.loadRoles = function() {
        // CustomerRoles enum'dan statik liste (API endpoint yok)
        self.roles([
            { id: 1, name: 'Firma Yoneticisi' },
            { id: 2, name: 'Ekip Lideri' },
            { id: 3, name: 'Operator' }
        ]);
    };

    self.resetForm = function() {
        self.form.id(null); self.form.fullName(''); self.form.userName('');
        self.form.email(''); self.form.password(''); self.form.title('');
        self.form.customerRoleId(''); self.form.organizationUnitId('');
        self.form.reportsToPersonnelId(''); self.form.isActive(true);
    };

    self.openCreate = function() { self.resetForm(); self.formTitle('Yeni Personel'); new bootstrap.Modal('#personnelModal').show(); };

    self.openEdit = function(item) {
        self.form.id(item.id); self.form.fullName(item.fullName || '');
        self.form.userName(item.userName || ''); self.form.email(item.email || '');
        self.form.password(''); self.form.title(item.title || '');
        self.form.customerRoleId(item.customerRoleId || '');
        self.form.organizationUnitId(item.organizationUnitId || '');
        self.form.reportsToPersonnelId(item.reportsToPersonnelId || '');
        self.form.isActive(item.isActive !== false);
        self.formTitle('Personel Duzenle');
        new bootstrap.Modal('#personnelModal').show();
    };

    self.save = function() {
        if (!self.form.fullName() || !self.form.userName()) { toastr.warning('Ad soyad ve kullanici adi zorunludur.'); return; }
        if (!self.form.email()) { toastr.warning('E-posta zorunludur.'); return; }
        self.isSaving(true);
        var payload = {
            fullName: self.form.fullName(),
            userName: self.form.userName(),
            email: self.form.email() || '',
            title: self.form.title() || '',
            customerRoleId: self.form.customerRoleId() ? parseInt(self.form.customerRoleId()) : 3,
            organizationUnitId: self.form.organizationUnitId() ? parseInt(self.form.organizationUnitId()) : null,
            reportsToPersonnelId: self.form.reportsToPersonnelId() ? parseInt(self.form.reportsToPersonnelId()) : null,
            isActive: self.form.isActive()
        };
        if (self.form.password()) payload.password = self.form.password();
        var method = self.form.id() ? 'PUT' : 'POST';
        var url = self.form.id() ? '/proxy/portal/personnel/' + self.form.id() + '?customerId=' + self.customerId()
                                 : '/proxy/portal/personnel?customerId=' + self.customerId();
        $.ajax({
            url: url, method: method, contentType: 'application/json', data: JSON.stringify(payload),
            success: function() {
                toastr.success('Kaydedildi.'); bootstrap.Modal.getInstance(document.getElementById('personnelModal')).hide();
                self.loadData();
            },
            error: function(xhr) {
                var msg = 'Kaydetme hatasi.';
                try { var r = JSON.parse(xhr.responseText); msg = r.message || r.title || JSON.stringify(r.errors || r); } catch(e) {}
                toastr.error(msg);
            }
        }).always(function() { self.isSaving(false); });
    };

    self.confirmDelete = function(item) { self.deleteId = item.id; new bootstrap.Modal('#deleteModal').show(); };

    self.executeDelete = function() {
        $.ajax({
            url: '/proxy/portal/personnel/' + self.deleteId + '?customerId=' + self.customerId(), method: 'DELETE',
            success: function() { toastr.success('Silindi.'); bootstrap.Modal.getInstance(document.getElementById('deleteModal')).hide(); self.loadData(); },
            error: function() { toastr.error('Silme hatasi.'); }
        });
    };

    self.unlockUser = function(item) {
        $.ajax({
            url: '/proxy/portal/personnel/' + item.id + '/unlock?customerId=' + self.customerId(), method: 'PUT',
            success: function() { toastr.success('Kilit acildi.'); self.loadData(); },
            error: function() { toastr.error('Kilit acma hatasi.'); }
        });
    };

    self.loadCustomers();
    self.loadRoles();
}

ko.applyBindings(new PersonnelViewModel(), document.getElementById('personnel-vm'));
