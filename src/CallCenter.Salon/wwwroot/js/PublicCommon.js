(function () {
    if (window.__slnPublicCommonBound) return;
    window.__slnPublicCommonBound = true;

    function closeLanguageMenus(exceptMenu) {
        document.querySelectorAll('[data-lang-menu]').forEach(function (menu) {
            if (menu !== exceptMenu) menu.style.display = 'none';
        });
    }

    function switchLanguage(langCode) {
        document.cookie = '.AspNetCore.Culture=c=' + langCode + '|uic=' + langCode + ';path=/;max-age=31536000';
        window.location.reload();
    }

    document.addEventListener('click', function (event) {
        var toggle = event.target.closest('[data-lang-menu-toggle]');
        if (toggle) {
            event.preventDefault();
            var menu = toggle.parentElement ? toggle.parentElement.querySelector('[data-lang-menu]') : null;
            if (!menu) return;
            var isOpen = menu.style.display === 'block';
            closeLanguageMenus(isOpen ? null : menu);
            menu.style.display = isOpen ? 'none' : 'block';
            return;
        }

        var langOption = event.target.closest('[data-lang-code]');
        if (langOption) {
            event.preventDefault();
            switchLanguage(langOption.getAttribute('data-lang-code'));
            return;
        }

        var historyBack = event.target.closest('[data-history-back]');
        if (historyBack) {
            event.preventDefault();
            if (window.history.length > 1) window.history.back();
            else window.location.href = '/';
            return;
        }

        if (!event.target.closest('.lang-dropdown')) {
            closeLanguageMenus(null);
        }
    });
})();
