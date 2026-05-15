function slnJsT(key, fallback) {
    return (window.salonT || function (k, f) { return f || k; })(key, fallback);
}

function PackagesViewModel() {
    var self = this;
    self.definitions = ko.observableArray([]);
    self.clientPackages = ko.observableArray([]);
    self.usageHistory = ko.observableArray([]);
    self.serviceList = ko.observableArray([]);
    self.clientList = ko.observableArray([]);

    self.isEditingDef = ko.observable(false);
    self.editingDefId = ko.observable(null);
    self.assigningDef = ko.observable(null);
    self.assignClientId = ko.observable(null);
    self.selectedClientPackage = ko.observable(null);
    self.searchQuery = ko.observable('');
    self.selectedServiceId = ko.observable('');
    self.showActiveOnly = ko.observable(true);
    self.manualUseNotes = ko.observable('');
    self.isSaving = ko.observable(false);
    self.isLoadingUsage = ko.observable(false);

    self.defForm = {
        name: ko.observable(''),
        description: ko.observable(''),
        serviceId: ko.observable(null),
        totalSessions: ko.observable(10),
        price: ko.observable(0),
        validDays: ko.observable(365)
    };

    self.clientAutocomplete = createAutocomplete(self.clientList, 'fullName', self.assignClientId);
    self.clientAutocomplete.query.subscribe(function (value) {
        if ((value || '').trim() && value !== self.clientAutocomplete.selectedName()) {
            self.clientAutocomplete.showDropdown(true);
        }
    });

    var defModal, assignModal;

    function normalizeList(data) {
        if (Array.isArray(data)) return data;
        if (data && Array.isArray(data.items)) return data.items;
        return [];
    }

    function sameId(a, b) {
        return parseInt(a) === parseInt(b);
    }

    function readError(xhr, fallback) {
        if (xhr && typeof xhr.responseJSON === 'string' && xhr.responseJSON.trim()) return xhr.responseJSON.trim();
        if (xhr && xhr.responseJSON && typeof xhr.responseJSON === 'object') {
            return xhr.responseJSON.error || xhr.responseJSON.message || xhr.responseJSON.title || fallback;
        }
        return (xhr && xhr.responseText) || fallback || slnJsT('salon.common.error.generic', 'Hata');
    }

    self.formatMoney = function (value) {
        return (parseFloat(value) || 0).toLocaleString(document.documentElement.lang || undefined) + ' TL';
    };

    self.formatDate = function (value) {
        return value ? new Date(value).toLocaleDateString(document.documentElement.lang || undefined) : '-';
    };

    self.formatDateTime = function (value) {
        return value ? new Date(value).toLocaleString(document.documentElement.lang || undefined) : '-';
    };

    self.statusText = function (pkg) {
        if (!pkg) return '-';
        if (!pkg.isActive || (parseInt(pkg.remainingSessions) || 0) <= 0) {
            return slnJsT('salon.packages.status.finished', 'Bitti');
        }
        return slnJsT('salon.common.status_active', 'Aktif');
    };

    self.packageProgress = function (pkg) {
        if (!pkg || !pkg.totalSessions) return 0;
        return Math.min(100, Math.max(0, Math.round(((parseInt(pkg.usedSessions) || 0) / pkg.totalSessions) * 100)));
    };

    self.stats = ko.computed(function () {
        var packages = self.clientPackages();
        var active = packages.filter(function (p) { return p.isActive && p.remainingSessions > 0; });
        return {
            definitions: self.definitions().length,
            assigned: packages.length,
            active: active.length,
            remaining: active.reduce(function (sum, p) { return sum + (parseInt(p.remainingSessions) || 0); }, 0)
        };
    });

    self.filteredClientPackages = ko.computed(function () {
        var query = (self.searchQuery() || '').toLowerCase();
        var serviceId = parseInt(self.selectedServiceId()) || 0;
        return self.clientPackages().filter(function (pkg) {
            if (self.showActiveOnly() && (!pkg.isActive || pkg.remainingSessions <= 0)) return false;
            if (serviceId && !sameId(pkg.serviceId, serviceId)) return false;
            if (!query) return true;
            return (pkg.clientName || '').toLowerCase().indexOf(query) >= 0
                || (pkg.packageName || '').toLowerCase().indexOf(query) >= 0
                || (pkg.serviceName || '').toLowerCase().indexOf(query) >= 0;
        });
    });

    function refreshSelectedPackage(selectedId) {
        var id = selectedId || (self.selectedClientPackage() ? self.selectedClientPackage().id : null);
        if (!id) return;

        var updated = self.clientPackages().find(function (pkg) { return sameId(pkg.id, id); });
        if (updated) {
            self.selectedClientPackage(updated);
            self.loadUsageHistory(updated);
        } else {
            self.selectedClientPackage(null);
            self.usageHistory([]);
        }
    }

    self.loadData = function (selectedId) {
        $.ajax({ url: '/proxy/sln-packages/definitions', method: 'GET' }).done(function (data) {
            self.definitions(normalizeList(data));
        });
        $.ajax({ url: '/proxy/sln-packages/client-packages', method: 'GET' }).done(function (data) {
            self.clientPackages(normalizeList(data));
            refreshSelectedPackage(selectedId);
        });
    };

    self.loadLookups = function () {
        $.ajax({ url: '/proxy/sln-services', method: 'GET' }).done(function (data) {
            self.serviceList(normalizeList(data));
        });
        $.ajax({ url: '/proxy/sln-clients?pageSize=1000', method: 'GET' }).done(function (data) {
            self.clientList(normalizeList(data));
        });
    };

    self.loadUsageHistory = function (pkg) {
        if (!pkg) {
            self.usageHistory([]);
            return;
        }

        self.isLoadingUsage(true);
        $.ajax({ url: '/proxy/sln-packages/usages?clientPackageId=' + pkg.id, method: 'GET' }).done(function (data) {
            self.usageHistory(normalizeList(data));
        }).fail(function (xhr) {
            toastr.error(readError(xhr, slnJsT('salon.packages.js.usage_history_failed', 'Kullanim gecmisi alinamadi')));
        }).always(function () {
            self.isLoadingUsage(false);
        });
    };

    self.selectClientPackage = function (pkg) {
        self.selectedClientPackage(pkg);
        self.manualUseNotes('');
        self.loadUsageHistory(pkg);
    };

    self.openNewDef = function () {
        self.isEditingDef(false);
        self.editingDefId(null);
        self.defForm.name('');
        self.defForm.description('');
        self.defForm.serviceId(null);
        self.defForm.totalSessions(10);
        self.defForm.price(0);
        self.defForm.validDays(365);
        defModal.show();
    };

    self.openEditDef = function (def) {
        self.isEditingDef(true);
        self.editingDefId(def.id);
        self.defForm.name(def.name);
        self.defForm.description(def.description || '');
        self.defForm.serviceId(def.serviceId);
        self.defForm.totalSessions(def.totalSessions);
        self.defForm.price(def.price);
        self.defForm.validDays(def.validDays);
        defModal.show();
    };

    self.saveDef = function () {
        var data = {
            name: (self.defForm.name() || '').trim(),
            description: self.defForm.description(),
            serviceId: parseInt(self.defForm.serviceId()) || 0,
            totalSessions: parseInt(self.defForm.totalSessions()) || 0,
            price: parseFloat(self.defForm.price()) || 0,
            validDays: parseInt(self.defForm.validDays()) || 365,
            isActive: true
        };

        if (!data.name || !data.serviceId) {
            toastr.warning(slnJsT('salon.packages.js.paket_adi_ve_hizmet_zorunludur', 'Paket adi ve hizmet zorunludur'));
            return;
        }
        if (data.totalSessions <= 0) {
            toastr.warning(slnJsT('salon.services.package_sessions_required', 'Seans sayisi 0dan buyuk olmalidir'));
            return;
        }

        self.isSaving(true);
        var url = '/proxy/sln-packages/definitions';
        var method = 'POST';
        if (self.isEditingDef()) {
            url += '/' + self.editingDefId();
            method = 'PUT';
        }

        $.ajax({ url: url, method: method, contentType: 'application/json', data: JSON.stringify(data) }).done(function () {
            defModal.hide();
            self.loadData();
            toastr.success(slnJsT('salon.packages.js.paket_tanimi_kaydedildi', 'Paket tanimi kaydedildi'));
        }).fail(function (xhr) {
            toastr.error(readError(xhr, slnJsT('salon.services.package_save_failed', 'Paket tanimi kaydedilemedi')));
        }).always(function () {
            self.isSaving(false);
        });
    };

    self.removeDef = function (def) {
        confirmModal(slnJsT('salon.common.btn.confirm', 'Onayla'), slnJsT('salon.packages.js.delete_def_confirm', "'{name}' paketini silmek istediginize emin misiniz?").replace('{name}', def.name || ''), function () {
            $.ajax({ url: '/proxy/sln-packages/definitions/' + def.id, method: 'DELETE' }).done(function () {
                self.loadData();
                toastr.success(slnJsT('salon.packages.js.paket_tanimi_silindi', 'Paket tanimi silindi'));
            }).fail(function (xhr) {
                toastr.error(readError(xhr, slnJsT('salon.services.package_delete_failed', 'Paket tanimi silinemedi')));
            });
        });
    };

    self.openAssign = function (def) {
        self.assigningDef(def);
        self.assignClientId(null);
        self.clientAutocomplete.clear();
        assignModal.show();
    };

    self.confirmAssign = function () {
        var def = self.assigningDef();
        if (!def) return;
        if (!self.assignClientId()) {
            toastr.warning(slnJsT('salon.packages.js.paket_atamak_icin_musteri_secilmelidir', 'Paket atamak icin musteri secilmelidir'));
            return;
        }

        var data = {
            packageDefinitionId: def.id,
            slnClientId: parseInt(self.assignClientId())
        };

        self.isSaving(true);
        $.ajax({
            url: '/proxy/sln-packages/assign',
            method: 'POST',
            contentType: 'application/json',
            data: JSON.stringify(data)
        }).done(function (pkg) {
            assignModal.hide();
            self.loadData(pkg && pkg.id);
            toastr.success(slnJsT('salon.packages.js.paket_musteriye_atandi', 'Paket musteriye atandi'));
        }).fail(function (xhr) {
            toastr.error(readError(xhr, slnJsT('salon.services.package_assign_failed', 'Paket atanamadi')));
        }).always(function () {
            self.isSaving(false);
        });
    };

    self.useSession = function (pkg) {
        var target = pkg || self.selectedClientPackage();
        if (!target || !target.isActive || target.remainingSessions <= 0) return;

        confirmModal(slnJsT('salon.common.btn.confirm', 'Onayla'), slnJsT('salon.packages.js.use_session_confirm', '1 seans kullanilacak. Emin misiniz?'), function () {
            self.isSaving(true);
            $.ajax({
                url: '/proxy/sln-packages/use',
                method: 'POST',
                contentType: 'application/json',
                data: JSON.stringify({
                    clientPackageId: target.id,
                    notes: (self.manualUseNotes() || slnJsT('salon.packages.manual_use_note', 'Manuel paket kullanimi')).trim()
                })
            }).done(function () {
                self.manualUseNotes('');
                self.loadData(target.id);
                toastr.success(slnJsT('salon.packages.js.session_used', '1 seans kullanildi'));
            }).fail(function (xhr) {
                toastr.error(readError(xhr, slnJsT('salon.packages.js.session_use_failed', 'Seans kullanimi kaydedilemedi')));
            }).always(function () {
                self.isSaving(false);
            });
        });
    };

    $(document).ready(function () {
        defModal = new bootstrap.Modal(document.getElementById('defModal'));
        assignModal = new bootstrap.Modal(document.getElementById('assignModal'));
        self.loadLookups();
        self.loadData();
    });
}

ko.applyBindings(new PackagesViewModel(), document.getElementById('packages-vm'));
