(function () {
    function Product(data) {
        data = data || {};
        this.name = ko.observable(data.name || '');
        this.category = ko.observable(data.category || '');
        this.price = ko.observable(data.price || '');
        this.allergens = ko.observable(data.allergens || '-');
        this.status = ko.observable(data.status || 'Taslak');
        this.badge = ko.pureComputed(function () {
            return this.status() === 'Aktif' ? 'text-bg-success' : 'text-bg-secondary';
        }, this);
    }

    function ProductsViewModel() {
        var self = this;
        self.products = ko.observableArray([
            new Product({ name: 'Izgara Tavuk Bowl', category: 'Ana yemek', price: '260 TL', allergens: 'Susam', status: 'Aktif' }),
            new Product({ name: 'San Sebastian', category: 'Tatli', price: '190 TL', allergens: 'Sut, yumurta', status: 'Aktif' }),
            new Product({ name: 'Cold Brew', category: 'Icecek', price: '120 TL', allergens: '-', status: 'Taslak' })
        ]);

        self.addProduct = function () {
            confirmModal('Urun ekle', 'Yeni urun adini yazin.', function (name) {
                if (!name) return;
                self.products.unshift(new Product({ name: name, category: 'Yeni kategori', price: '0 TL', allergens: '-', status: 'Taslak' }));
                if (window.toastr) toastr.success('Urun taslak olarak eklendi.');
            }, { input: true, inputLabel: 'Urun adi', confirmText: 'Ekle' });
        };

        self.editProduct = function (product) {
            if (window.toastr) toastr.info(product.name() + ' duzenleme formu API fazinda baglanacak.');
        };

        self.load = function () {
            $.getJSON('/proxy/products')
                .done(function (data) {
                    if (Array.isArray(data)) self.products(data.map(function (x) { return new Product(x); }));
                })
                .fail(function (xhr) {
                    if (xhr.status === 404 && window.toastr) toastr.info('Menu API hazir olana kadar ornek urun verisi gosteriliyor.');
                });
        };
    }

    var root = document.getElementById('menuProductsApp');
    if (root) {
        var vm = new ProductsViewModel();
        ko.applyBindings(vm, root);
        vm.load();
    }
})();
