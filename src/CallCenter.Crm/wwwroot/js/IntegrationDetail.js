function IntegrationDetailViewModel() {
    var self = this;
    var root = document.getElementById('detail-vm');
    var connectionUid = root ? (root.getAttribute('data-connection-uid') || '') : '';

    self.isLoading = ko.observable(true);
    self.isSaving = ko.observable(false);
    self.isTesting = ko.observable(false);
    self.conn = ko.observable({});
    self.createdApiKey = ko.observable('');
    self.availableEventTypes = ko.observableArray([]);
    self.availableScopes = ko.observableArray([
        'calls:read', 'calls:write', 'contacts:read', 'contacts:write',
        'agents:read', 'tickets:write'
    ]);

    // Edit form
    self.editForm = {
        name: ko.observable(''),
        autoLogCalls: ko.observable(true),
        contactSyncDirectionId: ko.observable('0')
    };

    // Webhook form
    self.webhookForm = {
        name: ko.observable(''),
        targetUrl: ko.observable(''),
        selectedEvents: ko.observableArray([])
    };

    // API Key form
    self.apiKeyForm = {
        name: ko.observable(''),
        selectedScopes: ko.observableArray([])
    };

    var webhookModal, apiKeyModal, apiKeyCreatedModal;

    self.loadConnection = function () {
        $.ajax({
            url: '/proxy/integrations/connections/' + connectionUid,
            method: 'GET'
        }).done(function (data) {
            self.conn(data);
            self.editForm.name(data.name || '');
            self.editForm.autoLogCalls(data.autoLogCalls || false);
            self.editForm.contactSyncDirectionId(String(data.contactSyncDirectionId || 0));
            self.isLoading(false);
        }).fail(function () {
            toastr.error('Baglanti detayi yuklenemedi');
            self.isLoading(false);
        });
    };

    self.loadEventTypes = function () {
        $.ajax({ url: '/proxy/integrations/event-types', method: 'GET' }).done(function (data) {
            self.availableEventTypes(data);
        });
    };

    self.testConnection = function () {
        self.isTesting(true);
        $.ajax({
            url: '/proxy/integrations/connections/' + connectionUid + '/test',
            method: 'POST'
        }).done(function (result) {
            if (result.success) {
                toastr.success('Baglanti basarili' + (result.platformInfo ? ': ' + result.platformInfo : ''));
            } else {
                toastr.error('Baglanti basarisiz: ' + (result.error || 'Bilinmeyen hata'));
            }
            self.isTesting(false);
            self.loadConnection();
        }).fail(function () {
            toastr.error('Test sirasinda hata olustu');
            self.isTesting(false);
        });
    };

    self.toggleActive = function () {
        var current = self.conn();
        self.isSaving(true);
        $.ajax({
            url: '/proxy/integrations/connections/' + connectionUid,
            method: 'PUT',
            contentType: 'application/json',
            data: JSON.stringify({ isActive: !current.isActive })
        }).done(function () {
            toastr.success(current.isActive ? 'Baglanti pasife alindi' : 'Baglanti aktif edildi');
            self.loadConnection();
            self.isSaving(false);
        }).fail(function () {
            toastr.error('Guncelleme basarisiz');
            self.isSaving(false);
        });
    };

    self.saveSettings = function () {
        self.isSaving(true);
        $.ajax({
            url: '/proxy/integrations/connections/' + connectionUid,
            method: 'PUT',
            contentType: 'application/json',
            data: JSON.stringify({
                name: self.editForm.name(),
                autoLogCalls: self.editForm.autoLogCalls(),
                contactSyncDirectionId: parseInt(self.editForm.contactSyncDirectionId()) || 0
            })
        }).done(function () {
            toastr.success('Ayarlar kaydedildi');
            self.loadConnection();
            self.isSaving(false);
        }).fail(function (xhr) {
            toastr.error(xhr.responseJSON?.message || 'Kaydetme basarisiz');
            self.isSaving(false);
        });
    };

    // ─── Webhook ───

    self.openNewWebhook = function () {
        self.webhookForm.name('');
        self.webhookForm.targetUrl('');
        self.webhookForm.selectedEvents([]);
        webhookModal.show();
    };

    self.saveWebhook = function () {
        var name = self.webhookForm.name();
        var url = self.webhookForm.targetUrl();
        if (!name || !url) { toastr.warning('Ad ve URL zorunludur'); return; }

        self.isSaving(true);
        $.ajax({
            url: '/proxy/integrations/webhooks',
            method: 'POST',
            contentType: 'application/json',
            data: JSON.stringify({
                name: name,
                targetUrl: url,
                eventFilter: self.webhookForm.selectedEvents().length > 0 ? self.webhookForm.selectedEvents() : null,
                integrationConnectionId: self.conn().id
            })
        }).done(function () {
            webhookModal.hide();
            toastr.success('Webhook olusturuldu');
            self.loadConnection();
            self.isSaving(false);
        }).fail(function (xhr) {
            toastr.error(xhr.responseJSON?.message || 'Webhook olusturulamadi');
            self.isSaving(false);
        });
    };

    self.testWebhook = function (wh) {
        toastr.info('Test webhook gonderiliyor...');
        $.ajax({
            url: '/proxy/integrations/webhooks/' + wh.uid + '/test',
            method: 'POST'
        }).done(function () {
            toastr.success('Test webhook gonderildi');
        }).fail(function (xhr) {
            toastr.error(xhr.responseJSON?.message || 'Test basarisiz');
        });
    };

    self.removeWebhook = function (wh) {
        confirmModal('Webhook Sil', wh.name + ' webhook\'unu silmek istediginize emin misiniz?', function () {
            $.ajax({
                url: '/proxy/integrations/webhooks/' + wh.uid,
                method: 'DELETE'
            }).done(function () {
                toastr.success('Webhook silindi');
                self.loadConnection();
            }).fail(function () {
                toastr.error('Silinemedi');
            });
        });
    };

    // ─── API Key ───

    self.openNewApiKey = function () {
        self.apiKeyForm.name('');
        self.apiKeyForm.selectedScopes([]);
        apiKeyModal.show();
    };

    self.createApiKey = function () {
        var name = self.apiKeyForm.name();
        if (!name) { toastr.warning('API Key adi zorunludur'); return; }

        self.isSaving(true);
        $.ajax({
            url: '/proxy/integrations/api-keys',
            method: 'POST',
            contentType: 'application/json',
            data: JSON.stringify({
                name: name,
                integrationConnectionId: self.conn().id,
                scopes: self.apiKeyForm.selectedScopes()
            })
        }).done(function (result) {
            apiKeyModal.hide();
            self.createdApiKey(result.apiKey);
            apiKeyCreatedModal.show();
            self.isSaving(false);
        }).fail(function (xhr) {
            toastr.error(xhr.responseJSON?.message || 'API Key olusturulamadi');
            self.isSaving(false);
        });
    };

    self.revokeApiKey = function (key) {
        confirmModal('API Key Iptal', key.name + ' API key\'ini iptal etmek istediginize emin misiniz?', function () {
            $.ajax({
                url: '/proxy/integrations/api-keys/' + key.uid,
                method: 'DELETE'
            }).done(function () {
                toastr.success('API key iptal edildi');
                self.loadConnection();
            }).fail(function () {
                toastr.error('Iptal edilemedi');
            });
        });
    };

    self.copyApiKey = function () {
        var key = self.createdApiKey();
        if (navigator.clipboard) {
            navigator.clipboard.writeText(key).then(function () {
                toastr.success('API key kopyalandi');
            });
        } else {
            // Fallback
            var el = document.createElement('textarea');
            el.value = key;
            document.body.appendChild(el);
            el.select();
            document.execCommand('copy');
            document.body.removeChild(el);
            toastr.success('API key kopyalandi');
        }
    };

    self.formatDate = function (dateStr) {
        if (!dateStr) return '-';
        var d = new Date(dateStr);
        var pad = function (n) { return n < 10 ? '0' + n : n; };
        return pad(d.getDate()) + '.' + pad(d.getMonth() + 1) + '.' + d.getFullYear() + ' ' + pad(d.getHours()) + ':' + pad(d.getMinutes());
    };

    $(document).ready(function () {
        webhookModal = new bootstrap.Modal(document.getElementById('webhookModal'));
        apiKeyModal = new bootstrap.Modal(document.getElementById('apiKeyModal'));
        apiKeyCreatedModal = new bootstrap.Modal(document.getElementById('apiKeyCreatedModal'));
        self.loadConnection();
        self.loadEventTypes();
    });
}

(function () {
    var root = document.getElementById('detail-vm');
    if (!root || typeof ko === 'undefined') return;
    ko.applyBindings(new IntegrationDetailViewModel(), root);
})();
