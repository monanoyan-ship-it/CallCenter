(function () {
    function closeMenus(exceptMenu) {
        document.querySelectorAll('.lang-menu').forEach(function (menu) {
            if (menu !== exceptMenu) menu.classList.remove('show');
        });
    }

    document.addEventListener('click', function (event) {
        var toggle = event.target.closest('[data-lang-toggle]');
        if (toggle) {
            event.preventDefault();
            var menu = toggle.parentElement ? toggle.parentElement.querySelector('.lang-menu') : null;
            if (!menu) return;
            var isOpen = menu.classList.contains('show');
            closeMenus(isOpen ? null : menu);
            menu.classList.toggle('show', !isOpen);
            return;
        }

        var option = event.target.closest('[data-lang-code]');
        if (option) {
            event.preventDefault();
            var pathSuffix = document.body.getAttribute('data-path-suffix') || '';
            var lang = option.getAttribute('data-lang-code');
            document.cookie = '.AspNetCore.Culture=c=' + lang + '|uic=' + lang + ';path=/;max-age=31536000';
            window.location.href = '/' + lang + pathSuffix;
            return;
        }

        if (!event.target.closest('.lang-dropdown')) {
            closeMenus(null);
        }
    });
})();
