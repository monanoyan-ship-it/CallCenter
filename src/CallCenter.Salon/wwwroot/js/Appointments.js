var APPOINTMENTS_LOCALE = document.documentElement.lang || undefined;
function appointmentT(key, fallback) {
    return (window.salonT || function (k, f) { return f || k; })(key, fallback);
}

function AppointmentsViewModel() {
    var self = this;
    self.appointments = ko.observableArray([]);
    self.clientList = ko.observableArray([]);
    self.serviceList = ko.observableArray([]);
    self.comboList = ko.observableArray([]);
    self.staffList = ko.observableArray([]);
    self.isEditing = ko.observable(false);
    self.editingId = ko.observable(null);
    self.isSaving = ko.observable(false);
    self.isLoadingAppointments = ko.observable(false);
    self.activeWaitlistCount = ko.observable(0);

    // ═══ Coklu Tarih Araligi ═══
    self.dateRanges = ko.observableArray([]);
    self.manualFrom = ko.observable('');
    self.manualTo = ko.observable('');

    // ═══ Coklu Personel Filtresi ═══
    self.selectedStaffIds = ko.observableArray([]);

    // ═══ Form ═══
    self.form = {
        slnClientId: ko.observable(null),
        startTime: ko.observable(''),
        comboId: ko.observable(null),
        serviceIds: ko.observableArray([]),
        personnelId: ko.observable(null),
        notes: ko.observable('')
    };

    // ═══ Musait Slotlar ═══
    self.availableSlots = ko.observableArray([]);
    self.slotsLoading = ko.observable(false);
    self.availableStaffForService = ko.observableArray([]);
    self.slotDate = ko.observable('');

    // ═══ Autocomplete'ler ═══
    self.clientAutocomplete = createAutocomplete(self.clientList, 'fullName', self.form.slnClientId);
    self.serviceAutocomplete = createMultiAutocomplete(self.serviceList, 'name', self.form.serviceIds);
    self.staffAutocomplete = createAutocomplete(self.staffList, 'fullName', self.form.personnelId);
    self.staffFilterAutocomplete = createMultiAutocomplete(self.staffList, 'fullName', self.selectedStaffIds);

    function toOptionalInt(value) {
        var parsed = parseInt(value, 10);
        return parsed > 0 ? parsed : null;
    }

    function getAjaxErrorMessage(xhr, fallback) {
        if (xhr && xhr.responseJSON && (xhr.responseJSON.error || xhr.responseJSON.message)) {
            return xhr.responseJSON.error || xhr.responseJSON.message;
        }
        if (xhr && typeof xhr.responseText === 'string' && xhr.responseText.trim()) {
            return xhr.responseText.trim();
        }
        return fallback;
    }

    self.getPersonnelBranchId = function (personnelId) {
        var id = toOptionalInt(personnelId);
        if (!id) return null;
        var staff = self.staffList().find(function (p) { return parseInt(p.id, 10) === id; });
        return staff ? toOptionalInt(staff.branchId) : null;
    };

    self.resolveFormBranchId = function () {
        var personnelBranchId = self.getPersonnelBranchId(self.form.personnelId());
        if (personnelBranchId) return personnelBranchId;
        if (window.slnGetBranch) return toOptionalInt(window.slnGetBranch());
        return null;
    };

    // Hizmet secildiginde uygun personelleri getir
    self.form.serviceIds.subscribe(function (ids) {
        if (!ids || ids.length === 0) {
            self.availableStaffForService([]);
            return;
        }
        $.get('/proxy/sln-appointments/available-staff?serviceIds=' + ids.join(','), function (data) {
            self.availableStaffForService(data || []);
        });
    });

    // Personel + tarih secildiginde musait slotlari getir
    function loadSlots() {
        var personnelId = self.form.personnelId();
        var dateStr = self.slotDate();
        var serviceIds = self.form.serviceIds();
        if (!personnelId || !dateStr || !serviceIds.length) {
            self.availableSlots([]);
            return;
        }
        // Toplam sure hesapla
        var totalDuration = 0;
        var allSvcs = [];
        self.serviceList().forEach(function (cat) { (cat.services || []).forEach(function (s) { allSvcs.push(s); }); });
        serviceIds.forEach(function (id) {
            var svc = allSvcs.find(function (s) { return s.id === id; });
            if (svc) totalDuration += Math.max((svc.durationMinutes || 30), (svc.processingMinutes || 0)) + (svc.bufferBeforeMinutes || 0) + (svc.bufferAfterMinutes || 0);
        });

        self.slotsLoading(true);
        $.get('/proxy/sln-appointments/available-slots?personnelId=' + personnelId + '&date=' + dateStr + '&durationMinutes=' + totalDuration + '&serviceIds=' + serviceIds.join(','), function (data) {
            var slots = data || [];
            self.availableSlots(slots);
            self.slotsLoading(false);
            // Bos slot listesi: personelin o gun calisma saati yok veya tum slotlar dolu
            if (slots.length === 0) {
                toastr.warning(appointmentT('salon.appointments.no_staff_slots_warning', 'Bu personelin seçili tarihte müsait saati yok. Çalışma saatlerini kontrol edin veya başka tarih/personel deneyin.'));
            }
        }).fail(function (xhr) {
            self.slotsLoading(false);
            toastr.error(appointmentT('salon.appointments.slots_load_failed', 'Müsait saatler yüklenemedi') + ' (HTTP ' + xhr.status + ').');
        });
    }

    self.form.personnelId.subscribe(loadSlots);
    self.slotDate.subscribe(loadSlots);

    self.selectSlot = function (slot) {
        if (!slot.available) return;
        // BUG2.17: Cift UTC donusum onleme — slot.startTime UTC kind ISO ("...Z") gelir
        // ama hour numarasi salon LOCAL saatini temsil eder. new Date() ile parse edersek
        // tarayici TZ'sine cevirir → +3 kayma. Naive olarak Z'siz string halini sakla.
        var raw = slot.startTime || '';
        self.form.startTime(raw.replace(/Z$/i, '').substring(0, 16));
    };

    var statusNames = {
        1: appointmentT('salon.appointments.status.scheduled', 'Planlanmış'),
        2: appointmentT('salon.appointments.status.confirmed', 'Onaylandı'),
        3: appointmentT('salon.appointments.status.completed', 'Tamamlandı'),
        4: appointmentT('salon.appointments.status.cancelled', 'İptal'),
        5: appointmentT('salon.appointments.status.no_show', 'Gelmedi'),
        6: appointmentT('salon.appointments.status.payment_pending', 'Ödeme bekliyor')
    };
    var statusCss = { 1: 'bg-warning text-dark', 2: 'bg-info', 3: 'bg-success', 4: 'bg-danger', 5: 'bg-secondary', 6: 'bg-warning text-dark' };

    // ═══ Tarih Yardimcilari ═══
    function toDateStr(d) {
        var month = String(d.getMonth() + 1).padStart(2, '0');
        var day = String(d.getDate()).padStart(2, '0');
        return d.getFullYear() + '-' + month + '-' + day;
    }
    function addDays(d, n) { var r = new Date(d); r.setDate(r.getDate() + n); return r; }

    function getMonday(d) {
        var day = d.getDay();
        var diff = d.getDate() - day + (day === 0 ? -6 : 1);
        return new Date(d.getFullYear(), d.getMonth(), diff);
    }

    function monthStart(d) { return new Date(d.getFullYear(), d.getMonth(), 1); }
    function monthEnd(d) { return new Date(d.getFullYear(), d.getMonth() + 1, 0); }

    // ═══ Preset Tanimlari ═══
    self.presetGroups = [
        {
            name: 'Gecmis', css: 'btn-outline-secondary', items: [
                { label: appointmentT('salon.appointments.preset.yesterday', 'Dün'), calc: function () { var d = addDays(new Date(), -1); return { from: toDateStr(d), to: toDateStr(d) }; } },
                { label: appointmentT('salon.appointments.preset.last_week', 'Geçen Hafta'), calc: function () { var mon = getMonday(addDays(new Date(), -7)); return { from: toDateStr(mon), to: toDateStr(addDays(mon, 6)) }; } },
                { label: appointmentT('salon.appointments.preset.last_month', 'Geçen Ay'), calc: function () { var d = new Date(); var prev = new Date(d.getFullYear(), d.getMonth() - 1, 1); return { from: toDateStr(prev), to: toDateStr(monthEnd(prev)) }; } }
            ]
        },
        {
            name: 'Su An', css: 'btn-outline-primary', items: [
                { label: appointmentT('salon.reports.today', 'Bugün'), calc: function () { var d = toDateStr(new Date()); return { from: d, to: d }; } },
                { label: appointmentT('salon.reports.this_week', 'Bu Hafta'), calc: function () { var mon = getMonday(new Date()); return { from: toDateStr(mon), to: toDateStr(addDays(mon, 6)) }; } },
                { label: appointmentT('salon.reports.this_month', 'Bu Ay'), calc: function () { var d = new Date(); return { from: toDateStr(monthStart(d)), to: toDateStr(monthEnd(d)) }; } }
            ]
        },
        {
            name: 'Gelecek', css: 'btn-outline-info', items: [
                { label: appointmentT('salon.appointments.preset.tomorrow', 'Yarın'), calc: function () { var d = addDays(new Date(), 1); return { from: toDateStr(d), to: toDateStr(d) }; } },
                { label: appointmentT('salon.appointments.preset.next_week', 'Gelecek Hafta'), calc: function () { var mon = getMonday(addDays(new Date(), 7)); return { from: toDateStr(mon), to: toDateStr(addDays(mon, 6)) }; } },
                { label: appointmentT('salon.appointments.preset.next_month', 'Gelecek Ay'), calc: function () { var d = new Date(); var next = new Date(d.getFullYear(), d.getMonth() + 1, 1); return { from: toDateStr(next), to: toDateStr(monthEnd(next)) }; } }
            ]
        }
    ];

    self.addPreset = function (preset) {
        var range = preset.calc();
        range.label = preset.label;
        var exists = self.dateRanges().some(function (r) { return r.from === range.from && r.to === range.to; });
        if (!exists) self.dateRanges.push(range);
    };

    self.addManualRange = function () {
        var f = self.manualFrom(), t = self.manualTo();
        if (!f || !t) { toastr.warning(appointmentT('salon.appointments.enter_start_end_dates', 'Başlangıç ve bitiş tarihi girin')); return; }
        if (f > t) { toastr.warning(appointmentT('salon.appointments.start_after_end', 'Başlangıç tarihi bitiş tarihinden büyük olamaz')); return; }
        var label = f + ' ~ ' + t;
        var exists = self.dateRanges().some(function (r) { return r.from === f && r.to === t; });
        if (!exists) {
            self.dateRanges.push({ from: f, to: t, label: label });
            self.manualFrom('');
            self.manualTo('');
        }
    };

    self.removeRange = function (range) {
        self.dateRanges.remove(range);
    };

    self.clearAllRanges = function () {
        self.dateRanges.removeAll();
    };

    self.addTodayRange = function () {
        var d = toDateStr(new Date());
        var exists = self.dateRanges().some(function (r) { return r.from === d && r.to === d; });
        if (!exists) {
            self.dateRanges.push({
                from: d,
                to: d,
                label: appointmentT('salon.reports.today', 'Bugün')
            });
        }
    };

    self.rangesDescription = ko.computed(function () {
        var ranges = self.dateRanges();
        if (ranges.length === 0) return appointmentT('salon.appointments.no_range_selected', 'Aralık seçilmedi');
        return ranges.map(function (r) { return r.label; }).join(' + ');
    });

    // ═══ Filtreleme ═══
    self.filteredAppointments = ko.computed(function () {
        var staffIds = self.selectedStaffIds();
        var all = self.appointments();
        if (!staffIds || staffIds.length === 0) return all;
        return all.filter(function (a) { return staffIds.indexOf(a.personnelId) >= 0; });
    });

    // ═══ Personel Raporu ═══
    self.personnelReport = ko.computed(function () {
        var filtered = self.filteredAppointments();
        var staffMap = {};
        filtered.forEach(function (a) {
            var pid = a.personnelId;
            if (!staffMap[pid]) staffMap[pid] = { personnelName: a.personnelName, total: 0, completed: 0 };
            staffMap[pid].total++;
            if (a.statusId === 3) staffMap[pid].completed++;
        });
        var result = [];
        for (var pid in staffMap) result.push(staffMap[pid]);
        result.sort(function (a, b) { return b.total - a.total; });
        return result;
    });

    // ═══ Veri Yukleme ═══
    var formModal;

    self.loadAppointments = function () {
        var ranges = self.dateRanges();
        if (ranges.length === 0) { self.appointments([]); return; }

        self.isLoadingAppointments(true);

        var promises = ranges.map(function (range) {
            return $.ajax({
                url: '/proxy/sln-appointments?from=' + range.from + 'T00:00:00Z&to=' + range.to + 'T23:59:59Z',
                method: 'GET'
            });
        });

        Promise.all(promises).then(function (results) {
            var allItems = [];
            var seenIds = {};

            results.forEach(function (data) {
                var items = data.items || data;
                items.forEach(function (a) {
                    if (seenIds[a.id]) return;
                    seenIds[a.id] = true;

                    a.statusText = statusNames[a.statusId] || appointmentT('salon.common.unknown', 'Bilinmiyor');
                    a.statusCss = statusCss[a.statusId] || 'bg-secondary';
                    a.serviceNamesText = (a.serviceNames && a.serviceNames.length > 0)
                        ? a.serviceNames.join(', ')
                        : (a.serviceName || '-');
                    if (a.startTime) {
                        // BUG2.17: Naive saat — DB'de Utc kind ile yazili ama hour LOCAL.
                        // toLocale ile cevirme +3 kayma yapar. ISO string'den dogrudan parse.
                        a.startTimeFormatted = a.startTime.substring(11, 16);
                        var datePart = a.startTime.substring(0, 10).split('-'); // YYYY-MM-DD
                        var monthText = new Intl.DateTimeFormat(APPOINTMENTS_LOCALE || undefined, { month: 'short' }).format(new Date(2026, parseInt(datePart[1]) - 1, 1));
                        a.dateFormatted = parseInt(datePart[2]) + ' ' + monthText;
                    }
                    if (a.startTime && a.endTime) {
                        // Suresi: iki ISO arasi dakika (UTC parse her iki tarafta da konsistan, kayma yok)
                        a.durationMinutes = a.durationMinutes || Math.round((new Date(a.endTime) - new Date(a.startTime)) / 60000);
                    } else {
                        a.durationMinutes = null;
                    }
                    allItems.push(a);
                });
            });

            allItems.sort(function (a, b) { return (a.startTime || '').localeCompare(b.startTime || ''); });
            self.appointments(allItems);
            self.isLoadingAppointments(false);
        }).catch(function () {
            toastr.error(appointmentT('salon.appointments.load_failed', 'Randevular yüklenemedi'));
            self.isLoadingAppointments(false);
        });
    };

    self.loadLookups = function () {
        $.ajax({ url: '/proxy/sln-clients?pageSize=1000', method: 'GET' }).done(function (data) {
            self.clientList(data.items || data);
        });
        $.ajax({ url: '/proxy/sln-services/categories', method: 'GET' }).done(function (data) {
            self.serviceList(data.items || data);
        });
        $.ajax({ url: '/proxy/sln-services/combos', method: 'GET' }).done(function (data) {
            self.comboList((data && data.items) || data || []);
        });
        $.ajax({ url: '/proxy/portal/personnel', method: 'GET' }).done(function (data) {
            self.staffList(data.items || data);
        });
    };

    self.loadWaitlistHint = function () {
        $.ajax({ url: '/proxy/sln-waitlist?scope=active', method: 'GET' }).done(function (data) {
            var items = data.items || data || [];
            var activeItems = items.filter(function (e) {
                return e.statusId === 1 || e.statusId === 2 || e.statusId === 3;
            });
            self.activeWaitlistCount(activeItems.length);
        }).fail(function () {
            self.activeWaitlistCount(0);
        });
    };

    // ═══ Kategori + Hizmet Secimi ═══
    self.selectedCategoryId = ko.observable(null);

    self.selectedCategoryServices = ko.computed(function () {
        var catId = self.selectedCategoryId();
        if (!catId) return [];
        var cat = self.serviceList().find(function (c) { return c.id === catId; });
        return cat ? (cat.services || []) : [];
    });

    // Tum servis listesinden secilen ID'lerin isimlerini bul
    self.selectedServiceNames = ko.computed(function () {
        var ids = self.form.serviceIds();
        if (!ids || !ids.length) return [];
        var allServices = [];
        self.serviceList().forEach(function (cat) {
            (cat.services || []).forEach(function (s) { allServices.push(s); });
        });
        return ids.map(function (id) {
            var svc = allServices.find(function (s) { return s.id === id; });
            return svc ? { id: svc.id, name: svc.name } : { id: id, name: '?' };
        });
    });

    self.toggleService = function (serviceId) {
        var ids = self.form.serviceIds().slice();
        var idx = ids.indexOf(serviceId);
        if (idx >= 0) ids.splice(idx, 1);
        else ids.push(serviceId);
        self.form.serviceIds(ids);
    };

    self.selectCombo = function (combo) {
        self.form.comboId(combo.id);
        self.form.serviceIds((combo.items || []).map(function (i) { return i.serviceId; }));
    };

    self.clearCombo = function () {
        self.form.comboId(null);
        self.form.serviceIds([]);
    };

    self.removeSelectedService = function (svc) {
        self.toggleService(svc.id);
    };

    // dateRanges degisince otomatik yukle
    self.dateRanges.subscribe(function () { self.loadAppointments(); });

    // ═══ Form Islemleri ═══
    self.resetForm = function () {
        self.form.slnClientId(null);
        self.form.startTime('');
        self.slotDate('');
        self.availableSlots([]);
        self.availableStaffForService([]);
        self.form.comboId(null);
        self.form.serviceIds([]);
        self.form.personnelId(null);
        self.form.notes('');
        self.isEditing(false);
        self.editingId(null);
        self.clientAutocomplete.clear();
        self.serviceAutocomplete.clear();
        self.staffAutocomplete.clear();
    };

    self.openNew = function () {
        self.resetForm();
        formModal.show();
    };

    self.openEdit = function (appt) {
        self.isEditing(true);
        self.editingId(appt.id);
        self.form.slnClientId(appt.slnClientId);
        self.form.startTime(appt.startTime ? appt.startTime.substring(0, 16) : '');
        self.slotDate(appt.startTime ? appt.startTime.substring(0, 10) : '');
        self.form.comboId(appt.comboId || null);
        self.form.serviceIds(appt.serviceIds || (appt.serviceId ? [appt.serviceId] : []));
        self.form.personnelId(appt.personnelId);
        self.form.notes(appt.notes || '');
        self.clientAutocomplete.setFromValue(appt.slnClientId);
        self.serviceAutocomplete.setFromValues(appt.serviceIds || (appt.serviceId ? [appt.serviceId] : []));
        self.staffAutocomplete.setFromValue(appt.personnelId);
        formModal.show();
    };

    self.save = function () {
        var startTimeVal = self.form.startTime();
        if (!startTimeVal) {
            toastr.warning(appointmentT('salon.appointments.date_time_required', 'Tarih ve saat zorunludur'));
            return;
        }
        if (startTimeVal.length <= 5) {
            startTimeVal = toDateStr(new Date()) + 'T' + startTimeVal + ':00';
        }
        if (startTimeVal && !startTimeVal.endsWith('Z')) startTimeVal += 'Z';

        var data = {
            slnClientId: self.form.slnClientId(),
            personnelId: self.form.personnelId(),
            branchId: self.resolveFormBranchId(),
            comboId: self.form.comboId(),
            serviceIds: self.form.serviceIds(),
            startTime: startTimeVal,
            notes: self.form.notes()
        };

        if (!data.slnClientId || !data.startTime || !data.serviceIds.length || !data.personnelId) {
            toastr.warning(appointmentT('salon.appointments.form_required', 'Müşteri, saat, hizmet ve personel zorunludur'));
            return;
        }

        self.isSaving(true);
        var url = '/proxy/sln-appointments';
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
            self.loadAppointments();
            toastr.success(self.isEditing() ? appointmentT('salon.appointments.updated', 'Randevu güncellendi') : appointmentT('salon.appointments.created', 'Randevu eklendi'));
            self.isSaving(false);
        }).fail(function (xhr) {
            toastr.error(getAjaxErrorMessage(xhr, appointmentT('salon.common.error.generic', 'Bir hata oluştu')));
            self.isSaving(false);
        });
    };

    // Planlanmis (1) -> Onaylandi (2): musteri rezervasyonu onayla
    self.confirm = function (appt) {
        $.ajax({
            url: '/proxy/sln-appointments/' + appt.id + '/status',
            method: 'PUT',
            contentType: 'application/json',
            data: JSON.stringify({ statusId: 2 })
        }).done(function () {
            self.loadAppointments();
            toastr.success(appointmentT('salon.appointments.confirmed', 'Randevu onaylandı'));
        });
    };

    // Onaylandi (2) -> Tamamlandi (3): hizmet bitti
    self.complete = function (appt) {
        $.ajax({
            url: '/proxy/sln-appointments/' + appt.id + '/status',
            method: 'PUT',
            contentType: 'application/json',
            data: JSON.stringify({ statusId: 3 })
        }).done(function () {
            self.loadAppointments();
            toastr.success(appointmentT('salon.appointments.completed', 'Randevu tamamlandı'));
        });
    };

    self.cancel = function (appt) {
        confirmModal(appointmentT('salon.common.btn.confirm', 'Onayla'), appointmentT('salon.appointments.cancel_confirm', 'Bu randevuyu iptal etmek istediğinize emin misiniz?'), function() {
            $.ajax({
                url: '/proxy/sln-appointments/' + appt.id + '/status',
                method: 'PUT',
                contentType: 'application/json',
                data: JSON.stringify({ statusId: 4 })
            }).done(function (res) {
                self.loadAppointments();
                if (res && res.penalty > 0) {
                    toastr.warning(appointmentT('salon.appointments.late_cancel_prefix', 'Geç iptal') + ' — ' + res.message);
                } else {
                    toastr.success(appointmentT('salon.appointments.cancelled', 'Randevu iptal edildi'));
                }
            });
        });
    };

    self.markNoShow = function (appt) {
        confirmModal(appointmentT('salon.common.btn.confirm', 'Onayla'), appointmentT('salon.appointments.no_show_confirm', 'Bu müşteriyi gelmedi olarak işaretlemek istiyor musunuz?'), function() {
            $.ajax({
                url: '/proxy/sln-appointments/' + appt.id + '/status',
                method: 'PUT',
                contentType: 'application/json',
                data: JSON.stringify({ statusId: 5 })
            }).done(function (res) {
                self.loadAppointments();
                var msg = appointmentT('salon.appointments.no_show_marked', 'Müşteri gelmedi olarak işaretlendi');
                if (res && res.penalty > 0) msg += ' — ' + res.message;
                toastr.warning(msg);
            });
        });
    };

    // ═══ Yeni Musteri Olusturma ═══
    self.newClientVisible = ko.observable(false);
    self.isCreatingClient = ko.observable(false);
    self.newClientForm = {
        fullName: ko.observable(''),
        phone: ko.observable('')
    };

    self.showNewClient = function (queryText) {
        self.newClientForm.fullName(queryText || '');
        self.newClientForm.phone('');
        self.newClientVisible(true);
        self.clientAutocomplete.showDropdown(false);
    };

    self.hideNewClient = function () {
        self.newClientVisible(false);
    };

    self.saveNewClient = function () {
        var name = self.newClientForm.fullName();
        var phone = self.newClientForm.phone();
        if (!name || !phone) { toastr.warning(appointmentT('salon.memberships.name_phone_required', 'Ad ve telefon zorunludur')); return; }

        self.isCreatingClient(true);
        $.ajax({
            url: '/proxy/sln-clients',
            method: 'POST',
            contentType: 'application/json',
            data: JSON.stringify({ fullName: name, phone: phone })
        }).done(function (newClient) {
            // Listeye ekle ve sec
            var list = self.clientList();
            list.push(newClient);
            self.clientList(list);
            self.form.slnClientId(newClient.id);
            self.clientAutocomplete.query(newClient.fullName);
            self.clientAutocomplete.selectedName(newClient.fullName);
            self.newClientVisible(false);
            toastr.success(appointmentT('salon.memberships.customer_created_selected', 'Müşteri oluşturuldu ve seçildi'));
        }).fail(function (xhr) {
            toastr.error(xhr.responseJSON?.error || appointmentT('salon.memberships.customer_create_failed', 'Müşteri oluşturulamadı'));
        }).always(function () { self.isCreatingClient(false); });
    };

    // ═══ Sube Normalize ═══
    self.normalizeBranches = function () {
        confirmModal(appointmentT('salon.common.btn.confirm', 'Onayla'), appointmentT('salon.appointments.normalize_confirm', 'Tüm randevular personellerinin şubesine göre senkronize edilecek. Personel şubesi yoksa merkez atanır. Devam?'), function () {
            $.ajax({
                url: '/proxy/sln-appointments/normalize-branches?_nb=1',
                method: 'POST'
            }).done(function (res) {
                if (res && res.error) { toastr.error(res.error); return; }
                var msg = (res.updated || 0) + ' ' + appointmentT('salon.appointments.updated_suffix', 'randevu güncellendi');
                if (res.syncedFromPersonnel > 0) msg += ' (' + appointmentT('salon.appointments.from_staff_suffix', 'personelden') + ': ' + res.syncedFromPersonnel + ')';
                if (res.assignedToHq > 0) msg += ' (' + appointmentT('salon.appointments.to_hq_suffix', 'merkeze') + ': ' + res.assignedToHq + ')';
                toastr.success(msg);
                self.loadAppointments();
            }).fail(function () {
                toastr.error(appointmentT('salon.appointments.normalize_failed', 'Normalize başarısız'));
            });
        });
    };

    $(document).ready(function () {
        formModal = new bootstrap.Modal(document.getElementById('appointmentModal'));
        self.loadLookups();
        self.loadWaitlistHint();
        // Default: Bu Hafta (Pazartesi - Pazar)
        var mon = getMonday(new Date());
        self.dateRanges.push({
            from: toDateStr(mon),
            to: toDateStr(addDays(mon, 6)),
            label: appointmentT('salon.reports.this_week', 'Bu Hafta')
        });
    });
}

ko.applyBindings(new AppointmentsViewModel(), document.getElementById('appointments-vm'));
