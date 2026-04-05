function EmailSettingsViewModel() {
    var self = this;
    self.integrations = ko.observableArray([]);
    self.isLoading = ko.observable(false);
    self.isSaving = ko.observable(false);
    self.isTesting = ko.observable(false);
    self.testEmail = ko.observable('');
    self._testUid = null;

    // ═══ Yandex Form ═══
    self.yandexForm = {
        email: ko.observable(''),
        appPassword: ko.observable(''),
        senderName: ko.observable(''),
        isDefault: ko.observable(false)
    };

    // ═══ SMTP Form ═══
    self.smtpForm = {
        host: ko.observable(''),
        port: ko.observable('587'),
        username: ko.observable(''),
        password: ko.observable(''),
        senderEmail: ko.observable(''),
        senderName: ko.observable(''),
        useSsl: ko.observable(true),
        isDefault: ko.observable(false)
    };

    var yandexModal, smtpModal, testModal;

    // ═══ Veri Yukleme ═══
    self.loadData = function () {
        self.isLoading(true);
        $.get('/proxy/sln-email-integrations').done(function (data) {
            self.integrations(Array.isArray(data) ? data : (data.items || []));
        }).always(function () { self.isLoading(false); });
    };

    // ═══ OAuth Baglanti ═══
    self.connectGmail = function () {
        $.get('/proxy/sln-email-integrations/gmail/auth-url').done(function (data) {
            if (data.authUrl) window.location.href = data.authUrl;
            else toastr.error(data.error || 'Gmail OAuth yapilandirilmamis.');
        }).fail(function (xhr) {
            toastr.error(xhr.responseJSON || 'Gmail OAuth hatasi.');
        });
    };

    self.connectOffice365 = function () {
        $.get('/proxy/sln-email-integrations/office365/auth-url').done(function (data) {
            if (data.authUrl) window.location.href = data.authUrl;
            else toastr.error(data.error || 'Office365 OAuth yapilandirilmamis.');
        }).fail(function (xhr) {
            toastr.error(xhr.responseJSON || 'Office365 OAuth hatasi.');
        });
    };

    // ═══ Yandex ═══
    self.openYandexModal = function () {
        self.yandexForm.email('');
        self.yandexForm.appPassword('');
        self.yandexForm.senderName('');
        self.yandexForm.isDefault(false);
        yandexModal.show();
    };

    self.saveYandex = function () {
        var email = self.yandexForm.email();
        var pass = self.yandexForm.appPassword();
        if (!email || !pass) { toastr.warning('E-posta ve uygulama sifresi zorunludur.'); return; }

        self.isSaving(true);
        $.ajax({
            url: '/proxy/sln-email-integrations',
            method: 'POST',
            contentType: 'application/json',
            data: JSON.stringify({
                providerTypeId: 3,
                displayName: 'Yandex (' + email + ')',
                senderEmail: email,
                senderName: self.yandexForm.senderName() || null,
                isDefault: self.yandexForm.isDefault(),
                credentials: { Email: email, AppPassword: pass }
            })
        }).done(function () {
            yandexModal.hide();
            toastr.success('Yandex hesabi eklendi.');
            self.loadData();
        }).fail(function (xhr) {
            toastr.error(xhr.responseJSON || 'Hata olustu.');
        }).always(function () { self.isSaving(false); });
    };

    // ═══ SMTP ═══
    self.openSmtpModal = function () {
        self.smtpForm.host('');
        self.smtpForm.port('587');
        self.smtpForm.username('');
        self.smtpForm.password('');
        self.smtpForm.senderEmail('');
        self.smtpForm.senderName('');
        self.smtpForm.useSsl(true);
        self.smtpForm.isDefault(false);
        smtpModal.show();
    };

    self.saveSmtp = function () {
        var host = self.smtpForm.host();
        var username = self.smtpForm.username();
        var password = self.smtpForm.password();
        if (!host || !username || !password) { toastr.warning('Sunucu, kullanici adi ve sifre zorunludur.'); return; }

        var senderEmail = self.smtpForm.senderEmail() || username;
        self.isSaving(true);
        $.ajax({
            url: '/proxy/sln-email-integrations',
            method: 'POST',
            contentType: 'application/json',
            data: JSON.stringify({
                providerTypeId: 4,
                displayName: 'SMTP (' + senderEmail + ')',
                senderEmail: senderEmail,
                senderName: self.smtpForm.senderName() || null,
                isDefault: self.smtpForm.isDefault(),
                credentials: {
                    Host: host,
                    Port: self.smtpForm.port() || '587',
                    Username: username,
                    Password: password,
                    UseSsl: self.smtpForm.useSsl() ? 'true' : 'false'
                }
            })
        }).done(function () {
            smtpModal.hide();
            toastr.success('SMTP hesabi eklendi.');
            self.loadData();
        }).fail(function (xhr) {
            toastr.error(xhr.responseJSON || 'Hata olustu.');
        }).always(function () { self.isSaving(false); });
    };

    // ═══ Islemler ═══
    self.testSend = function (item) {
        self._testUid = item.uid;
        self.testEmail('');
        testModal.show();
    };

    self.confirmTestSend = function () {
        var email = self.testEmail();
        if (!email) { toastr.warning('Alici e-posta zorunludur.'); return; }

        self.isTesting(true);
        $.ajax({
            url: '/proxy/sln-email-integrations/' + self._testUid + '/test',
            method: 'POST',
            contentType: 'application/json',
            data: JSON.stringify({ toAddress: email })
        }).done(function (result) {
            testModal.hide();
            if (result.success) toastr.success('Test e-postasi gonderildi!');
            else toastr.error(result.error || 'Gonderim basarisiz.');
            self.loadData();
        }).fail(function () {
            toastr.error('Bir hata olustu.');
        }).always(function () { self.isTesting(false); });
    };

    self.setDefault = function (item) {
        $.ajax({
            url: '/proxy/sln-email-integrations/' + item.uid,
            method: 'PUT',
            contentType: 'application/json',
            data: JSON.stringify({ isDefault: true })
        }).done(function () {
            toastr.success('Varsayilan olarak ayarlandi.');
            self.loadData();
        });
    };

    self.remove = function (item) {
        if (!confirm(item.senderEmail + ' hesabini kaldirmak istediginize emin misiniz?')) return;
        $.ajax({
            url: '/proxy/sln-email-integrations/' + item.uid,
            method: 'DELETE'
        }).done(function () {
            toastr.success('Hesap kaldirildi.');
            self.loadData();
        });
    };

    // ═══ OAuth Callback Handling ═══
    function handleOAuthCallback() {
        var params = new URLSearchParams(window.location.search);
        var provider = params.get('provider');
        var code = params.get('code');
        var error = params.get('error');

        if (error) {
            toastr.error('OAuth hatasi: ' + error);
            history.replaceState(null, '', window.location.pathname);
            return;
        }

        if (provider && code) {
            toastr.info(provider.toUpperCase() + ' hesabi baglaniyor...');
            $.ajax({
                url: '/proxy/sln-email-integrations/' + provider + '/exchange-code',
                method: 'POST',
                contentType: 'application/json',
                data: JSON.stringify({ code: code, isDefault: true })
            }).done(function (result) {
                if (result.success) {
                    toastr.success(result.email + ' hesabi basariyla baglandi!');
                    self.loadData();
                } else {
                    toastr.error(result.error || 'Baglanti hatasi.');
                }
            }).fail(function () {
                toastr.error('Baglanti sirasinda hata olustu.');
            });
            history.replaceState(null, '', window.location.pathname);
        }
    }

    // ═══ Init ═══
    $(document).ready(function () {
        yandexModal = new bootstrap.Modal(document.getElementById('yandexModal'));
        smtpModal = new bootstrap.Modal(document.getElementById('smtpModal'));
        testModal = new bootstrap.Modal(document.getElementById('testModal'));
        self.loadData();
        handleOAuthCallback();
    });
}

ko.applyBindings(new EmailSettingsViewModel(), document.getElementById('emailsettings-vm'));
