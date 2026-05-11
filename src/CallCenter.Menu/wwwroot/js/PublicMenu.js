(function () {
    if (window.toastr) {
        toastr.options = {
            closeButton: true,
            progressBar: true,
            positionClass: 'toast-bottom-center',
            timeOut: 2200
        };
    }

    function Product(data) {
        data = data || {};
        this.name = ko.observable(data.name || '');
        this.description = ko.observable(data.description || '');
        this.price = ko.observable(data.price || '');
        this.allergens = ko.observable(data.allergens || 'Alerjen yok');
        this.image = ko.observable(data.image || '/img/menu-bowl.svg');
    }

    function Category(data) {
        data = data || {};
        this.id = ko.observable(data.id || '');
        this.name = ko.observable(data.name || '');
        this.products = ko.observableArray((data.products || []).map(function (x) { return new Product(x); }));
    }

    function PublicMenuViewModel(slug) {
        var self = this;
        self.slug = ko.observable(slug || 'demo-kafe');
        self.venueName = ko.observable('Demo Kafe');
        self.venueDescription = ko.observable('El yapimi kahve, taze yemek ve gunluk tatlilar.');
        self.status = ko.observable('Acik');
        self.cartCount = ko.observable(0);
        self.categories = ko.observableArray([
            new Category({ id: 'popular', name: 'Populer', products: [
                { name: 'Izgara Tavuk Bowl', description: 'Tavuk, kinoali salata, avokado ve susam sos.', price: '260 TL', allergens: 'Susam', image: '/img/menu-bowl.svg' },
                { name: 'San Sebastian', description: 'Gunluk cheesecake, Belcika cikolata sos ile.', price: '190 TL', allergens: 'Sut, yumurta', image: '/img/menu-dessert.svg' }
            ] }),
            new Category({ id: 'main', name: 'Ana Yemek', products: [
                { name: 'Smash Burger', description: 'Cift kofteli, cheddar, karamelize sogan ve ozel sos.', price: '310 TL', allergens: 'Gluten, sut', image: '/img/menu-burger.svg' }
            ] }),
            new Category({ id: 'drinks', name: 'Icecek', products: [
                { name: 'Cold Brew', description: '18 saat demleme, buz ve portakal aromasi.', price: '120 TL', allergens: 'Alerjen yok', image: '/img/menu-coffee.svg' }
            ] })
        ]);

        self.cartSummary = ko.pureComputed(function () {
            var count = self.cartCount();
            return count === 0 ? 'Henuz urun secilmedi' : count + ' urun secildi';
        });

        self.addToCart = function (product) {
            self.cartCount(self.cartCount() + 1);
            if (window.toastr) toastr.success(product.name() + ' sepete eklendi.');
        };

        self.goToOrder = function () {
            if (self.cartCount() === 0) {
                if (window.toastr) toastr.warning('Once urun secin.');
                return;
            }

            if (window.toastr) toastr.info('Siparis formu API fazinda baglanacak.');
        };

        self.payAtTable = function () {
            if (window.toastr) toastr.info('Masadan odeme checkout akisi API fazinda baglanacak.');
        };

        self.load = function () {
            $.getJSON('/public-proxy/' + encodeURIComponent(self.slug()))
                .done(function (data) {
                    if (!data) return;
                    self.venueName(data.venueName || self.venueName());
                    self.venueDescription(data.venueDescription || self.venueDescription());
                    self.status(data.status || self.status());
                    if (Array.isArray(data.categories)) self.categories(data.categories.map(function (x) { return new Category(x); }));
                })
                .fail(function () {
                    if (window.toastr) toastr.info('Demo menu ornek veriyle acildi.');
                });
        };
    }

    var root = document.getElementById('publicMenuApp');
    if (root) {
        var vm = new PublicMenuViewModel(root.getAttribute('data-slug'));
        ko.applyBindings(vm, root);
        vm.load();
    }
})();
