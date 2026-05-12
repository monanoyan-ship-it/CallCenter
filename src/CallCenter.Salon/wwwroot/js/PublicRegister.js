(function () {
    function init() {
        var form = document.getElementById('register-form');
        var button = document.getElementById('regBtn');
        var errorBox = document.getElementById('error-msg');
        var successBox = document.getElementById('success-msg');
        if (!form || !button || !errorBox || !successBox) return;

        var returnUrl = new URLSearchParams(window.location.search).get('returnUrl') || '/user/panel';
        var loginLink = document.getElementById('loginLink');
        if (loginLink && returnUrl !== '/user/panel') {
            loginLink.href = '/user/login?returnUrl=' + encodeURIComponent(returnUrl);
        }

        if (window.salonPublicAuth && window.salonPublicAuth.getStoredPlatformToken()) {
            window.location.href = returnUrl;
            return;
        }

        button.addEventListener('click', async function () {
            if (!document.getElementById('kvkkConsent').checked) {
                errorBox.textContent = form.getAttribute('data-consent-required') || 'Onay zorunlu.';
                errorBox.classList.remove('d-none');
                return;
            }

            button.disabled = true;
            errorBox.classList.add('d-none');
            successBox.classList.add('d-none');

            try {
                var response = await fetch('/public-proxy/platform/register', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({
                        fullName: document.getElementById('fullName').value.trim(),
                        phone: window.salonPublicAuth.normalizePhoneValue('phoneCountry', 'phone'),
                        email: document.getElementById('email').value.trim() || null,
                        password: document.getElementById('password').value
                    })
                });
                var data = await response.json();
                var defaultError = form.getAttribute('data-register-failed') || 'Kayit basarisiz.';

                if (response.ok && (data.requiresEmailVerification || !data.token)) {
                    localStorage.removeItem('platformToken');
                    localStorage.removeItem('platformUser');
                    successBox.textContent = form.getAttribute('data-verify-required') || '';
                    successBox.classList.remove('d-none');
                    return;
                }

                if (!response.ok) throw new Error(data.message || defaultError);

                localStorage.setItem('platformToken', data.token);
                localStorage.setItem('platformUser', JSON.stringify(data.user));
                window.location.href = returnUrl;
            } catch (error) {
                errorBox.textContent = error.message;
                errorBox.classList.remove('d-none');
            } finally {
                button.disabled = false;
            }
        });
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', init, { once: true });
    } else {
        init();
    }
})();
