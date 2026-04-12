function LicensingViewModel() {
    var self = this;
    self.licenses = ko.observableArray([]);
    self.customers = ko.observableArray([]);
    self.totalLicenses = ko.observable(0);
    self.activeLicenses = ko.observable(0);
    self.expiringLicenses = ko.observable(0);

    self.form = {
        customerId: ko.observable(null),
        packageName: ko.observable('Starter'),
        maxUsers: ko.observable(5),
        endDate: ko.observable('')
    };

    self.openCreate = function () {
        $.get('/proxy/customers', function (d) {
            self.customers((d.items || d || []).filter(function(c) { return c.isActive; }));
        });
        new bootstrap.Modal('#licenseModal').show();
    };

    self.saveLicense = function () {
        toastr.info('Lisanslama sistemi henuz aktif degil. Ileri surumde eklenecek.');
        bootstrap.Modal.getInstance(document.getElementById('licenseModal')).hide();
    };
}
ko.applyBindings(new LicensingViewModel(), document.getElementById('licensing-vm'));
