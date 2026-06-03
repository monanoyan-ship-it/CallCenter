function AuditLogsViewModel() {
    var self = this;
    self.items = ko.observableArray([]);
    self.categories = ko.observableArray([]);
    self.actions = ko.observableArray([]);
    self.isLoading = ko.observable(false);
    self.searchText = ko.observable('');
    self.categoryFilter = ko.observable('');
    self.actionFilter = ko.observable('');
    self.dateFrom = ko.observable('');
    self.dateTo = ko.observable('');
    self.currentPage = ko.observable(1);
    self.totalCount = ko.observable(0);
    self.pageSize = 25;
    self.detail = ko.observable(null);
    self.detailLoading = ko.observable(false);

    self.pageNumbers = ko.computed(function() {
        var total = Math.ceil(self.totalCount() / self.pageSize);
        var pages = [];
        for (var i = 1; i <= total; i++) pages.push(i);
        return pages;
    });

    self.goToPage = function(page) { self.currentPage(page); self.loadData(); };
    self.onSearchKeyUp = function(d, e) { if (e.keyCode === 13) { self.currentPage(1); self.loadData(); } return true; };

    function buildParams(includePaging) {
        var params = includePaging ? { page: self.currentPage(), pageSize: self.pageSize } : {};
        if (self.searchText()) params.search = self.searchText();
        if (self.categoryFilter()) params.category = self.categoryFilter();
        if (self.actionFilter()) params.action = self.actionFilter();
        if (self.dateFrom()) params.dateFrom = self.dateFrom();
        if (self.dateTo()) params.dateTo = self.dateTo();
        return params;
    }

    self.loadFilters = function() {
        $.get('/proxy/auditlogs/categories', function(data) {
            self.categories(Array.isArray(data) ? data : []);
        });
        $.get('/proxy/auditlogs/actions', function(data) {
            self.actions(Array.isArray(data) ? data : []);
        });
    };

    self.loadData = function() {
        self.isLoading(true);
        var params = buildParams(true);
        $.get('/proxy/auditlogs', params, function(data) {
            var items = Array.isArray(data) ? data : (data.items || data.data || []);
            self.items(items);
            self.totalCount(data.totalCount || data.total || items.length);
        }).always(function() { self.isLoading(false); });
    };

    self.exportCsv = function() {
        var query = $.param(buildParams(false));
        window.location.href = '/proxy/auditlogs/export' + (query ? '?' + query : '');
    };

    self.showDetail = function(item) {
        self.detailLoading(true);
        self.detail(null);
        new bootstrap.Modal('#detailModal').show();
        $.get('/proxy/auditlogs/' + item.id, function(data) {
            self.detail(data);
        }).always(function() { self.detailLoading(false); });
    };

    self.loadFilters();
    self.loadData();
}

ko.applyBindings(new AuditLogsViewModel(), document.getElementById('auditlogs-vm'));
