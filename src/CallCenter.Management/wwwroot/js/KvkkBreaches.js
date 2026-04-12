function BreachesViewModel() {
    var self = this;
    self.items = ko.observableArray([]);
    self.isLoading = ko.observable(false);
    self.isSaving = ko.observable(false);
    self.formTitle = ko.observable('Yeni Ihlal Bildirimi');

    self.form = {
        uid: ko.observable(null), breachDate: ko.observable(''), breachType: ko.observable(''),
        affectedRecordCount: ko.observable(0), description: ko.observable(''), status: ko.observable('Acik')
    };

    self.loadData = function() {
        self.isLoading(true);
        $.get('/proxy/kvkk/breaches', function(data) {
            self.items(Array.isArray(data) ? data : (data.items || data.data || []));
        }).always(function() { self.isLoading(false); });
    };

    self.openCreate = function() {
        self.form.uid(null); self.form.breachDate(''); self.form.breachType('');
        self.form.affectedRecordCount(0); self.form.description(''); self.form.status('Acik');
        self.formTitle('Yeni Ihlal Bildirimi');
        new bootstrap.Modal('#breachModal').show();
    };

    self.openEdit = function(item) {
        self.form.uid(item.uid || item.id);
        self.form.breachDate(item.breachDate ? item.breachDate.substring(0, 10) : '');
        self.form.breachType(item.breachType || '');
        self.form.affectedRecordCount(item.affectedRecordCount || 0);
        self.form.description(item.description || '');
        self.form.status(item.status || 'Acik');
        self.formTitle('Ihlal Duzenle');
        new bootstrap.Modal('#breachModal').show();
    };

    self.save = function() {
        if (!self.form.breachDate() || !self.form.breachType()) { toastr.warning('Tarih ve tur zorunludur.'); return; }
        self.isSaving(true);
        var payload = {
            breachDate: self.form.breachDate(), breachType: self.form.breachType(),
            affectedRecordCount: parseInt(self.form.affectedRecordCount()),
            description: self.form.description(), status: self.form.status()
        };
        var method = self.form.uid() ? 'PUT' : 'POST';
        var url = self.form.uid() ? '/proxy/kvkk/breaches/' + self.form.uid() : '/proxy/kvkk/breaches';
        $.ajax({
            url: url, method: method, contentType: 'application/json', data: JSON.stringify(payload),
            success: function() {
                toastr.success('Kaydedildi.'); bootstrap.Modal.getInstance(document.getElementById('breachModal')).hide();
                self.loadData();
            },
            error: function() { toastr.error('Kaydetme hatasi.'); }
        }).always(function() { self.isSaving(false); });
    };

    self.loadData();
}

ko.applyBindings(new BreachesViewModel(), document.getElementById('breaches-vm'));
