function EmailTemplatesViewModel() {
    var self = this;

    // ─── State ───
    self.events = ko.observableArray([]);
    self.isLoading = ko.observable(false);
    self.searchText = ko.observable('');
    self.filterProduct = ko.observable('');

    // Event form
    self.eventForm = {
        id: ko.observable(null),
        eventKey: ko.observable(''),
        productType: ko.observable(''),
        description: ko.observable(''),
        availablePlaceholders: ko.observable(''),
        isActive: ko.observable(true)
    };

    // Template form
    self.currentEvent = ko.observable({ eventKey: '', availablePlaceholders: '' });
    self.templateTabs = ko.observableArray([]);
    self.activeTab = ko.observable('');
    self.templateForm = {
        id: ko.observable(null),
        subject: ko.observable(''),
        isActive: ko.observable(true)
    };
    self.newLanguage = ko.observable('en');

    self.deleteTarget = ko.observable({ eventKey: '' });

    var tinymceEditor = null;

    // ─── Filtered Events ───
    self.filteredEvents = ko.computed(function () {
        var search = (self.searchText() || '').toLowerCase();
        var product = self.filterProduct();
        return self.events().filter(function (e) {
            var matchSearch = !search ||
                e.eventKey.toLowerCase().indexOf(search) >= 0 ||
                (e.description || '').toLowerCase().indexOf(search) >= 0;
            var matchProduct = !product || e.productType === product || (!e.productType && !product);
            return matchSearch && (matchProduct || !product);
        });
    });

    // ─── Load ───
    self.loadData = function () {
        self.isLoading(true);
        $.get('/proxy/platform-email-templates', function (data) {
            self.events(data || []);
        }).fail(function () {
            toastr.error('Olaylar yuklenemedi');
        }).always(function () {
            self.isLoading(false);
        });
    };

    // ─── TinyMCE ───
    function initEditor(content) {
        if (tinymceEditor) { tinymceEditor.destroy(); tinymceEditor = null; }
        tinymce.init({
            selector: '#htmlEditor',
            height: 350,
            menubar: false,
            plugins: 'lists link image code table hr preview',
            toolbar: 'undo redo | blocks | bold italic underline | forecolor backcolor | alignleft aligncenter alignright | bullist numlist | link image table hr | code preview',
            content_style: 'body { font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, sans-serif; font-size: 14px; }',
            setup: function (editor) {
                tinymceEditor = editor;
                editor.on('init', function () { editor.setContent(content || ''); });
            }
        });
    }

    // ─── Event CRUD ───

    self.openCreateEvent = function () {
        self.eventForm.id(null);
        self.eventForm.eventKey('');
        self.eventForm.productType('');
        self.eventForm.description('');
        self.eventForm.availablePlaceholders('');
        self.eventForm.isActive(true);
        new bootstrap.Modal(document.getElementById('eventModal')).show();
    };

    self.openEditEvent = function (item) {
        self.eventForm.id(item.id);
        self.eventForm.eventKey(item.eventKey);
        self.eventForm.productType(item.productType || '');
        self.eventForm.description(item.description || '');
        self.eventForm.availablePlaceholders(item.availablePlaceholders || '');
        self.eventForm.isActive(item.isActive);
        new bootstrap.Modal(document.getElementById('eventModal')).show();
    };

    self.saveEvent = function () {
        var isEdit = !!self.eventForm.id();
        if (!isEdit && !self.eventForm.eventKey()) {
            toastr.warning('Olay anahtari zorunludur');
            return;
        }
        var payload = isEdit ? {
            productType: self.eventForm.productType() || null,
            description: self.eventForm.description() || null,
            availablePlaceholders: self.eventForm.availablePlaceholders() || null,
            isActive: self.eventForm.isActive()
        } : {
            eventKey: self.eventForm.eventKey(),
            productType: self.eventForm.productType() || null,
            description: self.eventForm.description() || null,
            availablePlaceholders: self.eventForm.availablePlaceholders() || null,
            isActive: self.eventForm.isActive()
        };
        $.ajax({
            url: isEdit ? '/proxy/platform-email-templates/' + self.eventForm.id() : '/proxy/platform-email-templates',
            method: isEdit ? 'PUT' : 'POST',
            contentType: 'application/json',
            data: JSON.stringify(payload),
            success: function () {
                toastr.success(isEdit ? 'Olay guncellendi' : 'Olay olusturuldu');
                bootstrap.Modal.getInstance(document.getElementById('eventModal')).hide();
                self.loadData();
            },
            error: function (xhr) { toastr.error(xhr.responseText || 'Islem basarisiz'); }
        });
    };

    self.confirmDeleteEvent = function (item) {
        self.deleteTarget(item);
        new bootstrap.Modal(document.getElementById('deleteModal')).show();
    };

    self.executeDeleteEvent = function () {
        $.ajax({
            url: '/proxy/platform-email-templates/' + self.deleteTarget().id,
            method: 'DELETE',
            success: function () {
                toastr.success('Olay silindi');
                bootstrap.Modal.getInstance(document.getElementById('deleteModal')).hide();
                self.loadData();
            },
            error: function () { toastr.error('Silme basarisiz'); }
        });
    };

    // ─── Template (Dil) Yonetimi ───

    self.openTemplates = function (item) {
        self.currentEvent(item);
        var tabs = (item.templates || []).map(function (t) { return { lang: t.language, template: t }; });
        self.templateTabs(tabs);
        self.activeTab('');
        self.templateForm.id(null);
        self.templateForm.subject('');
        self.templateForm.isActive(true);

        var modal = new bootstrap.Modal(document.getElementById('templateModal'));
        modal.show();

        if (tabs.length > 0) {
            setTimeout(function () { self.selectTab(tabs[0]); }, 300);
        }
    };

    self.selectTab = function (tab) {
        // Mevcut editor icerigini kaydetme (switch oncesi)
        self.activeTab(tab.lang);
        self.templateForm.id(tab.template ? tab.template.id : null);
        self.templateForm.subject(tab.template ? tab.template.subject : '');
        self.templateForm.isActive(tab.template ? tab.template.isActive : true);
        setTimeout(function () { initEditor(tab.template ? tab.template.htmlBody : ''); }, 200);
    };

    self.addLanguageTab = function () {
        new bootstrap.Modal(document.getElementById('addLangModal')).show();
    };

    self.confirmAddLanguage = function () {
        var lang = self.newLanguage();
        var exists = self.templateTabs().some(function (t) { return t.lang === lang; });
        if (exists) {
            toastr.warning(lang.toUpperCase() + ' dili zaten mevcut');
            return;
        }
        var newTab = { lang: lang, template: null };
        self.templateTabs.push(newTab);
        bootstrap.Modal.getInstance(document.getElementById('addLangModal')).hide();
        setTimeout(function () { self.selectTab(newTab); }, 200);
    };

    self.saveTemplate = function () {
        var htmlBody = tinymceEditor ? tinymceEditor.getContent() : '';
        var subject = self.templateForm.subject();
        if (!subject || !htmlBody) {
            toastr.warning('Konu ve HTML icerik zorunludur');
            return;
        }

        var templateId = self.templateForm.id();
        var eventId = self.currentEvent().id;

        if (templateId) {
            // Update
            $.ajax({
                url: '/proxy/platform-email-templates/templates/' + templateId,
                method: 'PUT',
                contentType: 'application/json',
                data: JSON.stringify({ subject: subject, htmlBody: htmlBody, isActive: self.templateForm.isActive() }),
                success: function () {
                    toastr.success('Taslak guncellendi');
                    refreshCurrentEvent(eventId);
                },
                error: function (xhr) { toastr.error(xhr.responseText || 'Guncelleme basarisiz'); }
            });
        } else {
            // Create
            $.ajax({
                url: '/proxy/platform-email-templates/' + eventId + '/templates',
                method: 'POST',
                contentType: 'application/json',
                data: JSON.stringify({ language: self.activeTab(), subject: subject, htmlBody: htmlBody, isActive: self.templateForm.isActive() }),
                success: function (data) {
                    toastr.success('Taslak olusturuldu');
                    self.templateForm.id(data.id);
                    refreshCurrentEvent(eventId);
                },
                error: function (xhr) { toastr.error(xhr.responseText || 'Olusturma basarisiz'); }
            });
        }
    };

    self.deleteTemplate = function () {
        var templateId = self.templateForm.id();
        if (!templateId) return;
        if (!confirm('Bu dil taslagi silinecek. Emin misiniz?')) return;

        $.ajax({
            url: '/proxy/platform-email-templates/templates/' + templateId,
            method: 'DELETE',
            success: function () {
                toastr.success('Dil taslagi silindi');
                var eventId = self.currentEvent().id;
                refreshCurrentEvent(eventId);
            },
            error: function () { toastr.error('Silme basarisiz'); }
        });
    };

    function refreshCurrentEvent(eventId) {
        $.get('/proxy/platform-email-templates/' + eventId, function (data) {
            self.currentEvent(data);
            var tabs = (data.templates || []).map(function (t) { return { lang: t.language, template: t }; });
            self.templateTabs(tabs);
            if (tabs.length > 0) {
                var currentLang = self.activeTab();
                var matchTab = tabs.find(function (t) { return t.lang === currentLang; });
                if (matchTab) self.selectTab(matchTab);
                else self.selectTab(tabs[0]);
            } else {
                self.activeTab('');
            }
            self.loadData();
        });
    }

    // Init
    self.loadData();
}

ko.applyBindings(new EmailTemplatesViewModel(), document.getElementById('email-templates-vm'));
