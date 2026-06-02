function IntegrationsViewModel() {
    var self = this;
    self.connections = ko.observableArray([]);
    self.platforms = ko.observableArray([]);
    self.searchQuery = ko.observable('');

    self.activeCount = ko.computed(function () {
        return self.connections().filter(function (c) { return c.isActive && c.isConnected; }).length;
    });

    self.totalWebhooks = ko.computed(function () {
        var sum = 0;
        self.connections().forEach(function (c) { sum += (c.webhookCount || 0); });
        return sum;
    });

    self.totalApiKeys = ko.computed(function () {
        var sum = 0;
        self.connections().forEach(function (c) { sum += (c.apiKeyCount || 0); });
        return sum;
    });

    self.filteredConnections = ko.computed(function () {
        var q = (self.searchQuery() || '').toLowerCase();
        if (!q) return self.connections();
        return self.connections().filter(function (c) {
            return (c.name || '').toLowerCase().indexOf(q) >= 0
                || (c.platformName || '').toLowerCase().indexOf(q) >= 0;
        });
    });

    self.loadPlatforms = function () {
        $.ajax({ url: '/proxy/integrations/platforms', method: 'GET' }).done(function (data) {
            // Her platforma isConnected bilgisi ekle
            var connectedPlatformIds = {};
            self.connections().forEach(function (c) { connectedPlatformIds[c.platformTypeId] = true; });
            data.forEach(function (p) { p.isConnected = !!connectedPlatformIds[p.id]; });
            self.platforms(data);
        });
    };

    self.loadConnections = function () {
        $.ajax({ url: '/proxy/integrations/connections', method: 'GET' }).done(function (data) {
            self.connections(data);
            self.loadPlatforms();
        }).fail(function () {
            toastr.error('Bağlantılar yüklenemedi');
        });
    };

    self.testConnection = function (conn) {
        toastr.info('Bağlantı test ediliyor...');
        $.ajax({
            url: '/proxy/integrations/connections/' + conn.uid + '/test',
            method: 'POST'
        }).done(function (result) {
            if (result.success) {
                toastr.success('Bağlantı başarılı' + (result.platformInfo ? ': ' + result.platformInfo : ''));
            } else {
                toastr.error('Bağlantı başarısız: ' + (result.error || 'Bilinmeyen hata'));
            }
        }).fail(function () {
            toastr.error('Test sırasında hata oluştu');
        });
    };

    self.goToDetail = function (conn) {
        window.location.href = '/Integrations/Detail/' + conn.uid;
    };

    self.removeConnection = function (conn) {
        confirmModal('Bağlantıyi Sil', conn.name + ' bağlantısını silmek istediğinize emin misiniz? Bu işlem geri alınamaz.', function () {
            $.ajax({
                url: '/proxy/integrations/connections/' + conn.uid,
                method: 'DELETE'
            }).done(function () {
                self.loadConnections();
                toastr.success('Bağlantı silindi');
            }).fail(function (xhr) {
                toastr.error(xhr.responseJSON?.message || 'Silinemedi');
            });
        });
    };

    self.formatDate = function (dateStr) {
        if (!dateStr) return '-';
        var d = new Date(dateStr);
        var pad = function (n) { return n < 10 ? '0' + n : n; };
        return pad(d.getDate()) + '.' + pad(d.getMonth() + 1) + '.' + d.getFullYear() + ' ' + pad(d.getHours()) + ':' + pad(d.getMinutes());
    };

    $(document).ready(function () {
        self.loadConnections();

        // OAuth callback sonrası basari mesaji
        var params = new URLSearchParams(window.location.search);
        if (params.get('oauth') === 'success') {
            toastr.success('OAuth2 yetkilendirme başarılı!');
            window.history.replaceState({}, '', window.location.pathname);
        }
    });
}

ko.applyBindings(new IntegrationsViewModel(), document.getElementById('integrations-vm'));
