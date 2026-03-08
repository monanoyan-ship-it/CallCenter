var statusMap = {
    1: { name: 'Taslak', css: 'bg-secondary' },
    2: { name: 'Aktif', css: 'bg-success' },
    3: { name: 'Duraklatilmis', css: 'bg-warning text-dark' },
    4: { name: 'Tamamlandi', css: 'bg-primary' },
    5: { name: 'Arsivlendi', css: 'bg-dark' }
};

var contactStatusMap = {
    1: { name: 'Bekliyor', css: 'bg-secondary' },
    2: { name: 'Araniyor', css: 'bg-info' },
    3: { name: 'Ulasildi', css: 'bg-success' },
    4: { name: 'Ulasilamadi', css: 'bg-danger' },
    5: { name: 'Iptal', css: 'bg-dark' }
};

function CampaignDetailViewModel() {
    var self = this;
    self.campaign = ko.observable(null);
    self.isLoading = ko.observable(true);
    self.selectedIds = ko.observableArray([]);

    // Add contacts
    self.contactSearch = ko.observable('');
    self.availableContacts = ko.observable(null);
    self.contactIdsToAdd = ko.observableArray([]);
    self.isAdding = ko.observable(false);

    // Assign
    self.operators = ko.observable(null);
    self.assignPersonnelId = ko.observable(null);
    self.isAssigning = ko.observable(false);

    var addModal, assignModal;

    self.statusCss = ko.computed(function () {
        var c = self.campaign();
        if (!c) return '';
        return (statusMap[c.statusId] || {}).css || 'bg-secondary';
    });

    self.contactStatusCss = function (statusId) {
        return (contactStatusMap[statusId] || {}).css || 'bg-secondary';
    };

    self.pctReached = ko.computed(function () {
        var c = self.campaign();
        if (!c || !c.totalContacts) return 0;
        return Math.round(c.reachedCount * 100 / c.totalContacts);
    });

    self.pctNotReached = ko.computed(function () {
        var c = self.campaign();
        if (!c || !c.totalContacts) return 0;
        return Math.round(c.notReachedCount * 100 / c.totalContacts);
    });

    self.pctCancelled = ko.computed(function () {
        var c = self.campaign();
        if (!c || !c.totalContacts) return 0;
        return Math.round(c.cancelledCount * 100 / c.totalContacts);
    });

    self.allSelected = ko.computed(function () {
        var c = self.campaign();
        if (!c || c.contacts.length === 0) return false;
        return self.selectedIds().length === c.contacts.length;
    });

    self.toggleSelectAll = function () {
        var c = self.campaign();
        if (!c) return true;
        if (self.selectedIds().length === c.contacts.length) {
            self.selectedIds([]);
        } else {
            self.selectedIds(c.contacts.map(function (cc) { return cc.id; }));
        }
        return true;
    };

    self.loadCampaign = function () {
        self.isLoading(true);
        $.ajax({
            url: '/proxy/campaigns/' + campaignUid,
            method: 'GET'
        }).done(function (data) {
            if (data) {
                var s = statusMap[data.statusId] || { name: '', css: 'bg-secondary' };
                data.statusName = s.name;
                (data.contacts || []).forEach(function (cc) {
                    var cs = contactStatusMap[cc.statusId] || { name: '' };
                    cc.statusName = cs.name;
                });
                self.campaign(data);
            }
        }).fail(function () {
            toastr.error('Kampanya yuklenemedi');
        }).always(function () {
            self.isLoading(false);
        });
    };

    // Status
    self.changeStatus = function (statusId) {
        $.ajax({
            url: '/proxy/campaigns/' + campaignUid,
            method: 'PUT',
            contentType: 'application/json',
            data: JSON.stringify({ statusId: statusId })
        }).done(function () {
            self.loadCampaign();
            toastr.success('Kampanya durumu guncellendi');
        }).fail(function () {
            toastr.error('Guncelenemedi');
        });
    };

    // Add Contacts
    self.isAlreadyAdded = function (contactId) {
        var c = self.campaign();
        if (!c) return false;
        return c.contacts.some(function (cc) { return cc.contactId === contactId; });
    };

    self.openAddContacts = function () {
        self.contactSearch('');
        self.availableContacts(null);
        self.contactIdsToAdd([]);
        addModal.show();
    };

    self.searchContacts = function () {
        var q = self.contactSearch() || '';
        $.ajax({
            url: '/proxy/crm/contacts?search=' + encodeURIComponent(q) + '&pageSize=50',
            method: 'GET'
        }).done(function (data) {
            self.availableContacts(data || []);
        }).fail(function () {
            toastr.error('Arama basarisiz');
        });
    };

    self.addSelectedContacts = function () {
        if (self.contactIdsToAdd().length === 0) return;
        self.isAdding(true);
        $.ajax({
            url: '/proxy/campaigns/' + campaignUid + '/contacts',
            method: 'POST',
            contentType: 'application/json',
            data: JSON.stringify({ contactIds: self.contactIdsToAdd() })
        }).done(function () {
            addModal.hide();
            self.loadCampaign();
            toastr.success('Kisiler eklendi');
        }).fail(function () {
            toastr.error('Eklenemedi');
        }).always(function () {
            self.isAdding(false);
        });
    };

    // Remove Contact
    self.removeContact = function (cc) {
        $.ajax({
            url: '/proxy/campaigns/' + campaignUid + '/contacts/' + cc.id,
            method: 'DELETE'
        }).done(function () {
            self.loadCampaign();
            toastr.success('Kisi cikarildi');
        }).fail(function () {
            toastr.error('Cikarilmadi');
        });
    };

    // Assign
    self.openAssign = function () {
        self.assignPersonnelId(null);
        self.operators(null);
        assignModal.show();
        $.ajax({
            url: '/proxy/campaigns/operators',
            method: 'GET'
        }).done(function (data) {
            self.operators(data || []);
        }).fail(function () {
            self.operators([]);
            toastr.error('Operatorler yuklenemedi');
        });
    };

    self.assignSelected = function () {
        if (!self.assignPersonnelId() || self.selectedIds().length === 0) return;
        self.isAssigning(true);
        $.ajax({
            url: '/proxy/campaigns/' + campaignUid + '/assign',
            method: 'POST',
            contentType: 'application/json',
            data: JSON.stringify({
                campaignContactIds: self.selectedIds(),
                personnelId: parseInt(self.assignPersonnelId())
            })
        }).done(function () {
            assignModal.hide();
            self.selectedIds([]);
            self.loadCampaign();
            toastr.success('Atama yapildi');
        }).fail(function () {
            toastr.error('Atama basarisiz');
        }).always(function () {
            self.isAssigning(false);
        });
    };

    $(document).ready(function () {
        addModal = new bootstrap.Modal(document.getElementById('addContactsModal'));
        assignModal = new bootstrap.Modal(document.getElementById('assignModal'));
        self.loadCampaign();
    });
}

ko.applyBindings(new CampaignDetailViewModel(), document.getElementById('campaign-detail-vm'));
