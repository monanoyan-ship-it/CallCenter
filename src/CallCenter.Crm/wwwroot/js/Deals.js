function DealsViewModel() {
    var self = this;
    self.deals = ko.observableArray([]);
    self.contactsList = ko.observableArray([]);
    self.searchQuery = ko.observable('');
    self.stageFilter = ko.observable('');
    self.isEditing = ko.observable(false);
    self.editingId = ko.observable(null);
    self.isSaving = ko.observable(false);

    self.form = {
        title: ko.observable(''),
        contactId: ko.observable(null),
        value: ko.observable(0),
        stageId: ko.observable(1),
        probability: ko.observable(0),
        expectedCloseDate: ko.observable(''),
        notes: ko.observable('')
    };

    self.stageCounts = ko.computed(function() {
        var all = self.deals();
        return {
            lead: all.filter(function(d) { return d.stageId === 1; }).length,
            qualified: all.filter(function(d) { return d.stageId === 2; }).length,
            proposal: all.filter(function(d) { return d.stageId === 3; }).length,
            negotiation: all.filter(function(d) { return d.stageId === 4; }).length,
            won: all.filter(function(d) { return d.stageId === 5; }).length,
            lost: all.filter(function(d) { return d.stageId === 6; }).length
        };
    });

    self.filteredDeals = ko.computed(function() {
        var q = (self.searchQuery() || '').toLowerCase();
        var s = self.stageFilter();
        return self.deals().filter(function(d) {
            var matchSearch = !q || (d.title || '').toLowerCase().indexOf(q) >= 0
                || (d.contactName || '').toLowerCase().indexOf(q) >= 0;
            var matchStage = !s || d.stageId === parseInt(s);
            return matchSearch && matchStage;
        });
    });

    self.totalPipeline = ko.computed(function() {
        var active = self.deals().filter(function(d) { return d.stageId !== 6; });
        var total = active.reduce(function(sum, d) { return sum + (d.value || 0); }, 0);
        return total.toLocaleString('tr-TR') + ' TL';
    });

    var formModal;

    self.loadData = function() {
        $.ajax({
            url: '/proxy/crm/deals',
            method: 'GET'
        }).done(function(data) {
            self.deals(data);
        }).fail(function() {
            toastr.error('Fırsatlar yüklenemedi');
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
        self.form.title('');
        self.form.contactId(null);
        self.form.value(0);
        self.form.stageId(1);
        self.form.probability(0);
        self.form.expectedCloseDate('');
        self.form.notes('');
        self.isEditing(false);
        self.editingId(null);
    };

    self.openNew = function() {
        self.resetForm();
        formModal.show();
    };

    self.openEdit = function(deal) {
        self.isEditing(true);
        self.editingId(deal.id);
        self.form.title(deal.title || '');
        self.form.contactId(deal.contactId);
        self.form.value(deal.value || 0);
        self.form.stageId(deal.stageId);
        self.form.probability(deal.probability || 0);
        self.form.expectedCloseDate(deal.expectedCloseDate ? deal.expectedCloseDate.substring(0, 10) : '');
        self.form.notes(deal.notes || '');
        formModal.show();
    };

    self.save = function() {
        var data = {
            title: self.form.title(),
            contactId: self.form.contactId(),
            value: parseFloat(self.form.value()) || 0,
            stageId: self.form.stageId(),
            probability: parseInt(self.form.probability()) || 0,
            expectedCloseDate: self.form.expectedCloseDate() || null,
            notes: self.form.notes()
        };

        if (!data.title) {
            toastr.warning('Fırsat adı zorunludur');
            return;
        }

        self.isSaving(true);
        var url = '/proxy/crm/deals';
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
            toastr.success(self.isEditing() ? 'Fırsat güncellendi' : 'Fırsat oluşturuldu');
            self.isSaving(false);
        }).fail(function(xhr) {
            toastr.error(xhr.responseJSON?.error || 'Bir hata oluştu');
            self.isSaving(false);
        });
    };

    self.remove = function(deal) {
        confirmModal('Fırsatı Sil', 'Bu fırsatı silmek istediğinize emin misiniz?', function () {
            $.ajax({
                url: '/proxy/crm/deals/' + deal.id,
                method: 'DELETE'
            }).done(function() {
                self.loadData();
                toastr.success('Fırsat silindi');
            }).fail(function() {
                toastr.error('Silinemedi');
            });
        });
    };

    $(document).ready(function() {
        formModal = new bootstrap.Modal(document.getElementById('dealModal'));
        self.loadData();
        self.loadContacts();
    });
}

ko.applyBindings(new DealsViewModel(), document.getElementById('deals-vm'));
