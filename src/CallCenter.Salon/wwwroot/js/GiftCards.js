function GiftCardsViewModel() {
    var self = this;
    self.cards = ko.observableArray([]);
    self.searchQuery = ko.observable('');
    self.isSaving = ko.observable(false);

    self.form = {
        amount: ko.observable(100),
        recipientName: ko.observable(''),
        recipientPhone: ko.observable(''),
        senderName: ko.observable(''),
        message: ko.observable(''),
        expiresAt: ko.observable('')
    };

    self.filteredCards = ko.computed(function () {
        var q = (self.searchQuery() || '').toLowerCase();
        if (!q) return self.cards();
        return self.cards().filter(function (c) {
            return (c.code || '').toLowerCase().indexOf(q) >= 0
                || (c.recipientName || '').toLowerCase().indexOf(q) >= 0
                || (c.senderName || '').toLowerCase().indexOf(q) >= 0;
        });
    });

    var formModal;

    self.loadData = function () {
        $.ajax({ url: '/proxy/sln-gift-cards', method: 'GET' }).done(function (data) {
            self.cards(data.items || data);
        });
    };

    self.openNew = function () {
        self.form.amount(100);
        self.form.recipientName('');
        self.form.recipientPhone('');
        self.form.senderName('');
        self.form.message('');
        self.form.expiresAt('');
        formModal.show();
    };

    self.save = function () {
        var data = {
            amount: parseFloat(self.form.amount()) || 0,
            recipientName: self.form.recipientName(),
            recipientPhone: self.form.recipientPhone(),
            senderName: self.form.senderName(),
            message: self.form.message(),
            expiresAt: self.form.expiresAt() ? self.form.expiresAt() + 'T23:59:59Z' : null
        };

        if (!data.amount || data.amount <= 0) { toastr.warning('Tutar zorunludur'); return; }

        self.isSaving(true);
        $.ajax({
            url: '/proxy/sln-gift-cards',
            method: 'POST',
            contentType: 'application/json',
            data: JSON.stringify(data)
        }).done(function (result) {
            formModal.hide();
            self.loadData();
            toastr.success('Hediye karti olusturuldu: ' + result.code);
            self.isSaving(false);
        }).fail(function (xhr) {
            toastr.error(xhr.responseJSON?.error || 'Bir hata olustu');
            self.isSaving(false);
        });
    };

    self.deactivate = function (card) {
        if (!confirm("'" + card.code + "' kartini iptal etmek istediginize emin misiniz?")) return;
        $.ajax({
            url: '/proxy/sln-gift-cards/' + card.id + '/deactivate',
            method: 'PUT'
        }).done(function () {
            self.loadData();
            toastr.success('Hediye karti iptal edildi');
        });
    };

    $(document).ready(function () {
        formModal = new bootstrap.Modal(document.getElementById('giftCardModal'));
        self.loadData();
    });
}

ko.applyBindings(new GiftCardsViewModel(), document.getElementById('giftcards-vm'));
