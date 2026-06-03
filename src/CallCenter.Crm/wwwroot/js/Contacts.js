function ContactsViewModel() {
    var self = this;
    self.contacts = ko.observableArray([]);
    self.searchQuery = ko.observable('');
    self.isEditing = ko.observable(false);
    self.editingId = ko.observable(null);
    self.isSaving = ko.observable(false);

    self.form = {
        fullName: ko.observable(''),
        phoneNumber: ko.observable(''),
        phoneNumber2: ko.observable(''),
        email: ko.observable(''),
        company: ko.observable(''),
        department: ko.observable(''),
        title: ko.observable(''),
        notes: ko.observable(''),
        isFavorite: ko.observable(false)
    };

    self.filteredContacts = ko.computed(function() {
        var q = (self.searchQuery() || '').toLowerCase();
        if (!q) return self.contacts();
        return self.contacts().filter(function(c) {
            return (c.fullName || '').toLowerCase().indexOf(q) >= 0
                || (c.company || '').toLowerCase().indexOf(q) >= 0
                || (c.phoneNumber || '').indexOf(q) >= 0
                || (c.email || '').toLowerCase().indexOf(q) >= 0;
        });
    });

    var formModal;

    self.loadData = function() {
        $.ajax({
            url: '/proxy/crm/contacts',
            method: 'GET'
        }).done(function(data) {
            self.contacts(data);
        }).fail(function() {
            toastr.error('Kişiler yüklenemedi');
        });
    };

    self.resetForm = function() {
        self.form.fullName('');
        self.form.phoneNumber('');
        self.form.phoneNumber2('');
        self.form.email('');
        self.form.company('');
        self.form.department('');
        self.form.title('');
        self.form.notes('');
        self.form.isFavorite(false);
        self.isEditing(false);
        self.editingId(null);
    };

    self.openNew = function() {
        self.resetForm();
        formModal.show();
    };

    self.openEdit = function(contact) {
        self.isEditing(true);
        self.editingId(contact.id);
        self.form.fullName(contact.fullName || '');
        self.form.phoneNumber(contact.phoneNumber || '');
        self.form.phoneNumber2(contact.phoneNumber2 || '');
        self.form.email(contact.email || '');
        self.form.company(contact.company || '');
        self.form.department(contact.department || '');
        self.form.title(contact.title || '');
        self.form.notes(contact.notes || '');
        self.form.isFavorite(contact.isFavorite || false);
        formModal.show();
    };

    self.save = function() {
        var data = {
            fullName: self.form.fullName(),
            phoneNumber: self.form.phoneNumber(),
            phoneNumber2: self.form.phoneNumber2(),
            email: self.form.email(),
            company: self.form.company(),
            department: self.form.department(),
            title: self.form.title(),
            notes: self.form.notes(),
            isFavorite: self.form.isFavorite()
        };

        if (!data.fullName || !data.phoneNumber) {
            toastr.warning('Ad ve telefon zorunludur');
            return;
        }

        self.isSaving(true);
        var url = '/proxy/crm/contacts';
        var method = 'POST';
        if (self.isEditing()) {
            url += '/' + self.editingId();
            method = 'PUT';
        }

        $.ajax({
            url: url,
            method: method,
            contentType: 'application/json',
            data: JSON.stringify(data)
        }).done(function() {
            formModal.hide();
            self.loadData();
            toastr.success(self.isEditing() ? 'Kişi güncellendi' : 'Kişi eklendi');
            self.isSaving(false);
        }).fail(function(xhr) {
            toastr.error(xhr.responseJSON?.error || 'Bir hata oluştu');
            self.isSaving(false);
        });
    };

    self.remove = function(contact) {
        confirmModal('Kişiyi Sil', 'Bu kişiyi silmek istediğinize emin misiniz?', function () {
            $.ajax({
                url: '/proxy/crm/contacts/' + contact.id,
                method: 'DELETE'
            }).done(function() {
                self.loadData();
                toastr.success('Kişi silindi');
            }).fail(function() {
                toastr.error('Silinemedi');
            });
        });
    };

    $(document).ready(function() {
        formModal = new bootstrap.Modal(document.getElementById('contactModal'));
        self.loadData();
    });
}

ko.applyBindings(new ContactsViewModel(), document.getElementById('contacts-vm'));
