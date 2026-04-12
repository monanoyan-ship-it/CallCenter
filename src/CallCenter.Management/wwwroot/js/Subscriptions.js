function SubViewModel() {
    var self = this;
    self.plans = ko.observableArray([]);
    self.subscriptions = ko.observableArray([]);
    self.customers = ko.observableArray([]);
    self.editingPlanId = ko.observable(null);
    self.isGenerating = ko.observable(false);
    self.billingResult = ko.observable('');
    self.billingYear = ko.observable(new Date().getFullYear());
    self.billingMonth = ko.observable(new Date().getMonth() + 1);

    self.planForm = { name: ko.observable(''), intervalMonths: ko.observable(1), discountPercent: ko.observable(0), branchPrice: ko.observable(0) };
    self.subForm = { customerId: ko.observable(null), planId: ko.observable(null), startDate: ko.observable(''), monthlyPrice: ko.observable(0) };

    self.computedPeriodPrice = ko.computed(function () {
        var plan = self.plans().find(function (p) { return p.id == self.subForm.planId(); });
        if (!plan) return '0';
        var mp = parseFloat(self.subForm.monthlyPrice()) || 0;
        var total = mp * plan.intervalMonths * (1 - (plan.discountPercent || 0) / 100);
        return total.toFixed(2);
    });

    var planModal, subModal;

    self.load = function () {
        $.get('/proxy/subscriptions/plans', function (d) { self.plans(d); });
        $.get('/proxy/subscriptions', function (d) { self.subscriptions(d); });
        $.get('/proxy/customers?pageSize=500', function (d) { self.customers(d.items || d); });
    };

    // Plan CRUD
    self.openNewPlan = function () {
        self.editingPlanId(null);
        self.planForm.name(''); self.planForm.intervalMonths(1); self.planForm.discountPercent(0); self.planForm.branchPrice(0);
        planModal.show();
    };
    self.editPlan = function (p) {
        self.editingPlanId(p.id);
        self.planForm.name(p.name); self.planForm.intervalMonths(p.intervalMonths); self.planForm.discountPercent(p.discountPercent); self.planForm.branchPrice(p.branchPrice || 0);
        planModal.show();
    };
    self.savePlan = function () {
        var d = { name: self.planForm.name(), intervalMonths: parseInt(self.planForm.intervalMonths()), discountPercent: parseFloat(self.planForm.discountPercent()) || 0, branchPrice: parseFloat(self.planForm.branchPrice()) || 0, isActive: true };
        var url = '/proxy/subscriptions/plans';
        var method = 'POST';
        if (self.editingPlanId()) { url += '/' + self.editingPlanId(); method = 'PUT'; }
        $.ajax({ url: url, method: method, contentType: 'application/json', data: JSON.stringify(d) }).done(function () {
            planModal.hide(); self.load(); toastr.success('Plan kaydedildi.');
        }).fail(function (x) { toastr.error(x.responseJSON?.message || 'Hata'); });
    };
    self.deletePlan = function (p) {
        confirmModal('Plan Sil', p.name + ' planini silmek istediginize emin misiniz?', function () {
            $.ajax({ url: '/proxy/subscriptions/plans/' + p.id, method: 'DELETE' }).done(function () {
                self.load(); toastr.success('Plan silindi.');
            }).fail(function (x) { toastr.error(x.responseJSON?.message || 'Silinemedi'); });
        }, { confirmText: 'Sil', confirmClass: 'btn-danger' });
    };

    // Abonelik
    self.openNewSub = function () {
        self.subForm.customerId(null); self.subForm.planId(null);
        self.subForm.startDate(new Date().toISOString().substring(0, 10));
        self.subForm.monthlyPrice(0);
        subModal.show();
    };
    self.saveSub = function () {
        var d = {
            customerId: parseInt(self.subForm.customerId()),
            planId: parseInt(self.subForm.planId()),
            startDate: self.subForm.startDate(),
            monthlyPrice: parseFloat(self.subForm.monthlyPrice()) || 0
        };
        if (!d.customerId || !d.planId || !d.startDate) { toastr.warning('Tum alanlar zorunludur.'); return; }
        $.ajax({ url: '/proxy/subscriptions', method: 'POST', contentType: 'application/json', data: JSON.stringify(d) }).done(function () {
            subModal.hide(); self.load(); toastr.success('Abonelik olusturuldu.');
        }).fail(function (x) { toastr.error(x.responseJSON?.message || 'Hata'); });
    };
    self.cancelSub = function (s) {
        confirmModal('Abonelik Iptali', s.customerName + ' aboneligini iptal etmek istiyor musunuz?', function () {
            $.ajax({ url: '/proxy/subscriptions/' + s.id + '/cancel', method: 'PUT' }).done(function () {
                self.load(); toastr.success('Abonelik iptal edildi.');
            }).fail(function (x) { toastr.error(x.responseJSON?.message || 'Hata'); });
        }, { confirmText: 'Iptal Et', confirmClass: 'btn-danger' });
    };

    // Tahakkuk
    self.generateBilling = function () {
        self.isGenerating(true); self.billingResult('');
        $.ajax({
            url: '/proxy/subscriptions/generate-billing',
            method: 'POST',
            contentType: 'application/json',
            data: JSON.stringify({ year: parseInt(self.billingYear()), month: parseInt(self.billingMonth()) })
        }).done(function (d) {
            self.billingResult('<strong>' + d.created + '</strong> tahakkuk olusturuldu, <strong>' + d.skipped + '</strong> atlandi (zaten mevcut).');
        }).fail(function (x) {
            toastr.error(x.responseJSON?.message || 'Hata');
        }).always(function () { self.isGenerating(false); });
    };

    $(document).ready(function () {
        planModal = new bootstrap.Modal(document.getElementById('planModal'));
        subModal = new bootstrap.Modal(document.getElementById('subModal'));
        self.load();
    });
}
ko.applyBindings(new SubViewModel(), document.getElementById('sub-vm'));
