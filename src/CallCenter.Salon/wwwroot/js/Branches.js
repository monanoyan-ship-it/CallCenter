function slnJsT(key, fallback) {
    return (window.salonT || function (k, f) { return f || k; })(key, fallback);
}

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
        photoUrl: ko.observable(''),
        coverImageUrl: ko.observable(''),
        galleryImages: ko.observableArray([]),
        latitude: ko.observable(''),
        longitude: ko.observable(''),
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

    function parseGalleryImages(json) {
        self.form.galleryImages([]);
        if (!json) return;
        try {
            var parsed = JSON.parse(json);
            if (!Array.isArray(parsed)) return;
            self.form.galleryImages(parsed.map(function (item) {
                return typeof item === 'string' ? item : (item && item.url);
            }).filter(Boolean));
        } catch (e) {}
    }

    self.loadData = function () {
        $.ajax({ url: '/proxy/sln-branches', method: 'GET' }).done(function (data) {
            self.branches(data || []);
        }).fail(function () {
            toastr.error(slnJsT('salon.branches.js.load_failed', 'Şubeler yüklenemedi'));
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
        self.form.photoUrl('');
        self.form.coverImageUrl('');
        self.form.galleryImages([]);
        self.form.latitude('');
        self.form.longitude('');
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
        initMap();
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
        self.form.photoUrl(branch.photoUrl || '');
        self.form.coverImageUrl(branch.coverImageUrl || '');
        parseGalleryImages(branch.galleryImagesJson);
        self.form.latitude(branch.latitude != null ? String(branch.latitude) : '');
        self.form.longitude(branch.longitude != null ? String(branch.longitude) : '');
        self.form.managerPersonnelId(branch.managerPersonnelId || '');
        self.form.isActive(branch.isActive);
        self.form.isHeadquarter(branch.isHeadquarter || false);
        self.form.companyTitle(branch.companyTitle || '');
        self.form.taxOffice(branch.taxOffice || '');
        self.form.taxNumber(branch.taxNumber || '');
        self.form.mersisNo(branch.mersisNo || '');
        parseWorkingHours(branch.workingHoursJson);
        formModal.show();
        initMap();
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
            photoUrl: self.form.photoUrl() || null,
            coverImageUrl: self.form.coverImageUrl() || null,
            galleryImagesJson: self.form.galleryImages().length > 0 ? JSON.stringify(self.form.galleryImages()) : null,
            latitude: self.form.latitude() ? parseFloat(self.form.latitude()) : null,
            longitude: self.form.longitude() ? parseFloat(self.form.longitude()) : null,
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
            toastr.warning(slnJsT('salon.branches.js.sube_adi_zorunludur', 'Şube adı zorunludur'));
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
            toastr.success(self.isEditing() ? slnJsT('salon.branches.js.sube_guncellendi', 'Şube güncellendi') : slnJsT('salon.branches.js.sube_eklendi', 'Şube eklendi'));
            self.isSaving(false);
        }).fail(function (xhr) {
            toastr.error(xhr.responseJSON || slnJsT('salon.common.error.generic', 'Bir hata oluştu'));
            self.isSaving(false);
        });
    };

    self.remove = function (branch) {
        var confirmText = slnJsT('salon.branches.js.delete_confirm', '{name} şubesini silmek istediğinize emin misiniz?')
            .replace('{name}', branch.name || '');
        confirmModal(slnJsT('salon.common.btn.confirm', 'Onayla'), confirmText, function() {
            $.ajax({
                url: '/proxy/sln-branches/' + branch.id,
                method: 'DELETE'
            }).done(function () {
                self.loadData();
                toastr.success(slnJsT('salon.branches.js.sube_silindi', 'Şube silindi'));
            }).fail(function () {
                toastr.error(slnJsT('salon.branches.js.delete_failed', 'Silinemedi'));
            });
        });
    };

    function uploadImage(file, type, done, always) {
        if (file.size > 5 * 1024 * 1024) {
            toastr.warning(slnJsT('salon.pagesettings.js.file_too_large', 'Dosya 5 MB dan büyük olamaz.'));
            if (always) always();
            return;
        }

        var formData = new FormData();
        formData.append('file', file);

        $.ajax({
            url: '/proxy/sln-profile/upload-image?type=' + type,
            method: 'POST',
            data: formData,
            processData: false,
            contentType: false
        }).done(function (result) {
            if (result && result.url) done(result.url);
        }).fail(function (xhr) {
            toastr.error(xhr.responseJSON?.message || slnJsT('salon.pagesettings.js.upload_error', 'Yükleme hatası.'));
        }).always(function () {
            if (always) always();
        });
    }

    self.uploadSingleImage = function (type, event) {
        var file = event.target.files[0];
        if (!file) return;

        toastr.info(slnJsT('salon.pagesettings.js.loading', 'Yükleniyor...'));
        uploadImage(file, type === 'photo' ? 'branch-photo' : 'branch-cover', function (url) {
            if (type === 'photo') self.form.photoUrl(url);
            else self.form.coverImageUrl(url);
            toastr.success(slnJsT('salon.pagesettings.js.image_uploaded', 'Görsel yüklendi.'));
        });

        event.target.value = '';
    };

    self.removeSingleImage = function (type) {
        if (type === 'photo') self.form.photoUrl('');
        else if (type === 'cover') self.form.coverImageUrl('');
    };

    self.uploadGalleryImages = function (data, event) {
        var files = event.target.files;
        if (!files || files.length === 0) return;

        var remaining = files.length;
        toastr.info(files.length + slnJsT('salon.pagesettings.js.images_uploading', ' görsel yükleniyor...'));

        for (var i = 0; i < files.length; i++) {
            (function (file) {
                uploadImage(file, 'branch-gallery', function (url) {
                    self.form.galleryImages.push(url);
                }, function () {
                    remaining--;
                    if (remaining <= 0) {
                        toastr.success(slnJsT('salon.pagesettings.js.gallery_images_uploaded', 'Galeri görselleri yüklendi.'));
                    }
                });
            })(files[i]);
        }

        event.target.value = '';
    };

    self.removeGalleryImage = function (index) {
        self.form.galleryImages.splice(index, 1);
    };

    // ═══ Harita Picker ═══
    self.isGeocoding = ko.observable(false);
    var _map = null;
    var _marker = null;
    var _defaultCenter = [39.9, 32.8]; // Turkiye merkezi

    function setMapPin(lat, lng) {
        var latlng = L.latLng(lat, lng);
        if (_marker) {
            _marker.setLatLng(latlng);
        } else {
            _marker = L.marker(latlng, { draggable: true }).addTo(_map);
            _marker.on('dragend', function (e) {
                var p = e.target.getLatLng();
                updateFromLatLng(p.lat, p.lng);
            });
        }
        _map.setView(latlng, 16);
    }

    function updateFromLatLng(lat, lng) {
        self.form.latitude(lat.toFixed(6));
        self.form.longitude(lng.toFixed(6));
        self.form.googleMapsUrl('https://maps.google.com/?q=' + lat.toFixed(6) + ',' + lng.toFixed(6));
    }

    function initMap() {
        if (_map) { _map.remove(); _map = null; _marker = null; }
        setTimeout(function () {
            var el = document.getElementById('branch-map-picker');
            if (!el) return;

            _map = L.map('branch-map-picker').setView(_defaultCenter, 6);
            L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
                attribution: '© OpenStreetMap'
            }).addTo(_map);

            _map.on('click', function (e) {
                updateFromLatLng(e.latlng.lat, e.latlng.lng);
                setMapPin(e.latlng.lat, e.latlng.lng);
            });

            // Mevcut koordinat varsa direkt pin koy
            var lat = parseFloat(self.form.latitude());
            var lng = parseFloat(self.form.longitude());
            if (lat && lng) {
                setMapPin(lat, lng);
                return;
            }

            // Yoksa tarayicidan konum iste
            if (navigator.geolocation) {
                navigator.geolocation.getCurrentPosition(
                    function (pos) {
                        _map.setView([pos.coords.latitude, pos.coords.longitude], 15);
                    },
                    function () { /* izin verilmedi, Turkiye kalir */ }
                );
            }
        }, 300);
    }

    self.onLatLonManualChange = function () {
        var lat = parseFloat(self.form.latitude());
        var lng = parseFloat(self.form.longitude());
        if (_map && lat && lng) {
            setMapPin(lat, lng);
            self.form.googleMapsUrl('https://maps.google.com/?q=' + lat.toFixed(6) + ',' + lng.toFixed(6));
        }
    };

    self.geocodeAddress = function () {
        var parts = [
            self.form.address(),
            self.form.district(),
            self.form.city(),
            'Turkey'
        ].filter(function (p) { return p && p.trim(); });

        if (parts.length < 2) {
            toastr.warning(slnJsT('salon.branches.js.geocode_need_address_city', 'Koordinat bulmak için en az adres ve şehir giriniz.'));
            return;
        }

        self.isGeocoding(true);
        $.ajax({
            url: 'https://nominatim.openstreetmap.org/search',
            method: 'GET',
            data: { q: parts.join(', '), format: 'json', limit: 1 },
            headers: { 'Accept-Language': 'tr' }
        }).done(function (results) {
            if (results && results.length > 0) {
                var r = results[0];
                updateFromLatLng(parseFloat(r.lat), parseFloat(r.lon));
                if (_map) setMapPin(parseFloat(r.lat), parseFloat(r.lon));
                toastr.success(slnJsT('salon.branches.js.geocode_found', 'Konum bulundu. Pini sürükleyerek hassasiyeti ayarlayabilirsiniz.'));
            } else {
                toastr.warning(slnJsT('salon.branches.js.geocode_not_found', 'Adres bulunamadı. Haritada manuel seçebilirsiniz.'));
            }
        }).fail(function () {
            toastr.error(slnJsT('salon.branches.js.geocode_service_failed', 'Konum servisi yanıtlamadı.'));
        }).always(function () {
            self.isGeocoding(false);
        });
    };

    // ═══ QR Kod ═══
    var qrModal;
    var qrInstance = null;
    self.qrBranchName = ko.observable('');
    self.qrUrl = ko.observable('');
    var qrCurrentBranch = null;

    self.showQr = function (branch) {
        if (!branch.slug) {
            toastr.warning(slnJsT('salon.branches.js.qr_slug_missing', 'Bu şubenin URL (slug) tanımlanmamış. Önce düzenleyip slug atayın.'));
            return;
        }
        qrCurrentBranch = branch;
        self.qrBranchName(branch.name + (branch.isHeadquarter ? ' (Merkez)' : ''));
        var url = window.location.origin + '/salon/' + branch.slug + '/book';
        self.qrUrl(url);

        var container = document.getElementById('qrCanvas');
        container.innerHTML = '';
        qrInstance = new QRCode(container, {
            text: url,
            width: 256,
            height: 256,
            colorDark: '#000000',
            colorLight: '#ffffff',
            correctLevel: QRCode.CorrectLevel.M
        });

        qrModal.show();
    };

    self.downloadQr = function () {
        var container = document.getElementById('qrCanvas');
        var img = container.querySelector('img') || container.querySelector('canvas');
        if (!img) return;
        var dataUrl = img.tagName === 'IMG' ? img.src : img.toDataURL('image/png');
        var a = document.createElement('a');
        a.href = dataUrl;
        a.download = 'qr-' + (qrCurrentBranch && qrCurrentBranch.slug ? qrCurrentBranch.slug : 'sube') + '.png';
        document.body.appendChild(a);
        a.click();
        document.body.removeChild(a);
    };

    $(document).ready(function () {
        formModal = new bootstrap.Modal(document.getElementById('branchModal'));
        qrModal = new bootstrap.Modal(document.getElementById('qrModal'));
        self.loadStaff();
        self.loadData();
    });
}

ko.applyBindings(new BranchesViewModel(), document.getElementById('branches-vm'));
