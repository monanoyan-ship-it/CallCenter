function dataImportT(key, fallback) {
    return (window.salonT || function (k, f) { return f || k; })(key, fallback);
}

function DataImportViewModel() {
    var self = this;

    self.templates = ko.observableArray([]);
    self.branches = ko.observableArray([]);
    self.importType = ko.observable('');
    self.branchId = ko.observable('');
    self.file = ko.observable(null);
    self.previewResult = ko.observable(null);
    self.rows = ko.observableArray([]);
    self.isBusy = ko.observable(false);

    function prop(obj, camel, pascal, fallback) {
        if (!obj) return fallback;
        if (Object.prototype.hasOwnProperty.call(obj, camel)) return obj[camel];
        if (Object.prototype.hasOwnProperty.call(obj, pascal)) return obj[pascal];
        return fallback;
    }

    function normalizeTemplate(t) {
        return {
            importType: prop(t, 'importType', 'ImportType', ''),
            displayName: prop(t, 'displayName', 'DisplayName', ''),
            columns: (prop(t, 'columns', 'Columns', []) || []).map(function (c) {
                return {
                    key: prop(c, 'key', 'Key', ''),
                    label: prop(c, 'label', 'Label', ''),
                    required: !!prop(c, 'required', 'Required', false),
                    description: prop(c, 'description', 'Description', '')
                };
            })
        };
    }

    function normalizeBranch(b) {
        return {
            id: prop(b, 'id', 'Id', ''),
            name: prop(b, 'name', 'Name', '')
        };
    }

    function normalizeRow(row) {
        var normalized = prop(row, 'normalized', 'Normalized', {}) || {};
        var pairs = Object.keys(normalized)
            .filter(function (key) { return normalized[key] !== null && normalized[key] !== undefined && normalized[key] !== ''; })
            .map(function (key) { return { key: key, value: normalized[key] }; });

        return {
            rowNumber: prop(row, 'rowNumber', 'RowNumber', ''),
            status: prop(row, 'status', 'Status', ''),
            message: prop(row, 'message', 'Message', ''),
            normalizedPairs: pairs
        };
    }

    self.selectedTemplate = ko.pureComputed(function () {
        var type = self.importType();
        return self.templates().find(function (t) { return t.importType === type; }) || null;
    });

    self.detectedColumns = ko.pureComputed(function () {
        var result = self.previewResult();
        return result ? prop(result, 'detectedColumns', 'DetectedColumns', []) || [] : [];
    });

    self.previewFileName = ko.pureComputed(function () {
        var result = self.previewResult();
        return result ? prop(result, 'fileName', 'FileName', '') : '';
    });

    self.canPreview = ko.pureComputed(function () {
        return !!self.importType() && !!self.file() && !self.isBusy();
    });

    self.canCommit = ko.pureComputed(function () {
        var result = self.previewResult();
        return !!result && !!self.file() && !self.isBusy() && Number(prop(result, 'readyRows', 'ReadyRows', 0)) > 0;
    });

    self.rowCountText = ko.pureComputed(function () {
        return self.rows().length
            ? dataImportT('salon.dataimport.js.row_count', '{count} satir').replace('{count}', self.rows().length)
            : '';
    });

    self.summaryValue = function (key) {
        var result = self.previewResult();
        if (!result) return '0';
        var pascal = key.charAt(0).toUpperCase() + key.slice(1);
        return String(prop(result, key, pascal, 0) || 0);
    };

    self.statusText = function (status) {
        var map = {
            ready: dataImportT('salon.dataimport.status.ready', 'Hazir'),
            imported: dataImportT('salon.dataimport.status.imported', 'Aktarildi'),
            duplicate: dataImportT('salon.dataimport.status.duplicate', 'Tekrar'),
            error: dataImportT('salon.dataimport.status.error', 'Hata'),
            skipped: dataImportT('salon.dataimport.status.skipped', 'Atlandi'),
            warning: dataImportT('salon.dataimport.status.warning', 'Uyari')
        };
        return map[status] || status || '-';
    };

    self.statusClass = function (status) {
        var map = {
            ready: 'text-bg-primary',
            imported: 'text-bg-success',
            duplicate: 'text-bg-warning',
            error: 'text-bg-danger',
            skipped: 'text-bg-secondary',
            warning: 'text-bg-info'
        };
        return map[status] || 'text-bg-light text-dark';
    };

    self.onFileChange = function (_, event) {
        var selected = event.target.files && event.target.files.length ? event.target.files[0] : null;
        self.file(selected);
        self.previewResult(null);
        self.rows([]);
    };

    function buildFormData() {
        var form = new FormData();
        form.append('importType', self.importType());
        if (self.branchId()) form.append('branchId', self.branchId());
        form.append('file', self.file());
        return form;
    }

    function applyResult(result) {
        self.previewResult(result);
        var rawRows = prop(result, 'rows', 'Rows', []) || [];
        self.rows(rawRows.map(normalizeRow));
    }

    function showRequestError(xhr, fallback) {
        var json = xhr && xhr.responseJSON;
        var message = json && (json.message || json.Message);
        if (!message && xhr && xhr.responseText) message = xhr.responseText;
        toastr.error(message || fallback);
    }

    self.loadLookups = function () {
        $.get('/proxy/sln-data-import/templates')
            .done(function (data) { self.templates((data || []).map(normalizeTemplate)); })
            .fail(function (xhr) { showRequestError(xhr, dataImportT('salon.dataimport.js.templates_failed', 'Import sablonlari yuklenemedi')); });

        $.get('/proxy/sln-branches')
            .done(function (data) { self.branches((data || []).map(normalizeBranch)); })
            .fail(function () { self.branches([]); });
    };

    self.downloadTemplate = function () {
        if (!self.importType()) {
            toastr.warning(dataImportT('salon.dataimport.js.select_type', 'Once veri tipi secin'));
            return;
        }
        window.location.href = '/proxy/sln-data-import/template/' + encodeURIComponent(self.importType());
    };

    self.preview = function () {
        if (!self.canPreview()) {
            toastr.warning(dataImportT('salon.dataimport.js.select_file', 'Dosya ve veri tipi secin'));
            return;
        }

        self.isBusy(true);
        $.ajax({
            url: '/proxy/sln-data-import/preview',
            method: 'POST',
            data: buildFormData(),
            processData: false,
            contentType: false
        }).done(function (result) {
            applyResult(result);
            toastr.success(dataImportT('salon.dataimport.js.preview_success', 'Onizleme hazir'));
        }).fail(function (xhr) {
            showRequestError(xhr, dataImportT('salon.dataimport.js.preview_failed', 'Onizleme yapilamadi'));
        }).always(function () {
            self.isBusy(false);
        });
    };

    self.commit = function () {
        if (!self.canCommit()) return;

        var ready = self.summaryValue('readyRows');
        var message = dataImportT('salon.dataimport.js.confirm_import', '{count} hazir satir aktarilsin mi?').replace('{count}', ready);
        confirmModal(
            dataImportT('salon.dataimport.js.confirm_title', 'Importu onayla'),
            message,
            function () {
                self.isBusy(true);
                $.ajax({
                    url: '/proxy/sln-data-import/commit',
                    method: 'POST',
                    data: buildFormData(),
                    processData: false,
                    contentType: false
                }).done(function (result) {
                    applyResult(result);
                    toastr.success(dataImportT('salon.dataimport.js.import_success', 'Veriler aktarildi'));
                }).fail(function (xhr) {
                    showRequestError(xhr, dataImportT('salon.dataimport.js.import_failed', 'Veriler aktarilamadi'));
                }).always(function () {
                    self.isBusy(false);
                });
            },
            { confirmClass: 'btn-success', confirmText: dataImportT('salon.dataimport.commit', 'Aktar') }
        );
    };
}

$(function () {
    var el = document.getElementById('data-import-vm');
    if (!el) return;
    var vm = new DataImportViewModel();
    ko.applyBindings(vm, el);
    vm.loadLookups();
});
