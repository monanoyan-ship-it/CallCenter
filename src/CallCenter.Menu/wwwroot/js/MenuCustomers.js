(function () {
    function Customer(data) {
        data = data || {};
        this.name = ko.observable(data.name || '');
        this.phone = ko.observable(data.phone || '');
        this.lastVisit = ko.observable(data.lastVisit || '');
        this.spend = ko.observable(data.spend || '');
        this.consent = ko.observable(data.consent || 'Yok');
        this.badge = ko.pureComputed(function () {
            return this.consent() === 'Var' ? 'text-bg-success' : 'text-bg-secondary';
        }, this);
    }

    function CustomersViewModel() {
        var self = this;
        self.customers = ko.observableArray([
            new Customer({ name: 'QR Musteri 001', phone: '05** *** 1122', lastVisit: 'Bugun', spend: '485 TL', consent: 'Var' }),
            new Customer({ name: 'QR Musteri 002', phone: '05** *** 3344', lastVisit: 'Dun', spend: '760 TL', consent: 'Yok' })
        ]);

        self.exportCustomers = function () {
            if (window.toastr) toastr.info('Musteri disa aktarma API fazinda baglanacak.');
        };

        self.load = function () {
            $.getJSON('/proxy/customers')
                .done(function (data) {
                    if (Array.isArray(data)) self.customers(data.map(function (x) { return new Customer(x); }));
                })
                .fail(function (xhr) {
                    if (xhr.status === 404 && window.toastr) toastr.info('Menu API hazir olana kadar ornek musteri verisi gosteriliyor.');
                });
        };
    }

    var root = document.getElementById('menuCustomersApp');
    if (root) {
        var vm = new CustomersViewModel();
        ko.applyBindings(vm, root);
        vm.load();
    }
})();
