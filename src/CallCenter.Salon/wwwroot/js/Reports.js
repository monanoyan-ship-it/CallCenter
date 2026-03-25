function ReportsViewModel() {
    var self = this;

    // Tarih araligi
    var today = new Date();
    var firstOfMonth = new Date(today.getFullYear(), today.getMonth(), 1);
    self.dateFrom = ko.observable(firstOfMonth.toISOString().substring(0, 10));
    self.dateTo = ko.observable(today.toISOString().substring(0, 10));
    self.isLoading = ko.observable(false);

    // Rapor verileri
    self.sales = ko.observable(null);
    self.staff = ko.observable(null);
    self.stock = ko.observable(null);
    self.finance = ko.observable(null);
    self.clientReport = ko.observable(null);

    self.formatMoney = function (val) {
        if (val === null || val === undefined) return '-';
        return parseFloat(val).toLocaleString('tr-TR', { minimumFractionDigits: 2, maximumFractionDigits: 2 }) + ' TL';
    };

    // Hizli tarih seciciler
    self.setToday = function () {
        var d = new Date().toISOString().substring(0, 10);
        self.dateFrom(d);
        self.dateTo(d);
        self.loadSales();
    };

    self.setWeek = function () {
        var now = new Date();
        var dayOfWeek = now.getDay();
        var diff = dayOfWeek === 0 ? 6 : dayOfWeek - 1;
        var monday = new Date(now);
        monday.setDate(now.getDate() - diff);
        self.dateFrom(monday.toISOString().substring(0, 10));
        self.dateTo(now.toISOString().substring(0, 10));
        self.loadSales();
    };

    self.setMonth = function () {
        var now = new Date();
        var first = new Date(now.getFullYear(), now.getMonth(), 1);
        self.dateFrom(first.toISOString().substring(0, 10));
        self.dateTo(now.toISOString().substring(0, 10));
        self.loadSales();
    };

    function dateParams() {
        return 'from=' + self.dateFrom() + '&to=' + self.dateTo();
    }

    self.loadSales = function () {
        self.isLoading(true);
        $.ajax({ url: '/proxy/sln-reports/sales?' + dateParams(), method: 'GET' })
            .done(function (data) { self.sales(data); })
            .fail(function () { toastr.error('Satis raporu yuklenemedi'); })
            .always(function () { self.isLoading(false); });
    };

    self.loadStaff = function () {
        self.isLoading(true);
        $.ajax({ url: '/proxy/sln-reports/staff?' + dateParams(), method: 'GET' })
            .done(function (data) { self.staff(data); })
            .fail(function () { toastr.error('Personel raporu yuklenemedi'); })
            .always(function () { self.isLoading(false); });
    };

    self.loadStock = function () {
        self.isLoading(true);
        $.ajax({ url: '/proxy/sln-reports/stock', method: 'GET' })
            .done(function (data) { self.stock(data); })
            .fail(function () { toastr.error('Stok raporu yuklenemedi'); })
            .always(function () { self.isLoading(false); });
    };

    self.loadFinance = function () {
        self.isLoading(true);
        $.ajax({ url: '/proxy/sln-reports/finance?' + dateParams(), method: 'GET' })
            .done(function (data) { self.finance(data); })
            .fail(function () { toastr.error('Finans raporu yuklenemedi'); })
            .always(function () { self.isLoading(false); });
    };

    self.loadClients = function () {
        self.isLoading(true);
        $.ajax({ url: '/proxy/sln-reports/clients?' + dateParams(), method: 'GET' })
            .done(function (data) { self.clientReport(data); })
            .fail(function () { toastr.error('Musteri raporu yuklenemedi'); })
            .always(function () { self.isLoading(false); });
    };

    // Baslangicta satis raporunu yukle
    $(document).ready(function () {
        self.loadSales();
    });
}

ko.applyBindings(new ReportsViewModel(), document.getElementById('reports-vm'));
