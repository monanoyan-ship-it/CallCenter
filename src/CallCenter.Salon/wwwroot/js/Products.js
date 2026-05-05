function ProductsViewModel() {
    var self = this;
    self.products = ko.observableArray([]);
    self.categories = ko.observableArray([]);
    self.brands = ko.observableArray([]);
    self.suppliers = ko.observableArray([]);
    self.branches = ko.observableArray([]);
    self.searchQuery = ko.observable('');
    self.selectedCategoryName = ko.observable(null);
    self.isEditing = ko.observable(false);
    self.editingId = ko.observable(null);
    self.isSaving = ko.observable(false);
    self.isPurchaseSaving = ko.observable(false);
    self.isStockOperationSaving = ko.observable(false);
    self.purchaseProduct = ko.observable(null);
    self.stockOperationProduct = ko.observable(null);
    self.stockOperationMode = ko.observable('transfer');

    self.form = {
        name: ko.observable(''),
        categoryId: ko.observable(null),
        brandId: ko.observable(null),
        barcode: ko.observable(''),
        unit: ko.observable('Adet'),
        stockQuantity: ko.observable(0),
        minStockLevel: ko.observable(0),
        purchasePrice: ko.observable(0),
        salePrice: ko.observable(0)
    };

    self.purchaseForm = {
        supplierId: ko.observable(null),
        quantity: ko.observable(1),
        unitPrice: ko.observable(0),
        notes: ko.observable('')
    };

    self.stockOperationForm = {
        fromBranchId: ko.observable(null),
        toBranchId: ko.observable(null),
        branchId: ko.observable(null),
        quantity: ko.observable(1),
        countedQuantity: ko.observable(0),
        notes: ko.observable('')
    };

    // ═══ Autocomplete'ler ═══
    self.categoryAutocomplete = createAutocomplete(self.categories, 'name', self.form.categoryId);
    self.brandAutocomplete = createAutocomplete(self.brands, 'name', self.form.brandId);

    self.filteredProducts = ko.computed(function () {
        var q = (self.searchQuery() || '').toLowerCase();
        var catName = self.selectedCategoryName();
        return self.products().filter(function (p) {
            var matchQ = !q || (p.name || '').toLowerCase().indexOf(q) >= 0 || (p.barcode || '').indexOf(q) >= 0;
            var matchCat = !catName || p.categoryName === catName;
            return matchQ && matchCat;
        });
    });

    self.purchaseTotal = ko.computed(function () {
        var quantity = parseFloat(self.purchaseForm.quantity()) || 0;
        var unitPrice = parseFloat(self.purchaseForm.unitPrice()) || 0;
        return quantity * unitPrice;
    });

    var formModal;
    var purchaseModal;
    var stockOperationModal;

    self.loadData = function () {
        $.ajax({ url: '/proxy/sln-products', method: 'GET' }).done(function (data) {
            self.products(data.items || data);
        }).fail(function () {
            toastr.error('Urunler yuklenemedi');
        });
    };

    self.loadLookups = function () {
        $.ajax({ url: '/proxy/sln-products/categories', method: 'GET' }).done(function (data) {
            self.categories(data);
        });
        $.ajax({ url: '/proxy/sln-products/brands', method: 'GET' }).done(function (data) {
            self.brands(data);
        });
        $.ajax({ url: '/proxy/sln-products/suppliers', method: 'GET' }).done(function (data) {
            self.suppliers(data.items || data);
        });
        $.ajax({ url: '/proxy/sln-branches?_nb=1', method: 'GET' }).done(function (data) {
            self.branches(data.items || data || []);
        });
    };

    self.resetForm = function () {
        self.form.name('');
        self.form.categoryId(null);
        self.form.brandId(null);
        self.form.barcode('');
        self.form.unit('Adet');
        self.form.stockQuantity(0);
        self.form.minStockLevel(0);
        self.form.purchasePrice(0);
        self.form.salePrice(0);
        self.isEditing(false);
        self.editingId(null);
        self.categoryAutocomplete.clear();
        self.brandAutocomplete.clear();
    };

    self.openNew = function () {
        self.resetForm();
        formModal.show();
    };

    self.openPurchase = function (product) {
        self.purchaseProduct(product);
        self.purchaseForm.supplierId(null);
        self.purchaseForm.quantity(1);
        self.purchaseForm.unitPrice(product.purchasePrice || 0);
        self.purchaseForm.notes('');
        purchaseModal.show();
    };

    self.openStockTransfer = function (product) {
        self.stockOperationProduct(product);
        self.stockOperationMode('transfer');
        self.stockOperationForm.fromBranchId(null);
        self.stockOperationForm.toBranchId(null);
        self.stockOperationForm.branchId(null);
        self.stockOperationForm.quantity(1);
        self.stockOperationForm.countedQuantity(product.stockQuantity || 0);
        self.stockOperationForm.notes('');
        stockOperationModal.show();
    };

    self.openStockCount = function (product) {
        self.stockOperationProduct(product);
        self.stockOperationMode('count');
        self.stockOperationForm.fromBranchId(null);
        self.stockOperationForm.toBranchId(null);
        self.stockOperationForm.branchId(null);
        self.stockOperationForm.quantity(1);
        self.stockOperationForm.countedQuantity(product.stockQuantity || 0);
        self.stockOperationForm.notes('');
        stockOperationModal.show();
    };

    self.openEdit = function (product) {
        self.isEditing(true);
        self.editingId(product.id);
        self.form.name(product.name || '');
        self.form.barcode(product.barcode || '');
        self.form.unit(product.unit || 'Adet');
        self.form.stockQuantity(product.stockQuantity || 0);
        self.form.minStockLevel(product.minStockLevel || 0);
        self.form.purchasePrice(product.purchasePrice || 0);
        self.form.salePrice(product.salePrice || 0);
        // DTO da categoryId/brandId yok, isimden bulalim
        var matchedCat = self.categories().find(function (c) { return c.name === product.categoryName; });
        var matchedBrand = self.brands().find(function (b) { return b.name === product.brandName; });
        var catId = matchedCat ? matchedCat.id : null;
        var brandId = matchedBrand ? matchedBrand.id : null;
        self.form.categoryId(catId);
        self.form.brandId(brandId);
        self.categoryAutocomplete.setFromValue(catId);
        self.brandAutocomplete.setFromValue(brandId);
        formModal.show();
    };

    // Autocomplete'de secilmemis ama yazilmis isim varsa otomatik olustur
    function ensureLookup(autocomplete, formField, listObservable, createUrl) {
        return new Promise(function (resolve) {
            // ID zaten secilmisse direkt don
            var selectedId = formField();
            if (selectedId) { resolve(selectedId); return; }

            // Yazilan text var mi?
            var text = (autocomplete.query() || '').trim();
            if (!text) { resolve(null); return; }

            // Listede var mi? (case-insensitive)
            var existing = listObservable().find(function (item) {
                return (item.name || '').toLowerCase() === text.toLowerCase();
            });
            if (existing) { resolve(existing.id); return; }

            // Yoksa olustur
            $.ajax({
                url: createUrl, method: 'POST',
                contentType: 'application/json',
                data: JSON.stringify({ name: text, sortOrder: 0 })
            }).done(function (created) {
                var list = listObservable();
                list.push(created);
                listObservable(list);
                resolve(created.id);
            }).fail(function () { resolve(null); });
        });
    }

    self.save = function () {
        if (!self.form.name()) { toastr.warning('Urun adi zorunludur'); return; }

        // Kategori zorunlu - secilmemis ve yazilmamissa uyar
        var catText = (self.categoryAutocomplete.query() || '').trim();
        if (!self.form.categoryId() && !catText) {
            toastr.warning('Kategori zorunludur');
            return;
        }

        self.isSaving(true);

        Promise.all([
            ensureLookup(self.categoryAutocomplete, self.form.categoryId, self.categories, '/proxy/sln-products/categories'),
            ensureLookup(self.brandAutocomplete, self.form.brandId, self.brands, '/proxy/sln-products/brands')
        ]).then(function (results) {
            if (!results[0]) {
                toastr.error('Kategori olusturulamadi');
                self.isSaving(false);
                return;
            }

            var data = {
                name: self.form.name(),
                categoryId: results[0],
                brandId: results[1],
                barcode: self.form.barcode(),
                unit: self.form.unit(),
                stockQuantity: parseInt(self.form.stockQuantity()) || 0,
                minStockLevel: parseInt(self.form.minStockLevel()) || 0,
                purchasePrice: parseFloat(self.form.purchasePrice()) || 0,
                salePrice: parseFloat(self.form.salePrice()) || 0
            };

            var url = '/proxy/sln-products';
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
                self.loadLookups();
                toastr.success(self.isEditing() ? 'Urun guncellendi' : 'Urun eklendi');
            }).fail(function (xhr) {
                toastr.error(xhr.responseJSON?.error || 'Bir hata olustu');
            }).always(function () { self.isSaving(false); });
        });
    };

    function getErrorMessage(xhr, fallback) {
        if (xhr.responseJSON && xhr.responseJSON.error) return xhr.responseJSON.error;
        if (xhr.responseText) return xhr.responseText.replace(/^"|"$/g, '');
        return fallback;
    }

    self.savePurchase = function () {
        var product = self.purchaseProduct();
        if (!product) { return; }

        var supplierId = parseInt(self.purchaseForm.supplierId());
        var quantity = parseFloat(self.purchaseForm.quantity()) || 0;
        var unitPrice = parseFloat(self.purchaseForm.unitPrice()) || 0;

        if (!supplierId) { toastr.warning('Tedarikci secilmelidir'); return; }
        if (quantity <= 0) { toastr.warning("Miktar 0'dan buyuk olmalidir"); return; }
        if (unitPrice <= 0) { toastr.warning("Alis fiyati 0'dan buyuk olmalidir"); return; }

        self.isPurchaseSaving(true);

        $.ajax({
            url: '/proxy/sln-products/' + product.id + '/stock-movements',
            method: 'POST',
            contentType: 'application/json',
            data: JSON.stringify({
                movementTypeId: 1,
                quantity: quantity,
                unitPrice: unitPrice,
                supplierId: supplierId,
                notes: self.purchaseForm.notes()
            })
        }).done(function () {
            purchaseModal.hide();
            self.loadData();
            toastr.success('Alis kaydi eklendi, tedarikci carisi guncellendi');
        }).fail(function (xhr) {
            toastr.error(getErrorMessage(xhr, 'Alis kaydi eklenemedi'));
        }).always(function () {
            self.isPurchaseSaving(false);
        });
    };

    self.saveStockOperation = function () {
        var product = self.stockOperationProduct();
        if (!product) { return; }

        self.isStockOperationSaving(true);

        if (self.stockOperationMode() === 'transfer') {
            var toBranchId = parseInt(self.stockOperationForm.toBranchId());
            var quantity = parseFloat(self.stockOperationForm.quantity()) || 0;
            if (!toBranchId) {
                toastr.warning('Hedef sube secilmelidir');
                self.isStockOperationSaving(false);
                return;
            }
            if (quantity <= 0) {
                toastr.warning("Transfer miktari 0'dan buyuk olmalidir");
                self.isStockOperationSaving(false);
                return;
            }

            $.ajax({
                url: '/proxy/sln-products/' + product.id + '/stock-transfer',
                method: 'POST',
                contentType: 'application/json',
                data: JSON.stringify({
                    fromBranchId: parseInt(self.stockOperationForm.fromBranchId()) || null,
                    toBranchId: toBranchId,
                    quantity: quantity,
                    notes: self.stockOperationForm.notes()
                })
            }).done(function () {
                stockOperationModal.hide();
                self.loadData();
                toastr.success('Stok transfer audit kaydi olusturuldu');
            }).fail(function (xhr) {
                toastr.error(getErrorMessage(xhr, 'Stok transferi kaydedilemedi'));
            }).always(function () {
                self.isStockOperationSaving(false);
            });
            return;
        }

        var countedQuantity = parseFloat(self.stockOperationForm.countedQuantity());
        if (isNaN(countedQuantity) || countedQuantity < 0) {
            toastr.warning('Sayilan stok negatif olamaz');
            self.isStockOperationSaving(false);
            return;
        }

        $.ajax({
            url: '/proxy/sln-products/' + product.id + '/stock-count',
            method: 'POST',
            contentType: 'application/json',
            data: JSON.stringify({
                branchId: parseInt(self.stockOperationForm.branchId()) || null,
                countedQuantity: countedQuantity,
                notes: self.stockOperationForm.notes()
            })
        }).done(function () {
            stockOperationModal.hide();
            self.loadData();
            toastr.success('Sayim farki kaydedildi');
        }).fail(function (xhr) {
            toastr.error(getErrorMessage(xhr, 'Sayim farki kaydedilemedi'));
        }).always(function () {
            self.isStockOperationSaving(false);
        });
    };

    self.remove = function (product) {
        confirmModal('Onay', "'" + product.name + "' urununu silmek istediginize emin misiniz?", function() {
            $.ajax({ url: '/proxy/sln-products/' + product.id, method: 'DELETE' }).done(function () {
                self.loadData();
                toastr.success('Urun silindi');
            }).fail(function () {
                toastr.error('Urun silinemedi');
            });
        });
    };

    $(document).ready(function () {
        formModal = new bootstrap.Modal(document.getElementById('productModal'));
        purchaseModal = new bootstrap.Modal(document.getElementById('purchaseModal'));
        stockOperationModal = new bootstrap.Modal(document.getElementById('stockOperationModal'));
        self.loadLookups();
        self.loadData();
    });
}

ko.applyBindings(new ProductsViewModel(), document.getElementById('products-vm'));
