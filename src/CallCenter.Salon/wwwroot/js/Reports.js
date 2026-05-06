function ReportsViewModel() {
    var self = this;
    var REPORTS_LOCALE = document.documentElement.lang || undefined;
    function reportT(key, fallback) {
        return (window.salonT || function (k, f) { return f || k; })(key, fallback);
    }

    // Tarih araligi
    var today = new Date();
    var firstOfMonth = new Date(today.getFullYear(), today.getMonth(), 1);
    self.dateFrom = ko.observable(firstOfMonth.toISOString().substring(0, 10));
    self.dateTo = ko.observable(today.toISOString().substring(0, 10));
    self.isLoading = ko.observable(false);
    self.activeTab = ko.observable('sales');
    self.isEmailSending = ko.observable(false);
    self.emailForm = {
        toAddresses: ko.observable(''),
        format: ko.observable('pdf'),
        scheduledAt: ko.observable(''),
        subject: ko.observable(''),
        message: ko.observable('')
    };

    // Rapor verileri
    self.overview = ko.observable(null);
    self.sales = ko.observable(null);
    self.staff = ko.observable(null);
    self.stock = ko.observable(null);
    self.finance = ko.observable(null);
    self.clientReport = ko.observable(null);
    self.branchComparison = ko.observable(null);

    self.formatMoney = function (val) {
        if (val === null || val === undefined) return '-';
        return parseFloat(val).toLocaleString(REPORTS_LOCALE, { minimumFractionDigits: 2, maximumFractionDigits: 2 }) + ' TL';
    };

    self.formatPercent = function (val) {
        if (val === null || val === undefined) return '-';
        return parseFloat(val).toLocaleString(REPORTS_LOCALE, { minimumFractionDigits: 0, maximumFractionDigits: 2 }) + '%';
    };

    self.formatNumber = function (val, digits) {
        if (val === null || val === undefined) return '-';
        return parseFloat(val).toLocaleString(REPORTS_LOCALE, { minimumFractionDigits: digits || 0, maximumFractionDigits: digits || 0 });
    };

    // Hizli tarih seciciler
    self.setToday = function () {
        var d = new Date().toISOString().substring(0, 10);
        self.dateFrom(d);
        self.dateTo(d);
        self.refreshReports();
    };

    self.setWeek = function () {
        var now = new Date();
        var dayOfWeek = now.getDay();
        var diff = dayOfWeek === 0 ? 6 : dayOfWeek - 1;
        var monday = new Date(now);
        monday.setDate(now.getDate() - diff);
        self.dateFrom(monday.toISOString().substring(0, 10));
        self.dateTo(now.toISOString().substring(0, 10));
        self.refreshReports();
    };

    self.setMonth = function () {
        var now = new Date();
        var first = new Date(now.getFullYear(), now.getMonth(), 1);
        self.dateFrom(first.toISOString().substring(0, 10));
        self.dateTo(now.toISOString().substring(0, 10));
        self.refreshReports();
    };

    function dateParams() {
        return 'from=' + encodeURIComponent(self.dateFrom()) + '&to=' + encodeURIComponent(self.dateTo());
    }

    self.loadOverview = function () {
        $.ajax({ url: '/proxy/sln-reports/kpis?' + dateParams(), method: 'GET' })
            .done(function (data) {
                data.staffEfficiency = data.staffEfficiency || [];
                self.overview(data);
            })
            .fail(function () { toastr.error(reportT('salon.reports.error.kpi', 'KPI raporu yüklenemedi')); });
    };

    self.refreshReports = function () {
        self.loadOverview();
        switch (self.activeTab()) {
            case 'staff':
                self.loadStaff();
                break;
            case 'stock':
                self.loadStock();
                break;
            case 'finance':
                self.loadFinance();
                break;
            case 'clients':
                self.loadClients();
                break;
            case 'branches':
                self.loadBranchComparison();
                break;
            default:
                self.loadSales();
                break;
        }
    };

    self.loadSales = function () {
        self.activeTab('sales');
        self.isLoading(true);
        $.ajax({ url: '/proxy/sln-reports/sales?' + dateParams(), method: 'GET' })
            .done(function (data) { self.sales(data); })
            .fail(function () { toastr.error(reportT('salon.reports.error.sales', 'Satış raporu yüklenemedi')); })
            .always(function () { self.isLoading(false); });
    };

    self.loadStaff = function () {
        self.activeTab('staff');
        self.isLoading(true);
        $.ajax({ url: '/proxy/sln-reports/staff?' + dateParams(), method: 'GET' })
            .done(function (data) { self.staff(data); })
            .fail(function () { toastr.error(reportT('salon.reports.error.staff', 'Personel raporu yüklenemedi')); })
            .always(function () { self.isLoading(false); });
    };

    self.loadStock = function () {
        self.activeTab('stock');
        self.isLoading(true);
        $.ajax({ url: '/proxy/sln-reports/stock', method: 'GET' })
            .done(function (data) {
                data.taxBreakdown = data.taxBreakdown || [];
                data.supplierDebtBreakdown = data.supplierDebtBreakdown || [];
                data.items = data.items || [];
                self.stock(data);
            })
            .fail(function () { toastr.error(reportT('salon.reports.error.stock', 'Stok raporu yüklenemedi')); })
            .always(function () { self.isLoading(false); });
    };

    self.loadFinance = function () {
        self.activeTab('finance');
        self.isLoading(true);
        $.ajax({ url: '/proxy/sln-reports/finance?' + dateParams(), method: 'GET' })
            .done(function (data) {
                data.paymentMethodBreakdown = data.paymentMethodBreakdown || [];
                data.taxBreakdown = data.taxBreakdown || [];
                data.expenseBreakdown = data.expenseBreakdown || [];
                self.finance(data);
            })
            .fail(function () { toastr.error(reportT('salon.reports.error.finance', 'Finans raporu yüklenemedi')); })
            .always(function () { self.isLoading(false); });
    };

    self.loadClients = function () {
        self.activeTab('clients');
        self.isLoading(true);
        $.ajax({ url: '/proxy/sln-reports/clients?' + dateParams(), method: 'GET' })
            .done(function (data) { self.clientReport(data); })
            .fail(function () { toastr.error(reportT('salon.reports.error.clients', 'Müşteri raporu yüklenemedi')); })
            .always(function () { self.isLoading(false); });
    };

    self.loadBranchComparison = function () {
        self.activeTab('branches');
        self.isLoading(true);
        $.ajax({ url: '/proxy/sln-reports/branch-comparison?' + dateParams(), method: 'GET' })
            .done(function (data) {
                data.branches = data.branches || [];
                data.services = data.services || [];
                data.personnel = data.personnel || [];
                data.products = data.products || [];
                self.branchComparison(data);
            })
            .fail(function () { toastr.error(reportT('salon.reports.error.branches', 'Şube karşılaştırma raporu yüklenemedi')); })
            .always(function () { self.isLoading(false); });
    };

    self.exportReport = function (format) {
        var report = self.activeTab();
        var query = dateParams()
            + '&report=' + encodeURIComponent(report)
            + '&format=' + encodeURIComponent(format);
        toastr.info(reportT('salon.reports.export_preparing', 'Rapor dosyası hazırlanıyor...'));
        window.location.href = '/proxy/sln-reports/export?' + query;
    };

    self.exportCsv = function () {
        self.exportReport('csv');
    };

    self.exportExcel = function () {
        self.exportReport('xlsx');
    };

    self.exportPdf = function () {
        self.exportReport('pdf');
    };

    self.openEmailModal = function () {
        self.emailForm.format('pdf');
        self.emailForm.scheduledAt('');
        self.emailForm.subject('');
        self.emailForm.message('');
        bootstrap.Modal.getOrCreateInstance(document.getElementById('reportEmailModal')).show();
    };

    self.sendReportEmail = function () {
        var recipients = (self.emailForm.toAddresses() || '').split(/[;,\n\r]+/).map(function (x) { return x.trim(); }).filter(Boolean);
        if (recipients.length === 0) {
            toastr.warning(reportT('salon.reports.email_required', 'En az bir e-posta adresi girin.'));
            return;
        }

        self.isEmailSending(true);
        $.ajax({
            url: '/proxy/sln-reports/email',
            method: 'POST',
            contentType: 'application/json',
            data: JSON.stringify({
                report: self.activeTab(),
                format: self.emailForm.format(),
                from: self.dateFrom(),
                to: self.dateTo(),
                toAddresses: recipients,
                scheduledAt: self.emailForm.scheduledAt() || null,
                subject: self.emailForm.subject() || null,
                message: self.emailForm.message() || null
            })
        }).done(function (data) {
            toastr.success((data && data.message) || reportT('salon.reports.email_queued', 'Rapor e-postası işlemi alındı.'));
            bootstrap.Modal.getOrCreateInstance(document.getElementById('reportEmailModal')).hide();
        }).fail(function (xhr) {
            var msg = xhr.responseJSON ? (xhr.responseJSON.message || xhr.responseJSON.error) : reportT('salon.reports.email_failed', 'Rapor e-postası gönderilemedi.');
            toastr.error(msg);
        }).always(function () {
            self.isEmailSending(false);
        });
    };

    // Baslangicta satis raporunu yukle
    $(document).ready(function () {
        self.refreshReports();
    });
}

ko.applyBindings(new ReportsViewModel(), document.getElementById('reports-vm'));
