function SalonLoyaltyViewModel() {
    var self = this;
    var t = window.crmT || function (key, fallback) { return fallback || key; };
    self.clientLoyalties = ko.observableArray([]);
    self.isSaving = ko.observable(false);

    self.config = {
        pointsPerTL: ko.observable(1),
        pointValue: ko.observable(0.1),
        minRedeemPoints: ko.observable(100),
        isActive: ko.observable(true)
    };

    self.money = function (value) {
        return (parseFloat(value) || 0).toLocaleString('tr-TR', { style: 'currency', currency: 'TRY' });
    };

    function readError(xhr, fallback) {
        return xhr.responseJSON?.error || xhr.responseJSON?.message || xhr.responseText || fallback;
    }

    self.loadConfig = function () {
        $.get('/proxy/crm/salon/loyalty/config')
            .done(function (data) {
                self.config.pointsPerTL(data.pointsPerTL || 1);
                self.config.pointValue(data.pointValue || 0.1);
                self.config.minRedeemPoints(data.minRedeemPoints || 100);
                self.config.isActive(data.isActive !== false);
            })
            .fail(function (xhr) {
                toastr.error(readError(xhr, t('crm.salon.loyalty.config_load_failed', 'Sadakat ayarlari yuklenemedi')));
            });
    };

    self.saveConfig = function () {
        var payload = {
            pointsPerTL: parseFloat(self.config.pointsPerTL()) || 1,
            pointValue: parseFloat(self.config.pointValue()) || 0.1,
            minRedeemPoints: parseInt(self.config.minRedeemPoints(), 10) || 100,
            isActive: self.config.isActive() === true
        };

        self.isSaving(true);
        $.ajax({
            url: '/proxy/crm/salon/loyalty/config',
            method: 'POST',
            contentType: 'application/json',
            data: JSON.stringify(payload)
        }).done(function () {
            toastr.success(t('crm.salon.loyalty.config_saved', 'Sadakat ayarlari kaydedildi'));
        }).fail(function (xhr) {
            toastr.error(readError(xhr, t('crm.salon.loyalty.config_save_failed', 'Sadakat ayarlari kaydedilemedi')));
        }).always(function () {
            self.isSaving(false);
        });
    };

    self.loadClients = function () {
        $.get('/proxy/crm/salon/loyalty/clients')
            .done(function (data) {
                self.clientLoyalties(data.items || data || []);
            })
            .fail(function (xhr) {
                toastr.error(readError(xhr, t('crm.salon.loyalty.clients_load_failed', 'Musteri puanlari yuklenemedi')));
            });
    };

    $(document).ready(function () {
        self.loadConfig();
        self.loadClients();
    });
}

ko.applyBindings(new SalonLoyaltyViewModel(), document.getElementById('salon-loyalty-vm'));
