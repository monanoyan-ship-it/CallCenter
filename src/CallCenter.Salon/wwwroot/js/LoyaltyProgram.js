function LoyaltyProgramViewModel() {
    var self = this;
    self.programs = ko.observableArray([]);
    self.progresses = ko.observableArray([]);
    self.services = ko.observableArray([]);
    self.isEditing = ko.observable(false);
    self.editingId = ko.observable(null);
    self.isSaving = ko.observable(false);

    self.form = {
        name: ko.observable(''),
        serviceId: ko.observable(null),
        rewardServiceId: ko.observable(null),
        requiredVisits: ko.observable(10),
        rewardValidDays: ko.observable(null),
        maxRewardsPerClient: ko.observable(null),
        isActive: ko.observable(true)
    };

    var modal;

    function normalizeList(data) {
        if (Array.isArray(data)) return data;
        if (data && Array.isArray(data.items)) return data.items;
        return [];
    }

    function flattenServiceCategories(data) {
        var list = normalizeList(data);
        var flat = [];
        list.forEach(function (item) {
            if (Array.isArray(item.services)) {
                item.services.forEach(function (svc) { flat.push(svc); });
            } else {
                flat.push(item);
            }
        });
        var seen = {};
        return flat.filter(function (s) {
            var id = parseInt(s.id, 10);
            if (!id || seen[id]) return false;
            seen[id] = true;
            return s.isActive !== false;
        }).sort(function (a, b) { return (a.name || '').localeCompare(b.name || ''); });
    }

    self.formatDateTime = function (value) {
        return value ? new Date(value).toLocaleString(document.documentElement.lang || undefined) : '—';
    };

    self.progressPct = function (p) {
        if (!p || !p.requiredVisits) return 0;
        var current = (p.visitCount || 0) % p.requiredVisits;
        return Math.round((current / p.requiredVisits) * 100);
    };

    self.totalEarned = ko.computed(function () {
        return self.progresses().reduce(function (s, p) { return s + (p.rewardsEarned || 0); }, 0);
    });

    self.totalAvailable = ko.computed(function () {
        return self.progresses().reduce(function (s, p) { return s + (p.availableRewards || 0); }, 0);
    });

    self.loadPrograms = function () {
        $.ajax({ url: '/proxy/sln-loyalty-programs/programs', method: 'GET' }).done(function (data) {
            self.programs(normalizeList(data));
        });
    };

    self.loadProgresses = function () {
        $.ajax({ url: '/proxy/sln-loyalty-programs/client-progress', method: 'GET' }).done(function (data) {
            self.progresses(normalizeList(data));
        });
    };

    self.loadServices = function () {
        $.ajax({ url: '/proxy/sln-services', method: 'GET' })
            .done(function (data) {
                var flat = flattenServiceCategories(data);
                if (flat.length === 0) {
                    $.ajax({ url: '/proxy/sln-services/categories', method: 'GET' }).done(function (cat) {
                        self.services(flattenServiceCategories(cat));
                    });
                    return;
                }
                self.services(flat);
            })
            .fail(function () {
                $.ajax({ url: '/proxy/sln-services/categories', method: 'GET' }).done(function (cat) {
                    self.services(flattenServiceCategories(cat));
                });
            });
    };

    self.openNewProgram = function () {
        self.isEditing(false);
        self.editingId(null);
        self.form.name('');
        self.form.serviceId(null);
        self.form.rewardServiceId(null);
        self.form.requiredVisits(10);
        self.form.rewardValidDays(null);
        self.form.maxRewardsPerClient(null);
        self.form.isActive(true);
        modal.show();
    };

    self.openEditProgram = function (program) {
        self.isEditing(true);
        self.editingId(program.id);
        self.form.name(program.name || '');
        self.form.serviceId(program.serviceId);
        self.form.rewardServiceId(program.rewardServiceId);
        self.form.requiredVisits(program.requiredVisits);
        self.form.rewardValidDays(program.rewardValidDays);
        self.form.maxRewardsPerClient(program.maxRewardsPerClient);
        self.form.isActive(program.isActive !== false);
        modal.show();
    };

    self.saveProgram = function () {
        var name = (self.form.name() || '').trim();
        var serviceId = parseInt(self.form.serviceId()) || 0;
        var rewardServiceId = parseInt(self.form.rewardServiceId()) || 0;
        var required = parseInt(self.form.requiredVisits()) || 0;
        if (!name || !serviceId || !rewardServiceId || required <= 0) {
            toastr.warning(slnJsT('salon.loyalty.program.validation', 'Ad, hizmet, odul hizmeti ve esik zorunlu.'));
            return;
        }

        var data = {
            name: name,
            serviceId: serviceId,
            rewardServiceId: rewardServiceId,
            requiredVisits: required,
            rewardValidDays: self.form.rewardValidDays() ? parseInt(self.form.rewardValidDays()) : null,
            maxRewardsPerClient: self.form.maxRewardsPerClient() ? parseInt(self.form.maxRewardsPerClient()) : null,
            isActive: !!self.form.isActive()
        };

        self.isSaving(true);
        var url = '/proxy/sln-loyalty-programs/programs';
        var method = 'POST';
        if (self.isEditing()) {
            url += '/' + self.editingId();
            method = 'PUT';
        }

        $.ajax({ url: url, method: method, contentType: 'application/json', data: JSON.stringify(data) })
            .done(function () {
                modal.hide();
                toastr.success(slnJsT('salon.loyalty.program.saved', 'Program kaydedildi'));
                self.loadPrograms();
            })
            .fail(function (xhr) {
                var msg = (xhr && xhr.responseJSON && xhr.responseJSON.error) || xhr.responseText || slnJsT('salon.common.error.save_failed', 'Kaydedilemedi');
                toastr.error(msg);
            })
            .always(function () { self.isSaving(false); });
    };

    self.removeProgram = function (program) {
        confirmModal(slnJsT('salon.common.btn.confirm', 'Onayla'),
            slnJsT('salon.loyalty.program.delete_confirm', "'{name}' programini silmek istediginize emin misiniz?").replace('{name}', program.name || ''),
            function () {
                $.ajax({ url: '/proxy/sln-loyalty-programs/programs/' + program.id, method: 'DELETE' })
                    .done(function () {
                        toastr.success(slnJsT('salon.loyalty.program.deleted', 'Program silindi'));
                        self.loadPrograms();
                    })
                    .fail(function (xhr) {
                        var msg = (xhr && xhr.responseJSON && xhr.responseJSON.error) || xhr.responseText || slnJsT('salon.common.error.delete_failed', 'Silinemedi');
                        toastr.error(msg);
                    });
            });
    };

    $(document).ready(function () {
        modal = new bootstrap.Modal(document.getElementById('loyaltyProgramModal'));
        self.loadServices();
        self.loadPrograms();
        self.loadProgresses();
    });
}

ko.applyBindings(new LoyaltyProgramViewModel(), document.getElementById('loyalty-program-vm'));
