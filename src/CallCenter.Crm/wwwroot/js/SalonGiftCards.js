function SalonGiftCardsViewModel() {
    var self = this;
    var t = window.crmT || function (key, fallback) { return fallback || key; };
    self.cards = ko.observableArray([]);
    self.searchQuery = ko.observable('');
    self.isSaving = ko.observable(false);

    self.form = {
        amount: ko.observable(100),
        paymentMethodId: ko.observable('1'),
        recipientName: ko.observable(''),
        recipientPhone: ko.observable(''),
        senderName: ko.observable(''),
        message: ko.observable(''),
        expiresAt: ko.observable('')
    };

    var formModal;

    self.money = function (value) {
        return (parseFloat(value) || 0).toLocaleString('tr-TR', { style: 'currency', currency: 'TRY' });
    };

    self.filteredCards = ko.computed(function () {
        var q = (self.searchQuery() || '').toLowerCase();
        if (!q) return self.cards();

        return self.cards().filter(function (card) {
            return (card.code || '').toLowerCase().indexOf(q) >= 0
                || (card.recipientName || '').toLowerCase().indexOf(q) >= 0
                || (card.senderName || '').toLowerCase().indexOf(q) >= 0
                || (card.recipientPhone || '').toLowerCase().indexOf(q) >= 0;
        });
    });

    function readError(xhr, fallback) {
        return xhr.responseJSON?.error || xhr.responseJSON?.message || xhr.responseText || fallback;
    }

    self.loadData = function () {
        $.get('/proxy/crm/salon/gift-cards')
            .done(function (data) {
                self.cards(data.items || data || []);
            })
            .fail(function (xhr) {
                toastr.error(readError(xhr, t('crm.salon.giftcards.load_failed', 'Hediye kartlari yuklenemedi')));
            });
    };

    self.openNew = function () {
        self.form.amount(100);
        self.form.paymentMethodId('1');
        self.form.recipientName('');
        self.form.recipientPhone('');
        self.form.senderName('');
        self.form.message('');
        self.form.expiresAt('');
        formModal.show();
    };

    self.save = function () {
        var payload = {
            amount: parseFloat(self.form.amount()) || 0,
            paymentMethodId: parseInt(self.form.paymentMethodId(), 10) || 1,
            recipientName: self.form.recipientName(),
            recipientPhone: self.form.recipientPhone(),
            senderName: self.form.senderName(),
            message: self.form.message(),
            expiresAt: self.form.expiresAt() ? self.form.expiresAt() + 'T23:59:59Z' : null
        };

        if (payload.amount <= 0) {
            toastr.warning(t('crm.salon.giftcards.amount_required_warning', 'Tutar zorunludur'));
            return;
        }

        self.isSaving(true);
        $.ajax({
            url: '/proxy/crm/salon/gift-cards',
            method: 'POST',
            contentType: 'application/json',
            data: JSON.stringify(payload)
        }).done(function (card) {
            formModal.hide();
            self.loadData();
            toastr.success(t('crm.salon.giftcards.created', 'Hediye karti olusturuldu: ') + (card.code || ''));
        }).fail(function (xhr) {
            toastr.error(readError(xhr, t('crm.salon.giftcards.create_failed', 'Hediye karti olusturulamadi')));
        }).always(function () {
            self.isSaving(false);
        });
    };

    self.deactivate = function (card) {
        confirmModal(
            t('crm.salon.giftcards.close_title', 'Hediye Kartini Kapat'),
            t('crm.salon.giftcards.close_confirm', "'{code}' kartini kapatmak istediginize emin misiniz?").replace('{code}', card.code || ''),
            function () {
            $.ajax({
                url: '/proxy/crm/salon/gift-cards/' + card.id + '/deactivate',
                method: 'PUT'
            }).done(function () {
                self.loadData();
                toastr.success(t('crm.salon.giftcards.closed', 'Hediye karti kapatildi'));
            }).fail(function (xhr) {
                toastr.error(readError(xhr, t('crm.salon.giftcards.close_failed', 'Hediye karti kapatilamadi')));
            });
        });
    };

    $(document).ready(function () {
        formModal = new bootstrap.Modal(document.getElementById('salonGiftCardModal'));
        self.loadData();
    });
}

ko.applyBindings(new SalonGiftCardsViewModel(), document.getElementById('salon-giftcards-vm'));
