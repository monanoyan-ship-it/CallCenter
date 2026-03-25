function ClientsViewModel() {
    var self = this;
    self.clients = ko.observableArray([]);
    self.searchQuery = ko.observable('');
    self.isEditing = ko.observable(false);
    self.editingId = ko.observable(null);
    self.isSaving = ko.observable(false);
    self.currentPage = ko.observable(1);
    self.totalCount = ko.observable(0);
    self.pageSize = 20;

    self.form = {
        firstName: ko.observable(''),
        lastName: ko.observable(''),
        phoneNumber: ko.observable(''),
        email: ko.observable(''),
        gender: ko.observable(''),
        birthDate: ko.observable(''),
        address: ko.observable(''),
        notes: ko.observable('')
    };

    self.totalPages = ko.computed(function () {
        return Math.ceil(self.totalCount() / self.pageSize) || 1;
    });

    self.pageNumbers = ko.computed(function () {
        var pages = [];
        var total = self.totalPages();
        var current = self.currentPage();
        var start = Math.max(1, current - 2);
        var end = Math.min(total, current + 2);
        for (var i = start; i <= end; i++) pages.push(i);
        return pages;
    });

    self.filteredClients = ko.computed(function () {
        var q = (self.searchQuery() || '').toLowerCase();
        if (!q) return self.clients();
        return self.clients().filter(function (c) {
            return (c.fullName || '').toLowerCase().indexOf(q) >= 0
                || (c.phoneNumber || '').indexOf(q) >= 0
                || (c.email || '').toLowerCase().indexOf(q) >= 0;
        });
    });

    var formModal;

    self.loadData = function () {
        var url = '/proxy/sln-clients?page=' + self.currentPage() + '&pageSize=' + self.pageSize;
        var q = self.searchQuery();
        if (q) url += '&search=' + encodeURIComponent(q);

        $.ajax({ url: url, method: 'GET' }).done(function (data) {
            var items = data.items || data;
            items.forEach(function (c) {
                c.fullName = (c.firstName || '') + ' ' + (c.lastName || '');
                c.genderText = c.gender === 'Female' ? 'Kadin' : c.gender === 'Male' ? 'Erkek' : '-';
            });
            self.clients(items);
            if (data.totalCount !== undefined) self.totalCount(data.totalCount);
        }).fail(function () {
            toastr.error('Musteriler yuklenemedi');
        });
    };

    self.goToPage = function (page) { self.currentPage(page); self.loadData(); };
    self.prevPage = function () { if (self.currentPage() > 1) self.goToPage(self.currentPage() - 1); };
    self.nextPage = function () { if (self.currentPage() < self.totalPages()) self.goToPage(self.currentPage() + 1); };

    self.resetForm = function () {
        self.form.firstName('');
        self.form.lastName('');
        self.form.phoneNumber('');
        self.form.email('');
        self.form.gender('');
        self.form.birthDate('');
        self.form.address('');
        self.form.notes('');
        self.isEditing(false);
        self.editingId(null);
    };

    self.openNew = function () {
        self.resetForm();
        formModal.show();
    };

    self.openEdit = function (client) {
        self.isEditing(true);
        self.editingId(client.id);
        self.form.firstName(client.firstName || '');
        self.form.lastName(client.lastName || '');
        self.form.phoneNumber(client.phoneNumber || '');
        self.form.email(client.email || '');
        self.form.gender(client.gender || '');
        self.form.birthDate(client.birthDate ? client.birthDate.substring(0, 10) : '');
        self.form.address(client.address || '');
        self.form.notes(client.notes || '');
        formModal.show();
    };

    self.save = function () {
        var data = {
            firstName: self.form.firstName(),
            lastName: self.form.lastName(),
            phoneNumber: self.form.phoneNumber(),
            email: self.form.email(),
            gender: self.form.gender(),
            birthDate: self.form.birthDate() || null,
            address: self.form.address(),
            notes: self.form.notes()
        };

        if (!data.firstName || !data.lastName || !data.phoneNumber) {
            toastr.warning('Ad, soyad ve telefon zorunludur');
            return;
        }

        self.isSaving(true);
        var url = '/proxy/sln-clients';
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
            toastr.success(self.isEditing() ? 'Musteri guncellendi' : 'Musteri eklendi');
            self.isSaving(false);
        }).fail(function (xhr) {
            toastr.error(xhr.responseJSON?.error || 'Bir hata olustu');
            self.isSaving(false);
        });
    };

    self.remove = function (client) {
        if (!confirm('Bu musteriyi silmek istediginize emin misiniz?')) return;
        $.ajax({
            url: '/proxy/sln-clients/' + client.id,
            method: 'DELETE'
        }).done(function () {
            self.loadData();
            toastr.success('Musteri silindi');
        }).fail(function () {
            toastr.error('Silinemedi');
        });
    };

    // Arama debounce
    var searchTimer;
    self.searchQuery.subscribe(function () {
        clearTimeout(searchTimer);
        searchTimer = setTimeout(function () {
            self.currentPage(1);
            self.loadData();
        }, 300);
    });

    $(document).ready(function () {
        formModal = new bootstrap.Modal(document.getElementById('clientModal'));
        self.loadData();
    });
}

ko.applyBindings(new ClientsViewModel(), document.getElementById('clients-vm'));
