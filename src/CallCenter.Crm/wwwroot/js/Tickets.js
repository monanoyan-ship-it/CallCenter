function TicketsViewModel() {
    var self = this;
    self.tickets = ko.observableArray([]);
    self.searchQuery = ko.observable('');
    self.statusFilter = ko.observable('');
    self.isEditing = ko.observable(false);
    self.editingId = ko.observable(null);
    self.isSaving = ko.observable(false);
    self.contactsList = ko.observableArray([]);

    self.form = {
        subject: ko.observable(''),
        description: ko.observable(''),
        contactId: ko.observable(null),
        priorityId: ko.observable(2),
        statusId: ko.observable(1),
        assignedToPersonnelId: ko.observable(null)
    };

    self.stats = ko.computed(function() {
        var all = self.tickets();
        return {
            open: all.filter(function(t) { return t.statusId === 1; }).length,
            inProgress: all.filter(function(t) { return t.statusId === 2; }).length,
            resolved: all.filter(function(t) { return t.statusId === 4; }).length,
            closed: all.filter(function(t) { return t.statusId === 5; }).length
        };
    });

    self.filteredTickets = ko.computed(function() {
        var q = (self.searchQuery() || '').toLowerCase();
        var s = self.statusFilter();
        return self.tickets().filter(function(t) {
            var matchSearch = !q || (t.subject || '').toLowerCase().indexOf(q) >= 0
                || (t.contactName || '').toLowerCase().indexOf(q) >= 0;
            var matchStatus = !s || t.statusId === parseInt(s);
            return matchSearch && matchStatus;
        });
    });

    var formModal;

    self.loadData = function() {
        $.ajax({
            url: '/proxy/crm/tickets',
            method: 'GET'
        }).done(function(data) {
            self.tickets(data);
        }).fail(function() {
            toastr.error('Talepler yüklenemedi');
        });
    };

    self.loadContacts = function() {
        $.ajax({
            url: '/proxy/crm/contacts',
            method: 'GET'
        }).done(function(data) {
            self.contactsList(data);
        });
    };

    self.resetForm = function() {
        self.form.subject('');
        self.form.description('');
        self.form.contactId(null);
        self.form.priorityId(2);
        self.form.statusId(1);
        self.form.assignedToPersonnelId(null);
        self.isEditing(false);
        self.editingId(null);
    };

    self.openNew = function() {
        self.resetForm();
        formModal.show();
    };

    self.openEdit = function(ticket) {
        self.isEditing(true);
        self.editingId(ticket.id);
        self.form.subject(ticket.subject || '');
        self.form.description(ticket.description || '');
        self.form.contactId(ticket.contactId);
        self.form.priorityId(ticket.priorityId);
        self.form.statusId(ticket.statusId);
        self.form.assignedToPersonnelId(ticket.assignedToPersonnelId);
        formModal.show();
    };

    self.save = function() {
        var data = {
            subject: self.form.subject(),
            description: self.form.description(),
            contactId: self.form.contactId(),
            priorityId: self.form.priorityId(),
            statusId: self.form.statusId(),
            assignedToPersonnelId: self.form.assignedToPersonnelId()
        };

        if (!data.subject) {
            toastr.warning('Başlık zorunludur');
            return;
        }

        self.isSaving(true);
        var url = '/proxy/crm/tickets';
        var method = 'POST';
        if (self.isEditing()) {
            url += '/' + self.editingId();
            method = 'PUT';
        }

        $.ajax({
            url: url,
            method: method,
            contentType: 'application/json',
            data: JSON.stringify(data)
        }).done(function() {
            formModal.hide();
            self.loadData();
            toastr.success(self.isEditing() ? 'Talep güncellendi' : 'Talep oluşturuldu');
            self.isSaving(false);
        }).fail(function(xhr) {
            toastr.error(xhr.responseJSON?.error || 'Bir hata oluştu');
            self.isSaving(false);
        });
    };

    $(document).ready(function() {
        formModal = new bootstrap.Modal(document.getElementById('ticketModal'));
        self.loadData();
        self.loadContacts();
    });
}

ko.applyBindings(new TicketsViewModel(), document.getElementById('tickets-vm'));
