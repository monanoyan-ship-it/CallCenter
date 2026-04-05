function EmailTemplatesViewModel() {
    var self = this;

    self.templates = ko.observableArray([]);
    self.isLoading = ko.observable(false);
    self.searchText = ko.observable('');
    self.filterProduct = ko.observable('');

    self.form = {
        id: ko.observable(null),
        eventKey: ko.observable(''),
        productType: ko.observable(''),
        subject: ko.observable(''),
        description: ko.observable(''),
        availablePlaceholders: ko.observable(''),
        language: ko.observable('tr'),
        isActive: ko.observable(true)
    };

    self.previewSubject = ko.observable('');
    self.previewHtml = ko.observable('');
    self.deleteTarget = ko.observable({ eventKey: '' });

    var tinymceEditor = null;

    // Filtered list
    self.filteredTemplates = ko.computed(function () {
        var search = (self.searchText() || '').toLowerCase();
        var product = self.filterProduct();
        return self.templates().filter(function (t) {
            var matchSearch = !search ||
                t.eventKey.toLowerCase().indexOf(search) >= 0 ||
                t.subject.toLowerCase().indexOf(search) >= 0;
            var matchProduct = !product ||
                (product && t.productType === product) ||
                (!t.productType && !product);
            return matchSearch && matchProduct;
        });
    });

    // Load
    self.loadData = function () {
        self.isLoading(true);
        $.get('/proxy/platform-email-templates', function (data) {
            self.templates(data || []);
        }).fail(function () {
            toastr.error('Taslaklar yuklenemedi');
        }).always(function () {
            self.isLoading(false);
        });
    };

    // Init TinyMCE
    function initEditor(content) {
        if (tinymceEditor) {
            tinymceEditor.destroy();
            tinymceEditor = null;
        }
        tinymce.init({
            selector: '#htmlEditor',
            height: 350,
            menubar: false,
            plugins: 'lists link image code table hr preview',
            toolbar: 'undo redo | blocks | bold italic underline | forecolor backcolor | alignleft aligncenter alignright | bullist numlist | link image table hr | code preview',
            content_style: 'body { font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, sans-serif; font-size: 14px; }',
            setup: function (editor) {
                tinymceEditor = editor;
                editor.on('init', function () {
                    editor.setContent(content || '');
                });
            }
        });
    }

    // Reset form
    function resetForm() {
        self.form.id(null);
        self.form.eventKey('');
        self.form.productType('');
        self.form.subject('');
        self.form.description('');
        self.form.availablePlaceholders('');
        self.form.language('tr');
        self.form.isActive(true);
    }

    // Open Create
    self.openCreate = function () {
        resetForm();
        var modal = new bootstrap.Modal(document.getElementById('templateModal'));
        modal.show();
        setTimeout(function () { initEditor(''); }, 300);
    };

    // Open Edit
    self.openEdit = function (item) {
        self.form.id(item.id);
        self.form.eventKey(item.eventKey);
        self.form.productType(item.productType || '');
        self.form.subject(item.subject);
        self.form.description(item.description || '');
        self.form.availablePlaceholders(item.availablePlaceholders || '');
        self.form.language(item.language);
        self.form.isActive(item.isActive);
        var modal = new bootstrap.Modal(document.getElementById('templateModal'));
        modal.show();
        setTimeout(function () { initEditor(item.htmlBody || ''); }, 300);
    };

    // Save (Create or Update)
    self.save = function () {
        var htmlBody = tinymceEditor ? tinymceEditor.getContent() : '';
        var isEdit = !!self.form.id();

        if (!self.form.eventKey() || !self.form.subject() || !htmlBody) {
            toastr.warning('Olay anahtari, konu ve HTML icerik zorunludur');
            return;
        }

        var payload = isEdit ? {
            productType: self.form.productType() || null,
            subject: self.form.subject(),
            htmlBody: htmlBody,
            isActive: self.form.isActive(),
            description: self.form.description() || null,
            availablePlaceholders: self.form.availablePlaceholders() || null,
            language: self.form.language()
        } : {
            eventKey: self.form.eventKey(),
            productType: self.form.productType() || null,
            subject: self.form.subject(),
            htmlBody: htmlBody,
            isActive: self.form.isActive(),
            description: self.form.description() || null,
            availablePlaceholders: self.form.availablePlaceholders() || null,
            language: self.form.language()
        };

        $.ajax({
            url: isEdit ? '/proxy/platform-email-templates/' + self.form.id() : '/proxy/platform-email-templates',
            method: isEdit ? 'PUT' : 'POST',
            contentType: 'application/json',
            data: JSON.stringify(payload),
            success: function () {
                toastr.success(isEdit ? 'Taslak guncellendi' : 'Taslak olusturuldu');
                bootstrap.Modal.getInstance(document.getElementById('templateModal')).hide();
                self.loadData();
            },
            error: function (xhr) {
                toastr.error(xhr.responseText || 'Islem basarisiz');
            }
        });
    };

    // Preview
    self.openPreview = function (item) {
        self.previewSubject(item.subject);
        self.previewHtml(item.htmlBody);
        new bootstrap.Modal(document.getElementById('previewModal')).show();
    };

    // Delete
    self.confirmDelete = function (item) {
        self.deleteTarget(item);
        new bootstrap.Modal(document.getElementById('deleteModal')).show();
    };

    self.executeDelete = function () {
        var id = self.deleteTarget().id;
        $.ajax({
            url: '/proxy/platform-email-templates/' + id,
            method: 'DELETE',
            success: function () {
                toastr.success('Taslak silindi');
                bootstrap.Modal.getInstance(document.getElementById('deleteModal')).hide();
                self.loadData();
            },
            error: function () {
                toastr.error('Silme basarisiz');
            }
        });
    };

    // Init
    self.loadData();
}

ko.applyBindings(new EmailTemplatesViewModel(), document.getElementById('email-templates-vm'));
