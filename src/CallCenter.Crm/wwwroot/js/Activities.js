function ActivitiesViewModel() {
    var self = this;
    self.activities = ko.observableArray([]);
    self.contactsList = ko.observableArray([]);
    self.searchQuery = ko.observable('');
    self.typeFilter = ko.observable('');
    self.isSaving = ko.observable(false);

    self.form = {
        typeId: ko.observable(1),
        summary: ko.observable(''),
        detail: ko.observable(''),
        contactId: ko.observable(null)
    };

    self.filteredActivities = ko.computed(function() {
        var q = (self.searchQuery() || '').toLowerCase();
        var t = self.typeFilter();
        return self.activities().filter(function(a) {
            var matchSearch = !q || (a.summary || '').toLowerCase().indexOf(q) >= 0
                || (a.contactName || '').toLowerCase().indexOf(q) >= 0
                || (a.personnelName || '').toLowerCase().indexOf(q) >= 0;
            var matchType = !t || a.typeId === parseInt(t);
            return matchSearch && matchType;
        });
    });

    var formModal;

    self.loadData = function() {
        $.ajax({
            url: '/proxy/crm/activities',
            method: 'GET'
        }).done(function(data) {
            self.activities(data);
        }).fail(function() {
            toastr.error('Etkilesimler yuklenemedi');
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
        self.form.typeId(1);
        self.form.summary('');
        self.form.detail('');
        self.form.contactId(null);
    };

    self.openNew = function() {
        self.resetForm();
        formModal.show();
    };

    self.save = function() {
        var data = {
            typeId: parseInt(self.form.typeId()),
            summary: self.form.summary(),
            detail: self.form.detail(),
            contactId: self.form.contactId()
        };

        if (!data.summary) {
            toastr.warning('Ozet zorunludur');
            return;
        }

        self.isSaving(true);
        $.ajax({
            url: '/proxy/crm/activities',
            method: 'POST',
            contentType: 'application/json',
            data: JSON.stringify(data)
        }).done(function() {
            formModal.hide();
            self.loadData();
            toastr.success('Etkilesim olusturuldu');
            self.isSaving(false);
        }).fail(function(xhr) {
            toastr.error(xhr.responseJSON?.error || 'Bir hata olustu');
            self.isSaving(false);
        });
    };

    $(document).ready(function() {
        formModal = new bootstrap.Modal(document.getElementById('activityModal'));
        self.loadData();
        self.loadContacts();
    });
}

ko.applyBindings(new ActivitiesViewModel(), document.getElementById('activities-vm'));
