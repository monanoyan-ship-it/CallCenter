function DashboardViewModel() {
    var self = this;
    self.data = ko.observable({
        totalCustomers: 0, activeCustomers: 0, totalUsers: 0, activeUsers: 0,
        totalSalonCustomers: 0, totalCallCenterCustomers: 0,
        onlineAgentCount: 0, activeCallCount: 0,
        todayTotalCalls: 0, todayAnsweredCalls: 0, todayMissedCalls: 0,
        customerModules: []
    });
    self.isLoading = ko.observable(true);

    self.loadData = function () {
        self.isLoading(true);
        $.get('/proxy/management/dashboard', function (d) {
            self.data(d);
        }).fail(function () {
            toastr.error('Dashboard verileri yuklenemedi.');
        }).always(function () {
            self.isLoading(false);
        });
    };

    self.loadData();
    // Ahmet talebi: otomatik yenileme kaldırıldı, kullanıcı sayfayı manuel yenilesin.
}
ko.applyBindings(new DashboardViewModel(), document.getElementById('dashboard-vm'));
