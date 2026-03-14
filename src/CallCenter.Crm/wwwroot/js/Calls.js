function CallsViewModel() {
    var self = this;

    self.calls = ko.observableArray([]);
    self.personnel = ko.observableArray([]);
    self.searchQuery = ko.observable("");
    self.statusFilter = ko.observable("");
    self.isLoading = ko.observable(false);
    self.isSaving = ko.observable(false);

    self.assignForm = {
        callId: ko.observable(null),
        assignedToId: ko.observable(null),
        note: ko.observable("")
    };

    self.load = function () {
        self.isLoading(true);
        $.get("/Calls/GetHistory", function (data) {
            self.calls(data.map(function(c) {
                return new CallItem(c);
            }));
        }).always(function() {
            self.isLoading(false);
        });
    };

    self.loadPersonnel = function() {
        $.get("/Calls/GetPersonnel", function(data) {
            self.personnel(data);
        });
    };

    self.filteredCalls = ko.computed(function () {
        var q = self.searchQuery().toLowerCase();
        var s = self.statusFilter();
        
        return self.calls().filter(function (c) {
            var matchSearch = !q || c.callerNumber.toLowerCase().indexOf(q) > -1 || c.calleeNumber.toLowerCase().indexOf(q) > -1;
            var matchStatus = !s || c.statusId == s;
            return matchSearch && matchStatus;
        });
    });

    self.openAssignModal = function(item) {
        self.assignForm.callId(item.id);
        self.assignForm.assignedToId(null);
        self.assignForm.note("");
        $("#assignModal").modal("show");
    };

    self.saveAssignment = function() {
        self.isSaving(true);
        var id = self.assignForm.callId();
        var data = {
            assignedToId: self.assignForm.assignedToId(),
            note: self.assignForm.note()
        };

        $.ajax({
            url: "/Calls/AssignCallback?id=" + id + "&assignedToId=" + data.assignedToId,
            type: "POST",
            contentType: "application/json",
            data: JSON.stringify(data.note),
            success: function() {
                toastr.success("Gorev başarıyla atandı.");
                $("#assignModal").modal("hide");
                self.load();
            },
            error: function(xhr) {
                var err = xhr.responseJSON ? xhr.responseJSON.message : "Hata olustu";
                toastr.error(err);
            },
            complete: function() {
                self.isSaving(false);
            }
        });
    };

    // Initial Load
    self.load();
    self.loadPersonnel();
}

function CallItem(data) {
    var self = this;
    self.id = data.id;
    self.uid = data.uid;
    self.callerNumber = data.callerNumber;
    self.calleeNumber = data.calleeNumber;
    self.startedAt = data.startedAt;
    self.statusId = data.statusId;
    self.durationSeconds = data.durationSeconds || 0;
    self.agentName = data.agentName || "-";
    
    // Callback fields
    self.callbackStatusId = data.callbackStatusId;
    self.callbackNote = data.callbackNote;

    self.statusName = ko.computed(function() {
        if (self.statusId === 1) return "Çalıyor";
        if (self.statusId === 2) return "Kuyrukta";
        if (self.statusId === 3) return "Beklemede";
        if (self.statusId === 4) return "Bağlandı";
        if (self.statusId === 5) return "Tamamlandı";
        if (self.statusId === 6) return "Cevapsız";
        return "Bilinmiyor";
    });

    self.statusCss = ko.computed(function() {
        if (self.statusId === 6) return "bg-danger";
        if (self.statusId === 5) return "bg-success";
        if (self.statusId === 4) return "bg-info";
        return "bg-secondary";
    });

    self.callbackStatusName = ko.computed(function() {
        if (self.callbackStatusId === 1) return "Geri Arama: Bekliyor";
        if (self.callbackStatusId === 2) return "Geri Arama: Aranıyor";
        if (self.callbackStatusId === 3) return "Geri Arama: Tamamlandı";
        return "";
    });

    self.callbackStatusCss = ko.computed(function() {
        if (self.callbackStatusId === 1) return "bg-warning text-dark";
        if (self.callbackStatusId === 2) return "bg-primary";
        if (self.callbackStatusId === 3) return "bg-success";
        return "";
    });

    self.durationText = ko.computed(function() {
        if (self.durationSeconds === 0) return "-";
        var m = Math.floor(self.durationSeconds / 60);
        var s = self.durationSeconds % 60;
        return (m < 10 ? "0"+m : m) + ":" + (s < 10 ? "0"+s : s);
    });
}

$(function () {
    ko.applyBindings(new CallsViewModel(), document.getElementById("calls-vm"));
});
