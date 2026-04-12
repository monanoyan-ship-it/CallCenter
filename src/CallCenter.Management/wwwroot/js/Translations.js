function TranslationsViewModel() {
    var self = this;
    self.items = ko.observableArray([]);
    self.isLoading = ko.observable(false);
    self.isSaving = ko.observable(false);
    self.searchText = ko.observable('');
    self.moduleFilter = ko.observable('');
    self.currentPage = ko.observable(1);
    self.totalCount = ko.observable(0);
    self.pageSize = 20;
    self.formTitle = ko.observable('Yeni Ceviri');
    self.deleteId = null;

    self.form = {
        id: ko.observable(null),
        key: ko.observable(''),
        module: ko.observable(''),
        description: ko.observable(''),
        tr: ko.observable(''),
        en: ko.observable('')
    };

    self.pageNumbers = ko.computed(function() {
        var total = Math.ceil(self.totalCount() / self.pageSize);
        var pages = [];
        for (var i = 1; i <= total; i++) pages.push(i);
        return pages;
    });

    self.goToPage = function(page) { self.currentPage(page); self.loadData(); };

    self.onSearchKeyUp = function(d, e) { if (e.keyCode === 13) { self.currentPage(1); self.loadData(); } return true; };

    self.loadData = function() {
        self.isLoading(true);
        var params = { page: self.currentPage(), pageSize: self.pageSize };
        if (self.searchText()) params.search = self.searchText();
        if (self.moduleFilter()) params.module = self.moduleFilter();
        $.get('/proxy/translations/keys', params, function(data) {
            var items = Array.isArray(data) ? data : (data.items || data.data || []);
            self.items(items);
            self.totalCount(data.totalCount || data.total || items.length);
        }).always(function() { self.isLoading(false); });
    };

    self.loadModules = function() {
        $.get('/proxy/translations/languages', function(data) {
            var langs = Array.isArray(data) ? data : (data.items || data.data || []);
            // modules populated from keys data
        });
    };

    self.resetForm = function() {
        self.form.id(null); self.form.key(''); self.form.module('');
        self.form.description(''); self.form.tr(''); self.form.en('');
    };

    self.openCreate = function() {
        self.resetForm();
        self.formTitle('Yeni Ceviri');
        new bootstrap.Modal('#translationModal').show();
    };

    self.openEdit = function(item) {
        self.form.id(item.id);
        self.form.key(item.key || '');
        self.form.module(item.module || '');
        self.form.description(item.description || '');
        self.form.tr(item.values && item.values['tr'] || '');
        self.form.en(item.values && item.values['en'] || '');
        self.formTitle('Ceviri Duzenle');
        new bootstrap.Modal('#translationModal').show();
    };

    self.save = function() {
        if (!self.form.key()) { toastr.warning('Key zorunludur.'); return; }
        self.isSaving(true);
        var payload = {
            key: self.form.key(), module: self.form.module(),
            description: self.form.description(),
            values: { tr: self.form.tr(), en: self.form.en() }
        };
        var method = self.form.id() ? 'PUT' : 'POST';
        var url = self.form.id() ? '/proxy/translations/keys/' + self.form.id() : '/proxy/translations/keys';
        $.ajax({
            url: url, method: method, contentType: 'application/json',
            data: JSON.stringify(payload),
            success: function() {
                toastr.success('Kaydedildi.'); bootstrap.Modal.getInstance(document.getElementById('translationModal')).hide();
                self.loadData();
            },
            error: function() { toastr.error('Kaydetme hatasi.'); }
        }).always(function() { self.isSaving(false); });
    };

    self.confirmDelete = function(item) {
        self.deleteId = item.id;
        new bootstrap.Modal('#deleteModal').show();
    };

    self.executeDelete = function() {
        $.ajax({
            url: '/proxy/translations/keys/' + self.deleteId, method: 'DELETE',
            success: function() { toastr.success('Silindi.'); bootstrap.Modal.getInstance(document.getElementById('deleteModal')).hide(); self.loadData(); },
            error: function() { toastr.error('Silme hatasi.'); }
        });
    };

    self.exportXml = function() {
        window.open('/proxy/translations/export/xml', '_blank');
    };

    self.reloadCache = function() {
        $.ajax({
            url: '/proxy/translations/reload-cache', method: 'POST',
            success: function() { toastr.success('Cache yenilendi.'); },
            error: function() { toastr.error('Cache yenileme hatasi.'); }
        });
    };

    self.importXml = function() {
        var input = document.getElementById('xmlImportFile');
        input.onchange = function() {
            if (!input.files.length) return;
            var formData = new FormData();
            formData.append('file', input.files[0]);
            $.ajax({
                url: '/proxy/translations/import/xml', method: 'POST',
                data: formData, processData: false, contentType: false,
                success: function(data) {
                    toastr.success(data.message || 'Import basarili.');
                    self.loadData(); self.reloadCache();
                },
                error: function(xhr) {
                    var msg = xhr.responseJSON && xhr.responseJSON.message || 'Import hatasi.';
                    toastr.error(msg);
                }
            });
            input.value = '';
        };
        input.click();
    };

    self.loadData();
    self.loadModules();
}

ko.applyBindings(new TranslationsViewModel(), document.getElementById('translations-vm'));
