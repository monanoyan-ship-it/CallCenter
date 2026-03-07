function ReportsViewModel() {
    var self = this;
    self.dashboard = ko.observable({});
    self.ticketStats = ko.observableArray([]);
    self.dealStats = ko.observableArray([]);
    self.recentActivities = ko.observableArray([]);
    self.upcomingTasks = ko.observableArray([]);
    self.pipelineText = ko.observable('0 TL');

    var statusMap = {
        1: { name: 'Acik', css: 'bg-danger' },
        2: { name: 'Devam Ediyor', css: 'bg-info' },
        3: { name: 'Beklemede', css: 'bg-warning' },
        4: { name: 'Cozuldu', css: 'bg-success' },
        5: { name: 'Kapali', css: 'bg-secondary' }
    };

    var stageMap = {
        1: { name: 'Lead', css: 'bg-secondary' },
        2: { name: 'Nitelikli', css: 'bg-info' },
        3: { name: 'Teklif', css: 'bg-primary' },
        4: { name: 'Muzakere', css: 'bg-warning' },
        5: { name: 'Kazanildi', css: 'bg-success' },
        6: { name: 'Kaybedildi', css: 'bg-danger' }
    };

    self.loadDashboard = function() {
        $.ajax({
            url: '/proxy/crm/dashboard',
            method: 'GET'
        }).done(function(data) {
            self.dashboard(data);
            self.pipelineText((data.pipelineValue || 0).toLocaleString('tr-TR') + ' TL');
            self.recentActivities(data.recentActivities || []);
            self.upcomingTasks(data.upcomingTasks || []);
        }).fail(function() {
            toastr.error('Dashboard verisi yuklenemedi');
        });
    };

    self.loadTicketStats = function() {
        $.ajax({
            url: '/proxy/crm/tickets',
            method: 'GET'
        }).done(function(data) {
            var counts = {};
            data.forEach(function(t) {
                counts[t.statusId] = (counts[t.statusId] || 0) + 1;
            });
            var total = data.length || 1;
            var stats = [];
            for (var id in statusMap) {
                var c = counts[id] || 0;
                stats.push({
                    name: statusMap[id].name,
                    count: c,
                    pct: Math.round((c / total) * 100),
                    css: statusMap[id].css
                });
            }
            self.ticketStats(stats);
        });
    };

    self.loadDealStats = function() {
        $.ajax({
            url: '/proxy/crm/deals',
            method: 'GET'
        }).done(function(data) {
            var counts = {};
            data.forEach(function(d) {
                counts[d.stageId] = (counts[d.stageId] || 0) + 1;
            });
            var total = data.length || 1;
            var stats = [];
            for (var id in stageMap) {
                var c = counts[id] || 0;
                stats.push({
                    name: stageMap[id].name,
                    count: c,
                    pct: Math.round((c / total) * 100),
                    css: stageMap[id].css
                });
            }
            self.dealStats(stats);
        });
    };

    $(document).ready(function() {
        self.loadDashboard();
        self.loadTicketStats();
        self.loadDealStats();
    });
}

ko.applyBindings(new ReportsViewModel(), document.getElementById('reports-vm'));
