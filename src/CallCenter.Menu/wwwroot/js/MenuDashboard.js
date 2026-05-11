(function () {
    function DashboardViewModel() {
        var self = this;

        self.metrics = ko.observableArray([
            { label: 'Aktif menu', value: '1', hint: 'Yayinda public slug' },
            { label: 'Bugun okutma', value: '248', hint: 'QR ve link toplam' },
            { label: 'Bekleyen siparis', value: '7', hint: 'Masa siparis akisi' },
            { label: 'Masadan odeme', value: '12', hint: 'Bugunku tahsilat' }
        ]);

        self.setupItems = ko.observableArray([
            { label: 'Isletme hesabi', status: 'done', icon: 'bi-check2' },
            { label: 'Public menu slug', status: 'done', icon: 'bi-check2' },
            { label: 'Urun ve kategori girisi', status: 'active', icon: 'bi-hourglass-split' },
            { label: 'QR baski ve odeme ayarlari', status: '', icon: 'bi-qr-code' }
        ]);

        self.orders = ko.observableArray([
            { table: '12', customer: 'QR musteri', total: '485 TL', status: 'Hazirlaniyor', badge: 'text-bg-warning', time: '14:18' },
            { table: '4', customer: 'QR musteri', total: '220 TL', status: 'Yeni', badge: 'text-bg-info', time: '14:12' },
            { table: 'Bahce 2', customer: 'QR musteri', total: '760 TL', status: 'Teslim', badge: 'text-bg-success', time: '13:55' }
        ]);

        self.load = function () {
            $.getJSON('/proxy/dashboard')
                .done(function (data) {
                    if (!data) return;
                    if (Array.isArray(data.metrics)) self.metrics(data.metrics);
                    if (Array.isArray(data.orders)) self.orders(data.orders);
                })
                .fail(function (xhr) {
                    if (xhr.status !== 404) return;
                    if (window.toastr) toastr.info('Menu API hazir olana kadar ornek panel verisi gosteriliyor.');
                });
        };
    }

    var root = document.getElementById('menuDashboardApp');
    if (root) {
        var vm = new DashboardViewModel();
        ko.applyBindings(vm, root);
        vm.load();
    }
})();
