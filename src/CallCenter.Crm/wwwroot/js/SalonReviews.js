function SalonReviewsViewModel() {
    var self = this;
    var t = window.crmT || function (key, fallback) { return fallback || key; };

    self.reviews = ko.observableArray([]);
    self.stats = ko.observable({ totalReviews: 0, pendingCount: 0, approvedCount: 0, rejectedCount: 0, averageRating: 0 });
    self.filterStatus = ko.observable(0);

    var sourceTexts = {
        1: t('crm.salon.reviews.source.internal', 'Dahili'),
        2: 'Google',
        3: 'Instagram',
        4: 'Facebook'
    };
    var statusTexts = {
        1: t('crm.salon.reviews.status.pending', 'Bekliyor'),
        2: t('crm.salon.reviews.status.approved', 'Onaylandı'),
        3: t('crm.salon.reviews.status.rejected', 'Reddedildi')
    };
    var statusBadges = { 1: 'bg-warning text-dark', 2: 'bg-success', 3: 'bg-danger' };

    function readError(xhr, fallback) {
        return xhr.responseJSON?.error || xhr.responseJSON?.message || xhr.responseText || fallback;
    }

    function items(data) {
        return data && Array.isArray(data.items) ? data.items : (Array.isArray(data) ? data : []);
    }

    self.statusText = function (statusId) {
        return statusTexts[statusId] || t('crm.common.unknown', 'Bilinmiyor');
    };

    self.statusClass = function (statusId) {
        return statusBadges[statusId] || 'bg-secondary';
    };

    self.sourceText = function (sourceId) {
        return sourceTexts[sourceId] || t('crm.common.unknown', 'Bilinmiyor');
    };

    self.filteredReviews = ko.computed(function () {
        var status = parseInt(self.filterStatus(), 10) || 0;
        if (!status) return self.reviews();
        return self.reviews().filter(function (review) { return review.statusId === status; });
    });

    self.dateText = function (value) {
        if (!value) return '-';
        var d = new Date(value);
        return isNaN(d.getTime()) ? '-' : d.toLocaleDateString(document.documentElement.lang || undefined);
    };

    self.loadData = function () {
        $.get('/proxy/crm/salon/reviews')
            .done(function (data) { self.reviews(items(data)); })
            .fail(function (xhr) { toastr.error(readError(xhr, t('crm.salon.reviews.load_failed', 'Yorumlar yüklenemedi'))); });

        $.get('/proxy/crm/salon/reviews/stats')
            .done(function (data) { self.stats(data || {}); })
            .fail(function () { self.stats({ totalReviews: 0, pendingCount: 0, approvedCount: 0, rejectedCount: 0, averageRating: 0 }); });
    };

    self.updateStatus = function (review, statusId) {
        $.ajax({ url: '/proxy/crm/salon/reviews/' + review.id + '/status/' + statusId, method: 'PUT' })
            .done(function () {
                self.loadData();
                toastr.success(t('crm.salon.reviews.status_saved', 'Yorum durumu güncellendi'));
            })
            .fail(function (xhr) { toastr.error(readError(xhr, t('crm.salon.reviews.status_save_failed', 'Yorum durumu güncellenemedi'))); });
    };

    self.remove = function (review) {
        confirmModal(t('crm.common.confirm', 'Onayla'), t('crm.salon.reviews.delete_confirm', 'Bu yorumu silmek istediğinize emin misiniz?'), function () {
            $.ajax({ url: '/proxy/crm/salon/reviews/' + review.id, method: 'DELETE' })
                .done(function () {
                    self.loadData();
                    toastr.success(t('crm.salon.reviews.deleted', 'Yorum silindi'));
                })
                .fail(function (xhr) { toastr.error(readError(xhr, t('crm.common.delete_failed', 'Silinemedi'))); });
        });
    };

    $(document).ready(function () {
        self.loadData();
    });
}

ko.applyBindings(new SalonReviewsViewModel(), document.getElementById('salon-reviews-vm'));
