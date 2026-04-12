function RolesViewModel() {
    var self = this;
    self.isLoading = ko.observable(true);
    self.isSaving = ko.observable(false);
    self.hasData = ko.observable(false);
    self.roles = ko.observableArray([]);
    self.pages = ko.observableArray([]);
    self.matrix = {}; // { "roleId:pageName": ko.observable(bool) }

    self.isAllowed = function (roleId, pageName) {
        var key = roleId + ':' + pageName;
        if (!self.matrix[key]) self.matrix[key] = ko.observable(false);
        return self.matrix[key];
    };

    self.toggle = function (roleId, pageName) {
        var key = roleId + ':' + pageName;
        if (!self.matrix[key]) self.matrix[key] = ko.observable(false);
        // checkbox binding handles the toggle
        return true;
    };

    self.load = function () {
        self.isLoading(true);
        $.get('/proxy/management/salon-role-matrix', function (data) {
            if (!Array.isArray(data) || data.length === 0) {
                self.hasData(false);
                self.isLoading(false);
                return;
            }

            self.hasData(true);

            // Roller
            self.roles(data.map(function (r) {
                return { id: r.roleId, name: r.roleName, icon: r.roleIcon || 'bi-person' };
            }));

            // Sayfalar (ilk rolden al)
            var pageNames = data[0].pages.map(function (p) { return { name: p.pageName, moduleId: p.moduleId }; });
            self.pages(pageNames);

            // Matris
            self.matrix = {};
            data.forEach(function (role) {
                role.pages.forEach(function (p) {
                    var key = role.roleId + ':' + p.pageName;
                    self.matrix[key] = ko.observable(p.isAllowed);
                });
            });

            self.isLoading(false);
        }).fail(function () {
            self.hasData(false);
            self.isLoading(false);
        });
    };

    self.seedFromStatic = function () {
        $.ajax({
            url: '/proxy/management/salon-role-matrix/seed',
            method: 'POST',
            success: function (resp) {
                toastr.success((resp.seeded || 0) + ' izin kaydi olusturuldu.');
                self.load();
            },
            error: function () { toastr.error('Seed hatasi.'); }
        });
    };

    self.save = function () {
        var items = [];
        self.roles().forEach(function (role) {
            self.pages().forEach(function (page) {
                var key = role.id + ':' + page.name;
                var allowed = self.matrix[key] ? self.matrix[key]() : false;
                items.push({ roleId: role.id, pageName: page.name, isAllowed: allowed });
            });
        });

        self.isSaving(true);
        $.ajax({
            url: '/proxy/management/salon-role-matrix',
            method: 'POST',
            contentType: 'application/json',
            data: JSON.stringify(items),
            success: function (resp) {
                toastr.success((resp.saved || items.length) + ' izin kaydedildi.');
            },
            error: function () { toastr.error('Kaydetme hatasi.'); },
            complete: function () { self.isSaving(false); }
        });
    };

    self.load();
}
ko.applyBindings(new RolesViewModel(), document.getElementById('roles-vm'));
