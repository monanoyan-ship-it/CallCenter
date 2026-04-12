function BranchesViewModel() {
    var self = this;
    self.branches = ko.observableArray([]);
    self.isEditing = ko.observable(false);
    self.editingId = ko.observable(null);
    self.isSaving = ko.observable(false);

    self.staffList = ko.observableArray([]);

    var dayLabels = [
        { key: 'mon', label: 'Pazartesi' }, { key: 'tue', label: 'Sali' },
        { key: 'wed', label: 'Carsamba' }, { key: 'thu', label: 'Persembe' },
        { key: 'fri', label: 'Cuma' }, { key: 'sat', label: 'Cumartesi' },
        { key: 'sun', label: 'Pazar' }
    ];

    self.workingDays = dayLabels.map(function (d) {
        return {
            key: d.key,
            label: d.label,
            isOpen: ko.observable(d.key !== 'sun'),
            open: ko.observable('09:00'),
            close: ko.observable('19:00')
        };
    });

    self.form = {
        name: ko.observable(''),
        slug: ko.observable(''),
        address: ko.observable(''),
        city: ko.observable(''),
        district: ko.observable(''),
        phone: ko.observable(''),
        email: ko.observable(''),
        googleMapsUrl: ko.observable(''),
        managerPersonnelId: ko.observable(''),
        isActive: ko.observable(true),
        isHeadquarter: ko.observable(false),
        companyTitle: ko.observable(''),
        taxOffice: ko.observable(''),
        taxNumber: ko.observable(''),
        mersisNo: ko.observable('')
    };

    var formModal;

    function buildWorkingHoursJson() {
        var hours = {};
        self.workingDays.forEach(function (d) {
            if (d.isOpen() && d.open() && d.close()) {
                hours[d.key] = d.open() + '-' + d.close();
            } else {
                hours[d.key] = 'closed';
            }
        });
        return JSON.stringify(hours);
    }

    function parseWorkingHours(json) {
        self.workingDays.forEach(function (d) {
            d.isOpen(d.key !== 'sun');
            d.open('09:00');
            d.close('19:00');
        });
        if (!json) return;
        try {
            var hours = JSON.parse(json);
            self.workingDays.forEach(function (d) {
                if (hours[d.key]) {
                    if (hours[d.key] === 'closed') {
                        d.isOpen(false);
                    } else {
                        d.isOpen(true);
                        var parts = hours[d.key].split('-');
                        if (parts.length === 2) { d.open(parts[0]); d.close(parts[1]); }
                    }
                }
            });
        } catch (e) {}
    }

    self.loadData = function () {
        $.ajax({ url: '/proxy/sln-branches', method: 'GET' }).done(function (data) {
            self.branches(data || []);
        }).fail(function () {
            toastr.error('Subeler yuklenemedi');
        });
    };

    self.loadStaff = function () {
        $.get('/proxy/portal/personnel', function (d) { self.staffList(d.items || d || []); });
    };

    self.resetForm = function () {
        self.form.name('');
        self.form.slug('');
        self.form.address('');
        self.form.city('');
        self.form.district('');
        self.form.phone('');
        self.form.email('');
        self.form.googleMapsUrl('');
        self.form.managerPersonnelId('');
        self.form.isActive(true);
        self.form.isHeadquarter(false);
        self.form.companyTitle('');
        self.form.taxOffice('');
        self.form.taxNumber('');
        self.form.mersisNo('');
        parseWorkingHours(null);
        self.isEditing(false);
        self.editingId(null);
    };

    self.openNew = function () {
        self.resetForm();
        formModal.show();
    };

    self.openEdit = function (branch) {
        self.isEditing(true);
        self.editingId(branch.id);
        self.form.name(branch.name || '');
        self.form.slug(branch.slug || '');
        self.form.address(branch.address || '');
        self.form.city(branch.city || '');
        self.form.district(branch.district || '');
        self.form.phone(branch.phone || '');
        self.form.email(branch.email || '');
        self.form.googleMapsUrl(branch.googleMapsUrl || '');
        self.form.managerPersonnelId(branch.managerPersonnelId || '');
        self.form.isActive(branch.isActive);
        self.form.isHeadquarter(branch.isHeadquarter || false);
        self.form.companyTitle(branch.companyTitle || '');
        self.form.taxOffice(branch.taxOffice || '');
        self.form.taxNumber(branch.taxNumber || '');
        self.form.mersisNo(branch.mersisNo || '');
        parseWorkingHours(branch.workingHoursJson);
        formModal.show();
    };

    self.save = function () {
        var data = {
            name: self.form.name(),
            slug: self.form.slug(),
            address: self.form.address(),
            city: self.form.city(),
            district: self.form.district(),
            phone: self.form.phone(),
            email: self.form.email(),
            googleMapsUrl: self.form.googleMapsUrl(),
            workingHoursJson: buildWorkingHoursJson(),
            managerPersonnelId: self.form.managerPersonnelId() ? parseInt(self.form.managerPersonnelId()) : null,
            isActive: self.form.isActive(),
            isHeadquarter: self.form.isHeadquarter(),
            companyTitle: self.form.companyTitle(),
            taxOffice: self.form.taxOffice(),
            taxNumber: self.form.taxNumber(),
            mersisNo: self.form.mersisNo()
        };

        if (!data.name) {
            toastr.warning('Sube adi zorunludur');
            return;
        }

        self.isSaving(true);
        var url = '/proxy/sln-branches';
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
            toastr.success(self.isEditing() ? 'Sube guncellendi' : 'Sube eklendi');
            self.isSaving(false);
        }).fail(function (xhr) {
            toastr.error(xhr.responseJSON || 'Bir hata olustu');
            self.isSaving(false);
        });
    };

    self.remove = function (branch) {
        confirmModal('Onay', branch.name + ' subesini silmek istediginize emin misiniz?', function() {
            $.ajax({
                url: '/proxy/sln-branches/' + branch.id,
                method: 'DELETE'
            }).done(function () {
                self.loadData();
                toastr.success('Sube silindi');
            }).fail(function () {
                toastr.error('Silinemedi');
            });
        });
    };

    $(document).ready(function () {
        formModal = new bootstrap.Modal(document.getElementById('branchModal'));
        self.loadStaff();
        self.loadData();
    });
}

ko.applyBindings(new BranchesViewModel(), document.getElementById('branches-vm'));
