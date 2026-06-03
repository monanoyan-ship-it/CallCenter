function SubMerchantsViewModel() {
    var self = this;
    self.items = ko.observableArray([]);
    self.isLoading = ko.observable(false);
    self.search = ko.observable('');
    self.statusFilter = ko.observable('');

    self.filtered = ko.pureComputed(function () {
        var q = (self.search() || '').trim().toLowerCase();
        if (!q) return self.items();
        return self.items().filter(function (x) {
            return (x.customerName || '').toLowerCase().indexOf(q) >= 0
                || (x.slug || '').toLowerCase().indexOf(q) >= 0
                || (x.iyzicoSubMerchantKey || '').toLowerCase().indexOf(q) >= 0
                || ((x.contactName || '') + ' ' + (x.contactSurname || '')).toLowerCase().indexOf(q) >= 0;
        });
    });

    self.countActive = ko.pureComputed(function () {
        return self.items().filter(function (x) { return x.onboardingStatusId === 2; }).length;
    });
    self.countPending = ko.pureComputed(function () {
        return self.items().filter(function (x) { return x.onboardingStatusId === 1; }).length;
    });
    self.countRejected = ko.pureComputed(function () {
        return self.items().filter(function (x) { return x.onboardingStatusId === 3; }).length;
    });
    self.countNotStarted = ko.pureComputed(function () {
        return self.items().filter(function (x) { return x.onboardingStatusId === 0; }).length;
    });

    self.statusBadge = function (statusId) {
        switch (statusId) {
            case 1: return 'bg-warning text-dark';
            case 2: return 'bg-success';
            case 3: return 'bg-danger';
            default: return 'bg-secondary';
        }
    };

    self.statusLabel = function (statusId, fallback) {
        switch (statusId) {
            case 1: return 'Beklemede';
            case 2: return 'Onaylandı';
            case 3: return 'Reddedildi';
            default: return fallback || 'Başlamadı';
        }
    };

    self.typeLabel = function (t) {
        if (!t) return '-';
        switch ((t || '').toUpperCase()) {
            case 'PERSONAL': return 'Şahıs';
            case 'PRIVATE_COMPANY': return 'Şirket';
            case 'LIMITED_OR_JOINT_STOCK_COMPANY': return 'Ltd/A.Ş.';
            default: return t;
        }
    };

    self.load = function () {
        self.isLoading(true);
        var url = '/proxy/management/sub-merchants';
        var qs = [];
        if (self.statusFilter() !== '') qs.push('statusId=' + encodeURIComponent(self.statusFilter()));
        if (qs.length) url += '?' + qs.join('&');
        $.ajax({ url: url, method: 'GET' })
            .done(function (data) { self.items(data || []); })
            .fail(function (xhr) {
                if (typeof toastr !== 'undefined') {
                    toastr.error('Liste yüklenemedi: ' + (xhr.responseJSON ? (xhr.responseJSON.message || xhr.statusText) : xhr.statusText));
                }
            })
            .always(function () { self.isLoading(false); });
    };

    self.load();
}

(function () {
    var root = document.getElementById('submerchants-vm');
    if (!root || typeof ko === 'undefined') return;
    ko.applyBindings(new SubMerchantsViewModel(), root);
})();
