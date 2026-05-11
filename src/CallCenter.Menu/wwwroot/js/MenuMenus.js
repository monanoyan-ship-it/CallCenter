(function () {
    function Category(data) {
        data = data || {};
        this.name = ko.observable(data.name || '');
        this.productCount = ko.observable(data.productCount || 0);
        this.productCountText = ko.pureComputed(function () {
            return this.productCount() + ' urun';
        }, this);
    }

    function MenusViewModel() {
        var self = this;
        self.menuName = ko.observable('Yayinlanan QR menu');
        self.publicUrl = ko.observable('/m/demo-kafe');
        self.language = ko.observable('TR');
        self.status = ko.observable('Yayinda');
        self.categories = ko.observableArray([
            new Category({ name: 'Kahvalti', productCount: 8 }),
            new Category({ name: 'Ana yemek', productCount: 14 }),
            new Category({ name: 'Tatli', productCount: 6 }),
            new Category({ name: 'Icecek', productCount: 18 })
        ]);

        self.addCategory = function () {
            confirmModal('Kategori ekle', 'Yeni kategori adini yazin.', function (name) {
                if (!name) return;
                self.categories.push(new Category({ name: name, productCount: 0 }));
                if (window.toastr) toastr.success('Kategori eklendi.');
            }, { input: true, inputLabel: 'Kategori adi', confirmText: 'Ekle' });
        };

        self.load = function () {
            $.getJSON('/proxy/menus/current')
                .done(function (data) {
                    if (!data) return;
                    self.menuName(data.menuName || self.menuName());
                    self.publicUrl(data.publicUrl || self.publicUrl());
                    self.language(data.language || self.language());
                    self.status(data.status || self.status());
                    if (Array.isArray(data.categories)) self.categories(data.categories.map(function (x) { return new Category(x); }));
                })
                .fail(function (xhr) {
                    if (xhr.status === 404 && window.toastr) toastr.info('Menu API hazir olana kadar ornek kategori verisi gosteriliyor.');
                });
        };
    }

    var root = document.getElementById('menuMenusApp');
    if (root) {
        var vm = new MenusViewModel();
        ko.applyBindings(vm, root);
        vm.load();
    }
})();
