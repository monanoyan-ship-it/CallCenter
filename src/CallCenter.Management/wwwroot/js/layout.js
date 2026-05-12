(function () {
    var redirecting = false;

    function toggleGroup(btn) {
        var items = btn.nextElementSibling;
        var chevron = btn.querySelector('.chevron');
        if (!items) return;
        items.classList.toggle('show');
        if (chevron) chevron.classList.toggle('open');
    }

    $.ajaxSetup({
        dataType: 'json',
        error: function (xhr) {
            if (xhr.status === 401 && !redirecting && !location.pathname.match(/^\/Account\/Login/i)) {
                redirecting = true;
                window.location.href = '/Account/Login?returnUrl=' + encodeURIComponent(location.pathname + location.search);
            }
        }
    });

    toastr.options = { closeButton: true, progressBar: true, positionClass: 'toast-top-right', timeOut: 3000 };

    document.addEventListener('click', function (event) {
        var toggle = event.target.closest('[data-nav-group-toggle]');
        if (!toggle) return;
        toggleGroup(toggle);
    });
})();
