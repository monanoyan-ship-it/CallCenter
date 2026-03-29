function ProfileViewModel() {
    var self = this;
    self.isSaving = ko.observable(false);

    var dayLabels = [
        { key: 'mon', label: 'Pazartesi' }, { key: 'tue', label: 'Sali' },
        { key: 'wed', label: 'Carsamba' }, { key: 'thu', label: 'Persembe' },
        { key: 'fri', label: 'Cuma' }, { key: 'sat', label: 'Cumartesi' },
        { key: 'sun', label: 'Pazar' }
    ];

    self.workingDays = dayLabels.map(function (d) {
        return { key: d.key, label: d.label, open: ko.observable('09:00'), close: ko.observable('19:00') };
    });

    self.form = {
        slug: ko.observable(''),
        description: ko.observable(''),
        address: ko.observable(''),
        city: ko.observable(''),
        district: ko.observable(''),
        phone: ko.observable(''),
        email: ko.observable(''),
        website: ko.observable(''),
        instagramHandle: ko.observable(''),
        facebookUrl: ko.observable(''),
        googleMapsUrl: ko.observable(''),
        isPublished: ko.observable('true')
    };

    self.loadProfile = function () {
        $.ajax({ url: '/proxy/sln-profile', method: 'GET' }).done(function (data) {
            if (data.exists === false) return;
            self.form.slug(data.slug || '');
            self.form.description(data.description || '');
            self.form.address(data.address || '');
            self.form.city(data.city || '');
            self.form.district(data.district || '');
            self.form.phone(data.phone || '');
            self.form.email(data.email || '');
            self.form.website(data.website || '');
            self.form.instagramHandle(data.instagramHandle || '');
            self.form.facebookUrl(data.facebookUrl || '');
            self.form.googleMapsUrl(data.googleMapsUrl || '');
            self.form.isPublished(data.isPublished ? 'true' : 'false');

            if (data.workingHoursJson) {
                try {
                    var hours = JSON.parse(data.workingHoursJson);
                    self.workingDays.forEach(function (d) {
                        if (hours[d.key]) {
                            var parts = hours[d.key].split('-');
                            if (parts.length === 2) { d.open(parts[0]); d.close(parts[1]); }
                        }
                    });
                } catch (e) {}
            }
        });
    };

    self.save = function () {
        var hours = {};
        self.workingDays.forEach(function (d) {
            if (d.open() && d.close()) hours[d.key] = d.open() + '-' + d.close();
        });

        var data = {
            slug: self.form.slug(),
            description: self.form.description(),
            address: self.form.address(),
            city: self.form.city(),
            district: self.form.district(),
            phone: self.form.phone(),
            email: self.form.email(),
            website: self.form.website(),
            instagramHandle: self.form.instagramHandle(),
            facebookUrl: self.form.facebookUrl(),
            googleMapsUrl: self.form.googleMapsUrl(),
            workingHoursJson: JSON.stringify(hours),
            isPublished: self.form.isPublished() === 'true'
        };

        if (!data.slug) { toastr.warning('Profil adresi (slug) zorunludur'); return; }

        self.isSaving(true);
        $.ajax({
            url: '/proxy/sln-profile', method: 'POST',
            contentType: 'application/json', data: JSON.stringify(data)
        }).done(function () {
            toastr.success('Profil kaydedildi');
            self.isSaving(false);
        }).fail(function (xhr) {
            toastr.error(xhr.responseJSON?.error || xhr.responseText || 'Kaydedilemedi');
            self.isSaving(false);
        });
    };

    $(document).ready(function () { self.loadProfile(); });
}

ko.applyBindings(new ProfileViewModel(), document.getElementById('profile-vm'));
