function DashboardViewModel() {
    var self = this;
    self.totalContacts = ko.observable(0);
    self.openTickets = ko.observable(0);
    self.activeDeals = ko.observable(0);
    self.todayActivities = ko.observable(0);
    self.pipelineValue = ko.observable('0 TL');
    self.recentActivities = ko.observableArray([]);
    self.upcomingTasks = ko.observableArray([]);

    self.loadData = function() {
        $.ajax({
            url: apiBaseUrl + '/api/crm/dashboard',
            method: 'GET'
        }).done(function(data) {
            self.totalContacts(data.totalContacts);
            self.openTickets(data.openTickets);
            self.activeDeals(data.activeDeals);
            self.todayActivities(data.todayActivities);
            self.pipelineValue(data.pipelineValue.toLocaleString('tr-TR') + ' TL');
            self.recentActivities(data.recentActivities.map(function(a) {
                a.timeAgo = self.formatTimeAgo(a.createdAt);
                return a;
            }));
            self.upcomingTasks(data.upcomingTasks.map(function(t) {
                t.dueDateFormatted = t.dueDate ? new Date(t.dueDate).toLocaleDateString('tr-TR') : '-';
                return t;
            }));
        }).fail(function() {
            toastr.error('Dashboard verileri yuklenemedi');
        });
    };

    self.formatTimeAgo = function(dateStr) {
        var diff = (new Date() - new Date(dateStr)) / 1000;
        if (diff < 60) return 'Az once';
        if (diff < 3600) return Math.floor(diff / 60) + ' dk once';
        if (diff < 86400) return Math.floor(diff / 3600) + ' saat once';
        return Math.floor(diff / 86400) + ' gun once';
    };

    $(document).ready(function() {
        self.loadData();
    });
}

ko.applyBindings(new DashboardViewModel(), document.getElementById('dashboard-vm'));
