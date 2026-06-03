function EmailTemplatesViewModel() {
    var self = this;

    self.events = ko.observableArray([]);
    self.isLoading = ko.observable(false);
    self.isSavingEvent = ko.observable(false);
    self.isSendingTest = ko.observable(false);
    self.searchText = ko.observable('');
    self.filterProduct = ko.observable('');

    self.eventForm = {
        id: ko.observable(null),
        eventKey: ko.observable(''),
        productType: ko.observable(''),
        description: ko.observable(''),
        availablePlaceholders: ko.observable(''),
        isActive: ko.observable(true)
    };

    self.currentEvent = ko.observable({ eventKey: '', description: '', availablePlaceholders: '' });
    self.templateTabs = ko.observableArray([]);
    self.activeTab = ko.observable('');
    self.templateForm = {
        id: ko.observable(null),
        subject: ko.observable(''),
        isActive: ko.observable(true)
    };

    self.previewSubject = ko.observable('');
    self.newLanguage = ko.observable('en');
    self.deleteTarget = ko.observable({ eventKey: '', description: '' });
    self.placeholderRows = ko.observableArray([]);
    self.testSendForm = {
        toEmail: ko.observable(''),
        toName: ko.observable('')
    };

    var tinymceEditor = null;

    function normalizeEventDescription(item) {
        var description = item && item.description ? item.description : '';
        switch (description) {
            case 'Kullanici email dogrulama maili':
                return 'Kullanıcı e-posta doğrulama';
            case 'Sifre sifirlama maili':
                return 'Şifre sıfırlama';
            case 'Salon musteri (PlatformUser) email dogrulama maili':
                return 'Salon müşterisi e-posta doğrulama';
            case 'Salon musteri (PlatformUser) sifre sifirlama maili':
                return 'Salon müşterisi şifre sıfırlama';
            case 'Kullanıcı e-posta doğrulama maili':
                return 'Kullanıcı e-posta doğrulama';
            case 'Şifre sıfırlama maili':
                return 'Şifre sıfırlama';
            case 'Salon müşterisi (PlatformUser) e-posta doğrulama maili':
                return 'Salon müşterisi e-posta doğrulama';
            case 'Salon müşterisi (PlatformUser) şifre sıfırlama maili':
                return 'Salon müşterisi şifre sıfırlama';
            default:
                return cleanupDescription(description) || (item ? item.eventKey : '');
        }
    }

    function cleanupDescription(description) {
        return String(description || '')
            .replace(/\s*\(PlatformUser\)\s*/g, ' ')
            .replace(/\bmaili\b/gi, '')
            .replace(/\s+/g, ' ')
            .trim();
    }

    function parsePlaceholders(value) {
        if (!value) return [];
        var text = String(value).trim();
        var parsed = null;

        try {
            parsed = JSON.parse(text);
        } catch (e) {
            parsed = null;
        }

        if (Array.isArray(parsed)) {
            return uniqueStrings(parsed);
        }

        if (parsed && typeof parsed === 'object') {
            return uniqueStrings(Object.keys(parsed));
        }

        return uniqueStrings(text
            .split(/[\n,;]/)
            .map(function (part) {
                return part.replace(/[{}\[\]"']/g, '').trim();
            }));
    }

    function uniqueStrings(values) {
        var seen = {};
        return (values || [])
            .map(function (value) { return String(value || '').trim(); })
            .filter(function (value) {
                if (!value || seen[value]) return false;
                seen[value] = true;
                return true;
            });
    }

    function sampleValue(key) {
        var lower = key.toLocaleLowerCase('tr-TR');
        if (lower.indexOf('link') >= 0 || lower.indexOf('url') >= 0) return 'https://corplynk.com/ornek';
        if (lower.indexOf('mail') >= 0 || lower.indexOf('email') >= 0) return self.testSendForm.toEmail() || 'test@example.com';
        if (lower.indexOf('ad') >= 0 || lower.indexOf('name') >= 0) return self.testSendForm.toName() || 'Test Müşteri';
        if (lower.indexOf('kod') >= 0 || lower.indexOf('code') >= 0) return '123456';
        if (lower.indexOf('tarih') >= 0 || lower.indexOf('date') >= 0) return '03.06.2026';
        return 'Örnek ' + key;
    }

    function getEditorHtml() {
        var fallbackEditor = document.getElementById('htmlEditor');
        return tinymceEditor ? tinymceEditor.getContent() : (fallbackEditor ? fallbackEditor.value : '');
    }

    function writePreviewFrame(htmlBody) {
        var frame = document.getElementById('templatePreviewFrame');
        if (!frame) return;
        frame.srcdoc = '<!doctype html><html><head><meta charset="utf-8">' +
            '<style>body{font-family:-apple-system,BlinkMacSystemFont,"Segoe UI",Roboto,sans-serif;font-size:14px;line-height:1.5;color:#1f2937;padding:24px;margin:0;} img{max-width:100%;height:auto;} table{max-width:100%;border-collapse:collapse;}</style>' +
            '</head><body>' + (htmlBody || '<p>Önizlenecek içerik yok.</p>') + '</body></html>';
    }

    function initEditor(content) {
        if (tinymceEditor) {
            tinymceEditor.destroy();
            tinymceEditor = null;
        }

        var fallbackEditor = document.getElementById('htmlEditor');
        if (fallbackEditor) fallbackEditor.value = content || '';
        if (!window.tinymce) return;

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

    function selectedTemplate() {
        var lang = self.activeTab();
        var tab = self.templateTabs().find(function (item) { return item.lang === lang; });
        return tab ? tab.template : null;
    }

    function refreshCurrentEvent(eventId) {
        $.get('/proxy/platform-email-templates/' + eventId, function (data) {
            self.currentEvent(data);
            var tabs = (data.templates || []).map(function (t) { return { lang: t.language, template: t }; });
            self.templateTabs(tabs);

            if (tabs.length > 0) {
                var currentLang = self.activeTab();
                var matchTab = tabs.find(function (t) { return t.lang === currentLang; });
                self.selectTab(matchTab || tabs[0]);
            } else {
                self.activeTab('');
            }

            self.loadData();
        });
    }

    self.eventTitle = normalizeEventDescription;
    self.productLabel = function (item) { return item.productType || 'Tümü'; };
    self.placeholderList = ko.pureComputed(function () {
        return parsePlaceholders(self.currentEvent().availablePlaceholders);
    });
    self.currentEventTitle = ko.pureComputed(function () {
        return normalizeEventDescription(self.currentEvent());
    });

    self.filteredEvents = ko.pureComputed(function () {
        var search = (self.searchText() || '').toLocaleLowerCase('tr-TR');
        var product = self.filterProduct();

        return self.events().filter(function (eventItem) {
            var description = normalizeEventDescription(eventItem).toLocaleLowerCase('tr-TR');
            var eventKey = (eventItem.eventKey || '').toLocaleLowerCase('tr-TR');
            var matchSearch = !search || description.indexOf(search) >= 0 || eventKey.indexOf(search) >= 0;
            var matchProduct = !product || eventItem.productType === product;
            return matchSearch && matchProduct;
        });
    });

    self.loadData = function () {
        self.isLoading(true);
        $.get('/proxy/platform-email-templates', function (data) {
            self.events(data || []);
        }).fail(function () {
            toastr.error('E-posta türleri yüklenemedi.');
        }).always(function () {
            self.isLoading(false);
        });
    };

    self.openCreateEvent = function () {
        self.eventForm.id(null);
        self.eventForm.eventKey('');
        self.eventForm.productType('');
        self.eventForm.description('');
        self.eventForm.availablePlaceholders('');
        self.eventForm.isActive(true);
        bootstrap.Modal.getOrCreateInstance(document.getElementById('eventModal')).show();
    };

    self.openEditEvent = function (item) {
        self.eventForm.id(item.id);
        self.eventForm.eventKey(item.eventKey);
        self.eventForm.productType(item.productType || '');
        self.eventForm.description(normalizeEventDescription(item));
        self.eventForm.availablePlaceholders(item.availablePlaceholders || '');
        self.eventForm.isActive(item.isActive);
        bootstrap.Modal.getOrCreateInstance(document.getElementById('eventModal')).show();
    };

    self.saveEvent = function () {
        var isEdit = !!self.eventForm.id();
        if (!self.eventForm.description()) {
            toastr.warning('E-posta türü adını girin.');
            return;
        }
        if (!isEdit && !self.eventForm.eventKey()) {
            toastr.warning('Teknik anahtar zorunludur.');
            return;
        }

        var payload = {
            productType: self.eventForm.productType() || null,
            description: self.eventForm.description() || null,
            availablePlaceholders: self.eventForm.availablePlaceholders() || null,
            isActive: self.eventForm.isActive()
        };
        if (!isEdit) payload.eventKey = self.eventForm.eventKey();

        self.isSavingEvent(true);
        $.ajax({
            url: isEdit ? '/proxy/platform-email-templates/' + self.eventForm.id() : '/proxy/platform-email-templates',
            method: isEdit ? 'PUT' : 'POST',
            contentType: 'application/json',
            data: JSON.stringify(payload)
        }).done(function () {
            toastr.success(isEdit ? 'E-posta türü güncellendi.' : 'E-posta türü oluşturuldu.');
            bootstrap.Modal.getInstance(document.getElementById('eventModal')).hide();
            self.loadData();
        }).fail(function (xhr) {
            toastr.error(xhr.responseText || 'İşlem başarısız.');
        }).always(function () {
            self.isSavingEvent(false);
        });
    };

    self.confirmDeleteEvent = function (item) {
        self.deleteTarget(item);
        bootstrap.Modal.getOrCreateInstance(document.getElementById('deleteModal')).show();
    };

    self.executeDeleteEvent = function () {
        $.ajax({
            url: '/proxy/platform-email-templates/' + self.deleteTarget().id,
            method: 'DELETE'
        }).done(function () {
            toastr.success('E-posta türü silindi.');
            bootstrap.Modal.getInstance(document.getElementById('deleteModal')).hide();
            self.loadData();
        }).fail(function () {
            toastr.error('Silme işlemi başarısız.');
        });
    };

    self.openTemplates = function (item) {
        self.currentEvent(item);
        var tabs = (item.templates || []).map(function (t) { return { lang: t.language, template: t }; });
        self.templateTabs(tabs);
        self.activeTab('');
        self.templateForm.id(null);
        self.templateForm.subject('');
        self.templateForm.isActive(true);

        bootstrap.Modal.getOrCreateInstance(document.getElementById('templateModal')).show();

        if (tabs.length > 0) {
            setTimeout(function () { self.selectTab(tabs[0]); }, 250);
        } else {
            var defaultTab = { lang: 'tr', template: null };
            self.templateTabs.push(defaultTab);
            setTimeout(function () { self.selectTab(defaultTab); }, 250);
        }
    };

    self.selectTab = function (tab) {
        self.activeTab(tab.lang);
        self.templateForm.id(tab.template ? tab.template.id : null);
        self.templateForm.subject(tab.template ? tab.template.subject : '');
        self.templateForm.isActive(tab.template ? tab.template.isActive : true);
        setTimeout(function () { initEditor(tab.template ? tab.template.htmlBody : ''); }, 150);
    };

    self.addLanguageTab = function () {
        bootstrap.Modal.getOrCreateInstance(document.getElementById('addLangModal')).show();
    };

    self.confirmAddLanguage = function () {
        var lang = self.newLanguage();
        var exists = self.templateTabs().some(function (t) { return t.lang === lang; });
        if (exists) {
            toastr.warning(lang.toUpperCase() + ' dili zaten var.');
            return;
        }

        var newTab = { lang: lang, template: null };
        self.templateTabs.push(newTab);
        bootstrap.Modal.getInstance(document.getElementById('addLangModal')).hide();
        setTimeout(function () { self.selectTab(newTab); }, 150);
    };

    self.openTemplatePreview = function () {
        var htmlBody = getEditorHtml();
        var subject = self.templateForm.subject();
        if (!subject && !htmlBody) {
            toastr.warning('Önizlenecek konu veya içerik girin.');
            return;
        }

        self.previewSubject(subject || 'Konu yok');
        bootstrap.Modal.getOrCreateInstance(document.getElementById('templatePreviewModal')).show();
        setTimeout(function () { writePreviewFrame(htmlBody); }, 100);
    };

    self.saveTemplate = function () {
        var htmlBody = getEditorHtml();
        var subject = self.templateForm.subject();
        if (!subject || !htmlBody) {
            toastr.warning('Konu ve içerik zorunludur.');
            return;
        }

        var templateId = self.templateForm.id();
        var eventId = self.currentEvent().id;
        var payload = {
            subject: subject,
            htmlBody: htmlBody,
            isActive: self.templateForm.isActive()
        };

        if (templateId) {
            $.ajax({
                url: '/proxy/platform-email-templates/templates/' + templateId,
                method: 'PUT',
                contentType: 'application/json',
                data: JSON.stringify(payload)
            }).done(function () {
                toastr.success('Şablon güncellendi.');
                refreshCurrentEvent(eventId);
            }).fail(function (xhr) {
                toastr.error(xhr.responseText || 'Güncelleme başarısız.');
            });
        } else {
            payload.language = self.activeTab();
            $.ajax({
                url: '/proxy/platform-email-templates/' + eventId + '/templates',
                method: 'POST',
                contentType: 'application/json',
                data: JSON.stringify(payload)
            }).done(function (data) {
                toastr.success('Şablon oluşturuldu.');
                self.templateForm.id(data.id);
                refreshCurrentEvent(eventId);
            }).fail(function (xhr) {
                toastr.error(xhr.responseText || 'Oluşturma başarısız.');
            });
        }
    };

    self.deleteTemplate = function () {
        var templateId = self.templateForm.id();
        if (!templateId) return;

        confirmModal('Şablonu Sil', 'Bu dil şablonu silinecek. Emin misiniz?', function () {
            $.ajax({
                url: '/proxy/platform-email-templates/templates/' + templateId,
                method: 'DELETE'
            }).done(function () {
                toastr.success('Şablon silindi.');
                refreshCurrentEvent(self.currentEvent().id);
            }).fail(function () {
                toastr.error('Silme işlemi başarısız.');
            });
        }, { confirmText: 'Sil', confirmClass: 'btn-danger' });
    };

    self.openTestSendModal = function () {
        if (!self.templateForm.id()) {
            toastr.warning('Test göndermek için önce şablonu kaydedin.');
            return;
        }

        var template = selectedTemplate();
        self.testSendForm.toEmail('');
        self.testSendForm.toName('');
        self.placeholderRows(parsePlaceholders(self.currentEvent().availablePlaceholders).map(function (key) {
            return { key: key, value: ko.observable(sampleValue(key)) };
        }));

        if (template && template.subject) self.previewSubject(template.subject);
        bootstrap.Modal.getOrCreateInstance(document.getElementById('templateTestModal')).show();
    };

    self.sendTestTemplate = function () {
        var toEmail = self.testSendForm.toEmail();
        if (!toEmail || toEmail.indexOf('@') < 0) {
            toastr.warning('Geçerli bir alıcı e-posta adresi girin.');
            return;
        }

        var placeholders = {};
        self.placeholderRows().forEach(function (row) {
            placeholders[row.key] = row.value();
        });

        self.isSendingTest(true);
        $.ajax({
            url: '/proxy/platform-email-templates/templates/' + self.templateForm.id() + '/test',
            method: 'POST',
            contentType: 'application/json',
            data: JSON.stringify({
                toEmail: toEmail,
                toName: self.testSendForm.toName() || null,
                placeholders: placeholders
            })
        }).done(function (result) {
            if (result && result.success) {
                toastr.success('Test e-postası gönderildi.');
                bootstrap.Modal.getInstance(document.getElementById('templateTestModal')).hide();
            } else {
                toastr.error((result && result.error) || 'Test e-postası gönderilemedi.');
            }
        }).fail(function (xhr) {
            toastr.error(xhr.responseText || 'Test e-postası gönderilemedi.');
        }).always(function () {
            self.isSendingTest(false);
        });
    };

    self.loadData();
}

ko.applyBindings(new EmailTemplatesViewModel(), document.getElementById('email-templates-vm'));
