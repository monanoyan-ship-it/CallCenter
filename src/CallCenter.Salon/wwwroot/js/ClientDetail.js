function slnJsT(key, fallback) {
    return (window.salonT || function (k, f) { return f || k; })(key, fallback);
}

function ClientDetailViewModel() {
    var self = this;
    var id = clientDetailId;

    self.client = ko.observable({});
    self.formulas = ko.observableArray([]);
    self.photos = ko.observableArray([]);
    self.treatmentRecords = ko.observableArray([]);
    self.appointments = ko.observableArray([]);
    self.invoices = ko.observableArray([]);
    self.walletMemberships = ko.observableArray([]);
    self.walletPackages = ko.observableArray([]);
    self.walletGiftCards = ko.observableArray([]);
    self.loyalty = ko.observable(null);
    self.totalSpent = ko.observable(0);
    self.lastVisit = ko.observable(null);
    self.isSaving = ko.observable(false);

    self.formulaForm = {
        formulaText: ko.observable(''),
        colorCode: ko.observable(''),
        oxidantRatio: ko.observable(''),
        applicationNotes: ko.observable('')
    };

    self.healthForm = {
        skinType: ko.observable(''),
        skinSensitivity: ko.observable(''),
        allergies: ko.observable(''),
        contraindications: ko.observable(''),
        medicalNotes: ko.observable('')
    };

    self.treatmentForm = {
        slnAppointmentId: ko.observable(null),
        serviceId: ko.observable(null),
        personnelId: ko.observable(null),
        appointmentSummary: ko.observable(''),
        treatmentDate: ko.observable(''),
        sessionNotes: ko.observable(''),
        deviceParameters: ko.observable(''),
        productNotes: ko.observable(''),
        aftercareNotes: ko.observable('')
    };

    var formulaModal, photoModal, treatmentModal;

    var appointmentStatusNames = {
        1: slnJsT('salon.appointments.status.scheduled', 'Planlanmış'),
        2: slnJsT('salon.appointments.status.confirmed', 'Onaylandı'),
        3: slnJsT('salon.appointments.status.completed', 'Tamamlandı'),
        4: slnJsT('salon.appointments.status.cancelled', 'İptal'),
        5: slnJsT('salon.appointments.status.no_show', 'Gelmedi')
    };
    var appointmentStatusCss = { 1: 'bg-warning text-dark', 2: 'bg-info', 3: 'bg-success', 4: 'bg-danger', 5: 'bg-secondary' };
    var invoiceStatusNames = {
        1: slnJsT('salon.invoices.status.open', 'Açık'),
        2: slnJsT('salon.invoices.status.paid', 'Ödendi'),
        3: slnJsT('salon.appointments.status.cancelled', 'İptal')
    };
    var invoiceStatusCss = { 1: 'bg-warning text-dark', 2: 'bg-success', 3: 'bg-danger' };

    function toLocalDateTimeInput(value) {
        if (!value) return '';
        var date = new Date(value);
        date.setMinutes(date.getMinutes() - date.getTimezoneOffset());
        return date.toISOString().slice(0, 16);
    }

    function fmtMoney(value) {
        return (Number(value) || 0).toLocaleString(document.documentElement.lang || undefined) + ' TL';
    }

    function normalizePhone(value) {
        return String(value || '').replace(/\D/g, '').replace(/^90/, '').replace(/^0/, '');
    }

    function silentGet(url, done) {
        $.ajax({ url: url, method: 'GET', global: false }).done(done);
    }

    self.activeMemberships = ko.computed(function () {
        return self.walletMemberships().filter(function (m) { return m.statusId === 1; });
    });

    self.activePackages = ko.computed(function () {
        return self.walletPackages().filter(function (pkg) {
            return pkg.isActive !== false && (pkg.remainingSessions || 0) > 0;
        });
    });

    self.loyaltyPointsText = ko.computed(function () {
        var loyalty = self.loyalty();
        return loyalty ? ((loyalty.currentBalance || 0).toLocaleString(document.documentElement.lang || undefined) + ' ' + slnJsT('salon.clients.wallet.points_suffix', 'puan')) : '-';
    });

    self.loyaltyValueText = ko.computed(function () {
        var loyalty = self.loyalty();
        return loyalty ? slnJsT('salon.clients.wallet.balance_value', 'Yaklaşık değer') + ': ' + fmtMoney(loyalty.balanceValue) : slnJsT('salon.clients.wallet.no_loyalty', 'Sadakat kaydı yok');
    });

    self.membershipText = ko.computed(function () {
        var active = self.activeMemberships();
        if (!active.length) return slnJsT('salon.clients.wallet.membership_none', 'Aktif üyelik yok');
        return active[0].planName || slnJsT('salon.clients.wallet.membership', 'Üyelik');
    });

    self.membershipSubtext = ko.computed(function () {
        var active = self.activeMemberships();
        if (!active.length) return slnJsT('salon.clients.wallet.membership_hint', 'Satıştan veya üyelik formundan eklenebilir');
        var suffix = active.length > 1 ? ' +' + (active.length - 1) : '';
        var discount = active[0].discountPercent ? '%' + active[0].discountPercent + ' ' + slnJsT('salon.profile.membership.discount_suffix', 'indirim') : slnJsT('salon.common.status_active', 'Aktif');
        return discount + suffix;
    });

    self.packageSessionsText = ko.computed(function () {
        var total = self.activePackages().reduce(function (sum, pkg) { return sum + (pkg.remainingSessions || 0); }, 0);
        return total > 0 ? total.toLocaleString(document.documentElement.lang || undefined) + ' ' + slnJsT('salon.clients.wallet.session_rights_suffix', 'seans') : '-';
    });

    self.packageSubtext = ko.computed(function () {
        var packages = self.activePackages();
        if (!packages.length) return slnJsT('salon.clients.wallet.no_packages', 'Aktif seans hakkı yok');
        return packages.length + ' ' + slnJsT('salon.clients.wallet.package_count_suffix', 'aktif seans paketi');
    });

    self.giftCardBalanceText = ko.computed(function () {
        var balance = self.walletGiftCards().reduce(function (sum, card) { return sum + (card.remainingBalance || 0); }, 0);
        return balance > 0 ? fmtMoney(balance) : '-';
    });

    self.giftCardSubtext = ko.computed(function () {
        var cards = self.walletGiftCards();
        if (!cards.length) return slnJsT('salon.clients.wallet.no_gift_cards', 'Aktif hediye kartı yok');
        return cards.length + ' ' + slnJsT('salon.clients.wallet.gift_card_count_suffix', 'kart');
    });

    function getAppointmentServiceOptions(appointment) {
        if (!appointment) return [];
        var ids = appointment.serviceIds || [];
        var names = appointment.serviceNames || [];
        if (!ids.length && appointment.serviceId) ids = [appointment.serviceId];
        if (!names.length && appointment.serviceName) names = [appointment.serviceName];
        return ids.map(function (serviceId, index) {
            return {
                id: serviceId,
                name: names[index] || slnJsT('salon.common.service', 'Hizmet') + ' #' + serviceId
            };
        });
    }

    function formatAppointmentLabel(appointment) {
        if (!appointment) return '';
        var serviceText = (appointment.serviceNames && appointment.serviceNames.length)
            ? appointment.serviceNames.join(', ')
            : (appointment.serviceName || '-');
        var timeText = appointment.startTime ? new Date(appointment.startTime).toLocaleString(document.documentElement.lang || undefined) : '-';
        return timeText + ' - ' + serviceText;
    }

    function findAppointment(appointmentId) {
        var parsedId = parseInt(appointmentId);
        if (!parsedId) return null;
        return self.appointments().find(function (appointment) { return appointment.id === parsedId; }) || null;
    }

    function hasTreatmentRecordForAppointment(appointmentId) {
        return self.treatmentRecords().some(function (record) { return record.slnAppointmentId === appointmentId; });
    }

    self.loadClient = function () {
        $.ajax({ url: '/proxy/sln-clients/' + id, method: 'GET' }).done(function (data) {
            data.genderText = data.genderId === 1 ? slnJsT('salon.common.gender.male', 'Erkek') : data.genderId === 2 ? slnJsT('salon.common.gender.female', 'Kadın') : '';
            if (data.birthDate) {
                var bd = new Date(data.birthDate);
                var today = new Date();
                data.age = today.getFullYear() - bd.getFullYear();
                if (today.getMonth() < bd.getMonth() || (today.getMonth() === bd.getMonth() && today.getDate() < bd.getDate())) data.age--;
            }
            self.client(data);
            self.formulas(data.formulas || []);
            self.photos(data.photos || []);
            self.treatmentRecords(data.treatmentRecords || []);
            self.healthForm.skinType(data.skinType || '');
            self.healthForm.skinSensitivity(data.skinSensitivity || '');
            self.healthForm.allergies(data.allergies || '');
            self.healthForm.contraindications(data.contraindications || '');
            self.healthForm.medicalNotes(data.medicalNotes || '');
            self.totalSpent(data.totalSpent || 0);
            self.lastVisit(data.lastVisit ? new Date(data.lastVisit).toLocaleDateString(document.documentElement.lang || undefined) : null);
            self.loadWallet();
        }).fail(function () {
            toastr.error(slnJsT('salon.clientdetail.js.musteri_bilgisi_yuklenemedi', 'Müşteri bilgisi yüklenemedi'));
        });
    };

    self.loadWallet = function () {
        silentGet('/proxy/sln-loyalty/clients/' + id, function (data) {
            self.loyalty(data || null);
        });
        silentGet('/proxy/sln-memberships?clientId=' + id, function (data) {
            self.walletMemberships(data.items || data || []);
        });
        silentGet('/proxy/sln-packages/client-packages?clientId=' + id, function (data) {
            self.walletPackages(data.items || data || []);
        });
        silentGet('/proxy/sln-gift-cards', function (data) {
            var phone = normalizePhone(self.client().phone);
            var cards = (data.items || data || []).filter(function (card) {
                return card.isActive !== false
                    && (card.remainingBalance || 0) > 0
                    && phone
                    && normalizePhone(card.recipientPhone) === phone;
            });
            self.walletGiftCards(cards);
        });
    };

    self.saveHealthInfo = function () {
        var data = {
            skinType: self.healthForm.skinType(),
            skinSensitivity: self.healthForm.skinSensitivity(),
            allergies: self.healthForm.allergies(),
            contraindications: self.healthForm.contraindications(),
            medicalNotes: self.healthForm.medicalNotes()
        };

        self.isSaving(true);
        $.ajax({
            url: '/proxy/sln-clients/' + id + '/health',
            method: 'PUT',
            contentType: 'application/json',
            data: JSON.stringify(data)
        }).done(function () {
            self.loadClient();
            toastr.success(slnJsT('salon.clients.health.saved', 'Saglik bilgileri kaydedildi'));
            self.isSaving(false);
        }).fail(function () {
            toastr.error(slnJsT('salon.clients.health.save_failed', 'Saglik bilgileri kaydedilemedi'));
            self.isSaving(false);
        });
    };

    self.reviewHealthInfo = function () {
        self.isSaving(true);
        $.ajax({
            url: '/proxy/sln-clients/' + id + '/health/review',
            method: 'PUT'
        }).done(function () {
            self.loadClient();
            toastr.success(slnJsT('salon.clients.health.reviewed', 'Saglik bilgileri incelendi olarak isaretlendi'));
            self.isSaving(false);
        }).fail(function () {
            toastr.error(slnJsT('salon.clients.health.review_failed', 'Inceleme durumu guncellenemedi'));
            self.isSaving(false);
        });
    };

    self.openTreatmentRecord = function (appointment) {
        resetTreatmentForm();
        if (appointment && appointment.id) {
            self.treatmentForm.slnAppointmentId(appointment.id);
        }
        treatmentModal.show();
    };

    self.saveTreatmentRecord = function () {
        var data = {
            slnClientId: id,
            slnAppointmentId: self.treatmentForm.slnAppointmentId() ? parseInt(self.treatmentForm.slnAppointmentId()) : null,
            serviceId: self.treatmentForm.serviceId() ? parseInt(self.treatmentForm.serviceId()) : null,
            personnelId: self.treatmentForm.personnelId() ? parseInt(self.treatmentForm.personnelId()) : null,
            treatmentDate: self.treatmentForm.treatmentDate() ? new Date(self.treatmentForm.treatmentDate()).toISOString() : null,
            sessionNotes: self.treatmentForm.sessionNotes(),
            deviceParameters: self.treatmentForm.deviceParameters(),
            productNotes: self.treatmentForm.productNotes(),
            aftercareNotes: self.treatmentForm.aftercareNotes()
        };

        if (!data.sessionNotes && !data.deviceParameters && !data.productNotes && !data.aftercareNotes) {
            toastr.warning(slnJsT('salon.clients.treatment.note_required', 'En az bir seans notu giriniz'));
            return;
        }

        self.isSaving(true);
        $.ajax({
            url: '/proxy/sln-clients/treatment-records',
            method: 'POST',
            contentType: 'application/json',
            data: JSON.stringify(data)
        }).done(function () {
            treatmentModal.hide();
            self.loadClient();
            toastr.success(slnJsT('salon.clients.treatment.saved', 'Seans kaydi kaydedildi'));
            self.isSaving(false);
        }).fail(function () {
            toastr.error(slnJsT('salon.clients.treatment.save_failed', 'Seans kaydi kaydedilemedi'));
            self.isSaving(false);
        });
    };

    self.removeTreatmentRecord = function (record) {
        confirmModal(slnJsT('salon.common.btn.confirm', 'Onayla'), slnJsT('salon.clients.treatment.delete_confirm', 'Bu seans kaydini silmek istediginize emin misiniz?'), function() {
            $.ajax({
                url: '/proxy/sln-clients/treatment-records/' + record.id,
                method: 'DELETE'
            }).done(function () {
                self.loadClient();
                toastr.success(slnJsT('salon.clients.treatment.deleted', 'Seans kaydi silindi'));
            });
        });
    };

    self.loadAppointments = function () {
        $.ajax({ url: '/proxy/sln-appointments?slnClientId=' + id, method: 'GET' }).done(function (data) {
            var items = data.items || data;
            items.forEach(function (a) {
                a.statusText = appointmentStatusNames[a.statusId] || slnJsT('salon.common.unknown', 'Bilinmiyor');
                a.statusCss = appointmentStatusCss[a.statusId] || 'bg-secondary';
            });
            self.appointments(items);
        });
    };

    self.treatmentAppointmentOptions = ko.computed(function () {
        return self.appointments().map(function (appointment) {
            return {
                id: appointment.id,
                label: formatAppointmentLabel(appointment)
            };
        });
    });

    self.treatmentServiceOptions = ko.computed(function () {
        return getAppointmentServiceOptions(findAppointment(self.treatmentForm.slnAppointmentId()));
    });

    self.hasTreatmentRecordForAppointment = function (appointment) {
        return appointment && hasTreatmentRecordForAppointment(appointment.id);
    };

    function resetTreatmentForm() {
        var now = new Date();
        now.setMinutes(now.getMinutes() - now.getTimezoneOffset());
        self.treatmentForm.slnAppointmentId(null);
        self.treatmentForm.serviceId(null);
        self.treatmentForm.personnelId(null);
        self.treatmentForm.appointmentSummary('');
        self.treatmentForm.treatmentDate(now.toISOString().slice(0, 16));
        self.treatmentForm.sessionNotes('');
        self.treatmentForm.deviceParameters('');
        self.treatmentForm.productNotes('');
        self.treatmentForm.aftercareNotes('');
    }

    function populateTreatmentFromAppointment(appointment) {
        if (!appointment) {
            self.treatmentForm.appointmentSummary('');
            self.treatmentForm.serviceId(null);
            self.treatmentForm.personnelId(null);
            return;
        }

        var serviceOptions = getAppointmentServiceOptions(appointment);
        var serviceNames = serviceOptions.map(function (service) { return service.name; }).join(', ');
        self.treatmentForm.slnAppointmentId(appointment.id);
        self.treatmentForm.serviceId(serviceOptions.length ? serviceOptions[0].id : null);
        self.treatmentForm.personnelId(appointment.personnelId || null);
        self.treatmentForm.appointmentSummary(formatAppointmentLabel(appointment));
        self.treatmentForm.treatmentDate(toLocalDateTimeInput(appointment.startTime));
        self.treatmentForm.sessionNotes(
            slnJsT('salon.clients.treatment.appointment_note_template', 'Randevu seansi tamamlandi.')
                + (serviceNames ? ' ' + serviceNames : '')
                + (appointment.personnelName ? ' - ' + appointment.personnelName : '')
        );

        if (hasTreatmentRecordForAppointment(appointment.id)) {
            toastr.info(slnJsT('salon.clients.treatment.appointment_has_record', 'Bu randevuya bagli seans kaydi zaten var. Gerekirse yeni not olarak kaydedebilirsiniz.'));
        }
    }

    self.treatmentForm.slnAppointmentId.subscribe(function (appointmentId) {
        populateTreatmentFromAppointment(findAppointment(appointmentId));
    });

    self.loadInvoices = function () {
        $.ajax({ url: '/proxy/sln-finance/invoices?slnClientId=' + id, method: 'GET' }).done(function (data) {
            var items = data.items || data;
            items.forEach(function (inv) {
                inv.statusText = invoiceStatusNames[inv.statusId] || slnJsT('salon.common.unknown', 'Bilinmiyor');
                inv.statusCss = invoiceStatusCss[inv.statusId] || 'bg-secondary';
                inv.servicesSummary = (inv.items || []).map(function (it) { return it.itemName; }).join(', ') || '-';
            });
            self.invoices(items);
        });
    };

    // Formula CRUD
    self.openNewFormula = function () {
        self.formulaForm.formulaText('');
        self.formulaForm.colorCode('');
        self.formulaForm.oxidantRatio('');
        self.formulaForm.applicationNotes('');
        formulaModal.show();
    };

    self.saveFormula = function () {
        var data = {
            slnClientId: id,
            formulaText: self.formulaForm.formulaText(),
            colorCode: self.formulaForm.colorCode(),
            oxidantRatio: self.formulaForm.oxidantRatio(),
            applicationNotes: self.formulaForm.applicationNotes()
        };
        if (!data.formulaText) { toastr.warning(slnJsT('salon.clientdetail.js.formula_required', 'Formül metni zorunludur')); return; }

        self.isSaving(true);
        $.ajax({
            url: '/proxy/sln-clients/formulas',
            method: 'POST',
            contentType: 'application/json',
            data: JSON.stringify(data)
        }).done(function () {
            formulaModal.hide();
            self.loadClient();
            toastr.success(slnJsT('salon.clientdetail.js.formul_kaydedildi', 'Formül kaydedildi'));
            self.isSaving(false);
        }).fail(function () {
            toastr.error(slnJsT('salon.clientdetail.js.formula_save_failed', 'Formül kaydedilemedi'));
            self.isSaving(false);
        });
    };

    self.removeFormula = function (formula) {
        confirmModal(slnJsT('salon.common.btn.confirm', 'Onayla'), slnJsT('salon.clientdetail.js.formula_delete_confirm', 'Bu formülü silmek istediğinize emin misiniz?'), function() {
            $.ajax({
                url: '/proxy/sln-clients/formulas/' + formula.id,
                method: 'DELETE'
            }).done(function () {
                self.loadClient();
                toastr.success(slnJsT('salon.clientdetail.js.formul_silindi', 'Formül silindi'));
            });
        });
    };

    // Photo CRUD
    self.openPhotoUpload = function () {
        document.getElementById('photoFile').value = '';
        photoModal.show();
    };

    self.uploadPhoto = function () {
        var fileInput = document.getElementById('photoFile');
        if (!fileInput.files || !fileInput.files[0]) {
            toastr.warning(slnJsT('salon.clientdetail.js.photo_required', 'Lütfen bir fotoğraf seçiniz'));
            return;
        }

        var formData = new FormData();
        formData.append('file', fileInput.files[0]);

        self.isSaving(true);
        $.ajax({
            url: '/proxy/sln-clients/' + id + '/photos',
            method: 'POST',
            data: formData,
            processData: false,
            contentType: false
        }).done(function () {
            photoModal.hide();
            self.loadClient();
            toastr.success(slnJsT('salon.clientdetail.js.photo_uploaded', 'Fotoğraf yüklendi'));
            self.isSaving(false);
        }).fail(function () {
            toastr.error(slnJsT('salon.clientdetail.js.photo_upload_failed', 'Fotoğraf yüklenemedi'));
            self.isSaving(false);
        });
    };

    self.removePhoto = function (photo) {
        confirmModal(slnJsT('salon.common.btn.confirm', 'Onayla'), slnJsT('salon.clientdetail.js.photo_delete_confirm', 'Bu fotoğrafı silmek istediğinize emin misiniz?'), function() {
            $.ajax({
                url: '/proxy/sln-clients/' + id + '/photos/' + photo.id,
                method: 'DELETE'
            }).done(function () {
                self.loadClient();
                toastr.success(slnJsT('salon.clientdetail.js.fotograf_silindi', 'Fotoğraf silindi'));
            });
        });
    };

    $(document).ready(function () {
        formulaModal = new bootstrap.Modal(document.getElementById('formulaModal'));
        photoModal = new bootstrap.Modal(document.getElementById('photoModal'));
        treatmentModal = new bootstrap.Modal(document.getElementById('treatmentModal'));
        self.loadClient();
        self.loadAppointments();
        self.loadInvoices();
    });
}

ko.applyBindings(new ClientDetailViewModel(), document.getElementById('client-detail-vm'));
