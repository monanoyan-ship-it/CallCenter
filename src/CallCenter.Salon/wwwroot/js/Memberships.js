function MembershipsViewModel() {
    var self = this;
    var MEMBERSHIPS_LOCALE = document.documentElement.lang || undefined;
    function memberT(key, fallback) {
        return (window.salonT || function (k, f) { return f || k; })(key, fallback);
    }
    self.plans = ko.observableArray([]);
    self.memberships = ko.observableArray([]);
    self.clientList = ko.observableArray([]);
    self.isEditingPlan = ko.observable(false);
    self.editingPlanId = ko.observable(null);
    self.isSaving = ko.observable(false);

    self.serviceCategories = ko.observableArray([]);

    self.planForm = {
        name: ko.observable(''), description: ko.observable(''), iconClass: ko.observable('bi-award'),
        color: ko.observable('#7b1fa2'), price: ko.observable(0), discountPercent: ko.observable(10),
        priorityBooking: ko.observable(false),
        durationType: ko.observable(1), durationDays: ko.observable(30),
        serviceIds: ko.observableArray([])
    };

    // Kategori → Hizmet secim yapisi (randevudaki gibi)
    self.selectedCategoryId = ko.observable(null);

    self.selectedCategoryServices = ko.computed(function () {
        var catId = self.selectedCategoryId();
        if (!catId) return [];
        var cat = self.serviceCategories().find(function (c) { return c.id === catId; });
        return cat ? (cat.services || []) : [];
    });

    // Hizmet detay listesi (freeCount + discount observable)
    self._planServiceDetailMap = {};

    self.planServiceDetails = ko.computed(function () {
        var ids = self.planForm.serviceIds();
        if (!ids || !ids.length) return [];
        var allServices = [];
        self.serviceCategories().forEach(function (cat) {
            (cat.services || []).forEach(function (s) { allServices.push(s); });
        });
        return ids.map(function (id) {
            var svc = allServices.find(function (s) { return s.id === id; });
            if (!self._planServiceDetailMap[id]) {
                self._planServiceDetailMap[id] = {
                    serviceId: id,
                    name: svc ? svc.name : '?',
                    freeCount: ko.observable(0),
                    discount: ko.observable(0)
                };
            }
            return self._planServiceDetailMap[id];
        });
    });

    self.togglePlanService = function (serviceId) {
        var ids = self.planForm.serviceIds().slice();
        var idx = ids.indexOf(serviceId);
        if (idx >= 0) ids.splice(idx, 1);
        else ids.push(serviceId);
        self.planForm.serviceIds(ids);
    };

    self.memberForm = { planId: ko.observable(null), slnClientId: ko.observable(null) };
    self.clientAutocomplete = createAutocomplete(self.clientList, 'fullName', self.memberForm.slnClientId);

    var planModal, memberModal;

    self.loadData = function () {
        $.ajax({ url: '/proxy/sln-memberships/plans', method: 'GET' }).done(function (d) { self.plans(d.items || d); });
        $.ajax({ url: '/proxy/sln-memberships', method: 'GET' }).done(function (d) { self.memberships(d.items || d); });
    };

    self.loadClients = function () {
        $.ajax({ url: '/proxy/sln-clients?pageSize=1000', method: 'GET' }).done(function (d) { self.clientList(d.items || d); });
    };

    self.loadServices = function () {
        $.ajax({ url: '/proxy/sln-services/categories', method: 'GET' }).done(function (d) { self.serviceCategories(d.items || d || []); });
    };

    // Yeni musteri inline form (Appointments ile ayni yapi)
    self.newClientVisible = ko.observable(false);
    self.isCreatingClient = ko.observable(false);
    self.newClientForm = {
        fullName: ko.observable(''),
        phone: ko.observable('')
    };

    self.showNewClient = function (queryText) {
        self.newClientForm.fullName(queryText || '');
        self.newClientForm.phone('');
        self.newClientVisible(true);
        self.clientAutocomplete.showDropdown(false);
    };

    self.hideNewClient = function () {
        self.newClientVisible(false);
    };

    self.saveNewClient = function () {
        var name = self.newClientForm.fullName();
        var phone = self.newClientForm.phone();
        if (!name || !phone) { toastr.warning(memberT('salon.memberships.name_phone_required', 'Ad ve telefon zorunludur')); return; }

        self.isCreatingClient(true);
        $.ajax({
            url: '/proxy/sln-clients',
            method: 'POST',
            contentType: 'application/json',
            data: JSON.stringify({ fullName: name, phone: phone })
        }).done(function (newClient) {
            var list = self.clientList();
            list.push(newClient);
            self.clientList(list);
            self.memberForm.slnClientId(newClient.id);
            self.clientAutocomplete.query(newClient.fullName);
            self.clientAutocomplete.selectedName(newClient.fullName);
            self.newClientVisible(false);
            toastr.success(memberT('salon.memberships.customer_created_selected', 'Müşteri oluşturuldu ve seçildi'));
        }).fail(function (xhr) {
            toastr.error(xhr.responseJSON?.error || memberT('salon.memberships.customer_create_failed', 'Müşteri oluşturulamadı'));
        }).always(function () { self.isCreatingClient(false); });
    };

    // Plan CRUD
    self.openNewPlan = function () {
        self.isEditingPlan(false); self.editingPlanId(null);
        self.planForm.name(''); self.planForm.description(''); self.planForm.color('#7b1fa2');
        self.planForm.price(0); self.planForm.discountPercent(10);
        self.planForm.priorityBooking(false);
        self.planForm.durationType(1); self.planForm.durationDays(30);
        self.planForm.serviceIds([]); self._planServiceDetailMap = {}; self.selectedCategoryId(null);
        planModal.show();
    };

    self.openEditPlan = function (p) {
        self.isEditingPlan(true); self.editingPlanId(p.id);
        self.planForm.name(p.name); self.planForm.description(p.description || '');
        self.planForm.color(p.color || '#7b1fa2'); self.planForm.price(p.price || 0);
        self.planForm.discountPercent(p.discountPercent); self.planForm.priorityBooking(p.priorityBooking);
        self.planForm.durationType(p.durationType || 1); self.planForm.durationDays(p.durationDays || 30);
        // Mevcut hizmet detaylarini yukle
        self._planServiceDetailMap = {};
        if (p.serviceDetails) {
            p.serviceDetails.forEach(function (sd) {
                self._planServiceDetailMap[sd.serviceId] = {
                    serviceId: sd.serviceId,
                    name: sd.serviceName || '?',
                    freeCount: ko.observable(sd.freeCount || 0),
                    discount: ko.observable(sd.discountPercent || 0)
                };
            });
        }
        self.planForm.serviceIds(p.serviceIds || []);
        self.selectedCategoryId(null);
        planModal.show();
    };

    self.savePlan = function () {
        var d = {
            name: self.planForm.name(), description: self.planForm.description(),
            iconClass: 'bi-award', color: self.planForm.color(),
            price: parseFloat(self.planForm.price()) || 0,
            discountPercent: parseInt(self.planForm.discountPercent()) || 0,
            durationType: parseInt(self.planForm.durationType()) || 1,
            durationDays: parseInt(self.planForm.durationDays()) || 30,
            priorityBooking: self.planForm.priorityBooking(), isActive: true,
            serviceIds: self.planForm.serviceIds() || [],
            serviceDetails: self.planServiceDetails().map(function (s) {
                return { serviceId: s.serviceId, freeCount: parseInt(s.freeCount()) || 0, discountPercent: parseInt(s.discount()) || 0 };
            })
        };
        if (!d.name) { toastr.warning(memberT('salon.memberships.plan_name_required', 'Plan adı zorunludur')); return; }

        self.isSaving(true);
        var url = '/proxy/sln-memberships/plans';
        var method = 'POST';
        if (self.isEditingPlan()) { url += '/' + self.editingPlanId(); method = 'PUT'; }
        $.ajax({ url: url, method: method, contentType: 'application/json', data: JSON.stringify(d) }).done(function () {
            planModal.hide(); self.loadData(); toastr.success(memberT('salon.memberships.plan_saved', 'Plan kaydedildi')); self.isSaving(false);
        }).fail(function (x) { toastr.error(x.responseJSON?.error || memberT('salon.common.error.generic', 'Bir hata oluştu')); self.isSaving(false); });
    };

    self.removePlan = function (p) {
        confirmModal(
            memberT('salon.common.btn.confirm', 'Onayla'),
            memberT('salon.memberships.delete_plan_confirm', "'{name}' planını silmek istediğinize emin misiniz?").replace('{name}', p.name),
            function() {
            $.ajax({ url: '/proxy/sln-memberships/plans/' + p.id, method: 'DELETE' }).done(function () {
                self.loadData(); toastr.success(memberT('salon.memberships.plan_deleted', 'Plan silindi'));
            }).fail(function (x) { toastr.error(x.responseJSON?.error || memberT('salon.common.error.delete_failed', 'Silinemedi')); });
        });
    };

    // Uye islemleri
    self.openNewMember = function () {
        self.memberForm.planId(null); self.memberForm.slnClientId(null); self.clientAutocomplete.clear();
        memberModal.show();
    };

    self.saveMember = function () {
        var d = { planId: parseInt(self.memberForm.planId()), slnClientId: parseInt(self.memberForm.slnClientId()) };
        if (!d.planId || !d.slnClientId) { toastr.warning(memberT('salon.memberships.plan_customer_required', 'Plan ve müşteri zorunludur')); return; }

        self.isSaving(true);
        $.ajax({ url: '/proxy/sln-memberships', method: 'POST', contentType: 'application/json', data: JSON.stringify(d) }).done(function () {
            memberModal.hide(); self.loadData(); toastr.success(memberT('salon.memberships.membership_created', 'Üyelik oluşturuldu')); self.isSaving(false);
        }).fail(function (x) { toastr.error(x.responseJSON?.error || memberT('salon.common.error.generic', 'Bir hata oluştu')); self.isSaving(false); });
    };

    self.freezeMember = function (m) {
        $.ajax({ url: '/proxy/sln-memberships/' + m.id + '/freeze', method: 'PUT' }).done(function () { self.loadData(); toastr.info(memberT('salon.memberships.membership_frozen', 'Üyelik donduruldu')); });
    };

    self.cancelMember = function (m) {
        confirmModal(memberT('salon.common.btn.confirm', 'Onayla'), memberT('salon.memberships.cancel_member_confirm', 'Üyeliği iptal etmek istediğinize emin misiniz?'), function() {
            $.ajax({ url: '/proxy/sln-memberships/' + m.id + '/cancel', method: 'PUT' }).done(function () { self.loadData(); toastr.success(memberT('salon.memberships.membership_cancelled', 'Üyelik iptal edildi')); });
        });
    };

    self.reactivateMember = function (m) {
        $.ajax({ url: '/proxy/sln-memberships/' + m.id + '/reactivate', method: 'PUT' }).done(function () { self.loadData(); toastr.success(memberT('salon.memberships.membership_reactivated', 'Üyelik tekrar aktif')); });
    };

    $(document).ready(function () {
        planModal = new bootstrap.Modal(document.getElementById('planModal'));
        memberModal = new bootstrap.Modal(document.getElementById('memberModal'));
        self.loadClients(); self.loadServices(); self.loadData();
    });
}

ko.applyBindings(new MembershipsViewModel(), document.getElementById('memberships-vm'));
