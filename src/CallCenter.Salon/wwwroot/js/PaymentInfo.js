function PaymentInfoViewModel() {
    var self = this;
    self.isLoading = ko.observable(true);
    self.isSaving = ko.observable(false);
    self.status = ko.observable(0); // 0=NotStarted, 1=Pending, 2=Approved, 3=Rejected
    self.onboardedAt = ko.observable(null);
    self.onboardedAtFormatted = ko.observable('');
    self.onboardingError = ko.observable('');
    self.commissionPercent = ko.observable(5);
    self.withholdingPercent = ko.observable(0);

    self.form = {
        subMerchantType: ko.observable('PERSONAL'),
        iban: ko.observable(''),
        contactName: ko.observable(''),
        contactSurname: ko.observable(''),
        identityNumber: ko.observable(''),
        legalCompanyTitle: ko.observable(''),
        taxOffice: ko.observable(''),
        taxNumber: ko.observable('')
    };

    self.load = function () {
        self.isLoading(true);
        $.get('/proxy/sln-profile/payment-info', function (data) {
            if (!data) { self.isLoading(false); return; }
            self.status(data.onboardingStatus || 0);
            self.onboardingError(data.onboardingError || '');
            self.commissionPercent(data.commissionPercent != null ? data.commissionPercent : 5);
            self.withholdingPercent(data.withholdingPercent != null ? data.withholdingPercent : 0);
            self.form.subMerchantType(data.subMerchantType || 'PERSONAL');
            self.form.iban(data.iban || '');
            self.form.contactName(data.contactName || '');
            self.form.contactSurname(data.contactSurname || '');
            self.form.identityNumber(data.identityNumber || '');
            self.form.legalCompanyTitle(data.legalCompanyTitle || '');
            self.form.taxOffice(data.taxOffice || '');
            self.form.taxNumber(data.taxNumber || '');

            if (data.onboardedAt) {
                self.onboardedAt(data.onboardedAt);
                try {
                    self.onboardedAtFormatted(new Date(data.onboardedAt).toLocaleString());
                } catch (e) { self.onboardedAtFormatted(''); }
            }
        }).fail(function () {
            toastr.error(salonT('salon.paymentinfo.load_error', 'Ödeme bilgileri yüklenemedi.'));
        }).always(function () {
            self.isLoading(false);
        });
    };

    self.submit = function () {
        var subType = self.form.subMerchantType();
        var iban = (self.form.iban() || '').trim();
        var contactName = (self.form.contactName() || '').trim();
        var contactSurname = (self.form.contactSurname() || '').trim();

        if (!iban) { toastr.warning(salonT('salon.paymentinfo.iban_required', 'IBAN zorunlu.')); return; }
        if (!contactName || !contactSurname) { toastr.warning(salonT('salon.paymentinfo.contact_required', 'Yetkili ad ve soyad zorunlu.')); return; }

        var payload = {
            subMerchantType: subType,
            iban: iban,
            contactName: contactName,
            contactSurname: contactSurname
        };

        if (subType === 'PERSONAL') {
            var idn = (self.form.identityNumber() || '').trim();
            if (!idn) { toastr.warning(salonT('salon.paymentinfo.identity_required', 'TC kimlik no zorunlu.')); return; }
            payload.identityNumber = idn;
        } else {
            var lct = (self.form.legalCompanyTitle() || '').trim();
            var to = (self.form.taxOffice() || '').trim();
            var tn = (self.form.taxNumber() || '').trim();
            if (!lct || !to || !tn) { toastr.warning(salonT('salon.paymentinfo.company_required', 'Şirket bilgileri (ünvan, vergi dairesi, vergi no) zorunlu.')); return; }
            payload.legalCompanyTitle = lct;
            payload.taxOffice = to;
            payload.taxNumber = tn;
        }

        self.isSaving(true);
        $.ajax({
            url: '/proxy/payments/sub-merchant',
            method: 'POST',
            contentType: 'application/json',
            data: JSON.stringify(payload),
            success: function () {
                toastr.success(salonT('salon.paymentinfo.save_success', 'Pazaryeri kaydı başarılı.'));
                self.load();
            },
            error: function (xhr) {
                toastr.error(xhr.responseJSON?.message || salonT('salon.paymentinfo.save_error', 'Pazaryeri kaydı başarısız.'));
            },
            complete: function () {
                self.isSaving(false);
            }
        });
    };

    self.load();
}

ko.applyBindings(new PaymentInfoViewModel(), document.getElementById('paymentinfo-vm'));
