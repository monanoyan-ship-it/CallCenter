function PricingViewModel() {
    var self = this;
    self.modules = ko.observableArray([]);
    self.isSaving = ko.observable(false);
    self.dirtyIds = ko.observableArray([]);

    self.defaultModules = ko.computed(function () {
        return self.modules().filter(function (m) { return m.isDefault; });
    });

    self.optionalModules = ko.computed(function () {
        return self.modules().filter(function (m) { return !m.isDefault; });
    });

    self.groupedModules = ko.computed(function () {
        var optional = self.optionalModules();
        var grouped = {};
        optional.forEach(function (m) {
            var gId = m.groupId || 0;
            var gName = m.groupName || 'Diger';
            if (!grouped[gId]) grouped[gId] = { groupId: gId, groupName: gName, modules: [] };
            grouped[gId].modules.push(m);
        });
        return Object.values(grouped).sort(function (a, b) { return a.groupId - b.groupId; });
    });

    self.markDirty = function (moduleId) {
        if (self.dirtyIds.indexOf(moduleId) < 0) self.dirtyIds.push(moduleId);
    };

    self.load = function () {
        $.get('/proxy/management/module-pricing', function (data) {
            var list = (Array.isArray(data) ? data : []).map(function (m) {
                m.price = ko.observable(m.monthlyPrice || 0);
                return m;
            });
            self.modules(list);
            self.dirtyIds([]);
        });
    };

    self.saveAll = function () {
        var prices = self.optionalModules().map(function (m) {
            return { moduleId: m.moduleId, monthlyPrice: parseFloat(m.price()) || 0 };
        });
        self.isSaving(true);
        $.ajax({
            url: '/proxy/management/module-pricing/bulk',
            method: 'POST',
            contentType: 'application/json',
            data: JSON.stringify({ prices: prices }),
            success: function (resp) {
                toastr.success((resp.count || prices.length) + ' hizmet fiyati kaydedildi.');
                self.dirtyIds([]);
                self.load();
            },
            error: function () { toastr.error('Kaydetme hatasi.'); },
            complete: function () { self.isSaving(false); }
        });
    };

    self.load();
}
ko.applyBindings(new PricingViewModel(), document.getElementById('pricing-vm'));
