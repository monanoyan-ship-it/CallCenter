function slnJsT(key, fallback) {
    return (window.salonT || function (k, f) { return f || k; })(key, fallback);
}

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
        phone: ko.observable(''),
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
                || (s.phone || '').indexOf(q) >= 0;
        });
    });

    self.totalBalance = ko.computed(function () {
        var total = 0;
        self.suppliers().forEach(function (s) { total += s.balance || 0; });
        return total;
    });

    var formModal;

    self.loadData = function () {
        $.ajax({ url: '/proxy/sln-products/suppliers', method: 'GET' }).done(function (data) {
            var items = data.items || data;
            // balance API'den hazir gelir
            self.suppliers(items);
        }).fail(function () {
            toastr.error('Tedarikciler yuklenemedi');
        });
    };

    self.resetForm = function () {
        self.form.name('');
        self.form.contactPerson('');
        self.form.phone('');
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
        self.form.phone(supplier.phone || '');
        self.form.email(supplier.email || '');
        // taxNumber, address, notes are not in the list DTO
        // Try to fetch detail if endpoint exists, otherwise use defaults
        self.form.taxNumber('');
        self.form.address('');
        self.form.notes('');

        // Try to load detail data
        $.ajax({ url: '/proxy/sln-products/suppliers/' + supplier.id, method: 'GET' }).done(function (data) {
            self.form.taxNumber(data.taxNumber || '');
            self.form.address(data.address || '');
            self.form.notes(data.notes || '');
        });

        formModal.show();
    };

    self.save = function () {
        var data = {
            name: self.form.name(),
            contactPerson: self.form.contactPerson(),
            phone: self.form.phone(),
            email: self.form.email(),
            taxNumber: self.form.taxNumber(),
            address: self.form.address(),
            notes: self.form.notes()
        };

        if (!data.name || !data.phone) {
            toastr.warning(slnJsT('salon.suppliers.js.firma_adi_ve_telefon_zorunludur', 'Firma adı ve telefon zorunludur'));
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
            toastr.success(self.isEditing() ? slnJsT('salon.suppliers.js.tedarikci_guncellendi', 'Tedarikçi güncellendi') : slnJsT('salon.suppliers.js.tedarikci_eklendi', 'Tedarikçi eklendi'));
            self.isSaving(false);
        }).fail(function (xhr) {
            toastr.error(xhr.responseJSON?.error || slnJsT('salon.common.error.generic', 'Bir hata oluştu'));
            self.isSaving(false);
        });
    };

    self.remove = function (supplier) {
        confirmModal(slnJsT('salon.common.btn.confirm', 'Onayla'), "'" + supplier.name + "' tedarikcisini silmek istediginize emin misiniz?", function() {
            $.ajax({ url: '/proxy/sln-products/suppliers/' + supplier.id, method: 'DELETE' }).done(function () {
                self.loadData();
                toastr.success(slnJsT('salon.suppliers.js.tedarikci_silindi', 'Tedarikçi silindi'));
            }).fail(function () {
                toastr.error(slnJsT('salon.suppliers.js.tedarikci_silinemedi', 'Tedarikçi silinemedi'));
            });
        });
    };

    $(document).ready(function () {
        formModal = new bootstrap.Modal(document.getElementById('supplierModal'));
        self.loadData();
    });
}

ko.applyBindings(new SuppliersViewModel(), document.getElementById('suppliers-vm'));
