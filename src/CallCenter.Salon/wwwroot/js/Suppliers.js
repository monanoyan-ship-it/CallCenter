function SuppliersViewModel() {
    var self = this;
    self.suppliers = ko.observableArray([]);
    self.searchQuery = ko.observable('');
    self.isEditing = ko.observable(false);
    self.editingId = ko.observable(null);
    self.isSaving = ko.observable(false);

    self.form = {
        name: ko.observable(''),
        contactPerson: ko.observable(''),
        phoneNumber: ko.observable(''),
        email: ko.observable(''),
        taxNumber: ko.observable(''),
        address: ko.observable(''),
        notes: ko.observable('')
    };

    self.filteredSuppliers = ko.computed(function () {
        var q = (self.searchQuery() || '').toLowerCase();
        if (!q) return self.suppliers();
        return self.suppliers().filter(function (s) {
            return (s.name || '').toLowerCase().indexOf(q) >= 0
                || (s.contactPerson || '').toLowerCase().indexOf(q) >= 0
                || (s.phoneNumber || '').indexOf(q) >= 0;
        });
    });

    self.totalDebit = ko.computed(function () {
        var total = 0;
        self.suppliers().forEach(function (s) { total += s.debit || 0; });
        return total;
    });

    self.totalCredit = ko.computed(function () {
        var total = 0;
        self.suppliers().forEach(function (s) { total += s.credit || 0; });
        return total;
    });

    self.netBalance = ko.computed(function () {
        return self.totalCredit() - self.totalDebit();
    });

    var formModal;

    self.loadData = function () {
        $.ajax({ url: '/proxy/sln-products/suppliers', method: 'GET' }).done(function (data) {
            var items = data.items || data;
            items.forEach(function (s) {
                s.balance = (s.credit || 0) - (s.debit || 0);
            });
            self.suppliers(items);
        }).fail(function () {
            toastr.error('Tedarikciler yuklenemedi');
        });
    };

    self.resetForm = function () {
        self.form.name('');
        self.form.contactPerson('');
        self.form.phoneNumber('');
        self.form.email('');
        self.form.taxNumber('');
        self.form.address('');
        self.form.notes('');
        self.isEditing(false);
        self.editingId(null);
    };

    self.openNew = function () {
        self.resetForm();
        formModal.show();
    };

    self.openEdit = function (supplier) {
        self.isEditing(true);
        self.editingId(supplier.id);
        self.form.name(supplier.name || '');
        self.form.contactPerson(supplier.contactPerson || '');
        self.form.phoneNumber(supplier.phoneNumber || '');
        self.form.email(supplier.email || '');
        self.form.taxNumber(supplier.taxNumber || '');
        self.form.address(supplier.address || '');
        self.form.notes(supplier.notes || '');
        formModal.show();
    };

    self.save = function () {
        var data = {
            name: self.form.name(),
            contactPerson: self.form.contactPerson(),
            phoneNumber: self.form.phoneNumber(),
            email: self.form.email(),
            taxNumber: self.form.taxNumber(),
            address: self.form.address(),
            notes: self.form.notes()
        };

        if (!data.name || !data.phoneNumber) {
            toastr.warning('Firma adi ve telefon zorunludur');
            return;
        }

        self.isSaving(true);
        var url = '/proxy/sln-products/suppliers';
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
            toastr.success(self.isEditing() ? 'Tedarikci guncellendi' : 'Tedarikci eklendi');
            self.isSaving(false);
        }).fail(function (xhr) {
            toastr.error(xhr.responseJSON?.error || 'Bir hata olustu');
            self.isSaving(false);
        });
    };

    self.remove = function (supplier) {
        if (!confirm("'" + supplier.name + "' tedarikcisini silmek istediginize emin misiniz?")) return;
        $.ajax({ url: '/proxy/sln-products/suppliers/' + supplier.id, method: 'DELETE' }).done(function () {
            self.loadData();
            toastr.success('Tedarikci silindi');
        }).fail(function () {
            toastr.error('Tedarikci silinemedi');
        });
    };

    $(document).ready(function () {
        formModal = new bootstrap.Modal(document.getElementById('supplierModal'));
        self.loadData();
    });
}

ko.applyBindings(new SuppliersViewModel(), document.getElementById('suppliers-vm'));
