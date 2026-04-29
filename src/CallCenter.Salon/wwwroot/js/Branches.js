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
            // Modal icinde: tiklama/pan sonrasi olay asagidaki sidebar linklerine sizmasin (tam sayfa navigasyon/yenileme hissi)
            L.DomEvent.disableClickPropagation(el);

            L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
                attribution: '© OpenStreetMap'
            }).addTo(_map);

            _map.on('click', function (e) {
                updateFromLatLng(e.latlng.lat, e.latlng.lng);
                setMapPin(e.latlng.lat, e.latlng.lng);
            });

            _map.whenReady(function () {
                _map.invalidateSize();
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
                        if (_map) _map.invalidateSize();
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
            toastr.warning('Koordinat bulmak icin en az adres ve sehir giriniz.');
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
                toastr.success('Konum bulundu. Pini surukleyerek hassasiyeti ayarlayabilirsiniz.');
            } else {
                toastr.warning('Adres bulunamadi. Haritada manuel secebilirsiniz.');
            }
        }).fail(function () {
            toastr.error('Konum servisi yanitlamadi.');
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
            toastr.warning('Bu subenin URL (slug) tanimlanmamis. Once duzenleyip slug atayin.');
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
        var branchModalEl = document.getElementById('branchModal');
        formModal = new bootstrap.Modal(branchModalEl);
        qrModal = new bootstrap.Modal(document.getElementById('qrModal'));

        // Modal tam acildiktan sonra harita boyutu guncellenmeli; aksi halde yanlis hit-test + sizan tiklar
        if (branchModalEl) {
            branchModalEl.addEventListener('shown.bs.modal', function () {
                if (_map) {
                    _map.invalidateSize();
                    var el = document.getElementById('branch-map-picker');
                    if (el) L.DomEvent.disableClickPropagation(el);
                }
            });
        }

        self.loadStaff();
        self.loadData();
    });
}

ko.applyBindings(new BranchesViewModel(), document.getElementById('branches-vm'));
