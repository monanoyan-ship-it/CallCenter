function ExpensesViewModel() {
    var self = this;
    self.expenses = ko.observableArray([]);
    self.expenseCategories = ko.observableArray([]);
    self.searchQuery = ko.observable('');
    self.selectedCategoryName = ko.observable(null);
    self.filterStartDate = ko.observable('');
    self.filterEndDate = ko.observable('');
    self.isEditing = ko.observable(false);
    self.editingId = ko.observable(null);
    self.isSaving = ko.observable(false);

    self.form = {
        expenseDate: ko.observable(new Date().toISOString().substring(0, 10)),
        categoryId: ko.observable(null),
        description: ko.observable(''),
        amount: ko.observable(0),
        paymentMethodId: ko.observable(1)
    };

    // ═══ Autocomplete'ler ═══
    self.categoryAutocomplete = createAutocomplete(self.expenseCategories, 'name', self.form.categoryId);

    self.filteredExpenses = ko.computed(function () {
        var q = (self.searchQuery() || '').toLowerCase();
        var catName = self.selectedCategoryName();
        var start = self.filterStartDate();
        var end = self.filterEndDate();

        return self.expenses().filter(function (e) {
            var matchQ = !q || (e.description || '').toLowerCase().indexOf(q) >= 0
                || (e.categoryName || '').toLowerCase().indexOf(q) >= 0;
            var matchCat = !catName || e.categoryName === catName;
            var matchStart = !start || e.expenseDate >= start;
            var matchEnd = !end || e.expenseDate <= end + 'T23:59:59';
            return matchQ && matchCat && matchStart && matchEnd;
        });
    });

    self.totalExpense = ko.computed(function () {
        var total = 0;
        self.filteredExpenses().forEach(function (e) { total += e.amount || 0; });
        return total;
    });

    self.monthlyExpense = ko.computed(function () {
        var now = new Date();
        var month = now.getMonth();
        var year = now.getFullYear();
        var total = 0;
        self.expenses().forEach(function (e) {
            var d = new Date(e.expenseDate);
            if (d.getMonth() === month && d.getFullYear() === year) total += e.amount || 0;
        });
        return total;
    });

    var formModal;

    var statusNames = { 1: 'Beklemede', 2: 'Onayli', 3: 'Reddedildi' };
    var statusCssMap = { 1: 'bg-warning text-dark', 2: 'bg-success', 3: 'bg-danger' };

    self.loadData = function () {
        var url = '/proxy/sln-finance/expenses';
        var params = [];
        if (self.filterStartDate()) params.push('startDate=' + self.filterStartDate());
        if (self.filterEndDate()) params.push('endDate=' + self.filterEndDate());
        if (params.length) url += '?' + params.join('&');

        $.ajax({ url: url, method: 'GET' }).done(function (data) {
            var items = data.items || data;
            var pmNames = { 1: 'Nakit', 2: 'Kredi Karti', 3: 'Havale/EFT' };
            items.forEach(function (e) {
                e.paymentMethodText = pmNames[e.paymentMethodId] || '-';
                e.statusId = e.statusId || 1;
                e.statusText = statusNames[e.statusId] || 'Beklemede';
                e.statusCss = statusCssMap[e.statusId] || 'bg-secondary';
            });
            self.expenses(items);
        }).fail(function () {
            toastr.error('Masraflar yuklenemedi');
        });
    };

    self.loadLookups = function () {
        $.ajax({ url: '/proxy/sln-finance/expense-categories', method: 'GET' }).done(function (data) {
            self.expenseCategories(data);
        });
    };

    self.resetForm = function () {
        self.form.expenseDate(new Date().toISOString().substring(0, 10));
        self.form.categoryId(null);
        self.form.description('');
        self.form.amount(0);
        self.form.paymentMethodId(1);
        self.isEditing(false);
        self.editingId(null);
        self.categoryAutocomplete.clear();
    };

    self.openNew = function () {
        self.resetForm();
        formModal.show();
    };

    self.openEdit = function (expense) {
        self.isEditing(true);
        self.editingId(expense.id);
        self.form.expenseDate(expense.expenseDate ? expense.expenseDate.substring(0, 10) : '');
        self.form.description(expense.description || '');
        self.form.amount(expense.amount || 0);
        self.form.paymentMethodId(expense.paymentMethodId || 1);
        // categoryId yok DTO da, categoryName den bulalim
        var matchedCat = self.expenseCategories().find(function (c) { return c.name === expense.categoryName; });
        var catId = matchedCat ? matchedCat.id : null;
        self.form.categoryId(catId);
        self.categoryAutocomplete.setFromValue(catId);
        formModal.show();
    };

    self.save = function () {
        var data = {
            expenseDate: self.form.expenseDate() ? self.form.expenseDate() + 'T00:00:00Z' : null,
            categoryId: self.form.categoryId() || null,
            description: self.form.description(),
            amount: parseFloat(self.form.amount()) || 0,
            paymentMethodId: parseInt(self.form.paymentMethodId()) || 1
        };

        if (!data.description || !data.amount) {
            toastr.warning('Aciklama ve tutar zorunludur');
            return;
        }

        self.isSaving(true);
        var url = '/proxy/sln-finance/expenses';
        var method = 'POST';
        if (self.isEditing()) {
            url += '/' + self.editingId();
            method = 'PUT';
        }

        $.ajax({
            url: url, method: method,
            contentType: 'application/json',
            data: JSON.stringify(data)
        }).done(function () {
            formModal.hide();
            self.loadData();
            toastr.success(self.isEditing() ? 'Masraf guncellendi' : 'Masraf eklendi');
            self.isSaving(false);
        }).fail(function (xhr) {
            toastr.error(xhr.responseJSON?.error || 'Bir hata olustu');
            self.isSaving(false);
        });
    };

    self.approveExpense = function (expense) {
        confirmModal('Onay', 'Bu masrafi onaylamak istediginize emin misiniz?', function() {
            $.ajax({
                url: '/proxy/sln-finance/expenses/' + expense.id,
                method: 'PUT',
                contentType: 'application/json',
                data: JSON.stringify({ statusId: 2 })
            }).done(function () {
                self.loadData();
                toastr.success('Masraf onaylandi');
            }).fail(function (xhr) {
                toastr.error(xhr.responseJSON?.error || 'Onaylama basarisiz');
            });
        });
    };

    self.rejectExpense = function (expense) {
        confirmModal('Onay', 'Bu masrafi reddetmek istediginize emin misiniz?', function() {
            $.ajax({
                url: '/proxy/sln-finance/expenses/' + expense.id,
                method: 'PUT',
                contentType: 'application/json',
                data: JSON.stringify({ statusId: 3 })
            }).done(function () {
                self.loadData();
                toastr.success('Masraf reddedildi');
            }).fail(function (xhr) {
                toastr.error(xhr.responseJSON?.error || 'Reddetme basarisiz');
            });
        });
    };

    self.remove = function (expense) {
        confirmModal('Onay', 'Bu masrafi silmek istediginize emin misiniz?', function() {
            $.ajax({ url: '/proxy/sln-finance/expenses/' + expense.id, method: 'DELETE' }).done(function () {
                self.loadData();
                toastr.success('Masraf silindi');
            }).fail(function () {
                toastr.error('Masraf silinemedi');
            });
        });
    };

    self.filterStartDate.subscribe(function () { self.loadData(); });
    self.filterEndDate.subscribe(function () { self.loadData(); });

    $(document).ready(function () {
        formModal = new bootstrap.Modal(document.getElementById('expenseModal'));
        self.loadLookups();
        self.loadData();
    });
}

ko.applyBindings(new ExpensesViewModel(), document.getElementById('expenses-vm'));
