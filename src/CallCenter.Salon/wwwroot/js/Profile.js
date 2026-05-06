function slnJsT(key, fallback) {
    return (window.salonT || function (k, f) { return f || k; })(key, fallback);
}

function ProfileViewModel() {
    var self = this;
    self.isSaving = ko.observable(false);

    self.form = {
        description: ko.observable(''),
        website: ko.observable(''),
        instagramHandle: ko.observable(''),
        facebookUrl: ko.observable(''),
        isPublished: ko.observable('true'),
        billingType: ko.observable('1')
    };

    self.loadProfile = function () {
        $.ajax({ url: '/proxy/sln-profile', method: 'GET' }).done(function (data) {
            if (data.exists === false) {
                self.form.billingType(String(data.billingType || 1));
                return;
            }
            self.form.description(data.description || '');
            self.form.website(data.website || '');
            self.form.instagramHandle(data.instagramHandle || '');
            self.form.facebookUrl(data.facebookUrl || '');
            self.form.isPublished(data.isPublished ? 'true' : 'false');
            self.form.billingType(String(data.billingType || 1));
        });
    };

    self.save = function () {
        var data = {
            description: self.form.description(),
            website: self.form.website(),
            instagramHandle: self.form.instagramHandle(),
            facebookUrl: self.form.facebookUrl(),
            isPublished: self.form.isPublished() === 'true',
            billingType: parseInt(self.form.billingType()) || 1
        };

        self.isSaving(true);
        $.ajax({
            url: '/proxy/sln-profile', method: 'POST',
            contentType: 'application/json', data: JSON.stringify(data)
        }).done(function () {
            toastr.success(slnJsT('salon.profile.saved', 'Profil kaydedildi'));
            self.isSaving(false);
        }).fail(function (xhr) {
            toastr.error(xhr.responseJSON?.error || xhr.responseText || slnJsT('salon.common.error.save_failed', 'Kaydedilemedi'));
            self.isSaving(false);
        });
    };

    $(document).ready(function () { self.loadProfile(); });
}

ko.applyBindings(new ProfileViewModel(), document.getElementById('profile-vm'));
