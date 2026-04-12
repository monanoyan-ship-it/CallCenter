function SettingsViewModel() {
    var self = this;
    self.settings = ko.observableArray([]);
    self.isLoading = ko.observable(true);

    self.loadSettings = function() {
        self.isLoading(true);
        $.get('/proxy/settings', function(data) {
            var items = Array.isArray(data) ? data : (data.items || data.data || []);
            self.settings(items.map(function(s) {
                return { key: s.key, value: ko.observable(s.value), id: s.id };
            }));
        }).always(function() {
            self.isLoading(false);
        });
    };

    self.saveSettings = function() {
        var payload = self.settings().map(function(s) {
            return { id: s.id, key: s.key, value: s.value() };
        });
        $.ajax({
            url: '/proxy/settings',
            method: 'PUT',
            contentType: 'application/json',
            data: JSON.stringify(payload),
            success: function() { toastr.success('Ayarlar kaydedildi.'); },
            error: function() { toastr.error('Kaydetme hatasi.'); }
        });
    };

    self.loadSettings();
}

ko.applyBindings(new SettingsViewModel(), document.getElementById('settings-vm'));
