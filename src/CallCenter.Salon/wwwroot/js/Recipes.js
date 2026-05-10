function slnJsT(key, fallback) {
    return (window.salonT || function (k, f) { return f || k; })(key, fallback);
}

function RecipesViewModel() {
    var self = this;
    self.recipes = ko.observableArray([]);
    self.productList = ko.observableArray([]);
    self.serviceList = ko.observableArray([]);
    self.searchQuery = ko.observable('');
    self.iconOptions = [
        // Saç
        'bi-scissors', 'bi-paint-bucket', 'bi-palette', 'bi-palette2',
        'bi-droplet', 'bi-droplet-half', 'bi-brush', 'bi-eyedropper',
        // Güzellik / Cilt
        'bi-stars', 'bi-gem', 'bi-flower1', 'bi-flower2', 'bi-flower3',
        'bi-heart-pulse', 'bi-feather', 'bi-sun',
        // Tırnak / El
        'bi-hand-index', 'bi-hand-thumbs-up', 'bi-fingerprint',
        // Genel
        'bi-magic', 'bi-fire', 'bi-snow', 'bi-lightning',
        'bi-moon-stars', 'bi-rainbow', 'bi-water',
        'bi-journal-text', 'bi-clipboard-check', 'bi-prescription2',
        // Vücut
        'bi-person-arms-up', 'bi-body-text', 'bi-emoji-smile'
    ];
    self.isEditing = ko.observable(false);
    self.editingId = ko.observable(null);
    self.isSaving = ko.observable(false);

    var unitOptions = ['gr', 'ml', 'adet', 'damla', 'cm'];

    self.form = {
        name: ko.observable(''),
        description: ko.observable(''),
        iconClass: ko.observable(''),
        serviceId: ko.observable(null),
        photoUrl: ko.observable(''),
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
        return (prod.purchasePrice * qty).toLocaleString(document.documentElement.lang || undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 }) + ' TL';
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
        $.ajax({ url: '/proxy/sln-products', method: 'GET' })
            .done(function (data) { self.productList(data.items || data); })
            .fail(function () { self.productList([]); });
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
        self.form.photoUrl('');
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
        self.form.photoUrl(recipe.photoUrl || '');
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
            photoUrl: self.form.photoUrl() || null,
            isActive: true,
            estimatedCost: self.calculatedCost(),
            items: items
        };

        if (!data.name) { toastr.warning(slnJsT('salon.recipes.js.name_required', 'Reçete adı zorunludur')); return; }
        if (items.length === 0) { toastr.warning(slnJsT('salon.recipes.js.item_required', 'En az bir malzeme ekleyiniz')); return; }

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
            toastr.success(self.isEditing() ? slnJsT('salon.recipes.js.updated', 'Reçete güncellendi') : slnJsT('salon.recipes.js.created', 'Reçete oluşturuldu'));
            self.isSaving(false);
        }).fail(function (xhr) {
            toastr.error(xhr.responseJSON?.error || slnJsT('salon.common.error.generic', 'Bir hata oluştu'));
            self.isSaving(false);
        });
    };

    self.uploadRecipePhoto = function (data, event) {
        var file = event.target.files[0];
        if (!file) return;
        if (file.size > 5 * 1024 * 1024) { toastr.warning(slnJsT('salon.recipes.js.dosya_5_mb_dan_buyuk_olamaz', 'Dosya 5 MB’dan büyük olamaz.')); return; }

        var formData = new FormData();
        formData.append('file', file);
        toastr.info(slnJsT('salon.common.loading', 'Yükleniyor...'));
        $.ajax({
            url: '/proxy/sln-profile/upload-image?type=recipe',
            method: 'POST', data: formData, processData: false, contentType: false
        }).done(function (result) {
            self.form.photoUrl(result.url);
            toastr.success(slnJsT('salon.recipes.js.fotograf_yuklendi', 'Fotoğraf yüklendi.'));
        }).fail(function () { toastr.error(slnJsT('salon.recipes.js.yukleme_hatasi', 'Yükleme hatası.')); });
        event.target.value = '';
    };

    self.remove = function (recipe) {
        confirmModal(slnJsT('salon.common.btn.confirm', 'Onayla'), slnJsT('salon.recipes.js.delete_confirm', "'{name}' reçetesini silmek istediğinize emin misiniz?").replace('{name}', recipe.name || ''), function() {
            $.ajax({ url: '/proxy/sln-recipes/' + recipe.id, method: 'DELETE' }).done(function () {
                self.loadData();
                toastr.success(slnJsT('salon.recipes.js.recete_silindi', 'Recete silindi'));
            });
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
