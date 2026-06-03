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
            toastr.warning('Başlık ve mesaj zorunlu.');
            return;
        }
        toastr.info('Bildirim sistemi henüz aktif değil. İleriki sürümde eklenecek.');
    };
}
ko.applyBindings(new NotificationsViewModel(), document.getElementById('notifications-vm'));
