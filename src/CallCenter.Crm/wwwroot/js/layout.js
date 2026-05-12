(function () {
    function toggleGroup(btn) {
        var items = btn.nextElementSibling;
        var chevron = btn.querySelector('.chevron');
        var isOpen = items && items.classList.contains('show');

        document.querySelectorAll('.nav-group-items.show').forEach(function (el) {
            el.classList.remove('show');
            var previous = el.previousElementSibling;
            var previousChevron = previous ? previous.querySelector('.chevron') : null;
            if (previousChevron) previousChevron.classList.remove('open');
        });

        if (!isOpen && items) {
            items.classList.add('show');
            if (chevron) chevron.classList.add('open');
        }
    }

    $.ajaxSetup({
        error: function (xhr) {
            if (xhr.status === 401) window.location.href = '/Account/Login';
        }
    });

    toastr.options = { closeButton: true, progressBar: true, positionClass: 'toast-top-right', timeOut: 3000 };

    document.addEventListener('click', function (event) {
        var toggle = event.target.closest('[data-nav-group-toggle]');
        if (!toggle) return;
        toggleGroup(toggle);
    });
})();
