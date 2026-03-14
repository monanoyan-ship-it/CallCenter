function WebhooksViewModel() {
    var self = this;
    self.webhooks = ko.observableArray([]);
    self.connections = ko.observableArray([]);
    self.eventTypes = ko.observableArray([]);
    self.searchQuery = ko.observable('');
    self.statusFilter = ko.observable('');
    self.isSaving = ko.observable(false);
    self.isLoadingDeliveries = ko.observable(false);
    self.deliveries = ko.observableArray([]);
    self.selectedWebhookName = ko.observable('');

    self.activeWebhookCount = ko.computed(function () {
        return self.webhooks().filter(function (w) { return w.isActive; }).length;
    });

    self.connectionOptions = ko.computed(function () {
        return self.connections();
    });

    self.filteredWebhooks = ko.computed(function () {
        var q = (self.searchQuery() || '').toLowerCase();
        var status = self.statusFilter();
        return self.webhooks().filter(function (w) {
            var matchQ = !q || (w.name || '').toLowerCase().indexOf(q) >= 0 || (w.targetUrl || '').toLowerCase().indexOf(q) >= 0;
            var matchStatus = !status
                || (status === 'active' && w.isActive)
                || (status === 'inactive' && !w.isActive);
            return matchQ && matchStatus;
        });
    });

    // Form
    self.form = {
        name: ko.observable(''),
        targetUrl: ko.observable(''),
        connectionId: ko.observable(null),
        selectedEvents: ko.observableArray([])
    };

    var formModal, deliveryOffcanvas;

    self.loadWebhooks = function () {
        $.ajax({ url: '/proxy/integrations/webhooks', method: 'GET' }).done(function (data) {
            self.webhooks(data);
        }).fail(function () {
            toastr.error('Webhooklar yuklenemedi');
        });
    };

    self.loadConnections = function () {
        $.ajax({ url: '/proxy/integrations/connections', method: 'GET' }).done(function (data) {
            self.connections(data);
        });
    };

    self.loadEventTypes = function () {
        $.ajax({ url: '/proxy/integrations/event-types', method: 'GET' }).done(function (data) {
            self.eventTypes(data);
        });
    };

    self.openNew = function () {
        self.form.name('');
        self.form.targetUrl('');
        self.form.connectionId(null);
        self.form.selectedEvents([]);
        formModal.show();
    };

    self.save = function () {
        var name = self.form.name();
        var url = self.form.targetUrl();
        if (!name || !url) { toastr.warning('Ad ve URL zorunludur'); return; }

        self.isSaving(true);
        $.ajax({
            url: '/proxy/integrations/webhooks',
            method: 'POST',
            contentType: 'application/json',
            data: JSON.stringify({
                name: name,
                targetUrl: url,
                integrationConnectionId: self.form.connectionId() ? parseInt(self.form.connectionId()) : null,
                eventFilter: self.form.selectedEvents().length > 0 ? self.form.selectedEvents() : null
            })
        }).done(function () {
            formModal.hide();
            toastr.success('Webhook olusturuldu');
            self.loadWebhooks();
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

    self.toggleWebhook = function (wh) {
        $.ajax({
            url: '/proxy/integrations/webhooks/' + wh.uid,
            method: 'PUT',
            contentType: 'application/json',
            data: JSON.stringify({ isActive: !wh.isActive })
        }).done(function () {
            toastr.success(wh.isActive ? 'Webhook pasife alindi' : 'Webhook aktif edildi');
            self.loadWebhooks();
        }).fail(function () {
            toastr.error('Guncelleme basarisiz');
        });
    };

    self.removeWebhook = function (wh) {
        if (!confirm(wh.name + ' webhook\'unu silmek istediginize emin misiniz?')) return;
        $.ajax({
            url: '/proxy/integrations/webhooks/' + wh.uid,
            method: 'DELETE'
        }).done(function () {
            toastr.success('Webhook silindi');
            self.loadWebhooks();
        }).fail(function () {
            toastr.error('Silinemedi');
        });
    };

    self.showDeliveries = function (wh) {
        self.selectedWebhookName(wh.name);
        self.deliveries([]);
        self.isLoadingDeliveries(true);
        deliveryOffcanvas.show();

        $.ajax({
            url: '/proxy/integrations/webhooks/' + wh.uid + '/deliveries?page=1&pageSize=50',
            method: 'GET'
        }).done(function (result) {
            self.deliveries(result.items || result.data || result || []);
            self.isLoadingDeliveries(false);
        }).fail(function () {
            toastr.error('Loglar yuklenemedi');
            self.isLoadingDeliveries(false);
        });
    };

    self.formatDate = function (dateStr) {
        if (!dateStr) return '-';
        var d = new Date(dateStr);
        var pad = function (n) { return n < 10 ? '0' + n : n; };
        return pad(d.getDate()) + '.' + pad(d.getMonth() + 1) + '.' + d.getFullYear() + ' ' + pad(d.getHours()) + ':' + pad(d.getMinutes());
    };

    $(document).ready(function () {
        formModal = new bootstrap.Modal(document.getElementById('webhookModal'));
        deliveryOffcanvas = new bootstrap.Offcanvas(document.getElementById('deliveryOffcanvas'));
        self.loadWebhooks();
        self.loadConnections();
        self.loadEventTypes();
    });
}

ko.applyBindings(new WebhooksViewModel(), document.getElementById('webhooks-vm'));
