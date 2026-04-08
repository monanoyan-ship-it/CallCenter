function MembershipsViewModel() {
    var self = this;
    self.plans = ko.observableArray([]);
    self.memberships = ko.observableArray([]);
    self.clientList = ko.observableArray([]);
    self.isEditingPlan = ko.observable(false);
    self.editingPlanId = ko.observable(null);
    self.isSaving = ko.observable(false);

    self.serviceCategories = ko.observableArray([]);

    self.planForm = {
        name: ko.observable(''), description: ko.observable(''), iconClass: ko.observable('bi-award'),
        color: ko.observable('#7b1fa2'), monthlyPrice: ko.observable(0), discountPercent: ko.observable(10),
        freeSessionsPerMonth: ko.observable(0), priorityBooking: ko.observable(false),
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

    self.selectedPlanServiceNames = ko.computed(function () {
        var ids = self.planForm.serviceIds();
        if (!ids || !ids.length) return [];
        var allServices = [];
        self.serviceCategories().forEach(function (cat) {
            (cat.services || []).forEach(function (s) { allServices.push(s); });
        });
        return ids.map(function (id) {
            var svc = allServices.find(function (s) { return s.id === id; });
            return svc ? { id: svc.id, name: svc.name } : { id: id, name: '?' };
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
        if (!name || !phone) { toastr.warning('Ad ve telefon zorunludur'); return; }

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
            toastr.success('Müşteri oluşturuldu ve seçildi');
        }).fail(function (xhr) {
            toastr.error(xhr.responseJSON?.error || 'Müşteri oluşturulamadı');
        }).always(function () { self.isCreatingClient(false); });
    };

    // Plan CRUD
    self.openNewPlan = function () {
        self.isEditingPlan(false); self.editingPlanId(null);
        self.planForm.name(''); self.planForm.description(''); self.planForm.color('#7b1fa2');
        self.planForm.monthlyPrice(0); self.planForm.discountPercent(10);
        self.planForm.freeSessionsPerMonth(0); self.planForm.priorityBooking(false);
        self.planForm.serviceIds([]); self.selectedCategoryId(null);
        planModal.show();
    };

    self.openEditPlan = function (p) {
        self.isEditingPlan(true); self.editingPlanId(p.id);
        self.planForm.name(p.name); self.planForm.description(p.description || '');
        self.planForm.color(p.color || '#7b1fa2'); self.planForm.monthlyPrice(p.monthlyPrice);
        self.planForm.discountPercent(p.discountPercent); self.planForm.freeSessionsPerMonth(p.freeSessionsPerMonth);
        self.planForm.priorityBooking(p.priorityBooking);
        self.planForm.serviceIds(p.serviceIds || []);
        self.selectedCategoryId(null);
        planModal.show();
    };

    self.savePlan = function () {
        var d = {
            name: self.planForm.name(), description: self.planForm.description(),
            iconClass: 'bi-award', color: self.planForm.color(),
            monthlyPrice: parseFloat(self.planForm.monthlyPrice()) || 0,
            discountPercent: parseInt(self.planForm.discountPercent()) || 0,
            freeSessionsPerMonth: parseInt(self.planForm.freeSessionsPerMonth()) || 0,
            priorityBooking: self.planForm.priorityBooking(), isActive: true,
            serviceIds: self.planForm.serviceIds() || []
        };
        if (!d.name) { toastr.warning('Plan adi zorunludur'); return; }

        self.isSaving(true);
        var url = '/proxy/sln-memberships/plans';
        var method = 'POST';
        if (self.isEditingPlan()) { url += '/' + self.editingPlanId(); method = 'PUT'; }
        $.ajax({ url: url, method: method, contentType: 'application/json', data: JSON.stringify(d) }).done(function () {
            planModal.hide(); self.loadData(); toastr.success('Plan kaydedildi'); self.isSaving(false);
        }).fail(function (x) { toastr.error(x.responseJSON?.error || 'Hata'); self.isSaving(false); });
    };

    self.removePlan = function (p) {
        if (!confirm("'" + p.name + "' planini silmek istediginize emin misiniz?")) return;
        $.ajax({ url: '/proxy/sln-memberships/plans/' + p.id, method: 'DELETE' }).done(function () {
            self.loadData(); toastr.success('Plan silindi');
        }).fail(function (x) { toastr.error(x.responseJSON?.error || 'Silinemedi'); });
    };

    // Uye islemleri
    self.openNewMember = function () {
        self.memberForm.planId(null); self.memberForm.slnClientId(null); self.clientAutocomplete.clear();
        memberModal.show();
    };

    self.saveMember = function () {
        var d = { planId: parseInt(self.memberForm.planId()), slnClientId: parseInt(self.memberForm.slnClientId()) };
        if (!d.planId || !d.slnClientId) { toastr.warning('Plan ve musteri zorunludur'); return; }

        self.isSaving(true);
        $.ajax({ url: '/proxy/sln-memberships', method: 'POST', contentType: 'application/json', data: JSON.stringify(d) }).done(function () {
            memberModal.hide(); self.loadData(); toastr.success('Uyelik olusturuldu'); self.isSaving(false);
        }).fail(function (x) { toastr.error(x.responseJSON?.error || 'Hata'); self.isSaving(false); });
    };

    self.freezeMember = function (m) {
        $.ajax({ url: '/proxy/sln-memberships/' + m.id + '/freeze', method: 'PUT' }).done(function () { self.loadData(); toastr.info('Uyelik donduruldu'); });
    };

    self.cancelMember = function (m) {
        if (!confirm('Uyeligi iptal etmek istediginize emin misiniz?')) return;
        $.ajax({ url: '/proxy/sln-memberships/' + m.id + '/cancel', method: 'PUT' }).done(function () { self.loadData(); toastr.success('Uyelik iptal edildi'); });
    };

    self.reactivateMember = function (m) {
        $.ajax({ url: '/proxy/sln-memberships/' + m.id + '/reactivate', method: 'PUT' }).done(function () { self.loadData(); toastr.success('Uyelik tekrar aktif'); });
    };

    $(document).ready(function () {
        planModal = new bootstrap.Modal(document.getElementById('planModal'));
        memberModal = new bootstrap.Modal(document.getElementById('memberModal'));
        self.loadClients(); self.loadServices(); self.loadData();
    });
}

ko.applyBindings(new MembershipsViewModel(), document.getElementById('memberships-vm'));
