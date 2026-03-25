function BranchesViewModel() {
    var self = this;
    self.branches = ko.observableArray([]);
    self.isEditing = ko.observable(false);
    self.editingId = ko.observable(null);
    self.isSaving = ko.observable(false);

    self.form = {
        name: ko.observable(''),
        address: ko.observable(''),
        phone: ko.observable(''),
        managerPersonnelId: ko.observable(''),
        isActive: ko.observable(true)
    };

    var formModal;

    self.loadData = function () {
        $.ajax({ url: '/proxy/sln-branches', method: 'GET' }).done(function (data) {
            self.branches(data || []);
        }).fail(function () {
            toastr.error('Subeler yuklenemedi');
        });
    };

    self.resetForm = function () {
        self.form.name('');
        self.form.address('');
        self.form.phone('');
        self.form.managerPersonnelId('');
        self.form.isActive(true);
        self.isEditing(false);
        self.editingId(null);
    };

    self.openNew = function () {
        self.resetForm();
        formModal.show();
    };

    self.openEdit = function (branch) {
        self.isEditing(true);
        self.editingId(branch.id);
        self.form.name(branch.name || '');
        self.form.address(branch.address || '');
        self.form.phone(branch.phone || '');
        self.form.managerPersonnelId(branch.managerPersonnelId || '');
        self.form.isActive(branch.isActive);
        formModal.show();
    };

    self.save = function () {
        var data = {
            name: self.form.name(),
            address: self.form.address(),
            phone: self.form.phone(),
            managerPersonnelId: self.form.managerPersonnelId() ? parseInt(self.form.managerPersonnelId()) : null,
            isActive: self.form.isActive()
        };

        if (!data.name) {
            toastr.warning('Sube adi zorunludur');
            return;
        }

        self.isSaving(true);
        var url = '/proxy/sln-branches';
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
            toastr.success(self.isEditing() ? 'Sube guncellendi' : 'Sube eklendi');
            self.isSaving(false);
        }).fail(function (xhr) {
            toastr.error(xhr.responseJSON || 'Bir hata olustu');
            self.isSaving(false);
        });
    };

    self.remove = function (branch) {
        if (!confirm(branch.name + ' subesini silmek istediginize emin misiniz?')) return;
        $.ajax({
            url: '/proxy/sln-branches/' + branch.id,
            method: 'DELETE'
        }).done(function () {
            self.loadData();
            toastr.success('Sube silindi');
        }).fail(function () {
            toastr.error('Silinemedi');
        });
    };

    $(document).ready(function () {
        formModal = new bootstrap.Modal(document.getElementById('branchModal'));
        self.loadData();
    });
}

ko.applyBindings(new BranchesViewModel(), document.getElementById('branches-vm'));
