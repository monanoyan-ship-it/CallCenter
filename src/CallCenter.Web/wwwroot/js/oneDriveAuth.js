// OneDrive OAuth2 popup helper
window.openOneDriveAuth = function (authUrl, dotNetRef) {
    var w = 600, h = 700;
    var left = (screen.width - w) / 2;
    var top = (screen.height - h) / 2;
    var popup = window.open(authUrl, 'OneDriveAuth', 'width=' + w + ',height=' + h + ',left=' + left + ',top=' + top);

    var handler = function (event) {
        if (!event.data || typeof event.data !== 'object') return;
        if (event.data.type === 'onedrive-auth-success') {
            window.removeEventListener('message', handler);
            dotNetRef.invokeMethodAsync('OnOneDriveAuthCallback', event.data.code);
        } else if (event.data.type === 'onedrive-auth-error') {
            window.removeEventListener('message', handler);
            dotNetRef.invokeMethodAsync('OnOneDriveAuthCallback', '');
        }
    };
    window.addEventListener('message', handler);

    // Popup kapanırsa temizle
    var check = setInterval(function () {
        if (!popup || popup.closed) {
            clearInterval(check);
            window.removeEventListener('message', handler);
        }
    }, 1000);
};
