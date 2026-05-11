(function () {
    function Order(data) {
        data = data || {};
        this.table = ko.observable(data.table || '');
        this.summary = ko.observable(data.summary || '');
        this.items = ko.observable(data.items || '');
    }

    function OrderColumn(data) {
        data = data || {};
        this.title = ko.observable(data.title || '');
        this.orders = ko.observableArray((data.orders || []).map(function (x) { return new Order(x); }));
    }

    function OrdersViewModel() {
        var self = this;
        self.columns = ko.observableArray([
            new OrderColumn({ title: 'Yeni', orders: [
                { table: 'Masa 4', summary: '2 urun - 220 TL', items: 'Cold Brew, Brownie' },
                { table: 'Masa 8', summary: '4 urun - 540 TL', items: 'Bowl, Ayran, Tatli' }
            ] }),
            new OrderColumn({ title: 'Hazirlaniyor', orders: [
                { table: 'Masa 12', summary: '3 urun - 485 TL', items: 'Kofte, Salata, Soda' }
            ] }),
            new OrderColumn({ title: 'Teslim', orders: [
                { table: 'Bahce 2', summary: '5 urun - 760 TL', items: 'Serpme kahvalti' }
            ] })
        ]);

        self.advanceOrder = function (order) {
            if (window.toastr) toastr.success(order.table() + ' siparis durumu guncellendi.');
        };

        self.load = function () {
            $.getJSON('/proxy/orders/board')
                .done(function (data) {
                    if (Array.isArray(data)) self.columns(data.map(function (x) { return new OrderColumn(x); }));
                })
                .fail(function (xhr) {
                    if (xhr.status === 404 && window.toastr) toastr.info('Menu API hazir olana kadar ornek siparis panosu gosteriliyor.');
                });
        };
    }

    var root = document.getElementById('menuOrdersApp');
    if (root) {
        var vm = new OrdersViewModel();
        ko.applyBindings(vm, root);
        vm.load();
    }
})();
