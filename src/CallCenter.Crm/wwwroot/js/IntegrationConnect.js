function IntegrationConnectViewModel() {
    var self = this;
    var root = document.getElementById('connect-vm');
    var platformStr = root ? (root.getAttribute('data-platform') || '') : '';

    self.platformName = ko.observable(platformStr);
    self.platformInfo = ko.observable({ systemName: platformStr, description: '', icon: '', cssClass: 'bg-secondary' });
    self.isSaving = ko.observable(false);

    self.form = {
        name: ko.observable(''),
        autoLogCalls: ko.observable(true),
        contactSyncDirectionId: ko.observable('0'),
        // Zendesk
        subdomain: ko.observable(''),
        email: ko.observable(''),
        apiToken: ko.observable('')
    };

    self.isOAuthPlatform = ko.computed(function () {
        var p = self.platformName();
        return p === 'Salesforce' || p === 'HubSpot';
    });

    self.loadPlatformInfo = function () {
        $.ajax({ url: '/proxy/integrations/platforms', method: 'GET' }).done(function (data) {
            var found = data.find(function (p) { return p.systemName === platformStr; });
            if (found) {
                self.platformInfo(found);
                if (!self.form.name()) {
                    self.form.name(found.description || found.systemName);
                }
            }
        });
    };

    self.connectDirect = function () {
        var name = self.form.name();
        if (!name) { toastr.warning('Baglanti adi zorunludur'); return; }

        var credentials = {};
        if (self.platformName() === 'Zendesk') {
            if (!self.form.subdomain() || !self.form.email() || !self.form.apiToken()) {
                toastr.warning('Zendesk icin subdomain, e-posta ve API token zorunludur');
                return;
            }
            credentials = {
                subdomain: self.form.subdomain(),
                email: self.form.email(),
                apiToken: self.form.apiToken()
            };
        }

        var data = {
            name: name,
            platformTypeId: self.platformInfo().id,
            credentials: Object.keys(credentials).length > 0 ? credentials : null,
            autoLogCalls: self.form.autoLogCalls(),
            contactSyncDirectionId: parseInt(self.form.contactSyncDirectionId()) || 0
        };

        self.isSaving(true);
        $.ajax({
            url: '/proxy/integrations/connections',
            method: 'POST',
            contentType: 'application/json',
            data: JSON.stringify(data)
        }).done(function () {
            toastr.success('Baglanti olusturuldu');
            window.location.href = '/Integrations';
        }).fail(function (xhr) {
            toastr.error(xhr.responseJSON?.message || 'Baglanti olusturulamadi');
            self.isSaving(false);
        });
    };

    self.connectWithOAuth = function () {
        var name = self.form.name();
        if (!name) { toastr.warning('Baglanti adi zorunludur'); return; }

        // Oncelikle baglanti kaydini olustur
        var data = {
            name: name,
            platformTypeId: self.platformInfo().id,
            autoLogCalls: self.form.autoLogCalls(),
            contactSyncDirectionId: parseInt(self.form.contactSyncDirectionId()) || 0
        };

        self.isSaving(true);
        $.ajax({
            url: '/proxy/integrations/connections',
            method: 'POST',
            contentType: 'application/json',
            data: JSON.stringify(data)
        }).done(function () {
            // Sonra OAuth2 akisini baslat
            var returnUrl = window.location.origin + '/Integrations';
            $.ajax({
                url: '/proxy/integrations/oauth/initiate?platformTypeId=' + self.platformInfo().id + '&returnUrl=' + encodeURIComponent(returnUrl),
                method: 'POST'
            }).done(function (result) {
                if (result.authorizationUrl) {
                    window.location.href = result.authorizationUrl;
                } else {
                    toastr.error('OAuth2 URL alinamadi');
                    self.isSaving(false);
                }
            }).fail(function (xhr) {
                toastr.error(xhr.responseJSON?.message || 'OAuth2 baslatma hatasi');
                self.isSaving(false);
            });
        }).fail(function (xhr) {
            toastr.error(xhr.responseJSON?.message || 'Baglanti olusturulamadi');
            self.isSaving(false);
        });
    };

    $(document).ready(function () {
        self.loadPlatformInfo();
    });
}

(function () {
    var root = document.getElementById('connect-vm');
    if (!root || typeof ko === 'undefined') return;
    ko.applyBindings(new IntegrationConnectViewModel(), root);
})();
