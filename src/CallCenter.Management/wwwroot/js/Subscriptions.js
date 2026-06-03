function SubViewModel() {
    var self = this;
    function toDateStr(d) {
        var month = String(d.getMonth() + 1).padStart(2, '0');
        var day = String(d.getDate()).padStart(2, '0');
        return d.getFullYear() + '-' + month + '-' + day;
    }

    self.plans = ko.observableArray([]);
    self.subscriptions = ko.observableArray([]);
    self.customers = ko.observableArray([]);
    self.editingPlanId = ko.observable(null);
    self.isGenerating = ko.observable(false);
    self.billingResult = ko.observable('');
    self.billingYear = ko.observable(new Date().getFullYear());
    self.billingMonth = ko.observable(new Date().getMonth() + 1);

    self.planForm = { name: ko.observable(''), intervalMonths: ko.observable(1), discountPercent: ko.observable(0) };
    self.subForm = { customerId: ko.observable(null), planId: ko.observable(null), branchId: ko.observable(null), startDate: ko.observable(''), monthlyPrice: ko.observable(0), discountOverride: ko.observable('') };
    self.branches = ko.observableArray([]);

    // Firma degisince o firmanin subelerini yukle
    self.subForm.customerId.subscribe(function (cid) {
        self.subForm.branchId(null);
        self.branches([]);
        if (!cid) return;
        $.get('/proxy/subscriptions/branches/' + cid, function (data) {
            self.branches(data || []);
        });
    });

    self.computedPeriodPrice = ko.computed(function () {
        var plan = self.plans().find(function (p) { return p.id == self.subForm.planId(); });
        if (!plan) return '0';
        var mp = parseFloat(self.subForm.monthlyPrice()) || 0;
        if (mp > 0) {
            return (mp * plan.intervalMonths).toFixed(2);
        }
        var o = self.subForm.discountOverride();
        var disc = o !== '' && o != null && !isNaN(parseFloat(o)) ? parseFloat(o) : (plan.discountPercent || 0);
        var total = mp * plan.intervalMonths * (1 - disc / 100);
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
        self.planForm.name(''); self.planForm.intervalMonths(1); self.planForm.discountPercent(0);
        planModal.show();
    };
    self.editPlan = function (p) {
        self.editingPlanId(p.id);
        self.planForm.name(p.name); self.planForm.intervalMonths(p.intervalMonths); self.planForm.discountPercent(p.discountPercent);
        planModal.show();
    };
    self.savePlan = function () {
        var d = { name: self.planForm.name(), intervalMonths: parseInt(self.planForm.intervalMonths()), discountPercent: parseFloat(self.planForm.discountPercent()) || 0, isActive: true };
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
        self.subForm.customerId(null); self.subForm.planId(null); self.subForm.branchId(null);
        self.subForm.startDate(toDateStr(new Date()));
        self.subForm.monthlyPrice(0);
        self.subForm.discountOverride('');
        self.branches([]);
        subModal.show();
    };
    self.saveSub = function () {
        var dOv = self.subForm.discountOverride();
        var discOv = (dOv !== '' && dOv != null && !isNaN(parseFloat(dOv))) ? parseFloat(dOv) : null;
        var payload = {
            customerId: parseInt(self.subForm.customerId()),
            planId: parseInt(self.subForm.planId()),
            branchId: self.subForm.branchId() ? parseInt(self.subForm.branchId()) : null,
            startDate: self.subForm.startDate(),
            monthlyPrice: parseFloat(self.subForm.monthlyPrice()) || 0,
            discountPercentOverride: discOv
        };
        if (!payload.customerId || !payload.planId || !payload.startDate) { toastr.warning('Tum alanlar zorunludur.'); return; }
        $.ajax({ url: '/proxy/subscriptions', method: 'POST', contentType: 'application/json', data: JSON.stringify(payload) }).done(function () {
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
            var el = d.eligible != null ? d.eligible : '';
            self.billingResult('<strong>' + d.created + '</strong> tahakkuk olusturuldu, <strong>' + d.skipped + '</strong> atlandi (zaten mevcut).' +
                (el !== '' ? ' <span class="text-muted">Bu ay icin uygun abonelik: <strong>' + el + '</strong>.</span>' : '') +
                (el === 0 ? ' <strong class="text-warning">Uyarı:</strong> Seçilen ayda hesaplanan sonraki kesim tarihi düşen aktif abonelik yok.' : ''));
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
