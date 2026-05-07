function UsersViewModel() {
    var self = this;
    self.users = ko.observableArray([]);
    self.isLoading = ko.observable(false);
    self.isSaving = ko.observable(false);
    self.searchText = ko.observable('');
    self.roleFilter = ko.observable('');
    self.currentPage = ko.observable(1);
    self.totalCount = ko.observable(0);
    self.totalPages = ko.observable(1);
    self.deleteTarget = ko.observable(null);

    self.form = {
        id: ko.observable(null), userName: ko.observable(''), fullName: ko.observable(''),
        email: ko.observable(''), password: ko.observable(''), roleId: ko.observable('1'),
        extension: ko.observable(''), isActive: ko.observable(true)
    };
    self.formTitle = ko.observable('Yeni Kullanici');

    self.pageNumbers = ko.computed(function () {
        var p = []; for (var i = 1; i <= self.totalPages(); i++) p.push(i); return p;
    });

    self.loadData = function () {
        self.isLoading(true);
        var url = '/proxy/users?page=' + self.currentPage() + '&pageSize=20';
        if (self.searchText()) url += '&search=' + encodeURIComponent(self.searchText());
        if (self.roleFilter()) url += '&roleId=' + self.roleFilter();
        $.get(url, function (data) {
            self.users(data.items || data || []);
            self.totalCount(data.totalCount || 0);
            self.totalPages(data.totalPages || 1);
        }).always(function () { self.isLoading(false); });
    };

    self.onSearchKeyUp = function (d, e) { if (e.key === 'Enter') { self.currentPage(1); self.loadData(); } return true; };
    self.goToPage = function (p) { self.currentPage(p); self.loadData(); };

    self.roleFilter.subscribe(function () { self.currentPage(1); self.loadData(); });

    self.openCreate = function () {
        self.form.id(null); self.form.userName(''); self.form.fullName('');
        self.form.email(''); self.form.password(''); self.form.roleId('1');
        self.form.extension(''); self.form.isActive(true);
        self.formTitle('Yeni Kullanici');
        new bootstrap.Modal('#userModal').show();
    };

    self.openEdit = function (u) {
        self.form.id(u.id); self.form.userName(u.userName); self.form.fullName(u.fullName);
        self.form.email(u.email || ''); self.form.password('');
        self.form.roleId(String(u.roleId || 1)); self.form.extension(u.extension || '');
        self.form.isActive(u.isActive !== false);
        self.formTitle('Kullanici Duzenle');
        new bootstrap.Modal('#userModal').show();
    };

    self.saveUser = function () {
        if (!self.form.fullName()) { toastr.warning('Ad soyad zorunlu.'); return; }
        self.isSaving(true);
        var payload = {
            userName: self.form.userName(), fullName: self.form.fullName(),
            email: self.form.email(), roleId: parseInt(self.form.roleId()),
            extension: self.form.extension(), isActive: self.form.isActive()
        };
        if (self.form.password()) payload.password = self.form.password();

        $.ajax({
            url: self.form.id() ? '/proxy/users/' + self.form.id() : '/proxy/users',
            method: self.form.id() ? 'PUT' : 'POST',
            contentType: 'application/json', data: JSON.stringify(payload),
            success: function () {
                toastr.success('Kaydedildi.');
                bootstrap.Modal.getInstance(document.getElementById('userModal')).hide();
                self.loadData();
            },
            error: function (xhr) { toastr.error(xhr.responseJSON?.message || 'Hata.'); },
            complete: function () { self.isSaving(false); }
        });
    };

    self.verifyTarget = ko.observable(null);
    self.confirmVerify = function (u) {
        self.verifyTarget(u);
        new bootstrap.Modal('#verifyEmailModal').show();
    };
    self.executeVerify = function () {
        if (!self.verifyTarget()) return;
        $.ajax({
            url: '/proxy/users/' + self.verifyTarget().id + '/verify-email-manual', method: 'POST',
            success: function () {
                toastr.success('Email doğrulandı olarak işaretlendi.');
                bootstrap.Modal.getInstance(document.getElementById('verifyEmailModal')).hide();
                self.loadData();
            },
            error: function (xhr) { toastr.error(xhr.responseJSON?.message || 'İşlem başarısız.'); }
        });
    };

    self.confirmDelete = function (u) {
        self.deleteTarget(u);
        new bootstrap.Modal('#deleteUserModal').show();
    };
    self.executeDelete = function () {
        if (!self.deleteTarget()) return;
        $.ajax({
            url: '/proxy/users/' + self.deleteTarget().id, method: 'DELETE',
            success: function () {
                toastr.success('Silindi.');
                bootstrap.Modal.getInstance(document.getElementById('deleteUserModal')).hide();
                self.loadData();
            },
            error: function () { toastr.error('Silme hatasi.'); }
        });
    };

    self.loadData();
}
ko.applyBindings(new UsersViewModel(), document.getElementById('users-vm'));
