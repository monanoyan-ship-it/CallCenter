function slnJsT(key, fallback) {
    return (window.salonT || function (k, f) { return f || k; })(key, fallback);
}

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
        if (!email || !pass) { toastr.warning(slnJsT('salon.emailsettings.js.e_posta_ve_uygulama_sifresi_zorunludur', 'E-posta ve uygulama sifresi zorunludur.')); return; }

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
            toastr.success(slnJsT('salon.emailsettings.js.yandex_hesabi_eklendi', 'Yandex hesabi eklendi.'));
            self.loadData();
        }).fail(function (xhr) {
            toastr.error(xhr.responseJSON || slnJsT('salon.common.error.generic', 'Hata oluştu.'));
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
        if (!host || !username || !password) { toastr.warning(slnJsT('salon.emailsettings.js.sunucu_kullanici_adi_ve_sifre_zorunludur', 'Sunucu, kullanici adı ve sifre zorunludur.')); return; }

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
            toastr.success(slnJsT('salon.emailsettings.js.smtp_hesabi_eklendi', 'SMTP hesabi eklendi.'));
            self.loadData();
        }).fail(function (xhr) {
            toastr.error(xhr.responseJSON || slnJsT('salon.common.error.generic', 'Hata oluştu.'));
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
        if (!email) { toastr.warning(slnJsT('salon.emailsettings.js.alici_e_posta_zorunludur', 'Alici e-posta zorunludur.')); return; }

        self.isTesting(true);
        $.ajax({
            url: '/proxy/sln-email-integrations/' + self._testUid + '/test',
            method: 'POST',
            contentType: 'application/json',
            data: JSON.stringify({ toAddress: email })
        }).done(function (result) {
            testModal.hide();
            if (result.success) toastr.success(slnJsT('salon.emailsettings.js.test_email_sent', 'Test e-postası gönderildi!'));
            else toastr.error(result.error || slnJsT('salon.emailsettings.js.gonderim_basarisiz', 'Gonderim basarisiz.'));
            self.loadData();
        }).fail(function () {
            toastr.error(slnJsT('salon.emailsettings.js.bir_hata_olustu', 'Bir hata oluştu.'));
        }).always(function () { self.isTesting(false); });
    };

    self.setDefault = function (item) {
        $.ajax({
            url: '/proxy/sln-email-integrations/' + item.uid,
            method: 'PUT',
            contentType: 'application/json',
            data: JSON.stringify({ isDefault: true })
        }).done(function () {
            toastr.success(slnJsT('salon.emailsettings.js.set_default_success', 'Varsayılan olarak ayarlandı.'));
            self.loadData();
        });
    };

    self.remove = function (item) {
        confirmModal(slnJsT('salon.common.btn.confirm', 'Onayla'), item.senderEmail + slnJsT('salon.emailsettings.js.hesabini_kaldirmak_istediginize_emin_misiniz', ' hesabını kaldırmak istediğinize emin misiniz?'), function() {
            $.ajax({
                url: '/proxy/sln-email-integrations/' + item.uid,
                method: 'DELETE'
            }).done(function () {
                toastr.success(slnJsT('salon.emailsettings.js.account_removed', 'Hesap kaldırıldı.'));
                self.loadData();
            });
        });
    };

    // ═══ OAuth Callback Handling ═══
    function handleOAuthCallback() {
        var params = new URLSearchParams(window.location.search);
        var provider = params.get('provider');
        var code = params.get('code');
        var error = params.get('error');

        if (error) {
            toastr.error(slnJsT('salon.emailsettings.js.oauth_error_prefix', 'OAuth hatası: ') + error);
            history.replaceState(null, '', window.location.pathname);
            return;
        }

        if (provider && code) {
            toastr.info(slnJsT('salon.emailsettings.js.provider_connecting', '{provider} hesabı bağlanıyor...').replace('{provider}', provider.toUpperCase()));
            $.ajax({
                url: '/proxy/sln-email-integrations/' + provider + '/exchange-code',
                method: 'POST',
                contentType: 'application/json',
                data: JSON.stringify({ code: code, isDefault: true })
            }).done(function (result) {
                if (result.success) {
                    toastr.success(slnJsT('salon.emailsettings.js.provider_connected', '{email} hesabı başarıyla bağlandı!').replace('{email}', result.email));
                    self.loadData();
                } else {
                    toastr.error(result.error || slnJsT('salon.emailsettings.js.baglanti_hatasi', 'Baglanti hatasi.'));
                }
            }).fail(function () {
                toastr.error(slnJsT('salon.emailsettings.js.baglanti_sirasinda_hata_olustu', 'Bağlantı sırasında hata oluştu.'));
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
