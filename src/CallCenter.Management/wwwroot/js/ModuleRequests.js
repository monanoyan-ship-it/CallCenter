function RequestsViewModel() {
    var self = this;
    self.pending = ko.observableArray([]);
    self.history = ko.observableArray([]);
    self.historyLoaded = ko.observable(false);

    self.pendingCount = ko.computed(function () { return self.pending().length; });
    self.approvedCount = ko.observable(0);
    self.rejectedCount = ko.observable(0);

    self.load = function () {
        $.get('/proxy/management/module-requests', function (data) {
            var list = Array.isArray(data) ? data : [];
            self.pending(list);
        });
    };

    self.loadHistory = function () {
        // Tum talepleri cek (tum statusler)
        $.get('/proxy/management/module-requests?all=true', function (data) {
            var list = Array.isArray(data) ? data : [];
            var nonPending = list.filter(function (r) { return r.statusId !== 1; });
            self.history(nonPending);
            self.historyLoaded(true);

            // Bu ayin istatistikleri
            var now = new Date();
            var thisMonth = nonPending.filter(function (r) {
                return r.reviewedAt && new Date(r.reviewedAt).getMonth() === now.getMonth()
                    && new Date(r.reviewedAt).getFullYear() === now.getFullYear();
            });
            self.approvedCount(thisMonth.filter(function (r) { return r.statusId === 2; }).length);
            self.rejectedCount(thisMonth.filter(function (r) { return r.statusId === 3; }).length);
        });
    };

    self.approve = function (req) {
        confirmModal('Talep Onayi', req.customerName + ' - ' + req.moduleName + ' talebini onaylamak istiyor musunuz?', function () {
            confirmModal('Admin Notu', 'Admin notu girin (opsiyonel):', function (notes) {
                $.ajax({
                    url: '/proxy/sln-module-requests/' + req.id + '/approve',
                    method: 'POST',
                    contentType: 'application/json',
                    data: JSON.stringify({ notes: notes || null }),
                    success: function () {
                        toastr.success('Talep onaylandi. Modul aktif edildi.');
                        self.load();
                    },
                    error: function (xhr) {
                        toastr.error(xhr.responseJSON?.message || 'Onay hatasi.');
                    }
                });
            }, { input: true, inputLabel: 'Admin notu (opsiyonel)', confirmText: 'Onayla', confirmClass: 'btn-success' });
        }, { confirmText: 'Devam Et', confirmClass: 'btn-success' });
    };

    self.reject = function (req) {
        confirmModal('Talep Reddi', 'Red sebebini girin:', function (notes) {
            if (notes === '' || notes === true) notes = null;
            $.ajax({
                url: '/proxy/sln-module-requests/' + req.id + '/reject',
                method: 'POST',
                contentType: 'application/json',
                data: JSON.stringify({ notes: notes }),
                success: function () {
                    toastr.success('Talep reddedildi.');
                    self.load();
                },
                error: function (xhr) {
                    toastr.error(xhr.responseJSON?.message || 'Red hatasi.');
                }
            });
        }, { input: true, inputLabel: 'Red sebebi', confirmText: 'Reddet', confirmClass: 'btn-danger' });
    };

    self.load();
}
ko.applyBindings(new RequestsViewModel(), document.getElementById('requests-vm'));
