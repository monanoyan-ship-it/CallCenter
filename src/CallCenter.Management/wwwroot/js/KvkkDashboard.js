function KvkkDashboardViewModel() {
    var self = this;
    self.dashboard = ko.observable({});
    self.recentActivities = ko.observableArray([]);
    self.isLoading = ko.observable(false);

    self.loadData = function() {
        self.isLoading(true);
        $.get('/proxy/kvkk/dashboard', function(data) {
            self.dashboard(data || {});
            self.recentActivities(data.recentActivities || []);
        }).always(function() { self.isLoading(false); });
    };

    self.loadData();
}

ko.applyBindings(new KvkkDashboardViewModel(), document.getElementById('kvkk-vm'));
