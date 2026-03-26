function ExpensesViewModel() {
    var self = this;
    self.expenses = ko.observableArray([]);
    self.expenseCategories = ko.observableArray([]);
    self.supplierList = ko.observableArray([]);
    self.searchQuery = ko.observable('');
    self.selectedCategoryId = ko.observable(null);
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
        paymentMethod: ko.observable('Cash'),
        supplierId: ko.observable(null),
        notes: ko.observable('')
    };

    // ═══ Autocomplete'ler ═══
    self.categoryAutocomplete = createAutocomplete(self.expenseCategories, 'name', self.form.categoryId);
    self.supplierAutocomplete = createAutocomplete(self.supplierList, 'name', self.form.supplierId);

    self.filteredExpenses = ko.computed(function () {
        var q = (self.searchQuery() || '').toLowerCase();
        var catId = self.selectedCategoryId();
        var start = self.filterStartDate();
        var end = self.filterEndDate();

        return self.expenses().filter(function (e) {
            var matchQ = !q || (e.description || '').toLowerCase().indexOf(q) >= 0
                || (e.categoryName || '').toLowerCase().indexOf(q) >= 0;
            var matchCat = !catId || e.categoryId == catId;
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

    self.loadData = function () {
        var url = '/proxy/sln-expenses';
        var params = [];
        if (self.filterStartDate()) params.push('startDate=' + self.filterStartDate());
        if (self.filterEndDate()) params.push('endDate=' + self.filterEndDate());
        if (params.length) url += '?' + params.join('&');

        $.ajax({ url: url, method: 'GET' }).done(function (data) {
            var items = data.items || data;
            items.forEach(function (e) {
                e.paymentMethodText = { Cash: 'Nakit', CreditCard: 'Kredi Karti', BankTransfer: 'Havale/EFT' }[e.paymentMethod] || e.paymentMethod || '-';
            });
            self.expenses(items);
        }).fail(function () {
            toastr.error('Masraflar yuklenemedi');
        });
    };

    self.loadLookups = function () {
        $.ajax({ url: '/proxy/sln-expense-categories', method: 'GET' }).done(function (data) {
            self.expenseCategories(data);
        });
        $.ajax({ url: '/proxy/sln-suppliers', method: 'GET' }).done(function (data) {
            self.supplierList(data.items || data);
        });
    };

    self.resetForm = function () {
        self.form.expenseDate(new Date().toISOString().substring(0, 10));
        self.form.categoryId(null);
        self.form.description('');
        self.form.amount(0);
        self.form.paymentMethod('Cash');
        self.form.supplierId(null);
        self.form.notes('');
        self.isEditing(false);
        self.editingId(null);
        self.categoryAutocomplete.clear();
        self.supplierAutocomplete.clear();
    };

    self.openNew = function () {
        self.resetForm();
        formModal.show();
    };

    self.openEdit = function (expense) {
        self.isEditing(true);
        self.editingId(expense.id);
        self.form.expenseDate(expense.expenseDate ? expense.expenseDate.substring(0, 10) : '');
        self.form.categoryId(expense.categoryId);
        self.form.description(expense.description || '');
        self.form.amount(expense.amount || 0);
        self.form.paymentMethod(expense.paymentMethod || 'Cash');
        self.form.supplierId(expense.supplierId);
        self.form.notes(expense.notes || '');
        // Autocomplete'lere mevcut degerleri set et
        self.categoryAutocomplete.setFromValue(expense.categoryId);
        self.supplierAutocomplete.setFromValue(expense.supplierId);
        formModal.show();
    };

    self.save = function () {
        var data = {
            expenseDate: self.form.expenseDate(),
            categoryId: self.form.categoryId() || null,
            description: self.form.description(),
            amount: parseFloat(self.form.amount()) || 0,
            paymentMethod: self.form.paymentMethod(),
            supplierId: self.form.supplierId() || null,
            notes: self.form.notes()
        };

        if (!data.description || !data.amount) {
            toastr.warning('Aciklama ve tutar zorunludur');
            return;
        }

        self.isSaving(true);
        var url = '/proxy/sln-expenses';
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

    self.remove = function (expense) {
        if (!confirm('Bu masrafi silmek istediginize emin misiniz?')) return;
        $.ajax({ url: '/proxy/sln-expenses/' + expense.id, method: 'DELETE' }).done(function () {
            self.loadData();
            toastr.success('Masraf silindi');
        }).fail(function () {
            toastr.error('Masraf silinemedi');
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
