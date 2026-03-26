function StaffViewModel() {
    var self = this;
    self.staffList = ko.observableArray([]);
    self.searchQuery = ko.observable('');
    self.isEditing = ko.observable(false);
    self.editingId = ko.observable(null);
    self.isSaving = ko.observable(false);

    self.form = {
        firstName: ko.observable(''),
        lastName: ko.observable(''),
        phoneNumber: ko.observable(''),
        email: ko.observable(''),
        speciality: ko.observable(''),
        startDate: ko.observable(''),
        salary: ko.observable(0),
        commissionRate: ko.observable(0),
        isActive: ko.observable('true'),
        notes: ko.observable('')
    };

    self.filteredStaff = ko.computed(function () {
        var q = (self.searchQuery() || '').toLowerCase();
        if (!q) return self.staffList();
        return self.staffList().filter(function (s) {
            return (s.fullName || '').toLowerCase().indexOf(q) >= 0
                || (s.phoneNumber || '').indexOf(q) >= 0
                || (s.speciality || '').toLowerCase().indexOf(q) >= 0;
        });
    });

    self.totalSalary = ko.computed(function () {
        var total = 0;
        self.staffList().forEach(function (s) { if (s.isActive) total += s.salary || 0; });
        return total;
    });

    self.totalCommission = ko.computed(function () {
        var total = 0;
        self.staffList().forEach(function (s) { if (s.isActive) total += s.monthlyCommission || 0; });
        return total;
    });

    self.activeCount = ko.computed(function () {
        return self.staffList().filter(function (s) { return s.isActive; }).length;
    });

    var formModal;

    self.loadData = function () {
        $.ajax({ url: '/proxy/portal/personnel', method: 'GET' }).done(function (data) {
            var items = data.items || data;
            items.forEach(function (s) {
                s.fullName = (s.firstName || '') + ' ' + (s.lastName || '');
            });
            self.staffList(items);
        }).fail(function () {
            toastr.error('Personel listesi yuklenemedi');
        });
    };

    self.resetForm = function () {
        self.form.firstName('');
        self.form.lastName('');
        self.form.phoneNumber('');
        self.form.email('');
        self.form.speciality('');
        self.form.startDate('');
        self.form.salary(0);
        self.form.commissionRate(0);
        self.form.isActive('true');
        self.form.notes('');
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
        self.form.firstName(staff.firstName || '');
        self.form.lastName(staff.lastName || '');
        self.form.phoneNumber(staff.phoneNumber || '');
        self.form.email(staff.email || '');
        self.form.speciality(staff.speciality || '');
        self.form.startDate(staff.startDate ? staff.startDate.substring(0, 10) : '');
        self.form.salary(staff.salary || 0);
        self.form.commissionRate(staff.commissionRate || 0);
        self.form.isActive(staff.isActive ? 'true' : 'false');
        self.form.notes(staff.notes || '');
        formModal.show();
    };

    self.save = function () {
        var data = {
            firstName: self.form.firstName(),
            lastName: self.form.lastName(),
            phoneNumber: self.form.phoneNumber(),
            email: self.form.email(),
            speciality: self.form.speciality(),
            startDate: self.form.startDate() || null,
            salary: parseFloat(self.form.salary()) || 0,
            commissionRate: parseFloat(self.form.commissionRate()) || 0,
            isActive: self.form.isActive() === 'true',
            notes: self.form.notes()
        };

        if (!data.firstName || !data.lastName || !data.phoneNumber) {
            toastr.warning('Ad, soyad ve telefon zorunludur');
            return;
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
            toastr.error(xhr.responseJSON?.error || 'Bir hata olustu');
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
        self.loadData();
    });
}

ko.applyBindings(new StaffViewModel(), document.getElementById('staff-vm'));
