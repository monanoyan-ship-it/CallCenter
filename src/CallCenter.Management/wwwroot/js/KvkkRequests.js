function RequestsViewModel() {
    var self = this;
    self.items = ko.observableArray([]);
    self.isLoading = ko.observable(false);
    self.isSaving = ko.observable(false);
    self.currentPage = ko.observable(1);
    self.totalCount = ko.observable(0);
    self.pageSize = 20;
    self.formTitle = ko.observable('Yeni Basvuru');

    self.form = {
        uid: ko.observable(null), personName: ko.observable(''),
        requestType: ko.observable('Erisim'), status: ko.observable('Beklemede'),
        description: ko.observable('')
    };

    self.pageNumbers = ko.computed(function() {
        var total = Math.ceil(self.totalCount() / self.pageSize);
        var pages = [];
        for (var i = 1; i <= total; i++) pages.push(i);
        return pages;
    });

    self.goToPage = function(page) { self.currentPage(page); self.loadData(); };

    self.loadData = function() {
        self.isLoading(true);
        $.get('/proxy/kvkk/requests', { page: self.currentPage(), pageSize: self.pageSize }, function(data) {
            var items = Array.isArray(data) ? data : (data.items || data.data || []);
            self.items(items);
            self.totalCount(data.totalCount || data.total || items.length);
        }).always(function() { self.isLoading(false); });
    };

    self.loadOverdue = function() {
        self.isLoading(true);
        $.get('/proxy/kvkk/requests/overdue', function(data) {
            var items = Array.isArray(data) ? data : (data.items || data.data || []);
            self.items(items);
            self.totalCount(items.length);
        }).always(function() { self.isLoading(false); });
    };

    self.openCreate = function() {
        self.form.uid(null); self.form.personName(''); self.form.requestType('Erisim');
        self.form.status('Beklemede'); self.form.description('');
        self.formTitle('Yeni Basvuru');
        new bootstrap.Modal('#requestModal').show();
    };

    self.openEdit = function(item) {
        self.form.uid(item.uid || item.id); self.form.personName(item.personName || '');
        self.form.requestType(item.requestType || 'Erisim');
        self.form.status(item.status || 'Beklemede');
        self.form.description(item.description || '');
        self.formTitle('Basvuru Duzenle');
        new bootstrap.Modal('#requestModal').show();
    };

    self.save = function() {
        if (!self.form.personName()) { toastr.warning('Kisi bilgisi zorunludur.'); return; }
        self.isSaving(true);
        var payload = {
            personName: self.form.personName(), requestType: self.form.requestType(),
            status: self.form.status(), description: self.form.description()
        };
        var method = self.form.uid() ? 'PUT' : 'POST';
        var url = self.form.uid() ? '/proxy/kvkk/requests/' + self.form.uid() : '/proxy/kvkk/requests';
        $.ajax({
            url: url, method: method, contentType: 'application/json', data: JSON.stringify(payload),
            success: function() {
                toastr.success('Kaydedildi.'); bootstrap.Modal.getInstance(document.getElementById('requestModal')).hide();
                self.loadData();
            },
            error: function() { toastr.error('Kaydetme hatasi.'); }
        }).always(function() { self.isSaving(false); });
    };

    self.loadData();
}

ko.applyBindings(new RequestsViewModel(), document.getElementById('requests-vm'));
