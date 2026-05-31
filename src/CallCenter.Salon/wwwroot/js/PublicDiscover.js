(function () {
    var discoverData = document.getElementById('discover-data');

    function discoverText(name, fallback) {
        if (!discoverData) return fallback;
        return discoverData.getAttribute('data-' + name) || fallback;
    }

    function DiscoverViewModel() {
        var self = this;
        self.salons = ko.observableArray([]);
        self.searchQuery = ko.observable('');
        self.isLoading = ko.observable(true);
        var map, markers = [];

        self.userLat = ko.observable(null);
        self.userLng = ko.observable(null);
        self.locationPending = ko.observable(true);

        if (typeof L === 'undefined') {
            self.locationPending(false);
            self.isLoading(false);
            fetch('/proxy/salon/')
                .then(function (r) { return r.ok ? r.json() : []; })
                .then(function (data) { self.salons(data); })
                .catch(function () { self.salons([]); });
            return;
        }

        function haversineKm(lat1, lon1, lat2, lon2) {
            var R = 6371;
            var toRad = function (x) { return x * Math.PI / 180; };
            var dLat = toRad(lat2 - lat1), dLon = toRad(lon2 - lon1);
            var a = Math.sin(dLat / 2) * Math.sin(dLat / 2)
                + Math.cos(toRad(lat1)) * Math.cos(toRad(lat2))
                * Math.sin(dLon / 2) * Math.sin(dLon / 2);
            return R * 2 * Math.atan2(Math.sqrt(a), Math.sqrt(1 - a));
        }

        var MAX_RADIUS_KM = 5;
        var pendingLocation = null;

        self.filteredSalons = ko.computed(function () {
            var q = (self.searchQuery() || '').toLowerCase();
            var uLat = self.userLat(), uLng = self.userLng();
            if (self.locationPending()) return [];

            var list = self.salons().map(function (s) {
                var d = null;
                if (uLat !== null && s.latitude && s.longitude) {
                    d = haversineKm(uLat, uLng, s.latitude, s.longitude);
                }
                return Object.assign({}, s, {
                    distanceKm: d,
                    distanceText: d === null ? ''
                        : d < 1 ? Math.round(d * 1000) + ' m'
                        : d.toFixed(1) + ' km'
                });
            });

            if (q) {
                list = list.filter(function (s) {
                    var name = (s.branchName || s.salonName || '').toLowerCase();
                    var salon = (s.salonName || '').toLowerCase();
                    var city = (s.city || '').toLowerCase();
                    var dist = (s.district || '').toLowerCase();
                    return name.indexOf(q) >= 0 || salon.indexOf(q) >= 0
                        || city.indexOf(q) >= 0 || dist.indexOf(q) >= 0;
                });
            } else if (uLat !== null) {
                list = list.filter(function (s) {
                    return s.distanceKm === null || s.distanceKm <= MAX_RADIUS_KM;
                });
            }

            if (uLat !== null) {
                list.sort(function (a, b) {
                    if (a.distanceKm === null && b.distanceKm === null) return 0;
                    if (a.distanceKm === null) return 1;
                    if (b.distanceKm === null) return -1;
                    return a.distanceKm - b.distanceKm;
                });
            }
            return list;
        });

        map = L.map('discoverMap').setView([39.9, 32.8], 6);
        L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
            attribution: '&copy; OpenStreetMap'
        }).addTo(map);

        var userMarker = null;
        var userIcon = L.divIcon({
            className: '',
            html: '<div style="background:#4285f4;width:14px;height:14px;border-radius:50%;border:3px solid #fff;box-shadow:0 0 6px rgba(0,0,0,.3);"></div>',
            iconSize: [14, 14],
            iconAnchor: [7, 7]
        });
        var branchMarkers = [];

        function updateMarkerVisibility(uLat, uLng) {
            branchMarkers.forEach(function (bm) {
                var d = haversineKm(uLat, uLng, bm.lat, bm.lng);
                if (d <= MAX_RADIUS_KM) {
                    if (!map.hasLayer(bm.marker)) bm.marker.addTo(map);
                } else if (map.hasLayer(bm.marker)) {
                    map.removeLayer(bm.marker);
                }
            });
        }

        function setUserLocation(lat, lng, zoom) {
            self.userLat(lat);
            self.userLng(lng);
            if (userMarker) {
                userMarker.setLatLng([lat, lng]);
            } else {
                userMarker = L.marker([lat, lng], { icon: userIcon }).addTo(map).bindPopup(discoverText('your-location', 'Konumunuz'));
            }
            if (zoom) map.setView([lat, lng], zoom);
            if (branchMarkers.length > 0) updateMarkerVisibility(lat, lng);
        }

        if (navigator.geolocation) {
            navigator.geolocation.getCurrentPosition(function (pos) {
                setUserLocation(pos.coords.latitude, pos.coords.longitude, 13);
                self.locationPending(false);
            }, function () {
                self.locationPending(false);
            }, { timeout: 5000 });
        } else {
            self.locationPending(false);
        }

        map.on('click', function (e) {
            pendingLocation = { lat: e.latlng.lat, lng: e.latlng.lng };
            var html = '<div style="min-width:180px;">' +
                '<div class="small mb-2">' + discoverText('move-location-prompt', 'Konumunuzu buraya taşımak ister misiniz?') + '</div>' +
                '<button type="button" class="btn btn-sm btn-primary w-100" data-discover-action="confirm-location">' +
                discoverText('move-location-confirm', 'Evet, buraya taşı') +
                '</button>' +
                '</div>';
            L.popup({ closeButton: true })
                .setLatLng([pendingLocation.lat, pendingLocation.lng])
                .setContent(html)
                .openOn(map);
        });

        document.addEventListener('click', function (event) {
            var actionEl = event.target.closest('[data-discover-action="confirm-location"]');
            if (!actionEl || !pendingLocation) return;
            event.preventDefault();
            setUserLocation(pendingLocation.lat, pendingLocation.lng, 13);
            pendingLocation = null;
            map.closePopup();
        });

        fetch('/proxy/salon')
            .then(function (r) { return r.ok ? r.json() : []; })
            .then(function (data) {
                self.salons(data);
                self.isLoading(false);
            })
            .catch(function () { self.isLoading(false); });

        fetch('/proxy/salon/branches-map')
            .then(function (r) { return r.ok ? r.json() : []; })
            .then(function (branches) {
                branches.forEach(function (b) {
                    if (!b.latitude || !b.longitude) return;
                    var marker = L.marker([b.latitude, b.longitude]).addTo(map);
                    var displayName = b.branchName || b.salonName;
                    marker.bindPopup(
                        '<b>' + displayName + '</b>' +
                        (!b.isHeadquarter && b.branchName && b.salonName !== b.branchName ? '<br><small class="text-muted">' + b.salonName + '</small>' : '') +
                        (b.city ? '<br><small>' + (b.district ? b.district + ', ' : '') + b.city + '</small>' : '') +
                        '<br><a href="/salon/' + b.slug + '">' + discoverText('view-profile', 'Profili Gör') + '</a>'
                    );
                    markers.push(marker);
                    branchMarkers.push({ marker: marker, lat: b.latitude, lng: b.longitude });
                });
                if (self.userLat() !== null) updateMarkerVisibility(self.userLat(), self.userLng());
                if ((self.userLat() === null || self.userLng() === null) && markers.length > 0) {
                    map.setView(markers[0].getLatLng(), 12);
                }
            })
            .catch(function () { });
    }

    ko.applyBindings(new DiscoverViewModel(), document.getElementById('discover-vm'));
})();
