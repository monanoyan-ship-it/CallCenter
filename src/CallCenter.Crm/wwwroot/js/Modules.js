function CrmModulesViewModel() {
    var self = this;

    self.modules = ko.observableArray([]);
    self.isLoading = ko.observable(false);

    self.coreModules = ko.computed(function () {
        return self.modules().filter(function (m) { return m.groupName === 'CRM Çekirdek'; });
    });

    self.salonModules = ko.computed(function () {
        return self.modules().filter(function (m) { return m.groupName !== 'CRM Çekirdek'; });
    });

    self.load = function () {
        self.isLoading(true);
        $.ajax({
            url: '/proxy/crm/modules',
            method: 'GET'
        }).done(function (data) {
            self.modules(Array.isArray(data) ? data : []);
        }).fail(function () {
            toastr.error('CRM modül kataloğu yüklenemedi.');
        }).always(function () {
            self.isLoading(false);
        });
    };

    self.load();
}

ko.applyBindings(new CrmModulesViewModel(), document.getElementById('crm-modules-vm'));
