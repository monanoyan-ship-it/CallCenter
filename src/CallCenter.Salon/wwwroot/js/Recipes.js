function RecipesViewModel() {
    var self = this;
    self.recipes = ko.observableArray([]);
    self.serviceList = ko.observableArray([]);
    self.searchQuery = ko.observable('');
    self.isEditing = ko.observable(false);
    self.editingId = ko.observable(null);
    self.isSaving = ko.observable(false);

    self.form = {
        name: ko.observable(''),
        description: ko.observable(''),
        iconClass: ko.observable(''),
        items: ko.observableArray([])
    };

    self.filteredRecipes = ko.computed(function () {
        var q = (self.searchQuery() || '').toLowerCase();
        if (!q) return self.recipes();
        return self.recipes().filter(function (r) {
            return (r.name || '').toLowerCase().indexOf(q) >= 0
                || (r.description || '').toLowerCase().indexOf(q) >= 0;
        });
    });

    self.calculatedPrice = ko.computed(function () {
        var total = 0;
        self.form.items().forEach(function (item) {
            var svc = self.serviceList().find(function (s) { return s.id == item.serviceId(); });
            if (svc) total += svc.price * (parseInt(item.quantity()) || 1);
        });
        return total;
    });

    self.calculatedDuration = ko.computed(function () {
        var total = 0;
        self.form.items().forEach(function (item) {
            var svc = self.serviceList().find(function (s) { return s.id == item.serviceId(); });
            if (svc) total += svc.durationMinutes * (parseInt(item.quantity()) || 1);
        });
        return total;
    });

    self.getItemTotal = function (item) {
        var svc = self.serviceList().find(function (s) { return s.id == item.serviceId(); });
        if (!svc) return '-';
        var qty = parseInt(item.quantity()) || 1;
        return (svc.price * qty).toLocaleString('tr-TR') + ' TL';
    };

    var formModal;

    self.loadData = function () {
        $.ajax({ url: '/proxy/sln-recipes', method: 'GET' }).done(function (data) {
            self.recipes(data.items || data);
        });
    };

    self.loadServices = function () {
        $.ajax({ url: '/proxy/sln-services', method: 'GET' }).done(function (data) {
            self.serviceList(data.items || data);
        });
    };

    self.resetForm = function () {
        self.form.name('');
        self.form.description('');
        self.form.iconClass('');
        self.form.items([]);
        self.isEditing(false);
        self.editingId(null);
    };

    self.openNew = function () {
        self.resetForm();
        self.addItem();
        formModal.show();
    };

    self.openEdit = function (recipe) {
        self.isEditing(true);
        self.editingId(recipe.id);
        self.form.name(recipe.name || '');
        self.form.description(recipe.description || '');
        self.form.iconClass(recipe.iconClass || '');
        var items = (recipe.items || []).map(function (item) {
            return {
                serviceId: ko.observable(item.serviceId),
                quantity: ko.observable(item.quantity || 1)
            };
        });
        self.form.items(items.length > 0 ? items : []);
        if (items.length === 0) self.addItem();
        formModal.show();
    };

    self.addItem = function () {
        self.form.items.push({
            serviceId: ko.observable(null),
            quantity: ko.observable(1)
        });
    };

    self.removeItem = function (item) {
        self.form.items.remove(item);
    };

    self.save = function () {
        var items = [];
        var sortOrder = 1;
        self.form.items().forEach(function (item) {
            if (item.serviceId()) {
                items.push({
                    serviceId: parseInt(item.serviceId()),
                    quantity: parseInt(item.quantity()) || 1,
                    sortOrder: sortOrder++
                });
            }
        });

        var data = {
            name: self.form.name(),
            description: self.form.description(),
            iconClass: self.form.iconClass(),
            isActive: true,
            items: items
        };

        if (!data.name) { toastr.warning('Recete adi zorunludur'); return; }
        if (items.length === 0) { toastr.warning('En az bir hizmet ekleyiniz'); return; }

        self.isSaving(true);
        var url = '/proxy/sln-recipes';
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
            toastr.success(self.isEditing() ? 'Recete guncellendi' : 'Recete olusturuldu');
            self.isSaving(false);
        }).fail(function (xhr) {
            toastr.error(xhr.responseJSON?.error || 'Bir hata olustu');
            self.isSaving(false);
        });
    };

    self.remove = function (recipe) {
        if (!confirm("'" + recipe.name + "' recetesini silmek istediginize emin misiniz?")) return;
        $.ajax({ url: '/proxy/sln-recipes/' + recipe.id, method: 'DELETE' }).done(function () {
            self.loadData();
            toastr.success('Recete silindi');
        });
    };

    $(document).ready(function () {
        formModal = new bootstrap.Modal(document.getElementById('recipeModal'));
        self.loadServices();
        self.loadData();
    });
}

ko.applyBindings(new RecipesViewModel(), document.getElementById('recipes-vm'));
