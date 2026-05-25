(function () {
    function tabTarget(tab) {
        return '#marketing-' + tab;
    }

    function tabFromTarget(target) {
        return (target || '').replace('#marketing-', '');
    }

    function setQuickActive(tab) {
        document.querySelectorAll('[data-marketing-tab]').forEach(function (button) {
            button.classList.toggle('active', button.getAttribute('data-marketing-tab') === tab);
        });
    }

    function setUrlTab(tab) {
        if (!window.history || !window.URL) return;
        var url = new URL(window.location.href);
        url.searchParams.set('tab', tab);
        window.history.replaceState({}, '', url.pathname + '?' + url.searchParams.toString() + url.hash);
    }

    function showMarketingTab(tab) {
        var target = tabTarget(tab);
        var tabButton = document.querySelector('.nav-pills [data-bs-toggle="pill"][data-bs-target="' + target + '"]');
        if (!tabButton) return;

        if (window.bootstrap && window.bootstrap.Tab) {
            window.bootstrap.Tab.getOrCreateInstance(tabButton).show();
        } else {
            tabButton.click();
        }

        setQuickActive(tab);
        setUrlTab(tab);
    }

    document.addEventListener('click', function (event) {
        var trigger = event.target.closest('[data-marketing-tab]');
        if (!trigger) return;

        event.preventDefault();
        showMarketingTab(trigger.getAttribute('data-marketing-tab'));
    });

    document.querySelectorAll('.nav-pills [data-bs-toggle="pill"][data-bs-target^="#marketing-"]').forEach(function (button) {
        button.addEventListener('shown.bs.tab', function () {
            var tab = tabFromTarget(button.getAttribute('data-bs-target'));
            setQuickActive(tab);
            setUrlTab(tab);
        });

        if (button.classList.contains('active')) {
            setQuickActive(tabFromTarget(button.getAttribute('data-bs-target')));
        }
    });
})();
