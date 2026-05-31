function SalonLoyaltyProgramViewModel() {
    var self = this;
    self.programs = ko.observableArray([]);
    self.progresses = ko.observableArray([]);

    function normalizeList(data) {
        if (Array.isArray(data)) return data;
        if (data && Array.isArray(data.items)) return data.items;
        return [];
    }

    self.totalEarned = ko.computed(function () {
        return self.progresses().reduce(function (s, p) { return s + (p.rewardsEarned || 0); }, 0);
    });

    self.totalAvailable = ko.computed(function () {
        return self.progresses().reduce(function (s, p) { return s + (p.availableRewards || 0); }, 0);
    });

    self.loadAll = function () {
        $.ajax({ url: '/proxy/crm/salon/loyalty-programs/programs', method: 'GET' })
            .done(function (data) { self.programs(normalizeList(data)); });
        $.ajax({ url: '/proxy/crm/salon/loyalty-programs/client-progress', method: 'GET' })
            .done(function (data) { self.progresses(normalizeList(data)); });
    };

    $(document).ready(function () {
        self.loadAll();
    });
}

ko.applyBindings(new SalonLoyaltyProgramViewModel(), document.getElementById('salon-loyalty-program-vm'));
