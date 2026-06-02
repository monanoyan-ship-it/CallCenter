function CrmTasksViewModel() {
    var self = this;
    self.tasks = ko.observableArray([]);
    self.contactsList = ko.observableArray([]);
    self.personnelList = ko.observableArray([]);
    self.searchQuery = ko.observable('');
    self.statusFilter = ko.observable('');
    self.isEditing = ko.observable(false);
    self.editingId = ko.observable(null);
    self.isSaving = ko.observable(false);

    self.form = {
        title: ko.observable(''),
        description: ko.observable(''),
        contactId: ko.observable(null),
        assignedToPersonnelId: ko.observable(null),
        dueDate: ko.observable(''),
        statusId: ko.observable(1)
    };

    self.stats = ko.computed(function() {
        var all = self.tasks();
        var now = new Date();
        now.setHours(0, 0, 0, 0);
        return {
            todo: all.filter(function(t) { return t.statusId === 1; }).length,
            inProgress: all.filter(function(t) { return t.statusId === 2; }).length,
            done: all.filter(function(t) { return t.statusId === 3; }).length,
            overdue: all.filter(function(t) {
                return t.dueDate && new Date(t.dueDate) < now && t.statusId < 3;
            }).length
        };
    });

    self.filteredTasks = ko.computed(function() {
        var q = (self.searchQuery() || '').toLowerCase();
        var s = self.statusFilter();
        return self.tasks().filter(function(t) {
            var matchSearch = !q || (t.title || '').toLowerCase().indexOf(q) >= 0
                || (t.contactName || '').toLowerCase().indexOf(q) >= 0
                || (t.assignedToName || '').toLowerCase().indexOf(q) >= 0;
            var matchStatus = !s || t.statusId === parseInt(s);
            return matchSearch && matchStatus;
        });
    });

    var formModal;

    self.loadData = function() {
        $.ajax({
            url: '/proxy/crm/tasks',
            method: 'GET'
        }).done(function(data) {
            var now = new Date();
            now.setHours(0, 0, 0, 0);
            var items = data.map(function(t) {
                t.isOverdue = t.dueDate && new Date(t.dueDate) < now && t.statusId < 3;
                t.dueDateFormatted = t.dueDate ? new Date(t.dueDate).toLocaleDateString('tr-TR') : '-';
                t.statusName = self.getStatusName(t.statusId);
                t.statusCss = self.getStatusCss(t.statusId);
                return t;
            });
            self.tasks(items);
        }).fail(function() {
            toastr.error('Görevler yüklenemedi');
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

    self.loadPersonnel = function() {
        $.ajax({
            url: '/proxy/portal/personnel',
            method: 'GET'
        }).done(function(data) {
            self.personnelList(data);
        });
    };

    self.getStatusName = function(id) {
        if (id === 1) return 'Yapılacak';
        if (id === 2) return 'Devam Ediyor';
        if (id === 3) return 'Tamamlandı';
        if (id === 4) return 'İptal';
        return '-';
    };

    self.getStatusCss = function(id) {
        if (id === 1) return 'bg-secondary';
        if (id === 2) return 'bg-info';
        if (id === 3) return 'bg-success';
        if (id === 4) return 'bg-danger';
        return 'bg-secondary';
    };

    self.resetForm = function() {
        self.form.title('');
        self.form.description('');
        self.form.contactId(null);
        self.form.assignedToPersonnelId(null);
        self.form.dueDate('');
        self.form.statusId(1);
        self.isEditing(false);
        self.editingId(null);
    };

    self.openNew = function() {
        self.resetForm();
        formModal.show();
    };

    self.openEdit = function(task) {
        self.isEditing(true);
        self.editingId(task.id);
        self.form.title(task.title || '');
        self.form.description(task.description || '');
        self.form.contactId(task.contactId);
        self.form.assignedToPersonnelId(task.assignedToPersonnelId);
        self.form.dueDate(task.dueDate ? task.dueDate.substring(0, 10) : '');
        self.form.statusId(task.statusId);
        formModal.show();
    };

    self.save = function() {
        var data = {
            title: self.form.title(),
            description: self.form.description(),
            contactId: self.form.contactId(),
            assignedToPersonnelId: self.form.assignedToPersonnelId() || 0,
            dueDate: self.form.dueDate() || null
        };

        if (!data.title) {
            toastr.warning('Başlık zorunludur');
            return;
        }

        self.isSaving(true);
        var url = '/proxy/crm/tasks';
        var method = 'POST';
        if (self.isEditing()) {
            url += '/' + self.editingId();
            method = 'PUT';
            data.statusId = parseInt(self.form.statusId());
        }

        $.ajax({
            url: url,
            method: method,
            contentType: 'application/json',
            data: JSON.stringify(data)
        }).done(function() {
            formModal.hide();
            self.loadData();
            toastr.success(self.isEditing() ? 'Görev güncellendi' : 'Görev oluşturuldu');
            self.isSaving(false);
        }).fail(function(xhr) {
            toastr.error(xhr.responseJSON?.error || 'Bir hata oluştu');
            self.isSaving(false);
        });
    };

    $(document).ready(function() {
        formModal = new bootstrap.Modal(document.getElementById('crmTaskModal'));
        self.loadData();
        self.loadContacts();
        self.loadPersonnel();
    });
}

ko.applyBindings(new CrmTasksViewModel(), document.getElementById('crmtasks-vm'));
