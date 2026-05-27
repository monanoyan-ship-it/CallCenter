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

    function asNumber(value) {
        var parsed = Number(value);
        return isNaN(parsed) ? 0 : parsed;
    }

    self.priorityLabel = function (priority) {
        if (priority === 'high') return reportT('salon.reports.actions.priority.high', 'Acil');
        if (priority === 'medium') return reportT('salon.reports.actions.priority.medium', 'Bugün');
        return reportT('salon.reports.actions.priority.low', 'Fırsat');
    };

    self.priorityBadgeClass = function (priority) {
        if (priority === 'high') return 'bg-danger';
        if (priority === 'medium') return 'bg-warning text-dark';
        return 'bg-info text-dark';
    };

    self.openReportAction = function (action) {
        if (action && action.href) {
            window.location.href = action.href;
        }
    };

    self.reportActions = ko.computed(function () {
        var actions = [];
        var priorityOrder = { high: 0, medium: 1, low: 2 };
        var overview = self.overview() || {};
        var sales = self.sales() || {};
        var stock = self.stock() || {};
        var finance = self.finance() || {};
        var clients = self.clientReport() || {};

        function add(priority, icon, title, description, href, source) {
            actions.push({
                priority: priority,
                icon: icon,
                title: title,
                description: description,
                href: href,
                source: source
            });
        }

        if (self.overview()) {
            if (asNumber(overview.activeStaffCount) === 0) {
                add('high', 'bi-person-exclamation',
                    reportT('salon.reports.actions.staff_missing.title', 'Aktif personel yok'),
                    reportT('salon.reports.actions.staff_missing.desc', 'Randevu takibi için en az bir aktif personel tanımlayın.'),
                    '/Staff',
                    reportT('salon.sidebar.staff', 'Personel'));
            }
            if (asNumber(overview.appointmentCount) === 0) {
                add('high', 'bi-calendar-plus',
                    reportT('salon.reports.actions.no_appointments.title', 'Bu aralıkta randevu yok'),
                    reportT('salon.reports.actions.no_appointments.desc', 'Boş saatleri doldurmak için bekleme listesindeki veya eski müşterilere haber verin.'),
                    '/Appointments',
                    reportT('salon.sidebar.appointments', 'Randevu'));
            }
            if (asNumber(overview.capacityHours) > 0 && asNumber(overview.occupancyPercent) < 35) {
                add('medium', 'bi-graph-up-arrow',
                    reportT('salon.reports.actions.low_occupancy.title', 'Doluluk düşük'),
                    reportT('salon.reports.actions.low_occupancy.desc', 'Doluluk {percent}. Boş saatler için kısa bir kampanya veya hatırlatma gönderebilirsiniz.')
                        .replace('{percent}', self.formatPercent(overview.occupancyPercent)),
                    '/Marketing',
                    reportT('salon.sidebar.marketing', 'Müşteri İlişkileri'));
            }
            if (asNumber(overview.activeClientCount) > 0 && asNumber(overview.repeatVisitRatePercent) < 25) {
                add('medium', 'bi-arrow-repeat',
                    reportT('salon.reports.actions.low_repeat.title', 'Müşteriler yeterince sık dönmüyor'),
                    reportT('salon.reports.actions.low_repeat.desc', 'Tekrar ziyaret {percent}. Sadakat, hatırlatma veya geri çağırma kampanyası deneyin.')
                        .replace('{percent}', self.formatPercent(overview.repeatVisitRatePercent)),
                    '/Marketing',
                    reportT('salon.sidebar.marketing', 'Müşteri İlişkileri'));
            }
        }

        if (self.sales()) {
            if (asNumber(sales.totalInvoices) === 0) {
                add('high', 'bi-receipt',
                    reportT('salon.reports.actions.no_sales.title', 'Satış kaydı yok'),
                    reportT('salon.reports.actions.no_sales.desc', 'Bu tarih aralığında adisyon yok; randevu ve hızlı satış kayıtlarını kontrol edin.'),
                    '/Sales',
                    reportT('salon.sidebar.sales', 'Satış'));
            }
            if (asNumber(sales.serviceRevenue) > 0 && asNumber(sales.productRevenue) === 0) {
                add('low', 'bi-bag-plus',
                    reportT('salon.reports.actions.product_attach.title', 'Hizmet sonrası ürün önerilebilir'),
                    reportT('salon.reports.actions.product_attach.desc', 'Hizmet geliri var ama ürün satışı yok; uygun ürünleri hizmet sonrası önermeyi deneyin.'),
                    '/Products',
                    reportT('salon.sidebar.products', 'Ürün'));
            }
        }

        if (self.stock()) {
            if (asNumber(stock.lowStockCount) > 0) {
                var lowItems = (stock.items || []).filter(function (item) { return item.isLowStock; })
                    .slice(0, 3)
                    .map(function (item) { return item.productName; })
                    .join(', ');
                add('high', 'bi-box-seam',
                    reportT('salon.reports.actions.low_stock.title', 'Stokta azalan ürün var'),
                    reportT('salon.reports.actions.low_stock.desc', '{count} ürün minimum seviyeye yaklaşmış. {items}')
                        .replace('{count}', stock.lowStockCount)
                        .replace('{items}', lowItems || reportT('salon.reports.actions.low_stock.items_fallback', 'Ürün listesini gözden geçirin.')),
                    '/Products',
                    reportT('salon.sidebar.inventory', 'Stok'));
            }
            if (asNumber(stock.supplierDebtTotal) > 0) {
                add('medium', 'bi-truck',
                    reportT('salon.reports.actions.supplier_debt.title', 'Tedarikçi bakiyesi açık'),
                    reportT('salon.reports.actions.supplier_debt.desc', 'Açık bakiye {amount}; ödeme ve tedarik planını gözden geçirin.')
                        .replace('{amount}', self.formatMoney(stock.supplierDebtTotal)),
                    '/Suppliers',
                    reportT('salon.sidebar.suppliers', 'Tedarikçi'));
            }
            if (asNumber(stock.totalProducts) > 0 && asNumber(stock.averageMarginPercent) < 20) {
                add('low', 'bi-tags',
                    reportT('salon.reports.actions.low_margin.title', 'Marj düşük görünüyor'),
                    reportT('salon.reports.actions.low_margin.desc', 'Ortalama marj {percent}. Maliyet ve satış fiyatlarını kontrol edin.')
                        .replace('{percent}', self.formatPercent(stock.averageMarginPercent)),
                    '/Products',
                    reportT('salon.sidebar.products', 'Ürün'));
            }
        }

        if (self.finance()) {
            if (asNumber(finance.netProfit) < 0) {
                add('high', 'bi-cash-coin',
                    reportT('salon.reports.actions.negative_profit.title', 'Dönem zararda'),
                    reportT('salon.reports.actions.negative_profit.desc', 'Net sonuç {amount}. Masraf kalemlerini ve indirimleri kontrol edin.')
                        .replace('{amount}', self.formatMoney(finance.netProfit)),
                    '/Expenses',
                    reportT('salon.sidebar.expenses', 'Masraf'));
            }
            if (asNumber(finance.cashNet) < 0) {
                add('medium', 'bi-wallet2',
                    reportT('salon.reports.actions.cash_negative.title', 'Kasa neti negatif'),
                    reportT('salon.reports.actions.cash_negative.desc', 'Kasa neti {amount}; kasa hareketlerini kapatmadan önce doğrulayın.')
                        .replace('{amount}', self.formatMoney(finance.cashNet)),
                    '/Cash',
                    reportT('salon.sidebar.cash', 'Kasa'));
            }
            if (asNumber(finance.vatPayable) > 0) {
                add('low', 'bi-file-earmark-spreadsheet',
                    reportT('salon.reports.actions.vat_payable.title', 'KDV yükümlülüğü oluştu'),
                    reportT('salon.reports.actions.vat_payable.desc', 'Ödenecek KDV {amount}; muhasebe raporunu dışa aktarın.')
                        .replace('{amount}', self.formatMoney(finance.vatPayable)),
                    '/Reports',
                    reportT('salon.sidebar.finance', 'Finans'));
            }
        }

        if (self.clientReport()) {
            if (asNumber(clients.totalClients) > 0 && asNumber(clients.newClientsInPeriod) === 0) {
                add('medium', 'bi-person-plus',
                    reportT('salon.reports.actions.no_new_clients.title', 'Yeni müşteri gelmemiş'),
                    reportT('salon.reports.actions.no_new_clients.desc', 'Bu dönemde yeni müşteri yok; profil ve randevu linklerini yeniden paylaşmayı deneyin.'),
                    '/Marketing',
                    reportT('salon.sidebar.marketing', 'Müşteri İlişkileri'));
            }
            if (asNumber(clients.totalClients) > 0 && asNumber(clients.averageVisitFrequency) < 1.2) {
                add('low', 'bi-chat-heart',
                    reportT('salon.reports.actions.low_visit_frequency.title', 'Ziyaret sıklığı düşük'),
                    reportT('salon.reports.actions.low_visit_frequency.desc', 'Ortalama ziyaret {count}. Tekrar randevu hatırlatması gönderebilirsiniz.')
                        .replace('{count}', self.formatNumber(clients.averageVisitFrequency, 1)),
                    '/Marketing',
                    reportT('salon.sidebar.marketing', 'Müşteri İlişkileri'));
            }
        }

        return actions
            .sort(function (a, b) { return priorityOrder[a.priority] - priorityOrder[b.priority]; })
            .slice(0, 6);
    });

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
