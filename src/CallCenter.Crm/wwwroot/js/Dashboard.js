function DashboardViewModel() {
    var self = this;
    self.totalContacts = ko.observable(0);
    self.openTickets = ko.observable(0);
    self.activeDeals = ko.observable(0);
    self.todayActivities = ko.observable(0);
    self.pipelineValue = ko.observable('0 TL');
    self.recentActivities = ko.observableArray([]);
    self.upcomingTasks = ko.observableArray([]);
    self.pendingCallbacks = ko.observableArray([]);
    self.personnel = ko.observableArray([]);
    self.isSaving = ko.observable(false);
    self.assignForm = {
        callId: ko.observable(null),
        assignedToId: ko.observable(null),
        note: ko.observable("")
    };

    self.loadData = function() {
        $.ajax({
            url: '/proxy/crm/dashboard',
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
            toastr.error('Ana sayfa verileri yüklenemedi');
        });

        // Cevapsızları yükle
        self.loadCallbacks();
        self.loadPersonnel();
    };

    self.loadCallbacks = function() {
        $.get("/Calls/GetHistory?pageSize=10", function (data) {
            var calls = Array.isArray(data) ? data : (data.items || []);
            var missed = calls.filter(function(c) {
                return c.statusId === 6 && !c.callbackStatusId; 
            });
            self.pendingCallbacks(missed);
        });
    };

    self.loadPersonnel = function() {
        $.get("/Calls/GetPersonnel", function(data) {
            self.personnel(data);
        });
    };

    self.openAssignModal = function(item) {
        self.assignForm.callId(item.id);
        self.assignForm.assignedToId(null);
        self.assignForm.note("");
        $("#assignModal").modal("show");
    };

    self.saveAssignment = function() {
        self.isSaving(true);
        var id = self.assignForm.callId();
        $.ajax({
            url: "/Calls/AssignCallback?id=" + id + "&assignedToId=" + self.assignForm.assignedToId(),
            type: "POST",
            contentType: "application/json",
            data: JSON.stringify(self.assignForm.note()),
            success: function() {
                toastr.success("Atama yapıldı");
                $("#assignModal").modal("hide");
                self.loadCallbacks();
            },
            error: function() { toastr.error("Bir hata oluştu"); },
            complete: function() { self.isSaving(false); }
        });
    };

    self.formatTimeAgo = function(dateStr) {
        var diff = (new Date() - new Date(dateStr)) / 1000;
        if (diff < 60) return 'Az önce';
        if (diff < 3600) return Math.floor(diff / 60) + ' dk önce';
        if (diff < 86400) return Math.floor(diff / 3600) + ' saat önce';
        return Math.floor(diff / 86400) + ' gün önce';
    };

    $(document).ready(function() {
        self.loadData();
    });
}

ko.applyBindings(new DashboardViewModel(), document.getElementById('dashboard-vm'));
