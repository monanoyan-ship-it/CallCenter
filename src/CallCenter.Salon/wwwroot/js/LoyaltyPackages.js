function slnJsT(key, fallback) {
    return (window.salonT || function (k, f) { return f || k; })(key, fallback);
}

function PackagesViewModel() {
    var self = this;
    self.definitions = ko.observableArray([]);
    self.clientPackages = ko.observableArray([]);
    self.usageHistory = ko.observableArray([]);
    self.serviceList = ko.observableArray([]);

    self.isEditingDef = ko.observable(false);
    self.editingDefId = ko.observable(null);
    self.usingPackage = ko.observable(null);
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

    var defModal, useSessionModal;

    function normalizeList(data) {
        if (typeof data === 'string' && data.trim()) {
            try {
                data = JSON.parse(data);
            } catch {
                return [];
            }
        }
        if (Array.isArray(data)) return data;
        if (data && Array.isArray(data.items)) return data.items;
        return [];
    }

    function normalizeServiceLookups(data) {
        var list = normalizeList(data);
        var flat = [];
        list.forEach(function (item) {
            if (Array.isArray(item.services)) {
                item.services.forEach(function (svc) {
                    svc.categoryId = svc.categoryId || item.id;
                    svc.categoryName = svc.categoryName || item.name;
                    flat.push(svc);
                });
            } else {
                flat.push(item);
            }
        });

        var seen = {};
        return flat.filter(function (svc) {
            var id = parseInt(svc.id, 10) || 0;
            if (!id || seen[id]) return false;
            seen[id] = true;
            return svc.isActive !== false;
        }).sort(function (a, b) {
            return (a.name || '').localeCompare(b.name || '', document.documentElement.lang || undefined);
        });
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
                || (pkg.offerName || '').toLowerCase().indexOf(query) >= 0
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
        $.ajax({ url: '/proxy/sln-loyalty-packages/offers', method: 'GET' }).done(function (data) {
            self.definitions(normalizeList(data));
        });
        $.ajax({ url: '/proxy/sln-loyalty-packages/purchases', method: 'GET' }).done(function (data) {
            self.clientPackages(normalizeList(data));
            refreshSelectedPackage(selectedId);
        });
    };

    self.loadLookups = function () {
        $.ajax({ url: '/proxy/sln-services', method: 'GET' })
            .done(function (data) {
                var services = normalizeServiceLookups(data);
                if (services.length > 0) {
                    self.serviceList(services);
                    return;
                }

                $.ajax({ url: '/proxy/sln-services/categories', method: 'GET' }).done(function (categoryData) {
                    self.serviceList(normalizeServiceLookups(categoryData));
                });
            })
            .fail(function () {
                $.ajax({ url: '/proxy/sln-services/categories', method: 'GET' }).done(function (categoryData) {
                    self.serviceList(normalizeServiceLookups(categoryData));
                });
            });
    };

    self.loadUsageHistory = function (pkg) {
        if (!pkg) {
            self.usageHistory([]);
            return;
        }

        self.isLoadingUsage(true);
        $.ajax({ url: '/proxy/sln-loyalty-packages/redemptions?purchaseId=' + pkg.id, method: 'GET' }).done(function (data) {
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
            toastr.warning(slnJsT('salon.session_plans.js.definition_and_service_required', 'Seans tanimi ve hizmet zorunludur'));
            return;
        }
        if (data.totalSessions <= 0) {
            toastr.warning(slnJsT('salon.services.package_sessions_required', 'Seans sayisi 0dan buyuk olmalidir'));
            return;
        }

        self.isSaving(true);
        var url = '/proxy/sln-loyalty-packages/offers';
        var method = 'POST';
        if (self.isEditingDef()) {
            url += '/' + self.editingDefId();
            method = 'PUT';
        }

        $.ajax({ url: url, method: method, contentType: 'application/json', data: JSON.stringify(data) }).done(function () {
            defModal.hide();
            self.loadData();
            toastr.success(slnJsT('salon.session_plans.js.definition_saved', 'Seans tanimi kaydedildi'));
        }).fail(function (xhr) {
            toastr.error(readError(xhr, slnJsT('salon.session_plans.js.definition_save_failed', 'Seans tanimi kaydedilemedi')));
        }).always(function () {
            self.isSaving(false);
        });
    };

    self.removeDef = function (def) {
        confirmModal(slnJsT('salon.common.btn.confirm', 'Onayla'), slnJsT('salon.session_plans.js.delete_def_confirm', "'{name}' seans tanimini silmek istediginize emin misiniz?").replace('{name}', def.name || ''), function () {
            $.ajax({ url: '/proxy/sln-loyalty-packages/offers/' + def.id, method: 'DELETE' }).done(function () {
                self.loadData();
                toastr.success(slnJsT('salon.session_plans.js.definition_deleted', 'Seans tanimi silindi'));
            }).fail(function (xhr) {
                toastr.error(readError(xhr, slnJsT('salon.session_plans.js.definition_delete_failed', 'Seans tanimi silinemedi')));
            });
        });
    };

    self.openUseSession = function (pkg) {
        var target = pkg || self.selectedClientPackage();
        if (!target || !target.isActive || target.remainingSessions <= 0) return;
        self.usingPackage(target);
        self.manualUseNotes('');
        if (!useSessionModal) {
            toastr.error(slnJsT('salon.common.reload_required', 'Sayfa güncellenmeli. Lütfen sayfayı yenileyin.'));
            return;
        }
        useSessionModal.show();
    };

    self.confirmUseSession = function () {
        var target = self.usingPackage();
        if (!target || !target.isActive || target.remainingSessions <= 0) return;

        self.isSaving(true);
        $.ajax({
            url: '/proxy/sln-loyalty-packages/redeem',
            method: 'POST',
            contentType: 'application/json',
            data: JSON.stringify({
                purchaseId: target.id,
                notes: (self.manualUseNotes() || slnJsT('salon.session_plans.manual_use_note', 'Manuel seans kullanimi')).trim()
            })
        }).done(function () {
            useSessionModal.hide();
            self.manualUseNotes('');
            self.usingPackage(null);
            self.loadData(target.id);
            toastr.success(slnJsT('salon.packages.js.session_used', '1 seans kullanildi'));
        }).fail(function (xhr) {
            toastr.error(readError(xhr, slnJsT('salon.packages.js.session_use_failed', 'Seans kullanimi kaydedilemedi')));
        }).always(function () {
            self.isSaving(false);
        });
    };

    self.useSession = self.openUseSession;

    $(document).ready(function () {
        defModal = new bootstrap.Modal(document.getElementById('defModal'));
        var useSessionEl = document.getElementById('useSessionModal');
        if (useSessionEl) useSessionModal = new bootstrap.Modal(useSessionEl);
        self.loadLookups();
        self.loadData();
    });
}

ko.applyBindings(new PackagesViewModel(), document.getElementById('packages-vm'));
