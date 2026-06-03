function slnJsT(key, fallback) {
    return (window.salonT || function (k, f) { return f || k; })(key, fallback);
}

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
        fullName: ko.observable(''),
        phone: ko.observable(''),
        phone2: ko.observable(''),
        email: ko.observable(''),
        genderId: ko.observable(''),
        birthDate: ko.observable(''),
        city: ko.observable(''),
        address: ko.observable(''),
        hairColor: ko.observable(''),
        skinType: ko.observable(''),
        notes: ko.observable('')
    };

    self.hairColorSuggestions = ko.observableArray([]);
    self.skinTypeSuggestions = ko.observableArray([]);
    self.hairColorAc = createTextAutocomplete(self.hairColorSuggestions, self.form.hairColor);
    self.skinTypeAc = createTextAutocomplete(self.skinTypeSuggestions, self.form.skinType);

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
                || (c.phone || '').indexOf(q) >= 0
                || (c.email || '').toLowerCase().indexOf(q) >= 0;
        });
    });

    var formModal;

    function read(obj, camel, pascal) {
        if (!obj) return undefined;
        if (obj[camel] !== undefined && obj[camel] !== null) return obj[camel];
        return obj[pascal];
    }

    function applyClientToForm(client) {
        self.form.fullName(read(client, 'fullName', 'FullName') || '');
        self.form.phone(read(client, 'phone', 'Phone') || '');
        self.form.phone2(read(client, 'phone2', 'Phone2') || '');
        self.form.email(read(client, 'email', 'Email') || '');
        var genderId = read(client, 'genderId', 'GenderId');
        self.form.genderId(genderId != null ? String(genderId) : '');
        var birthDate = read(client, 'birthDate', 'BirthDate');
        self.form.birthDate(birthDate ? String(birthDate).substring(0, 10) : '');
        self.form.city(read(client, 'city', 'City') || '');
        self.form.address(read(client, 'address', 'Address') || '');
        self.hairColorAc.setFromValue(read(client, 'hairColor', 'HairColor') || '');
        self.skinTypeAc.setFromValue(read(client, 'skinType', 'SkinType') || '');
        self.form.notes(read(client, 'notes', 'Notes') || '');
    }

    self.loadData = function () {
        var url = '/proxy/sln-clients?page=' + self.currentPage() + '&pageSize=' + self.pageSize;
        var q = self.searchQuery();
        if (q) url += '&search=' + encodeURIComponent(q);

        $.ajax({ url: url, method: 'GET' }).done(function (data) {
            var items = data.items || data;
            items.forEach(function (c) {
                c.genderText = c.genderId === 1 ? 'Erkek' : c.genderId === 2 ? 'Kadin' : '-';
            });
            self.clients(items);
            if (data.totalCount !== undefined) self.totalCount(data.totalCount);
        }).fail(function () {
            toastr.error(slnJsT('salon.clients.js.load_failed', 'Müşteriler yüklenemedi'));
        });
    };

    self.goToPage = function (page) { self.currentPage(page); self.loadData(); };
    self.prevPage = function () { if (self.currentPage() > 1) self.goToPage(self.currentPage() - 1); };
    self.nextPage = function () { if (self.currentPage() < self.totalPages()) self.goToPage(self.currentPage() + 1); };

    self.resetForm = function () {
        self.form.fullName('');
        self.form.phone('');
        self.form.phone2('');
        self.form.email('');
        self.form.genderId('');
        self.form.birthDate('');
        self.form.city('');
        self.form.address('');
        self.hairColorAc.clear();
        self.skinTypeAc.clear();
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
        applyClientToForm(client);

        $.ajax({ url: '/proxy/sln-clients/' + client.id, method: 'GET' }).done(function (data) {
            applyClientToForm(data);
        }).fail(function () {
            toastr.warning(slnJsT('salon.clients.js.musteri_detaylari_yuklenemedi_listedeki_bilgilerle_devam_ediliyor', 'Müşteri detayları yüklenemedi, listedeki bilgilerle devam ediliyor.'));
        }).always(function () {
            formModal.show();
        });
    };

    self.save = function () {
        var data = {
            fullName: self.form.fullName(),
            phone: self.form.phone(),
            phone2: self.form.phone2(),
            email: self.form.email(),
            genderId: self.form.genderId() ? parseInt(self.form.genderId()) : null,
            birthDate: self.form.birthDate() ? self.form.birthDate() + 'T00:00:00Z' : null,
            city: self.form.city(),
            address: self.form.address(),
            hairColor: self.form.hairColor(),
            skinType: self.form.skinType(),
            notes: self.form.notes()
        };

        if (!data.fullName || !data.phone) {
            toastr.warning(slnJsT('salon.clients.js.ad_soyad_ve_telefon_zorunludur', 'Ad soyad ve telefon zorunludur'));
            return;
        }

        if (self.isEditing()) {
            data.isFavorite = false;
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
            toastr.success(self.isEditing() ? slnJsT('salon.clients.js.musteri_guncellendi', 'Müşteri güncellendi') : slnJsT('salon.clients.js.musteri_eklendi', 'Müşteri eklendi'));
            self.isSaving(false);
        }).fail(function (xhr) {
            if (window.slnShowAjaxError) {
                window.slnShowAjaxError(xhr, slnJsT('salon.common.error.generic', 'Bir hata oluştu'));
            } else {
                toastr.error(xhr.responseJSON?.error || xhr.responseText || slnJsT('salon.common.error.generic', 'Bir hata oluştu'));
            }
            self.isSaving(false);
        });
    };

    self.remove = function (client) {
        confirmModal(slnJsT('salon.common.btn.confirm', 'Onayla'), slnJsT('salon.clients.js.bu_musteriyi_silmek_istediginize_emin_misiniz', 'Bu müşteriyi silmek istediğinize emin misiniz?'), function() {
            $.ajax({
                url: '/proxy/sln-clients/' + client.id,
                method: 'DELETE'
            }).done(function () {
                self.loadData();
                toastr.success(slnJsT('salon.clients.js.musteri_silindi', 'Müşteri silindi'));
            }).fail(function () {
                toastr.error(slnJsT('salon.clients.js.delete_failed', 'Silinemedi'));
            });
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

    self.loadSuggestions = function () {
        $.ajax({ url: '/proxy/sln-clients/suggestions', method: 'GET' }).done(function (data) {
            self.hairColorSuggestions(data.hairColors || []);
            self.skinTypeSuggestions(data.skinTypes || []);
        });
    };

    $(document).ready(function () {
        formModal = new bootstrap.Modal(document.getElementById('clientModal'));
        self.loadData();
        self.loadSuggestions();
    });
}

ko.applyBindings(new ClientsViewModel(), document.getElementById('clients-vm'));
