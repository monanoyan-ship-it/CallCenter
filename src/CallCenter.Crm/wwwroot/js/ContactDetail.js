function ContactDetailViewModel() {
    var self = this;
    var root = document.getElementById('contact-detail-vm');
    var id = root ? parseInt(root.getAttribute('data-contact-id') || '0', 10) : 0;

    self.contact = ko.observable({});
    self.tickets = ko.observableArray([]);
    self.deals = ko.observableArray([]);
    self.activities = ko.observableArray([]);
    self.tasks = ko.observableArray([]);

    self.loadContact = function() {
        $.ajax({
            url: '/proxy/crm/contacts/' + id,
            method: 'GET'
        }).done(function(data) {
            self.contact(data);
        }).fail(function() {
            toastr.error('Kisi bilgisi yuklenemedi');
        });
    };

    self.loadTickets = function() {
        $.ajax({
            url: '/proxy/crm/tickets',
            method: 'GET'
        }).done(function(data) {
            self.tickets(data.filter(function(t) { return t.contactId === id; }));
        });
    };

    self.loadDeals = function() {
        $.ajax({
            url: '/proxy/crm/deals',
            method: 'GET'
        }).done(function(data) {
            self.deals(data.filter(function(d) { return d.contactId === id; }));
        });
    };

    self.loadActivities = function() {
        $.ajax({
            url: '/proxy/crm/activities?contactId=' + id,
            method: 'GET'
        }).done(function(data) {
            self.activities(data);
        });
    };

    self.loadTasks = function() {
        $.ajax({
            url: '/proxy/crm/tasks',
            method: 'GET'
        }).done(function(data) {
            self.tasks(data.filter(function(t) { return t.contactId === id; }));
        });
    };

    $(document).ready(function() {
        self.loadContact();
        self.loadTickets();
        self.loadDeals();
        self.loadActivities();
        self.loadTasks();
    });
}

(function () {
    var root = document.getElementById('contact-detail-vm');
    if (!root || typeof ko === 'undefined') return;
    ko.applyBindings(new ContactDetailViewModel(), root);
})();
