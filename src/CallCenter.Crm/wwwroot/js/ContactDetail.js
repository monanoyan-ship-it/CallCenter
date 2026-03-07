function ContactDetailViewModel() {
    var self = this;
    var id = contactDetailId;

    self.contact = ko.observable({});
    self.tickets = ko.observableArray([]);
    self.deals = ko.observableArray([]);
    self.activities = ko.observableArray([]);
    self.tasks = ko.observableArray([]);

    self.loadContact = function() {
        $.ajax({
            url: apiBaseUrl + '/api/crm/contacts/' + id,
            method: 'GET'
        }).done(function(data) {
            self.contact(data);
        }).fail(function() {
            toastr.error('Kisi bilgisi yuklenemedi');
        });
    };

    self.loadTickets = function() {
        $.ajax({
            url: apiBaseUrl + '/api/crm/tickets',
            method: 'GET'
        }).done(function(data) {
            self.tickets(data.filter(function(t) { return t.contactId === id; }));
        });
    };

    self.loadDeals = function() {
        $.ajax({
            url: apiBaseUrl + '/api/crm/deals',
            method: 'GET'
        }).done(function(data) {
            self.deals(data.filter(function(d) { return d.contactId === id; }));
        });
    };

    self.loadActivities = function() {
        $.ajax({
            url: apiBaseUrl + '/api/crm/activities?contactId=' + id,
            method: 'GET'
        }).done(function(data) {
            self.activities(data);
        });
    };

    self.loadTasks = function() {
        $.ajax({
            url: apiBaseUrl + '/api/crm/tasks',
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

ko.applyBindings(new ContactDetailViewModel(), document.getElementById('contact-detail-vm'));
