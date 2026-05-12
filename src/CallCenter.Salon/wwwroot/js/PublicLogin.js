(function () {
    function init() {
        var form = document.getElementById('login-form');
        var button = document.getElementById('loginBtn');
        var errorBox = document.getElementById('error-msg');
        if (!form || !button || !errorBox) return;

        var returnUrl = new URLSearchParams(window.location.search).get('returnUrl') || '/user/panel';
        var registerLink = document.getElementById('registerLink');
        if (registerLink && returnUrl !== '/user/panel') {
            registerLink.href = '/user/register?returnUrl=' + encodeURIComponent(returnUrl);
        }

        if (window.salonPublicAuth && window.salonPublicAuth.getStoredPlatformToken()) {
            window.location.href = returnUrl;
            return;
        }

        button.addEventListener('click', async function () {
            button.disabled = true;
            errorBox.classList.add('d-none');
            var textNode = button.querySelector('.btn-text');
            if (textNode) textNode.textContent = button.getAttribute('data-loading-text') || textNode.textContent;

            try {
                var response = await fetch('/public-proxy/platform/login', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({
                        phone: window.salonPublicAuth.normalizePhoneValue('phoneCountry', 'phone'),
                        password: document.getElementById('password').value
                    })
                });
                var data = await response.json();
                var defaultError = form.getAttribute('data-login-failed') || 'Giris basarisiz.';

                if (response.ok && !data.token) throw new Error(data.message || defaultError);
                if (!response.ok) throw new Error(data.message || defaultError);

                localStorage.setItem('platformToken', data.token);
                localStorage.setItem('platformUser', JSON.stringify(data.user));
                window.location.href = returnUrl;
            } catch (error) {
                errorBox.textContent = error.message;
                errorBox.classList.remove('d-none');
            } finally {
                button.disabled = false;
                var idleText = button.getAttribute('data-idle-text');
                if (textNode && idleText) textNode.textContent = idleText;
            }
        });
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', init, { once: true });
    } else {
        init();
    }
})();
