(function () {
    function Payment(data) {
        data = data || {};
        this.table = ko.observable(data.table || '');
        this.amount = ko.observable(data.amount || '');
        this.method = ko.observable(data.method || '');
        this.status = ko.observable(data.status || '');
        this.time = ko.observable(data.time || '');
        this.badge = ko.pureComputed(function () {
            if (this.status() === 'Basarili') return 'text-bg-success';
            if (this.status() === 'Bekliyor') return 'text-bg-warning';
            return 'text-bg-secondary';
        }, this);
    }

    function PaymentsViewModel() {
        var self = this;
        self.tablePaymentEnabled = ko.observable(true);
        self.tipEnabled = ko.observable(true);
        self.splitPaymentEnabled = ko.observable(false);
        self.metrics = ko.observableArray([
            { label: 'Bugunku tahsilat', value: '4.850 TL', hint: 'Masadan odeme' },
            { label: 'Bekleyen', value: '3', hint: 'Checkout baslatildi' },
            { label: 'Basarili', value: '12', hint: 'Odeme tamamlandi' },
            { label: 'Ortalama sepet', value: '404 TL', hint: 'Masa odemesi' }
        ]);
        self.payments = ko.observableArray([
            new Payment({ table: 'Masa 12', amount: '485 TL', method: 'Kart', status: 'Bekliyor', time: '14:23' }),
            new Payment({ table: 'Bahce 2', amount: '760 TL', method: 'Kart', status: 'Basarili', time: '13:58' }),
            new Payment({ table: 'Masa 6', amount: '310 TL', method: 'Nakit', status: 'Kasada', time: '13:41' })
        ]);

        self.saveSettings = function () {
            if (window.toastr) toastr.success('Masadan odeme ayarlari kaydedildi.');
        };

        self.refresh = function () {
            $.getJSON('/proxy/payments/table')
                .done(function (data) {
                    if (Array.isArray(data)) self.payments(data.map(function (x) { return new Payment(x); }));
                })
                .fail(function (xhr) {
                    if (xhr.status === 404 && window.toastr) toastr.info('Odeme API hazir olana kadar ornek veri gosteriliyor.');
                });
        };
    }

    var root = document.getElementById('menuPaymentsApp');
    if (root) {
        var vm = new PaymentsViewModel();
        ko.applyBindings(vm, root);
        vm.refresh();
    }
})();
