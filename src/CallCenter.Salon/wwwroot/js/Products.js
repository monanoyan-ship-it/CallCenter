function ProductsViewModel() {
    var self = this;
    self.products = ko.observableArray([]);
    self.categories = ko.observableArray([]);
    self.brands = ko.observableArray([]);
    self.searchQuery = ko.observable('');
    self.selectedCategoryId = ko.observable(null);
    self.isEditing = ko.observable(false);
    self.editingId = ko.observable(null);
    self.isSaving = ko.observable(false);

    self.form = {
        name: ko.observable(''),
        categoryId: ko.observable(null),
        brandId: ko.observable(null),
        barcode: ko.observable(''),
        unit: ko.observable('Adet'),
        stock: ko.observable(0),
        minStock: ko.observable(0),
        vatRate: ko.observable(20),
        purchasePrice: ko.observable(0),
        salePrice: ko.observable(0)
    };

    // ═══ Autocomplete'ler ═══
    self.categoryAutocomplete = createAutocomplete(self.categories, 'name', self.form.categoryId);
    self.brandAutocomplete = createAutocomplete(self.brands, 'name', self.form.brandId);

    self.filteredProducts = ko.computed(function () {
        var q = (self.searchQuery() || '').toLowerCase();
        var catId = self.selectedCategoryId();
        return self.products().filter(function (p) {
            var matchQ = !q || (p.name || '').toLowerCase().indexOf(q) >= 0 || (p.barcode || '').indexOf(q) >= 0;
            var matchCat = !catId || p.categoryId == catId;
            return matchQ && matchCat;
        });
    });

    var formModal;

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
    };

    self.resetForm = function () {
        self.form.name('');
        self.form.categoryId(null);
        self.form.brandId(null);
        self.form.barcode('');
        self.form.unit('Adet');
        self.form.stock(0);
        self.form.minStock(0);
        self.form.vatRate(20);
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

    self.openEdit = function (product) {
        self.isEditing(true);
        self.editingId(product.id);
        self.form.name(product.name || '');
        self.form.categoryId(product.categoryId);
        self.form.brandId(product.brandId);
        self.form.barcode(product.barcode || '');
        self.form.unit(product.unit || 'Adet');
        self.form.stock(product.stock || 0);
        self.form.minStock(product.minStock || 0);
        self.form.vatRate(product.vatRate || 20);
        self.form.purchasePrice(product.purchasePrice || 0);
        self.form.salePrice(product.salePrice || 0);
        // Autocomplete'lere mevcut degerleri set et
        self.categoryAutocomplete.setFromValue(product.categoryId);
        self.brandAutocomplete.setFromValue(product.brandId);
        formModal.show();
    };

    self.save = function () {
        var data = {
            name: self.form.name(),
            categoryId: self.form.categoryId() || null,
            brandId: self.form.brandId() || null,
            barcode: self.form.barcode(),
            unit: self.form.unit(),
            stock: parseInt(self.form.stock()) || 0,
            minStock: parseInt(self.form.minStock()) || 0,
            vatRate: parseInt(self.form.vatRate()) || 0,
            purchasePrice: parseFloat(self.form.purchasePrice()) || 0,
            salePrice: parseFloat(self.form.salePrice()) || 0
        };

        if (!data.name) { toastr.warning('Urun adi zorunludur'); return; }

        self.isSaving(true);
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
            toastr.success(self.isEditing() ? 'Urun guncellendi' : 'Urun eklendi');
            self.isSaving(false);
        }).fail(function (xhr) {
            toastr.error(xhr.responseJSON?.error || 'Bir hata olustu');
            self.isSaving(false);
        });
    };

    self.remove = function (product) {
        if (!confirm("'" + product.name + "' urununu silmek istediginize emin misiniz?")) return;
        $.ajax({ url: '/proxy/sln-products/' + product.id, method: 'DELETE' }).done(function () {
            self.loadData();
            toastr.success('Urun silindi');
        }).fail(function () {
            toastr.error('Urun silinemedi');
        });
    };

    $(document).ready(function () {
        formModal = new bootstrap.Modal(document.getElementById('productModal'));
        self.loadLookups();
        self.loadData();
    });
}

ko.applyBindings(new ProductsViewModel(), document.getElementById('products-vm'));
