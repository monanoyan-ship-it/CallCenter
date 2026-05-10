function slnJsT(key, fallback) {
    return (window.salonT || function (k, f) { return f || k; })(key, fallback);
}

function ReviewsViewModel() {
    var self = this;
    self.reviews = ko.observableArray([]);
    self.stats = ko.observable({ totalReviews: 0, pendingCount: 0, approvedCount: 0, rejectedCount: 0, averageRating: 0 });
    self.filterStatus = ko.observable(0);
    self.isSaving = ko.observable(false);

    self.form = {
        clientName: ko.observable(''),
        rating: ko.observable('5'),
        sourceId: ko.observable('1'),
        comment: ko.observable(''),
        externalUrl: ko.observable('')
    };

    var sourceTexts = {
        1: slnJsT('salon.reviews.source.internal', 'Dahili'),
        2: 'Google',
        3: 'Instagram',
        4: 'Facebook'
    };
    var reviewStatusTexts = {
        1: slnJsT('salon.reviews.status.pending', 'Bekliyor'),
        2: slnJsT('salon.reviews.status.approved', 'Onaylandı'),
        3: slnJsT('salon.reviews.status.rejected', 'Reddedildi')
    };
    var reviewStatusBadges = { 1: 'bg-warning', 2: 'bg-success', 3: 'bg-danger' };

    self.sourceText = function (id) { return sourceTexts[id] || slnJsT('salon.common.unknown', 'Bilinmiyor'); };
    self.reviewStatusText = function (id) { return reviewStatusTexts[id] || slnJsT('salon.common.unknown', 'Bilinmiyor'); };
    self.reviewStatusBadge = function (id) { return reviewStatusBadges[id] || 'bg-secondary'; };

    self.filteredReviews = ko.computed(function () {
        var status = self.filterStatus();
        if (status === 0) return self.reviews();
        return self.reviews().filter(function (r) { return r.statusId === status; });
    });

    var formModal;

    self.loadData = function () {
        $.ajax({ url: '/proxy/sln-reviews', method: 'GET' }).done(function (data) {
            self.reviews(data.items || data);
        });
        $.ajax({ url: '/proxy/sln-reviews/stats', method: 'GET' }).done(function (data) {
            self.stats(data);
        });
    };

    self.openNew = function () {
        self.form.clientName('');
        self.form.rating('5');
        self.form.sourceId('1');
        self.form.comment('');
        self.form.externalUrl('');
        formModal.show();
    };

    self.save = function () {
        var data = {
            clientName: self.form.clientName(),
            rating: parseInt(self.form.rating()) || 5,
            sourceId: parseInt(self.form.sourceId()) || 1,
            comment: self.form.comment() || null,
            externalUrl: self.form.externalUrl() || null
        };

        if (!data.clientName) {
            toastr.warning(slnJsT('salon.reviews.js.musteri_adi_zorunludur', 'Müşteri adı zorunludur'));
            return;
        }

        self.isSaving(true);
        $.ajax({
            url: '/proxy/sln-reviews', method: 'POST',
            contentType: 'application/json', data: JSON.stringify(data)
        }).done(function () {
            formModal.hide();
            self.loadData();
            toastr.success(slnJsT('salon.reviews.js.yorum_eklendi', 'Yorum eklendi'));
            self.isSaving(false);
        }).fail(function (xhr) {
            toastr.error(xhr.responseJSON || slnJsT('salon.common.error.generic', 'Bir hata oluştu'));
            self.isSaving(false);
        });
    };

    self.updateStatus = function (review, statusId) {
        $.ajax({ url: '/proxy/sln-reviews/' + review.id + '/status/' + statusId, method: 'PUT' })
            .done(function () { self.loadData(); toastr.success(slnJsT('salon.reviews.js.yorum_durumu_guncellendi', 'Yorum durumu güncellendi')); })
            .fail(function () { toastr.error(slnJsT('salon.reviews.js.update_failed', 'Güncellenemedi')); });
    };

    self.remove = function (review) {
        confirmModal(slnJsT('salon.common.btn.confirm', 'Onayla'), slnJsT('salon.reviews.js.bu_yorumu_silmek_istediginize_emin_misiniz', 'Bu yorumu silmek istediğinize emin misiniz?'), function() {
            $.ajax({ url: '/proxy/sln-reviews/' + review.id, method: 'DELETE' })
                .done(function () { self.loadData(); toastr.success(slnJsT('salon.reviews.js.yorum_silindi', 'Yorum silindi')); })
                .fail(function () { toastr.error(slnJsT('salon.common.delete_failed', 'Silinemedi')); });
        });
    };

    $(document).ready(function () {
        formModal = new bootstrap.Modal(document.getElementById('reviewModal'));
        self.loadData();
    });
}

ko.applyBindings(new ReviewsViewModel(), document.getElementById('reviews-vm'));
