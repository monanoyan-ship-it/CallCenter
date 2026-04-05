function StaffViewModel() {
    var self = this;
    self.staffList = ko.observableArray([]);
    self.roleList = ko.observableArray([]);
    self.searchQuery = ko.observable('');
    self.isEditing = ko.observable(false);
    self.editingId = ko.observable(null);
    self.isSaving = ko.observable(false);

    self.form = {
        userName: ko.observable(''),
        fullName: ko.observable(''),
        email: ko.observable(''),
        password: ko.observable(''),
        title: ko.observable(''),
        customerRoleId: ko.observable(103),
        isActive: ko.observable('true')
    };

    self.filteredStaff = ko.computed(function () {
        var q = (self.searchQuery() || '').toLowerCase();
        if (!q) return self.staffList();
        return self.staffList().filter(function (s) {
            return (s.fullName || '').toLowerCase().indexOf(q) >= 0
                || (s.email || '').toLowerCase().indexOf(q) >= 0
                || (s.title || '').toLowerCase().indexOf(q) >= 0;
        });
    });

    self.activeCount = ko.computed(function () {
        return self.staffList().filter(function (s) { return s.isActive; }).length;
    });

    var formModal;

    self.loadData = function () {
        $.ajax({ url: '/proxy/portal/personnel', method: 'GET' }).done(function (data) {
            self.staffList(data.items || data);
        }).fail(function () {
            toastr.error('Personel listesi yuklenemedi');
        });
    };

    self.loadRoles = function () {
        $.ajax({ url: '/proxy/portal/salon-roles', method: 'GET' }).done(function (data) {
            self.roleList(data);
        });
    };

    self.resetForm = function () {
        self.form.userName('');
        self.form.fullName('');
        self.form.email('');
        self.form.password('');
        self.form.title('');
        self.form.customerRoleId(103);
        self.form.isActive('true');
        self.isEditing(false);
        self.editingId(null);
    };

    self.openNew = function () {
        self.resetForm();
        formModal.show();
    };

    self.openEdit = function (staff) {
        self.isEditing(true);
        self.editingId(staff.id);
        self.form.userName(staff.userName || '');
        self.form.fullName(staff.fullName || '');
        self.form.email(staff.email || '');
        self.form.password('');
        self.form.title(staff.title || '');
        self.form.customerRoleId(staff.customerRoleId || 103);
        self.form.isActive(staff.isActive ? 'true' : 'false');
        formModal.show();
    };

    function validatePassword(pwd) {
        var errors = [];
        if (pwd.length < 8) errors.push('En az 8 karakter');
        if (!/[A-Z]/.test(pwd)) errors.push('En az 1 buyuk harf');
        if (!/[a-z]/.test(pwd)) errors.push('En az 1 kucuk harf');
        if (!/[0-9]/.test(pwd)) errors.push('En az 1 rakam');
        return errors;
    }

    self.save = function () {
        var data = {
            fullName: self.form.fullName(),
            email: self.form.email(),
            title: self.form.title(),
            customerRoleId: parseInt(self.form.customerRoleId()) || 103,
            isActive: self.form.isActive() === 'true'
        };

        if (!data.fullName || !data.email) {
            toastr.warning('Ad soyad ve e-posta zorunludur');
            return;
        }

        if (!self.isEditing()) {
            data.userName = self.form.userName();
            data.password = self.form.password();
            if (!data.userName) { toastr.warning('Kullanici adi zorunludur'); return; }
            if (!data.password) { toastr.warning('Sifre zorunludur'); return; }
            if (data.userName.length < 3) { toastr.warning('Kullanici adi en az 3 karakter olmali'); return; }

            var pwdErrors = validatePassword(data.password);
            if (pwdErrors.length > 0) {
                toastr.warning('Sifre gereksinimleri:\n' + pwdErrors.join(', '));
                return;
            }
        } else {
            var pwd = self.form.password();
            if (pwd) {
                var pwdErrors = validatePassword(pwd);
                if (pwdErrors.length > 0) {
                    toastr.warning('Sifre gereksinimleri:\n' + pwdErrors.join(', '));
                    return;
                }
                data.password = pwd;
            }
        }

        self.isSaving(true);
        var url = '/proxy/portal/personnel';
        var method = 'POST';
        if (self.isEditing()) {
            url += '/' + self.editingId();
            method = 'PUT';
        }

        $.ajax({
            url: url, method: method,
            contentType: 'application/json',
            data: JSON.stringify(data)
        }).done(function () {
            formModal.hide();
            self.loadData();
            toastr.success(self.isEditing() ? 'Personel guncellendi' : 'Personel eklendi');
            self.isSaving(false);
        }).fail(function (xhr) {
            var msg = xhr.responseJSON?.message || xhr.responseJSON?.error || xhr.responseJSON;
            if (typeof msg === 'string') {
                // Backend hata mesajlarini kullanici dostu hale getir
                if (msg.indexOf('kullanici adi') >= 0) toastr.error('Bu kullanici adi zaten kullaniliyor. Farkli bir isim deneyin.');
                else if (msg.indexOf('e-posta') >= 0) toastr.error('Bu e-posta adresi zaten kullaniliyor.');
                else if (msg.indexOf('limit') >= 0) toastr.error('Maksimum personel limitine ulasildi.');
                else toastr.error(msg);
            } else {
                toastr.error('Bir hata olustu');
            }
            self.isSaving(false);
        });
    };

    self.remove = function (staff) {
        if (!confirm("'" + staff.fullName + "' personelini silmek istediginize emin misiniz?")) return;
        $.ajax({ url: '/proxy/portal/personnel/' + staff.id, method: 'DELETE' }).done(function () {
            self.loadData();
            toastr.success('Personel silindi');
        }).fail(function () {
            toastr.error('Personel silinemedi');
        });
    };

    $(document).ready(function () {
        formModal = new bootstrap.Modal(document.getElementById('staffModal'));
        self.loadRoles();
        self.loadData();
    });
}

ko.applyBindings(new StaffViewModel(), document.getElementById('staff-vm'));
