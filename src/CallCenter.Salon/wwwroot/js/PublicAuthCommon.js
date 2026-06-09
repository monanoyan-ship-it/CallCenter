(function () {
    function isTokenExpired(token) {
        try {
            var payload = token.split('.')[1];
            if (!payload) return false;
            payload = payload.replace(/-/g, '+').replace(/_/g, '/');
            while (payload.length % 4) payload += '=';
            var data = JSON.parse(atob(payload));
            return data.exp && (data.exp * 1000) <= Date.now();
        } catch (error) {
            return false;
        }
    }

    function clearStoredPlatformUser() {
        localStorage.removeItem('platformToken');
        localStorage.removeItem('platformUser');
    }

    function cleanDisplayText(value, fallback) {
        return (value || fallback || '').trim().replace(/[\s;'’‘"“”]+$/g, '').trim();
    }

    function getStoredPlatformToken() {
        var token = localStorage.getItem('platformToken');
        if (!token || token === 'null' || token === 'undefined' || isTokenExpired(token)) {
            clearStoredPlatformUser();
            return '';
        }
        return token;
    }

    function setPlatformAuthLink(link, signedIn) {
        var label = link.querySelector('[data-platform-auth-label]');
        var icon = link.querySelector('[data-platform-auth-icon]');
        var signedOutText = cleanDisplayText(link.getAttribute('data-signed-out-text'), 'Giris Yap');
        var signedInText = cleanDisplayText(link.getAttribute('data-signed-in-text'), 'Profilim');

        link.href = signedIn
            ? (link.getAttribute('data-signed-in-href') || '/user/panel')
            : (link.getAttribute('data-signed-out-href') || '/user/login');

        if (label) label.textContent = signedIn ? signedInText : signedOutText;
        if (icon) {
            icon.className = signedIn
                ? (link.getAttribute('data-signed-in-icon') || 'bi bi-person-circle me-1')
                : (link.getAttribute('data-signed-out-icon') || 'bi bi-box-arrow-in-right me-1');
        }

        link.setAttribute('data-auth-state', signedIn ? 'signed-in' : 'signed-out');
    }

    var api = window.salonPublicAuth || {};
    api.getStoredPlatformToken = getStoredPlatformToken;
    api.clearStoredPlatformUser = clearStoredPlatformUser;
    api.refreshPlatformAuthLinks = function () {
        var signedIn = !!getStoredPlatformToken();
        document.querySelectorAll('[data-platform-auth-link]').forEach(function (link) {
            setPlatformAuthLink(link, signedIn);
        });
    };
    api.normalizePhoneValue = function (countrySelectorId, phoneSelectorId) {
        var code = document.getElementById(countrySelectorId).value;
        var raw = document.getElementById(phoneSelectorId).value
            .replace(/[\s\-\(\)]/g, '')
            .replace(/\D/g, '');
        if (raw.startsWith('0')) raw = raw.substring(1);
        return code + raw;
    };
    window.salonPublicAuth = api;

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', window.salonPublicAuth.refreshPlatformAuthLinks, { once: true });
    } else {
        window.salonPublicAuth.refreshPlatformAuthLinks();
    }
})();
