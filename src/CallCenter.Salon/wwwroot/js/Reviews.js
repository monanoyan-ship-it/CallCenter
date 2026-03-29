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

    var sourceTexts = { 1: 'Dahili', 2: 'Google', 3: 'Instagram', 4: 'Facebook' };
    var reviewStatusTexts = { 1: 'Bekliyor', 2: 'Onaylandi', 3: 'Reddedildi' };
    var reviewStatusBadges = { 1: 'bg-warning', 2: 'bg-success', 3: 'bg-danger' };

    self.sourceText = function (id) { return sourceTexts[id] || 'Bilinmiyor'; };
    self.reviewStatusText = function (id) { return reviewStatusTexts[id] || 'Bilinmiyor'; };
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
            toastr.warning('Musteri adi zorunludur');
            return;
        }

        self.isSaving(true);
        $.ajax({
            url: '/proxy/sln-reviews', method: 'POST',
            contentType: 'application/json', data: JSON.stringify(data)
        }).done(function () {
            formModal.hide();
            self.loadData();
            toastr.success('Yorum eklendi');
            self.isSaving(false);
        }).fail(function (xhr) {
            toastr.error(xhr.responseJSON || 'Bir hata olustu');
            self.isSaving(false);
        });
    };

    self.updateStatus = function (review, statusId) {
        $.ajax({ url: '/proxy/sln-reviews/' + review.id + '/status/' + statusId, method: 'PUT' })
            .done(function () { self.loadData(); toastr.success('Yorum durumu guncellendi'); })
            .fail(function () { toastr.error('Guncellenemedi'); });
    };

    self.remove = function (review) {
        if (!confirm('Bu yorumu silmek istediginize emin misiniz?')) return;
        $.ajax({ url: '/proxy/sln-reviews/' + review.id, method: 'DELETE' })
            .done(function () { self.loadData(); toastr.success('Yorum silindi'); })
            .fail(function () { toastr.error('Silinemedi'); });
    };

    $(document).ready(function () {
        formModal = new bootstrap.Modal(document.getElementById('reviewModal'));
        self.loadData();
    });
}

ko.applyBindings(new ReviewsViewModel(), document.getElementById('reviews-vm'));
