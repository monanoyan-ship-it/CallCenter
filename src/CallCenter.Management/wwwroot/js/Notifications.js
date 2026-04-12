function NotificationsViewModel() {
    var self = this;
    self.history = ko.observableArray([]);
    self.form = {
        title: ko.observable(''),
        message: ko.observable(''),
        target: ko.observable('all'),
        priority: ko.observable('info')
    };

    self.sendNotification = function () {
        if (!self.form.title() || !self.form.message()) {
            toastr.warning('Baslik ve mesaj zorunlu.');
            return;
        }
        toastr.info('Bildirim sistemi henuz aktif degil. Ileri surumde eklenecek.');
    };
}
ko.applyBindings(new NotificationsViewModel(), document.getElementById('notifications-vm'));
