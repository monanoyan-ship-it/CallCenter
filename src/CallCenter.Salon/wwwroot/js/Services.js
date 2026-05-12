function slnJsT(key, fallback) {
    return (window.salonT || function (k, f) { return f || k; })(key, fallback);
}

function ServicesViewModel() {
    var self = this;
    self.categories = ko.observableArray([]);
    self.services = ko.observableArray([]);
    self.resources = ko.observableArray([]);
    self.combos = ko.observableArray([]);
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
        bufferBeforeMinutes: ko.observable(0),
        bufferAfterMinutes: ko.observable(0),
        processingMinutes: ko.observable(0),
        price: ko.observable(0),
        parentServiceId: ko.observable(null),
        isAddOn: ko.observable(false),
        requiresConsultation: ko.observable(false),
        requiresPatchTest: ko.observable(false),
        prerequisiteNotes: ko.observable(''),
        isActive: ko.observable('true')
    };
    self.serviceResourceSelections = ko.observableArray([]);

    self.resourceForm = {
        id: ko.observable(null),
        name: ko.observable(''),
        resourceKind: ko.observable(''),
        quantity: ko.observable(1),
        notes: ko.observable(''),
        isActive: ko.observable('true')
    };

    self.comboForm = {
        id: ko.observable(null),
        name: ko.observable(''),
        description: ko.observable(''),
        price: ko.observable(0),
        serviceIds: ko.observableArray([]),
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

    function readModalValue(modalSelector, selector, fallback) {
        var $field = $(modalSelector).find(selector).first();
        var value = $field.length ? $field.val() : null;
        return value !== null && value !== undefined ? value : fallback;
    }

    function readModalValues(modalSelector, selector) {
        return $(modalSelector).find(selector).map(function () {
            return $(this).val();
        }).get();
    }

    function ajaxErrorMessage(xhr, fallback) {
        if (xhr && typeof xhr.responseJSON === 'string' && xhr.responseJSON.trim()) {
            return xhr.responseJSON.trim();
        }
        if (xhr && xhr.responseJSON && typeof xhr.responseJSON === 'object') {
            return xhr.responseJSON.message || xhr.responseJSON.error || xhr.responseJSON.title || fallback;
        }

        var raw = xhr && typeof xhr.responseText === 'string' ? xhr.responseText.trim() : '';
        if (!raw) return fallback;

        if ((raw.startsWith('"') && raw.endsWith('"')) || (raw.startsWith("'") && raw.endsWith("'"))) {
            try { return JSON.parse(raw); } catch (e) { }
            return raw.substring(1, raw.length - 1);
        }

        return raw;
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

    self.allServicesFlat = ko.computed(function () {
        var list = [];
        self.categories().forEach(function (cat) {
            (cat.services || []).forEach(function (svc) {
                list.push(svc);
            });
        });
        return self.services().length ? self.services() : list;
    });

    self.parentServiceOptions = ko.computed(function () {
        var editingId = self.isEditingService() ? parseInt(self.editingServiceId()) : 0;
        return self.allServicesFlat().filter(function (svc) {
            return !editingId || svc.id !== editingId;
        });
    });

    self.resourceSummary = function (svc) {
        var items = svc.resourceRequirements || [];
        if (!items.length) return '-';
        return items.map(function (r) {
            return (r.resourceName || '') + (r.quantityRequired > 1 ? ' x' + r.quantityRequired : '');
        }).join(', ');
    };

    self.comboSummary = function (combo) {
        var names = (combo.items || []).map(function (i) { return i.serviceName; }).filter(Boolean);
        return names.length ? names.join(' + ') : '-';
    };

    function rebuildResourceSelections(existing) {
        var byResource = {};
        (existing || []).forEach(function (r) {
            byResource[r.resourceId] = r.quantityRequired || 1;
        });
        self.serviceResourceSelections(self.resources().map(function (r) {
            return {
                resourceId: r.id,
                name: r.name,
                quantity: r.quantity || 1,
                quantityRequired: ko.observable(byResource[r.id] || 0)
            };
        }));
    }

    var categoryModal, serviceModal, resourceModal, comboModal;

    self.loadData = function () {
        $.ajax({ url: '/proxy/sln-services/categories', method: 'GET' }).done(function (data) {
            self.categories(normalizeCategories(data));
        });
        $.ajax({ url: '/proxy/sln-services', method: 'GET' }).done(function (data) {
            self.services(normalizeList(data));
        });
        $.ajax({ url: '/proxy/sln-services/resources', method: 'GET' }).done(function (data) {
            self.resources(normalizeList(data));
            rebuildResourceSelections([]);
        });
        $.ajax({ url: '/proxy/sln-services/combos', method: 'GET' }).done(function (data) {
            self.combos(normalizeList(data));
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
        if (!data.name) { toastr.warning(slnJsT('salon.services.js.kategori_adi_zorunludur', 'Kategori adi zorunludur')); return; }

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
            toastr.success(slnJsT('salon.services.js.kategori_kaydedildi', 'Kategori kaydedildi'));
            self.isSaving(false);
        }).fail(function () {
            toastr.error(slnJsT('salon.services.js.kategori_kaydedilemedi', 'Kategori kaydedilemedi'));
            self.isSaving(false);
        });
    };

    self.removeCategory = function (cat) {
        confirmModal(slnJsT('salon.common.btn.confirm', 'Onayla'), slnJsT('salon.services.js.category_delete_confirm', "'{name}' kategorisini silmek istediğinize emin misiniz?").replace('{name}', cat.name || ''), function() {
            $.ajax({ url: '/proxy/sln-services/categories/' + cat.id, method: 'DELETE' }).done(function () {
                self.loadData();
                toastr.success(slnJsT('salon.services.js.kategori_silindi', 'Kategori silindi'));
            }).fail(function () {
                toastr.error(slnJsT('salon.services.js.kategori_silinemedi', 'Kategori silinemedi'));
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
        self.serviceForm.bufferBeforeMinutes(0);
        self.serviceForm.bufferAfterMinutes(0);
        self.serviceForm.processingMinutes(0);
        self.serviceForm.price(0);
        self.serviceForm.parentServiceId(null);
        self.serviceForm.isAddOn(false);
        self.serviceForm.requiresConsultation(false);
        self.serviceForm.requiresPatchTest(false);
        self.serviceForm.prerequisiteNotes('');
        self.serviceForm.isActive('true');
        rebuildResourceSelections([]);
        self.categoryAutocomplete.clear();
        serviceModal.show();
    };

    self.openEditService = function (svc) {
        self.isEditingService(true);
        self.editingServiceId(svc.id);
        self.serviceForm.name(svc.name);
        self.serviceForm.categoryId(svc.categoryId);
        self.serviceForm.durationMinutes(svc.durationMinutes || 30);
        self.serviceForm.bufferBeforeMinutes(svc.bufferBeforeMinutes || 0);
        self.serviceForm.bufferAfterMinutes(svc.bufferAfterMinutes || 0);
        self.serviceForm.processingMinutes(svc.processingMinutes || 0);
        self.serviceForm.price(svc.price);
        self.serviceForm.parentServiceId(svc.parentServiceId || null);
        self.serviceForm.isAddOn(!!svc.isAddOn);
        self.serviceForm.requiresConsultation(!!svc.requiresConsultation);
        self.serviceForm.requiresPatchTest(!!svc.requiresPatchTest);
        self.serviceForm.prerequisiteNotes(svc.prerequisiteNotes || '');
        self.serviceForm.isActive(svc.isActive ? 'true' : 'false');
        rebuildResourceSelections(svc.resourceRequirements || []);
        // Autocomplete'e mevcut degeri set et
        self.categoryAutocomplete.setFromValue(svc.categoryId);
        serviceModal.show();
    };

    self.saveService = function () {
        var resourceQuantityValues = readModalValues('#serviceModal', '[data-bind*="quantityRequired"]');
        var data = {
            name: self.serviceForm.name(),
            categoryId: parseInt(self.serviceForm.categoryId()) || 0,
            durationMinutes: parseInt(self.serviceForm.durationMinutes()) || 30,
            bufferBeforeMinutes: parseInt(self.serviceForm.bufferBeforeMinutes()) || 0,
            bufferAfterMinutes: parseInt(self.serviceForm.bufferAfterMinutes()) || 0,
            processingMinutes: parseInt(self.serviceForm.processingMinutes()) || 0,
            price: parseFloat(self.serviceForm.price()) || 0,
            parentServiceId: parseInt(self.serviceForm.parentServiceId()) || null,
            isAddOn: !!self.serviceForm.isAddOn(),
            requiresConsultation: !!self.serviceForm.requiresConsultation(),
            requiresPatchTest: !!self.serviceForm.requiresPatchTest(),
            prerequisiteNotes: self.serviceForm.prerequisiteNotes(),
            resourceRequirements: self.serviceResourceSelections()
                .map(function (r, idx) {
                    var quantity = resourceQuantityValues.length > idx ? resourceQuantityValues[idx] : r.quantityRequired();
                    return { resourceId: r.resourceId, quantityRequired: parseInt(quantity) || 0 };
                })
                .filter(function (r) { return r.resourceId && r.quantityRequired > 0; }),
            isActive: self.serviceForm.isActive() === 'true'
        };
        if (!data.name) { toastr.warning(slnJsT('salon.services.js.hizmet_adi_zorunludur', 'Hizmet adı zorunludur')); return; }
        if (!data.categoryId) { toastr.warning(slnJsT('salon.services.js.kategori_secimi_zorunludur', 'Kategori seçimi zorunludur')); return; }

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
            toastr.success(slnJsT('salon.services.js.hizmet_kaydedildi', 'Hizmet kaydedildi'));
            self.isSaving(false);
        }).fail(function () {
            toastr.error(slnJsT('salon.services.js.hizmet_kaydedilemedi', 'Hizmet kaydedilemedi'));
            self.isSaving(false);
        });
    };

    self.openResourceManager = function () {
        self.resourceForm.id(null);
        self.resourceForm.name('');
        self.resourceForm.resourceKind('');
        self.resourceForm.quantity(1);
        self.resourceForm.notes('');
        self.resourceForm.isActive('true');
        resourceModal.show();
    };

    self.editResource = function (resource) {
        self.resourceForm.id(resource.id);
        self.resourceForm.name(resource.name);
        self.resourceForm.resourceKind(resource.resourceKind || '');
        self.resourceForm.quantity(resource.quantity || 1);
        self.resourceForm.notes(resource.notes || '');
        self.resourceForm.isActive(resource.isActive ? 'true' : 'false');
    };

    self.saveResource = function () {
        var selectedBranch = window.slnGetBranch ? parseInt(window.slnGetBranch()) : null;
        var modalName = readModalValue('#resourceModal', '[data-bind*="resourceForm.name"]', self.resourceForm.name());
        var modalKind = readModalValue('#resourceModal', '[data-bind*="resourceForm.resourceKind"]', self.resourceForm.resourceKind());
        var modalQuantity = readModalValue('#resourceModal', '[data-bind*="resourceForm.quantity"]', self.resourceForm.quantity());
        var modalNotes = readModalValue('#resourceModal', '[data-bind*="resourceForm.notes"]', self.resourceForm.notes());
        var modalActive = readModalValue('#resourceModal', '[data-bind*="resourceForm.isActive"]', self.resourceForm.isActive());
        var data = {
            branchId: selectedBranch || null,
            name: (modalName || '').trim(),
            resourceKind: (modalKind || '').trim(),
            quantity: parseInt(modalQuantity) || 1,
            notes: modalNotes,
            isActive: modalActive === 'true'
        };
        if (!data.name) { toastr.warning(slnJsT('salon.services.resource_name_required', 'Kaynak adi zorunludur')); return; }

        var id = self.resourceForm.id();
        $.ajax({
            url: '/proxy/sln-services/resources' + (id ? '/' + id : ''),
            method: id ? 'PUT' : 'POST',
            contentType: 'application/json',
            data: JSON.stringify(data)
        }).done(function () {
            self.loadData();
            self.resourceForm.id(null);
            self.resourceForm.name('');
            self.resourceForm.resourceKind('');
            self.resourceForm.quantity(1);
            self.resourceForm.notes('');
            toastr.success(slnJsT('salon.services.resource_saved', 'Kaynak kaydedildi'));
        }).fail(function (xhr) {
            toastr.error(xhr.responseText || slnJsT('salon.services.resource_save_failed', 'Kaynak kaydedilemedi'));
        });
    };

    self.removeResource = function (resource) {
        confirmModal(slnJsT('salon.common.btn.confirm', 'Onayla'), slnJsT('salon.services.resource_delete_confirm', "'{name}' kaynagini silmek istiyor musunuz?").replace('{name}', resource.name || ''), function () {
            $.ajax({ url: '/proxy/sln-services/resources/' + resource.id, method: 'DELETE' }).done(function () {
                self.loadData();
                toastr.success(slnJsT('salon.services.resource_deleted', 'Kaynak silindi'));
            }).fail(function (xhr) {
                toastr.error(xhr.responseText || slnJsT('salon.services.resource_delete_failed', 'Kaynak silinemedi'));
            });
        });
    };

    self.openComboManager = function () {
        self.comboForm.id(null);
        self.comboForm.name('');
        self.comboForm.description('');
        self.comboForm.price(0);
        self.comboForm.serviceIds([]);
        self.comboForm.isActive('true');
        comboModal.show();
    };

    self.editCombo = function (combo) {
        self.comboForm.id(combo.id);
        self.comboForm.name(combo.name);
        self.comboForm.description(combo.description || '');
        self.comboForm.price(combo.price || 0);
        self.comboForm.serviceIds((combo.items || []).map(function (i) { return i.serviceId; }));
        self.comboForm.isActive(combo.isActive ? 'true' : 'false');
    };

    self.toggleComboService = function (serviceId) {
        var ids = self.comboForm.serviceIds().slice();
        var idx = ids.indexOf(serviceId);
        if (idx >= 0) ids.splice(idx, 1);
        else ids.push(serviceId);
        self.comboForm.serviceIds(ids);
    };

    self.saveCombo = function () {
        var selected = self.comboForm.serviceIds();
        var modalName = readModalValue('#comboModal', '[data-bind*="comboForm.name"]', self.comboForm.name());
        var modalDescription = readModalValue('#comboModal', '[data-bind*="comboForm.description"]', self.comboForm.description());
        var modalPrice = readModalValue('#comboModal', '[data-bind*="comboForm.price"]', self.comboForm.price());
        var modalActive = readModalValue('#comboModal', '[data-bind*="comboForm.isActive"]', self.comboForm.isActive());
        var data = {
            name: (modalName || '').trim(),
            description: modalDescription,
            price: parseFloat(modalPrice) || 0,
            isActive: modalActive === 'true',
            items: selected.map(function (id, idx) { return { serviceId: id, sortOrder: idx + 1 }; })
        };
        if (!data.name) { toastr.warning(slnJsT('salon.services.combo_name_required', 'Combo adi zorunludur')); return; }
        if (!data.items.length) { toastr.warning(slnJsT('salon.services.combo_services_required', 'Combo icin en az bir hizmet secin')); return; }

        var id = self.comboForm.id();
        $.ajax({
            url: '/proxy/sln-services/combos' + (id ? '/' + id : ''),
            method: id ? 'PUT' : 'POST',
            contentType: 'application/json',
            data: JSON.stringify(data)
        }).done(function () {
            self.loadData();
            self.comboForm.id(null);
            self.comboForm.name('');
            self.comboForm.description('');
            self.comboForm.price(0);
            self.comboForm.serviceIds([]);
            toastr.success(slnJsT('salon.services.combo_saved', 'Combo kaydedildi'));
        }).fail(function (xhr) {
            toastr.error(ajaxErrorMessage(xhr, slnJsT('salon.services.combo_save_failed', 'Combo kaydedilemedi')));
        });
    };

    self.removeCombo = function (combo) {
        confirmModal(slnJsT('salon.common.btn.confirm', 'Onayla'), slnJsT('salon.services.combo_delete_confirm', "'{name}' combosunu silmek istiyor musunuz?").replace('{name}', combo.name || ''), function () {
            $.ajax({ url: '/proxy/sln-services/combos/' + combo.id, method: 'DELETE' }).done(function () {
                self.loadData();
                toastr.success(slnJsT('salon.services.combo_deleted', 'Combo silindi'));
            }).fail(function (xhr) {
                toastr.error(xhr.responseText || slnJsT('salon.services.combo_delete_failed', 'Combo silinemedi'));
            });
        });
    };

    self.removeService = function (svc) {
        confirmModal(slnJsT('salon.common.btn.confirm', 'Onayla'), slnJsT('salon.services.js.service_delete_confirm', "'{name}' hizmetini silmek istediğinize emin misiniz?").replace('{name}', svc.name || ''), function() {
            $.ajax({ url: '/proxy/sln-services/' + svc.id, method: 'DELETE' }).done(function () {
                self.loadData();
                toastr.success(slnJsT('salon.services.js.hizmet_silindi', 'Hizmet silindi'));
            }).fail(function () {
                toastr.error(slnJsT('salon.services.js.hizmet_silinemedi', 'Hizmet silinemedi'));
            });
        });
    };

    $(document).ready(function () {
        categoryModal = new bootstrap.Modal(document.getElementById('categoryModal'));
        serviceModal = new bootstrap.Modal(document.getElementById('serviceModal'));
        resourceModal = new bootstrap.Modal(document.getElementById('resourceModal'));
        comboModal = new bootstrap.Modal(document.getElementById('comboModal'));
        self.loadData();
    });
}

ko.applyBindings(new ServicesViewModel(), document.getElementById('services-vm'));
