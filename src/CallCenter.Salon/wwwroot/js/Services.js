function ServicesViewModel() {
    var self = this;
    self.categories = ko.observableArray([]);
    self.services = ko.observableArray([]);
    self.searchQuery = ko.observable('');
    self.isSaving = ko.observable(false);

    // Category state
    self.isEditingCategory = ko.observable(false);
    self.editingCategoryId = ko.observable(null);
    self.categoryForm = {
        name: ko.observable(''),
        sortOrder: ko.observable(0)
    };

    // Service state
    self.isEditingService = ko.observable(false);
    self.editingServiceId = ko.observable(null);
    self.serviceForm = {
        name: ko.observable(''),
        categoryId: ko.observable(null),
        durationMinutes: ko.observable(30),
        price: ko.observable(0),
        isActive: ko.observable('true')
    };

    // ═══ Autocomplete ═══
    self.categoryAutocomplete = createAutocomplete(self.categories, 'name', self.serviceForm.categoryId);

    function normalizeList(data) {
        if (Array.isArray(data)) return data;
        if (data && Array.isArray(data.items)) return data.items;
        return [];
    }

    function normalizeCategories(data) {
        return normalizeList(data).map(function (cat) {
            cat.services = Array.isArray(cat.services) ? cat.services : [];
            return cat;
        });
    }

    self.filteredCategories = ko.computed(function () {
        var q = (self.searchQuery() || '').toLowerCase();
        var cats = self.categories().map(function (cat) {
            var filtered = (cat.services || []).filter(function (s) {
                return !q || (s.name || '').toLowerCase().indexOf(q) >= 0;
            });
            return { id: cat.id, name: cat.name, sortOrder: cat.sortOrder, services: filtered };
        });
        if (q) return cats.filter(function (c) { return c.services.length > 0; });
        return cats;
    });

    self.uncategorizedServices = ko.computed(function () {
        var q = (self.searchQuery() || '').toLowerCase();
        return self.services().filter(function (s) {
            var matchQ = !q || (s.name || '').toLowerCase().indexOf(q) >= 0;
            return !s.categoryId && matchQ;
        });
    });

    var categoryModal, serviceModal;

    self.loadData = function () {
        $.ajax({ url: '/proxy/sln-services/categories', method: 'GET' }).done(function (data) {
            self.categories(normalizeCategories(data));
        });
        $.ajax({ url: '/proxy/sln-services', method: 'GET' }).done(function (data) {
            self.services(normalizeList(data));
        });
    };

    // Category CRUD
    self.openNewCategory = function () {
        self.isEditingCategory(false);
        self.editingCategoryId(null);
        self.categoryForm.name('');
        self.categoryForm.sortOrder(0);
        categoryModal.show();
    };

    self.openEditCategory = function (cat) {
        self.isEditingCategory(true);
        self.editingCategoryId(cat.id);
        self.categoryForm.name(cat.name);
        self.categoryForm.sortOrder(cat.sortOrder || 0);
        categoryModal.show();
    };

    self.saveCategory = function () {
        var data = { name: self.categoryForm.name(), sortOrder: parseInt(self.categoryForm.sortOrder()) || 0 };
        if (!data.name) { toastr.warning('Kategori adi zorunludur'); return; }

        self.isSaving(true);
        var url = '/proxy/sln-services/categories';
        var method = 'POST';
        if (self.isEditingCategory()) {
            url += '/' + self.editingCategoryId();
            method = 'PUT';
        }

        $.ajax({ url: url, method: method, contentType: 'application/json', data: JSON.stringify(data) }).done(function () {
            categoryModal.hide();
            self.loadData();
            toastr.success('Kategori kaydedildi');
            self.isSaving(false);
        }).fail(function () {
            toastr.error('Kategori kaydedilemedi');
            self.isSaving(false);
        });
    };

    self.removeCategory = function (cat) {
        confirmModal('Onay', "'" + cat.name + "' kategorisini silmek istediginize emin misiniz?", function() {
            $.ajax({ url: '/proxy/sln-services/categories/' + cat.id, method: 'DELETE' }).done(function () {
                self.loadData();
                toastr.success('Kategori silindi');
            }).fail(function () {
                toastr.error('Kategori silinemedi');
            });
        });
    };

    // Service CRUD
    self.openNewService = function () {
        self.isEditingService(false);
        self.editingServiceId(null);
        self.serviceForm.name('');
        self.serviceForm.categoryId(null);
        self.serviceForm.durationMinutes(30);
        self.serviceForm.price(0);
        self.serviceForm.isActive('true');
        self.categoryAutocomplete.clear();
        serviceModal.show();
    };

    self.openEditService = function (svc) {
        self.isEditingService(true);
        self.editingServiceId(svc.id);
        self.serviceForm.name(svc.name);
        self.serviceForm.categoryId(svc.categoryId);
        self.serviceForm.durationMinutes(svc.durationMinutes || 30);
        self.serviceForm.price(svc.price);
        self.serviceForm.isActive(svc.isActive ? 'true' : 'false');
        // Autocomplete'e mevcut degeri set et
        self.categoryAutocomplete.setFromValue(svc.categoryId);
        serviceModal.show();
    };

    self.saveService = function () {
        var data = {
            name: self.serviceForm.name(),
            categoryId: parseInt(self.serviceForm.categoryId()) || 0,
            durationMinutes: parseInt(self.serviceForm.durationMinutes()) || 30,
            price: parseFloat(self.serviceForm.price()) || 0,
            isActive: self.serviceForm.isActive() === 'true'
        };
        if (!data.name) { toastr.warning('Hizmet adi zorunludur'); return; }
        if (!data.categoryId) { toastr.warning('Kategori secimi zorunludur'); return; }

        self.isSaving(true);
        var url = '/proxy/sln-services';
        var method = 'POST';
        if (self.isEditingService()) {
            url += '/' + self.editingServiceId();
            method = 'PUT';
        }

        $.ajax({ url: url, method: method, contentType: 'application/json', data: JSON.stringify(data) }).done(function () {
            serviceModal.hide();
            self.loadData();
            toastr.success('Hizmet kaydedildi');
            self.isSaving(false);
        }).fail(function () {
            toastr.error('Hizmet kaydedilemedi');
            self.isSaving(false);
        });
    };

    self.removeService = function (svc) {
        confirmModal('Onay', "'" + svc.name + "' hizmetini silmek istediginize emin misiniz?", function() {
            $.ajax({ url: '/proxy/sln-services/' + svc.id, method: 'DELETE' }).done(function () {
                self.loadData();
                toastr.success('Hizmet silindi');
            }).fail(function () {
                toastr.error('Hizmet silinemedi');
            });
        });
    };

    $(document).ready(function () {
        categoryModal = new bootstrap.Modal(document.getElementById('categoryModal'));
        serviceModal = new bootstrap.Modal(document.getElementById('serviceModal'));
        self.loadData();
    });
}

ko.applyBindings(new ServicesViewModel(), document.getElementById('services-vm'));
