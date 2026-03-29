function NoShowPolicyViewModel() {
    var self = this;
    self.isSaving = ko.observable(false);

    self.config = {
        requireDeposit: ko.observable(false),
        depositAmount: ko.observable(0),
        freeCancellationHours: ko.observable(24),
        lateCancellationFee: ko.observable(0),
        noShowFee: ko.observable(0),
        blacklistThreshold: ko.observable(3),
        isActive: ko.observable('true')
    };

    self.loadConfig = function () {
        $.ajax({ url: '/proxy/sln-noshow-policy', method: 'GET' }).done(function (data) {
            self.config.requireDeposit(data.requireDeposit || false);
            self.config.depositAmount(data.depositAmount || 0);
            self.config.freeCancellationHours(data.freeCancellationHours || 24);
            self.config.lateCancellationFee(data.lateCancellationFee || 0);
            self.config.noShowFee(data.noShowFee || 0);
            self.config.blacklistThreshold(data.blacklistThreshold || 3);
            self.config.isActive(data.isActive ? 'true' : 'false');
        });
    };

    self.saveConfig = function () {
        var data = {
            requireDeposit: self.config.requireDeposit(),
            depositAmount: parseFloat(self.config.depositAmount()) || 0,
            freeCancellationHours: parseInt(self.config.freeCancellationHours()) || 24,
            lateCancellationFee: parseFloat(self.config.lateCancellationFee()) || 0,
            noShowFee: parseFloat(self.config.noShowFee()) || 0,
            blacklistThreshold: parseInt(self.config.blacklistThreshold()) || 3,
            isActive: self.config.isActive() === 'true'
        };

        self.isSaving(true);
        $.ajax({
            url: '/proxy/sln-noshow-policy', method: 'POST',
            contentType: 'application/json', data: JSON.stringify(data)
        }).done(function () {
            toastr.success('No-Show politikasi kaydedildi');
            self.isSaving(false);
        }).fail(function () {
            toastr.error('Kaydedilemedi');
            self.isSaving(false);
        });
    };

    $(document).ready(function () {
        self.loadConfig();
    });
}

ko.applyBindings(new NoShowPolicyViewModel(), document.getElementById('noshow-vm'));
