function slnJsT(key, fallback) {
    return (window.salonT || function (k, f) { return f || k; })(key, fallback);
}

function PageSettingsViewModel() {
    var self = this;
    self.isLoading = ko.observable(true);
    self.isSaving = ko.observable(false);
    self.profileExists = ko.observable(false);
    self.slug = ko.observable('');
    self.isPublished = ko.observable(false);
    self.banners = ko.observableArray([]);
    self.publicBranches = ko.observableArray([]);

    // Gorseller
    self.logoUrl = ko.observable('');
    self.coverImageUrl = ko.observable('');
    self.faviconUrl = ko.observable('');
    self.galleryImages = ko.observableArray([]);

    var allSections = [
        { key: 'banners', label: slnJsT('salon.pagesettings.js.banner_images', 'Reklam Görselleri'), icon: 'bi bi-collection-play', field: 'showBanners' },
        { key: 'gallery', label: slnJsT('salon.pagesettings.js.gallery', 'Galeri'), icon: 'bi bi-images', field: 'showGallery' },
        { key: 'services', label: slnJsT('salon.pagesettings.js.services', 'Hizmetler'), icon: 'bi bi-list-check', field: 'showServices' },
        { key: 'memberships', label: slnJsT('salon.pagesettings.js.membership_plans', 'Üyelik Planları'), icon: 'bi bi-award', field: 'showMemberships' },
        { key: 'booking', label: slnJsT('salon.pagesettings.js.online_randevu', 'Online Randevu'), icon: 'bi bi-calendar-check', field: 'showBooking' },
        { key: 'team', label: slnJsT('salon.pagesettings.js.team', 'Ekibimiz'), icon: 'bi bi-people', field: 'showTeam' },
        { key: 'reviews', label: slnJsT('salon.pagesettings.js.musteri_yorumlari', 'Müşteri Yorumları'), icon: 'bi bi-chat-square-text', field: 'showReviews' },
        { key: 'hours', label: slnJsT('salon.pagesettings.js.working_hours', 'Çalışma Saatleri'), icon: 'bi bi-clock', field: 'showHours' },
        { key: 'map', label: slnJsT('salon.pagesettings.js.map', 'Harita'), icon: 'bi bi-geo-alt', field: 'showMap' },
        { key: 'contact', label: slnJsT('salon.pagesettings.js.contact', 'İletişim'), icon: 'bi bi-telephone', field: 'showContact' }
    ];

    self.sections = ko.observableArray([]);

    function buildPublicUrl(pathSuffix) {
        if (!self.slug()) return '';
        return window.location.origin + '/salon/' + self.slug() + (pathSuffix || '');
    }

    self.publicPageUrl = ko.computed(function () {
        return buildPublicUrl('');
    });

    self.bookingUrl = ko.computed(function () {
        return buildPublicUrl('/book');
    });

    self.hasPublicBranchLinks = ko.computed(function () {
        return self.publicBranches().length > 0;
    });

    self.copyText = function (value, successMessage) {
        if (!value) return;
        var done = function () {
            toastr.success(successMessage || slnJsT('salon.pagesettings.js.link_copied', 'Link kopyalandı.'));
        };
        if (navigator.clipboard && navigator.clipboard.writeText) {
            navigator.clipboard.writeText(value).then(done).catch(function () { fallbackCopy(value, done); });
            return;
        }
        fallbackCopy(value, done);
    };

    self.copyPublicPageUrl = function () {
        self.copyText(self.publicPageUrl(), slnJsT('salon.pagesettings.public_link_copied', 'Profil linki kopyalandı.'));
    };

    self.copyBookingUrl = function () {
        self.copyText(self.bookingUrl(), slnJsT('salon.pagesettings.booking_link_copied', 'Direkt randevu linki kopyalandı.'));
    };

    self.copyBranchPublicUrl = function (branch) {
        self.copyText(branch.publicPageUrl, slnJsT('salon.pagesettings.public_link_copied', 'Profil linki kopyalandı.'));
    };

    self.copyBranchBookingUrl = function (branch) {
        self.copyText(branch.bookingUrl, slnJsT('salon.pagesettings.booking_link_copied', 'Direkt randevu linki kopyalandı.'));
    };

    function fallbackCopy(value, done) {
        var input = document.createElement('textarea');
        input.value = value;
        input.setAttribute('readonly', 'readonly');
        input.style.position = 'fixed';
        input.style.opacity = '0';
        document.body.appendChild(input);
        input.select();
        document.execCommand('copy');
        document.body.removeChild(input);
        done();
    }

    // ═══ Veri Yukleme ═══
    self.loadData = function () {
        self.isLoading(true);
        var pendingRequests = 2;
        var completeRequest = function () {
            pendingRequests--;
            if (pendingRequests <= 0) {
                self.isLoading(false);
            }
        };

        $.get('/proxy/sln-branches').done(function (branches) {
            var mapped = (branches || []).filter(function (branch) {
                return branch && branch.slug && branch.isActive !== false;
            }).map(function (branch) {
                var baseUrl = window.location.origin + '/salon/' + branch.slug;
                return {
                    name: branch.name || branch.slug,
                    slug: branch.slug,
                    isHeadquarter: !!branch.isHeadquarter,
                    publicPageUrl: baseUrl,
                    bookingUrl: baseUrl + '/book'
                };
            });
            self.publicBranches(mapped);
        }).fail(function () {
            self.publicBranches([]);
        }).always(completeRequest);

        $.get('/proxy/sln-profile').done(function (data) {
            if (data.exists === false) {
                self.profileExists(false);
                return;
            }

            self.profileExists(true);
            self.slug(data.slug || '');
            self.isPublished(data.isPublished || false);

            // Gorseller
            self.logoUrl(data.logoUrl || '');
            self.coverImageUrl(data.coverImageUrl || '');
            self.faviconUrl(data.faviconUrl || '');

            // Galeri
            if (data.galleryImagesJson) {
                try { self.galleryImages(JSON.parse(data.galleryImagesJson)); } catch (e) {}
            }

            // Bannerlar
            if (data.bannersJson) {
                try {
                    var b = JSON.parse(data.bannersJson);
                    self.banners(b.map(function (item) {
                        return { url: ko.observable(item.url || ''), title: ko.observable(item.title || ''), link: ko.observable(item.link || '') };
                    }));
                } catch (e) {}
            }

            // Bolum siralamasi
            var order = null;
            if (data.sectionOrderJson) {
                try { order = JSON.parse(data.sectionOrderJson); } catch (e) {}
            }

            var ordered = [];
            if (order && Array.isArray(order)) {
                order.forEach(function (key) {
                    var sec = allSections.find(function (s) { return s.key === key; });
                    if (sec) ordered.push(sec.key);
                });
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
        }).always(completeRequest);
    };

    // ═══ Tekli Gorsel Yukleme (logo, cover, favicon) ═══
    self.uploadSingle = function (type, event) {
        var file = event.target.files[0];
        if (!file) return;
        if (file.size > 5 * 1024 * 1024) { toastr.warning(slnJsT('salon.pagesettings.js.file_too_large', 'Dosya 5 MB’dan büyük olamaz.')); return; }

        var formData = new FormData();
        formData.append('file', file);

        toastr.info(slnJsT('salon.pagesettings.js.yukleniyor', 'Yukleniyor...'));
        $.ajax({
            url: '/proxy/sln-profile/upload-image?type=' + type,
            method: 'POST',
            data: formData,
            processData: false,
            contentType: false
        }).done(function (result) {
            if (type === 'logo') self.logoUrl(result.url);
            else if (type === 'cover') self.coverImageUrl(result.url);
            else if (type === 'favicon') self.faviconUrl(result.url);
            self.save({ silent: true });
            toastr.success(slnJsT('salon.pagesettings.js.image_uploaded', 'Görsel yüklendi.'));
        }).fail(function (xhr) {
            toastr.error(xhr.responseJSON?.message || slnJsT('salon.pagesettings.js.upload_error', 'Yükleme hatası.'));
        });

        // Input'u sifirla (ayni dosyayi tekrar secebilsin)
        event.target.value = '';
    };

    self.removeSingleImage = function (type) {
        if (type === 'logo') self.logoUrl('');
        else if (type === 'cover') self.coverImageUrl('');
        else if (type === 'favicon') self.faviconUrl('');
    };

    // ═══ Galeri Yukleme ═══
    self.uploadGalleryImages = function (data, event) {
        var files = event.target.files;
        if (!files || files.length === 0) return;

        var remaining = files.length;
        toastr.info(files.length + slnJsT('salon.pagesettings.js.images_uploading', ' görsel yükleniyor...'));

        for (var i = 0; i < files.length; i++) {
            (function (file) {
                if (file.size > 5 * 1024 * 1024) { toastr.warning(file.name + slnJsT('salon.pagesettings.js.file_too_large_suffix', ' 5 MB sınırını aşıyor.')); remaining--; return; }

                var formData = new FormData();
                formData.append('file', file);

                $.ajax({
                    url: '/proxy/sln-profile/upload-image?type=gallery',
                    method: 'POST',
                    data: formData,
                    processData: false,
                    contentType: false
                }).done(function (result) {
                    self.galleryImages.push(result.url);
                }).fail(function () {
                    toastr.error(file.name + slnJsT('salon.pagesettings.js.file_upload_failed_suffix', ' yüklenemedi.'));
                }).always(function () {
                    remaining--;
                    if (remaining <= 0) {
                        toastr.success(slnJsT('salon.pagesettings.js.gallery_images_uploaded', 'Galeri görselleri yüklendi.'));
                        self.save({ silent: true });
                    }
                });
            })(files[i]);
        }

        event.target.value = '';
    };

    self.removeGalleryImage = function (index) {
        self.galleryImages.splice(index, 1);
    };

    // ═══ Banner Yonetimi ═══
    self.addBanner = function () {
        self.banners.push({ url: ko.observable(''), title: ko.observable(''), link: ko.observable('') });
    };

    self.removeBanner = function (index) {
        self.banners.splice(index, 1);
    };

    self.uploadBanner = function (index, event) {
        var file = event.target.files[0];
        if (!file) return;
        if (file.size > 5 * 1024 * 1024) { toastr.warning(slnJsT('salon.pagesettings.js.file_too_large', 'Dosya 5 MB dan büyük olamaz.')); return; }

        var formData = new FormData();
        formData.append('file', file);

        toastr.info(slnJsT('salon.pagesettings.js.loading', 'Yükleniyor...'));
        $.ajax({
            url: '/proxy/sln-profile/upload-image?type=banner',
            method: 'POST',
            data: formData,
            processData: false,
            contentType: false
        }).done(function (result) {
            self.banners()[index].url(result.url);
            self.save({ silent: true });
            toastr.success(slnJsT('salon.pagesettings.js.image_uploaded', 'Görsel yüklendi.'));
        }).fail(function (xhr) {
            toastr.error(xhr.responseJSON?.message || slnJsT('salon.pagesettings.js.upload_error', 'Yükleme hatası.'));
        });

        event.target.value = '';
    };

    // ═══ Kaydet ═══
    self.save = function (options) {
        options = options || {};
        var sectionOrder = self.sections().map(function (s) { return s.key; });

        var bannersData = self.banners().map(function (b) {
            return { url: b.url(), title: b.title(), link: b.link() };
        }).filter(function (b) { return b.url; });

        var payload = {
            showServices: true,
            showMemberships: true,
            showBooking: true,
            showHours: true,
            showContact: true,
            showBanners: true,
            showTeam: true,
            showReviews: true,
            showMap: true,
            sectionOrderJson: JSON.stringify(sectionOrder),
            bannersJson: JSON.stringify(bannersData),
            logoUrl: self.logoUrl() || null,
            coverImageUrl: self.coverImageUrl() || null,
            faviconUrl: self.faviconUrl() || null,
            galleryImagesJson: self.galleryImages().length > 0 ? JSON.stringify(self.galleryImages()) : null
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
            if (!options.silent)
                toastr.success(slnJsT('salon.pagesettings.js.saved', 'Sayfa ayarları kaydedildi.'));
        }).fail(function () {
            toastr.error(options.silent
                ? slnJsT('salon.pagesettings.js.image_save_error', 'Görsel yüklendi ancak sayfa ayarlarına kaydedilemedi.')
                : slnJsT('salon.pagesettings.js.save_error', 'Kaydetme hatası.'));
        }).always(function () { self.isSaving(false); });
    };

    self.loadData();
}

ko.applyBindings(new PageSettingsViewModel(), document.getElementById('pagesettings-vm'));
