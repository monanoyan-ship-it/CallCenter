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
        assignedToId: ko.observable(0), // 0 = Havuz
        note: ko.observable("")
    };

    self.load = function () {
        self.isLoading(true);
        $.get("/Calls/GetHistory", function (data) {
            // PagedResult veya Array kontrolu
            var list = Array.isArray(data) ? data : (data.items || []);
            self.calls(list.map(function(c) {
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

    // Atama listesinden sadece Operatör olanları filtrele
    self.filteredPersonnel = ko.computed(function() {
        return self.personnel().filter(function(p) {
            // CustomerRoleId 3 = Operatör
            return p.customerRoleId === 3;
        });
    });

    self.filteredCalls = ko.computed(function () {
        var q = self.searchQuery().toLowerCase();
        var s = self.statusFilter();
        
        return self.calls().filter(function (c) {
            var caller = c.callerNumber ? c.callerNumber.toLowerCase() : "";
            var callee = c.calleeNumber ? c.calleeNumber.toLowerCase() : "";
            var matchSearch = !q || caller.indexOf(q) > -1 || callee.indexOf(q) > -1;
            var matchStatus = !s || c.statusId == s;
            return matchSearch && matchStatus;
        });
    });

    self.openAssignModal = function(item) {
        self.assignForm.callId(item.id);
        self.assignForm.assignedToId(0);
        self.assignForm.note("");
        $("#assignModal").modal("show");
    };

    self.saveAssignment = function() {
        self.isSaving(true);
        var id = self.assignForm.callId();
        var targetId = parseInt(self.assignForm.assignedToId());

        $.ajax({
            url: "/Calls/AssignCallback?id=" + id + "&assignedToId=" + targetId,
            type: "POST",
            contentType: "application/json",
            data: JSON.stringify(self.assignForm.note()),
            success: function() {
                toastr.success(targetId === 0 ? "Havuza atandı" : "Temsilciye atandı");
                $("#assignModal").modal("hide");
                self.load();
            },
            error: function(xhr) {
                var err = xhr.responseJSON ? xhr.responseJSON.message : "Bir hata oluştu";
                toastr.error(err);
            },
            complete: function() {
                self.isSaving(false);
            }
        });
    };

    // ─── Numara Geçmişi ───
    self.historyNumber = ko.observable("");
    self.numberHistory = ko.observableArray([]);
    self.isHistoryLoading = ko.observable(false);

    self.showNumberHistory = function(number) {
        if (!number) return;
        self.historyNumber(number);
        self.numberHistory([]);
        self.isHistoryLoading(true);
        $("#numberHistoryModal").modal("show");

        $.get("/Calls/GetNumberHistory?number=" + encodeURIComponent(number), function(data) {
            var list = Array.isArray(data) ? data : (data.items || []);
            self.numberHistory(list.map(function(c) { return new CallItem(c); }));
        }).always(function() {
            self.isHistoryLoading(false);
        });
    };

    self.load();
    self.loadPersonnel();
}

function CallItem(data) {
    var self = this;
    self.id = data.id;
    self.callerNumber = data.callerNumber;
    self.calleeNumber = data.calleeNumber;
    self.startedAt = data.startedAt;
    self.statusId = data.statusId;
    self.durationSeconds = data.durationSeconds || 0;
    self.agentName = data.agentName || "-";
    
    self.callbackStatusId = data.callbackStatusId;
    self.callbackNote = data.callbackNote;

    self.statusName = ko.computed(function() {
        if (self.statusId === 1) return "Çalıyor";
        if (self.statusId === 5) return "Tamamlandı";
        if (self.statusId === 6) return "Cevapsız";
        return "Bitti";
    });

    self.statusCss = ko.computed(function() {
        if (self.statusId === 6) return "bg-danger";
        if (self.statusId === 5) return "bg-success";
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
