function DetailViewModel() {
    var self = this;
    self.customer = ko.observable({});
    self.newPassword = ko.observable('');

    self.edit = {
        name: ko.observable(''), taxNumber: ko.observable(''),
        phone: ko.observable(''), email: ko.observable(''),
        address: ko.observable(''),
        maxUsers: ko.observable(5),
        timeZone: ko.observable('Europe/Istanbul'),
        isTest: ko.observable(false),
        testNotes: ko.observable(''),
        products: ko.observableArray([])
    };

    self.productTypes = ko.observableArray([]);
    self.personnel = ko.observableArray([]);
    self.organizations = ko.observableArray([]);
    self.queues = ko.observableArray([]);
    self.billing = ko.observableArray([]);
    self.modules = ko.observableArray([]);
    self.moduleRequests = ko.observableArray([]);

    // Default moduller (grupsuz, temel paket)
    self.defaultModules = ko.computed(function () {
        return (self.modules() || []).filter(function (m) { return m.isDefault && m.productTypeId === 2; });
    });

    // Salon modulleri grup bazli
    self.salonModuleGroups = ko.computed(function () {
        var all = (self.modules() || []).filter(function (m) { return m.productTypeId === 2 && !m.isDefault; });
        var grouped = {};
        all.forEach(function (m) {
            var gId = m.groupId || 0;
            var gName = m.groupName || 'Diger';
            var gIcon = 'bi-puzzle';
            if (!grouped[gId]) grouped[gId] = { groupId: gId, groupName: gName, groupIcon: gIcon, modules: [], activeCount: 0 };
            grouped[gId].modules.push(m);
            if (m.isActive) grouped[gId].activeCount++;
        });
        return Object.values(grouped).sort(function (a, b) { return a.groupId - b.groupId; });
    });

    self.moduleTotalPrice = ko.computed(function () {
        var total = 0;
        (self.modules() || []).forEach(function (m) {
            if (m && m.isActive && !m.isDefault) total += (m.effectivePrice || 0);
        });
        return total.toLocaleString('tr-TR');
    });

    // Urun checkbox/fiyat yonetimi
    self.isProductActive = function (productTypeId) {
        var prod = self.edit.products().find(function(p) { return p.productTypeId === productTypeId; });
        return prod ? prod.active : ko.observable(false);
    };
    self.getProductPrice = function (productTypeId) {
        var prod = self.edit.products().find(function(p) { return p.productTypeId === productTypeId; });
        return prod ? prod.monthlyPrice : ko.observable(0);
    };

    self.loadProductTypes = function (onDone) {
        $.get('/proxy/management/product-types', function (d) {
            var types = Array.isArray(d) ? d : [];
            self.productTypes(types);
            if (typeof onDone === 'function') onDone();
        }).fail(function () {
            self.productTypes([]);
            toastr.warning('Urun tipleri yuklenemedi.');
            if (typeof onDone === 'function') onDone();
        });
    };

    self.loadCustomer = function () {
        $.get('/proxy/customers/' + CUSTOMER_ID, function (c) {
            self.customer(c);
            self.edit.name(c.name || '');
            self.edit.taxNumber(c.taxNumber || '');
            self.edit.phone(c.contactPhone || c.phone || '');
            self.edit.email(c.contactEmail || c.email || '');
            self.edit.address(c.address || '');
            self.edit.maxUsers(c.maxUsers || 5);
            self.edit.timeZone(c.timeZone || 'Europe/Istanbul');
            self.edit.isTest(c.isTest || false);
            self.edit.testNotes(c.testNotes || '');

            // Products bilgisini isle
            var apiProducts = c.products || [];
            var types = self.productTypes();
            self.edit.products(types.map(function(t) {
                var existing = apiProducts.find(function(p) { return p.productTypeId === t.id; });
                return {
                    productTypeId: t.id,
                    active: ko.observable(!!existing),
                    monthlyPrice: ko.observable(existing ? existing.monthlyPrice : 0)
                };
            }));
        });
    };

    self.loadTabs = function () {
        $.get('/proxy/portal/personnel?customerId=' + CUSTOMER_ID, function (d) {
            self.personnel(Array.isArray(d) ? d : (d.items || d.data || []));
        });
        $.get('/proxy/organizations?customerId=' + CUSTOMER_ID, function (d) {
            self.organizations(Array.isArray(d) ? d : (d.items || d.data || []));
        });
        $.get('/proxy/queues?customerId=' + CUSTOMER_ID, function (d) {
            self.queues(Array.isArray(d) ? d : (d.items || d.data || []));
        });
        $.get('/proxy/customers/' + CUSTOMER_ID + '/billing', function (d) {
            self.billing(Array.isArray(d) ? d : (d.items || d.data || []));
        });
        $.get('/proxy/customers/' + CUSTOMER_ID + '/modules', function (d) {
            self.modules(Array.isArray(d) ? d : (d.items || d.data || []));
        });
        self.loadModuleRequests();
    };

    self.saveGeneral = function () {
        var activeProducts = self.edit.products().filter(function(p) { return p.active(); });
        $.ajax({
            url: '/proxy/customers/' + CUSTOMER_ID, method: 'PUT',
            contentType: 'application/json',
            data: JSON.stringify({
                name: self.edit.name(), taxNumber: self.edit.taxNumber(),
                contactPhone: self.edit.phone(), contactEmail: self.edit.email(),
                address: self.edit.address(),
                maxUsers: parseInt(self.edit.maxUsers()),
                timeZone: self.edit.timeZone(),
                isTest: self.edit.isTest(),
                testNotes: self.edit.testNotes(),
                products: activeProducts.map(function(p) {
                    return { productTypeId: p.productTypeId, monthlyPrice: parseFloat(p.monthlyPrice()) || 0 };
                })
            }),
            success: function () { toastr.success('Kaydedildi.'); self.loadCustomer(); },
            error: function () { toastr.error('Kaydetme hatasi.'); }
        });
    };

    self.resetAdminPassword = function () {
        $.ajax({
            url: '/proxy/customers/' + CUSTOMER_ID + '/reset-admin-password', method: 'POST',
            success: function (data) {
                self.newPassword(data.newPassword || data.password || '???');
                toastr.success('Sifre sifirlandi.');
            },
            error: function () { toastr.error('Sifre sifirlama hatasi.'); }
        });
    };

    self.toggleGroup = function (groupId, activate) {
        var group = self.salonModuleGroups().find(function (g) { return g.groupId === groupId; });
        if (!group) return;
        var moduleIds = group.modules.map(function (m) { return m.id; });

        if (activate) {
            // Toplu aktif et
            $.ajax({
                url: '/proxy/customers/' + CUSTOMER_ID + '/modules/assign',
                method: 'POST',
                contentType: 'application/json',
                data: JSON.stringify({ moduleIds: moduleIds, notes: group.groupName + ' grubu toplu aktif' }),
                success: function () { toastr.success(group.groupName + ' grubu aktif edildi.'); self.loadTabs(); },
                error: function () { toastr.error('Islem hatasi.'); }
            });
        } else {
            // Toplu deaktif et
            $.ajax({
                url: '/proxy/customers/' + CUSTOMER_ID + '/modules/deactivate-bulk',
                method: 'POST',
                contentType: 'application/json',
                data: JSON.stringify(moduleIds),
                success: function () { toastr.success(group.groupName + ' grubu kapatildi.'); self.loadTabs(); },
                error: function () { toastr.error('Islem hatasi.'); }
            });
        }
    };

    self.syncModules = function () {
        $.ajax({
            url: '/proxy/customers/' + CUSTOMER_ID + '/modules/sync',
            method: 'POST',
            success: function (data) {
                var count = data.addedCount || 0;
                if (count > 0) {
                    toastr.success(count + ' eksik modul eklendi.');
                } else {
                    toastr.info('Tum moduller zaten mevcut.');
                }
                self.loadTabs();
            },
            error: function () { toastr.error('Senkronizasyon hatasi.'); }
        });
    };

    self.activateAllDefaults = function () {
        // Core (IsDefault=true) modul ID leri — SalonPortalModules ile eslestirilmeli
        var defaultIds = [201, 202, 203, 204, 206, 207, 209, 213, 214, 215, 220, 228];
        $.ajax({
            url: '/proxy/customers/' + CUSTOMER_ID + '/modules/assign',
            method: 'POST',
            contentType: 'application/json',
            data: JSON.stringify({ moduleIds: defaultIds, notes: 'Default moduller toplu aktif edildi' }),
            success: function () {
                toastr.success('Default moduller aktif edildi.');
                self.loadTabs();
            },
            error: function () { toastr.error('Islem hatasi.'); }
        });
    };

    self.toggleModule = function (mod) {
        var action = mod.isActive ? 'deactivate' : 'activate';
        var url = '/proxy/customers/' + CUSTOMER_ID + '/modules/' + (mod.moduleId || mod.id) + '/' + action;
        $.ajax({
            url: url, method: 'POST',
            success: function () { toastr.success('Modul guncellendi.'); self.loadTabs(); },
            error: function () { toastr.error('Islem hatasi.'); }
        });
    };

    self.loadModuleRequests = function () {
        $.get('/proxy/management/module-requests', function (d) {
            var all = Array.isArray(d) ? d : [];
            // Sadece bu musterinin taleplerini filtrele
            self.moduleRequests(all.filter(function (r) { return r.customerId === CUSTOMER_ID; }));
        }).fail(function () { self.moduleRequests([]); });
    };

    self.approveRequest = function (req) {
        confirmModal('Talep Onayi', 'Bu talebi onaylamak istiyor musunuz?', function () {
            confirmModal('Admin Notu', 'Admin notu girin (opsiyonel):', function (notes) {
                $.ajax({
                    url: '/proxy/sln-module-requests/' + req.id + '/approve',
                    method: 'POST',
                    contentType: 'application/json',
                    data: JSON.stringify({ notes: notes || null }),
                    success: function () { toastr.success('Talep onaylandi.'); self.loadTabs(); self.loadModuleRequests(); },
                    error: function () { toastr.error('Onay hatasi.'); }
                });
            }, { input: true, inputLabel: 'Admin notu (opsiyonel)', confirmText: 'Onayla', confirmClass: 'btn-success' });
        }, { confirmText: 'Devam Et', confirmClass: 'btn-success' });
    };

    self.rejectRequest = function (req) {
        confirmModal('Talep Reddi', 'Bu talebi reddetmek istiyor musunuz?', function () {
            confirmModal('Red Sebebi', 'Red sebebi girin:', function (notes) {
                $.ajax({
                    url: '/proxy/sln-module-requests/' + req.id + '/reject',
                    method: 'POST',
                    contentType: 'application/json',
                    data: JSON.stringify({ notes: notes || null }),
                    success: function () { toastr.success('Talep reddedildi.'); self.loadModuleRequests(); },
                    error: function () { toastr.error('Red hatasi.'); }
                });
            }, { input: true, inputLabel: 'Red sebebi', confirmText: 'Reddet', confirmClass: 'btn-danger' });
        }, { confirmText: 'Devam Et', confirmClass: 'btn-danger' });
    };

    // Urun tipleri olmadan musteri satirlari olusturulamaz; yoksa Salon/ CC tiklari bos kalir (yaris kosulu).
    self.loadProductTypes(function () {
        self.loadCustomer();
        self.loadTabs();
    });
}

ko.applyBindings(new DetailViewModel(), document.getElementById('detail-vm'));
