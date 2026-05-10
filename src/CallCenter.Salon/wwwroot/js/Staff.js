function slnJsT(key, fallback) {
    return (window.salonT || function (k, f) { return f || k; })(key, fallback);
}

function StaffViewModel() {
    var self = this;
    self.staffList = ko.observableArray([]);
    self.roleList = ko.observableArray([]);
    self.searchQuery = ko.observable('');
    self.isEditing = ko.observable(false);
    self.isOwnerEdit = ko.observable(false); // Salon sahibi editleniyorsa true — sadece temel alanlar gosterilir
    self.editingId = ko.observable(null);
    self.isSaving = ko.observable(false);

    self.branchList = ko.observableArray([]);
    self.serviceCategories = ko.observableArray([]);

    self.usernameAvailable = ko.observable(null); // null=kontrol edilmedi, true=musait, false=alinmis
    self.usernameChecking = ko.observable(false);
    var usernameCheckTimer = null;

    self.form = {
        userName: ko.observable(''),
        fullName: ko.observable(''),
        email: ko.observable(''),
        password: ko.observable(''),
        title: ko.observable(''),
        customerRoleId: ko.observable(103),
        branchId: ko.observable(null),
        skillServiceIds: ko.observableArray([]),
        isActive: ko.observable('true'),
        photoUrl: ko.observable(''),
        publicVisible: ko.observable(true),
        publicShowFullName: ko.observable(true),
        publicShowPhoto: ko.observable(true),
        publicShowTitle: ko.observable(true),
        publicShowSpecialty: ko.observable(true)
    };

    // Personel calisma saatleri (null/bos -> sube saatleri kullanilir)
    var dayLabels = [
        { key: 'mon', label: 'Pazartesi' }, { key: 'tue', label: 'Salı' },
        { key: 'wed', label: 'Çarşamba' }, { key: 'thu', label: 'Perşembe' },
        { key: 'fri', label: 'Cuma' }, { key: 'sat', label: 'Cumartesi' },
        { key: 'sun', label: 'Pazar' }
    ];
    self.workingHoursOverride = ko.observable(false);
    self.workingDays = dayLabels.map(function (d) {
        return {
            key: d.key,
            label: d.label,
            isOpen: ko.observable(d.key !== 'sun'),
            open: ko.observable('09:00'),
            close: ko.observable('19:00')
        };
    });

    function buildPersonnelWorkingHoursJson() {
        if (!self.workingHoursOverride()) return null;
        var hours = {};
        self.workingDays.forEach(function (d) {
            if (d.isOpen() && d.open() && d.close()) hours[d.key] = d.open() + '-' + d.close();
            else hours[d.key] = 'closed';
        });
        return JSON.stringify(hours);
    }

    function parsePersonnelWorkingHours(json) {
        self.workingDays.forEach(function (d) {
            d.isOpen(d.key !== 'sun'); d.open('09:00'); d.close('19:00');
        });
        if (!json) {
            self.workingHoursOverride(false);
            return;
        }
        self.workingHoursOverride(true);
        try {
            var hours = JSON.parse(json);
            self.workingDays.forEach(function (d) {
                if (hours[d.key]) {
                    if (hours[d.key] === 'closed') d.isOpen(false);
                    else {
                        d.isOpen(true);
                        var parts = hours[d.key].split('-');
                        if (parts.length === 2) { d.open(parts[0]); d.close(parts[1]); }
                    }
                }
            });
        } catch (e) {}
    }
    self.buildPersonnelWorkingHoursJson = buildPersonnelWorkingHoursJson;
    self.parsePersonnelWorkingHours = parsePersonnelWorkingHours;

    self.isUploadingPhoto = ko.observable(false);

    self.uploadPhoto = function (data, event) {
        var file = event.target.files[0];
        if (!file) return;
        if (file.size > 3 * 1024 * 1024) { toastr.warning(slnJsT('salon.staff.js.photo_too_large', 'Dosya 3 MB’den büyük olamaz.')); return; }

        var staffId = self.editingId();
        if (!staffId) { toastr.warning(slnJsT('salon.staff.js.save_before_photo', 'Önce personeli kaydedin, sonra fotoğraf yükleyin.')); return; }

        var formData = new FormData();
        formData.append('file', file);

        self.isUploadingPhoto(true);
        $.ajax({
            url: '/proxy/portal/personnel/' + staffId + '/upload-photo',
            method: 'POST',
            data: formData,
            processData: false,
            contentType: false
        }).done(function (result) {
            self.form.photoUrl(result.url);
            toastr.success(slnJsT('salon.staff.js.photo_uploaded', 'Fotoğraf yüklendi.'));
        }).fail(function (xhr) {
            toastr.error(xhr.responseJSON?.message || slnJsT('salon.staff.js.upload_error', 'Yükleme hatası.'));
        }).always(function () {
            self.isUploadingPhoto(false);
            event.target.value = '';
        });
    };

    self.toggleSkill = function (serviceId) {
        var ids = self.form.skillServiceIds().slice();
        var idx = ids.indexOf(serviceId);
        if (idx >= 0) ids.splice(idx, 1);
        else ids.push(serviceId);
        self.form.skillServiceIds(ids);
    };

    self.filteredStaff = ko.computed(function () {
        var q = (self.searchQuery() || '').toLowerCase();
        if (!q) return self.staffList();
        return self.staffList().filter(function (s) {
            return (s.fullName || '').toLowerCase().indexOf(q) >= 0
                || (s.email || '').toLowerCase().indexOf(q) >= 0
                || (s.title || '').toLowerCase().indexOf(q) >= 0;
        });
    });

    self.activeCount = ko.computed(function () {
        return self.staffList().filter(function (s) { return s.isActive; }).length;
    });

    self.form.userName.subscribe(function (val) {
        self.usernameAvailable(null);
        if (usernameCheckTimer) clearTimeout(usernameCheckTimer);
        if (!val || val.length < 3 || self.isEditing()) return;
        self.usernameChecking(true);
        usernameCheckTimer = setTimeout(function () {
            $.get('/proxy/portal/personnel/check-username?username=' + encodeURIComponent(val), function (res) {
                self.usernameAvailable(res.available);
                self.usernameChecking(false);
            }).fail(function () { self.usernameChecking(false); });
        }, 500);
    });

    var formModal;

    function read(obj, camel, pascal) {
        if (!obj) return undefined;
        if (obj[camel] !== undefined && obj[camel] !== null) return obj[camel];
        return obj[pascal];
    }

    self.loadData = function () {
        $.ajax({ url: '/proxy/portal/personnel', method: 'GET' }).done(function (data) {
            self.staffList(data.items || data);
        }).fail(function () {
            toastr.error(slnJsT('salon.staff.js.personel_listesi_yuklenemedi', 'Personel listesi yüklenemedi'));
        });
    };

    self.loadRoles = function () {
        $.ajax({ url: '/proxy/portal/salon-roles', method: 'GET' }).done(function (data) {
            self.roleList(data);
        });
    };

    self.loadBranches = function () {
        $.get('/proxy/sln-branches', function (data) {
            self.branchList(data || []);
            if (data && data.length === 1) self.form.branchId(data[0].id);
        });
    };

    self.loadServices = function () {
        $.get('/proxy/sln-services/categories', function (data) {
            self.serviceCategories(data || []);
        });
    };

    self.resetForm = function () {
        self.form.userName('');
        self.form.fullName('');
        self.form.email('');
        self.form.password('');
        self.form.title('');
        self.form.customerRoleId(103);
        self.form.branchId(self.branchList().length === 1 ? self.branchList()[0].id : null);
        self.form.skillServiceIds([]);
        self.form.isActive('true');
        self.form.photoUrl('');
        self.form.publicVisible(true);
        self.form.publicShowFullName(true);
        self.form.publicShowPhoto(true);
        self.form.publicShowTitle(true);
        self.form.publicShowSpecialty(true);
        parsePersonnelWorkingHours(null);
        self.isEditing(false);
        self.isOwnerEdit(false);
        self.editingId(null);
    };

    self.openNew = function () {
        self.resetForm();
        formModal.show();
    };

    self.openEdit = function (staff) {
        var roleId = Number(read(staff, 'customerRoleId', 'CustomerRoleId') || 103);
        self.isEditing(true);
        self.isOwnerEdit(roleId === 101); // SalonOwner = 101
        self.editingId(read(staff, 'id', 'Id'));
        self.form.userName(read(staff, 'userName', 'UserName') || '');
        self.form.fullName(read(staff, 'fullName', 'FullName') || '');
        self.form.email(read(staff, 'email', 'Email') || '');
        self.form.password('');
        self.form.title(read(staff, 'title', 'Title') || '');
        self.form.customerRoleId(roleId);
        self.form.branchId(read(staff, 'branchId', 'BranchId') || null);
        self.form.skillServiceIds(read(staff, 'skillServiceIds', 'SkillServiceIds') || []);
        self.form.isActive(read(staff, 'isActive', 'IsActive') ? 'true' : 'false');
        self.form.photoUrl(read(staff, 'photoUrl', 'PhotoUrl') || '');
        self.form.publicVisible(read(staff, 'publicVisible', 'PublicVisible') !== false);
        self.form.publicShowFullName(read(staff, 'publicShowFullName', 'PublicShowFullName') !== false);
        self.form.publicShowPhoto(read(staff, 'publicShowPhoto', 'PublicShowPhoto') !== false);
        self.form.publicShowTitle(read(staff, 'publicShowTitle', 'PublicShowTitle') !== false);
        self.form.publicShowSpecialty(read(staff, 'publicShowSpecialty', 'PublicShowSpecialty') !== false);
        parsePersonnelWorkingHours(read(staff, 'workingHoursJson', 'WorkingHoursJson') || null);
        formModal.show();
    };

    function validatePassword(pwd) {
        var errors = [];
        if (pwd.length < 8) errors.push(slnJsT('salon.staff.password.min_length', 'En az 8 karakter'));
        if (!/[A-Z]/.test(pwd)) errors.push(slnJsT('salon.staff.password.uppercase', 'En az 1 büyük harf'));
        if (!/[a-z]/.test(pwd)) errors.push(slnJsT('salon.staff.password.lowercase', 'En az 1 küçük harf'));
        if (!/[0-9]/.test(pwd)) errors.push(slnJsT('salon.staff.password.digit', 'En az 1 rakam'));
        return errors;
    }

    self.save = function () {
        var data = {
            fullName: self.form.fullName(),
            email: self.form.email(),
            title: self.form.title(),
            customerRoleId: parseInt(self.form.customerRoleId()) || 103,
            branchId: self.form.branchId() ? parseInt(self.form.branchId()) : null,
            skillServiceIds: self.form.skillServiceIds(),
            isActive: self.form.isActive() === 'true',
            publicVisible: self.form.publicVisible(),
            publicShowFullName: self.form.publicShowFullName(),
            publicShowPhoto: self.form.publicShowPhoto(),
            publicShowTitle: self.form.publicShowTitle(),
            publicShowSpecialty: self.form.publicShowSpecialty(),
            workingHoursJson: buildPersonnelWorkingHoursJson()
        };

        if (!data.fullName || !data.email) {
            toastr.warning(slnJsT('salon.staff.js.name_email_required', 'Ad soyad ve e-posta zorunludur'));
            return;
        }

        if (self.branchList().length > 1 && !data.branchId) {
            toastr.warning(slnJsT('salon.staff.js.sube_secimi_zorunludur', 'Şube seçimi zorunludur'));
            return;
        }

        if (!self.isEditing()) {
            data.userName = self.form.userName();
            data.password = self.form.password();
            if (!data.userName) { toastr.warning(slnJsT('salon.staff.js.username_required', 'Kullanıcı adı zorunludur')); return; }
            if (self.usernameAvailable() === false) { toastr.warning(slnJsT('salon.staff.js.username_taken', 'Bu kullanıcı adı zaten kullanılıyor')); return; }
            if (!data.password) { toastr.warning(slnJsT('salon.staff.js.sifre_zorunludur', 'Şifre zorunludur')); return; }
            if (data.userName.length < 3) { toastr.warning(slnJsT('salon.staff.js.username_min_length', 'Kullanıcı adı en az 3 karakter olmalı')); return; }

            var pwdErrors = validatePassword(data.password);
            if (pwdErrors.length > 0) {
                toastr.warning(slnJsT('salon.staff.js.sifre_gereksinimleri_n', 'Şifre gereksinimleri:\\n') + pwdErrors.join(', '));
                return;
            }
        } else {
            var pwd = self.form.password();
            if (pwd) {
                var pwdErrors = validatePassword(pwd);
                if (pwdErrors.length > 0) {
                    toastr.warning(slnJsT('salon.staff.js.sifre_gereksinimleri_n', 'Şifre gereksinimleri:\\n') + pwdErrors.join(', '));
                    return;
                }
                data.password = pwd;
            }
        }

        self.isSaving(true);
        var url = '/proxy/portal/personnel';
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
            toastr.success(self.isEditing() ? slnJsT('salon.staff.js.personel_guncellendi', 'Personel güncellendi') : slnJsT('salon.staff.js.personel_eklendi', 'Personel eklendi'));
            self.isSaving(false);
        }).fail(function (xhr) {
            var msg = xhr.responseJSON?.message || xhr.responseJSON?.error || xhr.responseJSON;
            if (typeof msg === 'string') {
                // Backend hata mesajlarini kullanici dostu hale getir
                if (msg.indexOf('kullanici adi') >= 0) toastr.error(slnJsT('salon.staff.js.username_taken_try_another', 'Bu kullanıcı adı zaten kullanılıyor. Farklı bir isim deneyin.'));
                else if (msg.indexOf('e-posta') >= 0) toastr.error(slnJsT('salon.staff.js.email_taken', 'Bu e-posta adresi zaten kullanılıyor.'));
                else if (msg.indexOf(slnJsT('salon.staff.js.limit', 'limit')) >= 0) toastr.error(slnJsT('salon.staff.js.maksimum_personel_limitine_ulasildi', 'Maksimum personel limitine ulaşıldı.'));
                else toastr.error(msg);
            } else {
                toastr.error(slnJsT('salon.common.error.generic', 'Bir hata oluştu'));
            }
            self.isSaving(false);
        });
    };

    self.resetPassword = function (staff) {
        confirmModal(slnJsT('salon.staff.js.sifre_sifirla', 'Şifre Sıfırla'), staff.fullName + slnJsT('salon.staff.js.icin_yeni_sifre_giriniz', ' icin yeni sifre giriniz:'), function (newPassword) {
            if (!newPassword || newPassword.length < 8) {
                toastr.warning(slnJsT('salon.staff.js.sifre_en_az_8_karakter_olmalidir', 'Şifre en az 8 karakter olmalidir'));
                return;
            }
            $.ajax({
                url: '/proxy/portal/personnel/' + staff.id,
                method: 'PUT',
                contentType: 'application/json',
                data: JSON.stringify({
                    fullName: staff.fullName,
                    email: staff.email,
                    title: staff.title,
                    customerRoleId: staff.customerRoleId,
                    branchId: staff.branchId,
                    isActive: staff.isActive,
                    publicVisible: staff.publicVisible !== false,
                    publicShowFullName: staff.publicShowFullName !== false,
                    publicShowPhoto: staff.publicShowPhoto !== false,
                    publicShowTitle: staff.publicShowTitle !== false,
                    publicShowSpecialty: staff.publicShowSpecialty !== false,
                    skillServiceIds: staff.skillServiceIds || [],
                    password: newPassword
                })
            }).done(function () {
                toastr.success(slnJsT('salon.staff.js.sifre_sifirlandi', 'Şifre sifirlandi'));
            }).fail(function (xhr) {
                toastr.error(xhr.responseJSON?.message || xhr.responseJSON?.error || slnJsT('salon.staff.js.sifre_sifirlanamadi', 'Şifre sifirlanamadi'));
            });
        }, { input: true, inputLabel: slnJsT('salon.staff.new_password', 'Yeni Şifre') });
    };

    self.remove = function (staff) {
        confirmModal(slnJsT('salon.common.btn.confirm', 'Onayla'), slnJsT('salon.staff.js.delete_confirm', "'{name}' personelini silmek istediğinize emin misiniz?").replace('{name}', staff.fullName || ''), function() {
            $.ajax({ url: '/proxy/portal/personnel/' + staff.id, method: 'DELETE' }).done(function () {
                self.loadData();
                toastr.success(slnJsT('salon.staff.js.personel_silindi', 'Personel silindi'));
            }).fail(function () {
                toastr.error(slnJsT('salon.staff.js.personel_silinemedi', 'Personel silinemedi'));
            });
        });
    };

    $(document).ready(function () {
        formModal = new bootstrap.Modal(document.getElementById('staffModal'));
        self.loadRoles();
        self.loadBranches();
        self.loadServices();
        self.loadData();
    });
}

ko.applyBindings(new StaffViewModel(), document.getElementById('staff-vm'));
