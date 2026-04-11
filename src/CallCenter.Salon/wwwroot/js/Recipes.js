function RecipesViewModel() {
    var self = this;
    self.recipes = ko.observableArray([]);
    self.productList = ko.observableArray([]);
    self.serviceList = ko.observableArray([]);
    self.searchQuery = ko.observable('');
    self.isEditing = ko.observable(false);
    self.editingId = ko.observable(null);
    self.isSaving = ko.observable(false);

    var unitOptions = ['gr', 'ml', 'adet', 'damla', 'cm'];

    self.form = {
        name: ko.observable(''),
        description: ko.observable(''),
        iconClass: ko.observable(''),
        serviceId: ko.observable(null),
        items: ko.observableArray([])
    };

    self.filteredRecipes = ko.computed(function () {
        var q = (self.searchQuery() || '').toLowerCase();
        if (!q) return self.recipes();
        return self.recipes().filter(function (r) {
            return (r.name || '').toLowerCase().indexOf(q) >= 0
                || (r.description || '').toLowerCase().indexOf(q) >= 0;
        });
    });

    // ═══ Maliyet Hesaplama ═══
    self.calculatedCost = ko.computed(function () {
        var total = 0;
        self.form.items().forEach(function (item) {
            var prod = self.productList().find(function (p) { return p.id == item.productId(); });
            if (prod) total += (prod.purchasePrice || 0) * (parseFloat(item.quantity()) || 0);
        });
        return total;
    });

    self.getItemCost = function (item) {
        var prod = self.productList().find(function (p) { return p.id == item.productId(); });
        if (!prod) return '-';
        var qty = parseFloat(item.quantity()) || 0;
        return (prod.purchasePrice * qty).toLocaleString('tr-TR', { minimumFractionDigits: 2, maximumFractionDigits: 2 }) + ' TL';
    };

    // ═══ Hizmet Autocomplete (opsiyonel) ═══
    self.serviceAutocomplete = createAutocomplete(self.serviceList, 'name', self.form.serviceId);

    var formModal;

    // ═══ Malzeme Satiri Olusturma ═══
    function createRecipeItem(productId, quantity, unit, notes) {
        var item = {
            productId: ko.observable(productId || null),
            quantity: ko.observable(quantity || ''),
            unit: ko.observable(unit || 'gr'),
            notes: ko.observable(notes || ''),
            unitOptions: unitOptions
        };
        item.productAutocomplete = createAutocomplete(self.productList, 'name', item.productId);
        if (productId) {
            item.productAutocomplete.setFromValue(productId);
        }
        // Satir maliyeti
        item.lineCost = ko.computed(function () {
            var prod = self.productList().find(function (p) { return p.id == item.productId(); });
            if (!prod) return 0;
            return (prod.purchasePrice || 0) * (parseFloat(item.quantity()) || 0);
        });
        return item;
    }

    // ═══ Veri Yukleme ═══
    self.loadData = function () {
        $.ajax({ url: '/proxy/sln-recipes', method: 'GET' }).done(function (data) {
            self.recipes(data.items || data);
        });
    };

    self.loadProducts = function () {
        $.ajax({ url: '/proxy/sln-products', method: 'GET' }).done(function (data) {
            self.productList(data.items || data);
        });
    };

    self.loadServices = function () {
        $.ajax({ url: '/proxy/sln-services', method: 'GET' }).done(function (data) {
            var allServices = [];
            var items = data.items || data;
            items.forEach(function (cat) {
                (cat.services || []).forEach(function (s) {
                    allServices.push(s);
                });
            });
            // Eger kategori yapisi yoksa direkt listeyi kullan
            if (allServices.length === 0) {
                self.serviceList(items);
            } else {
                self.serviceList(allServices);
            }
        });
    };

    self.resetForm = function () {
        self.form.name('');
        self.form.description('');
        self.form.iconClass('');
        self.form.serviceId(null);
        self.form.items([]);
        self.serviceAutocomplete.clear();
        self.isEditing(false);
        self.editingId(null);
    };

    self.openNew = function () {
        self.resetForm();
        self.addItem();
        formModal.show();
    };

    self.openEdit = function (recipe) {
        self.isEditing(true);
        self.editingId(recipe.id);
        self.form.name(recipe.name || '');
        self.form.description(recipe.description || '');
        self.form.iconClass(recipe.iconClass || '');
        self.form.serviceId(recipe.serviceId || null);
        if (recipe.serviceId) {
            self.serviceAutocomplete.setFromValue(recipe.serviceId);
        } else {
            self.serviceAutocomplete.clear();
        }
        var items = (recipe.items || []).map(function (item) {
            return createRecipeItem(item.productId, item.quantity, item.unit, item.notes);
        });
        self.form.items(items.length > 0 ? items : []);
        if (items.length === 0) self.addItem();
        formModal.show();
    };

    self.addItem = function () {
        self.form.items.push(createRecipeItem());
    };

    self.removeItem = function (item) {
        self.form.items.remove(item);
    };

    // ═══ Kartlarda hizmet adini bul ═══
    self.getServiceName = function (serviceId) {
        if (!serviceId) return null;
        var svc = self.serviceList().find(function (s) { return s.id == serviceId; });
        return svc ? svc.name : null;
    };

    // ═══ Kaydet ═══
    self.save = function () {
        var items = [];
        var sortOrder = 1;
        self.form.items().forEach(function (item) {
            if (item.productId()) {
                items.push({
                    productId: parseInt(item.productId()),
                    quantity: parseFloat(item.quantity()) || 0,
                    unit: item.unit() || 'gr',
                    notes: item.notes() || '',
                    sortOrder: sortOrder++
                });
            }
        });

        var data = {
            name: self.form.name(),
            description: self.form.description(),
            iconClass: self.form.iconClass(),
            serviceId: self.form.serviceId() ? parseInt(self.form.serviceId()) : null,
            isActive: true,
            estimatedCost: self.calculatedCost(),
            items: items
        };

        if (!data.name) { toastr.warning('Recete adi zorunludur'); return; }
        if (items.length === 0) { toastr.warning('En az bir malzeme ekleyiniz'); return; }

        self.isSaving(true);
        var url = '/proxy/sln-recipes';
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
            toastr.success(self.isEditing() ? 'Recete guncellendi' : 'Recete olusturuldu');
            self.isSaving(false);
        }).fail(function (xhr) {
            toastr.error(xhr.responseJSON?.error || 'Bir hata olustu');
            self.isSaving(false);
        });
    };

    self.remove = function (recipe) {
        if (!confirm("'" + recipe.name + "' recetesini silmek istediginize emin misiniz?")) return;
        $.ajax({ url: '/proxy/sln-recipes/' + recipe.id, method: 'DELETE' }).done(function () {
            self.loadData();
            toastr.success('Recete silindi');
        });
    };

    $(document).ready(function () {
        formModal = new bootstrap.Modal(document.getElementById('recipeModal'));
        self.loadProducts();
        self.loadServices();
        self.loadData();
    });
}

ko.applyBindings(new RecipesViewModel(), document.getElementById('recipes-vm'));
