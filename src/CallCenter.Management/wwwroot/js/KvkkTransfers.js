function TransfersViewModel() {
    var self = this;
    self.items = ko.observableArray([]);
    self.isLoading = ko.observable(false);
    self.isSaving = ko.observable(false);
    self.formTitle = ko.observable('Yeni Aktarim');

    self.form = {
        uid: ko.observable(null), targetCountry: ko.observable(''),
        organization: ko.observable(''), purpose: ko.observable(''), legalBasis: ko.observable('')
    };

    self.loadData = function() {
        self.isLoading(true);
        $.get('/proxy/kvkk/transfers', function(data) {
            self.items(Array.isArray(data) ? data : (data.items || data.data || []));
        }).always(function() { self.isLoading(false); });
    };

    self.openCreate = function() {
        self.form.uid(null); self.form.targetCountry(''); self.form.organization('');
        self.form.purpose(''); self.form.legalBasis('');
        self.formTitle('Yeni Aktarim');
        new bootstrap.Modal('#transferModal').show();
    };

    self.openEdit = function(item) {
        self.form.uid(item.uid || item.id); self.form.targetCountry(item.targetCountry || '');
        self.form.organization(item.organization || '');
        self.form.purpose(item.purpose || ''); self.form.legalBasis(item.legalBasis || '');
        self.formTitle('Aktarim Duzenle');
        new bootstrap.Modal('#transferModal').show();
    };

    self.save = function() {
        if (!self.form.targetCountry() || !self.form.organization()) { toastr.warning('Ulke ve kurulus zorunludur.'); return; }
        self.isSaving(true);
        var payload = {
            targetCountry: self.form.targetCountry(), organization: self.form.organization(),
            purpose: self.form.purpose(), legalBasis: self.form.legalBasis()
        };
        var method = self.form.uid() ? 'PUT' : 'POST';
        var url = self.form.uid() ? '/proxy/kvkk/transfers/' + self.form.uid() : '/proxy/kvkk/transfers';
        $.ajax({
            url: url, method: method, contentType: 'application/json', data: JSON.stringify(payload),
            success: function() {
                toastr.success('Kaydedildi.'); bootstrap.Modal.getInstance(document.getElementById('transferModal')).hide();
                self.loadData();
            },
            error: function() { toastr.error('Kaydetme hatasi.'); }
        }).always(function() { self.isSaving(false); });
    };

    self.loadData();
}

ko.applyBindings(new TransfersViewModel(), document.getElementById('transfers-vm'));
