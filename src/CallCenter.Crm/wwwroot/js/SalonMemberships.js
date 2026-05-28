function SalonMembershipsViewModel() {
    var self = this;
    var t = window.crmT || function (key, fallback) { return fallback || key; };
    self.plans = ko.observableArray([]);
    self.memberships = ko.observableArray([]);
    self.clients = ko.observableArray([]);
    self.isSaving = ko.observable(false);
    self.isEditingPlan = ko.observable(false);
    self.editingPlanId = ko.observable(null);

    self.planForm = {
        name: ko.observable(''),
        description: ko.observable(''),
        price: ko.observable(0),
        discountPercent: ko.observable(10),
        durationDays: ko.observable(30),
        priorityBooking: ko.observable(false)
    };

    self.memberForm = {
        planId: ko.observable(null),
        slnClientId: ko.observable(null)
    };

    var planModal;
    var memberModal;

    self.money = function (value) {
        return (parseFloat(value) || 0).toLocaleString('tr-TR', { style: 'currency', currency: 'TRY' });
    };

    self.dateRange = function (start, end) {
        var startText = start ? new Date(start).toLocaleDateString('tr-TR') : '-';
        var endText = end ? new Date(end).toLocaleDateString('tr-TR') : t('crm.salon.memberships.no_end_date', 'Suresiz');
        return startText + ' - ' + endText;
    };

    self.statusText = function (statusId) {
        if (statusId === 1) return t('crm.common.active', 'Aktif');
        if (statusId === 2) return t('crm.salon.memberships.frozen', 'Donuk');
        if (statusId === 3) return t('crm.common.cancelled', 'Iptal');
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

    self.loadData = function () {
        $.get('/proxy/crm/salon/memberships/plans')
            .done(function (data) { self.plans(data.items || data || []); })
            .fail(function (xhr) { toastr.error(readError(xhr, t('crm.salon.memberships.plans_load_failed', 'Uyelik planlari yuklenemedi'))); });

        $.get('/proxy/crm/salon/memberships')
            .done(function (data) { self.memberships(data.items || data || []); })
            .fail(function (xhr) { toastr.error(readError(xhr, t('crm.salon.memberships.load_failed', 'Musteri uyelikleri yuklenemedi'))); });
    };

    self.loadClients = function () {
        $.get('/proxy/crm/salon/clients?pageSize=1000')
            .done(function (data) { self.clients(data.items || data || []); });
    };

    self.openNewPlan = function () {
        self.isEditingPlan(false);
        self.editingPlanId(null);
        self.planForm.name('');
        self.planForm.description('');
        self.planForm.price(0);
        self.planForm.discountPercent(10);
        self.planForm.durationDays(30);
        self.planForm.priorityBooking(false);
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
        planModal.show();
    };

    self.savePlan = function () {
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
            serviceIds: [],
            serviceDetails: []
        };

        if (!payload.name) {
            toastr.warning(t('crm.salon.memberships.plan_name_required_warning', 'Plan adi zorunludur'));
            return;
        }

        self.isSaving(true);
        var url = '/proxy/crm/salon/memberships/plans?allBranches=true';
        var method = 'POST';
        if (self.isEditingPlan()) {
            url = '/proxy/crm/salon/memberships/plans/' + self.editingPlanId() + '?allBranches=true';
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
            t('crm.salon.memberships.delete_plan_title', 'Plani Sil'),
            t('crm.salon.memberships.delete_plan_confirm', "'{name}' planini silmek istediginize emin misiniz?").replace('{name}', plan.name || ''),
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

    self.openNewMembership = function () {
        self.memberForm.planId(null);
        self.memberForm.slnClientId(null);
        memberModal.show();
    };

    self.saveMembership = function () {
        var payload = {
            planId: parseInt(self.memberForm.planId(), 10) || 0,
            slnClientId: parseInt(self.memberForm.slnClientId(), 10) || 0
        };

        if (!payload.planId || !payload.slnClientId) {
            toastr.warning(t('crm.salon.memberships.plan_client_required', 'Plan ve musteri zorunludur'));
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
            toastr.success(t('crm.salon.memberships.created', 'Uyelik olusturuldu'));
        }).fail(function (xhr) {
            toastr.error(readError(xhr, t('crm.salon.memberships.create_failed', 'Uyelik olusturulamadi')));
        }).always(function () {
            self.isSaving(false);
        });
    };

    self.freezeMembership = function (membership) {
        $.ajax({ url: '/proxy/crm/salon/memberships/' + membership.id + '/freeze', method: 'PUT' })
            .done(function () { self.loadData(); toastr.info(t('crm.salon.memberships.frozen_success', 'Uyelik donduruldu')); })
            .fail(function (xhr) { toastr.error(readError(xhr, t('crm.salon.memberships.freeze_failed', 'Uyelik dondurulamadi'))); });
    };

    self.cancelMembership = function (membership) {
        confirmModal(t('crm.salon.memberships.cancel_title', 'Uyelik Iptali'), t('crm.salon.memberships.cancel_confirm', 'Uyeligi iptal etmek istediginize emin misiniz?'), function () {
            $.ajax({ url: '/proxy/crm/salon/memberships/' + membership.id + '/cancel', method: 'PUT' })
                .done(function () { self.loadData(); toastr.success(t('crm.salon.memberships.cancelled_success', 'Uyelik iptal edildi')); })
                .fail(function (xhr) { toastr.error(readError(xhr, t('crm.salon.memberships.cancel_failed', 'Uyelik iptal edilemedi'))); });
        });
    };

    self.reactivateMembership = function (membership) {
        $.ajax({ url: '/proxy/crm/salon/memberships/' + membership.id + '/reactivate', method: 'PUT' })
            .done(function () { self.loadData(); toastr.success(t('crm.salon.memberships.activated_success', 'Uyelik aktif edildi')); })
            .fail(function (xhr) { toastr.error(readError(xhr, t('crm.salon.memberships.activate_failed', 'Uyelik aktif edilemedi'))); });
    };

    $(document).ready(function () {
        planModal = new bootstrap.Modal(document.getElementById('membershipPlanModal'));
        memberModal = new bootstrap.Modal(document.getElementById('clientMembershipModal'));
        self.loadClients();
        self.loadData();
    });
}

ko.applyBindings(new SalonMembershipsViewModel(), document.getElementById('salon-memberships-vm'));
