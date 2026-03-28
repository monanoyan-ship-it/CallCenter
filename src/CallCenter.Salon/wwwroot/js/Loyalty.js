function LoyaltyViewModel() {
    var self = this;
    self.clientLoyalties = ko.observableArray([]);
    self.isSaving = ko.observable(false);

    self.config = {
        pointsPerTL: ko.observable(1),
        pointValue: ko.observable(0.1),
        minRedeemPoints: ko.observable(100),
        isActive: ko.observable('true')
    };

    self.loadConfig = function () {
        $.ajax({ url: '/proxy/sln-loyalty/config', method: 'GET' }).done(function (data) {
            self.config.pointsPerTL(data.pointsPerTL || 1);
            self.config.pointValue(data.pointValue || 0.1);
            self.config.minRedeemPoints(data.minRedeemPoints || 100);
            self.config.isActive(data.isActive ? 'true' : 'false');
        });
    };

    self.saveConfig = function () {
        var data = {
            pointsPerTL: parseFloat(self.config.pointsPerTL()) || 1,
            pointValue: parseFloat(self.config.pointValue()) || 0.1,
            minRedeemPoints: parseInt(self.config.minRedeemPoints()) || 100,
            isActive: self.config.isActive() === 'true'
        };

        self.isSaving(true);
        $.ajax({
            url: '/proxy/sln-loyalty/config', method: 'POST',
            contentType: 'application/json', data: JSON.stringify(data)
        }).done(function () {
            toastr.success('Sadakat ayarlari kaydedildi');
            self.isSaving(false);
        }).fail(function () {
            toastr.error('Kaydedilemedi');
            self.isSaving(false);
        });
    };

    self.loadClients = function () {
        $.ajax({ url: '/proxy/sln-loyalty/clients', method: 'GET' }).done(function (data) {
            self.clientLoyalties(data.items || data);
        });
    };

    $(document).ready(function () {
        self.loadConfig();
        self.loadClients();
    });
}

ko.applyBindings(new LoyaltyViewModel(), document.getElementById('loyalty-vm'));
