function ModulesViewModel() {
    var self = this;
    self.activeModules = ko.observableArray([]);
    self.availableModules = ko.observableArray([]);
    self.requests = ko.observableArray([]);

    self.defaultModules = ko.computed(function () {
        return self.activeModules().filter(function (m) { return m.isDefault; });
    });

    // Paket sabit fiyatları — SalonModuleGroups.cs ile senkron
    var PACKAGE_PRICES = { 1: 400, 3: 1500, 5: 1500, 6: 200 };
    var PACKAGE_NAMES = { 1: 'Stok Tedarik / Finans', 3: 'Müşteri Sadakati / Pazarlama', 5: 'Profesyonel', 6: 'Kurumsal' };

    self.activeGroups = ko.computed(function () {
        var nonDefault = self.activeModules().filter(function (m) { return !m.isDefault && m.isActive; });
        var grouped = {};
        nonDefault.forEach(function (m) {
            var gId = m.groupId || 0;
            var gName = PACKAGE_NAMES[gId] || m.groupName || 'Diger';
            if (!grouped[gId]) grouped[gId] = { groupId: gId, groupName: gName, packagePrice: PACKAGE_PRICES[gId] || 0, modules: [] };
            grouped[gId].modules.push(m);
        });
        return Object.values(grouped).sort(function (a, b) { return a.groupId - b.groupId; });
    });

    self.availableGroups = ko.computed(function () {
        var all = self.availableModules();
        var grouped = {};
        all.forEach(function (m) {
            var gId = m.groupId || 0;
            var gName = PACKAGE_NAMES[gId] || m.groupName || 'Diger';
            if (!grouped[gId]) grouped[gId] = { groupId: gId, groupName: gName, packagePrice: PACKAGE_PRICES[gId] || 0, modules: [] };
            grouped[gId].modules.push(m);
        });
        return Object.values(grouped).sort(function (a, b) { return a.groupId - b.groupId; });
    });

    self.branchCount = ko.observable(1);

    // Aylik toplam = (Temel Paket 1700 + aktif grup paket fiyatları) × (1 + 0.9*(N-1)) (sube indirimi)
    self.baseMonthly = ko.computed(function () {
        var total = 1700; // Temel Paket zorunlu
        var activeGroupIds = {};
        self.activeModules().forEach(function (m) {
            if (!m.isDefault && m.isActive && m.groupId) activeGroupIds[m.groupId] = true;
        });
        Object.keys(activeGroupIds).forEach(function (gId) {
            total += PACKAGE_PRICES[gId] || 0;
        });
        return total;
    });
    self.branchMultiplier = ko.computed(function () {
        var n = self.branchCount() || 1;
        return 1 + 0.9 * (n - 1);
    });
    self.monthlyTotal = ko.computed(function () {
        return Math.round(self.baseMonthly() * self.branchMultiplier() * 100) / 100;
    });

    self.load = function () {
        $.get('/proxy/sln-module-requests', function (data) { self.requests(data || []); });
        $.get('/proxy/sln-module-requests/available', function (data) { self.availableModules(data || []); });
        $.get('/proxy/sln-module-requests/active', function (data) { self.activeModules(data || []); });
        $.get('/proxy/sln-branches?_nb=1', function (data) {
            var branches = Array.isArray(data) ? data : (data.items || []);
            var active = branches.filter(function (b) { return b.isActive !== false; }).length;
            self.branchCount(active > 0 ? active : 1);
        });
    };

    self.requestDeactivation = function (mod) {
        var name = mod.description || mod.systemName;
        confirmModal('Modul Iptali', name + ' modulunu iptal etmek istediginize emin misiniz?\nIptal talebi admin onayina gonderilecektir.', function () {
            confirmModal('Iptal Sebebi', 'Iptal sebebini girebilirsiniz (zorunlu degil):', function (notes) {
                $.ajax({
                    url: '/proxy/sln-module-requests',
                    method: 'POST',
                    contentType: 'application/json',
                    data: JSON.stringify({ moduleId: mod.id, requestTypeId: 2, notes: notes || null }),
                    success: function () { toastr.success('Iptal talebi olusturuldu.'); self.load(); },
                    error: function (xhr) { toastr.error(xhr.responseJSON?.message || 'Talep olusturulamadi.'); }
                });
            }, { input: true, inputLabel: 'Iptal sebebi' });
        }, { confirmClass: 'btn-danger', confirmText: 'Iptal Talep Et' });
    };

    self.requestModule = function (mod) {
        var name = mod.description || mod.moduleName;
        confirmModal('Modul Talebi', name + ' modulunu talep etmek istiyor musunuz?', function () {
            confirmModal('Not Ekle', 'Not eklemek ister misiniz? (Bos birakilabilir)', function (notes) {
                $.ajax({
                    url: '/proxy/sln-module-requests',
                    method: 'POST',
                    contentType: 'application/json',
                    data: JSON.stringify({ moduleId: mod.moduleId, notes: notes || null }),
                    success: function () { toastr.success('Modul talebi olusturuldu.'); self.load(); },
                    error: function (xhr) { toastr.error(xhr.responseJSON?.message || 'Talep olusturulamadi.'); }
                });
            }, { input: true, inputLabel: 'Notunuz' });
        });
    };

    self.cancelPackage = function (pkg) {
        var name = pkg.groupName;
        var count = pkg.modules.length;
        confirmModal('Paket Iptali', name + ' paketini iptal etmek ister misiniz?\n\nIçindeki ' + count + ' modül için toplu iptal talebi oluşturulacak.', function () {
            var done = 0, errors = 0;
            pkg.modules.forEach(function (m) {
                $.ajax({
                    url: '/proxy/sln-module-requests',
                    method: 'POST',
                    contentType: 'application/json',
                    data: JSON.stringify({ moduleId: m.id, requestTypeId: 2, notes: 'Paket iptali: ' + name })
                }).always(function (res, status) {
                    done++;
                    if (status === 'error') errors++;
                    if (done === pkg.modules.length) {
                        if (errors === 0) toastr.success('Paket iptal talebi olusturuldu (' + done + ' modül).');
                        else toastr.warning(done - errors + '/' + done + ' modul icin iptal talebi olusturuldu.');
                        self.load();
                    }
                });
            });
        }, { confirmClass: 'btn-danger', confirmText: 'Paketi İptal Et' });
    };

    self.requestPackage = function (pkg) {
        var name = pkg.groupName;
        var count = pkg.modules.length;
        var price = (pkg.packagePrice || 0).toLocaleString('tr-TR');
        confirmModal('Paket Talebi', name + ' paketini talep etmek ister misiniz?\n\nFiyat: ' + price + ' ₺/ay (KDV dahil)\nIçindeki ' + count + ' modül topluca aktif olacak.', function () {
            var errors = 0, done = 0;
            pkg.modules.forEach(function (m) {
                $.ajax({
                    url: '/proxy/sln-module-requests',
                    method: 'POST',
                    contentType: 'application/json',
                    data: JSON.stringify({ moduleId: m.moduleId, notes: 'Paket talebi: ' + name })
                }).always(function (res, status) {
                    done++;
                    if (status === 'error') errors++;
                    if (done === pkg.modules.length) {
                        if (errors === 0) toastr.success('Paket talebi olusturuldu (' + done + ' modül).');
                        else toastr.warning(done - errors + '/' + done + ' modul icin talep olusturuldu.');
                        self.load();
                    }
                });
            });
        });
    };

    self.cancelRequest = function (req) {
        confirmModal('Talep Iptali', 'Bu talebi iptal etmek istiyor musunuz?', function () {
            $.ajax({
                url: '/proxy/sln-module-requests/' + req.id,
                method: 'DELETE',
                success: function () { toastr.success('Talep iptal edildi.'); self.load(); },
                error: function (xhr) { toastr.error(xhr.responseJSON?.message || 'Iptal edilemedi.'); }
            });
        }, { confirmClass: 'btn-danger', confirmText: 'Iptal Et' });
    };

    self.load();
}

ko.applyBindings(new ModulesViewModel(), document.getElementById('modules-vm'));
