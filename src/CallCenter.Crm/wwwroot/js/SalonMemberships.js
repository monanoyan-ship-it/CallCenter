function SalonMembershipsViewModel() {
    var self = this;
    var t = window.crmT || function (key, fallback) { return fallback || key; };
    self.plans = ko.observableArray([]);
    self.memberships = ko.observableArray([]);
    self.clients = ko.observableArray([]);
    self.services = ko.observableArray([]);
    self.branches = ko.observableArray([]);
    self.isSaving = ko.observable(false);
    self.isCreatingClient = ko.observable(false);
    self.newClientVisible = ko.observable(false);
    self.isEditingPlan = ko.observable(false);
    self.editingPlanId = ko.observable(null);
    self._serviceDetails = {};

    var allBranchesValue = '__all__';
    self.branchTargetOptions = ko.computed(function () {
        return [{ id: allBranchesValue, name: t('crm.salon.common.all_branches', 'Tüm Şubeler') }].concat(self.branches() || []);
    });

    self.planForm = {
        name: ko.observable(''),
        description: ko.observable(''),
        price: ko.observable(0),
        discountPercent: ko.observable(10),
        durationDays: ko.observable(30),
        priorityBooking: ko.observable(false),
        branchTarget: ko.observable(allBranchesValue),
        serviceIds: ko.observableArray([])
    };

    self.memberForm = {
        planId: ko.observable(null),
        slnClientId: ko.observable(null)
    };

    self.newClientForm = {
        fullName: ko.observable(''),
        phone: ko.observable('')
    };

    var planModal;
    var memberModal;

    self.money = function (value) {
        return (parseFloat(value) || 0).toLocaleString('tr-TR', { style: 'currency', currency: 'TRY' });
    };

    self.dateRange = function (start, end) {
        var startText = start ? new Date(start).toLocaleDateString('tr-TR') : '-';
        var endText = end ? new Date(end).toLocaleDateString('tr-TR') : t('crm.salon.memberships.no_end_date', 'Süresiz');
        return startText + ' - ' + endText;
    };

    self.statusText = function (statusId) {
        if (statusId === 1) return t('crm.common.active', 'Aktif');
        if (statusId === 2) return t('crm.salon.memberships.frozen', 'Donuk');
        if (statusId === 3) return t('crm.common.cancelled', 'İptal');
        return t('crm.common.status', 'Durum') + ' ' + statusId;
    };

    self.statusClass = function (statusId) {
        if (statusId === 1) return 'bg-success';
        if (statusId === 2) return 'bg-warning text-dark';
        if (statusId === 3) return 'bg-secondary';
        return 'bg-light text-dark';
    };

    function readError(xhr, fallback) {
        return xhr.responseJSON?.error || xhr.responseJSON?.message || xhr.responseText || fallback;
    }

    function resolveBranchTarget(value) {
        if (value === allBranchesValue) return { ok: true, allBranches: true, branchId: null };
        var branchId = parseInt(value, 10) || null;
        if (branchId) return { ok: true, allBranches: false, branchId: branchId };

        toastr.warning(t('crm.salon.common.branch_target_required', 'Şube seçin veya Tüm Şubeler seçeneğini seçin'));
        return { ok: false };
    }

    function appendBranchTarget(url, target) {
        var separator = url.indexOf('?') >= 0 ? '&' : '?';
        if (target.allBranches) return url + separator + 'allBranches=true';
        if (target.branchId) return url + separator + 'branchId=' + encodeURIComponent(target.branchId);
        return url;
    }

    self.serviceSelected = function (serviceId) {
        return (self.planForm.serviceIds() || []).indexOf(serviceId) >= 0;
    };

    self.serviceDetail = function (serviceId) {
        if (!self._serviceDetails[serviceId]) {
            self._serviceDetails[serviceId] = {
                serviceId: serviceId,
                freeCount: ko.observable(0),
                discountPercent: ko.observable(0)
            };
        }
        return self._serviceDetails[serviceId];
    };

    self.loadData = function () {
        $.get('/proxy/crm/salon/memberships/plans')
            .done(function (data) { self.plans(data.items || data || []); })
            .fail(function (xhr) { toastr.error(readError(xhr, t('crm.salon.memberships.plans_load_failed', 'Üyelik planları yüklenemedi'))); });

        $.get('/proxy/crm/salon/memberships')
            .done(function (data) { self.memberships(data.items || data || []); })
            .fail(function (xhr) { toastr.error(readError(xhr, t('crm.salon.memberships.load_failed', 'Müşteri üyelikleri yüklenemedi'))); });
    };

    self.loadClients = function () {
        $.get('/proxy/crm/salon/clients?pageSize=1000')
            .done(function (data) { self.clients(data.items || data || []); });
    };

    self.loadBranches = function () {
        $.get('/proxy/crm/salon/branches')
            .done(function (data) { self.branches(data.items || data || []); })
            .fail(function () { self.branches([]); });
    };

    self.loadServices = function () {
        $.get('/proxy/crm/salon/services-lookup')
            .done(function (data) { self.services(data.items || data || []); })
            .fail(function () { self.services([]); });
    };

    self.openNewPlan = function () {
        self.isEditingPlan(false);
        self.editingPlanId(null);
        self._serviceDetails = {};
        self.planForm.name('');
        self.planForm.description('');
        self.planForm.price(0);
        self.planForm.discountPercent(10);
        self.planForm.durationDays(30);
        self.planForm.priorityBooking(false);
        self.planForm.branchTarget(allBranchesValue);
        self.planForm.serviceIds([]);
        planModal.show();
    };

    self.openEditPlan = function (plan) {
        self.isEditingPlan(true);
        self.editingPlanId(plan.id);
        self.planForm.name(plan.name || '');
        self.planForm.description(plan.description || '');
        self.planForm.price(plan.price || 0);
        self.planForm.discountPercent(plan.discountPercent || 0);
        self.planForm.durationDays(plan.durationDays || 30);
        self.planForm.priorityBooking(plan.priorityBooking === true);
        self.planForm.branchTarget(plan.branchId ? String(plan.branchId) : allBranchesValue);
        self._serviceDetails = {};
        (plan.serviceDetails || []).forEach(function (detail) {
            self._serviceDetails[detail.serviceId] = {
                serviceId: detail.serviceId,
                freeCount: ko.observable(detail.freeCount || 0),
                discountPercent: ko.observable(detail.discountPercent || 0)
            };
        });
        self.planForm.serviceIds(plan.serviceIds || (plan.serviceDetails || []).map(function (detail) { return detail.serviceId; }));
        planModal.show();
    };

    self.savePlan = function () {
        var target = resolveBranchTarget(self.planForm.branchTarget());
        if (!target.ok) return;

        var payload = {
            name: self.planForm.name(),
            description: self.planForm.description(),
            iconClass: 'bi-award',
            color: '#0d6efd',
            durationType: 1,
            durationDays: parseInt(self.planForm.durationDays(), 10) || 30,
            price: parseFloat(self.planForm.price()) || 0,
            discountPercent: parseInt(self.planForm.discountPercent(), 10) || 0,
            priorityBooking: self.planForm.priorityBooking() === true,
            isActive: true,
            serviceIds: self.planForm.serviceIds() || [],
            serviceDetails: (self.planForm.serviceIds() || []).map(function (serviceId) {
                var detail = self.serviceDetail(serviceId);
                return {
                    serviceId: serviceId,
                    freeCount: parseInt(detail.freeCount(), 10) || 0,
                    discountPercent: parseInt(detail.discountPercent(), 10) || 0
                };
            })
        };

        if (!payload.name) {
            toastr.warning(t('crm.salon.memberships.plan_name_required_warning', 'Plan adı zorunludur'));
            return;
        }

        self.isSaving(true);
        var url = appendBranchTarget('/proxy/crm/salon/memberships/plans', target);
        var method = 'POST';
        if (self.isEditingPlan()) {
            url = appendBranchTarget('/proxy/crm/salon/memberships/plans/' + self.editingPlanId(), target);
            method = 'PUT';
        }

        $.ajax({
            url: url,
            method: method,
            contentType: 'application/json',
            data: JSON.stringify(payload)
        }).done(function () {
            planModal.hide();
            self.loadData();
            toastr.success(t('crm.salon.memberships.plan_saved', 'Plan kaydedildi'));
        }).fail(function (xhr) {
            toastr.error(readError(xhr, t('crm.salon.memberships.plan_save_failed', 'Plan kaydedilemedi')));
        }).always(function () {
            self.isSaving(false);
        });
    };

    self.removePlan = function (plan) {
        confirmModal(
            t('crm.salon.memberships.delete_plan_title', 'Planı Sil'),
            t('crm.salon.memberships.delete_plan_confirm', "'{name}' planını silmek istediğinize emin misiniz?").replace('{name}', plan.name || ''),
            function () {
            $.ajax({
                url: '/proxy/crm/salon/memberships/plans/' + plan.id,
                method: 'DELETE'
            }).done(function () {
                self.loadData();
                toastr.success(t('crm.salon.memberships.plan_deleted', 'Plan silindi'));
            }).fail(function (xhr) {
                toastr.error(readError(xhr, t('crm.salon.memberships.plan_delete_failed', 'Plan silinemedi')));
            });
        });
    };

    self.openNewMembership = function (plan) {
        self.memberForm.planId(plan && plan.id ? plan.id : null);
        self.memberForm.slnClientId(null);
        self.newClientVisible(false);
        self.newClientForm.fullName('');
        self.newClientForm.phone('');
        memberModal.show();
    };

    self.toggleNewClient = function () {
        self.newClientVisible(!self.newClientVisible());
    };

    self.saveNewClient = function () {
        var payload = {
            fullName: self.newClientForm.fullName(),
            phone: self.newClientForm.phone()
        };

        if (!payload.fullName || !payload.phone) {
            toastr.warning(t('crm.salon.memberships.name_phone_required', 'Ad ve telefon zorunludur'));
            return;
        }

        self.isCreatingClient(true);
        $.ajax({
            url: '/proxy/crm/salon/clients',
            method: 'POST',
            contentType: 'application/json',
            data: JSON.stringify(payload)
        }).done(function (client) {
            var list = self.clients().slice();
            list.push(client);
            self.clients(list);
            self.memberForm.slnClientId(client.id);
            self.newClientVisible(false);
            toastr.success(t('crm.salon.memberships.client_created', 'Müşteri oluşturuldu ve seçildi'));
        }).fail(function (xhr) {
            toastr.error(readError(xhr, t('crm.salon.memberships.client_create_failed', 'Müşteri oluşturulamadı')));
        }).always(function () {
            self.isCreatingClient(false);
        });
    };

    self.saveMembership = function () {
        var payload = {
            planId: parseInt(self.memberForm.planId(), 10) || 0,
            slnClientId: parseInt(self.memberForm.slnClientId(), 10) || 0
        };

        if (!payload.planId || !payload.slnClientId) {
            toastr.warning(t('crm.salon.memberships.plan_client_required', 'Plan ve müşteri zorunludur'));
            return;
        }

        self.isSaving(true);
        $.ajax({
            url: '/proxy/crm/salon/memberships',
            method: 'POST',
            contentType: 'application/json',
            data: JSON.stringify(payload)
        }).done(function () {
            memberModal.hide();
            self.loadData();
            toastr.success(t('crm.salon.memberships.created', 'Üyelik oluşturuldu'));
        }).fail(function (xhr) {
            toastr.error(readError(xhr, t('crm.salon.memberships.create_failed', 'Üyelik oluşturulamadı')));
        }).always(function () {
            self.isSaving(false);
        });
    };

    self.freezeMembership = function (membership) {
        $.ajax({ url: '/proxy/crm/salon/memberships/' + membership.id + '/freeze', method: 'PUT' })
            .done(function () { self.loadData(); toastr.info(t('crm.salon.memberships.frozen_success', 'Üyelik donduruldu')); })
            .fail(function (xhr) { toastr.error(readError(xhr, t('crm.salon.memberships.freeze_failed', 'Üyelik dondurulamadı'))); });
    };

    self.cancelMembership = function (membership) {
        confirmModal(t('crm.salon.memberships.cancel_title', 'Üyelik İptali'), t('crm.salon.memberships.cancel_confirm', 'Üyeliği iptal etmek istediğinize emin misiniz?'), function () {
            $.ajax({ url: '/proxy/crm/salon/memberships/' + membership.id + '/cancel', method: 'PUT' })
                .done(function () { self.loadData(); toastr.success(t('crm.salon.memberships.cancelled_success', 'Üyelik iptal edildi')); })
                .fail(function (xhr) { toastr.error(readError(xhr, t('crm.salon.memberships.cancel_failed', 'Üyelik iptal edilemedi'))); });
        });
    };

    self.reactivateMembership = function (membership) {
        $.ajax({ url: '/proxy/crm/salon/memberships/' + membership.id + '/reactivate', method: 'PUT' })
            .done(function () { self.loadData(); toastr.success(t('crm.salon.memberships.activated_success', 'Üyelik aktif edildi')); })
            .fail(function (xhr) { toastr.error(readError(xhr, t('crm.salon.memberships.activate_failed', 'Üyelik aktif edilemedi'))); });
    };

    $(document).ready(function () {
        planModal = new bootstrap.Modal(document.getElementById('membershipPlanModal'));
        memberModal = new bootstrap.Modal(document.getElementById('clientMembershipModal'));
        self.loadClients();
        self.loadBranches();
        self.loadServices();
        self.loadData();
    });
}

ko.applyBindings(new SalonMembershipsViewModel(), document.getElementById('salon-memberships-vm'));
