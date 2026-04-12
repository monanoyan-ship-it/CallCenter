function ModulesViewModel() {
    var self = this;
    self.activeModules = ko.observableArray([]);
    self.availableModules = ko.observableArray([]);
    self.requests = ko.observableArray([]);

    self.defaultModules = ko.computed(function () {
        return self.activeModules().filter(function (m) { return m.isDefault; });
    });

    self.activeGroups = ko.computed(function () {
        var nonDefault = self.activeModules().filter(function (m) { return !m.isDefault && m.isActive; });
        var grouped = {};
        nonDefault.forEach(function (m) {
            var gId = m.groupId || 0;
            var gName = m.groupName || 'Diger';
            if (!grouped[gId]) grouped[gId] = { groupId: gId, groupName: gName, modules: [] };
            grouped[gId].modules.push(m);
        });
        return Object.values(grouped).sort(function (a, b) { return a.groupId - b.groupId; });
    });

    self.availableGroups = ko.computed(function () {
        var all = self.availableModules();
        var grouped = {};
        all.forEach(function (m) {
            var gId = m.groupId || 0;
            var gName = m.groupName || 'Diger';
            if (!grouped[gId]) grouped[gId] = { groupId: gId, groupName: gName, modules: [] };
            grouped[gId].modules.push(m);
        });
        return Object.values(grouped).sort(function (a, b) { return a.groupId - b.groupId; });
    });

    self.monthlyTotal = ko.computed(function () {
        var total = 0;
        self.activeModules().forEach(function (m) {
            if (!m.isDefault && m.isActive) total += (m.effectivePrice || 0);
        });
        return total;
    });

    self.load = function () {
        $.get('/proxy/sln-module-requests', function (data) { self.requests(data || []); });
        $.get('/proxy/sln-module-requests/available', function (data) { self.availableModules(data || []); });
        $.get('/proxy/sln-module-requests/active', function (data) { self.activeModules(data || []); });
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
