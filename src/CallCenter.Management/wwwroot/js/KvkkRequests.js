function RequestsViewModel() {
    var self = this;
    self.items = ko.observableArray([]);
    self.customers = ko.observableArray([]);
    self.isLoading = ko.observable(false);
    self.isSaving = ko.observable(false);
    self.currentPage = ko.observable(1);
    self.totalCount = ko.observable(0);
    self.pageSize = 20;
    self.formTitle = ko.observable('Yeni Basvuru');

    self.requestTypes = [
        { id: 1, name: 'Bilgi Edinme Hakki' },
        { id: 2, name: 'Duzeltme Hakki' },
        { id: 3, name: 'Silme Hakki' },
        { id: 4, name: 'Kisitlama Hakki' },
        { id: 5, name: 'Tasinabilirlik Hakki' },
        { id: 6, name: 'Itiraz Hakki' }
    ];

    self.statuses = [
        { id: 1, name: 'Alindi' },
        { id: 2, name: 'Isleniyor' },
        { id: 3, name: 'Tamamlandi' },
        { id: 4, name: 'Reddedildi' },
        { id: 5, name: 'Suresi Gecti' }
    ];

    self.form = {
        uid: ko.observable(null),
        customerId: ko.observable(''),
        requesterName: ko.observable(''),
        requesterIdentifier: ko.observable(''),
        requesterContact: ko.observable(''),
        requestTypeId: ko.observable(3),
        statusId: ko.observable(1),
        requestDescription: ko.observable(''),
        responseDescription: ko.observable(''),
        assignedToUserName: ko.observable(''),
        rejectionReason: ko.observable('')
    };

    self.pageNumbers = ko.computed(function() {
        var total = Math.ceil(self.totalCount() / self.pageSize);
        var pages = [];
        for (var i = 1; i <= total; i++) pages.push(i);
        return pages;
    });

    self.goToPage = function(page) {
        self.currentPage(page);
        self.loadData();
    };

    function remainingDays(deadline) {
        if (!deadline) return null;
        return Math.ceil((new Date(deadline) - new Date()) / 86400000);
    }

    function normalize(item) {
        var uid = item.uid || '';
        var left = remainingDays(item.deadline);
        return Object.assign({}, item, {
            requestNumber: uid ? uid.substring(0, 8).toUpperCase() : item.id,
            customerName: item.customerName || 'CorpLynk Platform',
            requesterName: item.requesterName || '-',
            requesterContact: item.requesterContact || item.requesterIdentifier || '-',
            requestTypeName: item.requestTypeName || '-',
            statusName: item.statusName || '-',
            remainingDays: left,
            isOverdue: item.isOverdue || (left != null && left < 0 && item.statusId !== 3 && item.statusId !== 4)
        });
    }

    self.loadCustomers = function() {
        $.get('/proxy/customers', { page: 1, pageSize: 500 }, function(data) {
            var items = Array.isArray(data) ? data : (data.items || data.data || []);
            self.customers(items);
        });
    };

    self.loadData = function() {
        self.isLoading(true);
        $.get('/proxy/kvkk/requests', { page: self.currentPage(), pageSize: self.pageSize }, function(data) {
            var items = Array.isArray(data) ? data : (data.items || data.data || []);
            self.items(items.map(normalize));
            self.totalCount(data.totalCount || data.total || items.length);
        }).always(function() { self.isLoading(false); });
    };

    self.loadOverdue = function() {
        self.isLoading(true);
        $.get('/proxy/kvkk/requests/overdue', function(data) {
            var items = Array.isArray(data) ? data : (data.items || data.data || []);
            self.items(items.map(normalize));
            self.totalCount(items.length);
        }).always(function() { self.isLoading(false); });
    };

    self.openCreate = function() {
        self.form.uid(null);
        self.form.customerId('');
        self.form.requesterName('');
        self.form.requesterIdentifier('');
        self.form.requesterContact('');
        self.form.requestTypeId(3);
        self.form.statusId(1);
        self.form.requestDescription('');
        self.form.responseDescription('');
        self.form.assignedToUserName('');
        self.form.rejectionReason('');
        self.formTitle('Yeni Basvuru');
        new bootstrap.Modal('#requestModal').show();
    };

    self.openEdit = function(item) {
        self.form.uid(item.uid);
        self.form.customerId(item.customerId || '');
        self.form.requesterName(item.requesterName || '');
        self.form.requesterIdentifier(item.requesterIdentifier || '');
        self.form.requesterContact(item.requesterContact || '');
        self.form.requestTypeId(item.requestTypeId || 3);
        self.form.statusId(item.statusId || 1);
        self.form.requestDescription(item.requestDescription || '');
        self.form.responseDescription(item.responseDescription || '');
        self.form.assignedToUserName(item.assignedToUserName || '');
        self.form.rejectionReason(item.rejectionReason || '');
        self.formTitle('Basvuru Detayi');
        new bootstrap.Modal('#requestModal').show();
    };

    self.save = function() {
        self.isSaving(true);
        var isEdit = !!self.form.uid();
        var payload;

        if (isEdit) {
            payload = {
                statusId: Number(self.form.statusId()),
                responseDescription: self.form.responseDescription() || null,
                assignedToUserName: self.form.assignedToUserName() || null,
                rejectionReason: self.form.rejectionReason() || null
            };
        } else {
            if (!self.form.customerId()) { toastr.warning('Musteri secimi zorunludur.'); self.isSaving(false); return; }
            if (!self.form.requesterName()) { toastr.warning('Basvuran adi zorunludur.'); self.isSaving(false); return; }
            if (!self.form.requesterContact()) { toastr.warning('Iletisim bilgisi zorunludur.'); self.isSaving(false); return; }
            payload = {
                customerId: Number(self.form.customerId()),
                requestTypeId: Number(self.form.requestTypeId()),
                requesterName: self.form.requesterName(),
                requesterIdentifier: self.form.requesterIdentifier() || self.form.requesterContact(),
                requesterContact: self.form.requesterContact(),
                requestDescription: self.form.requestDescription()
            };
        }

        $.ajax({
            url: isEdit ? '/proxy/kvkk/requests/' + self.form.uid() : '/proxy/kvkk/requests',
            method: isEdit ? 'PUT' : 'POST',
            contentType: 'application/json',
            data: JSON.stringify(payload),
            success: function() {
                toastr.success('Kaydedildi.');
                bootstrap.Modal.getInstance(document.getElementById('requestModal')).hide();
                self.loadData();
            },
            error: function(xhr) {
                var msg = xhr.responseJSON && (xhr.responseJSON.error || xhr.responseJSON.message);
                toastr.error(msg || 'Kaydetme hatasi.');
            }
        }).always(function() { self.isSaving(false); });
    };

    self.loadCustomers();
    self.loadData();
}

ko.applyBindings(new RequestsViewModel(), document.getElementById('requests-vm'));
