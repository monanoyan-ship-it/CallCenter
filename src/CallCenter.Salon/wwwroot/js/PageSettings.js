function PageSettingsViewModel() {
    var self = this;
    self.isLoading = ko.observable(true);
    self.isSaving = ko.observable(false);
    self.profileExists = ko.observable(false);
    self.slug = ko.observable('');
    self.isPublished = ko.observable(false);

    var allSections = [
        { key: 'services', label: 'Hizmetler', icon: 'bi bi-list-check', field: 'showServices' },
        { key: 'memberships', label: 'Uyelik Planlari', icon: 'bi bi-award', field: 'showMemberships' },
        { key: 'booking', label: 'Online Randevu', icon: 'bi bi-calendar-check', field: 'showBooking' },
        { key: 'hours', label: 'Calisma Saatleri', icon: 'bi bi-clock', field: 'showHours' },
        { key: 'contact', label: 'Iletisim', icon: 'bi bi-telephone', field: 'showContact' }
    ];

    self.sections = ko.observableArray([]);

    self.loadData = function () {
        self.isLoading(true);
        $.get('/proxy/sln-profile').done(function (data) {
            if (data.exists === false) {
                self.profileExists(false);
                self.isLoading(false);
                return;
            }

            self.profileExists(true);
            self.slug(data.slug || '');
            self.isPublished(data.isPublished || false);

            // Siralama
            var order = null;
            if (data.sectionOrderJson) {
                try { order = JSON.parse(data.sectionOrderJson); } catch (e) {}
            }

            var ordered = [];
            if (order && Array.isArray(order)) {
                // Kayitli sirada ekle
                order.forEach(function (key) {
                    var sec = allSections.find(function (s) { return s.key === key; });
                    if (sec) ordered.push(sec.key);
                });
                // Kayitli sirada olmayanlari sona ekle
                allSections.forEach(function (sec) {
                    if (ordered.indexOf(sec.key) < 0) ordered.push(sec.key);
                });
            } else {
                ordered = allSections.map(function (s) { return s.key; });
            }

            var items = ordered.map(function (key) {
                var sec = allSections.find(function (s) { return s.key === key; });
                var fieldValue = data[sec.field];
                return {
                    key: sec.key,
                    label: sec.label,
                    icon: sec.icon,
                    field: sec.field,
                    enabled: ko.observable(fieldValue !== false)
                };
            });

            self.sections(items);
            self.isLoading(false);

            // SortableJS
            setTimeout(function () {
                var el = document.getElementById('sectionList');
                if (el) {
                    new Sortable(el, {
                        animation: 150,
                        handle: '.bi-grip-vertical',
                        onEnd: function () {
                            var newOrder = [];
                            el.querySelectorAll('.list-group-item').forEach(function (item) {
                                newOrder.push(item.getAttribute('data-key'));
                            });
                            // KO array'i guncelle
                            var currentItems = self.sections();
                            var reordered = newOrder.map(function (key) {
                                return currentItems.find(function (s) { return s.key === key; });
                            }).filter(Boolean);
                            self.sections(reordered);
                        }
                    });
                }
            }, 100);
        }).fail(function () {
            self.profileExists(false);
            self.isLoading(false);
        });
    };

    self.save = function () {
        var sectionOrder = self.sections().map(function (s) { return s.key; });
        var payload = {
            showServices: true,
            showMemberships: true,
            showBooking: true,
            showHours: true,
            showContact: true,
            sectionOrderJson: JSON.stringify(sectionOrder)
        };

        self.sections().forEach(function (s) {
            payload[s.field] = s.enabled();
        });

        self.isSaving(true);
        $.ajax({
            url: '/proxy/sln-profile/page-settings',
            method: 'PUT',
            contentType: 'application/json',
            data: JSON.stringify(payload)
        }).done(function () {
            toastr.success('Sayfa ayarlari kaydedildi.');
        }).fail(function () {
            toastr.error('Kaydetme hatasi.');
        }).always(function () { self.isSaving(false); });
    };

    self.loadData();
}

ko.applyBindings(new PageSettingsViewModel(), document.getElementById('pagesettings-vm'));
