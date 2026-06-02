function SalonLoyaltyProgramViewModel() {
    var self = this;
    self.programs = ko.observableArray([]);
    self.progresses = ko.observableArray([]);
    self.services = ko.observableArray([]);
    self.editingId = ko.observable(null);
    self.form = {
        name: ko.observable(''),
        serviceId: ko.observable(null),
        rewardServiceId: ko.observable(null),
        requiredVisits: ko.observable(10),
        isActive: ko.observable(true)
    };

    function normalizeList(data) {
        if (Array.isArray(data)) return data;
        if (data && Array.isArray(data.items)) return data.items;
        return [];
    }

    function t(key, fallback) {
        return (window.__crmT && window.__crmT[key]) || fallback;
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

    self.loadServices = function () {
        $.ajax({ url: '/proxy/crm/salon/services-lookup', method: 'GET' })
            .done(function (data) { self.services(normalizeList(data)); })
            .fail(function (xhr) { toastr.error(t('crm.salon.loyalty.program.services_load_failed', 'Hizmetler yüklenemedi')); });
    };

    function resetForm() {
        self.form.name('');
        self.form.serviceId(null);
        self.form.rewardServiceId(null);
        self.form.requiredVisits(10);
        self.form.isActive(true);
    }

    self.openCreate = function () {
        self.editingId(null);
        resetForm();
        var modal = bootstrap.Modal.getOrCreateInstance(document.getElementById('loyaltyProgramModal'));
        modal.show();
    };

    self.openEdit = function (program) {
        self.editingId(program.id);
        self.form.name(program.name || '');
        self.form.serviceId(program.serviceId || null);
        self.form.rewardServiceId(program.rewardServiceId || null);
        self.form.requiredVisits(program.requiredVisits || 10);
        self.form.isActive(!!program.isActive);
        var modal = bootstrap.Modal.getOrCreateInstance(document.getElementById('loyaltyProgramModal'));
        modal.show();
    };

    self.saveProgram = function () {
        var payload = {
            name: (self.form.name() || '').trim(),
            serviceId: parseInt(self.form.serviceId(), 10) || 0,
            rewardServiceId: parseInt(self.form.rewardServiceId(), 10) || 0,
            requiredVisits: parseInt(self.form.requiredVisits(), 10) || 0,
            isActive: !!self.form.isActive()
        };
        if (!payload.name) { toastr.warning(t('crm.salon.loyalty.program.validation.name', 'Ad zorunlu')); return; }
        if (!payload.serviceId) { toastr.warning(t('crm.salon.loyalty.program.validation.service', 'Takip edilecek hizmeti seçin')); return; }
        if (!payload.rewardServiceId) { toastr.warning(t('crm.salon.loyalty.program.validation.reward', 'Ödül hizmeti zorunlu')); return; }
        if (payload.requiredVisits < 1) { toastr.warning(t('crm.salon.loyalty.program.validation.required', 'Ziyaret sayısı en az 1 olmalı')); return; }

        var id = self.editingId();
        var url = id
            ? '/proxy/crm/salon/loyalty-programs/programs/' + id
            : '/proxy/crm/salon/loyalty-programs/programs';
        var method = id ? 'PUT' : 'POST';

        $.ajax({ url: url, method: method, contentType: 'application/json', data: JSON.stringify(payload) })
            .done(function () {
                toastr.success(t('crm.salon.loyalty.program.saved', 'Program kaydedildi'));
                var modal = bootstrap.Modal.getInstance(document.getElementById('loyaltyProgramModal'));
                if (modal) modal.hide();
                self.loadAll();
            })
            .fail(function (xhr) {
                toastr.error((xhr.responseJSON && xhr.responseJSON.error) || t('crm.salon.loyalty.program.save_failed', 'Program kaydedilemedi'));
            });
    };

    self.removeProgram = function (program) {
        if (typeof confirmModal === 'function') {
            confirmModal(t('crm.salon.loyalty.program.confirm_delete', 'Bu program silinsin mi?'), function () {
                doDelete(program.id);
            });
        } else if (window.confirm(t('crm.salon.loyalty.program.confirm_delete', 'Bu program silinsin mi?'))) {
            doDelete(program.id);
        }
    };

    function doDelete(id) {
        $.ajax({ url: '/proxy/crm/salon/loyalty-programs/programs/' + id, method: 'DELETE' })
            .done(function () {
                toastr.success(t('crm.salon.loyalty.program.deleted', 'Program silindi'));
                self.loadAll();
            })
            .fail(function (xhr) {
                toastr.error((xhr.responseJSON && xhr.responseJSON.error) || t('crm.salon.loyalty.program.delete_failed', 'Program silinemedi'));
            });
    }

    $(document).ready(function () {
        self.loadAll();
        self.loadServices();
    });
}

ko.applyBindings(new SalonLoyaltyProgramViewModel(), document.getElementById('salon-loyalty-program-vm'));
