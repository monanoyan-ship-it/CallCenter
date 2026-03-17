// Generic OAuth2 popup helper — tum provider'lar icin kullanilir
window.openOAuthPopup = function (authUrl, successType, errorType, dotNetRef, callbackMethod) {
    var w = 600, h = 700;
    var left = (screen.width - w) / 2;
    var top = (screen.height - h) / 2;
    var popup = window.open(authUrl, 'OAuthPopup', 'width=' + w + ',height=' + h + ',left=' + left + ',top=' + top);

    var handler = function (event) {
        if (!event.data || typeof event.data !== 'object') return;
        if (event.data.type === successType) {
            window.removeEventListener('message', handler);
            dotNetRef.invokeMethodAsync(callbackMethod, event.data.code);
        } else if (event.data.type === errorType) {
            window.removeEventListener('message', handler);
            dotNetRef.invokeMethodAsync(callbackMethod, '');
        }
    };
    window.addEventListener('message', handler);

    // Popup kapanirsa temizle
    var check = setInterval(function () {
        if (!popup || popup.closed) {
            clearInterval(check);
            window.removeEventListener('message', handler);
        }
    }, 1000);
};

// OneDrive backward compat
window.openOneDriveAuth = function (authUrl, dotNetRef) {
    window.openOAuthPopup(authUrl, 'onedrive-auth-success', 'onedrive-auth-error', dotNetRef, 'OnOneDriveAuthCallback');
};

// OAuth callback sayfasindan cagrilir — popup icerisinde sonucu opener'a iletir ve kapatir
window.postOAuthResult = function (provider, code, error) {
    if (window.opener) {
        if (code) {
            window.opener.postMessage({ type: provider + '-auth-success', code: code }, '*');
        } else {
            window.opener.postMessage({ type: provider + '-auth-error', error: error || 'Bilinmeyen hata' }, '*');
        }
    }
    window.close();
};
